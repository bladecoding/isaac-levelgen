using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isaac_levelgen
{
    class Program
    {
        static void Main(string[] args) {
            var roomDescs = new RoomsProvider();
            // Xml room files
            roomDescs.Load(@"C:\Program Files (x86)\Steam\steamapps\common\The Binding of Isaac Rebirth\resources\rooms");

            var tLayouts = 0;
            var mLayouts = 0;
            var tLevels = 0;
            var mLevels = 0;

            // Dump files for testing
            foreach (var fp in new DirectoryInfo(@"dumps").GetFiles()) {
                using (var file = File.OpenRead(fp.FullName)) {
                    Console.WriteLine(file.Name);
                    var reader = new DumpReader(file, roomDescs);
                    DumpStageBlock sb;
                    var total = 0;
                    while ((sb = reader.Read()) != null) {
                        Console.WriteLine(Rng.SeedToString(sb.Seed));
                        var seed = new Rng(sb.Seed, 0x3, 0x17, 0x19);
                        var stageSeeds = Enumerable.Range(0, 14).Select(_ => seed.Next()).ToArray();

                        for (var i = 0; i < sb.Stages.Length; i++) {
                            if (sb.Stages[i].StageId == 0x9 || sb.Stages[i].StageId == 0xC)
                                continue;
                            var lg = new LevelGen(roomDescs);
                            var input = new GameStateInput() {
                                IsHard = true,
                                Stage = sb.Stages[i].StageId,
                                Hearts = 6,
                                MaxHearts = 6,
                                Character = PlayerType.PLAYER_ISAAC
                            };
                            input.StageVariant = sb.Stages[i].StageType;//input.CalculateStageVariant();
                            input.StartSeed = stageSeeds[input.Stage];

                            //if (total != 0x131 || i != 0) continue;

                            var level = lg.CreateLevel(input);
                            tLevels++;
                            if (CompareLevels(sb.Stages[i], level))
                                mLevels++;
                            tLayouts++;
                            if (CompareLayouts(sb.Stages[i], level))
                                mLayouts++;
                        }
                        total++;
                    }
                }
            }

            Console.ReadLine();
        }

        public static bool CompareLevels(DumpStage dump, StageLayout layout) {
            for (var y = 0; y < 13; y++) {
                for (var x = 0; x < 13; x++) {
                    var dr = dump.RoomGrid[y][x];
                    var r = layout.RoomGrid[y][x];
                    if (dr == null && r == null)
                        continue;
                    if (dr != null && r != null && dr.RoomId == r.RoomId && dr.RoomType == r.RoomType && dr.RoomSubType == r.RoomSubType && dr.Shape == r.Shape)
                        continue;

                    return false;
                }
            }
            return true;
        }

        public static bool CompareLayouts(DumpStage dump, StageLayout layout) {
            for (var y = 0; y < 13; y++) {
                for (var x = 0; x < 13; x++) {
                    var dr = dump.RoomGrid[y][x];
                    var r = layout.RoomGrid[y][x];
                    if (dr == null && r == null)
                        continue;
                    if (dr != null && r != null)
                        continue;

                    return false;
                }
            }
            return true;
        }

        public static void PrintLayout(DumpStage layout) {
            for (var y = 0; y < 13; y++) {
                for (var x = 0; x < 13; x++) {
                    var room = layout.RoomGrid[y][x];
                    var str = room != null ? $"{(int)room.RoomType}-{room.RoomId}" : "";
                    Console.Write(str.PadLeft(8,' '));
                }
                Console.WriteLine();
            }
        }
        public static void PrintLayout(StageLayout layout) {
            for (var y = 0; y < 13; y++) {
                for (var x = 0; x < 13; x++) {
                    var room = layout.RoomGrid[y][x];
                    var str = room != null ? $"{(int)room.RoomType}-{room.RoomId}" : "";
                    Console.Write(str.PadLeft(8, ' '));
                }
                Console.WriteLine();
            }
        }
    }
}
