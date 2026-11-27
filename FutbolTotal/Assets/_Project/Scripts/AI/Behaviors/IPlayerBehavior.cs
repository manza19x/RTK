namespace FutbolTotal
{
    /// <summary>
    /// Un comportamiento de IA. Sin estado propio: la memoria vive en <see cref="PlayerBrain"/>,
    /// así una única instancia sirve para todos los jugadores con ese rol.
    /// </summary>
    public interface IPlayerBehavior
    {
        void Tick(PlayerBrain brain);
    }

    /// <summary>Instancias compartidas de cada comportamiento.</summary>
    public static class AIBehaviors
    {
        public static readonly IPlayerBehavior Goalkeeper = new GoalkeeperBehavior();
        public static readonly IPlayerBehavior BallCarrier = new BallCarrierBehavior();
        public static readonly IPlayerBehavior Support = new SupportBehavior();
        public static readonly IPlayerBehavior Chase = new PressBehavior(PressBehavior.Mode.Chase);
        public static readonly IPlayerBehavior Press = new PressBehavior(PressBehavior.Mode.Tackle);
        public static readonly IPlayerBehavior Contain = new PressBehavior(PressBehavior.Mode.Contain);
        public static readonly IPlayerBehavior Zone = new ZoneCoverBehavior();

        public static IPlayerBehavior For(AIRole role)
        {
            switch (role)
            {
                case AIRole.Goalkeeper: return Goalkeeper;
                case AIRole.BallCarrier: return BallCarrier;
                case AIRole.Support: return Support;
                case AIRole.Chase: return Chase;
                case AIRole.Press: return Press;
                case AIRole.Contain: return Contain;
                default: return Zone;
            }
        }
    }
}
