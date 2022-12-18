﻿using System;
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
    public partial class MainForm : Form
    {
        public void add_row_to_matrix(uint index, uint[] row, uint[,] matrix)
        {
            for (int col = 0; col < 8; col++)
                matrix[index, col] = row[col];
        }
        private Thread workerThread = null;
        private bool stopProcess = false;
        private delegate void UpdateStatusDelegate();
        private delegate void FinishStatusDelegate();
        private UpdateStatusDelegate updateStatusDelegate = null;
        private FinishStatusDelegate finishStatusDelegate = null;
        private String UpdateStatusMSG;

        private uint[,] ItemProbMatrix_char1 = new uint[10, 8];
        private uint[,] ItemProbMatrix_char2 = new uint[10, 8];
        private uint[] total_column_weight_1 = new uint[8];
        private uint[] total_column_weight_2 = new uint[8];
        private uint RNG;
        private List<BruteforceCondition> Conditions;

        private String selected_driver_1;
        private String selected_driver_2;

        private void print_HistoryMatrix_partial(int[,,] history_matrix)
        {
            for (int i = 0; i < 6; i++)
            {
                Console.WriteLine(String.Format("{0:X} {1:X} {2:X} {3:X}",
                    history_matrix[0, i, 2],
                    history_matrix[0, i, 3],
                    history_matrix[0, i, 4],
                    history_matrix[0, i, 5]));
                Console.WriteLine(String.Format("{0:X} {1:X} {2:X} {3:X}",
                    history_matrix[1, i, 2],
                    history_matrix[1, i, 3],
                    history_matrix[1, i, 4],
                    history_matrix[1, i, 5]));
                Console.WriteLine("------");
            }
            Console.WriteLine("=========");
        }

        private void HeavyOperation()
        {
            this.UpdateStatusMSG = "Starting...";

            // Setup History vals
            int HistoryDepth = 6;
            int[,,] history_matrix = new int[2, HistoryDepth, 8];
            uint[] RNG_history = new uint[HistoryDepth];
            for (int step = 0; step < HistoryDepth; step++)
            {
                RNG_history[step] = this.RNG;
            }

            // create row string array
            int[] row_content_1 = new int[8];
            int[] row_content_2 = new int[8];
            uint bruteforce_attempts = 0;
            while (true)
            {
                // get row content fromg RNG simulation
                row_content_1 = get_RollsPerPos_ItemIDs(this.RNG, this.ItemProbMatrix_char1, this.total_column_weight_1, this.selected_driver_1);
                row_content_2 = get_RollsPerPos_ItemIDs(this.RNG, this.ItemProbMatrix_char2, this.total_column_weight_2, this.selected_driver_2);

                // populate lowest history matrix row with the new content
                RNG_history[(HistoryDepth - 1)] = RNG;
                for (int pos = 0; pos < 8; pos++)
                {
                    history_matrix[0, (HistoryDepth - 1), pos] = row_content_1[pos];
                    history_matrix[1, (HistoryDepth - 1), pos] = row_content_2[pos];
                }
                // compare the history matrix with the condition matrix
                bool bruteforce_match = true;

                foreach (BruteforceCondition cond in this.Conditions)
                {
                    // check if we want a specific driver first
                    if (cond.driver_id > 0)
                    {
                        int actual_roll = history_matrix[(cond.driver_id - 1), cond.roll, (cond.pos - 1)];
                        if (actual_roll != cond.item_id)
                        {
                            bruteforce_match = false;
                            break;
                        }
                    }
                    // cond.driver_id == 0, so we dont care abt the driver
                    else
                    {
                        int actual_roll_1 = history_matrix[0, cond.roll, (cond.pos - 1)];
                        int actual_roll_2 = history_matrix[1, cond.roll, (cond.pos - 1)];
                        //print_HistoryMatrix_partial(history_matrix);

                        if (actual_roll_1 != cond.item_id & actual_roll_2 != cond.item_id)
                        {
                            bruteforce_match = false;
                            break;
                        }
                    }
                }
                if (bruteforce_match == true)
                {
                    this.RNG = RNG_history[0];
                    Console.WriteLine(String.Format("MATCH: RNG = {0:X}", this.RNG));
                    this.Invoke(this.finishStatusDelegate);
                    break;
                }

                if (bruteforce_attempts % 100000 == 0)
                {
                    double percentage = (100.0 * bruteforce_attempts) / ItemData.MAX_RNG_Combinations;
                    Console.WriteLine(String.Format("{0:n0} Attempts ({1:0.00}%) cur_RNG: 0x{2:X}", bruteforce_attempts, percentage, this.RNG));

                    this.UpdateStatusMSG = String.Format("{0:n0} ({1:0.00}%)", bruteforce_attempts, percentage);
                    this.Invoke(this.updateStatusDelegate);
                }
                bruteforce_attempts++;

                // step through the history matrix and shift everything up 1 row to update it
                for (int step = 0; step < (HistoryDepth - 1); step++)
                {
                    RNG_history[step] = RNG_history[(step + 1)];
                    for (int pos = 0; pos < 8; pos++)
                    {
                        history_matrix[0, step, pos] = history_matrix[0, (step + 1), pos];
                        history_matrix[1, step, pos] = history_matrix[1, (step + 1), pos];
                    }
                }

                // update the RNG
                RNG = CodeRandomness.AdvanceRNG(RNG);
            }
        }
        private void UpdateStatus()
        {
            this.textBox1.Text = "Searching...";
            this.textBox1.Update();
            this.textBox2.Text = this.UpdateStatusMSG;
            this.textBox2.Update();
        }
        private void FinishStatus()
        {
            this.textBox1.Text = String.Format("{0:X}", this.RNG);
            this.textBox1.Update();
        }

        public String[] get_RollsPerPos_ItemNames(uint RNG, uint[,] ProbMatrix, uint[] ColWeightVector, String driver_name)
        {
            // create row string array
            String[] row_content = new String[9];
            // RNG value goes into first column
            row_content[0] = "0x" + RNG.ToString("X8");

            // Get ItemRolls for every Position
            for (int pos = 0; pos < 8; pos++)
            {
                int rolledItemID = CodeRandomness.calc_ItemRoll(RNG, ProbMatrix, pos, ColWeightVector[pos]);

                // defaulting to "Invalid" first
                String rolledItem_name= "Invalid Roll";
                // check if we have a valid roll and get the corresponding itemname
                if (rolledItemID != -1)
                {
                    rolledItem_name = ItemData.rollable_items_names[rolledItemID];
                }

                // if the result is "Special", replace it by the correct special item
                if (rolledItem_name == "Special")
                {
                    rolledItem_name = ItemData.item_names[CharData.specials_dict[driver_name]];
                    rolledItemID = (int)ItemData.item_name_to_ID(rolledItem_name);
                }

                // special check for Triple Reds only if not Pos#1
                if (rolledItem_name == "Triple Greens" & pos > 0)
                {
                    // advance RNG once more for this extra check (temporarily!!!)
                    uint shellRNG = CodeRandomness.AdvanceRNG(RNG);
                    // 40% chance of converting to Triple Reds
                    if (0.4 < (CodeRandomness.shiftRNGcnvtoFloat(shellRNG)))
                    {
                        rolledItemID = (int)ItemData.item_name_to_ID("Triple Reds");
                        rolledItem_name = ItemData.rollable_items_names[rolledItemID];
                    }
                }

                // rolled items into the others
                row_content[pos + 1] = rolledItem_name;
            }
            // return resulting content array
            return row_content;
        }
        public int[] get_RollsPerPos_ItemIDs(uint RNG, uint[,] ProbMatrix, uint[] ColWeightVector, String driver_name)
        {
            // create row string array
            int[] row_content = new int[8];

            // Get ItemRolls for every Position
            for (int pos = 0; pos < 8; pos++)
            {
                int rolledItemID = CodeRandomness.calc_ItemRoll(RNG, ProbMatrix, pos, ColWeightVector[pos]);
                String rolledItem_name;

                // if the result is "Special", replace it by the correct special item
                if (rolledItemID == 0x9)
                {
                    rolledItem_name = ItemData.item_names[CharData.specials_dict[driver_name]];
                    rolledItemID = (int)ItemData.item_name_to_ID(rolledItem_name);
                }
                else
                {
                    // a normal item was rolled, convert it from rollableID to itemID
                    rolledItem_name = ItemData.rollable_items_names[rolledItemID];
                    rolledItemID = (int)ItemData.item_name_to_ID(rolledItem_name);
                }

                // special check for Triple Reds only if not Pos#1
                if (rolledItemID == 0x13 & pos > 0)
                {
                    // advance RNG once more for this extra check (temporarily!!!)
                    uint shellRNG = CodeRandomness.AdvanceRNG(RNG);
                    // 40% chance of converting to Triple Reds
                    if (0.4 < CodeRandomness.shiftRNGcnvtoFloat(shellRNG))
                        rolledItemID = 0x11;
                }

                // rolled items into the others
                row_content[pos] = rolledItemID;
            }
            // return resulting content array
            return row_content;
        }

        public MainForm()
        {
            InitializeComponent();
            // Im putting these defaults HERE because VSC doesnt want me to edit the auto-gen code...
            this.comboBox1.SelectedIndex = 1; // BL
            this.comboBox2.SelectedIndex = 7; // BJR

            // this forbids the user from changing grid content (and removes last row)
            this.dataGridView1.AllowUserToAddRows = false;
        }

        // this button controls the plain RNG-Simulation
        private void button1_Click(object sender, EventArgs e)
        {
            // reset the grid contents
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            // create column headers - Add(col_name, col_text);
            dataGridView1.Columns.Add("RNGSeed", "RNG Value");
            for (int pos = 1; pos < 9; pos++)
                dataGridView1.Columns.Add("Pos" + pos.ToString(), "Pos " + pos.ToString());
            for (int col = 0; col < 9; col++)
                dataGridView1.Columns[col].Width = 90;

            // some nice colors
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 170, 170, 170);
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.Columns[0].DefaultCellStyle.BackColor = Color.FromArgb(255, 192, 255, 192);

            // init RNG from TextField
            this.RNG = uint.Parse(textBox3.Text.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);

            // set up the 2 probability matrices
            add_row_to_matrix(0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")], this.ItemProbMatrix_char1);
            add_row_to_matrix(1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")], this.ItemProbMatrix_char1);
            add_row_to_matrix(2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")], this.ItemProbMatrix_char1);
            add_row_to_matrix(3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")], this.ItemProbMatrix_char1);
            add_row_to_matrix(4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")], this.ItemProbMatrix_char1);
            add_row_to_matrix(5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")], this.ItemProbMatrix_char1);
            add_row_to_matrix(6, ItemData.item_weights[ItemData.item_name_to_ID("Star")], this.ItemProbMatrix_char1);
            add_row_to_matrix(7, ItemData.item_weights[ItemData.item_name_to_ID("Lightning")], this.ItemProbMatrix_char1);
            add_row_to_matrix(8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")], this.ItemProbMatrix_char1);
            add_row_to_matrix(9, ItemData.item_weights[CharData.specials_dict[this.selected_driver_1]], this.ItemProbMatrix_char1);
            // and #2
            add_row_to_matrix(0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")], this.ItemProbMatrix_char2);
            add_row_to_matrix(1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")], this.ItemProbMatrix_char2);
            add_row_to_matrix(2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")], this.ItemProbMatrix_char2);
            add_row_to_matrix(3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")], this.ItemProbMatrix_char2);
            add_row_to_matrix(4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")], this.ItemProbMatrix_char2);
            add_row_to_matrix(5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")], this.ItemProbMatrix_char2);
            add_row_to_matrix(6, ItemData.item_weights[ItemData.item_name_to_ID("Star")], this.ItemProbMatrix_char2);
            add_row_to_matrix(7, ItemData.item_weights[ItemData.item_name_to_ID("Lightning")], this.ItemProbMatrix_char2);
            add_row_to_matrix(8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")], this.ItemProbMatrix_char2);
            add_row_to_matrix(9, ItemData.item_weights[CharData.specials_dict[this.selected_driver_2]], this.ItemProbMatrix_char2);

            // un-set the weights of unobtainable items now
            // Sware's default is that Lightning is NOT obtainable...
            // Mind that BlueShell and all Specials may only be held once

            // pre-calculate the total weight for each column
            for (int pos = 0; pos < 8; pos++)
            {
                this.total_column_weight_1[pos] = 0;
                this.total_column_weight_2[pos] = 0;
                for (int rollableItemID = 0; rollableItemID < this.ItemProbMatrix_char1.GetLength(0); rollableItemID++)
                {
                    this.total_column_weight_1[pos] += this.ItemProbMatrix_char1[rollableItemID, pos];
                    this.total_column_weight_2[pos] += this.ItemProbMatrix_char2[rollableItemID, pos];
                }
            }

            // create row string array
            String[] row_content = new String[9];
            // and start simulating the RNG
            for (int i = 0; i < 10; i++)
            {
                // get row content fromg RNG simulation - character 1
                row_content = get_RollsPerPos_ItemNames(this.RNG, this.ItemProbMatrix_char1, this.total_column_weight_1, selected_driver_1);
                this.dataGridView1.Rows.Add(row_content);
                // check if there is a special cell color defined for the pull
                for (int col = 1; col < 9; col++)
                {
                    if (ItemData.ItemColor_Dict.ContainsKey(row_content[col]) == true)
                    {
                        Color cell_color = ItemData.ItemColor_Dict[row_content[col]];
                        this.dataGridView1.Rows[(i*2)+0].Cells[col].Style.BackColor = cell_color;
                    }
                }

                // get row content fromg RNG simulation - character 2
                row_content = get_RollsPerPos_ItemNames(this.RNG, this.ItemProbMatrix_char2, this.total_column_weight_2, selected_driver_2);
                row_content[0] = ""; // removing the RNG entry here bc its a duplicate
                this.dataGridView1.Rows.Add(row_content);
                // check if there is a special cell color defined for the pull
                for (int col = 1; col < 9; col++)
                {
                    if (ItemData.ItemColor_Dict.ContainsKey(row_content[col]) == true)
                    {
                        Color cell_color = ItemData.ItemColor_Dict[row_content[col]];
                        this.dataGridView1.Rows[(i*2)+1].Cells[col].Style.BackColor = cell_color;
                    }
                }

                // visually divide the different Pulls more
                this.dataGridView1.Rows[(i * 2) + 1].DividerHeight = 4;
                //dataGridView1.GridColor = Color.Black;

                // update the RNG
                RNG = CodeRandomness.AdvanceRNG(RNG);
            }
        }
        // controls the bruteforcing
        private void button2_Click(object sender, EventArgs e)
        {
            // create a new, empty list of bruteforcer conditions
            this.Conditions = new List<BruteforceCondition>();
            uint xml_pos = 0;
            uint xml_roll = 0;
            uint xml_driver_id = 0; // 0 = dont care, 1 = 1, 2 = 2
            string xml_item_name = "";

            // open the pattern XML and readout the conditions
            XmlReader reader = XmlReader.Create("../../BruteForcePattern.xml");
            while (reader.Read())
            {
                switch (reader.Name.ToString())
                {
                    case "Pos":
                        xml_pos = UInt32.Parse(reader.ReadString());
                        break;
                    case "Roll":
                        xml_roll = UInt32.Parse(reader.ReadString());
                        break;
                    case "Item":
                        xml_item_name = reader.ReadString();
                        break;
                    case "Driver":
                        xml_driver_id = UInt32.Parse(reader.ReadString());
                        break;
                    case "Condition":
                        if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            // create a new Condition instance from collected Data
                            BruteforceCondition cond = new BruteforceCondition();
                            cond.pos = xml_pos;
                            cond.roll = xml_roll;
                            cond.driver_id = xml_driver_id;
                            cond.item_id = ItemData.item_name_to_ID(xml_item_name);
                            xml_driver_id = 0; // restore default
                            // and add it to the List IF its not 0 (Green Shell, but we'd never bruteforce for that)
                            if (cond.item_id > 0)
                            {
                                this.Conditions.Add(cond);
                            }
                            else
                            {
                                Console.WriteLine(String.Format("Ruh-Oh! A Condition couldnt be parsed: ItemName={0}", xml_item_name));
                                this.textBox2.Text = String.Format("XMLR saw: {0}", xml_item_name);
                                this.Conditions = new List<BruteforceCondition>();
                                return;
                            }
                        }
                        break;
                }
            }

            // init min + max to find them from the conditions list
            uint min_roll = Int32.MaxValue;
            foreach (BruteforceCondition cond in Conditions)
            {
                if (cond.roll < min_roll) min_roll = cond.roll;
            }
            Console.WriteLine("Collected Conditions:");
            foreach (BruteforceCondition cond in Conditions)
            {
                cond.roll -= min_roll;
                Console.WriteLine(String.Format("{0} at #{1} on Roll-{2} For Driver #{3}", ItemData.item_names[cond.item_id], cond.pos, cond.roll, cond.driver_id));
            }

            // init RNG from TextField
            this.RNG = uint.Parse(textBox3.Text.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);

            // set up the 2 probability matrices
            add_row_to_matrix(0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")], this.ItemProbMatrix_char1);
            add_row_to_matrix(1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")], this.ItemProbMatrix_char1);
            add_row_to_matrix(2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")], this.ItemProbMatrix_char1);
            add_row_to_matrix(3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")], this.ItemProbMatrix_char1);
            add_row_to_matrix(4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")], this.ItemProbMatrix_char1);
            add_row_to_matrix(5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")], this.ItemProbMatrix_char1);
            add_row_to_matrix(6, ItemData.item_weights[ItemData.item_name_to_ID("Star")], this.ItemProbMatrix_char1);
            add_row_to_matrix(7, ItemData.item_weights[ItemData.item_name_to_ID("Lightning")], this.ItemProbMatrix_char1);
            add_row_to_matrix(8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")], this.ItemProbMatrix_char1);
            add_row_to_matrix(9, ItemData.item_weights[CharData.specials_dict[selected_driver_1]], this.ItemProbMatrix_char1);
            // and #2
            add_row_to_matrix(0, ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")], this.ItemProbMatrix_char2);
            add_row_to_matrix(1, ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")], this.ItemProbMatrix_char2);
            add_row_to_matrix(2, ItemData.item_weights[ItemData.item_name_to_ID("Blue")], this.ItemProbMatrix_char2);
            add_row_to_matrix(3, ItemData.item_weights[ItemData.item_name_to_ID("Banana")], this.ItemProbMatrix_char2);
            add_row_to_matrix(4, ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")], this.ItemProbMatrix_char2);
            add_row_to_matrix(5, ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")], this.ItemProbMatrix_char2);
            add_row_to_matrix(6, ItemData.item_weights[ItemData.item_name_to_ID("Star")], this.ItemProbMatrix_char2);
            add_row_to_matrix(7, ItemData.item_weights[ItemData.item_name_to_ID("Lightning")], this.ItemProbMatrix_char2);
            add_row_to_matrix(8, ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")], this.ItemProbMatrix_char2);
            add_row_to_matrix(9, ItemData.item_weights[CharData.specials_dict[selected_driver_2]], this.ItemProbMatrix_char2);

            // un-set the weights of unobtainable items now
            // Sware's default is that Lightning is NOT obtainable...
            // Mind that BlueShell and all Specials may only be held once

            // pre-calculate the total weight for each column
            for (int pos = 0; pos < 8; pos++)
            {
                this.total_column_weight_1[pos] = 0;
                this.total_column_weight_2[pos] = 0;
                for (int rollableItemID = 0; rollableItemID < ItemProbMatrix_char1.GetLength(0); rollableItemID++)
                {
                    this.total_column_weight_1[pos] += this.ItemProbMatrix_char1[rollableItemID, pos];
                    this.total_column_weight_2[pos] += this.ItemProbMatrix_char2[rollableItemID, pos];
                }
            }

            this.workerThread = new Thread(new ThreadStart(this.HeavyOperation));
            this.workerThread.Start();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialise the delegate
            this.updateStatusDelegate = new UpdateStatusDelegate(this.UpdateStatus);
            this.finishStatusDelegate = new FinishStatusDelegate(this.FinishStatus);

            // generate a random number and put it as initial seed (generally helps bruteforcing)
            Random generator = new Random();
            this.RNG = (uint)(generator.NextDouble() * 0x100000000);
            this.textBox3.Text = String.Format("{0:X}", this.RNG);

            // force button1 to be clicked
            this.button1_Click(null, EventArgs.Empty);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.selected_driver_1 = this.comboBox1.GetItemText(this.comboBox1.SelectedItem);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void comboBox11_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox10_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox9_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label15_Click(object sender, EventArgs e)
        {

        }

        private void comboBox11_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.selected_driver_2 = this.comboBox2.GetItemText(this.comboBox2.SelectedItem);
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {

        }
    }
}
