using System.Collections.Generic;
using UnityEngine;

namespace FutbolTotal
{
    /// <summary>
    /// Un equipo: identidad, dirección de ataque, formación y lista de jugadores (los hijos de este objeto).
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class Team : MonoBehaviour
    {
        [SerializeField] private TeamSettings settings;
        [SerializeField] private FormationSettings formation;
        [SerializeField] private TeamSide side = TeamSide.Home;
        [Tooltip("+1: ataca hacia +X. -1: ataca hacia -X. Se invertirá en el descanso (Fase 2).")]
        [SerializeField] private int attackDirection = 1;

        private readonly List<FootballPlayer> players = new List<FootballPlayer>();

        public TeamSettings Settings => settings;
        public FormationSettings Formation => formation;
        public TeamSide Side => side;
        public int AttackDirection => attackDirection;
        public IReadOnlyList<FootballPlayer> Players => players;
        public Team Opponent { get; private set; }

        public bool HasBall
        {
            get
            {
                var context = MatchContext.Current;
                return context != null && context.Ball.Owner != null && context.Ball.Owner.Team == this;
            }
        }

        public Vector3 OwnGoalCenter => MatchContext.Current.Pitch.GoalCenter(-attackDirection);
        public Vector3 OpponentGoalCenter => MatchContext.Current.Pitch.GoalCenter(attackDirection);

        public FootballPlayer Goalkeeper
        {
            get
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].IsGoalkeeper) return players[i];
                }
                return null;
            }
        }

        private void Awake()
        {
            GetComponentsInChildren(true, players);
            foreach (FootballPlayer player in players) player.Initialize(this);
        }

        public void SetOpponent(Team opponent) => Opponent = opponent;

        public void SetAttackDirection(int direction) => attackDirection = direction >= 0 ? 1 : -1;

        /// <summary>Convierte una posición de formación normalizada en coordenadas del mundo para este equipo.</summary>
        public Vector3 FormationToWorld(Vector2 normalized)
        {
            Pitch pitch = MatchContext.Current.Pitch;
            return FormationToWorld(normalized, attackDirection, pitch.Center, pitch.HalfLength, pitch.HalfWidth);
        }

        /// <summary>Versión pura (sin escena) que también usa el generador de escenas del editor.</summary>
        public static Vector3 FormationToWorld(Vector2 normalized, int attackDirection, Vector3 pitchCenter,
                                               float halfLength, float halfWidth)
        {
            float x = (-halfLength + normalized.x * halfLength * 2f) * attackDirection;
            // Mirando hacia la portería rival, la derecha del equipo es -Z cuando ataca hacia +X.
            float z = -normalized.y * halfWidth * attackDirection;
            return pitchCenter + new Vector3(x, 0f, z);
        }

        public Vector2 SlotBasePosition(int index)
        {
            if (formation == null || index < 0 || index >= formation.slots.Count) return new Vector2(0.3f, 0f);
            return formation.slots[index].basePosition;
        }

        /// <summary>Coloca a todos los jugadores en su posición de saque inicial (en su propio campo).</summary>
        public void ResetToKickoffPositions()
        {
            float maxDepth = formation != null ? formation.kickoffMaxDepth : 0.46f;
            foreach (FootballPlayer player in players)
            {
                Vector2 slot = SlotBasePosition(player.FormationIndex);
                slot.x = Mathf.Min(slot.x, maxDepth);
                player.Motor.Teleport(FormationToWorld(slot), new Vector3(attackDirection, 0f, 0f));
                player.Stamina.Refill();
            }
        }
    }
}
