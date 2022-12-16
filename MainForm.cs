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
    public partial class MainForm : Form
    {
        public string get_selected_char_name(uint ID)
        {
            // ughhh Im sorry I have to do it like this...
            if (ID == 1) return this.comboBox1.GetItemText(this.comboBox1.SelectedItem);
            if (ID == 2) return this.comboBox2.GetItemText(this.comboBox2.SelectedItem);
            else return "Invalid";
        }

        public String[] get_RollsPerPos(uint RNG, uint[][] ProbMatrix, uint[] ColWeightVector, uint driverID)
        {
            // create row string array
            String[] row_content = new String[9];

            // RNG value goes into first column
            row_content[0] = "0x" + RNG.ToString("X8");

            // Get ItemRolls for every Position
            for (int pos = 0; pos < 8; pos++)
            {
                int rolledItemID = CodeRandomness.calc_ItemRoll(RNG, ProbMatrix, pos, ColWeightVector[pos]);
                if (rolledItemID == -1) rolledItemID = 0x10; // that's one of the normally unobtainable ones, called "- - -"

                String rolledItem_name = ItemData.rollable_items_names[rolledItemID];
                // if the result is "Special", replace it by the correct special item
                if (rolledItem_name == "Special")
                {
                    rolledItem_name = ItemData.item_names[CharData.specials_dict[get_selected_char_name(driverID)]];
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

            string char_name1 = get_selected_char_name(1);
            string char_name2 = get_selected_char_name(2);

            // init RNG from TextField
            uint RNG = uint.Parse(textBox3.Text.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);

            // set up the 2 probability matrices
            uint[][] ItemProbMatrix_char1 =
            {
                ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")],
                ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")],
                ItemData.item_weights[ItemData.item_name_to_ID("Blue")],
                ItemData.item_weights[ItemData.item_name_to_ID("Banana")],
                ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")],
                ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")],
                ItemData.item_weights[ItemData.item_name_to_ID("Star")],
                ItemData.item_weights[ItemData.item_name_to_ID("Lightning")],
                ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")],
                ItemData.item_weights[CharData.specials_dict[get_selected_char_name(1)]]
            };
            // the 2nd matrix is a clone, except the last weight array is replaced by the diff special
            uint[][] ItemProbMatrix_char2 = (uint[][])ItemProbMatrix_char1.Clone();
            ItemProbMatrix_char2[9] = ItemData.item_weights[CharData.specials_dict[get_selected_char_name(2)]];

            // un-set the weights of unobtainable items now
                // Sware's default is that Lightning is NOT obtainable...
                // Mind that BlueShell and all Specials may only be held once

            // pre-calculate the total weight for each column
            uint[] total_column_weight_1 = new uint[8];
            uint[] total_column_weight_2 = new uint[8];
            for (int pos = 0; pos < 8; pos++)
            {
                for (int rollableItemID = 0; rollableItemID < ItemProbMatrix_char1.Length; rollableItemID++)
                {
                    total_column_weight_1[pos] += ItemProbMatrix_char1[rollableItemID][pos];
                    total_column_weight_2[pos] += ItemProbMatrix_char2[rollableItemID][pos];
                }
            }

            // create row string array
            String[] row_content = new String[9];
            // and start simulating the RNG
            for (int i = 0; i < 10; i++)
            {
                // get row content fromg RNG simulation - character 1
                row_content = get_RollsPerPos(RNG, ItemProbMatrix_char1, total_column_weight_1, 1);
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
                row_content = get_RollsPerPos(RNG, ItemProbMatrix_char2, total_column_weight_2, 2);
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
        private void button2_Click(object sender, EventArgs e)
        {
            // create a new, empty list of bruteforcer conditions
            List<BruteforceCondition> Conditions = new List<BruteforceCondition>();
            uint xml_pos = 0;
            uint xml_roll = 0;
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
                    case "Condition":
                        if (reader.NodeType == XmlNodeType.EndElement)
                        {
                            // create a new Condition instance from collected Data
                            BruteforceCondition cond = new BruteforceCondition();
                            cond.pos = xml_pos;
                            cond.roll = xml_roll;
                            cond.item_id = ItemData.item_name_to_ID(xml_item_name);
                            // and add it to the List
                            Conditions.Add(cond);
                        }
                        break;
                }
            }

            // init min + max to find them from the conditions list
            uint min_roll = Int32.MaxValue;
            uint max_roll = 0;
            foreach (BruteforceCondition cond in Conditions)
            {
                if (cond.roll < min_roll) min_roll = cond.roll;
                if (cond.roll > max_roll) max_roll = cond.roll;
            }
            // create a condition matrix from this
            max_roll = (max_roll - min_roll) + 1;
            String[,] condition_matrix = new string[max_roll, 8];
            String[,] history_matrix_1 = new string[10, 8];
            String[,] history_matrix_2 = new string[10, 8];
            Console.WriteLine("Collected Conditions:");
            foreach (BruteforceCondition cond in Conditions)
            {
                Console.WriteLine(String.Format("{0} at Pos{1} on Roll={2}", ItemData.item_names[cond.item_id], cond.pos, cond.roll));
                // adjust all rolls so the new min becomes 0
                condition_matrix[(cond.roll - min_roll), (cond.pos - 1)] = ItemData.item_names[cond.item_id];
            }

            // init RNG from TextField
            uint RNG = uint.Parse(textBox3.Text.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
            // Setup History vals
            int HistoryDepth = 6;
            uint[] RNG_history = new uint[HistoryDepth];
            for (int step = 0; step < HistoryDepth; step++)
            {
                RNG_history[step] = RNG;
            }

            // set up the 2 probability matrices
            uint[][] ItemProbMatrix_char1 =
            {
                ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")],
                ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")],
                ItemData.item_weights[ItemData.item_name_to_ID("Blue")],
                ItemData.item_weights[ItemData.item_name_to_ID("Banana")],
                ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")],
                ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")],
                ItemData.item_weights[ItemData.item_name_to_ID("Star")],
                ItemData.item_weights[ItemData.item_name_to_ID("Lightning")],
                ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")],
                ItemData.item_weights[CharData.specials_dict[get_selected_char_name(1)]]
            };
            // the 2nd matrix is a clone, except the last weight array is replaced by the diff special
            uint[][] ItemProbMatrix_char2 = (uint[][])ItemProbMatrix_char1.Clone();
            ItemProbMatrix_char2[9] = ItemData.item_weights[CharData.specials_dict[get_selected_char_name(2)]];

            // un-set the weights of unobtainable items now
            // Sware's default is that Lightning is NOT obtainable...
            // Mind that BlueShell and all Specials may only be held once

            // pre-calculate the total weight for each column
            uint[] total_column_weight_1 = new uint[8];
            uint[] total_column_weight_2 = new uint[8];
            for (int pos = 0; pos < 8; pos++)
            {
                for (int rollableItemID = 0; rollableItemID < ItemProbMatrix_char1.Length; rollableItemID++)
                {
                    total_column_weight_1[pos] += ItemProbMatrix_char1[rollableItemID][pos];
                    total_column_weight_2[pos] += ItemProbMatrix_char2[rollableItemID][pos];
                }
            }

            // create row string array
            String[] row_content_1 = new String[9];
            String[] row_content_2 = new String[9];
            uint bruteforce_attempts = 0;
            while (true)
            {
                // get row content fromg RNG simulation
                row_content_1 = get_RollsPerPos(RNG, ItemProbMatrix_char1, total_column_weight_1, 1);
                row_content_2 = get_RollsPerPos(RNG, ItemProbMatrix_char2, total_column_weight_2, 2);

                // populate lowest history matrix row with the new content
                RNG_history[(HistoryDepth - 1)] = RNG;
                for (int pos = 0; pos < 8; pos++)
                {
                    history_matrix_1[(HistoryDepth - 1), pos] = row_content_1[(pos + 1)];
                    history_matrix_2[(HistoryDepth - 1), pos] = row_content_2[(pos + 1)];
                }
                // compare the history matrix with the condition matrix
                bool bruteforce_match = true;
                for (int step = 0; step < max_roll; step++)
                {
                    for (int pos = 2; pos < 6; pos++)
                    {
                        string desired_roll = condition_matrix[(max_roll - 1 - step), pos];
                        if (String.IsNullOrEmpty(desired_roll)) continue;

                        string actual_roll_1 = history_matrix_1[(HistoryDepth - 1 - step), pos];
                        string actual_roll_2 = history_matrix_2[(HistoryDepth - 1 - step), pos];
                        if (desired_roll != actual_roll_1 & desired_roll != actual_roll_2)
                        {
                            bruteforce_match = false;
                            break;
                        }
                    }
                }
                if (bruteforce_match == true)
                {
                    uint MatchingRNG = RNG_history[HistoryDepth - max_roll];
                    Console.WriteLine(String.Format("MATCH: RNG = {0:X}", MatchingRNG));
                    this.textBox1.Text = String.Format("{0:X}", MatchingRNG);
                    break;
                }

                bruteforce_attempts++;
                if (bruteforce_attempts % 100000 == 0)
                {
                    double percentage = (100.0 * bruteforce_attempts) / ItemData.MAX_RNG_Combinations;
                    Console.WriteLine(String.Format("{0:n0} Attempts ({1:0.00}%)", bruteforce_attempts, percentage));
                    //this.textBox2.Text = String.Format("{0:n0} Attempts ({1:0.00}%)", bruteforce_attempts, percentage);
                }

                // step through the history matrix and shift everything up 1 row to update it
                for (int step = 0; step < (HistoryDepth - 1); step++)
                {
                    RNG_history[step] = RNG_history[(step + 1)];
                    for (int pos = 0; pos < 8; pos++)
                    {
                        history_matrix_1[step, pos] = history_matrix_1[(step + 1), pos];
                        history_matrix_2[step, pos] = history_matrix_2[(step + 1), pos];
                    }
                }
                // this.dataGridView1.Rows.Add(row_content);

                // update the RNG
                RNG = CodeRandomness.AdvanceRNG(RNG);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
        private void Form1_Load(object sender, EventArgs e)
        {

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
    }
}
