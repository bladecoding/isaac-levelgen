namespace isaac_levelgen
{
    public class GameStateInput
    {
        public uint StartSeed { get; set; }
        public int Stage { get; set; }
        public int StageVariant { get; set; }
        public bool[] Trinkets { get; set; } = new bool[1000];
        public bool IsHard { get; set; }
        //Note: Starting items are given after stage 1 generation
        public int ActiveItem { get; set; }
        public int Keys { get; set; }
        public int Coins { get; set; }
        public int SoulHearts { get; set; }
        public int Hearts { get; set; }
        public int MaxHearts { get; set; }
        public int BoneHearts { get; set; }
        public PlayerType Character { get; set; }
        public int CalculateStageVariant() {
            if (Stage < 1 && Stage > 8)
                return -1; // Variant isn't random beyond stage 8
            var variant = 0;
            if ((StartSeed & 1) == 0)
                variant = 1;
            if (StartSeed % 3 == 0)
                variant = 2;
            return variant;
        }

        public static int GetStageId(int stage, int variant) {
            if (variant == 3)
                return stage + 0x12; //Greed mode
            if (stage < 9)
                return variant + 1 + ((stage - 1) / 2 * 3);
            if (stage == 9)
                return 0xD;
            return variant + (stage * 2) - 6;
        }
    }

}
