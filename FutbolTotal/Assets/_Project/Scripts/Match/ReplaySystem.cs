using System.Collections.Generic;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Repetición de los goles. Graba continuamente (30 veces por segundo) la posición y rotación de los jugadores,
    /// sus articulaciones y el balón en un búfer circular. Al marcar, espera un instante, reproduce a cámara lenta
    /// los últimos segundos con una cámara baja que sigue al balón y devuelve el control.
    /// Se puede saltar con pase (J / A). Solo es visual: el partido sigue parado en la celebración y el saque
    /// inicial posterior recoloca a todos.
    /// </summary>
    [DefaultExecutionOrder(1000)] // después de animadores y cámaras: lo reproducido es lo último que se escribe
    public class ReplaySystem : MonoBehaviour
    {
        [Tooltip("Segundos que se guardan.")]
        [SerializeField] private float bufferSeconds = 6f;
        [Tooltip("Segundos antes del gol que se repiten.")]
        [SerializeField] private float secondsBeforeGoal = 3.5f;
        [Tooltip("Segundos después del gol que se repiten (balón en la red).")]
        [SerializeField] private float secondsAfterGoal = 0.8f;
        [Tooltip("Espera desde el gol hasta empezar la repetición.")]
        [SerializeField] private float startDelay = 1.3f;
        [SerializeField, Range(0.2f, 1f)] private float playbackSpeed = 0.5f;
        [SerializeField] private float samplesPerSecond = 30f;
        [Header("Cámara de la repetición")]
        [SerializeField] private float cameraHeight = 2.6f;
        [SerializeField] private float cameraDistance = 7f;

        private readonly List<Transform> tracked = new List<Transform>();
        private Vector3[,] positions;
        private Quaternion[,] rotations;
        private float[] times;
        private int capacity;
        private int head;   // siguiente hueco a escribir
        private int count;  // fotogramas válidos
        private float nextSample;

        private MatchContext context;
        private MatchManager manager;
        private FootballInput input;
        private MatchCamera matchCamera;
        private Transform cameraTransform;

        private float goalTime = -1f;
        private float replayStartsAt = -1f;
        private float playhead;
        private float replayFrom, replayTo;
        private Vector3 cameraPosition;

        /// <summary>True desde el gol hasta que termina (o se salta) la repetición.</summary>
        public bool IsBusy => replayStartsAt >= 0f || IsReplaying;
        public bool IsReplaying { get; private set; }

        private void Start()
        {
            context = MatchContext.Current;
            manager = context != null ? context.Rules as MatchManager : null;
            if (manager == null)
            {
                enabled = false;
                return;
            }

            foreach (FootballPlayer player in context.AllPlayers)
            {
                tracked.Add(player.transform);
                Transform visual = player.transform.Find("Visual");
                if (visual != null) tracked.AddRange(visual.GetComponentsInChildren<Transform>(true));
            }
            tracked.Add(context.Ball.transform);
            tracked.AddRange(context.Ball.GetComponentsInChildren<Transform>(true));

            capacity = Mathf.CeilToInt(bufferSeconds * samplesPerSecond) + 2;
            positions = new Vector3[capacity, tracked.Count];
            rotations = new Quaternion[capacity, tracked.Count];
            times = new float[capacity];

            var human = Object.FindFirstObjectByType<HumanTeamController>();
            input = human != null ? human.Input : null;
            Camera cam = Camera.main;
            if (cam != null)
            {
                cameraTransform = cam.transform;
                matchCamera = cam.GetComponent<MatchCamera>();
            }
            manager.GoalScored += OnGoal;
            manager.MatchStarted += Cancel;
        }

        private void OnDestroy()
        {
            if (manager == null) return;
            manager.GoalScored -= OnGoal;
            manager.MatchStarted -= Cancel;
        }

        private void OnGoal(Team scorer)
        {
            goalTime = Time.time;
            replayStartsAt = Time.time + startDelay;
        }

        private void LateUpdate()
        {
            if (IsReplaying)
            {
                Play();
                return;
            }

            if (replayStartsAt >= 0f && Time.time >= replayStartsAt) Begin();
            else Record();
        }

        // ------------------------------------------------------------------ Grabación

        private void Record()
        {
            if (manager.IsPaused || Time.time < nextSample) return;
            nextSample = Time.time + 1f / samplesPerSecond;
            for (int i = 0; i < tracked.Count; i++)
            {
                Transform t = tracked[i];
                if (t == null) continue;
                positions[head, i] = t.position;
                rotations[head, i] = t.rotation;
            }
            times[head] = Time.time;
            head = (head + 1) % capacity;
            count = Mathf.Min(count + 1, capacity);
        }

        private int Index(int age) => ((head - 1 - age) % capacity + capacity) % capacity;

        // ------------------------------------------------------------------ Reproducción

        private void Begin()
        {
            replayStartsAt = -1f;
            if (count < 2)
            {
                return;
            }
            float oldest = times[Index(count - 1)];
            float newest = times[Index(0)];
            replayFrom = Mathf.Max(oldest, goalTime - secondsBeforeGoal);
            replayTo = Mathf.Min(newest, goalTime + secondsAfterGoal);
            if (replayTo <= replayFrom) return;

            playhead = replayFrom;
            IsReplaying = true;
            if (matchCamera != null) matchCamera.enabled = false;
            if (cameraTransform != null) cameraPosition = cameraTransform.position;
        }

        private void Play()
        {
            bool skip = input != null && (input.ShortPass.ReleasedThisFrame || input.Shot.ReleasedThisFrame);
            playhead += Time.unscaledDeltaTime * playbackSpeed;
            if (skip || playhead >= replayTo || manager.State != MatchState.GoalCelebration)
            {
                End();
                return;
            }

            // Busca los dos fotogramas que rodean el instante actual y mezcla entre ellos.
            int newer = 0;
            while (newer < count - 1 && times[Index(newer + 1)] > playhead) newer++;
            int a = Index(Mathf.Min(newer + 1, count - 1));
            int b = Index(newer);
            float span = times[b] - times[a];
            float k = span > 1e-4f ? Mathf.Clamp01((playhead - times[a]) / span) : 1f;

            for (int i = 0; i < tracked.Count; i++)
            {
                Transform t = tracked[i];
                if (t == null) continue;
                t.SetPositionAndRotation(Vector3.Lerp(positions[a, i], positions[b, i], k),
                                         Quaternion.Slerp(rotations[a, i], rotations[b, i], k));
            }

            MoveCamera();
        }

        /// <summary>Cámara baja a pie de campo, del lado de la banda, siguiendo al balón con suavidad.</summary>
        private void MoveCamera()
        {
            if (cameraTransform == null) return;
            Vector3 ball = context.Ball.transform.position;
            Vector3 toCenter = KickMath.Flat(context.Pitch.Center - ball);
            Vector3 side = toCenter.sqrMagnitude > 0.01f ? toCenter.normalized : Vector3.back;
            Vector3 desired = ball + (side * 0.6f + Vector3.back * 0.8f).normalized * cameraDistance + Vector3.up * cameraHeight;
            cameraPosition = Vector3.Lerp(cameraPosition, desired, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
            cameraTransform.position = cameraPosition;
            cameraTransform.rotation = Quaternion.LookRotation(ball + Vector3.up * 0.6f - cameraPosition, Vector3.up);
        }

        private void End()
        {
            IsReplaying = false;
            // Deja todo como estaba al empezar la repetición (el último fotograma grabado = el presente).
            int last = Index(0);
            for (int i = 0; i < tracked.Count; i++)
            {
                Transform t = tracked[i];
                if (t != null) t.SetPositionAndRotation(positions[last, i], rotations[last, i]);
            }
            if (matchCamera != null)
            {
                matchCamera.enabled = true;
                matchCamera.SnapToTarget();
            }
            count = 0; // Se empieza a grabar de nuevo desde el saque.
        }

        private void Cancel()
        {
            replayStartsAt = -1f;
            if (IsReplaying) End();
        }
    }
}
