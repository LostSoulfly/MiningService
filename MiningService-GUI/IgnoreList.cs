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
    public partial class IgnoreList : Form
    {

        private Settings settings;

        public IgnoreList(Settings settings)
        {
            InitializeComponent();
            this.settings = settings;
        }

        private void LoadIgnoreList()
        {
            listIgnore.Items.Clear();
            for (int i = 0; i < settings.ignoredFullscreenApps.Count; i++)
            {
                listIgnore.Items.Add(settings.ignoredFullscreenApps[i]);
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

        private void IgnoreList_Load(object sender, EventArgs e)
        {
            LoadIgnoreList();
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

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            string app = string.Empty;
            Utilities.ShowInputDialog(ref app, "EXE Name?");
            app = Path.GetFileNameWithoutExtension(app);
            if (app.Length > 1)
                listIgnore.Items.Add(app);

        }

    }
}
