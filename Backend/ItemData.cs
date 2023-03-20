using System;
using System.Collections.Generic;
using System.Drawing;

namespace MKDD_TAS_Tool
{
    public class BruteforceCondition
    {
        public uint driver_id;
        public uint pos;
        public uint reality;
        public uint roll;
        public uint item_id;

        public float likelyness;
    }

    public class CharData
    {
        // all character names sorted by order in roaster
        public static List<string> char_names = new List<string>()
        {
            "Mario",
            "Luigi",
            "Peach",
            "Daisy",
            "Yoshi",
            "Birdo",
            "Baby Mario",
            "Baby Luigi",
            "Toad",
            "Toadette",
            "Koopa",
            "Paratroopa",
            "DK",
            "Diddy",
            "Bowser",
            "Bowser JR",
            "Wario",
            "Waluigi"
        };
        // dict that assigns special item IDs to character name
        public static Dictionary<String, uint> specials_dict = new Dictionary<string, uint>()
        {
            // Fire
            { "Mario",          0x09 },
            { "Luigi",          0x15 },
            // Hearts
            { "Peach",          0x0E },
            { "Daisy",          0x0E },
            // Egg
            { "Yoshi",          0x0B },
            { "Birdo",          0x1B },
            // Chomp
            { "Baby Mario",     0x07 }, 
            { "Baby Luigi",     0x07 },
            // GoldShroom
            { "Toad",           0x0C }, 
            { "Toadette",       0x0C },
            // TripleGreen (both can get Reds aswell, but thats not implemented yet I think)
            { "Koopa",          0x11 }, 
            { "Paratroopa",     0x11 },
            // Big Banana
            { "DK",             0x04 },
            { "Diddy",          0x04 },
            // Bowser Shell
            { "Bowser",         0x01 },
            { "Bowser JR",      0x01 },
            // Bomb
            { "Wario",          0x08 },
            { "Waluigi",        0x08 },
        };
    }
    public class ItemData
    {
        public static ulong MAX_RNG_Combinations = 4294967295;

        public static Dictionary<String, Color> ItemColor_Dict = new Dictionary<String, Color>()
        {
            { "Bowser Shell", Color.FromArgb(255, 150, 200, 150) },
            // - - -
            { "Big Banana", Color.FromArgb(255, 220, 205, 200) },
            { "Mushroom", Color.FromArgb(255, 255, 230, 230) },
            // - - -
            { "Star", Color.FromArgb(255, 255, 255, 190) },
            { "Chomp", Color.FromArgb(255, 220, 220, 220) },
            { "Bomb", Color.FromArgb(255, 210, 190, 255) },
            { "Red Fire", Color.FromArgb(255, 255, 120, 100) },
            { "Shock", Color.FromArgb(255, 255, 220, 60) },
            { "Yoshi Egg", Color.FromArgb(255, 200, 230, 210) },
            { "Goldshroom", Color.FromArgb(255, 255, 235, 155) },
            { "Blue", Color.FromArgb(255, 180, 210, 255) },
            // - - -
            { "Hearts", Color.FromArgb(255, 255, 110, 225) },
            // - - -
            { "Triple Greens", Color.FromArgb(255, 210, 255, 200) },
            { "Triple Shrooms", Color.FromArgb(255, 255, 170, 170) },
            { "Triple Reds", Color.FromArgb(255, 250, 200, 235) },
            // - - - 
            { "Green Fire", Color.FromArgb(255, 140, 235, 120) },
            // - - - 
            { "Birdo Egg", Color.FromArgb(255, 215, 200, 225) },
            // - - - 
            { "Invalid Roll", Color.FromArgb(255, 255, 0, 0) },
        };

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
            { "Shock", 0x0A },
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
            "Shock",
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
        public static List<string> special_items = new List<string>()
        {
            "Bowser Shell", "BS", "BShell",
            "Big Banana", "Bignana", "BANANA",
            "Chain Chomp", "Chomp", "WanWan",
            "Bomb", "Bob Omb",
            "Red Fire", "Fire", "Green Fire",
            "Triple Greens", "Triple Reds",
            "Hearts",
            "Goldshroom", "GShroom",
            "Yoshi Egg", "Birdo Egg",
        };
        public static Dictionary<String, uint> realities = new Dictionary<string, uint>()
        {
            { "Default", 0x00 },
            { "No Special", 0x01 },
            { "No Blue", 0x02 },
            { "No Star", 0x03 },
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
            "Shock",
            "Fake Box",
            "Special"
        };

        public static uint item_name_to_ID(string item_name)
        {
            if (items_dict.ContainsKey(item_name) == true)
                return items_dict[item_name];

            else return 0;
        }
        public static uint item_name_to_rollable_ID(string item_name)
        {
            if (special_items.Contains(item_name))
                item_name = "Special";

            for (uint rollableItemID = 0; rollableItemID < 10; rollableItemID++)
                if (ItemData.rollable_items_names[rollableItemID] == item_name) return rollableItemID;

            return 0;
        }


        // all item weights sorted by ID (need to pre-init all of them like this because C# is ass ?)
        public static uint[] item_w00 = new uint[] { 100, 60, 45, 10, 0, 0, 0, 0 };         // Green    0x00
        public static uint[] item_w01 = new uint[] { 20, 50, 100, 100, 100, 100, 50, 0 };   // BowserS
        public static uint[] item_w02 = new uint[] { 0, 45, 60, 75, 70, 50, 40, 20 };       // Red
        public static uint[] item_w03 = new uint[] { 70, 35, 15, 5, 0, 0, 0, 0 };           // Banana
        public static uint[] item_w04 = new uint[] { 120, 100, 90, 60, 30, 0, 0, 0 };       // Bignana  0x04
        public static uint[] item_w05 = new uint[] { 0, 40, 60, 70, 60, 35, 10, 0 };        // Shroom
        public static uint[] item_w06 = new uint[] { 0, 0, 0, 10, 20, 30, 40, 40 };         // Star
        public static uint[] item_w07 = new uint[] { 0, 0, 1, 3, 20, 20, 130, 180 };        // Chomp
        public static uint[] item_w08 = new uint[] { 20, 60, 100, 100, 100, 100, 60, 0 };   // Bomb     0x08
        public static uint[] item_w09 = new uint[] { 0, 60, 100, 100, 100, 100, 60, 0 };    // R-Fire
        public static uint[] item_w0A = new uint[] { 0, 0, 0, 0, 0, 10, 20, 30 };           // Shock
        public static uint[] item_w0B = new uint[] { 40, 70, 80, 80, 80, 70, 60, 0 };       // YoshiEgg
        public static uint[] item_w0C = new uint[] { 0, 3, 10, 30, 50, 80, 100, 120 };      // Gold     0x0C
        public static uint[] item_w0D = new uint[] { 0, 0, 0, 10, 15, 20, 20, 20 };         // Blue
        public static uint[] item_w0E = new uint[] { 0, 1, 3, 10, 30, 90, 110, 130 };       // Hearts
        public static uint[] item_w0F = new uint[] { 30, 20, 10, 0, 0, 0, 0, 0 };           // FakeBox
        public static uint[] item_w10 = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };              // ---      0x10
        public static uint[] item_w11 = new uint[] { 20, 50, 100, 100, 100, 100, 50, 0 };   // TripGr
        public static uint[] item_w12 = new uint[] { 0, 0, 10, 20, 35, 55, 70, 90 };        // TripSh
        public static uint[] item_w13 = new uint[] { 20, 50, 100, 100, 100, 100, 50, 0 };   // TripRe   assumed equal to triple greens
        public static uint[] item_w14 = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };              // ---      0x14
        public static uint[] item_w15 = new uint[] { 0, 60, 100, 100, 100, 100, 60, 0 };    // G-Fire   assumed equal to red fire
        public static uint[] item_w16 = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };              // ---
        public static uint[] item_w17 = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };              // ---
        public static uint[] item_w18 = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };              // ---      0x18
        public static uint[] item_w19 = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };              // ---
        public static uint[] item_w1A = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };              // ---
        public static uint[] item_w1B = new uint[] { 40, 70, 80, 80, 80, 70, 60, 0 };       // BirdoEgg assumed equal to yoshi egg
        public static uint[] item_w1C = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };              // --- 0x1C
        public static uint[] item_w1D = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };              // ---
        public static uint[] item_w1E = new uint[] { 0, 0, 0, 0, 0, 0, 0, 0 };              // ---
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
