using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Sonido del partido. Solo escucha eventos (no cambia el juego):
    /// golpeos y botes del balón, postes y red, silbato (saques, faltas, descanso y final),
    /// público de fondo que se anima cerca de las porterías y celebración de los goles.
    /// </summary>
    public class MatchAudio : MonoBehaviour
    {
        [SerializeField] private SoundSettings settings;

        private MatchContext context;
        private MatchManager manager;
        private AudioSource effects;
        private AudioSource crowd;
        private float lastBounceTime;

        private void Awake()
        {
            settings = SettingsProvider.Resolve(settings, SettingsProvider.SoundName, this);
            AudioListener.volume = GameOptions.Volume;

            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;

            crowd = gameObject.AddComponent<AudioSource>();
            crowd.clip = ProceduralAudio.CrowdLoop;
            crowd.loop = true;
            crowd.playOnAwake = false;
            crowd.volume = settings.crowd;
        }

        private void Start()
        {
            context = MatchContext.Current;
            if (context == null)
            {
                enabled = false;
                return;
            }
            crowd.Play();

            context.Ball.Kicked += OnKicked;
            context.Ball.Bounced += OnBounced;
            context.FoulCommitted += OnFoul;
            manager = context.Rules as MatchManager;
            if (manager != null)
            {
                manager.StateChanged += OnStateChanged;
                manager.GoalScored += OnGoal;
            }
        }

        private void OnDestroy()
        {
            if (context == null) return;
            context.Ball.Kicked -= OnKicked;
            context.Ball.Bounced -= OnBounced;
            context.FoulCommitted -= OnFoul;
            if (manager != null)
            {
                manager.StateChanged -= OnStateChanged;
                manager.GoalScored -= OnGoal;
            }
        }

        private void Update()
        {
            // El público se anima cuando el balón se acerca a una portería; en pausa baja.
            Pitch pitch = context.Pitch;
            Vector3 ball = context.Ball.Position;
            float toGoal = Mathf.Min(KickMath.FlatDistance(ball, pitch.GoalCenter(1)), KickMath.FlatDistance(ball, pitch.GoalCenter(-1)));
            float excitement = Mathf.InverseLerp(30f, 8f, toGoal);
            bool paused = context.Rules != null && context.Rules.IsPaused;
            float target = paused ? settings.crowd * 0.4f : settings.crowd + settings.crowdExcitement * excitement;
            crowd.volume = Mathf.MoveTowards(crowd.volume, target, Time.unscaledDeltaTime * 0.5f);
        }

        private void OnKicked(KickEvent kick)
        {
            float strength = Mathf.Clamp01(kick.Velocity.magnitude / Mathf.Max(1f, settings.kickSpeedForFullVolume));
            effects.pitch = Mathf.Lerp(1.1f, 0.9f, strength);
            effects.PlayOneShot(ProceduralAudio.Kick, settings.kick * Mathf.Lerp(0.35f, 1f, strength));
            effects.pitch = 1f;
        }

        private void OnBounced(Collision collision)
        {
            float speed = collision.relativeVelocity.magnitude;
            if (speed < 1.5f || Time.time - lastBounceTime < 0.05f) return;
            lastBounceTime = Time.time;
            float strength = Mathf.Clamp01(speed / 15f);

            string name = collision.collider.name;
            if (name.StartsWith("Post") || name.StartsWith("Crossbar"))
                effects.PlayOneShot(ProceduralAudio.Post, settings.post * Mathf.Lerp(0.4f, 1f, strength));
            else if (name.StartsWith("Net"))
                effects.PlayOneShot(ProceduralAudio.Net, settings.net * Mathf.Lerp(0.4f, 1f, strength));
            else
                effects.PlayOneShot(ProceduralAudio.Bounce, settings.bounce * strength);
        }

        private void OnFoul(FoulEvent foul) => effects.PlayOneShot(ProceduralAudio.ShortWhistle, settings.whistle);

        private void OnGoal(Team scorer) => effects.PlayOneShot(ProceduralAudio.Cheer, settings.cheer);

        private void OnStateChanged(MatchState state)
        {
            switch (state)
            {
                case MatchState.Kickoff:
                    effects.PlayOneShot(ProceduralAudio.ShortWhistle, settings.whistle);
                    break;
                case MatchState.HalfTime:
                    effects.PlayOneShot(ProceduralAudio.LongWhistle, settings.whistle);
                    break;
                case MatchState.FullTime:
                    // Tres pitidos: dos cortos y uno largo.
                    effects.PlayOneShot(ProceduralAudio.ShortWhistle, settings.whistle);
                    Invoke(nameof(SecondWhistle), 0.45f);
                    Invoke(nameof(FinalWhistle), 0.9f);
                    break;
            }
        }

        private void SecondWhistle() => effects.PlayOneShot(ProceduralAudio.ShortWhistle, settings.whistle);
        private void FinalWhistle() => effects.PlayOneShot(ProceduralAudio.LongWhistle, settings.whistle);
    }
}
