namespace isaac_levelgen
{
    public class LayoutState
    {
        public int Stage { get; set; }
        public int StageVariant { get; set; }
        public Rng Seed { get; set; }
        public RoomsProvider Provider { get; set; }

        public bool[] EnabledShapes { get; set; } = new bool[13];
        public bool MegaSatanDoorExists { get { return Stage == 0xB; } }
        public bool IsXL { get; set; }
        public bool IsVoid { get { return Stage == 0xC; } }
        public bool IsShapeEnabled(RoomShape shape) { return EnabledShapes[(int)shape]; }

        public int StageId {
            get {
                return GameStateInput.GetStageId(Stage, StageVariant);
            }
        }

        public LayoutState(uint seed, int stage, int stageVariant, RoomsProvider provider) {
            Seed = new Rng(seed, 5, 9, 7);
            Stage = stage;
            StageVariant = stageVariant;
            Provider = provider;
        }

        public void CalculateEnabledShapes(uint minDiff, uint maxDiff) {
            EnabledShapes = Provider.GetEnabledShapes(StageId, minDiff, maxDiff);
        }
    }

}
