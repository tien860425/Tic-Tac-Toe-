using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
//for arraylist

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace SKServer
{
    public partial class Form1 : Form
    {
        public readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Form1()
        {
            InitializeComponent();
        }
        List<pairPlayer> playerList = new List<pairPlayer>();
        TcpListener tcpl;
        byte[] data = new byte[1024];
        //byte[] data2 = new byte[1024];
        //bool IsConnected;
        private LingerOption sckoptLinger = null;
        private string NickName1 = string.Empty;
        private string NickName2 = string.Empty;

        private void button1_Click(object sender, EventArgs e)
        {
            Init();
        }
        public void Init()
        {
            try
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(textBox1.Text), int.Parse(textBox2.Text));
                tcpl = new TcpListener(ep);
                tcpl.Start();
                tcpl.BeginAcceptSocket(new AsyncCallback(this.acceptClient), tcpl);
                //IsConnected = true;
                listBox1.Items.Add("伺服器Socket建立完成！");
                listBox1.Update();
                Connect.Enabled = false;
                Exit.Enabled = true;
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
        }
        private void acceptClient(IAsyncResult ar){
            ListBox.CheckForIllegalCrossThreadCalls = false;
            try
            {
                sckoptLinger = new LingerOption(true,3);
                Socket SocketClient;
                
                    SocketClient = tcpl.EndAcceptSocket(ar);
                    listBox1.Items.Add("新客戶端進入..");
                    string ClientIP = ((IPEndPoint)SocketClient.RemoteEndPoint).Address.ToString();
                    int nPort = ((IPEndPoint)SocketClient.RemoteEndPoint).Port;
                    SocketClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, sckoptLinger);
                    log.Info("新客戶端進入 connected! IP:" + ClientIP + " port: " + nPort .ToString ());
                    pairPlayer pp= new pairPlayer (SocketClient);
                    log.Info("Ready to receive data from Client with ID= " + pp.Id);
                    matchPair(pp);
                    SocketClient.BeginReceive(data, 0, 1024, SocketFlags.None, new AsyncCallback(this.ReceiveData), pp);
                    tcpl.BeginAcceptSocket(new AsyncCallback(this.acceptClient), tcpl);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                log.Error(ex.StackTrace);
            }
        }

        public void matchPair(pairPlayer pp){

            foreach (pairPlayer p in playerList)
            {
                if (p.oppenent == null)
                {
                    p.oppenent = pp;
                    pp.oppenent = p;
                    SendFlag(pp,"G");
                    break;
                }
            }
                playerList.Add(pp);
        }
        public void ReceiveData(IAsyncResult ar)
        {
            ListBox.CheckForIllegalCrossThreadCalls = false;
            int recvLength = 0;
            pairPlayer pp=null ;
            try
            {
                pp = (pairPlayer)ar.AsyncState;
                recvLength= pp.mySK.EndReceive(ar);
                if ((recvLength > 0)) {
                    string msg = Encoding.Default.GetString(data).Trim();
                    listBox1.Items.Add("Client1:" + msg);
                    log.Info("receive from :" + pp.Id  + ":[" + msg + "]");
                    if (pp.oppenent != null && pp.oppenent.mySK.Connected == true) Send(pp.oppenent, msg);
                    if (msg.Substring(0, 1) == "Y")
                        {
                        SendFlag(pp,"G");
                        }
                    if (msg.Substring(0, 1) == "C")
                    {
                        String[] pt = msg.Split(',');
                        pp.NickName = pt[1];
                    }
                    Array.Clear(data, 0, data.Length);
                    pp.mySK .BeginReceive(data, 0, 1024, SocketFlags.None, new AsyncCallback(this.ReceiveData), pp);

                    }
                 else
                    {
                        if (pp.mySK  != null)
                        {
                            playerList.Remove(pp);
                            pp.mySK.Shutdown(SocketShutdown.Both);
                            // add linger 
                            pp.mySK.Close();
                            pp.mySK.Dispose();
                            pp.mySK = null;

                        }
                        if (pp.oppenent != null)
                        {
                            pp.oppenent.oppenent = null;
                            playerList.Remove(pp.oppenent);
                            matchPair(pp.oppenent);
                        }
                        listBox1.Items.Add("客戶端" + pp.NickName + "from:" + pp.Id);
                        log.Info("客戶端" + pp.NickName + "from:" + pp.Id);
                        pp = null;
                    }
            }
            catch (Exception ex)
            {
                playerList.Remove(pp);
                if (pp !=null && pp.mySK  != null)
                {
                    pp.mySK.Dispose();
                    pp.mySK = null;

                }
                if (pp.oppenent != null)
                {
                    pp.oppenent.oppenent = null;
                    playerList.Remove(pp.oppenent);
                    matchPair(pp.oppenent);
                }
                listBox1.Items.Add(ex.ToString ());

                listBox1.Items.Add("客戶端" + pp.NickName + "from:" + pp.Id);
                log.Error(ex.ToString());
                log.Error(ex.StackTrace );
                log.Info("客戶端" + pp.NickName + "from:" + pp.Id);

                pp = null;
            }
        }

 

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (pairPlayer pp in playerList)
                {
                    pp.mySK .Shutdown(SocketShutdown.Both);
                    pp.mySK.Close();
                 }
                playerList.Clear();
                Connect.Enabled = true;
                Exit.Enabled = false;
                listBox1.Items.Clear();
           }
            catch (Exception ex)
            {
                Connect.Enabled = false;
                log.Error (ex.ToString());
            }

        }

        private void  SendFlag(pairPlayer pp,String msg)
        {
            Send(pp , msg);
            Send(pp.oppenent , msg);
            //SocketClient1.Send(Encoding.Default.GetBytes("G"));
            //    SocketClient2.Send(Encoding.Default.GetBytes("G"));
                Thread.Sleep(1000);
                chooseWhoFirst(pp);
        }
        private void Send(pairPlayer pp, String msg)
        {
            Send(pp, Encoding.Default.GetBytes(msg));

        }


        private void Send(pairPlayer pp, byte[] byteArray) 
        {
            try
            {
                pp.mySK.Send(byteArray);
                log.Info("Send[" + Encoding.Default.GetString(byteArray).Trim() + "]  to: " + pp.Id);
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                //每隔3秒try connecting to Client
                int tryCount = 1;
                while (tryCount < 4)
                {
                    try
                    {
                        listBox1.Items.Add("等3秒...第" + tryCount.ToString() + "次重送！");
                        Thread.Sleep(3000);
                        pp.mySK .Send(byteArray);
                        log.Info("Send[" + Encoding.Default.GetString(byteArray).Trim() + "]  to: " + pp.Id);
                        return;
                    }
                    catch
                    {
                        listBox1.Items.Add("第" + tryCount.ToString() + "次重送, 失敗！");
                        tryCount++;
                    }
                }
                    Send(pp.oppenent , "E," + pp.NickName );
                    pp.oppenent.oppenent = null;

                pp.mySK.Close();
                pp.mySK = null;
                playerList.Remove(pp);
                tcpl.BeginAcceptSocket(new AsyncCallback(this.acceptClient), tcpl);
            }

        }

        private void chooseWhoFirst(pairPlayer pp)
        {
            //The Next(Int32, Int32) overload returns random integers that range from minValue to maxValue – 1.
            //However, if maxValue equals minValue, the method returns minValue.


            Random Rnd = new Random();
            int client=Rnd.Next(1,3);
            //Server 決定那方先選先下，收到S的。myPick=true;
            if (client==1){
                //SocketClient1.Send(Encoding.Default.GetBytes("S")); //Select
                //SocketClient2.Send(Encoding.Default.GetBytes("W")); //wait
                Send(pp, "S");
                Send(pp.oppenent , "W");
            }
            else
            {
                //SocketClient2.Send(Encoding.Default.GetBytes("S"));
                //SocketClient1.Send(Encoding.Default.GetBytes("W"));
                Send(pp, "W");
                Send(pp.oppenent, "S");

            }
        }
    }

    public class pairPlayer:object
    {
        public Socket mySK;
        public string NickName;
        public pairPlayer oppenent;
        public string Id = "";
        public pairPlayer(Socket sk)
        {
            mySK = sk;
            if (sk != null)
            {
                Id = ((IPEndPoint)sk.RemoteEndPoint).Address.ToString() + ":" + ((IPEndPoint)sk.RemoteEndPoint).Port.ToString ();
            }
        }
        //public string myId()
        //{
        //    if (mySK == null)
        //    {
        //        return "";
        //    }
        //    else if(Id=="")
        //    {
        //        Id= ((IPEndPoint)mySK.RemoteEndPoint).Address.ToString() + ":" + ((IPEndPoint)mySK.RemoteEndPoint).Port.ToString();
        //        return Id;
        //    }
        //    else
        //    {
        //        return Id;
        //    }
        //}
    }
}
