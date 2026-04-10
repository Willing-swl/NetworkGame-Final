using RenderMoodSnapshot = NanoFrame.Rendering.RenderMoodManager.RenderMoodSnapshot;

namespace NanoFrame.Rendering
{
    [System.Serializable]
    public class RenderMoodProfileData
    {
        public float IntroEndTime;
        public float CombatEndTime;
        public float ClimaxEndTime;
        public float MatchDuration;
        public float PhaseBlendSpeed;
        public RenderMoodSnapshot IntroSnapshot;
        public RenderMoodSnapshot CombatSnapshot;
        public RenderMoodSnapshot ClimaxSnapshot;
        public RenderMoodSnapshot EliminationSnapshot;

        public static RenderMoodProfileData CreateDefault()
        {
            return new RenderMoodProfileData
            {
                IntroEndTime = 20f,
                CombatEndTime = 35f,
                ClimaxEndTime = 50f,
                MatchDuration = 60f,
                PhaseBlendSpeed = 2.5f,
                IntroSnapshot = RenderMoodManager.CreateDefaultSnapshot(RenderMoodPhase.Intro),
                CombatSnapshot = RenderMoodManager.CreateDefaultSnapshot(RenderMoodPhase.Combat),
                ClimaxSnapshot = RenderMoodManager.CreateDefaultSnapshot(RenderMoodPhase.Climax),
                EliminationSnapshot = RenderMoodManager.CreateDefaultSnapshot(RenderMoodPhase.Elimination)
            };
        }
    }
}
