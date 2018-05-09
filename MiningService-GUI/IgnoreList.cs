using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MiningService
{
    public partial class IgnoreList : Form
    {
        private Settings settings;

        public IgnoreList(Settings settings)
        {
            InitializeComponent();
            this.settings = settings;
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            string app = string.Empty;
            Utilities.ShowInputDialog(ref app, "EXE Name?");
            app = Path.GetFileNameWithoutExtension(app);
            if (app.Length > 1)
                listIgnore.Items.Add(app);
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            UpdateSettings();
            this.Close();
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            foreach (string s in listIgnore.SelectedItems.OfType<string>().ToList())
                listIgnore.Items.Remove(s);
        }

        private void IgnoreList_Load(object sender, EventArgs e)
        {
            LoadIgnoreList();
        }

        private void LoadIgnoreList()
        {
            listIgnore.Items.Clear();
            for (int i = 0; i < settings.ignoredFullscreenApps.Count; i++)
            {
                listIgnore.Items.Add(settings.ignoredFullscreenApps[i]);
            }

            if (listIgnore.Items.Count == 0)
                if (MessageBox.Show("The Ignore list is empty. Would you like to load defaults?", "Load default ignored programs", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    listIgnore.Items.Add("explorer");
                    listIgnore.Items.Add("LockApp");
                    listIgnore.Items.Add("mstsc");
                }
        }

        private void UpdateSettings()
        {
            settings.ignoredFullscreenApps.Clear();
            foreach (var item in listIgnore.Items)
            {
                settings.ignoredFullscreenApps.Add(item as string);
            }
        }
    }
}