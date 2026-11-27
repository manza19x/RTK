namespace FutbolTotal
{
    /// <summary>
    /// Ajusta una copia de <see cref="AISettings"/> para el equipo rival según la dificultad elegida.
    /// Cambia tiempos de reacción y capacidades, nunca añade azar: la IA sigue siendo predecible.
    /// </summary>
    public static class DifficultyScaling
    {
        public static void Apply(AISettings s, DifficultyOption difficulty)
        {
            switch (difficulty)
            {
                case DifficultyOption.Easy:
                    s.tackleReactionDelay *= 1.6f;
                    s.keeperReaction *= 1.5f;
                    s.diveSpeed *= 0.8f;
                    s.carrierDecisionInterval *= 1.4f;
                    s.shootRange *= 0.85f;
                    s.pressLead = 0f;
                    break;
                case DifficultyOption.Hard:
                    s.tackleReactionDelay *= 0.7f;
                    s.keeperReaction *= 0.75f;
                    s.diveSpeed *= 1.15f;
                    s.carrierDecisionInterval *= 0.8f;
                    s.minShootAngle *= 0.85f;
                    break;
            }
        }
    }
}
