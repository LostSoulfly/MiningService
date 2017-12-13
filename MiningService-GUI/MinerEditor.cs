using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MiningService
{
    public partial class MinerEditor : Form
    {

        int listIndex;

        private Settings settings;
        private List<MinerList> tempCpu;
        private List<MinerList> tempGpu;


        public MinerEditor(Settings settings)
        {
            InitializeComponent();
            this.settings = settings;
            tempCpu = settings.cpuMiners;
            tempGpu = settings.gpuMiners;
        }

        private void Navigate(bool forward = false, bool backward = false, bool add = false, bool remove = false, bool update = true)
        {
            switch (comboMiners.SelectedIndex)
            {
                case 0: //cpu list
                    if (update)
                        UpdateMiner(tempCpu[listIndex]);

                    if (forward)
                    {
                        if (tempCpu.Count > this.listIndex + 1)
                            this.listIndex++;

                    }
                    else if (backward)
                    {
                        if (this.listIndex - 1 >= 0)
                            this.listIndex--;
                    }
                    else if (add)
                    {
                        tempCpu.Add(new MinerList("", "", "", true));
                        this.listIndex = tempCpu.Count - 1;
                    }
                    else if (remove)
                    {
                        tempCpu.RemoveAt(this.listIndex);
                        this.listIndex = tempCpu.Count - 1;
                    }
                    if (listIndex > -1)
                        PopulateMiner(tempCpu[listIndex]);
                    groupMiner.Text = String.Format("Miner {0} / {1}", listIndex + 1, tempCpu.Count);

                    break;

                case 1: //gpu list

                    if (update)
                        UpdateMiner(tempGpu[listIndex]);

                    if (forward)
                    {
                        if (tempGpu.Count > this.listIndex + 1)
                            this.listIndex++;

                    }
                    else if (backward)
                    {
                        if (this.listIndex - 1 >= 0)
                            this.listIndex--;
                    }
                    else if (add)
                    {
                        tempGpu.Add(new MinerList("", "", "", true));
                        this.listIndex = tempGpu.Count - 1;
                    }
                    else if (remove)
                    {
                        tempGpu.RemoveAt(this.listIndex);
                        this.listIndex = tempGpu.Count - 1;
                    }
                    if (listIndex > -1)
                        PopulateMiner(tempGpu[listIndex]);
                    groupMiner.Text = String.Format("Miner {0} / {1}", listIndex + 1, tempGpu.Count);

                    break;
            }

        }

        private void UpdateSettings()
        {
            settings.cpuMiners = new List<MinerList>();
            settings.gpuMiners = new List<MinerList>();

            settings.cpuMiners = tempCpu;
            settings.gpuMiners = tempGpu;

            //MessageBox.Show("Settings updated!");
        }

        private void PopulateMiner(MinerList miner)
        {
            textExecutable.Text = miner.executable;
            textActive.Text = miner.activeArguments;
            textIdleArgs.Text = miner.idleArguments;
            checkMineNotIdle.Checked = miner.mineWhileNotIdle;
            checkMinerDisabled.Checked = miner.minerDisabled;
        }

        private void UpdateMiner(MinerList miner)
        {
            miner.executable = textExecutable.Text;
            miner.activeArguments = textActive.Text;
            miner.idleArguments = textIdleArgs.Text;
            miner.mineWhileNotIdle = checkMineNotIdle.Checked;
            miner.minerDisabled = checkMinerDisabled.Checked;
        }

        private void MinerEditor_Load(object sender, EventArgs e)
        {
            comboMiners.SelectedIndex = 0;
            toolTip.SetToolTip(buttonAdd, "Adds a new Miner to the currently selected Miner list.");
            toolTip.SetToolTip(buttonBack, "Goes back one entry in the currently selected Miner list.");
            toolTip.SetToolTip(buttonForward, "Goes forward one entry in the currently selected Miner list.");
            toolTip.SetToolTip(buttonCancel, "Close this window and cancel all changes.");
            toolTip.SetToolTip(buttonSave, "Save all current changes. Closes the window!");
            toolTip.SetToolTip(comboMiners, "Choose which Miner list to view or edit the contents of.");
            toolTip.SetToolTip(checkMineNotIdle, "If checked, this miner will be enabled while a user is both idle OR inactive.");
            toolTip.SetToolTip(checkMinerDisabled, "If checked, this miner will not be used at all.");
            toolTip.SetToolTip(buttonOpen, "Open a File Dialog to select a Miner's executable location.");
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void comboMiners_SelectedIndexChanged(object sender, EventArgs e)
        {
            listIndex = 0;
            Navigate(backward: true, update: false);
        }

        private void buttonForward_Click(object sender, EventArgs e)
        {
            Navigate(forward: true);
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (comboMiners.SelectedIndex == 0)
                UpdateMiner(tempCpu[listIndex]);
            else
                UpdateMiner(tempGpu[listIndex]);

            UpdateSettings();
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            Navigate(backward: true);
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            Navigate(add: true, update: false);
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            Navigate(remove: true, update: false);
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "executables (*.exe)|*.exe|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (File.Exists(openFileDialog1.FileName))
                    {
                        textExecutable.Text = openFileDialog1.FileName;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not find file: " + ex.Message);
                }
            }
        }

        private void textExecutable_Enter(object sender, EventArgs e)
        {

        }

        private void textIdleArgs_Enter(object sender, EventArgs e)
        {

        }

        private void textActive_Enter(object sender, EventArgs e)
        {

        }

        private void textActive_MouseEnter(object sender, EventArgs e)
        {
            toolTip.Show("The arguments passed to the executable when mining while a user is active.", textActive, 5000);
        }

        private void textIdleArgs_MouseEnter(object sender, EventArgs e)
        {
            toolTip.Show("The arguments passed to the executable when mining while a user is idle.", textIdleArgs, 5000);
        }

        private void textExecutable_MouseEnter(object sender, EventArgs e)
        {
            toolTip.Show("The executable that the below arguments are passed to when launching.", textExecutable, 5000);
        }
    }
}
