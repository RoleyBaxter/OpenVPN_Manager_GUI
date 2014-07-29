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
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using NetFwTypeLib;

namespace OpenVPN_MyGUI
{
    public partial class Console : Form
    {
        public string mainDir = Application.StartupPath;
        public int defaultPort = 0;
        public string oVPNCat = "";
        Configuration config;
        OVPN oVPNWin;
        public Process p;
        public StreamWriter output;
        public RichTextBox consoleTB;
        private bool vpnConnected = false;
        private Label connectedLabel;
        private Label ipLabel;
        public List<GuardRule> guardRules;
        public INetFwAuthorizedApplications defaultFWRules;
        public bool settingsChanged = false;
        public string[] cfgSave;

        public Console()
        {
            InitializeComponent();
            config = new Configuration(this);
            oVPNWin = new OVPN(this);
            consoleTextBox.ReadOnly = true;
            consoleTB = consoleTextBox;
            connectedLabel = label2;
            ipLabel = label8;
            ipLabel.Text = "";
            

            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(Console_KeyDown);
            this.connectedLabel.TextChanged += new EventHandler(ConnectedChanged);
           
            guardRules = new List<GuardRule>();

            ReadConfig();

            listBox2.DataSource = guardRules;
            defaultFWRules = GetFWRules();

            if (defaultPort != 0)
                portBox.Text = defaultPort.ToString();

            FindAndListService();
            notifyIcon1.Visible = false;

        }


        private void PrintCurrentFWRules()
        {
            INetFwAuthorizedApplications apps = GetFWRules();
            foreach(INetFwAuthorizedApplication app in apps)
            {
                consoleTB.Text += app.Name + "\r\n";
            }
        }
    
        //class used to list VPN services. Holds reference to .ovpn files.
        public class ServiceOption
        {
            public ServiceOption(string text, string value)
            {
                Value = value;
                Text = text;
                Ca = Value.Replace(text, "ca.crt");
            }
            public string Value { get; set; }
            public string Text { get; set; }
            public string Ca { get; set; }

            public override string ToString()
            {
                return this.Text;
            }
        }

        public class GuardRule
        {
            public GuardRule(string text, string value, string rule)
            {
                Text = text;
                Value = value;
                int rl = 0;
                try
                {
                    rl = Convert.ToInt32(rule);
                }
                catch(FormatException)
                {
                    rl = 0;
                }
                catch(OverflowException)
                {
                    rl = 0;
                }
                finally
                {
                    Rule = rl;
                }
          
            }
            public string Text {get;set;}
            public string Value {get;set;}
            public int Rule {get;set;}

            public override string ToString()
                {
                    string str = "";
                    if (Rule == 0)
                        str = "Block";
                    if (Rule == 1)
                        str = "Kill";
 	                return this.Text+" Rule: "+str;
                }

        }

        void ConnectedChanged(object sender, EventArgs e)
        {
            if (connectedLabel.Text == "Connected")
                connectedLabel.ForeColor = System.Drawing.Color.Lime;
            else
                connectedLabel.ForeColor = System.Drawing.Color.DarkRed;
        }

        private INetFwAuthorizedApplications GetFWRules()
        {
            INetFwAuthorizedApplications apps;

            Type NetFwMgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", false);
            INetFwMgr mgr = (INetFwMgr)Activator.CreateInstance(NetFwMgrType);
            apps = (INetFwAuthorizedApplications)mgr.LocalPolicy.CurrentProfile.AuthorizedApplications;
            return apps;

        }
        void ApplyRules()
        {
           /* INetFwAuthorizedApplications apps = GetFWRules();
            Type FwAuthorisedApp = Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication", false);
            INetFwAuthorizedApplication app = (INetFwAuthorizedApplication)Activator.CreateInstance(FwAuthorisedApp); */
            
            foreach(GuardRule gr in guardRules)
            {
                /*if(gr.Rule == 0)  //Not working 
                {
                    foreach(INetFwAuthorizedApplication a in apps)
                    {
                        if (a.Name == "µTorrent")
                            a.Enabled = false;   
                    }
                }*/  
                if(gr.Rule == 1)
                {
                    // Kill App processes
                    Process[] proc = Process.GetProcessesByName(gr.Text);
                    foreach(Process pr in proc)
                    {
                        pr.Kill();
                    }
              
                }
            }
        }

        void ResetFWRules()
        {
            INetFwAuthorizedApplications apps = GetFWRules();
            apps = defaultFWRules;
        }

        //Sets Connected/Disconnected status in GUI.
        public Boolean ConnectedBool
        {
            get { return vpnConnected; }
            set
            {
                bool oldValue = vpnConnected;
                vpnConnected = value;
                if(vpnConnected)
                {
                    if(!oldValue)
                    {
                        SetConnectedLabel("Connected");
                    }
                }
                if(!vpnConnected)
                {
                    SetIpLabel("");
                    SetConnectedLabel("Disconnected");
                    if (oldValue)
                        ApplyRules();
                }
            }
        }

        //check config folder for .ovpn files.
        void FindAndListService()
        {
            try
            {
                string[] serviceFiles = Directory.GetFiles(mainDir + @"\config\", "*.ovpn", SearchOption.AllDirectories);
                List<ServiceOption> serviceList = new List<ServiceOption>();
                for (int i = 0; i < serviceFiles.Length; i++)
                {
                    ServiceOption newOption = new ServiceOption(Path.GetFileName(serviceFiles[i]), serviceFiles[i]);
                    serviceList.Add(newOption);
                }
                listBox1.DataSource = serviceList;

            }
            catch(DirectoryNotFoundException)
            {
                consoleTextBox.Text += @"Could not find ..\config directory"+"\r\n";
                consoleTextBox.Text += @"Place your .ovpn and .ca files in 'Main Directory'\config\"+"\r\n";
            }
        }

        //read cfg and import settings.
        public void ReadConfig()
        {
            if(File.Exists(mainDir+@"\cfg.txt"))
            {
                StreamReader cfgRead = new StreamReader(mainDir+@"\cfg.txt");

                List<string> configList = new List<string>();

                while(cfgRead.EndOfStream != true)
                {
                    string line = cfgRead.ReadLine();
                    if (line != null)
                    {
                        configList.Add(line);
                    }
                }

                ParseConfig(configList);

            }
            else
            {
                using (StreamWriter sw = new StreamWriter(mainDir + @"\cfg.txt"))
                {
                    sw.WriteLine(@"openvpncat,c:\");
                    sw.WriteLine(@"defaultport,0");
                }
                //maybe write config ?
            }
        }

        //Add ablity to save to config.

        //parse commands from config.
        void ParseConfig(List<string> cfg) 
        {
            cfgSave = cfg.ToArray();
            foreach(string cfgLine in cfg)
            {
                string[] cmd = cfgLine.Split(',');
                if(cmd[0] == "defaultport")  
                    SetDefaultPort(cmd[1]);
                if (cmd[0] == "openvpncat")
                    SetOVPNCat(cmd[1]);
                if (cmd[0] == "guardrule")
                {
                    GuardRule gr = null;
                    if(cmd.Length == 4)
                         gr = new GuardRule(cmd[1], cmd[2], cmd[3]);
                    if (gr != null)
                        guardRules.Add(gr);
                }
            }
        }

        //set OpenVPN directory
        void SetOVPNCat(string cat)
        {
            if (cat != "")
            {
                oVPNCat = cat;
            }
        }

        // set default port for OpenVPN management interface.
        void SetDefaultPort(string portNr)
        {
            try
            {
                defaultPort = Convert.ToInt16(portNr);
            }
            catch (FormatException)
            {
                consoleTextBox.Text += "Configuration error: Default input for 'port' is not a sequence of digits." + "\r\n";
            }
            catch (OverflowException)
            {
                consoleTextBox.Text += "Configuration Error: Default port number is too long" + "\r\n";
            }
        }

        // If KeyDown Enter post input to Mangagement Interface.
        void Console_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && entryBox.Focused == true)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (MyControllers.tc != null && MyControllers.tc.Connected == true)
                {
                    string str = entryBox.Text;
                    entryBox.Text = "";
                    if (str == "exit" || str == "quit")  // disconnect managment interface and streams.
                    {
                        MyControllers.tc.Close();
                        MyControllers.sr.Close();
                        MyControllers.sw.Close();
                        MyControllers.sr = null;
                        consoleTextBox.Text += "Disconnected from management interface" + "\r\n";
                    }
                    else
                    {
                        MyControllers.sw.WriteLine(str);
                        MyControllers.sw.Flush();
                    }

                }
                else
                {
                    consoleTextBox.Text += "Not connected to management interface, specify port and press 'Connect'" + "\r\n";
                    entryBox.Text = "";
                }
                    
           
            }
        }

        // Connect to specified port on click.
        private void connectButton_Click(object sender, EventArgs e)
        {
            if (MyControllers.tc == null || MyControllers.tc.Connected == false)
            {
                if (portBox.Text != "")
                {
                    string input = portBox.Text;
                    int port = 0;
                    try
                    {
                        port = Convert.ToInt16(input);
                    }
                    catch (FormatException)
                    {
                        consoleTextBox.Text += "Error: Input for 'port' is not a sequence of digits" + "\r\n";
                    }
                    catch (OverflowException)
                    {
                        consoleTextBox.Text += "Error: port number is too long" + "\r\n";
                    }
                    finally
                    {
                        ConnectManager(port);
                    }
                }
            }
            else
                consoleTextBox.Text += "Already connected to management interface."+"\r\n";


        }

        //Connect OpenVPN management interface.
        public void ConnectManager(int port = 0)
        {
           
            MyControllers.tc = new TcpClient();
            try
            {
                MyControllers.tc.Connect("localhost", port);
            }
            catch (SocketException)
            {
                consoleTextBox.Text += "No connection at port: " + port + "\r\n";
            }
            finally
            {
                if (MyControllers.tc != null && MyControllers.tc.Connected == true)
                {
                    consoleTextBox.Text += "Connected to localhost:" + port + "\r\n";
                    MyControllers.sr = new StreamReader(MyControllers.tc.GetStream());
                    MyControllers.sw = new StreamWriter(MyControllers.tc.GetStream());
                    MonitorStart();
                    Thread.Sleep(100);
                    MyControllers.sw.WriteLine("state");
                    MyControllers.sw.Flush();
                    Thread.Sleep(20);
                    MyControllers.sw.WriteLine("state on");
                    MyControllers.sw.Flush();
                    
                }
            }
            
        }

        //Set new text in consoleTextBox, connectedLabel and ipLabel respectivly. Multi-thread safe.
        delegate void SetTextCallback(String Text);
        private void SetIpLabel(string txt)
        {
            if(this.ipLabel.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetIpLabel);
                this.Invoke(d, new object[] { txt });
            }   
            else
            {
                this.ipLabel.Text = txt;
            }
        }
        private void SetConnectedLabel(string txt)
        {
            if(this.connectedLabel.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetConnectedLabel);
                this.Invoke(d, new object[] { txt });
            }
            else
            {
                this.connectedLabel.Text = txt;
            }
        }
        private void SetText(string txt)
        {
            if (this.consoleTextBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { txt });
            }
            else
            {
                this.consoleTextBox.AppendText(txt + "\r\n");
                this.consoleTextBox.ScrollToCaret();
            }
        }


        //Check for output from Management Interface.
        private void MonitorStream()
        {
            while(true)
            {
                if(MyControllers.sr != null)
                {
                    string line = "";
                    try
                    {
                        line = MyControllers.sr.ReadLine();
                    }
                    catch(IOException)
                    { }

                    if (line != null)
                    {
                        ParseLine(line);

                        SetText(line);
                    }
                    Thread.Sleep(10);
                }
                else
                {
                    break;
                }
            }
        }

        private void ParseLine(string line)
        {
            if (line.Contains(">STATE:") || line.Contains("CONNECTED"))
            {
                string[] result = line.Split(',');
                for (int i = 0; i < result.Length; i++)
                {
                    if (result[i] == "CONNECTED")
                    {
                        ConnectedBool = true;
                        if(i < result.Length-2)
                            SetIpLabel(result[i + 2]);
                    }

                       
                    if (result[i] == "EXITING" || result[i] == "RECONNECTING")
                    {
                        ConnectedBool = false;

                    }

                }

            }
            
        }

        //Start monitoring Management Interface stream in a new Thread.
        void MonitorStart()
        {
            
            Thread newThread = new Thread(() => MonitorStream());
            newThread.Start();
            consoleTextBox.Text += "Monitoring" + "\r\n"; //delete or change.
        }

        private void Console_Load(object sender, EventArgs e)
        {

        }

        //Display configuration form.
        private void button1_Click(object sender, EventArgs e)
        {
            // config.Visible = true;
        }

        //Connect service, Start OpenVPN.
        private void button2_Click(object sender, EventArgs e)
        {
            StartService();
        }

        public void StartService()
        {
            try
            {
                p = new Process();
                ServiceOption s = (ServiceOption)listBox1.SelectedItem;
                p.StartInfo.FileName = oVPNCat + @"\bin\openvpn.exe";
                p.StartInfo.Arguments = @"--config " + '"' + s.Value + '"' + " --ca " + '"' + s.Ca + '"' + " --management localhost " + defaultPort;
                consoleTextBox.Text += p.StartInfo.Arguments + "\r\n";
                consoleTextBox.Text += s.Ca + "\r\n";
                p.StartInfo.Verb = "runas";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = true;

                p.OutputDataReceived += p_OutputDataReceived;
                p.ErrorDataReceived += p_OutputDataReceived;

                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                if (!oVPNWin.IsDisposed && !oVPNWin.Visible)
                    oVPNWin.Visible = true;
                output = p.StandardInput;
            }
            catch(Win32Exception e)
            {
                consoleTextBox.Text += "Error!" + "\r\n";
                consoleTextBox.Text += e.Message + "\r\n";
                consoleTextBox.Text += "Make sure you have a choosen the correct main directory for OpenVPN" + "\r\n";
            }
        }

        //Log OpenVPN connection output.
        void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {                
            oVPNWin.SetText(e.Data);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if(MyControllers.tc != null && MyControllers.tc.Connected == true && MyControllers.sw != null)
            {
                MyControllers.sw.WriteLine("signal SIGTERM");
                MyControllers.sw.Flush();
            }
                
        }

        private void Console_FormClosing(object sender, FormClosingEventArgs e)
        {
            // ResetFWRules();
            Application.Exit();
            
        }

        private void button6_Click(object sender, EventArgs e)
        {
            ApplyRules();
        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void Console_Resize(object sender, EventArgs e)
        {
            if(this.WindowState ==  FormWindowState.Minimized)
            {
                this.Visible = false;
                notifyIcon1.Visible = true;

            }
        }

        private void notifyIcon1_MouseMove(object sender, MouseEventArgs e)
        {
            notifyIcon1.Text = connectedLabel.Text + "\r\n" + ipLabel.Text;

        }



             

    }
}
