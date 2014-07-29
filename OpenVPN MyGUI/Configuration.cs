using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace OpenVPN_MyGUI
{
    public partial class Configuration : Form
    {
        private Console console;
        private int defaultPort = 0;
        public Configuration(Console parent)
        {
            console = parent;
            InitializeComponent();
            
        }
      
        private void Configuration_Load(object sender, EventArgs e)
        {
            if (console.defaultPort != 0)
                defaultPort = console.defaultPort;
            textBox2.Text = defaultPort.ToString();
            if (console.oVPNCat != "")
                textBox1.Text = console.oVPNCat;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (File.Exists(console.mainDir + @"\cfg.txt"))
            {
                string[] cfgList = File.ReadAllLines(console.mainDir+@"\cfg.txt").ToArray();

                for (int i = 0; i < cfgList.Length; i++)
                {
                    string[] cmd = cfgList[i].Split(',');
                    if (cmd[0] == "openvpncat")
                        cfgList[i] = "openvpncat," + @textBox1.Text;
                    if (cmd[0] == "defaultport")
                        cfgList[i] = "defaultport," + @textBox2.Text;
                }
                console.cfgSave = cfgList;
                console.settingsChanged = true;
            }
        }

    }
    
}
