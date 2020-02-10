using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isaac_levelgen
{
    public class Rng
    {
        public uint Seed;
        public int Shift1;
        public int Shift2;
        public int Shift3;
        public uint Next()
        {
            var num = Seed;
            num ^= num >> Shift1;
            num ^= num << Shift2;
            num ^= num >> Shift3;
            Seed = num;
            return num;
        }

        public int NextInt()
        {
            return (int)Next();
        }

        public int NextInt(int max)
        {
            return (int)(Next() % max);
        }

        public unsafe float NextFloat()
        {
            uint multi = 0x2F7FFFFE;
            return Next() * (*(float*)&multi);
        }

        public Rng(uint seed, int s1, int s2, int s3)
        {
            this.Seed = seed;
            Shift1 = s1;
            Shift2 = s2;
            Shift3 = s3;
        }

        public Rng Clone()
        {
            return new Rng(this.Seed, this.Shift1, this.Shift2, this.Shift3);
        }
        public void Shuffle<T>(List<T> list) {
            if (list.Count < 2)
                return;

            for (var i = list.Count - 1; i > 0; i--) {
                var randIdx = (int)(this.Next() % (i + 1));

                var t = list[randIdx];
                list[randIdx] = list[i];
                list[i] = t;
            }
        }

        public static string SeedToString(uint num) {
            const string chars = "ABCDEFGHJKLMNPQRSTWXYZ01234V6789";
            byte x = 0;
            var tnum = num;
            while (tnum != 0) {
                x += ((byte)tnum);
                x += (byte)(x + (x >> 7));
                tnum >>= 5;
            }
            num ^= 0x0FEF7FFD;
            tnum = (num) << 8 | x;

            var ret = new char[8];
            for (int i = 0; i < 6; i++) {
                ret[i] = chars[(int)(num >> (27 - (i * 5)) & 0x1F)];
            }
            ret[6] = chars[(int)(tnum >> 5 & 0x1F)];
            ret[7] = chars[(int)(tnum & 0x1F)];

            return new string(ret);
        }

        public override string ToString() {
            return $"Rng(0x{Seed:X8}, {Shift1}, {Shift2}, {Shift3})";
        }
    };
}
