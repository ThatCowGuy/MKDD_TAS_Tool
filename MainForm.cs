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
        public void insert_row_in_matrix(uint[,] matrix, uint index, uint[] row)
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

        private uint[,] ItemProbMatrix_char1_NoShock = new uint[10, 8];
        private uint[,] ItemProbMatrix_char2_NoShock = new uint[10, 8];
        private uint[] total_column_weight_1_NoShock = new uint[8];
        private uint[] total_column_weight_2_NoShock = new uint[8];

        private uint[,] ItemProbMatrix_char1_NoShock_NoSpecial = new uint[10, 8];
        private uint[,] ItemProbMatrix_char2_NoShock_NoSpecial = new uint[10, 8];
        private uint[] total_column_weight_1_NoShock_NoSpecial = new uint[8];
        private uint[] total_column_weight_2_NoShock_NoSpecial = new uint[8];

        private uint[,] ItemProbMatrix_char1_NoShock_NoBlue = new uint[10, 8];
        private uint[,] ItemProbMatrix_char2_NoShock_NoBlue = new uint[10, 8];
        private uint[] total_column_weight_1_NoShock_NoBlue = new uint[8];
        private uint[] total_column_weight_2_NoShock_NoBlue = new uint[8];

        private uint[,] ItemProbMatrix_char1_NoShock_NoStar = new uint[10, 8];
        private uint[,] ItemProbMatrix_char2_NoShock_NoStar = new uint[10, 8];
        private uint[] total_column_weight_1_NoShock_NoStar = new uint[8];
        private uint[] total_column_weight_2_NoShock_NoStar = new uint[8];

        private uint[,][,] ItemProbMatrix_List;
        private uint[,][,] TotalColWeight_List;

        private uint RNG;
        private int max_roll = 0;
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
            int[,,,] history_matrix = new int[2, 5, 4, HistoryDepth]; // Driver, Position, Reality, HistDepth
            uint[] RNG_history = new uint[HistoryDepth];
            for (int step = 0; step < HistoryDepth; step++)
            {
                RNG_history[step] = this.RNG;
            }

            // create row string array
            int[] row_content_1 = new int[5];
            int[] row_content_2 = new int[5];
            uint bruteforce_attempts = 0;

            // I can use this here to iterate through the History matrices, instead
            // of shifting the matrix contents on every sim. Should save some time.
            int Hist_IDX = (HistoryDepth - 1);

            long sim_start = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            while (true)
            {
                if (Hist_IDX < 0) Hist_IDX = (HistoryDepth - 1);

                for (uint reality = 0; reality < 4; reality++)
                {
                    // get row content from RNG simulation
                    row_content_1 = get_RollsPerPos_ItemIDs(this.RNG, get_prob_matrix(1, reality), get_col_weight(1, reality), this.selected_driver_1);
                    row_content_2 = get_RollsPerPos_ItemIDs(this.RNG, get_prob_matrix(2, reality), get_col_weight(2, reality), this.selected_driver_2);

                    for (int pos = 0; pos < 5; pos++)
                    {
                        history_matrix[0, pos, reality, Hist_IDX] = row_content_1[pos];
                        history_matrix[1, pos, reality, Hist_IDX] = row_content_2[pos];
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
                    if (bruteforce_attempts < HistoryDepth)
                        Hist_IDX += this.max_roll;
                    this.RNG = RNG_history[Hist_IDX];

                    Console.WriteLine(String.Format("MATCH: RNG = {0:X8}", this.RNG));
                    this.Invoke(this.finishStatusDelegate);
                    break;
                }
                if (bruteforce_attempts % 100000 == 0)
                {
                    long sim_time = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    Console.WriteLine(String.Format("TIME: {0:0.00} s", (sim_time - sim_start) / 1000.0));

                    double percentage = (100.0 * bruteforce_attempts) / ItemData.MAX_RNG_Combinations;
                    Console.WriteLine(String.Format("{0:n0} Attempts ({1:0.00}%) cur_RNG: 0x{2:X8}\n", bruteforce_attempts, percentage, this.RNG));

                    this.UpdateStatusMSG = String.Format("{0:n0} ({1:0.00}%)", bruteforce_attempts, percentage);
                    this.Invoke(this.updateStatusDelegate);
                }
                bruteforce_attempts++;

                /*
                // step through the history matrix and shift everything up by 1 row to update it
                for (int step = 0; step < (HistoryDepth - 1); step++)
                {
                    RNG_history[step] = RNG_history[(step + 1)];

                    for (uint reality = 0; reality < 4; reality++)
                    {
                        for (int pos = 0; pos < 4; pos++)
                        {
                            history_matrix[0, pos, reality, step] = history_matrix[0, pos, reality, (step + 1)];
                            history_matrix[1, pos, reality, step] = history_matrix[1, pos, reality, (step + 1)];
                        }
                    }
                }
                */

                // update the RNG
                RNG = CodeRandomness.AdvanceRNG(RNG);

                Hist_IDX -= 1;
            }
        }
        private void UpdateStatus()
        {
            this.textBox2.Text = this.UpdateStatusMSG;
            this.textBox2.BackColor = System.Drawing.Color.White;
            this.textBox2.Update();
            this.textBox1.Text = "Searching...";
            this.textBox1.BackColor = System.Drawing.Color.White;
            this.textBox1.Update();
        }
        private void FinishStatus()
        {
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

        public String[] get_RollsPerPos_ItemNames(uint RNG, uint[,] ProbMatrix, uint[] ColWeightVector, String driver_name)
        {
            // create row string array
            String[] row_content = new String[5];

            // Get ItemRolls for every Position
            for (int pos = 0; pos < 5; pos++) // NOTE - skipping pos #1 because its uninteresting
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
                row_content[pos] = rolledItem_name;
            }
            // return resulting content array
            return row_content;
        }
        public int[] get_RollsPerPos_ItemIDs(uint RNG, uint[,] ProbMatrix, uint[] ColWeightVector, String driver_name)
        {
            // create row string array
            int[] row_content = new int[5];

            // Get ItemRolls for every Position
            for (int pos = 2; pos < 5; pos++) // NOTE - COMPLETELY disregarding pos #1 + #2 because its uninteresting
            {
                row_content[pos] = CodeRandomness.calc_ItemRoll(RNG, ProbMatrix, pos, ColWeightVector[pos]);
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

        string[] concat_string_arrays(string[] A, string[] B)
        {
            string[] C = new string[A.Length + B.Length];
            A.CopyTo(C, 0);
            B.CopyTo(C, A.Length);
            return C;
        }
        string[] interweave_string_arrays(string[] A, string[] B)
        {
            if (A.Length != B.Length) return null;

            string[] C = new string[2 * A.Length];
            for (int i = 0; i < A.Length; i++)
            {
                C[(2 * i) + 0] = A[i];
                C[(2 * i) + 1] = B[i];
            }
            return C;
        }

        // this button controls the plain RNG-Simulation
        private void button1_Click(object sender, EventArgs e)
        {
            // reset the grid contents
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            // create column headers - Add(col_name, col_text);
            dataGridView1.Columns.Add("RNGSeed", "RNG Value");
            // NOTE - I WANT this to repeat
            for (int pos = 1; pos < 6; pos++)
            {
                dataGridView1.Columns.Add("UnusedName", "(D-1) Pos " + pos.ToString());
                dataGridView1.Columns.Add("UnusedName", "(D-2) Pos " + pos.ToString());
            }
            for (int col = 0; col < 11; col++)
                dataGridView1.Columns[col].Width = 90;
            // and sepperate the cols a bit for visibility
            this.dataGridView1.Columns[0].DividerWidth = 3;
            this.dataGridView1.Columns[2].DividerWidth = 3;
            this.dataGridView1.Columns[4].DividerWidth = 3;
            this.dataGridView1.Columns[6].DividerWidth = 3;
            this.dataGridView1.Columns[8].DividerWidth = 3;
            this.dataGridView1.Columns[10].DividerWidth = 3;

            // some nice colors
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 170, 170, 170);
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.Columns[0].DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230, 230);

            // init RNG from TextField
            this.RNG = uint.Parse(textBox3.Text.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);

            // set up the probability matrices
            this.set_up_matrices();

            // create row string array
            String[] row_content_1 = new String[5];
            String[] row_content_2 = new String[5];
            String[] row_content = new String[11];
            // and start simulating the RNG
            for (int i = 0; i < 10; i++)
            {
                for (uint reality = 0; reality < 4; reality++)
                {
                    string[] RNG_string = { RNG.ToString("X8") };
                    if (reality == 1) RNG_string[0] = "No Special";
                    if (reality == 2) RNG_string[0] = "No Blue";
                    if (reality == 3) RNG_string[0] = "No Star";

                    // get row content from RNG simulation - character 1
                    row_content_1 = get_RollsPerPos_ItemNames(this.RNG, get_prob_matrix(1, reality), get_col_weight(1, reality), selected_driver_1);
                    // get row content from RNG simulation - character 2
                    row_content_2 = get_RollsPerPos_ItemNames(this.RNG, get_prob_matrix(2, reality), get_col_weight(2, reality), selected_driver_2);
                    // and build the full row
                    row_content = interweave_string_arrays(row_content_1, row_content_2);
                    row_content = concat_string_arrays(RNG_string, row_content);

                    this.dataGridView1.Rows.Add(row_content);
                    // check if there is a special cell color defined for the pull
                    for (int col = 1; col < 11; col++)
                    {
                        if (ItemData.ItemColor_Dict.ContainsKey(row_content[col]) == true)
                        {
                            Color cell_color = ItemData.ItemColor_Dict[row_content[col]];
                            this.dataGridView1.Rows[this.dataGridView1.Rows.Count - 1].Cells[col].Style.BackColor = cell_color;
                        }
                    }
                }
                this.dataGridView1.Rows[this.dataGridView1.Rows.Count - 4].Cells[0].Style.BackColor = Color.FromArgb(255, 255, 200, 150);

                // visually divide the different Pulls more
                this.dataGridView1.Rows[(i * 4) + 3].DividerHeight = 4;
                // dataGridView1.GridColor = Color.Black;

                // update the RNG
                RNG = CodeRandomness.AdvanceRNG(RNG);
            }
        }

        // controls the bruteforcing
        private void button2_Click(object sender, EventArgs e)
        {
            // create a new, empty list of bruteforcer conditions
            this.Conditions = new List<BruteforceCondition>();
            uint xml_driver_id = 0; // 0 = dont care, 1 = 1, 2 = 2
            uint xml_pos = 0;
            uint xml_reality = 0;
            uint xml_roll = 0;
            string xml_item_name = "";

            // open the pattern XML and readout the conditions
            XmlReader PatternFile = XmlReader.Create("../../BruteForcePattern.xml");
            
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

            // reset the grid contents
            dataGridView2.Rows.Clear();
            dataGridView2.Columns.Clear();
            // create column headers - Add(col_name, col_text);
            dataGridView2.Columns.Add("RNGSeed", "Collected Conditions =");
            dataGridView2.Columns[0].Width = (dataGridView2.Width - 3);

            this.max_roll = 0;
            foreach (BruteforceCondition cond in Conditions)
            {
                string condition_msg = String.Format("{0} at #{1} on Roll-{2} For Driver #{3} in Reality #{4}",
                    ItemData.rollable_items_names[cond.item_id], cond.pos, cond.roll, cond.driver_id, cond.reality);

                Console.WriteLine(condition_msg);
                dataGridView2.Rows.Add(condition_msg);

                if (cond.roll > max_roll) max_roll = (int) cond.roll;
            }
            // some nice colors
            dataGridView2.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 192, 192, 255);
            dataGridView2.EnableHeadersVisualStyles = false;
            // this removes the auto select
            dataGridView2.Enabled = false;
            dataGridView2.ClearSelection();
            // this forbids the user from changing grid content (and removes last row)
            this.dataGridView1.AllowUserToAddRows = false;

            // init RNG from TextField
            this.RNG = uint.Parse(textBox3.Text.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
            // set up the probability matrices
            this.set_up_matrices();
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

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void panel1_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {

        }
        // I know this looks very painful and stupid, but its quicker for a bruteforcer to have these matrices
        // available like this (although maybe there is a neater way of filling them...)
        public void set_up_matrices()
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
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock, 9, ItemData.item_weights[CharData.specials_dict[this.selected_driver_1]]);
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
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock, 9, ItemData.item_weights[CharData.specials_dict[this.selected_driver_2]]);
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
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoBlue, 9, ItemData.item_weights[CharData.specials_dict[this.selected_driver_1]]);
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
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoBlue, 9, ItemData.item_weights[CharData.specials_dict[this.selected_driver_2]]);
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
            insert_row_in_matrix(this.ItemProbMatrix_char1_NoShock_NoStar, 9, ItemData.item_weights[CharData.specials_dict[this.selected_driver_1]]);
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
            insert_row_in_matrix(this.ItemProbMatrix_char2_NoShock_NoStar, 9, ItemData.item_weights[CharData.specials_dict[this.selected_driver_2]]);
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
        }

        public uint[,] get_prob_matrix(uint driver, uint reality)
        {
            if (driver == 1)
            {
                if (reality == 0) return this.ItemProbMatrix_char1_NoShock;
                if (reality == 1) return this.ItemProbMatrix_char1_NoShock_NoSpecial;
                if (reality == 2) return this.ItemProbMatrix_char1_NoShock_NoBlue;
                if (reality == 3) return this.ItemProbMatrix_char1_NoShock_NoStar;
            }
            if (driver == 2)
            {
                if (reality == 0) return this.ItemProbMatrix_char2_NoShock;
                if (reality == 1) return this.ItemProbMatrix_char2_NoShock_NoSpecial;
                if (reality == 2) return this.ItemProbMatrix_char2_NoShock_NoBlue;
                if (reality == 3) return this.ItemProbMatrix_char2_NoShock_NoStar;
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
            }
            if (driver == 2)
            {
                if (reality == 0) return this.total_column_weight_2_NoShock;
                if (reality == 1) return this.total_column_weight_2_NoShock_NoSpecial;
                if (reality == 2) return this.total_column_weight_2_NoShock_NoBlue;
                if (reality == 3) return this.total_column_weight_2_NoShock_NoStar;
            }
            return null;
        }

        private void button3_MouseClick(object sender, MouseEventArgs e)
        {
            // generate a random number and put it as initial seed (generally helps bruteforcing)
            Random generator = new Random();
            this.RNG = (uint)(generator.NextDouble() * 0x100000000);
            this.textBox3.Text = String.Format("{0:X}", this.RNG);

            // force button1 to be clicked
            this.button1_Click(null, EventArgs.Empty);
        }

        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
