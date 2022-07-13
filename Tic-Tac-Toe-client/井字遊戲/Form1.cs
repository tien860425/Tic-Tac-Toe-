using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.IO;
using log4net;

namespace 聊天室
{
    public partial class Form1 : Form
    {
        public readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
        public static bool isClose = false;
        public static string NickName = "";
        public static string sIP = "";
        public static string sPort = "";
        Main m;
        public static LingerOption sckoptLinger = null;
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                sckoptLinger = new LingerOption(true, 3);
                NickName = textBox3.Text.Trim();
                sIP = textBox1.Text.Trim();
                sPort = textBox2.Text.Trim();
                m = new Main();
                m.SK = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m.SK.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, sckoptLinger);
                m.EP = new IPEndPoint(IPAddress.Parse(textBox1.Text), int.Parse(textBox2.Text));
                m.SK.Connect(m.EP);
                textBox1.Enabled = false;
                textBox2.Enabled = false;
                textBox3.Enabled = false;
                button1.Enabled = false;
                this.Hide();
                m.ShowDialog();
                this.Show();
                if (isClose)
                {
                    textBox1.Enabled = true;
                    textBox2.Enabled = true;
                    textBox3.Enabled = true;
                    button1.Enabled = true;

                }
            }
            catch (Exception ex)
            {
                log.Error (ex.ToString());
                timer1.Enabled = true;
                lblMsg.Text = @"重新連線主機中...";
            }
        }
        int count = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            count++;
            if (count > 10)
            {
                timer1.Enabled = false;
                lblMsg.Text = @"請按登入，重新連線...";
                count = 0;
                return;
            }
            try
            {
                m.SK.Connect(m.EP);
                timer1.Enabled = false;
                lblMsg.Text = "";
                textBox1.Enabled = false;
                textBox2.Enabled = false;
                textBox3.Enabled = false;
                button1.Enabled = false;
                m.ShowDialog();
                if (isClose)
                {
                    textBox1.Enabled = true;
                    textBox2.Enabled = true;
                    textBox3.Enabled = true;
                    button1.Enabled = true;

                }

            }
            catch (Exception ex)
            {
                lblMsg.Text = @"重新連線主機中..." + count.ToString () ;
                log.Error(ex.ToString());

            }
        }

  
    }
}
