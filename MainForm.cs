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

        public Bruteforcer bruteforcer = new Bruteforcer();

        private uint RNG;
        private int max_roll = 0;

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
            if (this.bruteforcer.active == 1)
            {
                this.UpdateStatusMSG = String.Format("Interrupted [!]");
                Console.WriteLine(this.UpdateStatusMSG);
                this.Invoke(this.updateStatusDelegate);
                this.Invoke(this.finishStatusDelegate);
                return;
            }
            this.bruteforcer.active = 1;
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

                foreach (BruteforceCondition cond in bruteforcer.Conditions)
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
                foreach (BruteforceCondition cond in bruteforcer.Conditions)
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

                    Hist_IDX = Hist_IDX + this.max_roll;
                    if (Hist_IDX >= HistoryDepth)
                        Hist_IDX -= HistoryDepth;
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
                    if (bruteforce_attempts > ItemData.MAX_RNG_Combinations + (ulong) this.max_roll)
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
            this.bruteforcer.active = 0;

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
            String[] row_content = new String[8];

            // Get ItemRolls for every Position
            for (int pos = 0; pos < 8; pos++)
            {
                int rolledItemID = CodeRandomness.calc_ItemRoll(RNG, ProbMatrix, pos, ColWeightVector[pos]);

                // defaulting to "Invalid" first
                String rolledItem_name = "Invalid Roll";
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
                if (rolledItem_name == "Triple Greens" && pos > 0)
                {
                    // advance RNG once more for this extra check (temporarily!!!)
                    // NOTE: once more means we need to advance it twice, because we only IMPLICITLY
                    //       advance within the RNG calc function now...
                    uint shellRNG = CodeRandomness.AdvanceRNG(this.RNG);
                    shellRNG = CodeRandomness.AdvanceRNG(shellRNG);

                    // 40% chance of converting to Triple Reds
                    if (0.4 < CodeRandomness.translate_RNG_to_Float(shellRNG))
                    {
                        rolledItem_name = "Triple Reds";
                        rolledItemID = (int)ItemData.item_name_to_ID(rolledItem_name);
                    }
                }

                // rolled items into the others
                row_content[pos] = rolledItem_name;
            }
            // return resulting content array
            return row_content;
        }

        public int get_Roll_ItemIDs(uint RNG, uint driver, int pos, uint reality)
        {
            return CodeRandomness.calc_ItemRoll(RNG, bruteforcer.get_prob_matrix(driver, reality), pos, bruteforcer.get_col_weight(driver, reality)[pos]);
        }
        public int[] get_RollsPerPos_ItemIDs(uint RNG, uint driver, uint reality)
        {
            // create row string array
            int[] row_content = new int[8];

            // Get ItemRolls for every Position
            for (int pos = 0; pos < 8; pos++) // NOTE - COMPLETELY disregarding pos #1 + #2 because its uninteresting
            {
                // row_content[pos] = CodeRandomness.calc_ItemRoll(RNG, ProbMatrix, pos, ColWeightVector[pos]);
                row_content[pos] = get_Roll_ItemIDs(RNG, driver, pos, reality);
            }
            // return resulting content array
            return row_content;
        }

        public MainForm()
        {
            InitializeComponent();
            // Im putting these defaults HERE because VSC doesnt want me to edit the auto-gen code...

            this.comboBox1.SelectedIndex = CharData.char_names.IndexOf("Baby Luigi");
            this.comboBox2.SelectedIndex = CharData.char_names.IndexOf("Bowser JR");

            // this forbids the user from changing grid content (and removes last row)
            this.dataGridView1.AllowUserToAddRows = false;

            this.bruteforcer.set_up_matrices(this.selected_driver_1, this.selected_driver_2);
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
            for (int pos = 1; pos <= 8; pos++)
            {
                dataGridView1.Columns.Add("UnusedName", string.Format("( {0} ) {1}", pos, this.selected_driver_1));
                dataGridView1.Columns.Add("UnusedName", string.Format("( {0} ) {1}", pos, this.selected_driver_2));
            }
            for (int col = 0; col < (1+8*2); col++)
                dataGridView1.Columns[col].Width = 110;
            // and sepperate the cols a bit for visibility
            for (int col = 0; col < (1+8*2); col += 2)
                this.dataGridView1.Columns[col].DividerWidth = 3;

            // some nice colors
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 192, 255, 192);
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.Columns[0].DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230, 230);

            // init RNG from TextField
            this.RNG = uint.Parse(textBox3.Text.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);

            // set up the probability matrices
            this.bruteforcer.set_up_matrices(this.selected_driver_1, this.selected_driver_2);

            // create row string array
            String[] row_content_1 = new String[8];
            String[] row_content_2 = new String[8];
            String[] row_content = new String[1+8*2];
            // and start simulating the RNG
            for (int i = 0; i < 10; i++)
            {
                for (uint reality = 0; reality < 6; reality++)
                {
                    string[] RNG_string = { RNG.ToString("X8") };
                    if (reality == 1) RNG_string[0] = "No Special";
                    if (reality == 2) RNG_string[0] = "No Blue";
                    if (reality == 3) RNG_string[0] = "No Star";
                    if (reality == 4) RNG_string[0] = "No Mushroom";
                    if (reality == 5) RNG_string[0] = "No TripShrooms";

                    // get row content from RNG simulation - character 1
                    row_content_1 = get_RollsPerPos_ItemNames(this.RNG, bruteforcer.get_prob_matrix(1, reality), bruteforcer.get_col_weight(1, reality), selected_driver_1);
                    // get row content from RNG simulation - character 2
                    row_content_2 = get_RollsPerPos_ItemNames(this.RNG, bruteforcer.get_prob_matrix(2, reality), bruteforcer.get_col_weight(2, reality), selected_driver_2);
                    // and build the full row
                    row_content = interweave_string_arrays(row_content_1, row_content_2);
                    row_content = concat_string_arrays(RNG_string, row_content);

                    this.dataGridView1.Rows.Add(row_content);
                    // check if there is a special cell color defined for the pull
                    for (int col = 1; col < (1+8*2); col++)
                    {
                        if (ItemData.ItemColor_Dict.ContainsKey(row_content[col]) == true)
                        {
                            Color cell_color = ItemData.ItemColor_Dict[row_content[col]];
                            this.dataGridView1.Rows[this.dataGridView1.Rows.Count - 1].Cells[col].Style.BackColor = cell_color;
                        }
                    }
                }
                this.dataGridView1.Rows[this.dataGridView1.Rows.Count - 6].Cells[0].Style.BackColor = Color.FromArgb(255, 255, 200, 150);

                // visually divide the different Pulls more
                this.dataGridView1.Rows[(i * 6) + 5].DividerHeight = 4;
                // dataGridView1.GridColor = Color.Black;

                // update the RNG
                RNG = CodeRandomness.AdvanceRNG(RNG);
            }
        }

        // controls the bruteforcing
        private void button2_Click(object sender, EventArgs e)
        {
            string error_code = this.bruteforcer.read_pattern_file("../../BruteForcePattern.xml");
            if (string.IsNullOrEmpty(error_code) == false)
            {
                Console.WriteLine(String.Format("Ruh-Oh! A Condition couldnt be parsed: ItemName={0}", error_code));
                this.textBox2.Text = String.Format("XMLR saw: {0}", error_code);
                bruteforcer.Conditions = new List<BruteforceCondition>();
                return;
            }

            // reset the grid contents
            dataGridView2.Rows.Clear();
            dataGridView2.Columns.Clear();
            // create column headers - Add(col_name, col_text);
            dataGridView2.Columns.Add("RNGSeed", string.Format("{0} Conditions collected from XML", bruteforcer.Conditions.Count));
            dataGridView2.Columns[0].Width = (dataGridView2.Width - 3);

            // briefly reorder the list of conditions by their likelyhood to increase
            // performance during bruteforcing (having less likely conditions up front
            // means the bruteforcer can determine a failure earlier)
            List<BruteforceCondition> sorted_condis = bruteforcer.Conditions.OrderBy(o => o.likelyness).ToList();
            bruteforcer.Conditions = sorted_condis;

            this.max_roll = 0;
            foreach (BruteforceCondition cond in bruteforcer.Conditions)
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
            this.bruteforcer.set_up_matrices(this.selected_driver_1, this.selected_driver_2);
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
