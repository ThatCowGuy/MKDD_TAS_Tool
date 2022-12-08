using System;
using System.Linq;

// all of this was reverse engineered by SwareJonge
namespace MKDD_TAS_Tool
{
    public class CodeRandomness
    {
        static public double shiftRNGcnvtoFloat(uint SEED)
        {
            uint nextSeed2 = (SEED >> 9); // shift the seed 9 bits to the right
            nextSeed2 |= 0x3f800000; // OR with 0x3f800000 to essentially convert it to a floating point number
            byte[] bytes = BitConverter.GetBytes(nextSeed2);
            double myFloat = BitConverter.ToSingle(bytes, 0);
            myFloat = myFloat - 1.0;
            return myFloat;
        }

        static uint convert_fp2unsigned(double d)
        {
            uint uVar1 = 0;
            if ((0.0 <= d) && (d < 4.294967296e9))
            {
                uVar1 = 0xffffffff;
                if (2.147483648e9 <= d)
                {
                    d = d - 2.147483648e9;
                }
                uVar1 = (uint)d;
                if (2.147483648E9 <= d)
                {
                    uVar1 = uVar1 + 0x80000000;
                }
            }
            return uVar1;
        }

        // advancing the RNG value like the game does
        static public uint AdvanceRNG(uint seed)
        {
            return (seed * 0x0019660D) + 0x3C6EF35F;
        }

        static public uint getRandomMax(uint seed, uint MAX)
        {
            seed = AdvanceRNG(seed);
            byte[] arr = new byte[8];
            arr[0] = 0x43;
            arr[1] = 0x30;
            arr[2] = 0x00;
            arr[3] = 0x00; // 44.0f

            byte[] maxARR = BitConverter.GetBytes(MAX + 1);
            maxARR = maxARR.Reverse().ToArray();
            for (int i = 0; i < 4; i++)
            {
                arr[4 + i] = maxARR[i];
            }
            arr = arr.Reverse().ToArray();
            double max = BitConverter.ToDouble(arr, 0) - 4.503599627370496e15;
            return convert_fp2unsigned(max * shiftRNGcnvtoFloat(seed));
        }

        public static int calc_ItemRoll(uint Seed, uint[][] ItemProbMatrix, int pos, uint maxItemCnt) // maxItemCnt is annoying me still
        {
            // get the actual random number
            uint randomNum = getRandomMax(Seed, maxItemCnt - 1);
            // init the integrated weigth
            uint integrated_weight = 0;

            int rollableItemID;
            // iterate over the possible items
            for (rollableItemID = 0; rollableItemID < 10; rollableItemID++)
            {
                integrated_weight += ItemProbMatrix[rollableItemID][pos];
                // check if we stepped over the randomNum threshold
                if (randomNum < integrated_weight) return rollableItemID;
            }
            return -1;
        }
    }
}
