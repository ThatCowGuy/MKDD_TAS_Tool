using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Xml;

namespace MKDD_TAS_Tool
{
    public class Bruteforcer
    {


        public void insert_row_in_matrix(uint[,] matrix, uint index, uint[] row)
        {
            for (int col = 0; col < 8; col++)
                matrix[index, col] = row[col];
        }

        // threadding
        public Thread workerThread = null;
        public delegate void UpdateStatusDelegate();
        public delegate void FinishStatusDelegate();
        public UpdateStatusDelegate updateStatusDelegate = null;
        public FinishStatusDelegate finishStatusDelegate = null;
        public int active = 0;

        public String UpdateStatusMSG;

        public int max_roll = 0;
        public List<BruteforceCondition> Conditions;

        // return empty string if everything is okay; else return the parsing error
        public string read_pattern_file(string filename)
        {
            // create a new, empty list of bruteforcer conditions
            this.Conditions = new List<BruteforceCondition>();
            uint xml_driver_id = 0; // 0 = dont care, 1 = 1, 2 = 2
            uint xml_pos = 0;
            uint xml_reality = 0;
            uint xml_roll = 0;
            string xml_item_name = "";

            // open the pattern XML and readout the conditions
            XmlReader PatternFile = XmlReader.Create(filename);

            // reset the simulated realities
            for (int i = 0; i < 6; i++)
                this.simulated_realities[i] = 0;

            while (PatternFile.Read())
            {
                switch (PatternFile.Name.ToString())
                {
                    case "Driver":
                        xml_driver_id = UInt32.Parse(PatternFile.ReadString());
                        break;
                    case "Pos":
                        xml_pos = UInt32.Parse(PatternFile.ReadString());
                        break;
                    case "Reality":
                        xml_reality = UInt32.Parse(PatternFile.ReadString());
                        break;
                    case "Roll":
                        xml_roll = UInt32.Parse(PatternFile.ReadString());
                        break;
                    case "Item":
                        xml_item_name = PatternFile.ReadString();
                        break;
                    case "Condition":
                        if (PatternFile.NodeType == XmlNodeType.EndElement)
                        {
                            // create a new Condition instance from collected Data
                            BruteforceCondition cond = new BruteforceCondition();
                            cond.driver_id = xml_driver_id;
                            cond.pos = xml_pos;
                            cond.reality = xml_reality;
                            cond.roll = xml_roll;
                            // NOTE - converting item ID to rollable ID HERE to make bruteforcing faster
                            cond.item_id = ItemData.item_name_to_rollable_ID(xml_item_name);

                            // calculated manually for now, but could easily be done in a func
                            cond.likelyness = 1.0000f;
                            if (xml_item_name == "Chomp")
                            {
                                if (xml_pos == 3) cond.likelyness = 0.0050f;
                                if (xml_pos == 4) cond.likelyness = 0.0148f;
                            }
                            if (xml_item_name == "Blue")
                            {
                                if (xml_pos == 4) cond.likelyness = 0.0493f;
                                if (xml_pos == 5) cond.likelyness = 0.0682f;
                                if (xml_pos == 6) cond.likelyness = 0.0909f;
                            }
                            if (xml_item_name == "Star")
                            {
                                if (xml_pos == 4) cond.likelyness = 0.0493f;
                            }
                            if (xml_item_name == "Triple Shrooms")
                            {
                                if (xml_pos == 4) cond.likelyness = 0.0985f;
                            }

                            // and add it to the List IF its not 0 (Green Shell, but we'd never bruteforce for that)
                            if (cond.item_id > 0)
                            {
                                this.Conditions.Add(cond);
                                // add this reality to the simulated ones
                                this.simulated_realities[cond.reality] = 1;
                            }
                            else
                            {
                                // return an error
                                return xml_item_name;
                            }
                        }
                        break;
                }
            }
            return "";
        }


        /*
        public void HeavyOperation()
        {
            if (this.active == 1)
            {
                this.UpdateStatusMSG = "Interrupted.";
                this.Invoke(this.updateStatusDelegate);
                this.Invoke(this.finishStatusDelegate);
                return;
            }
            this.active = 1;

            this.UpdateStatusMSG = "Starting...";

            // Set up some vals
            int Driver_CNT = 2;
            int Considered_Positions = 5;
            int Considered_Realities = 6;
            int HistoryDepth = 10;

            int[,,,] history_matrix = new int[Driver_CNT, Considered_Positions, Considered_Realities, HistoryDepth];
            uint[] RNG_history = new uint[HistoryDepth];
            for (int step = 0; step < HistoryDepth; step++)
            {
                RNG_history[step] = this.RNG;
            }

            // create row string array
            int[] row_content_1 = new int[5];
            int[] row_content_2 = new int[5];
            ulong bruteforce_attempts = 0;

            // I can use this here to iterate through the History matrices, instead
            // of shifting the matrix contents on every sim. Should save some time.
            int Hist_IDX = (HistoryDepth - 1);

            long sim_start = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            while (true)
            {
                if (Hist_IDX < 0) Hist_IDX = (HistoryDepth - 1);

                foreach (BruteforceCondition cond in this.Conditions)
                {
                    int c_pos = (int)(cond.pos - 1);
                    // sim this pos+reality combo for both drivers
                    if (cond.driver_id == 0)
                    {
                        history_matrix[0, (cond.pos - 1), cond.reality, Hist_IDX] = get_Roll_ItemIDs(this.RNG, 1, c_pos, cond.reality);
                        history_matrix[1, (cond.pos - 1), cond.reality, Hist_IDX] = get_Roll_ItemIDs(this.RNG, 2, c_pos, cond.reality);
                    }
                    else // only sim this specific pos+reality+driver combo
                    {
                        history_matrix[(cond.driver_id - 1), c_pos, cond.reality, Hist_IDX] = get_Roll_ItemIDs(this.RNG, cond.driver_id, c_pos, cond.reality);
                    }
                }

                // populate LOWEST history matrix row with the new content
                RNG_history[Hist_IDX] = this.RNG;

                bool bruteforce_match = true;
                foreach (BruteforceCondition cond in this.Conditions)
                {
                    // check if we want a specific driver first
                    if (cond.driver_id > 0)
                    {
                        int actual_roll = history_matrix[(cond.driver_id - 1), (cond.pos - 1), cond.reality, ((Hist_IDX + (this.max_roll - cond.roll) + HistoryDepth) % HistoryDepth)];
                        if (actual_roll != cond.item_id)
                        {
                            bruteforce_match = false;
                            break;
                        }
                    }
                    // cond.driver_id == 0, so we dont care abt the driver
                    else
                    {
                        int actual_roll_1 = history_matrix[0, (cond.pos - 1), cond.reality, ((Hist_IDX + (this.max_roll - cond.roll) + HistoryDepth) % HistoryDepth)];
                        int actual_roll_2 = history_matrix[1, (cond.pos - 1), cond.reality, ((Hist_IDX + (this.max_roll - cond.roll) + HistoryDepth) % HistoryDepth)];
                        //print_HistoryMatrix_partial(history_matrix);

                        if (actual_roll_1 != cond.item_id && actual_roll_2 != cond.item_id)
                        {
                            bruteforce_match = false;
                            break;
                        }
                    }
                }
                if (bruteforce_match == true)
                {
                    long sim_time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    Console.WriteLine(String.Format("TIME: {0:0.00} s", (sim_time - sim_start) / 1000.0));

                    double percentage = (100.0 * bruteforce_attempts) / ItemData.MAX_RNG_Combinations;
                    Console.WriteLine(String.Format("{0:n0} Attempts ({1:0.00}%) cur_RNG: 0x{2:X8}\n", bruteforce_attempts, percentage, this.RNG));

                    this.UpdateStatusMSG = String.Format("{0:n0} ({1:0.00}%)", bruteforce_attempts, percentage);
                    this.Invoke(this.updateStatusDelegate);

                    Hist_IDX = Hist_IDX - ((HistoryDepth - this.max_roll));
                    if (Hist_IDX < 0)
                        Hist_IDX += HistoryDepth;
                    if (bruteforce_attempts < (ulong)HistoryDepth)
                        Hist_IDX += this.max_roll;
                    this.RNG = RNG_history[Hist_IDX];

                    Console.WriteLine(String.Format("MATCH: RNG = {0:X8}", this.RNG));
                    this.Invoke(this.finishStatusDelegate);
                    break;
                }
                if (bruteforce_attempts % 1.00e6 == 0)
                {
                    long sim_time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    Console.WriteLine(String.Format("TIME: {0:0.00} s", (sim_time - sim_start) / 1000.0));

                    double percentage = (100.0 * bruteforce_attempts) / ItemData.MAX_RNG_Combinations;
                    Console.WriteLine(String.Format("{0:n0} Attempts ({1:0.00}%) cur_RNG: 0x{2:X8}\n", bruteforce_attempts, percentage, this.RNG));

                    this.UpdateStatusMSG = String.Format("{0:n0} ({1:0.00}%)", bruteforce_attempts, percentage);
                    this.Invoke(this.updateStatusDelegate);

                    // check if we looped through all seeds
                    if (bruteforce_attempts > 4.35e9)
                    {
                        this.UpdateStatusMSG = "IMPOSSIBLE PATTERN";
                        this.Invoke(this.updateStatusDelegate);
                        this.Invoke(this.finishStatusDelegate);
                        break;
                    }
                }
                bruteforce_attempts++;

                // update the RNG and HistIDX
                RNG = CodeRandomness.AdvanceRNG(RNG);
                Hist_IDX -= 1;
            }
        }
        // Delegate Functions that I keep forgetting where they are and I really should tidy this up but I wont
        public void UpdateStatus()
        {
            bruteforcer_button.Text = "Stop Bruteforcer";
            bruteforcer_button.Update();

            this.textBox2.Text = this.UpdateStatusMSG;
            this.textBox2.BackColor = System.Drawing.Color.White;
            this.textBox2.Update();
            this.textBox1.Text = "Searching...";
            this.textBox1.BackColor = System.Drawing.Color.White;
            this.textBox1.Update();
        }
        public void FinishStatus()
        {
            this.active = 0;
            bruteforcer_button.Text = "Bruteforce from XML";
            bruteforcer_button.Update();

            this.textBox2.Text = this.UpdateStatusMSG;
            this.textBox2.BackColor = System.Drawing.Color.Cyan;
            this.textBox2.Update();
            this.textBox1.Text = String.Format("{0:X}", this.RNG);
            this.textBox1.BackColor = System.Drawing.Color.Cyan;
            this.textBox1.Update();

            // place the found RNG into the initial RNG box and display results
            this.textBox3.Text = String.Format("{0:X}", this.RNG);

            // force button1 to be clicked
            this.button1_Click(null, EventArgs.Empty);
        }
        */




        // yeah yeah this looks bad, but its faster to have all of these just available 
        // and grab a handle to them through a function.. I think
        public uint[,] ItemProbMatrix_char1_NoShock = new uint[10, 8];
        public uint[,] ItemProbMatrix_char2_NoShock = new uint[10, 8];
        public uint[] total_column_weight_1_NoShock = new uint[8];
        public uint[] total_column_weight_2_NoShock = new uint[8];
        //
        public uint[,] ItemProbMatrix_char1_NoShock_NoSpecial = new uint[10, 8];
        public uint[,] ItemProbMatrix_char2_NoShock_NoSpecial = new uint[10, 8];
        public uint[] total_column_weight_1_NoShock_NoSpecial = new uint[8];
        public uint[] total_column_weight_2_NoShock_NoSpecial = new uint[8];
        //
        public uint[,] ItemProbMatrix_char1_NoShock_NoBlue = new uint[10, 8];
        public uint[,] ItemProbMatrix_char2_NoShock_NoBlue = new uint[10, 8];
        public uint[] total_column_weight_1_NoShock_NoBlue = new uint[8];
        public uint[] total_column_weight_2_NoShock_NoBlue = new uint[8];
        //
        public uint[,] ItemProbMatrix_char1_NoShock_NoStar = new uint[10, 8];
        public uint[,] ItemProbMatrix_char2_NoShock_NoStar = new uint[10, 8];
        public uint[] total_column_weight_1_NoShock_NoStar = new uint[8];
        public uint[] total_column_weight_2_NoShock_NoStar = new uint[8];
        //
        public uint[,] ItemProbMatrix_char1_NoShock_NoShroom = new uint[10, 8];
        public uint[,] ItemProbMatrix_char2_NoShock_NoShroom = new uint[10, 8];
        public uint[] total_column_weight_1_NoShock_NoShroom = new uint[8];
        public uint[] total_column_weight_2_NoShock_NoShroom = new uint[8];
        //
        public uint[,] ItemProbMatrix_char1_NoShock_NoTripleShroom = new uint[10, 8];
        public uint[,] ItemProbMatrix_char2_NoShock_NoTripleShroom = new uint[10, 8];
        public uint[] total_column_weight_1_NoShock_NoTripleShroom = new uint[8];
        public uint[] total_column_weight_2_NoShock_NoTripleShroom = new uint[8];



        public uint[] simulated_realities = new uint[6];



        // I know this looks very painful and stupid, but its quicker for a bruteforcer to have these matrices
        // available like this (although maybe there is a neater way of filling them...)
        public void set_up_matrices(string driver_1, string driver_2)
        {
            // set up the probability matrices
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock, 0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock, 1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock, 2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock, 3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock, 4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock, 5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock, 6, ItemData.item_weights[ItemData.item_name_to_ID("Star")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock, 7, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock, 8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock, 9, ItemData.item_weights[CharData.specials_dict[driver_1]]);
            // and #2
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock, 0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock, 1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock, 2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock, 3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock, 4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock, 5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock, 6, ItemData.item_weights[ItemData.item_name_to_ID("Star")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock, 7, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock, 8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock, 9, ItemData.item_weights[CharData.specials_dict[driver_2]]);
            // pre-calculate the total weight for each column
            for (int pos = 0; pos < 8; pos++)
            {
                this.total_column_weight_1_NoShock[pos] = 0;
                this.total_column_weight_2_NoShock[pos] = 0;
                for (int rollableItemID = 0; rollableItemID < this.ItemProbMatrix_char1_NoShock.GetLength(0); rollableItemID++)
                {
                    this.total_column_weight_1_NoShock[pos] += this.ItemProbMatrix_char1_NoShock[rollableItemID, pos];
                    this.total_column_weight_2_NoShock[pos] += this.ItemProbMatrix_char2_NoShock[rollableItemID, pos];
                }
            }

            // set up the probability matrices
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoSpecial, 0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoSpecial, 1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoSpecial, 2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoSpecial, 3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoSpecial, 4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoSpecial, 5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoSpecial, 6, ItemData.item_weights[ItemData.item_name_to_ID("Star")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoSpecial, 7, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoSpecial, 8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoSpecial, 9, ItemData.item_wNONE);
            // and #2
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoSpecial, 0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoSpecial, 1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoSpecial, 2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoSpecial, 3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoSpecial, 4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoSpecial, 5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoSpecial, 6, ItemData.item_weights[ItemData.item_name_to_ID("Star")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoSpecial, 7, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoSpecial, 8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoSpecial, 9, ItemData.item_wNONE);
            // pre-calculate the total weight for each column
            for (int pos = 0; pos < 8; pos++)
            {
                this.total_column_weight_1_NoShock_NoSpecial[pos] = 0;
                this.total_column_weight_2_NoShock_NoSpecial[pos] = 0;
                for (int rollableItemID = 0; rollableItemID < this.ItemProbMatrix_char1_NoShock_NoSpecial.GetLength(0); rollableItemID++)
                {
                    this.total_column_weight_1_NoShock_NoSpecial[pos] += this.ItemProbMatrix_char1_NoShock_NoSpecial[rollableItemID, pos];
                    this.total_column_weight_2_NoShock_NoSpecial[pos] += this.ItemProbMatrix_char2_NoShock_NoSpecial[rollableItemID, pos];
                }
            }
            // set up the probability matrices
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoBlue, 0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoBlue, 1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoBlue, 2, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoBlue, 3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoBlue, 4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoBlue, 5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoBlue, 6, ItemData.item_weights[ItemData.item_name_to_ID("Star")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoBlue, 7, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoBlue, 8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoBlue, 9, ItemData.item_weights[CharData.specials_dict[driver_1]]);
            // and #2
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoBlue, 0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoBlue, 1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoBlue, 2, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoBlue, 3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoBlue, 4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoBlue, 5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoBlue, 6, ItemData.item_weights[ItemData.item_name_to_ID("Star")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoBlue, 7, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoBlue, 8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoBlue, 9, ItemData.item_weights[CharData.specials_dict[driver_2]]);
            // pre-calculate the total weight for each column
            for (int pos = 0; pos < 8; pos++)
            {
                this.total_column_weight_1_NoShock_NoBlue[pos] = 0;
                this.total_column_weight_2_NoShock_NoBlue[pos] = 0;
                for (int rollableItemID = 0; rollableItemID < this.ItemProbMatrix_char1_NoShock_NoBlue.GetLength(0); rollableItemID++)
                {
                    this.total_column_weight_1_NoShock_NoBlue[pos] += this.ItemProbMatrix_char1_NoShock_NoBlue[rollableItemID, pos];
                    this.total_column_weight_2_NoShock_NoBlue[pos] += this.ItemProbMatrix_char2_NoShock_NoBlue[rollableItemID, pos];
                }
            }

            // set up the probability matrices
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoStar, 0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoStar, 1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoStar, 2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoStar, 3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoStar, 4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoStar, 5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoStar, 6, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoStar, 7, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoStar, 8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoStar, 9, ItemData.item_weights[CharData.specials_dict[driver_1]]);
            // and #2
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoStar, 0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoStar, 1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoStar, 2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoStar, 3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoStar, 4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoStar, 5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoStar, 6, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoStar, 7, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoStar, 8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoStar, 9, ItemData.item_weights[CharData.specials_dict[driver_2]]);
            // pre-calculate the total weight for each column
            for (int pos = 0; pos < 8; pos++)
            {
                this.total_column_weight_1_NoShock_NoStar[pos] = 0;
                this.total_column_weight_2_NoShock_NoStar[pos] = 0;
                for (int rollableItemID = 0; rollableItemID < this.ItemProbMatrix_char1_NoShock_NoStar.GetLength(0); rollableItemID++)
                {
                    this.total_column_weight_1_NoShock_NoStar[pos] += this.ItemProbMatrix_char1_NoShock_NoStar[rollableItemID, pos];
                    this.total_column_weight_2_NoShock_NoStar[pos] += this.ItemProbMatrix_char2_NoShock_NoStar[rollableItemID, pos];
                }
            }

            // set up the probability matrices
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoShroom, 0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoShroom, 1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoShroom, 2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoShroom, 3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoShroom, 4, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoShroom, 5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoShroom, 6, ItemData.item_weights[ItemData.item_name_to_ID("Star")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoShroom, 7, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoShroom, 8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoShroom, 9, ItemData.item_weights[CharData.specials_dict[driver_1]]);
            // and #2
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoShroom, 0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoShroom, 1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoShroom, 2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoShroom, 3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoShroom, 4, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoShroom, 5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoShroom, 6, ItemData.item_weights[ItemData.item_name_to_ID("Star")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoShroom, 7, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoShroom, 8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoShroom, 9, ItemData.item_weights[CharData.specials_dict[driver_2]]);
            // pre-calculate the total weight for each column
            for (int pos = 0; pos < 8; pos++)
            {
                this.total_column_weight_1_NoShock_NoShroom[pos] = 0;
                this.total_column_weight_2_NoShock_NoShroom[pos] = 0;
                for (int rollableItemID = 0; rollableItemID < this.ItemProbMatrix_char1_NoShock_NoShroom.GetLength(0); rollableItemID++)
                {
                    this.total_column_weight_1_NoShock_NoShroom[pos] += this.ItemProbMatrix_char1_NoShock_NoShroom[rollableItemID, pos];
                    this.total_column_weight_2_NoShock_NoShroom[pos] += this.ItemProbMatrix_char2_NoShock_NoShroom[rollableItemID, pos];
                }
            }

            // set up the probability matrices
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoTripleShroom, 0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoTripleShroom, 1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoTripleShroom, 2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoTripleShroom, 3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoTripleShroom, 4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoTripleShroom, 5, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoTripleShroom, 6, ItemData.item_weights[ItemData.item_name_to_ID("Star")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoTripleShroom, 7, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoTripleShroom, 8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")]);
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoTripleShroom, 9, ItemData.item_weights[CharData.specials_dict[driver_1]]);
            // and #2
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoTripleShroom, 0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoTripleShroom, 1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoTripleShroom, 2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoTripleShroom, 3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoTripleShroom, 4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoTripleShroom, 5, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoTripleShroom, 6, ItemData.item_weights[ItemData.item_name_to_ID("Star")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoTripleShroom, 7, ItemData.item_wNONE);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoTripleShroom, 8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")]);
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoTripleShroom, 9, ItemData.item_weights[CharData.specials_dict[driver_2]]);
            // pre-calculate the total weight for each column
            for (int pos = 0; pos < 8; pos++)
            {
                this.total_column_weight_1_NoShock_NoTripleShroom[pos] = 0;
                this.total_column_weight_2_NoShock_NoTripleShroom[pos] = 0;
                for (int rollableItemID = 0; rollableItemID < this.ItemProbMatrix_char1_NoShock_NoTripleShroom.GetLength(0); rollableItemID++)
                {
                    this.total_column_weight_1_NoShock_NoTripleShroom[pos] += this.ItemProbMatrix_char1_NoShock_NoTripleShroom[rollableItemID, pos];
                    this.total_column_weight_2_NoShock_NoTripleShroom[pos] += this.ItemProbMatrix_char2_NoShock_NoTripleShroom[rollableItemID, pos];
                }
            }
        }

        public uint[,] get_prob_matrix(uint driver, uint reality)
        {
            if (driver == 1)
            {
                if (reality == 0) return this.ItemProbMatrix_char1_NoShock;
                if (reality == 1) return this.ItemProbMatrix_char1_NoShock_NoSpecial;
                if (reality == 2) return this.ItemProbMatrix_char1_NoShock_NoBlue;
                if (reality == 3) return this.ItemProbMatrix_char1_NoShock_NoStar;
                if (reality == 4) return this.ItemProbMatrix_char1_NoShock_NoShroom;
                if (reality == 5) return this.ItemProbMatrix_char1_NoShock_NoTripleShroom;
            }
            if (driver == 2)
            {
                if (reality == 0) return this.ItemProbMatrix_char2_NoShock;
                if (reality == 1) return this.ItemProbMatrix_char2_NoShock_NoSpecial;
                if (reality == 2) return this.ItemProbMatrix_char2_NoShock_NoBlue;
                if (reality == 3) return this.ItemProbMatrix_char2_NoShock_NoStar;
                if (reality == 4) return this.ItemProbMatrix_char2_NoShock_NoShroom;
                if (reality == 5) return this.ItemProbMatrix_char2_NoShock_NoTripleShroom;
            }
            return null;
        }
        public uint[] get_col_weight(uint driver, uint reality)
        {
            if (driver == 1)
            {
                if (reality == 0) return this.total_column_weight_1_NoShock;
                if (reality == 1) return this.total_column_weight_1_NoShock_NoSpecial;
                if (reality == 2) return this.total_column_weight_1_NoShock_NoBlue;
                if (reality == 3) return this.total_column_weight_1_NoShock_NoStar;
                if (reality == 4) return this.total_column_weight_1_NoShock_NoShroom;
                if (reality == 5) return this.total_column_weight_1_NoShock_NoTripleShroom;
            }
            if (driver == 2)
            {
                if (reality == 0) return this.total_column_weight_2_NoShock;
                if (reality == 1) return this.total_column_weight_2_NoShock_NoSpecial;
                if (reality == 2) return this.total_column_weight_2_NoShock_NoBlue;
                if (reality == 3) return this.total_column_weight_2_NoShock_NoStar;
                if (reality == 4) return this.total_column_weight_2_NoShock_NoShroom;
                if (reality == 5) return this.total_column_weight_2_NoShock_NoTripleShroom;
            }
            return null;
        }
    }
}
