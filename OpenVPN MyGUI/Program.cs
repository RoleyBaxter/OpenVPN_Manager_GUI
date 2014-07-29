using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;
using NetFwTypeLib;


namespace OpenVPN_MyGUI
{

    public class MyControllers
    {
        public static TcpClient tc;
        public static StreamReader sr;
        public static StreamWriter sw;
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Console());
        }
    }
}
