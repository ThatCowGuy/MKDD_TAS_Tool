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
        public MainForm()
        {
            InitializeComponent();
            // Im putting these defaults HERE because VSC doesnt want me to edit the auto-gen code...
            this.comboBox1.SelectedIndex = 1; // BL
            this.comboBox2.SelectedIndex = 7; // BJR

            this.comboBox3.SelectedIndex = 0;
            this.comboBox4.SelectedIndex = 0;
            this.comboBox5.SelectedIndex = 0;
            this.comboBox6.SelectedIndex = 0;
            this.comboBox7.SelectedIndex = 0;
            this.comboBox8.SelectedIndex = 0;
            this.comboBox9.SelectedIndex = 0;
            this.comboBox10.SelectedIndex = 0;
            this.comboBox11.SelectedIndex = 0;
            this.comboBox12.SelectedIndex = 0;
            this.comboBox13.SelectedIndex = 0;
            this.comboBox14.SelectedIndex = 0;
            this.comboBox15.SelectedIndex = 0;
            this.comboBox16.SelectedIndex = 0;
            this.comboBox17.SelectedIndex = 0;

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
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200, 200);
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
                ItemData.item_weights[ItemData.item_name_to_ID("Blue Shell")],
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
            for (uint i = 0; i < 20; i++)
            {
                // RNG value goes into first column
                row_content[0] = "0x" + RNG.ToString("X8");

                // Get ItemRolls for every Position
                for (int pos = 0; pos < 8; pos++)
                {
                    int rolledItemID = CodeRandomness.calc_ItemRoll(RNG, ItemProbMatrix_char1, pos, total_column_weight_1[pos]);
                    if (rolledItemID == -1) rolledItemID = 0x10; // that's one of the normally unobtainable ones, called "- - -"

                    String rolledItem_name = ItemData.rollable_items_names[rolledItemID];
                    // if the result is "Special", replace it by the correct special item
                    if (rolledItem_name == "Special")
                    {
                        rolledItem_name = ItemData.item_names[CharData.specials_dict[get_selected_char_name(1)]];
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

                // add results to grid and party
                dataGridView1.Rows.Add(row_content);

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
            Console.WriteLine("Collected Conditions:");
            foreach (BruteforceCondition cond in Conditions)
            {
                Console.WriteLine(String.Format("{0} at Pos{1} on Roll={2}", ItemData.item_names[cond.item_id], cond.pos, cond.roll));
            }

            // reset the grid contents
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            // create column headers - Add(col_name, col_text);
            dataGridView1.Columns.Add("RNGSeed", "RNG Value");
            for (int pos = 1; pos < 9; pos++)
                dataGridView1.Columns.Add("Pos" + pos.ToString(), "Pos " + pos.ToString());
            for (int col = 0; col < 9; col++)
                dataGridView1.Columns[col].Width = 90;

            // init RNG from TextField
            uint RNG = uint.Parse(textBox3.Text.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
            // Setup History vals
            uint[] RNG_history = new uint[10];
            char[,] Item_history = new char[8, 10];
            for (int k = 0; k < 8; k++)
            {
                for (int j = 0; j < 10; j++)
                {
                    RNG_history[j] = RNG;
                    Item_history[k, j] = 'x';
                }
            }

            // set up the 2 probability matrices
            uint[][] ItemProbMatrix_char1 =
            {
                ItemData.item_weights[ItemData.item_name_to_ID("Green Shell")],
                ItemData.item_weights[ItemData.item_name_to_ID("Red Shell")],
                ItemData.item_weights[ItemData.item_name_to_ID("Blue Shell")],
                ItemData.item_weights[ItemData.item_name_to_ID("Banana")],
                ItemData.item_weights[ItemData.item_name_to_ID("Mushroom")],
                ItemData.item_weights[ItemData.item_name_to_ID("Triple Shrooms")],
                ItemData.item_weights[ItemData.item_name_to_ID("Star")],
                ItemData.item_weights[ItemData.item_name_to_ID("Lightning")],
                ItemData.item_weights[ItemData.item_name_to_ID("Fake Box")],
                ItemData.item_weights[CharData.specials_dict[get_selected_char_name(1)]]
            };
            // the 2nd matrix is a clone, except the last weight array is replaced by the diff special
            uint[][] ItemProbMatrix_char2 = (uint[][]) ItemProbMatrix_char1.Clone();
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

            for (uint i = 0; i < 5; i++)
            {
                // Get ItemRolls for every Position
                for (int pos = 0; pos < 8; pos++)
                {
                    int rolledItemID = CodeRandomness.calc_ItemRoll(RNG, ItemProbMatrix_char1, pos, total_column_weight_1[pos]);
                    if (rolledItemID == -1) rolledItemID = 0x10; // that's one of the normally unobtainable ones, called "- - -"

                    String rolledItem_name = ItemData.rollable_items_names[rolledItemID];
                    // if the result is "Special", replace it by the correct special item
                    if (rolledItem_name == "Special")
                    {
                        rolledItem_name = ItemData.item_names[CharData.specials_dict[get_selected_char_name(1)]];
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
                            rolledItemID = (int) ItemData.item_name_to_ID("Triple Reds");
                            rolledItem_name = ItemData.rollable_items_names[rolledItemID];
                        }
                    }
                }

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
    }
}
