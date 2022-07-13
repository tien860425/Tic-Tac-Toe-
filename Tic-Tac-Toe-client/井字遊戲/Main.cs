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
using System.IO;
using log4net;
using System.Resources;
using System.Collections;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace 聊天室
{
    public partial class Main : Form
    {
        public readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Socket SK;
        public Thread Td;
        public IPEndPoint EP;
        String strCase;
        byte[] data = new byte[1024];
        //bool IsConnect = false;
        string NickName = "";
        List<BasicPic> pickImgList = new List<BasicPic>();

        public Main()
        {
            InitializeComponent();
            this.Closing += new System.ComponentModel.CancelEventHandler(this.Main_Closed);
            for (int i = 0; i < 5; i++)
            {
                Xpic[i].BackgroundImage = pictureBox2.BackgroundImage;
                Xpic[i].BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
                Xpic[i].Size = pictureBox2.Size;
                Xpic[i].BackColor = System.Drawing.Color.Transparent;
                Xpic[i].Top = 400;
                Xpic[i].Left = 400;
                //Xpic[i].Visible = false;
                pnlBoard.Controls.Add(Xpic[i]);
                Opic[i].BackgroundImage = pictureBox1.BackgroundImage;
                Opic[i].BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
                Opic[i].Size = pictureBox1.Size;
                Opic[i].BackColor = System.Drawing.Color.Transparent;
                Opic[i].Top = 400;
                Opic[i].Left = 400;
                //Opic[i].Visible = false;
                pnlBoard.Controls.Add(Opic[i]);

            }
            //ResourceManager rm = new ResourceManager(typeof(聊天室.Properties.Resources)); 

            //put pictureBox into array
            //winPattern[0, 0] = rm.GetObject("_0") as Image ;



        }
        private void Main_Closed(object sender, System.EventArgs e)
        {
            Form1.isClose = true;
        }
        ////NetworkStream stream;
        private void Main_Shown(object sender, EventArgs e)
        {
            //SK.ReceiveAsync
            //Td = new Thread(socketReceive);
            //Td.IsBackground = true;
            //Td.Start();
            try
            {

                //stream = new NetworkStream(SK);
                //stream.BeginRead(data, 0, 1024, new AsyncCallback(this.socketReceive), stream);
                LingerOption sckoptLinger = new LingerOption(true,3);
                SK.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, sckoptLinger);
                SK.BeginReceive(data, 0, 1024, SocketFlags.None, new AsyncCallback(this.socketReceive), SK);
                log.Info("Socket ready to receive message!");
                //IsConnect = true;
                NickName = Form1.NickName;
                addMessage("歡迎 " + NickName + " 先生/小姐 您的加入");
                addMessage("請稍後..................................");

            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
        }

        public void socketSend(string sendData)
        {
            //if (IsConnect)
            {
                try
                {
                    SK.Send(Encoding.UTF8.GetBytes(sendData));
                    //addMessage("Send out :[" + sendData + "]");
                    log.Info("Send out :[" + sendData + "]");
                }
                catch (Exception ex)
                {
                    log.Error(ex.ToString());
                    //每隔3秒try connecting to Server
                    int tryCount = 1;
                    while (tryCount < 4)
                    {
                        try
                        {
                            addMessage("等3秒...第" + tryCount.ToString() + "次重送Server！");
                            Thread.Sleep(3000);
                            if (SK == null)
                            {
                                SK.Connect(EP);
                                //stream = new NetworkStream(SK);
                                //stream.BeginRead(data, 0, 1024, new AsyncCallback(this.socketReceive), stream);
                                LingerOption sckoptLinger = new LingerOption(true, 3);
                                SK.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, sckoptLinger);
                                SK.BeginReceive(data, 0, 1024, SocketFlags.None, new AsyncCallback(this.socketReceive), SK);
                                log.Info("Socket ready to receive message again !");
                            }
                            //if (stream == null)
                            //{
                            //    stream = new NetworkStream(SK);
                            //    stream.BeginRead(data, 0, 1024, new AsyncCallback(this.socketReceive), stream);
                            //    log.Info("Socket ready to receive message again !");
                            //}
                            SK.Send(Encoding.UTF8.GetBytes(sendData)); return;

                        }
                        catch
                        {
                            addMessage("第" + tryCount.ToString() + "次重送Server, 失敗！");
                            tryCount++;
                        }
                    }
                    SK.Shutdown(SocketShutdown.Both);
                    SK.Close();
                    //Close();

                }
            }
            //else
            //    log.Error("請等待對方連線");
        }


        public void socketReceive(IAsyncResult ar)
        {
            Form.CheckForIllegalCrossThreadCalls = false;
            int recvLength = 0;

            //stream = ((NetworkStream)(ar.AsyncState));
            try
            {
            //    if (((stream == null) || !stream.CanRead))
            //    {
            //        return;
            //    }

                recvLength  = SK.EndReceive(ar);

                if (recvLength > 0)
                {
                    string msg = Encoding.UTF8.GetString(data, 0, recvLength).Trim();
                    log.Info("Receive message:[" + msg + "]");
                    Receive(msg);
                    Array.Clear(data, 0, data.Length);
                    //stream.BeginRead(data, 0, 1024, new AsyncCallback(this.socketReceive), stream);
                    SK.BeginReceive(data, 0, 1024, SocketFlags.None, new AsyncCallback(this.socketReceive), SK);
                }
                else
                {
                    log.Info("Server 關閉連線!!");
                    //if (stream != null)
                    //{
                    //    stream.Close();
                    //    SK.Shutdown(SocketShutdown.Both);
                    //    SK.Close();
                    //}
                    if (SK != null)
                    {
                        SK.Shutdown(SocketShutdown.Both);
                        SK.Close();

                    }
                    //stream = null;
                    SK = null;
                    Close();
                    //IsConnect = false;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                if (SK != null && SK.Connected )
                {
                    SK.Shutdown(SocketShutdown.Both);
                    SK.Close();

                }
                //stream = null;
                SK = null;
                Close();
           }
        }
        public void Receive(string recvData) //接收資料判斷輸贏
        {
            Panel.CheckForIllegalCrossThreadCalls = false;
            Label.CheckForIllegalCrossThreadCalls = false;
            ListBox.CheckForIllegalCrossThreadCalls = false;
            try
            {
                strCase = recvData.Substring(0, 1);
                char token = ',';
                string[] pt = recvData.Split(token);
                //string str = "";
                //for (int i = 1; i < pt.Length; i++)
                //{
                //    str += pt[i];
                //}
                switch (strCase)
                {
                    case "G":
                        //IsConnect = true;
                        addMessage("連線完成...，準備選擇那一方先選符號!");

                        //有時GS會一起接收
                        if (recvData.Length > 1)
                            Receive(recvData.Substring(1, 1));
                        break;
                    //case "T":
                    //    addMessage(str);
                    //    break;
                    case "S":
                        myTurn = true;
                        addMessage("請選擇您要的符號！");
                        break;
                    case "W":
                        addMessage("遊戲開始！");
                        addMessage("對方選擇符號並下第一步中...！");
                        gameStart = true;
                        break;
                    case "C":
                        if (myTurn)
                        {
                            addMessage("遊戲開始！");
                            if (myImage == 'O')
                            {
                                lblX.Text = pt[1];
                                lblX.Visible = true;
                            }
                            else
                            {
                                lblO.Text = pt[1];
                                lblO.Visible = true;
                            }
                        }
                        else
                        {
                            if (pt[2] == "O")
                            {
                                socketSend("C," + NickName + ",X,");
                                myImage = 'X';
                                pictureBox1.Enabled = false;
                                myPicture = pictureBox2;
                                otherPicture = pictureBox1;
                                lblO.Text = pt[1];
                                lblX.Text = NickName;
                                lblO.Visible = true;
                                lblX.Visible = true;

                            }
                            else
                            {
                                socketSend("C," + NickName + ",O,");
                                myImage = 'O';
                                pictureBox2.Enabled = false;
                                myPicture = pictureBox1;
                                otherPicture = pictureBox2;
                                lblX.Text = pt[1];
                                lblO.Text = NickName;
                                lblO.Visible = true;
                                lblX.Visible = true;
                            }
                            addMessage("對方:" + pt[1] + "選擇了 " + pt[2] + ". 所以您是" + myImage.ToString());
                            addMessage("遊戲開始！, " + pt[1] + "正在下...");
                        }
                        break;
                    case "P": //期局進行中
                        putImageToBoard(Convert.ToInt32(pt[2]));
                        myTurn = true;
                        myPicture.Enabled = true;
                        addMessage(NickName + ", 該您下！ 您是 " + myImage.ToString());
                        break;
                    case "A"://要求再一盤
                        if (MessageBox.Show(pt[1] + " 邀請再來一局！好嗎？", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            socketSend("Y," + NickName + ",");
                            btnReNew.Enabled = false;
                        }
                        else
                            socketSend("N," + NickName + ",");
                        refresh();

                        break;
                    case "F"://比賽有結果
                        btnReNew.Enabled = true;
                        putImageToBoard(Convert.ToInt32(pt[2]));
                        pattern = Convert.ToInt32(pt[3]);
                        myTurn = false;
                        gameStart = false;
                        addMessage("可惜 !" + pt[1] + "贏了！ 要再來一局嗎?");
                        setPattern();
                        break;
                    case "D"://比賽和局
                        btnReNew.Enabled = true;
                        putImageToBoard(Convert.ToInt32(pt[2]));
                        myTurn = false;
                        gameStart = false;
                        addMessage("這局平手!! 要再來一局嗎?");
                        break;

                    case "N"://收到A回覆不接受
                        addMessage(pt[1] + "不玩了 !");
                        break;
                    case "Y":
                        addMessage(pt[1] + ",接受您的邀請再來一局 !");
                        refresh();
                        btnReNew.Enabled = false;
                        break;
                    case "E":
                        addMessage(pt[1] + ",已離線 !");
                        refresh();
                        break;


                }
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
        }


        private void putImageToBoard(int idx)
        {
            //對方符號
            BasicPic tempPic;
            if (myImage == 'O')
            {
                occupation[idx] = 'X';
                tempPic = Xpic[XPick++];

            }
            else
            {
                occupation[idx] = 'O';
                tempPic = Opic[OPick++];

            }
            //創造圖示並填入相對位置
            tempPic.Top = cross[idx, 0] + (cross[idx, 1] - cross[idx, 0] - tempPic.Height) / 2 - 100;
            tempPic.Left = cross[idx, 2] + (cross[idx, 3] - cross[idx, 2] - tempPic.Width) / 2 - 100;
            tempPic.Visible = true;
            imageOccupation[idx] = tempPic;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (pic == null || gameStart ==false) return;
            if (pic.BackColor == System.Drawing.Color.Transparent) return;
            pic.EnableTM(false);
            pic.BackColor = System.Drawing.Color.Transparent;
            myTurn = false;
            myPicture.Enabled = false;
            imageOccupation[CurrPos] = pic;
            if (CalculateFinal())
            {
                socketSend("F," + NickName + "," + CurrPos.ToString() + "," + pattern.ToString() + ",");
                setPattern();
                addMessage(NickName + ", 恭喜,您贏了!! 要再來一局嗎?");
                gameStart = false;
                btnReNew.Enabled = true;
            }
            else
            {
                if (checkDrawDTie())
                {
                    socketSend("D," + NickName + "," + CurrPos.ToString() + "," + pattern.ToString() + ",");
                    addMessage(NickName + ", 這局平手!! 要再來一局嗎?");
                    gameStart = false;
                    btnReNew.Enabled = true;

                }
                else
                {
                    socketSend("P," + NickName + "," + CurrPos.ToString() + ",");
                    addMessage(NickName + ", 對方正在下...");
                }
            }
        }

        /// <summary>
        /// 將提示訊息填入畫面上方的ListBox，並維持僅保留4筆
        /// </summary>
        /// <param name="message"></param>
        private void addMessage(String message)
        {
            listBox1.Items.Add(message);
            if (listBox1.Items.Count > 4)
            {
                listBox1.Items.RemoveAt(0);
            }
        }

        private void setPattern()
        {
            imageOccupation[winArray[pattern, 0]].EnableTM(true);
            imageOccupation[winArray[pattern, 1]].EnableTM(true);
            imageOccupation[winArray[pattern, 2]].EnableTM(true);

        }


        bool gameStart = false; //在兩個來賓均上線，且已選符號後gameStart=true;
        bool myTurn = false;    //維持雙方僅其中一方取得myTurn =true, 以便下棋，下完傳P訊息換對方取得myTurn =true
        BasicPic pic;           //在走每一步時作用的圖形符號儲存於此(記憶體儲存位址)
        bool newpicMove = false;//在mouse_down後產生新BasicPic符號後，設為true, 以作為避免觸發mouse_move,mouse_up執行相關callback Function不應執行的code之旗號(flag)
        int ix, iy;            //儲存mouse_down,mouse_move前滑鼠pointer相對於pictureBox1或pictureBox2左上角位置
        char myImage = 'O';      //選擇或被選擇的圖形(O/X)
        int CurrPos;           //dragdrop拖放前格子的編號，左上第一格為0，再來向右到第3格，再來下一列，同樣左至右的順序編號
        int prePos;             //dragdrop拖放後格子的編號

        PictureBox myPicture, otherPicture;  //選擇的符號是左上Picture1或右上Picture2
        char[] occupation = { 'E', 'E', 'E', 'E', 'E', 'E', 'E', 'E', 'E' }; //棋盤九格每格是那一符號(O/X)佔據，沒有則填E
        //{上，下，左，右}座標相對於Form的座標
        int[,] cross = { { 127, 241, 128, 243 }, { 127, 241, 243, 355 }, { 127, 241, 355, 464 }, { 241, 355, 128, 243 }, { 241, 355, 243, 355 }, { 241, 355, 355, 464 }, { 355, 461, 128, 243 }, { 355, 461, 243, 355 }, { 355, 461, 355, 464 } };
        //八種贏棋的位置
        int[,] winArray = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 }, { 0, 3, 6 }, { 1, 4, 7 }, { 2, 5, 8 }, { 0, 4, 8 }, { 2, 4, 6 } };

        //O符號圖形, X符號圖形各五個，並且存在Opic array, Xpic array。作為對手下完一步後傳來所下位置，在我方棋盤上應相對顯示該位置的圖形
        BasicPic[] Xpic = { new BasicPic(), new BasicPic(), new BasicPic(), new BasicPic(), new BasicPic() };
        BasicPic[] Opic = { new BasicPic(), new BasicPic(), new BasicPic(), new BasicPic(), new BasicPic() };
        BasicPic[] imageOccupation = { null, null, null, null, null, null, null, null, null };
        //贏棋的模式
        int pattern;
        int XPick = 0;  //影像取到第幾位, 不同thread不能將物件宣告為其他thread物件的子物件
        int OPick = 0;  //影像取到第幾位


        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox aa = (PictureBox)sender;
            //挑選符號階段
            if (gameStart == false && myTurn == true)
            {
                gameStart = true;

                myImage = aa.Tag.ToString()[0];
                if (myImage == 'O')
                {
                    pictureBox2.Enabled = false;
                    lblO.Text = NickName;
                    addMessage("您選擇 O !");
                    myPicture = pictureBox1;
                    otherPicture = pictureBox2;
                }
                else
                {
                    pictureBox1.Enabled = false;
                    lblX.Text = NickName;
                    myPicture = pictureBox2;
                    otherPicture = pictureBox1;
                    addMessage("您選擇 X !");

                }
                socketSend("C," + NickName + "," + myImage.ToString() + ",");

            }
            if (!myTurn) return;
            pic = new BasicPic();
            pic.BackColor = System.Drawing.Color.Yellow;
            pic.BackgroundImage = aa.BackgroundImage;
            pic.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            pic.Size = aa.Size;
            pic.Location = aa.Location;
            pic.Visible = true;
            pic.MouseDown += pic_MouseDown;
            //pic.MouseMove += pic_MouseMove;
            //pic.MouseUp += pic_MouseUp;
            newpicMove = true;
            ix = e.X;
            iy = e.Y;
            this.Controls.Add(pic);
            pic.BringToFront();

        }

        private void pic_MouseDown(object sender, MouseEventArgs e)
        {
            Point tempP;
            pic = (BasicPic)sender;
            //之前已下棋子不可移動
            if (pic.BackColor == System.Drawing.Color.Transparent || gameStart == false) return;
            tempP = this.PointToClient(pnlBoard.PointToScreen(pic.Location));
            int i;
            int picCenterX = tempP.X + 32;
            int picCenterY = tempP.Y + 30;
            for (i = 0; i < 9; i++)
            {
                if (picCenterY >= cross[i, 0] && picCenterY <= cross[i, 1] && picCenterX >= cross[i, 2] && picCenterX <= cross[i, 3])
                {
                    if (occupation[i] != myImage)
                    {
                        return;
                    }
                    //occupation[i] = 'E';
                    prePos = i;
                    break;

                }
            }
            pic.DoDragDrop(pic, DragDropEffects.All);
            pic.BringToFront();

        }



        private void pic_MouseMove(object sender, MouseEventArgs e)
        {

            if (newpicMove)
            {
                pic.Top = pic.Top + (e.Y - iy);
                pic.Left = pic.Left + (e.X - ix);

                if ((pic.Top >= 127 && pic.Top <= 461 && pic.Left >= 128 && pic.Left <= 464) ||
                    (pic.Top >= 127 && pic.Top <= 461 && pic.Left + 65 >= 128 && pic.Left + 65 <= 464) ||
                    (pic.Top + 61 >= 127 && pic.Top + 61 <= 461 && pic.Left >= 128 && pic.Left <= 464) ||
                    (pic.Top + 61 >= 127 && pic.Top + 61 <= 461 && pic.Left + 65 >= 128 && pic.Left + 65 <= 464))
                {
                    pic.BackColor = System.Drawing.Color.Cyan;
                }
                else
                {
                    pic.BackColor = System.Drawing.Color.Yellow;
                }
                iy = e.Y;
                ix = e.X;
            }
        }

        private void pic_MouseUp(object sender, MouseEventArgs e)
        {
            if (!newpicMove) return;
            int picCenterX = pic.Left + 32;
            int picCenterY = pic.Top + 30;
            try
            {
                newpicMove = false;
                if (picCenterX < 128 || picCenterX > 464 || picCenterY < 127 || picCenterY > 461)
                {
                    this.Controls.Remove(pic);
                    pic.Dispose();
                    pic = null;
                    return;

                }
                int i;
                for (i = 0; i < 9; i++)
                {
                    if (picCenterY >= cross[i, 0] && picCenterY <= cross[i, 1] && picCenterX >= cross[i, 2] && picCenterX <= cross[i, 3])
                    {
                        if (occupation[i] == 'E')
                        {
                            pnlBoard.Controls.Add(pic);
                            pic.Top = cross[i, 0] + (cross[i, 1] - cross[i, 0] - pic.Height) / 2 - 100;
                            pic.Left = cross[i, 2] + (cross[i, 3] - cross[i, 2] - pic.Width) / 2 - 100;
                            occupation[i] = myImage;
                            pic.EnableTM(true);
                            CurrPos = i;
                            pickImgList.Add(pic);
                        }
                        else
                        {
                            String msg;
                            if (occupation[i] == myImage)
                            {
                                msg = "此格您已選填過!";
                            }
                            {
                                msg = "此格對方已選填過!";

                            }
                            MessageBox.Show(msg);
                            this.Controls.Remove(pic);
                            pic.Dispose();
                        }

                        break;

                    }
                }
                if (i >= 9)
                {
                    this.Controls.Remove(pic);
                    pic.Dispose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //檢查是否平手
        private bool checkDrawDTie()
        {
            //空格小於等於2, (因之前已檢查沒有一方贏)
            ArrayList tie =new ArrayList ();
            //ArrayList emptyLocation =new ArrayList ();
            string locationCat = "";
            for (int i = 0; i < 9; i++)
            {
                if (occupation[i] == 'E')
                {
                    locationCat += i.ToString();
                }
            }
            if (locationCat.Length  == 2)
            {
                tie.Add("01");
                tie.Add("03");
                tie.Add("12");
                tie.Add("14");
                tie.Add("25");
                tie.Add("34");
                tie.Add("36");
                tie.Add("45");
                tie.Add("47");
                tie.Add("58");
                tie.Add("67");
                tie.Add("78");
                if (tie.IndexOf(locationCat) < 0)
                    return false;
                else
                    return true;

            }
                 //int[,] winArray = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 }, { 0, 3, 6 }, { 1, 4, 7 }, { 2, 5, 8 }, { 0, 4, 8 }, { 2, 4, 6 } };
                 if (locationCat.Length == 1)
                 {
                     for (int i = 0; i < 9; i++)
                     {
                         for (int j = 0; j < 3; j++)
                         {
                             if (winArray[i, j] == int.Parse(locationCat))
                             {
                                 string ss = "";
                                 for (int k = 0; k < 3; k++)
                                 {
                                     if (j != k)
                                     {
                                         ss += occupation[winArray[i, j]].ToString();
                                         if((myImage =='O' && ss=="XX") || ( myImage =='X' && ss=="OO"))
                                            return false;
                                     }
                                 }
                             }
                         }
                     }
                return true;
                 }
            return false;

        }
        private bool CalculateFinal()
        {
            int count = 0;
            int[] myChosenPos = { 9, 9, 9, 9, 9 };
            for (int i = 0; i < 9; i++)
            {
                if (occupation[i] == myImage)
                {
                    myChosenPos[count] = i;
                    count++;
                }
            }
            if (count < 3) return false;
            for (int i = 0; i < 8; i++)
            {
                int match = 0;
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 5; k++)
                    {
                        if (winArray[i, j] == myChosenPos[k])
                        {
                            match++;
                            break;
                        }
                    }
                    if (match == 3)
                    {
                        pattern = i;
                        return true;
                    }

                }
            }
            return false;

        }

      
        private void pnlBoard_DragDrop(object sender, DragEventArgs e)
        {
            pic = (BasicPic)e.Data.GetData("聊天室.BasicPic", false);
            Point pt = this.PointToClient(Cursor.Position);
            int i;
            for (i = 0; i < 9; i++)
            {
                if (pt.Y >= cross[i, 0] && pt.Y <= cross[i, 1] && pt.X >= cross[i, 2] && pt.X <= cross[i, 3])
                {
                    if (i == prePos) return;
                    if (occupation[i] == 'E')
                    {

                        pic.Top = cross[i, 0] + (cross[i, 1] - cross[i, 0] - pic.Height) / 2 - 100;
                        pic.Left = cross[i, 2] + (cross[i, 3] - cross[i, 2] - pic.Width) / 2 - 100;
                        occupation[i] = myImage;
                        occupation[prePos] = 'E';
                        CurrPos = i;
                        //pic.BackColor = System.Drawing.Color.Transparent;
                        pic.EnableTM(true);
                        return;
                        //CalculateFinal();
                    }
                    else
                    {
                        String msg;
                        if (occupation[i] == myImage)
                        {
                            msg = "此格您已選填過!";
                        }
                        {
                            msg = "此格對方已選填過!";

                        }
                        occupation[prePos] = myImage;
                        MessageBox.Show(msg);
                    }

                    return;

                }
                //else
                //{
                //    pnlBoard.Controls.Remove(pic);
                //    pic.Dispose();
                //    //PictureBox1.Enabled=true;
                //}

            }
            occupation[prePos] = 'E';
            pnlBoard.Controls.Remove(pic);
            pic.Dispose();
        }

        private void pnlBoard_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("聊天室.BasicPic"))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void btnEnd_Click_1(object sender, EventArgs e)
        {
            socketSend("E," + NickName + ",");
            
            SK.Shutdown(SocketShutdown.Both);
            SK.Close();
            //Close();

        }

        private void btnReNew_Click(object sender, EventArgs e)
        {
           
            //要求從新來一局
            socketSend("A," + NickName + ",");


        }
        private void refresh()
        {
            //Panel.CheckForIllegalCrossThreadCalls = false;
            //BasicPic.CheckForIllegalCrossThreadCalls = false; 
            gameStart = false;
            myTurn = false;
            pic = null;
            newpicMove = false;
            myImage = 'O';
            myPicture = null;
            XPick = 0;
            OPick = 0;
            lblO.Text = "";
            lblX.Text = "";
            pictureBox1.Enabled = true;
            pictureBox2.Enabled = true;
            //影像隱藏
            for (int i = 0; i < 5; i++)
            {
                Xpic[i].Visible = false;
                Xpic[i].EnableTM(false);
                Opic[i].Visible = false;
                Opic[i].EnableTM(false);

            }
            //清空棋盤記錄

            for (int i = 0; i < 9; i++)
                occupation[i] = 'E';
            //清空棋盤圖像
            BasicPic pp;
            for (int i = 0; i < pickImgList.Count; i++)
            {
                pp = pickImgList[i];
                //pnlBoard.Controls.Remove(pp);
                pp.EnableTM(false);
                pp.Dispose();

            }
            pickImgList.Clear();

 

        }



    }


    public class BasicPic : PictureBox
    {
        private System.Timers.Timer tm;
        private int turn = 0;
        public BasicPic()
        {
            tm = new System.Timers.Timer();
            tm.Interval = 1000;
            this.tm.Elapsed += new System.Timers.ElapsedEventHandler(this.tm_Tick);
        }

        private void tm_Tick(object sender, EventArgs e)
        {
            switch (turn)
            {
                case 0:
                    this.BackColor = System.Drawing.Color.Magenta;
                    break;
                case 1:
                    this.BackColor = System.Drawing.Color.Cyan;
                    break;
                case 2:
                    this.BackColor = System.Drawing.Color.Yellow;
                    break;
            }
            turn++;
            if (turn > 2) turn = 0;
        }
        public void EnableTM(bool set)
        {
            tm.Enabled = set;
        }
    }
}

