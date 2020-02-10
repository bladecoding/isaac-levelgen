using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isaac_levelgen
{
    public class GameState
    {
        public int Stage { get; set; }
        public int StageVariant { get; set; }

        public LevelCurse Curse { get; set; }

        public bool IsHard { get; set; }

        public bool HasBlackCandle { get; set; }

        public Rng StageSeed { get; set; } //5 9 7
        public bool[] Secrets { get; set; } = Enumerable.Range(0, 0x200).Select(_ => true).ToArray(); //All secrets enabled

        public bool[] Flags { get; set; } = new bool[(int)GameStateFlags.NUM_STATE_FLAGS]; //Note: This number is wrong on the doc

        // Player States
        public bool[] Trinkets { get; set; }
        public int ActiveItem { get; set; }
        public int Keys { get; set; }
        public int Coins { get; set; }
        public int SoulHearts { get; set; }
        public int Hearts { get; set; }
        public int MaxHearts { get; set; }
        public int BoneHearts { get; set; }
        public PlayerType Character { get; set; }

        public GameState(GameStateInput input) {
            StageSeed = new Rng(input.StartSeed, 5, 9, 7);
            IsHard = input.IsHard;
            Stage = input.Stage;
            StageVariant = input.StageVariant;
            Trinkets = input.Trinkets;
            ActiveItem = input.ActiveItem;
            Keys = input.Keys;
            Coins = input.Coins;
            SoulHearts = input.SoulHearts;
            Hearts = input.Hearts;
            MaxHearts = input.MaxHearts;
            BoneHearts = input.BoneHearts;
            Character = input.Character;
        }

        public void SetFlag(GameStateFlags flag, bool val) {
            Flags[(int)flag] = val;
        }
        public bool GetFlag(GameStateFlags flag) {
            return Flags[(int)flag];
        }

        public int StageId {
            get {
                if (StageVariant == 3)
                    return Stage + 0x12; //Greed mode
                if (Stage < 9)
                    return StageVariant + 1 + ((Stage - 1) / 2 * 3);
                if (Stage == 9)
                    return 0xD;
                return StageVariant + (Stage * 2) - 6;
            }
        }
    }

}
