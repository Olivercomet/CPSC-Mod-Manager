using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CPSC_Mod_Manager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            Icon = Properties.Resources.favicon;
            InitializeComponent();
        }

        public string configPath = "";
        public Dictionary<string,bool> enableds_from_config = new Dictionary<string,bool>();

        public string htDocsPath = "";
        public string backupPath = "";

        public List<mod> mods = new List<mod>();
        public class mod {
            public mod(string newPath, string newName, bool newEnabled) {
                path = newPath;
                displayname = newName;
                enabled = newEnabled;
                }
            public string path = "path";
            public string displayname = "defaultName";
            public bool enabled = false;

            public string InternalName = "";
            public string Author = "";
            public string ModVersion = "";
            public string ModDescription = "";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Path.GetFileName(Path.GetDirectoryName(Application.ExecutablePath)) != "mods")
                {
                MessageBox.Show("This application should only be run from the /mods/ directory.", "Wrong directory", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
                return;
                }

                DirectoryInfo modDir = new DirectoryInfo(Path.GetDirectoryName(Application.ExecutablePath));

                checkedListBox1.Items.Clear();

                configPath = Path.Combine(modDir.FullName, "mod_config.ini");
                htDocsPath = Path.GetDirectoryName(modDir.FullName);

                backupPath = Path.Combine(htDocsPath, "vanilla_backups");
                
                if (!Directory.Exists(backupPath)) 
                    {
                    Directory.CreateDirectory(backupPath);
                    }

                if (File.Exists(configPath))
                    {
                    LoadConfig();
                    }

                foreach (DirectoryInfo dirInfo in modDir.GetDirectories())
                    {
                    if (Path.GetFileName(dirInfo.FullName) == "src")
                        {
                        continue;
                        }

                    mod newMod = new mod(dirInfo.FullName, Path.GetFileNameWithoutExtension(dirInfo.FullName), false);
                    
                    checkedListBox1.Items.Add(newMod.displayname);

                    string modInfoPath = Path.Combine(dirInfo.FullName, "modinfo.txt");

                    if (File.Exists(modInfoPath))    //if modinfo.txt exists in this mod's directory
                        {
                        string[] modinfo = File.ReadAllLines(modInfoPath);

                        //read modinfo

                        foreach (string line in modinfo)
                            {
                            string[] splitline = line.Split('=');
                            
                            if (splitline.Length == 1)  //if there wasn't a value after the equals sign
                                {
                                continue;
                                }

                            switch (splitline[0])   //read the key and use the value accordingly
                                {
                                case "name":
                                    newMod.InternalName = splitline[1];
                                    break;
                                case "author":
                                    newMod.Author = splitline[1];
                                    break;
                                case "version":
                                    newMod.ModVersion = splitline[1];
                                    break;
                                case "description":
                                    newMod.ModDescription = splitline[1].Replace("<newline>", "\n");
                                    break;
                                }
                            }
                        }
                    else
                        {
                        File.Create(Path.Combine(dirInfo.FullName, "modinfo.txt")); //modinfo.txt didn't exist for this mod, create a new modinfo.txt
                        }

                    if (enableds_from_config.ContainsKey(newMod.displayname))   //if this mod is enabled in mod_config.ini, tick its box in the mod manager
                        {
                        if (enableds_from_config[newMod.displayname])
                            {
                            checkedListBox1.SetItemChecked(checkedListBox1.Items.IndexOf(newMod.displayname),true);
                            }
                        }

                    mods.Add(newMod);
                    }
        }

        public void LoadConfig() {
            enableds_from_config = new Dictionary<string, bool>();

            string[] config = File.ReadAllLines(configPath);
        
            for (int i = 0; i < config.Length; i++)
                {
                if (!config[i].Contains(","))
                    {
                    continue;
                    }

                string[] splitString = config[i].Split(',');

                bool enabled = false;
                if (splitString[1] == "true")
                    {
                    enabled = true;
                    }
                enableds_from_config.Add(splitString[0], enabled);
                }
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            string newconfig = "";

            for (int i = 0; i < mods.Count; i++)
                {
                if (mods[i].displayname == (string)checkedListBox1.Items[e.Index])
                    {
                    if (e.NewValue == CheckState.Checked)
                        {
                        mods[i].enabled = true;
                        EnableModFiles(mods[i].path);
                        }
                    else
                        {
                        mods[i].enabled = false;
                        RestoreVanillaFiles(mods[i].path);
                        }
                    }
                string enabled = "false";
                if (mods[i].enabled)
                    {
                    enabled = "true";
                    }
                newconfig += mods[i].displayname + "," + enabled + "\n";
                }

            File.WriteAllText(configPath,newconfig);
        }

        public void EnableModFiles(string modPath)
            {
            DirectoryInfo dirInfo = new DirectoryInfo(modPath);
        
            foreach (DirectoryInfo d in dirInfo.GetDirectories())
                {
                EnableDir(d, modPath);
                }
            }

        public void EnableDir(DirectoryInfo d,string root) {
            foreach (DirectoryInfo subDir in d.GetDirectories())
                {
                EnableDir(subDir,root);
                }
            CopyVanillaToBackupIfNotAlreadyThere(d,root);
            CopyModFilesToGame(d,root);
        }

        public void CopyVanillaToBackupIfNotAlreadyThere(DirectoryInfo d, string root) { 
        
        foreach (FileInfo f in d.GetFiles())
            {
                if (Path.GetFileName(f.FullName) == "modinfo.txt")
                {
                    continue;
                }

                string relativePath = f.FullName.Replace(root, "");

                if (File.Exists(htDocsPath + relativePath))
                {
                    if (!File.Exists(backupPath + relativePath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(backupPath + relativePath));
                        File.Copy(htDocsPath + relativePath, backupPath + relativePath);
                    }
                }
            }
        }


        public void CopyModFilesToGame(DirectoryInfo d, string root) {

            foreach (FileInfo f in d.GetFiles())
            {
                if (Path.GetFileName(f.FullName) == "modinfo.txt")
                    {
                    continue;
                    }

                string relativePath = f.FullName.Replace(root, "");

                if (File.Exists(htDocsPath + relativePath))
                    {
                    File.Delete(htDocsPath + relativePath);
                    }

                File.Copy(f.FullName, htDocsPath + relativePath);
            }
        }


        public void RestoreVanillaFiles(string modPath)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(modPath);

            foreach (DirectoryInfo d in dirInfo.GetDirectories())
                {
                RestoreDir(d, modPath);
                }
        }


        public void RestoreDir(DirectoryInfo d, string root)
        {
            foreach (DirectoryInfo subDir in d.GetDirectories())
                {
                RestoreDir(subDir, root);
                }
            RestoreGameFromBackup(d, root);
        }


        public void RestoreGameFromBackup(DirectoryInfo d, string root)
        {
            foreach (FileInfo f in d.GetFiles())
            {
                if (Path.GetFileName(f.FullName) == "modinfo.txt")
                {
                    continue;
                }

                string relativePath = f.FullName.Replace(root, "");
                if (File.Exists(htDocsPath + relativePath))
                    {
                    File.Delete(htDocsPath + relativePath);
                    }

                if (File.Exists(backupPath + relativePath))
                    {
                    File.Copy(backupPath + relativePath, htDocsPath + relativePath);
                    }
            }
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (checkedListBox1.SelectedIndex == -1)
                {
                return;
                }

            ModNameBox.Enabled = true;
            ModAuthorBox.Enabled = true;
            ModVersionBox.Enabled = true;
            ModDescriptionBox.Enabled = true;

            ModNameBox.Text = mods[checkedListBox1.SelectedIndex].InternalName;
            ModAuthorBox.Text = mods[checkedListBox1.SelectedIndex].Author;
            ModVersionBox.Text = mods[checkedListBox1.SelectedIndex].ModVersion;
            ModDescriptionBox.Text = mods[checkedListBox1.SelectedIndex].ModDescription.Replace("<newline>","\n");
        }


        public void WriteNewModInfoTxt(mod selectedMod)
        {
            string txt = "";

            txt += "name=" + selectedMod.InternalName+"\n";
            txt += "author=" + selectedMod.Author + "\n";
            txt += "version=" + selectedMod.ModVersion + "\n";
            txt += "description=" + selectedMod.ModDescription + "\n";

            File.WriteAllText(Path.Combine(selectedMod.path, "modinfo.txt"), txt);
        }

        private void ModNameBox_TextChanged(object sender, EventArgs e)
        {
            mods[checkedListBox1.SelectedIndex].InternalName = ModNameBox.Text.Replace("\n", "").Replace("\r", "");
            WriteNewModInfoTxt(mods[checkedListBox1.SelectedIndex]);
        }

        private void ModAuthorBox_TextChanged(object sender, EventArgs e)
        {
            mods[checkedListBox1.SelectedIndex].Author = ModAuthorBox.Text.Replace("\n", "").Replace("\r", "");
            WriteNewModInfoTxt(mods[checkedListBox1.SelectedIndex]);
        }

        private void ModVersionBox_TextChanged(object sender, EventArgs e)
        {
            mods[checkedListBox1.SelectedIndex].ModVersion = ModVersionBox.Text.Replace("\n", "").Replace("\r", "");
            WriteNewModInfoTxt(mods[checkedListBox1.SelectedIndex]);
        }

        private void ModDescriptionBox_TextChanged(object sender, EventArgs e)
        {
            mods[checkedListBox1.SelectedIndex].ModDescription = ModDescriptionBox.Text.Replace("\n", "<newline>");
            WriteNewModInfoTxt(mods[checkedListBox1.SelectedIndex]);
        }

        private void OpenDir_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", @Path.GetDirectoryName(Application.ExecutablePath));
        }
    }
}
