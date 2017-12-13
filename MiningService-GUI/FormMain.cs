using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MiningService;

namespace MiningService
{
    public partial class FormMain : Form
    {
        private bool changesMade;
        private string settingsFileName = "MinerService.json";
        private Settings settings = new Settings();
        private List<TextBox> formTextBoxes = new List<TextBox>();
        private List<Label> textBoxLabels = new List<Label>();
        private List<NumericUpDown> formNumericUpDown = new List<NumericUpDown>();
        private List<Label> numericUpDownLabels = new List<Label>();

        public FormMain()
        {
            InitializeComponent();
        }

        public void LoadSettings()
        {
            settings = new Settings();
            saveToolStripMenuItem.Enabled = true;

            try
            {
                if (File.Exists(settingsFileName))
                {
                    //Try to read and deserialize the passed file path into the Settings object
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFileName));
                }
                CreateFormObjects(settings);
            }
            catch (Exception ex)
            {
                MessageBox.Show("LoadSettings exception: " + ex.Message);
            }

            changesMade = false;
        }

        public void SaveSettings()
        {
            UpdateSettings(settings);

            try
            {
                File.WriteAllText(settingsFileName, JsonConvert.SerializeObject(settings, Formatting.Indented));
                changesMade = false;
                MessageBox.Show("Settings were saved succsssfully!", "MiningService GUI");
            }
            catch (Exception ex)
            {
                MessageBox.Show("SaveSettings exception: " + ex.Message);
            }
        }

        private void UpdateSettings(Settings passedSettings)
        {
            List<TextBox> textBoxes = new List<TextBox>();
            List<NumericUpDown> numericBoxes = new List<NumericUpDown>();

            foreach (Control c in this.Controls)
            {
                if (c.GetType() == typeof(TextBox))
                    textBoxes.Add((TextBox)c);

                if (c.GetType() == typeof(NumericUpDown))
                    numericBoxes.Add((NumericUpDown)c);
            }

            foreach (TextBox textBox in textBoxes)
            {
                PropertyInfo propertyInfo = passedSettings.GetType().GetProperty(textBox.Name);
                propertyInfo.SetValue(passedSettings, Convert.ChangeType(textBox.Text, propertyInfo.PropertyType), null);
            }

            foreach (NumericUpDown numeric in numericBoxes)
            {
                PropertyInfo propertyInfo = passedSettings.GetType().GetProperty(numeric.Name);
                propertyInfo.SetValue(passedSettings, Convert.ChangeType(numeric.Value, propertyInfo.PropertyType), null);
            }

            for (int i = 0; i < checkedListSettings.Items.Count; i++)
            {
                PropertyInfo propertyInfo = passedSettings.GetType().GetProperty(checkedListSettings.Items[i].ToString());
                propertyInfo.SetValue(passedSettings, checkedListSettings.GetItemChecked(i));
            }
        }

        private void PopulateCheckedListBox(Settings passedSettings, CheckedListBox checkedListBox)
        {
            checkedListBox.Items.Clear();

            foreach (var prop in passedSettings.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(bool))
                    checkedListBox.Items.Add(prop.Name, (bool)prop.GetValue(passedSettings, null));
            }

            checkedListBox.Visible = true;
        }

        private void CreateFormObjects(Settings passedSettings)
        {
            //Delete any objects we've created before
            DeleteFormObjects();

            //Read settings variables and load them into CheckedListBox
            PopulateCheckedListBox(passedSettings, checkedListSettings);

            int textBoxLabelTop = menu.Height + 5;
            int numericLabelTop = checkedListSettings.Height + menu.Height + 10;
            foreach (var prop in passedSettings.GetType().GetProperties())
            {
                if (prop.PropertyType == typeof(string))
                {
                    TextBox textBox = new TextBox();
                    textBox.Name = prop.Name;
                    textBox.Text = prop.GetValue(settings, null) as string;
                    Label label = new Label();
                    label.Name = prop.Name;
                    label.Text = prop.Name;
                    label.AutoSize = true;

                    label.Top = textBoxLabelTop;
                    label.Left = checkedListSettings.Width + 15;
                    textBox.Top = label.Top + label.Height - 5;
                    textBox.Left = label.Left + 2;
                    textBox.Width = (int)(this.Width / 2.2);

                    textBox.TextChanged += ItemWasUpdated();

                    textBoxLabelTop = textBox.Top + textBox.Height + 5;

                    formTextBoxes.Add(textBox);
                    textBoxLabels.Add(label);
                    this.Controls.Add(textBox);
                    this.Controls.Add(label);
                }

                if (prop.PropertyType == typeof(int))
                {
                    NumericUpDown numericBox = new NumericUpDown();
                    numericBox.Name = prop.Name;
                    Label label = new Label();
                    label.Name = prop.Name;
                    label.Text = prop.Name;
                    label.AutoSize = true;
                    numericBox.Width = 100;

                    PropertyDescriptor property = TypeDescriptor.GetProperties(prop)[numericBox.Name];
                    int test = (int)prop.GetValue(settings);
                    numericBox.Value = test;

                    numericBox.ValueChanged += ItemWasUpdated();

                    label.Top = numericLabelTop;
                    label.Left = checkedListSettings.Left - 2;
                    numericBox.Top = label.Top + label.Height - 10;
                    numericBox.Left = label.Left + 2;

                    numericLabelTop = numericBox.Top + numericBox.Height + 5;

                    formNumericUpDown.Add(numericBox);
                    textBoxLabels.Add(label);
                    this.Controls.Add(numericBox);
                    this.Controls.Add(label);
                }

                //Let the form resize itself to match the new properties.
                this.AutoSize = true;
            }
        }

        private void DeleteFormObjects()
        {
            foreach (var item in formTextBoxes)
            {
                this.Controls.Remove(item);
            }

            foreach (var item in formNumericUpDown)
            {
                this.Controls.Remove(item);
            }

            foreach (var item in textBoxLabels)
            {
                this.Controls.Remove(item);
            }

            foreach (var item in numericUpDownLabels)
            {
                this.Controls.Remove(item);
            }

            formTextBoxes = new List<TextBox>();
            formNumericUpDown = new List<NumericUpDown>();
            numericUpDownLabels = new List<Label>();
            textBoxLabels = new List<Label>();

            checkedListSettings.Visible = false;
        }

        private void SaveOnExit()
        {
            if (changesMade)
            {
                if (MessageBox.Show("You have unsaved changes. Would you like to save them now?", "Unsaved Changes", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    SaveSettings();
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void formMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (changesMade)
                SaveOnExit();
        }

        private void ItemWasUpdated(object sender, ItemCheckEventArgs e)
        {
            changesMade = true;
        }

        private EventHandler ItemWasUpdated()
        {
            changesMade = true;
            return null;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DeleteFormObjects();
        }

        private void minerConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MinerEditor editor = new MinerEditor(settings);
            if (editor.ShowDialog() == DialogResult.Yes)
                changesMade = true;

        }

        private void ignoredProgramsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IgnoreList ignore = new IgnoreList(settings);
            if (ignore.ShowDialog() == DialogResult.Yes)
                changesMade = true;
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!File.Exists("MiningService.exe"))
            {
                MessageBox.Show("MiningService was not found in this directory.", "Error", MessageBoxButtons.OK);
            }

            Process.Start("MiningService.exe");
        }

        private void startServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!File.Exists("MiningService.exe"))
            {
                MessageBox.Show("MiningService was not found in this directory.", "Error", MessageBoxButtons.OK);
            }

            Process.Start("MiningService.exe", "start");
        }

        private void stopServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!File.Exists("MiningService.exe"))
            {
                MessageBox.Show("MiningService was not found in this directory.", "Error", MessageBoxButtons.OK);
            }

            Process.Start("MiningService.exe", "stop");
        }

        private void uninstallServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!File.Exists("MiningService.exe"))
            {
                MessageBox.Show("MiningService was not found in this directory.", "Error", MessageBoxButtons.OK);
            }

            Process.Start("MiningService.exe", "uninstall");
        }

        private void installServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!File.Exists("MiningService.exe"))
            {
                MessageBox.Show("MiningService was not found in this directory.", "Error", MessageBoxButtons.OK);
            }

            Process.Start("MiningService.exe", "install");
        }
    }
}