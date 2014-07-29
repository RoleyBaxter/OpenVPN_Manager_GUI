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
using System.Text.RegularExpressions;

namespace OpenVPN_MyGUI
{
    public partial class OVPN : Form
    {
        public RichTextBox vpnConsole;
        private Console console;
        public OVPN(Console parent)
        {
            InitializeComponent();
            console = parent;
            vpnConsole = this.richTextBox1;
            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(OVPN_KeyDown);
            this.richTextBox1.TextChanged += new EventHandler(richTextBox1_TextChanged);
            this.ActiveControl = textBox1;
            
            
        }

       

        void richTextBox1_TextChanged(Object sender, EventArgs e)
        {
                        
            string str = richTextBox1.Lines[richTextBox1.Lines.Length-2];

            if(str != null)
            
                if(str.Contains("Initialization Sequence Completed"))
                
                {
                    this.Visible = false;
                    console.ConnectedBool = true;
                    console.ConnectManager(console.defaultPort);
                   
                }


        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.WindowsShutDown) return;
            if (e.CloseReason == CloseReason.FormOwnerClosing) return;

            e.Cancel = true;
            this.Visible = false;
        }


        delegate void SetTextCallback(String Text);
        public void SetText(string txt)
        {
            if (this.vpnConsole.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { txt });
            }
            else
            {
                this.vpnConsole.AppendText(txt + "\r\n");
                this.vpnConsole.ScrollToCaret();
            }
        }
        void OVPN_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && textBox1.Focused == true)
            {
                
                e.Handled = true;
                e.SuppressKeyPress = true;
                
                string str = textBox1.Text;
                textBox1.Text = "";
                if (console.output != null)
                {
                    console.output.WriteLine(str);
                    console.output.Flush();
                }
            }
        }

       

        private void button2_Click(object sender, EventArgs e)
        {
            if(!console.p.HasExited)
            {
                console.p.Kill();
                richTextBox1.AppendText("\r\n" + "Connection Aborted / Disconnected" + "\r\n");
                this.Visible = false;
            }
           
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(console.p.HasExited)
                console.StartService();
        }
    }
   
}
