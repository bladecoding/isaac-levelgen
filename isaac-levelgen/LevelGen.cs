using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace isaac_levelgen
{
    public class LevelGen
    {
        public LayoutState State;
        public GameState Game;
        public RoomsProvider Provider;
        public LayoutGenerator LayoutGen;

        public LevelGen(RoomsProvider provider) {
            Provider = provider;

        }

        public StageLayout CreateLevel(GameStateInput input) {
            Game = new GameState(input);

            Game.StageSeed.Next(); //?
            Game.StageSeed.Next(); //?

            //This is the curse chance with all unlocks
            var curseChance = Game.IsHard ? 0x2 : 0x5;

            Game.Curse = LevelCurse.CURSE_NONE;
            var rng = Game.StageSeed.Clone();
            if (!Game.HasBlackCandle && rng.NextInt(curseChance) == 0) {
                switch (rng.Next() % 6) {
                    case 0:
                        if (CanStageHaveCurseOfLabyrinth(Game.Stage))
                            Game.Curse |= LevelCurse.CURSE_OF_LABYRINTH;
                        break;
                    case 1:
                        Game.Curse |= LevelCurse.CURSE_OF_THE_LOST;
                        break;
                    case 2:
                        Game.Curse |= LevelCurse.CURSE_OF_DARKNESS;
                        break;
                    case 3:
                        Game.Curse |= LevelCurse.CURSE_OF_THE_UNKNOWN;
                        break;
                    case 4:
                        Game.Curse |= LevelCurse.CURSE_OF_MAZE;
                        break;
                    case 5:
                        Game.Curse |= LevelCurse.CURSE_OF_BLIND;
                        break;
                }
            }

            Provider.ResetWeights(0);
            Provider.ResetWeights(Game.StageId);

            return CreateLevel2(rng);
        }

        StageLayout CreateLevel2(Rng seed) {

            var roomCount = Math.Min(20, Game.StageSeed.NextInt(2) + 5 + (Game.Stage * 10 / 3));
            if ((Game.Curse & LevelCurse.CURSE_OF_LABYRINTH) != 0)
                roomCount = Math.Min(45, (int)(roomCount * 1.8d));
            else if ((Game.Curse & LevelCurse.CURSE_OF_THE_LOST) != 0)
                roomCount += 4;
            var hardAdditional = seed.NextInt(2);
            if (Game.IsHard)
                roomCount += 2 + hardAdditional;

            //Todo: Combine layout/game states
            State = new LayoutState(Game.StageSeed.Next(), Game.Stage, Game.StageVariant, Provider);
            State.IsXL = (Game.Curse & LevelCurse.CURSE_OF_LABYRINTH) != 0;

            uint minDiff;
            uint maxDiff;
            if (Game.IsHard) {
                minDiff = Game.Stage < 9 && Game.Stage % 2 == 0 ? 0xAu : 0x5u;
                maxDiff = Game.Stage < 9 && Game.Stage % 2 == 0 ? 0xFu : 5u;
            } else {
                minDiff = Game.Stage < 9 && Game.Stage % 2 == 0 ? 0x5u : 1u;
                maxDiff = Game.Stage < 9 && Game.Stage % 2 == 0 ? 0xau : 0x5u;
            }

            State.CalculateEnabledShapes(minDiff, maxDiff);

            var minDeadEnds = 5;
            if (Game.Stage != 1)
                minDeadEnds += 1;
            if ((Game.Curse & LevelCurse.CURSE_OF_LABYRINTH) != 0)
                minDeadEnds += 1;

            LayoutGen = new LayoutGenerator(Game, State);
            StageLayout layout;
            for (int i = 0; i < 100; i++) {
                layout = LayoutGen.Create(roomCount, minDeadEnds);

                if (layout.DeadEnds.Count < minDeadEnds)
                    continue;

                if (!PlaceRooms(layout, minDiff, maxDiff))
                    continue;

                return layout;
            }

            return null; //Failed to make stage in 100 loops D:
        }

        public int ChooseBossRoomSubtype(int stage) {
            var variant = Game.StageVariant;
            var seed = Game.StageSeed;

            var headlessRng = new Rng(Game.StageSeed.Next(), 0x1, 0x5, 0x10);

            if (stage == 6) //Depths
                return 6; //Mom's foot

            if (stage == 8) {
                //It lives and Heart
                return Game.Secrets[0x22] ? 0x19 : 0x8;
            }

            if (stage == 0xb) {
                //??? and lamb
                return variant != 1 ? 0x36 : 0x28;
            }

            if (stage == 0xa) {
                //isaac and satan
                return variant != 1 ? 0x18 : 0x27;
            }

            if (stage == 0xc) //void
                return 0x46;

            if (Game.Secrets[0x15b]) //Something wicked this way comes+!
            {
                if (stage == 0x7) {
                    var chance = variant == 2 ? .33f : .1f;
                    if (seed.NextFloat() < chance)
                        return 0x48; //matriarch
                }
                if (seed.NextInt(0x5) == 0) {
                    if (stage == 0x3) {
                        Game.SetFlag(GameStateFlags.STATE_ABPLUS_BOSS_SWITCHED, seed.NextInt(2) == 1);

                    }
                    //Bug? Should be ABP
                    var switched = Game.GetFlag(GameStateFlags.STATE_AFTERBIRTH_BOSS_SWITCHED);
                    if (stage == 0x3) {
                        return 0x43 + (switched ? 0x0 : 0x2); //Rag Mega and Big Horn
                    }
                    if (stage == 0x4) {
                        return 0x43 + (switched ? 0x2 : 0x0); //Rag Mega and Big Horn
                    }
                    if (stage == 0x5 || stage == 0x6 || stage == 0x7) {
                        if (!Game.GetFlag(GameStateFlags.STATE_SISTERS_VIS_SELECTED)) {
                            Game.SetFlag(GameStateFlags.STATE_SISTERS_VIS_SELECTED, true);
                            return 0x44; //sister vis
                        }
                    }
                }
            }

            if (Game.Secrets[0x15a]) //Something wicked this way comes!
            {
                if (seed.NextInt(0x4) == 0) {
                    if (stage < 9 && (stage & 1) == 1) //odd stages
                    {
                        Game.SetFlag(GameStateFlags.STATE_AFTERBIRTH_BOSS_SWITCHED, seed.NextInt(2) == 1);
                    }

                    var switched = Game.GetFlag(GameStateFlags.STATE_AFTERBIRTH_BOSS_SWITCHED);
                    var randBoss = seed.NextInt(0x2) == 1 ? 0x3B : 0x42;
                    if (stage == 1) {
                        return 0x3c + (switched ? 0 : 1); //ragman and little horn 
                    }
                    if (stage == 2) {
                        return 0x3c + (switched ? 1 : 0); //ragman and little horn 
                    }
                    if (stage == 3) {
                        return (switched ? 0x39 : randBoss);
                    }
                    if (stage == 4) {
                        return (switched ? randBoss : 0x39);
                    }
                    if (stage == 5 || stage == 6) {
                        if (!Game.GetFlag(GameStateFlags.STATE_BROWNIE_SELECTED)) {
                            Game.SetFlag(GameStateFlags.STATE_BROWNIE_SELECTED, true);
                            return 0x3A; //brownie
                        }
                    }
                }
            }

            if (seed.NextInt(0x6) == 0) {
                if (stage < 9 && (stage & 1) == 1) //odd stages
                {
                    Game.SetFlag(GameStateFlags.STATE_REBIRTH_BOSS_SWITCHED, seed.NextInt(2) == 1);
                }

                var switched = Game.GetFlag(GameStateFlags.STATE_REBIRTH_BOSS_SWITCHED);
                var randBoss1 = seed.NextInt(0x2) == 1 ? 0x40 : 0x2c;
                var randBoss2 = seed.NextInt(0x2) == 1 ? 0x41 : 0x38;
                if (!Game.Secrets[0x15a]) //Something wicked this way comes!
                {
                    randBoss1 = 0x2c;
                    randBoss2 = 0x38;
                }
                if (variant == 0) {
                    if (stage == 1) {
                        return (switched ? randBoss1 : randBoss2);
                    }
                    if (stage == 2) {
                        return (switched ? randBoss2 : randBoss1);
                    }
                    if (stage == 3) {
                        return (switched ? 0x2f : 0x2d);
                    }
                    if (stage == 4) {
                        return (switched ? 0x2d : 0x2f);
                    }
                    if (stage == 5) {
                        return (switched ? 0x30 : 0x2e);
                    }
                    if (stage == 6) {
                        return (switched ? 0x2e : 0x30);
                    }
                    if (stage == 7 || stage == 8) {
                        if (!Game.GetFlag(GameStateFlags.STATE_MR_FRED_SELECTED)) {
                            Game.SetFlag(GameStateFlags.STATE_MR_FRED_SELECTED, true);
                            return 0x35; //Mr Fred
                        }
                    }
                } else {
                    if (stage == 1 || stage == 2) {
                        if (!Game.GetFlag(GameStateFlags.STATE_HAUNT_SELECTED)) {
                            Game.SetFlag(GameStateFlags.STATE_HAUNT_SELECTED, true);
                            return 0x2b; //Haunt
                        }
                    }
                    if (stage == 3) {
                        return (switched ? 0x32 : 0x34);
                    }
                    if (stage == 4) {
                        return (switched ? 0x34 : 0x32);
                    }
                    if (stage == 5 || stage == 6) {
                        if (!Game.GetFlag(GameStateFlags.STATE_ADVERSARY_SELECTED)) {
                            Game.SetFlag(GameStateFlags.STATE_ADVERSARY_SELECTED, true);
                            return 0x33; //Adversary
                        }
                    }
                    if (stage == 7 || stage == 8) {
                        if (!Game.GetFlag(GameStateFlags.STATE_MAMA_GURDY_SELECTED)) {
                            Game.SetFlag(GameStateFlags.STATE_MAMA_GURDY_SELECTED, true);
                            return 0x31; //Mama Gurdy
                        }
                    }
                }
            }

            var swapDeath = seed.NextInt(0x2) == 0 && Game.Secrets[0x42];
            if (seed.NextInt(0x5) == 0 && Game.Secrets[0x5]) {
                if (stage == 1 || stage == 3 || stage == 5 || stage == 7) {
                    if (headlessRng.NextInt(0xA) == 0) {
                        if (!Game.GetFlag(GameStateFlags.STATE_HEADLESS_HORSEMAN_SPAWNED)) {
                            Game.SetFlag(GameStateFlags.STATE_HEADLESS_HORSEMAN_SPAWNED, true);
                            return 0x16;
                        }
                    }
                    if (stage == 1) {
                        if (!Game.GetFlag(GameStateFlags.STATE_FAMINE_SPAWNED)) {
                            Game.SetFlag(GameStateFlags.STATE_FAMINE_SPAWNED, true);
                            return 0x9;
                        }
                    }
                    if (stage == 3) {
                        if (!Game.GetFlag(GameStateFlags.STATE_PESTILENCE_SPAWNED)) {
                            Game.SetFlag(GameStateFlags.STATE_PESTILENCE_SPAWNED, true);
                            return 0xa;
                        }
                    }
                    if (stage == 5) {
                        if (!Game.GetFlag(GameStateFlags.STATE_WAR_SPAWNED)) {
                            Game.SetFlag(GameStateFlags.STATE_WAR_SPAWNED, true);
                            return 0xb;
                        }
                    }
                    if (stage == 7) {
                        if (!Game.GetFlag(GameStateFlags.STATE_DEATH_SPAWNED)) {
                            Game.SetFlag(GameStateFlags.STATE_DEATH_SPAWNED, true);
                            return swapDeath ? 0x26 : 0xC;
                        }
                    }
                }
            }

            var unk1 = seed.NextInt(0x2) == 0 && Game.Secrets[0x44];
            if (seed.NextInt(0x3) == 0 && stage == 0x7) {
                return unk1 ? 0x2a : 0x29; //Triachnid and daddy long legs
            }

            if (seed.NextInt(0xA) == 0 && Game.GetFlag(GameStateFlags.STATE_DEVILROOM_VISITED) && !Game.GetFlag(GameStateFlags.STATE_FALLEN_SPAWNED)) {
                Game.SetFlag(GameStateFlags.STATE_FALLEN_SPAWNED, true);
                return 0x17; //Fallen
            }

            if (seed.NextInt(0x3) == 0 && stage == 0x7) {
                return seed.NextInt(0x2) == 0 ? 0x1E : 0x21;
            }

            if (seed.NextInt(0x4) == 0 && variant == 1) {
                if (stage == 2) {
                    return seed.NextInt(0x2) == 0 ? 0x12 : 0x20;
                }
                if (stage == 4) {
                    return seed.NextInt(0x2) == 0 ? 0x24 : 0x1b;
                }
            }

            if (seed.NextInt(0x5) == 0) {
                if (stage == 1) {
                    return 0xd;
                }
                if (stage == 3) {
                    return 0xe;
                }
                if (stage == 5) {
                    return 0xf;
                }
                if (stage == 7) {
                    return 0x10;
                }
            }

            var unk2 = seed.NextInt(0x2) == 1 && Game.Secrets[0x10];
            if (seed.NextInt(0x4) == 0) {
                if (stage == 1 && variant == 0) {
                    return unk2 ? 0x14 : 0x11;
                }
                if (stage == 3) {
                    return 0x1C;
                }
            }

            var unk3 = seed.NextInt(0x2) != 0 && Game.Secrets[0x11];
            var unk4 = seed.NextInt(0x2) != 0 && Game.Secrets[0x12];

            var table1 = new int[]
            {
                0x2,
                0x1,
                unk3 ? 0x15 : 0x3,
                0x4,
                unk4 ? 0x13 : 0x5,
                0x6,
                0x7,
                Game.Secrets[0x22] ? 0x19 : 0x8,
            };
            var table2 = new int[]
            {
                0x22,
                0x25,
                0x1D,
                0x1A,
                seed.NextInt(0x2) == 0 ? 0x1e : 0x23,
                0x1e,
                0x1f,
                Game.Secrets[0x22] ? 0x19 : 0x8,
            };

            if (stage == 9) {
                return 0x3F;
            }

            var idx = stage - 1;
            if (stage == 1 || stage == 3) {
                Game.SetFlag(GameStateFlags.STATE_BOSSPOOL_SWITCHED, seed.NextInt(0x2) == 1);
                if (Game.GetFlag(GameStateFlags.STATE_BOSSPOOL_SWITCHED))
                    idx = stage;
            }
            if (stage == 2 || stage == 4) {
                if (Game.GetFlag(GameStateFlags.STATE_BOSSPOOL_SWITCHED))
                    idx--;
            }

            return Game.StageVariant == 1 ? table2[idx] : table1[idx];
        }

        public uint ChooseDoubleTrouble(int stage) {
            var seed = Game.StageSeed;
            if ((stage == 3 || stage == 4) && seed.NextInt(0x32) == 0) {
                return 0xe74;
            }
            if (stage == 5 && seed.NextInt(0x19) == 0) {
                return 0xea6;
            }
            if (stage == 7 && seed.NextInt(0x28) == 0) {
                return 0xed8;
            }
            return 0;
        }

        bool PlaceRooms(StageLayout layout, uint minDiff, uint maxDiff) {
            Room deadEnd;

            var bossRoom = ChooseBossRoomSubtype(Game.Stage);
            var dt = ChooseDoubleTrouble(Game.Stage);

            var roomSeed = new Rng(Game.StageSeed.Next(), 0x5, 0xf, 0x11);

            RoomDescriptor boss = TryGetBossRoom(roomSeed, bossRoom, dt);
            if (boss == null) {
                return false;
            }

            // First boss room
            var bossDeadend = PopBossDeadEnd(layout, boss.RoomShape, boss.Doors);
            if (bossDeadend == null)
                return false;
            bossDeadend.ApplyDescriptor(boss, Game.StageSeed); //In isaac this is place_room. Kept layout rooms and game rooms together for simplicity

            // Second boss room
            if (State.IsXL) {
                var origSeed = Game.StageSeed.Clone();
                var secBossRoom = ChooseBossRoomSubtype(Game.Stage + 1);
                var secDt = ChooseDoubleTrouble(Game.Stage + 1);
                var secBoss = TryGetBossRoom(Game.StageSeed, secBossRoom, secDt);
                if (secBoss == null)
                    return false;
                var secBossDeadEnd = LayoutGen.TryAddBossDeadend(layout, bossDeadend, true);
                if (secBossDeadEnd == null)
                    return false; //Couldn't place the second boss room
                if (!TryResizeRoom(layout, secBossDeadEnd, secBoss.RoomShape, secBoss.Doors))
                    return false;

                secBossDeadEnd.ApplyDescriptor(secBoss, Game.StageSeed);
                Game.StageSeed = origSeed;
            }

            // Super Secret
            {
                var superSecret = Provider.GetRandomRoom(Game.StageSeed.Next(), true, 0, RoomType.ROOM_SUPERSECRET, RoomShape.NUM_ROOMSHAPES, 0, uint.MaxValue, 1, 10, 0, -1);
                if ((deadEnd = PopDeadEnd(layout, superSecret.RoomShape, superSecret.Doors)) == null)
                    return false;
                deadEnd.ApplyDescriptor(superSecret, Game.StageSeed);
                deadEnd.Invisible = true; //Todo: Check that this actually gets set
            }

            var secretSeed = new Rng(Game.StageSeed.Next(), 0x1, 0x5, 0x10);

            // Shop
            if (Game.Stage < 7 || (Game.Stage < 9 && Game.Trinkets[0x6e])) {
                var shopSubtype = 4; //Based on unlocks
                var num = (Game.StageSeed.Next() & 0xFF);
                if (num == 0)
                    shopSubtype = 0xB;
                else if (num == 1)
                    shopSubtype = 0xA;

                var shop = Provider.GetRandomRoom(Game.StageSeed.Next(), true, 0, RoomType.ROOM_SHOP, RoomShape.NUM_ROOMSHAPES, 0, uint.MaxValue, 1, 10, 0, shopSubtype);
                if ((deadEnd = PopDeadEnd(layout, shop.RoomShape, shop.Doors)) == null)
                    return false;
                deadEnd.ApplyDescriptor(shop, Game.StageSeed);
            }

            // Treasure
            if (Game.Stage < 7 || (Game.Stage < 9 && Game.Trinkets[0x6f])) {
                var treasureCount = State.IsXL ? 2 : 1;
                var seed = Game.StageSeed;
                for (var i = 0; i < treasureCount; i++) {
                    int trSubType;
                    var hsChance = Game.ActiveItem == 0x1b7 ? 0x32 : 0x64;
                    //Golden Horseshoe
                    if (seed.NextInt(100) == 0 || (seed.NextInt(hsChance) < 15 && Game.Trinkets[0x52])) {
                        //Pay To Win
                        trSubType = Game.Trinkets[0x70] ? 3 : 1;

                    } else {
                        //Pay To Win
                        trSubType = Game.Trinkets[0x70] ? 2 : 0;
                    }

                    var treasure = Provider.GetRandomRoom(seed.Next(), true, 0, RoomType.ROOM_TREASURE, RoomShape.NUM_ROOMSHAPES, 0, uint.MaxValue, 1, 10, 0, trSubType);
                    if ((deadEnd = PopDeadEnd(layout, treasure.RoomShape, treasure.Doors)) == null)
                        return false;
                    deadEnd.ApplyDescriptor(treasure, seed);
                    seed = seed.Clone();
                }
            }

            // Dice/Sacrifice
            if (Game.Stage < 0xB) {
                {
                    var roomType = Game.StageSeed.NextInt(0x32) == 0 || (Game.StageSeed.NextInt(0x5) == 0 && Game.Keys > 1)
                        ? RoomType.ROOM_DICE
                        : RoomType.ROOM_SACRIFICE;
                    var room = Provider.GetRandomRoom(Game.StageSeed.Next(), true, 0, roomType, RoomShape.NUM_ROOMSHAPES, 0, uint.MaxValue, 1, 0xa, 0, -1);
                    //Bug? Resizes the dead end before determining if the room will be placed
                    deadEnd = PopDeadEnd(layout, room.RoomShape, room.Doors);
                    if (Game.StageSeed.NextInt(0x7) == 0 || ((Game.StageSeed.Next() & 3) == 0 && Game.Hearts + Game.SoulHearts >= Game.MaxHearts)) {
                        deadEnd?.ApplyDescriptor(room, Game.StageSeed);
                    } else {
                        layout.ReaddDeadEnd(deadEnd); //Dead end gets readded at the end
                    }
                }

                // Library
                {
                    var libSubtype = 4; //Based on unlocks
                    var room = Provider.GetRandomRoom(Game.StageSeed.Next(), false, 0, RoomType.ROOM_LIBRARY, RoomShape.NUM_ROOMSHAPES, 0, uint.MaxValue, 1, 0xa, 0, Game.StageSeed.NextInt(libSubtype + 1));
                    deadEnd = PopDeadEnd(layout, room.RoomShape, room.Doors);
                    if (Game.StageSeed.NextInt(0x14) == 0 || ((Game.StageSeed.Next() & 3) == 0 && Game.GetFlag(GameStateFlags.STATE_BOOK_PICKED_UP))) {
                        deadEnd?.ApplyDescriptor(room, Game.StageSeed);
                    } else {
                        layout.ReaddDeadEnd(deadEnd); //Dead end gets readded at the end
                    }
                }

                // Curse
                {
                    var room = Provider.GetRandomRoom(Game.StageSeed.Next(), true, 0, RoomType.ROOM_CURSE, RoomShape.NUM_ROOMSHAPES, 0, uint.MaxValue, 1, 0xa, 0, -1);
                    deadEnd = PopDeadEnd(layout, room.RoomShape, room.Doors);
                    if ((Game.StageSeed.Next() & 1) == 0 || ((Game.StageSeed.Next() & 3) == 0 && Game.GetFlag(GameStateFlags.STATE_DEVILROOM_VISITED))) {
                        deadEnd?.ApplyDescriptor(room, Game.StageSeed);
                    } else {
                        layout.ReaddDeadEnd(deadEnd); //Dead end gets readded at the end
                    }
                }

                // Mini Boss
                {
                    uint variant = uint.MaxValue;
                    GameStateFlags flags = 0;
                    if (Game.Stage > 2 && Game.StageSeed.NextInt(0xA) == 0 && !Game.GetFlag(GameStateFlags.STATE_ULTRAPRIDE_SPAWNED)) {
                        Game.StageSeed.Next(); //Unused
                        variant = 0x8D4;
                        flags = GameStateFlags.STATE_ULTRAPRIDE_SPAWNED;
                    } else {
                        var isAlt = Game.StageSeed.NextInt(0x5) == 0; //Based on unlocks
                        var miniBosses = MiniBosses.Where(t => !Game.GetFlag(t.Item1)).ToArray();
                        var randMiniBossIdx = miniBosses.Length > 0 ? Game.StageSeed.NextInt(miniBosses.Length) : -1;
                        if (randMiniBossIdx != -1) {
                            var randMiniboss = miniBosses[randMiniBossIdx];
                            variant = isAlt ? randMiniboss.Item3 : randMiniboss.Item2;
                            flags = randMiniboss.Item1;
                        }
                    }
                    if (variant != uint.MaxValue) {
                        var room = Provider.GetRandomRoom(Game.StageSeed.Next(), true, 0, RoomType.ROOM_MINIBOSS, RoomShape.NUM_ROOMSHAPES, variant, variant + 0x9, 1, 0xa, 0, -1);
                        deadEnd = room != null ? PopDeadEnd(layout, room.RoomShape, room.Doors) : null;
                        if (((Game.StageSeed.Next() & 3) == 0 || (Game.StageSeed.NextInt(3) == 0 && Game.Stage == 1)) && deadEnd != null) {
                            deadEnd?.ApplyDescriptor(room, Game.StageSeed);
                            Game.SetFlag(flags, true);
                        } else {
                            layout.ReaddDeadEnd(deadEnd); //Dead end gets readded at the end
                        }
                    } else {
                        //Bug: The dead end placed before MiniBoss gets placed again. TBD Does this have any side effects?
                    }
                }

                // Challenge
                {
                    var room = Provider.GetRandomRoom(Game.StageSeed.Next(), true, 0, RoomType.ROOM_CHALLENGE, RoomShape.NUM_ROOMSHAPES, 0, uint.MaxValue, 1, 0xa, 0, -1);
                    deadEnd = PopDeadEnd(layout, room.RoomShape, room.Doors);
                    if (((Game.StageSeed.Next() & 1) == 0 || Game.Stage > 2) && Game.Hearts + Game.SoulHearts >= Game.MaxHearts && Game.Stage > 1) {
                        deadEnd?.ApplyDescriptor(room, Game.StageSeed);
                    } else {
                        layout.ReaddDeadEnd(deadEnd); //Dead end gets readded at the end
                    }
                }

                // Chest and Arcade

                {
                    var roomType = Game.StageSeed.NextInt(0xA) == 0 || (Game.StageSeed.NextInt(0x3) == 0 && Game.Keys > 1)
                        ? RoomType.ROOM_CHEST
                        : RoomType.ROOM_ARCADE;
                    var room = Provider.GetRandomRoom(Game.StageSeed.Next(), true, 0, roomType, RoomShape.NUM_ROOMSHAPES, 0, uint.MaxValue, 1, 0xa, 0, -1);
                    deadEnd = PopDeadEnd(layout, room.RoomShape, room.Doors);
                    if (Game.Coins >= 5 && Game.Stage % 2 == 0 && Game.Stage < 9) {
                        deadEnd?.ApplyDescriptor(room, Game.StageSeed);
                    } else {
                        layout.ReaddDeadEnd(deadEnd); //Dead end gets readded at the end
                    }
                }
            }

            // Isaacs/Barren
            if (Game.Stage < 7) {
                var roomType = (Game.StageSeed.Next() & 1) == 0
                    ? RoomType.ROOM_ISAACS
                    : RoomType.ROOM_BARREN;
                //From the wiki. Didn't double check
                var maxHearts = 0;
                if (Game.Character == PlayerType.PLAYER_THELOST || Game.Character == PlayerType.PLAYER_XXX || Game.Character == PlayerType.PLAYER_THESOUL) {
                    maxHearts = Game.MaxHearts;
                } else {
                    maxHearts = Game.MaxHearts + Game.BoneHearts * 2;
                }
                var room = Provider.GetRandomRoom(Game.StageSeed.Next(), true, 0, roomType, RoomShape.NUM_ROOMSHAPES, 0, uint.MaxValue, 1, 0xa, 0, -1);
                deadEnd = PopDeadEnd(layout, room.RoomShape, room.Doors);
                if (Game.StageSeed.NextInt(0x32) == 0 || (Game.StageSeed.NextInt(0x5) == 0 && ((Game.Hearts < 2 && Game.SoulHearts == 0) || (maxHearts == 0 && Game.SoulHearts <= 2)))) {
                    deadEnd?.ApplyDescriptor(room, Game.StageSeed);
                } else {
                    layout.ReaddDeadEnd(deadEnd); //Dead end gets readded at the end
                }
            }

            // Secret
            {
                var secretCount = Game.Trinkets[0x66] ? 2 : 1;
                for (var i = 0; i < secretCount; i++) {
                    var room = Provider.GetRandomRoom(secretSeed.Seed, true, 0, RoomType.ROOM_SECRET, RoomShape.NUM_ROOMSHAPES, 0, uint.MaxValue, 1, 10, 0, -1);
                    var blacklist = CalculateSecretRoomBlacklist(layout);
                    var secret = CreateSecretRoom(layout, blacklist);
                    if (secret != null) {
                        secret.ApplyDescriptor(room, new Rng(secretSeed.Seed, Game.StageSeed.Shift1, Game.StageSeed.Shift2, Game.StageSeed.Shift3));
                    }
                    secretSeed.Next();
                }
            }

            //Grave
            if (Game.Stage == 0xB && Game.StageVariant == 0) {
                var room = Provider.GetRandomRoom(Game.StageSeed.Next(), true, 0, RoomType.ROOM_DEFAULT, RoomShape.NUM_ROOMSHAPES, 3, 9, 1, 0xa, 0, -1);
                deadEnd = PopDeadEnd(layout, room.RoomShape, room.Doors);
                if (deadEnd == null)
                    return false;
                deadEnd?.ApplyDescriptor(room, Game.StageSeed);
            }

            var stageId = Game.StageId;
            var normalRooms = layout.Rooms.Where(r => !r.IsDeadEnd).Concat(layout.DeadEnds);
            foreach (var room in normalRooms) {
                if (room.RoomType != RoomType.ROOM_NULL) //Ignore placed rooms
                    continue;

                RoomDescriptor rd;
                if (room.RoomX == 6 && room.RoomY == 6 && room.Distance == 0) {
                    if (Game.Stage == 0xB && Game.StageVariant == 0) {
                        rd = Provider.StageRooms[0x10].First(e => e.RoomId == 0);
                    } else if (Game.Stage == 0xB && Game.StageVariant == 1) {
                        rd = Provider.StageRooms[0x11].First(e => e.RoomId == 0);
                    } else {
                        rd = Provider.StageRooms[0].First(e => e.RoomId == 2);
                    }
                } else {
                    var doors = layout.CalculateDoorBits(room);
                    var minVar = Game.Stage == 0xB ? 1u : 0; //Don't include the start rooms
                    rd = Provider.GetRandomRoom(Game.StageSeed.Next(), true, stageId, RoomType.ROOM_DEFAULT, room.Shape, minVar, uint.MaxValue, minDiff, maxDiff, doors, -1);
                }

                if (rd == null) {
                    //Console.WriteLine("Couldn't find required room");
                    return false;
                }

                room.ApplyDescriptor(rd, Game.StageSeed);

            }


            return true;
        }

        Room CreateSecretRoom(StageLayout layout, HashSet<Point> blacklist) {
            var maxWeight = 0;
            var cands = new List<Point>();
            for (var y = 0; y < 13; y++) {
                for (var x = 0; x < 13; x++) {
                    var curCoords = new Point(x, y);
                    if (layout.GetRoom(curCoords) != null) //Can't create the secret on top of another room
                        continue;
                    if (blacklist.Contains(curCoords))
                        continue;

                    var weight = 0xA + State.Seed.NextInt(0x5);

                    var offsets = Room.ShapeDoors[RoomShape.ROOMSHAPE_1x1];
                    var neighborCount = 0;
                    for (int dir = 0; dir < 4; dir++) {
                        var neighborCoords = curCoords.Add(offsets[dir].Value);
                        var neighbor = layout.GetRoom(neighborCoords);
                        if (neighbor == null)
                            continue;
                        var oppDir = (dir + 2) % 4;
                        //Make sure the neighbor shape has doors in this direction
                        if (Room.GetNeighborCoords(neighborCoords, neighbor.Shape, oppDir) == PointExt.Invalid) {
                            weight -= 0x64;
                            continue;
                        }
                        neighborCount++;
                    }
                    if (neighborCount == 0)
                        continue;
                    if (weight < 0)
                        continue;

                    if (neighborCount < 3)
                        weight -= 3;
                    if (neighborCount < 2)
                        weight -= 3;

                    if (weight >= maxWeight) {
                        if (weight != maxWeight)
                            cands.Clear();
                        maxWeight = weight;
                        cands.Add(curCoords);
                    }
                }
            }

            if (cands.Count == 0)
                return null;

            var picked = cands[State.Seed.NextInt(cands.Count)];
            var secret = new Room(RoomShape.ROOMSHAPE_1x1, picked, -1) {
                Invisible = true
            };
            return layout.PlaceRoom(secret);
        }

        //Secret rooms cannot be placed next to boss/secret/supersecret room when there is a door to it
        HashSet<Point> CalculateSecretRoomBlacklist(StageLayout layout) {
            //In isaac, Room::ApplyDescriptor would be level::place_room and that adds to a separate list that would be iterated here
            //Since we aren't doing that we instead filter on room type
            var blacklist = new HashSet<Point>();
            //Can't be next to the start room
            if (Game.Stage == 0xB) {
                blacklist.Add(new Point(7, 6));
                blacklist.Add(new Point(5, 6));
                blacklist.Add(new Point(6, 7));
                blacklist.Add(new Point(6, 5));
            }

            foreach (var r in layout.Rooms) {
                if (r.RoomType == RoomType.ROOM_NULL)
                    continue;
                var rd = r.Descriptor;
                //Yet another method for finding neighbors :unamused:
                var width = rd.Width / 13;
                var height = rd.Height / 7;

                var neighbors = new Point[] {
                    new Point(r.RoomX-1,     r.RoomY),
                    new Point(r.RoomX,       r.RoomY-1),
                    new Point(r.RoomX+width, r.RoomY),
                    new Point(r.RoomX,       r.RoomY+height),
                    new Point(r.RoomX-1,     r.RoomY+1),
                    new Point(r.RoomX+1,     r.RoomY-1),
                    new Point(r.RoomX+width, r.RoomY+1),
                    new Point(r.RoomX+1,     r.RoomY+height),
                };

                for (var door = 0; door < 8; door++) {
                    if (r.Shape == RoomShape.ROOMSHAPE_1x1 && door >= 4)
                        continue;
                    if (r.Shape == RoomShape.ROOMSHAPE_2x1 && (door == 4 || door == 6))
                        continue;
                    if (r.Shape == RoomShape.ROOMSHAPE_1x2 && (door == 5 || door == 7))
                        continue;

                    var hasDoorToNeighbor = (rd.Doors & (1 << door)) != 0;
                    if (hasDoorToNeighbor && r.RoomType != RoomType.ROOM_BOSS && r.RoomType != RoomType.ROOM_SECRET && r.RoomType != RoomType.ROOM_SUPERSECRET)
                        continue;
                    if (!StageLayout.InBounds(neighbors[door]))
                        continue;

                    blacklist.Add(neighbors[door]);
                }
            }
            return blacklist;
        }

        Room PopBossDeadEnd(StageLayout layout, RoomShape shape, int doors) {
            if (layout.DeadEnds.Count < 1)
                return null;

            var deadEnds = layout.DeadEnds;
            for (var i = 0; i < deadEnds.Count; i++) {
                var de = deadEnds[i];
                if (de.Distance <= 1) //Ignore rooms attached to the start
                    continue;

                if (State.IsXL) {
                    var coords = de.PlacedCoords;
                    var valid = true;
                    for (var j = 0; j < 3; j++) {
                        coords = Room.GetNeighborCellCoords(coords, de.DirectionFromParent);
                        if (!StageLayout.InBounds(coords)) {
                            valid = false;
                            break;
                        }
                        var room = layout.GetRoom(coords);
                        if (room != null && room != de) {
                            valid = false;
                            break;
                        }
                        if (layout.GetNeighborCount(coords, de) > 0){
                            valid = false;
                            break;
                        }
                    }
                    if (!valid)
                        continue;
                }

                if (TryResizeRoom(layout, de, shape, doors)) {
                    deadEnds.Remove(de);
                    return de;
                }
            }

            return null;
        }
        Room PopDeadEnd(StageLayout layout, RoomShape shape, int doors) {
            if (layout.DeadEnds.Count < 1)
                return null;

            var deadEnds = layout.DeadEnds;
            for (var i = 0; i < deadEnds.Count; i++) {
                var de = deadEnds[i];

                if (TryResizeRoom(layout, de, shape, doors)) {
                    deadEnds.Remove(de);
                    return de;
                }
            }

            return null;
        }

        //Try to get a boss room 50 times
        RoomDescriptor TryGetBossRoom(Rng seed, int bossSubType, uint dtVariant) {
            RoomDescriptor boss = null;
            for (var i = 0; i < 0x32 && boss == null; i++) {
                if (dtVariant != 0) {
                    //Double trouble boss
                    boss = Provider.GetRandomRoom(
                        seed.Next(),
                        true,
                        0,
                        RoomType.ROOM_BOSS,
                        RoomShape.NUM_ROOMSHAPES,
                        dtVariant,
                        dtVariant + 0x31,
                        0,
                        10,
                        0,
                        -1);
                } else {
                    //Regular boss
                    boss = Provider.GetRandomRoom(
                        seed.Next(),
                        true,
                        0,
                        RoomType.ROOM_BOSS,
                        RoomShape.NUM_ROOMSHAPES,
                        0,
                        uint.MaxValue,
                        1,
                        10,
                        0,
                        bossSubType);
                    if (boss != null && boss.RoomId >= 0xE74 && boss.RoomId <= 0xF09)
                        boss = null; //Can't be a double trouble room
                }
            }
            return boss;
        }

        bool TryResizeRoom(StageLayout layout, Room deadEnd, RoomShape newShape, int doors) {
            //Why doesn't this return early if the new sharp is the current shape? :thinking:
            var dirFrom = deadEnd.DirectionFromParent;
            var dirTo = (dirFrom + 2) % 4;

            //Bug: The game is reading out of bounds for the last door flag
            var doorFlags = Room.ResizeRoomDoorMask.Select(e => dirTo + (e * 4)).ToArray();
            //Add 1x1 to the direction arrays
            var shapes = (new int[] { (int)RoomShape.ROOMSHAPE_1x1 }).Concat(Room.DirectionToShape[dirFrom]).ToArray();
            var dirs = (new Point[] { Point.Empty }).Concat(Room.DirectionShapeOffsets[dirFrom]).ToArray();

            for (var i = 0; i < shapes.Length; i++) {
                var canPlace = true;
                var newCoords = deadEnd.PlacedCoords.Add(dirs[i]);

                if (i >= doorFlags.Length) {
                    //Console.WriteLine("Hit the bug");
                    return false;
                }

                if (shapes[i] != (int)newShape)
                    continue;

                if ((doors & (1 << doorFlags[i])) == 0)
                    continue;

                //Make sure the new shape fits in the new coords
                var points = Room.ShapePoints[newShape];
                for (var j = 0; j < points.Length; j++) {
                    var test = newCoords.Add(points[j]);
                    if (!StageLayout.InBounds(test)) {
                        canPlace = false;
                        break;
                    }

                    //Bug?: Shouldn't this check if the new shape overlaps megasatan?
                    var room = layout.RoomGrid[test.Y][test.X];
                    if (room != null && room != deadEnd)
                        canPlace = false;
                }

                //Make sure the new shape and new coords is still a dead end
                int visibleNeighbors = 0;
                for (var door = 0; door < 8; door++) {
                    var neighbor = layout.GetRoom(Room.GetNeighborCoords(newCoords, newShape, door));
                    if (neighbor == null || neighbor == deadEnd)
                        continue;

                    visibleNeighbors += neighbor.Invisible ? 0 : 1;

                    if ((doors & (1 << door)) != 0)
                        continue;

                    canPlace = false;
                }

                if (visibleNeighbors > 1 || !canPlace)
                    continue;

                layout.ReshapeRoom(deadEnd, newShape, newCoords);
                return true;
            }
            return false;
        }


        static bool CanStageHaveCurseOfLabyrinth(int stage) {
            return stage % 2 == 1 && stage < 8;
        }


        public static List<Tuple<GameStateFlags, uint, uint>> MiniBosses = new List<Tuple<GameStateFlags, uint, uint>>() {
            Tuple.Create(GameStateFlags.STATE_WRATH_SPAWNED,    0x7d0u, 0x834u),
            Tuple.Create(GameStateFlags.STATE_GLUTTONY_SPAWNED, 0x7dau, 0x83eu),
            Tuple.Create(GameStateFlags.STATE_LUST_SPAWNED,     0x7e4u, 0x848u),
            Tuple.Create(GameStateFlags.STATE_SLOTH_SPAWNED,    0x7eeu, 0x852u),
            Tuple.Create(GameStateFlags.STATE_ENVY_SPAWNED,     0x802u, 0x866u),
            Tuple.Create(GameStateFlags.STATE_PRIDE_SPAWNED,    0x80cu, 0x870u),

        };
    }

}
