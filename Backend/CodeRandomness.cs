using System;
using System.Linq;

// all of this was reverse engineered by SwareJonge
namespace MKDD_TAS_Tool
{
    public class CodeRandomness
    {
        // translates the RNG value into a Float within [0 : 1]
        public static double translate_RNG_to_Float(uint RNG)
        {
            // shift the RNG 9 bits to the right
            uint transformed_RNG = (RNG >> 9);
            // OR with 0x3f800000 to essentially convert it to a floating point number
            transformed_RNG |= 0x3f800000;

            // get a reversed byte-representation of the value, and transform into float 
            byte[] bytes = BitConverter.GetBytes(transformed_RNG);
            double float_representation = BitConverter.ToSingle(bytes, 0);

            return float_representation - 1.0;
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
        static uint convert_fp2unsigned_specialized(double d)
        {
            if (d > 1000 || d < 0) Console.WriteLine(d);
            // since MAX is never > 1000, the resulting 8-byte array will never start with a 1-bit
            // therefore, d will never be negative. Furthermore, MAX wont reach into the exponent
            // bits of the double, leaving it to be == 0 always, which also means d will never
            // exceed 2.147483648e9; So all the if clauses here are meaningless IN OUR USECASE(!)
            //
            // => Replace entire func by a simple cast...

            if ((0.0 <= d) && (d < 4.294967296e9))
            {
                if (d >= 2.147483648e9)
                {
                    d = d - 2.147483648e9;
                }

                if (d >= 2.147483648e9)
                {
                    return ((uint)d) + 0x80000000;
                }

                return (uint)d;
            }
            return 0;
        }

        // advancing the RNG value like the game does
        public static uint AdvanceRNG(uint RNG)
        {
            return (RNG * 0x0019660D) + 0x3C6EF35F;
        }

        //                                      First 4B will change    Latter 4B = 176.0f in reverse
        public static byte[] RandomMaxArray = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x30, 0x43 };

        public static uint getRandomMax(uint RNG, uint MAX)
        {
            // overwrite the first 4B of RandomMaxArray with the new MAX+1 value
            byte[] maxARR = BitConverter.GetBytes(MAX + 1);
            RandomMaxArray[0] = maxARR[0];
            RandomMaxArray[1] = maxARR[1];
            RandomMaxArray[2] = maxARR[2];
            RandomMaxArray[3] = maxARR[3];

            // and create a double from the 8B array
            double max = BitConverter.ToDouble(RandomMaxArray, 0) - 4.503599627370496e15;

            Console.WriteLine(max);
            Console.WriteLine(MAX);

            RNG = AdvanceRNG(RNG);
            //return convert_fp2unsigned(max * translate_RNG_to_Float(RNG));
            return (uint)(max * translate_RNG_to_Float(RNG));
        }

        public static int calc_ItemRoll(uint RNG, uint[,] ItemProbMatrix, int pos, uint WeightTotal) // WeightTotal is annoying me still
        {
            // this basically has no effect
            // uint randomNum = getRandomMax(RNG, WeightTotal - 1);

            // Get a random Integer within [0 : WeightTotal]
            RNG = AdvanceRNG(RNG);
            uint randomNum = (uint)(translate_RNG_to_Float(RNG) * WeightTotal);

            // init the integrated weigth
            uint integrated_weight = 0;

            // iterate over the possible items
            for (int rollableItemID = 0; rollableItemID < 10; rollableItemID++)
            {
                integrated_weight += ItemProbMatrix[rollableItemID, pos];
                // check if we stepped over the randomNum threshold
                if (randomNum < integrated_weight) return rollableItemID;
            }
            return -1;
        }
    }
}
