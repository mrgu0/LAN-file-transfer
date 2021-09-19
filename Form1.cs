using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LAN_file_transfer
{
    public partial class Form1 : Form
    {
        static Form1 mainFrm;
        public Form1()
        {
            mainFrm = this;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IPAddress[] ip = Dns.GetHostAddresses(Dns.GetHostName());
            IPAddress localIp = ip[ip.Length - 1];
            textBox2.Text = localIp.ToString();
        }

        private void RevFile()
        {
            byte[] recByte = new byte[4096];
            IPEndPoint ipe;
            Socket socket;
            try
            {
                ipe = new IPEndPoint(IPAddress.Parse(mainFrm.textBox2.Text), int.Parse(mainFrm.textBox4.Text));
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(ipe);
                socket.Listen(1);
            }
            catch (Exception)
            {
                MessageBox.Show("IP或端口错误，无法监听");
                return;     //结束监听
            }
            mainFrm.richTextBox1.AppendText("开始监听 " + mainFrm.textBox2.Text + ":" + mainFrm.textBox4.Text + "端口\r\n");
            while (true)
            {
                mainFrm.button1.Enabled = false;
                Socket serverSocket = socket.Accept();
                int flag = 0;       //进度条
                int count = 0;
                int bytes;
                //开始接收
                mainFrm.button2.Enabled = false;

                bytes = serverSocket.Receive(recByte, recByte.Length, 0);       //接收名字
                string fileName = Encoding.Unicode.GetString(recByte, 0, bytes);
                mainFrm.richTextBox1.AppendText("接收文件:" + fileName);
                bytes = serverSocket.Receive(recByte, recByte.Length, 0);       //接收大小
                string fileSize = Encoding.Unicode.GetString(recByte, 0, bytes);
                long reFileSize = Convert.ToInt64(fileSize);                //文件字节
                if (reFileSize / 1024 / 1024 >= 1024)
                    mainFrm.richTextBox1.AppendText("   " + ((reFileSize / 1024 / 1024 / 1024)).ToString() + "Gb\r\n");
                else if (reFileSize / 1024 >= 1024)
                    mainFrm.richTextBox1.AppendText("   " + ((reFileSize / 1024 / 1024)).ToString() + "Mb\r\n");
                else
                    mainFrm.richTextBox1.AppendText("   " + ((reFileSize / 1024)).ToString() + "KB\r\n");

                BinaryWriter Bwfilw;            //文件指针
                try
                {
                    Bwfilw = new BinaryWriter(new FileStream(fileName, FileMode.Create));
                }
                catch (Exception)
                {
                    MessageBox.Show("文件创建失败");
                    serverSocket.Close();
                    continue;       //开始下一次监听
                }
                /*--------------------------接收文件内容--------------------------*/
                mainFrm.label4.Text = "下载中";
                do
                {
                    try
                    {
                        bytes = serverSocket.Receive(recByte, recByte.Length, 0);
                        Bwfilw.Write(recByte, 0, bytes);

                        flag++;
                        count++;
                        if(flag== 10240*2)         //每40Mb更新进度条
                        {
                            float x = (float)(count*4 /1024) / (reFileSize/1024/1024);
                            mainFrm.progressBar1.Value = (int)(x * 100);
                            flag = 0;
                        }

                    }
                    catch (Exception)
                    {
                        MessageBox.Show("文件接收过程出错");
                        break;
                    }
                }
                while (bytes > 0);
                serverSocket.Close();
                Bwfilw.Close();
                mainFrm.label4.Text = "空闲";
                mainFrm.progressBar1.Value = 0;
                mainFrm.button2.Enabled = true;
            }

        }
        /// <summary>
        /// 监听端口按钮
        /// </summary>
        private void button1_Click(object sender, EventArgs e)      
        {
            Thread thread = new Thread(RevFile);             //发送文件服务
            thread.IsBackground = true;
            thread.Start();
        }
        /// <summary>
        /// 选择文件按钮
        /// </summary>
        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "所有文件(*.*)|*.*"; //设置要选择的文件的类型
            fileDialog.Multiselect = false; //是否可以多选

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = fileDialog.FileName;//返回文件的完整路径
            }
        }
        /// <summary>
        /// 发送文件
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            if(mainFrm.textBox1.Text.Length==0)
            {
                MessageBox.Show("未选择文件");
                return;
            }
            Thread thFile = new Thread(SendFile);             //发送文件服务
            thFile.IsBackground = true;
            thFile.Start();
        }
        /// <summary>
        /// 发送文件
        /// </summary>
        private void SendFile()
        {
            Socket socket;
            try
            {
                string IP = textBox3.Text;
                IPAddress ip = IPAddress.Parse(IP);
                IPEndPoint ipe = new IPEndPoint(ip, int.Parse(mainFrm.textBox5.Text));
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ipe);
            }
            catch (Exception)
            {
                MessageBox.Show("无法连接对方主机");
                return;
            }
            FileStream fsRead;             //打开文件
            try
            {
                fsRead = new FileStream(textBox1.Text, FileMode.Open);
            }
            catch (Exception)
            {
                MessageBox.Show("无法发送此文件");
                socket.Close();
                return;
            }
            //修改状态
            mainFrm.button2.Enabled = false;
            mainFrm.label4.Text = "发送中";

            long maxFile = fsRead.Length;       //文件大小
            //显示文件数据
            mainFrm.richTextBox1.AppendText("文件名：" + System.IO.Path.GetFileName(textBox1.Text) + "\r\n");
            if (maxFile / 1024 / 1024 >= 1024)
            {
                mainFrm.richTextBox1.AppendText("文件大小：" + ((double)(maxFile / 1024 / 1024 / 1024)).ToString() + "Gb\r\n");
            }
            else if (maxFile / 1024 >= 1024)
            {
                mainFrm.richTextBox1.AppendText("文件大小：" + ((double)(maxFile / 1024 / 1024)).ToString() + "Mb\r\n");
            }
            else
            {
                mainFrm.richTextBox1.AppendText("文件大小：" + ((double)(maxFile / 1024)).ToString() + "KB\r\n");
            }

            socket.Send(Encoding.Unicode.GetBytes(System.IO.Path.GetFileName(textBox1.Text)));   //文件名字
            Thread.Sleep(500);
            
            socket.Send(Encoding.Unicode.GetBytes(maxFile.ToString()));     //发送文件大小
            Thread.Sleep(500);

            long leftLength = fsRead.Length;
            //创建接收文件内容的字节数组
            byte[] buffer = new byte[4096];
            //每次读取的最大字节数
            long maxLength = buffer.Length;
            //每次实际返回的字节数长度
            long num = 0;
            //文件开始读取的位置
            long fileStart = 0;
            int flag = 0;
            int count = 0;

            while (leftLength > 0)
            {
                try
                {
                    //设置文件流的读取位置
                    fsRead.Position = fileStart;
                    num = fsRead.Read(buffer, 0, 4096);
                    if (num == 0)
                    {
                        break;
                    }
                    fileStart += num;
                    leftLength -= num;
                    socket.Send(buffer, (int)num, 0);

                    flag++;
                    count++;
                    if (flag == 10240*2)         //每40Mb更新进度条
                    {
                        float x = (float)(count *4/1024) / (maxFile/1024/1024);        //此处会数值溢出
                        mainFrm.progressBar1.Value = (int)(x * 100);
                        flag = 0;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("传输过程中出错");
                    break;
                }
            }
            mainFrm.progressBar1.Value = 0;
            socket.Close();
            mainFrm.label4.Text = "空闲";
            mainFrm.button2.Enabled = true;
            fsRead.Close();


        }
    }
}
