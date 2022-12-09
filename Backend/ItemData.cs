using System;
using System.Collections.Generic;

namespace MKDD_TAS_Tool
{
    public class BruteforceCondition
    {
        public uint pos;
        public uint roll;
        public uint item_id;
    }

    public class CharData
    {
        // all small character names sorted by order in roaster
        public static String[] char_names = new string[]
        {
            "Baby Mario",
            "Baby Luigi",
            "Toad",
            "Toadette",
            "Koopa",
            "Paratroopa",
            "Diddy Kong",
            "Bowser Jr."
        };
        // dict that assigns special item IDs to character name
        public static Dictionary<String, uint> specials_dict = new Dictionary<string, uint>()
        {
            // Chomp
            { "Baby Mario", 0x07 }, 
            { "Baby Luigi", 0x07 },
            // G-Shroom
            { "Toad", 0x0C }, 
            { "Toadette", 0x0C },
            // T-Green
            { "Koopa", 0x11 }, 
            // T-Red
            { "Paratroopa", 0x13 },
            // Big Banana
            { "Diddy Kong", 0x04 },
            // Bowser Shell
            { "Bowser Jr.", 0x01 },
        };
    }
    public class ItemData
    {
        public static uint MAX_RNG_Combinations = 4294967295;

        // all gettable item names sorted by ID
        public static String[] gettable_item_names = new string[]
        {
            "Bowser Shell",
            "Mushroom",
            "Star",
            "Chomp",
            "Goldshroom",
            "Blue",
            "Triple Shrooms",
        };
        public static Dictionary<String, uint> items_dict = new Dictionary<string, uint>()
        {
            { "Green Shell", 0x00 },
            { "Bowser Shell", 0x01 },
            { "Red Shell", 0x02 },
            { "Banana", 0x03 },
            { "Big Banana", 0x04 },
            { "Mushroom", 0x05 },
            { "Star", 0x06 },
            { "Chomp", 0x07 },
            { "Bomb", 0x08 },
            { "Red Fire", 0x09 },
            { "Lightning", 0x0A },
            { "Yoshi Egg", 0x0B },
            { "Goldshroom", 0x0C },
            { "Blue", 0x0D },
            { "Hearts", 0x0E },
            { "Fake Box", 0x0F },
            // - - - - -
            { "Triple Greens", 0x11 },
            { "Triple Shrooms", 0x12 },
            { "Triple Reds", 0x13 },
            // - - - - -
            { "Green Fire", 0x15 },
            // - - - - -
            // - - - - -
            // - - - - -
            // - - - - -
            // - - - - -
            { "Birdo Egg", 0x1B },
            // - - - - -
            // - - - - -
            // - - - - -
        };

        // as sorted ingame in the RNG get func... hopefully
        public static String[] rollable_items_names = new string[]
        {
            "Green Shell",
            "Red Shell",
            "Blue",
            "Banana",
            "Mushroom",
            "Triple Shrooms",
            "Star",
            "Lightning",
            "Fake Box",
            "Special"
        };

        public static uint item_name_to_ID(string item_name)
        {
            return items_dict[item_name];
        }

        // all item names sorted by ID
        public static String[] item_names = new string[]
        {
            "Green Shell",      // 0x00
            "Bowser Shell",
            "Red Shell",
            "Banana",
            "Big Banana",       // 0x04
            "Mushroom",
            "Star",
            "Chomp",
            "Bomb",             // 0x08
            "Red Fire",
            "Lightning",
            "Yoshi Egg",
            "Goldshroom",       // 0x0C
            "Blue",
            "Hearts",
            "Fake Box",
            "- - -",            // 0x10
            "Triple Greens",
            "Triple Shrooms",
            "Triple Reds",
            "- - - ",           // 0x14
            "Green Fire",
            "- - -",
            "Double Greens",
            "Double Shrooms",   // 0x18
            "Double Reds",
            "Also Red Fire ?",
            "Birdo Egg",
            "Single Greens",    // 0x1C
            "Single Shrooms",
            "Single Reds"
        };

        // all item weights sorted by ID (need to pre-init all of them like this because C# is ass ?)
        public static uint[] item_w00 = new uint[] { 100, 60, 45, 10, 0, 0, 0, 0 };        // 0x00
        public static uint[] item_w01 = new uint[] { 20, 50, 100, 100, 100, 100, 50, 0 };
        public static uint[] item_w02 = new uint[] { 0, 45, 60, 75, 70, 50, 40, 20 };
        public static uint[] item_w03 = new uint[] { 70, 35, 15, 5, 0, 0, 0, 0 };
        public static uint[] item_w04 = new uint[] { 120, 100, 90, 60, 30, 0, 0, 0 };      // 0x04
        public static uint[] item_w05 = new uint[] { 0, 40, 60, 70, 60, 35, 10, 0 };
        public static uint[] item_w06 = new uint[] { 0, 0, 0, 10, 20, 30, 40, 40 };
        public static uint[] item_w07 = new uint[] { 0, 0, 1, 3, 20, 20, 130, 180 };
        public static uint[] item_w08 = new uint[] { 20, 60, 100, 100, 100, 100, 60, 0 };  // 0x08
        public static uint[] item_w09 = new uint[] { 0, 60, 100, 100, 100, 100, 60, 0 };
        public static uint[] item_w0A = new uint[] { 0, 0, 0, 0, 0, 10, 20, 30 };
        public static uint[] item_w0B = new uint[] { 40, 70, 80, 80, 80, 70, 60, 0 };
        public static uint[] item_w0C = new uint[] { 0, 3, 10, 30, 50, 80, 100, 120 };     // 0x0C
        public static uint[] item_w0D = new uint[] { 0, 0, 0, 10, 15, 20, 20, 20 };
        public static uint[] item_w0E = new uint[] { 0, 1, 3, 10, 30, 90, 110, 130 };
        public static uint[] item_w0F = new uint[] { 30, 20, 10, 0, 0, 0, 0, 0 };
        public static uint[] item_w10 = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };             // 0x10
        public static uint[] item_w11 = new uint[] { 20, 50, 100, 100, 100, 100, 50, 0 };
        public static uint[] item_w12 = new uint[] { 0, 0, 10, 20, 35, 55, 70, 90 };
        public static uint[] item_w13 = new uint[] { 20, 50, 100, 100, 100, 100, 50, 0 };  // assumed equal to triple greens
        public static uint[] item_w14 = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };             // 0x14
        public static uint[] item_w15 = new uint[] { 0, 60, 100, 100, 100, 100, 60, 0 };   // assumed equal to red fire
        public static uint[] item_w16 = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        public static uint[] item_w17 = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        public static uint[] item_w18 = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };             // 0x18
        public static uint[] item_w19 = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        public static uint[] item_w1A = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        public static uint[] item_w1B = new uint[] { 40, 70, 80, 80, 80, 70, 60, 0 };      // assumed equal to yoshi egg
        public static uint[] item_w1C = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };             // 0x1C
        public static uint[] item_w1D = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        public static uint[] item_w1E = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        // and then we can combine them into a PROPER 2Darray...
        public static uint[][] item_weights = new uint[][]
        {
            item_w00, item_w01, item_w02, item_w03, item_w04, item_w05, item_w06, item_w07,
            item_w08, item_w09, item_w0A, item_w0B, item_w0C, item_w0D, item_w0E, item_w0F,
            item_w10, item_w11, item_w12, item_w13, item_w14, item_w15, item_w16, item_w17,
            item_w18, item_w19, item_w1A, item_w1B, item_w1C, item_w1D, item_w1E
        };
        // and create an empty dud
        public static uint[] item_wNONE = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };
    }
}
