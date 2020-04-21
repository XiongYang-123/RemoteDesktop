using RemoteModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;

namespace RemoteClient
{
    public class DesktopClient
    {
        public string CaCertPath { get; set; }

        public MQTTMan MqttClient { get; set; }

        public byte[] ClientEui { get; set; }

        public string ServerEui { get; set; }

        public string ServerIP { get; set; }

        public string ServerPort { get; set; }

        public byte[] CommID { get; set; } = new byte[4];

        public byte[] OtherCommid { get; set; } = new byte[4];

        public string OtherCommidStr { get; set; }

        public bool IsCompression { get; set; } = true;

        public int CompressionLevel { get; set; } = 50;

        public string OtherPwd { get; set; }
        public string Password { get; set; }
        /// <summary>
        /// 本设备状态 0待定 1 客户端 2 控制端
        /// </summary>
        public int Status { get; set; }
        /// <summary>
        /// 会话是否开启
        /// </summary>
        public bool IsSession { get; set; }

        public delegate void SelectServerHandle(byte b);
        /// <summary>
        /// 查询服务器对方是否在线回复 0x00成功  0x01设备不线  0x02 密码错误
        /// </summary>
        public event SelectServerHandle SelectServerEvent;

        public delegate void RcvImageHandle(Bitmap bitmap);
        /// <summary>
        /// 图象接收事件
        /// </summary>
        public event RcvImageHandle RcvImageEvent;

        public delegate void RcvKeyHandle(byte key, byte flag);
        /// <summary>
        /// 按键事件
        /// </summary>
        public event RcvKeyHandle RcvKeyEvent;

        public delegate void MouseMoveHandle(int type,int x, int y,int dw,int info);
        /// <summary>
        /// 鼠标移动事件
        /// </summary>
        public event MouseMoveHandle MouseMoveEvent;


        public delegate void Conn2Handle(byte b);
        public event Conn2Handle Conn2Event;

        public delegate void Conn1Handle();
        public event Conn1Handle Conn1Event;

        public bool IsRunning = false;

        /// <summary>
        /// 客户端初始化
        /// </summary>
        /// <param name="CaCertPath">客户端证书</param>
        /// <param name="ClientEui">客户端自己的Eui</param>
        /// <param name="ServerEui">服务器Mqtt Eui</param>
        /// <param name="ServerIP">MQTT ip</param>
        /// <param name="ServerPort">MQTT 服务器端口</param>
        public DesktopClient(string CaCertPath, byte[] ClientEui, string ServerEui, string ServerIP, string ServerPort)
        {
            this.CaCertPath = CaCertPath;
            this.ClientEui = ClientEui;
            this.ServerEui = ServerEui;
            this.ServerIP = ServerIP;
            this.ServerPort = ServerPort;
        }
        public void Start()
        {
            IsRunning = true;
            Console.WriteLine("*************服务器开始启动");
            try
            {
                X509Certificate CaCert = X509Certificate.CreateFromCertFile(CaCertPath); //注意证书文件不存在会导致相关Man初始化失败
                MqttClient = new MQTTMan(ClientEui.ToHexString(), ServerEui, ServerIP, int.Parse(ServerPort), CaCert, MqttSslProtocols.TLSv1_2);
                MqttClient.Start();
                MqttClient.RcvDataEvent += MqttClient_RcvDataEvent;

                Task.Run(() =>
                {
                    while (IsRunning)
                    {
                        try
                        {
                            MqttClient?.Request(Package_0x00());
                            Thread.Sleep(1000);
                        }
                        catch { }
                    }
                });
                Task.Run(() =>
                {
                    while (IsRunning)
                    {
                        try
                        {
                            if (Status == 1 && IsSession)
                            {
                                MqttClient?.Request(OtherCommidStr, Package_0x02(GetImage()));
                            }
                            Thread.Sleep(100);
                        }
                        catch { }
                    }
                });
            }
            catch (Exception ex) { Console.WriteLine(ex + " ca:" + CaCertPath); }
            Console.WriteLine("*************服务器启动完成");
        }

        public void Close()
        {
            IsRunning = false;
            MqttClient.Dispose();
        }
        private void MqttClient_RcvDataEvent(byte[] data)
        {
            try
            {
                if (data?.Length > 0)
                {
                    switch (data[0])
                    {
                        case 0x00:
                            if (CommID.ToHexString() != data.SubArray(1, 4).ToHexString())
                            {
                                MqttClient.Unsubscribe(CommID.GetInt().ToString());
                                MqttClient.Subscribe(data.SubArray(1, 4).GetInt().ToString());
                            }
                            CommID = data.SubArray(1, 4);
                            Password = data.SubArray(6, data[5]).ToASCIIString();
                            break;
                        case 0x01:
                            SelectServerEvent?.BeginInvoke(data[1], null, null);
                            break;
                        case 0x02:
                            RcvImageEvent?.BeginInvoke(data.SubArray(1, data.Length - 1).Bytes2BitMap(), null, null);
                            break;
                        case 0x03:
                            IsCompression = data[1] == 1;
                            CompressionLevel = data[2];
                            break;
                        case 0x04:
                            IsSession = false;
                            Status = 0;
                            break;
                        case 0x05:
                            RcvKeyEvent?.BeginInvoke(data[1], data[2], null, null);
                            break;
                        case 0x06:
                            int type= (data[1] << 8) | data[2];
                            int x = (data[3] << 8) | data[4];
                            int y = (data[5] << 8) | data[6];
                            int dw= (data[7] << 24) | (data[8] << 16) | (data[9] << 8) | data[10];
                            int info = (data[11] << 24) | (data[12] << 16) | (data[13] << 8) | data[14];

                            MouseMoveEvent?.BeginInvoke(type,x, y,dw,info, null, null);
                            break;
                        case 0x07:
                            if (data.Length > 2)
                            {
                                string pwd = data.SubArray(6, data[5]).ToASCIIString();
                                OtherCommid = data.SubArray(1, 4);
                                OtherCommidStr = OtherCommid.GetInt().ToString();
                                if (pwd == Password)
                                {
                                    MqttClient?.Request(OtherCommidStr, new byte[] { 0x07, 0x00 });
                                    IsSession = true;
                                    Status = 1;
                                    Conn1Event?.BeginInvoke(null, null);
                                }
                                else MqttClient?.Request(OtherCommidStr, new byte[] { 0x07, 0x01 });

                            }
                            else
                            {
                                if (data[1] == 0x00)
                                {
                                    Status = 2;
                                    IsSession = true;
                                }
                                Conn2Event?.Invoke(data[1]);
                            }
                            break;

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "****" + ex.StackTrace);
            }
        }

        #region 请求数据包
        /// <summary>
        /// 心跳帧
        /// </summary>
        /// <returns></returns>
        public byte[] Package_0x00()
        {
            List<byte> lb = new List<byte>();
            lb.Add(0x00);
            lb.AddRange(ClientEui);
            lb.AddRange(CommID);
            if (Password?.Length > 0)
            {
                byte[] pwd = Password.ToAsciiArray();
                lb.Add((byte)pwd.Length);
                lb.AddRange(pwd);
            }
            else lb.Add(0x00);
            return lb.ToArray();
        }
        /// <summary>
        /// 开启会话帧
        /// </summary>
        /// <param name="otherCommid"></param>
        /// <param name="otherPwd"></param>
        /// <returns></returns>

        public byte[] Package_0x01(byte[] otherCommid)
        {
            List<byte> lb = new List<byte>();
            lb.Add(0x01);
            lb.AddRange(CommID);
            lb.AddRange(otherCommid);
            return lb.ToArray();
        }
        /// <summary>
        /// 图象传递帧
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public byte[] Package_0x02(Bitmap image)
        {
            List<byte> lb = new List<byte>();
            lb.Add(0x02);
            if (!IsCompression)
                lb.AddRange(image.Bitmap2Bytes());
            else lb.AddRange(image.Compress(CompressionLevel));
           Console.WriteLine($"Img Lenght:"+lb.Count);
            return lb.ToArray();
        }
        /// <summary>
        /// 图形压缩度帧
        /// </summary>
        /// <returns></returns>
        public byte[] Package_0x03(bool flag, int level)
        {
            List<byte> lb = new List<byte>();
            lb.Add(0x03);
            lb.Add((byte)(flag ? 0x01 : 0x00));
            lb.Add((byte)level);
            return lb.ToArray();
        }
        /// <summary>
        ///退出会话帧
        /// </summary>
        /// <returns></returns>
        public byte[] Package_0x04()
        {
            return new byte[] { 0x04 };
        }
        /// <summary>
        /// 键盘输入
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Down"></param>
        /// <returns></returns>
        public byte[] Package_0x05(byte Key, byte Down)
        {
            return new byte[] { 0x05, Key, Down };
        }
        /// <summary>
        /// 鼠标输入
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public byte[] Package_0x06(int type, int x, int y,int dw,int info)
        {
            return new byte[] { 0x06, (byte)(type >> 8), (byte)(type & 0xff),(byte)(x >> 8), (byte)(x & 0xff), (byte)(y >> 8), (byte)(y & 0xff),
                (byte)((dw>>24)&0xff), (byte)((dw >> 16) & 0xff), (byte)((dw >> 8) & 0xff), (byte)(dw & 0xff),
                (byte)((info >> 24) & 0xff), (byte)((info >> 16) & 0xff), (byte)((info >> 8) & 0xff), (byte)(info & 0xff) };
        }

        public byte[] Package_0x07(string pwd)
        {
            List<byte> lb = new List<byte>();
            lb.Add(0x07);
            lb.AddRange(CommID);
            byte[] pwds = pwd.ToAsciiArray();
            lb.Add((byte)pwds.Length);
            lb.AddRange(pwds);
            return lb.ToArray();
        }
        #endregion

        private Bitmap GetImage()
        {
            //屏幕宽
            int iWidth = Screen.PrimaryScreen.Bounds.Width;
            //屏幕高
            int iHeight = Screen.PrimaryScreen.Bounds.Height;
            //按照屏幕宽高创建位图
            Bitmap img = new Bitmap(iWidth, iHeight);
            //从一个继承自Image类的对象中创建Graphics对象
            Graphics gc = Graphics.FromImage(img);
            //抓屏并拷贝到myimage里
            gc.CopyFromScreen(new Point(0, 0), new Point(0, 0), new Size(iWidth, iHeight));
            //this.BackgroundImage = img;
            //保存位图
            return img;
        }

        /// <summary>
        /// 查询服务器在线状态
        /// </summary>
        /// <param name="otherid"></param>
        public void Check(uint otherid)
        {
            MqttClient?.Request(Package_0x01(otherid.GetBytes()));
            OtherCommid = otherid.GetBytes();
            OtherCommidStr = otherid.ToString();
        }
        /// <summary>
        /// 发起请求
        /// </summary>
        /// <param name="pwd"></param>
        public void Conn(string pwd)
        {
            MqttClient?.Request(OtherCommidStr, Package_0x07(pwd));
        }
        /// <summary>
        /// 发送远程按键
        /// </summary>
        /// <param name="key"></param>
        /// <param name="flag"></param>
        public void SendKey(byte key, byte flag)
        {
            if (IsSession && Status == 2)
            {
                MqttClient?.Request(OtherCommidStr, Package_0x05(key, flag));
            }
        }
        /// <summary>
        /// 鼠标移动事件
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void SendMouseMove(int type,int x, int y,int dw ,int info)
        {
            if (IsSession && Status == 2)
            {
                MqttClient?.Request(OtherCommidStr, Package_0x06(type,x, y,dw,info));
            }
        }
        /// <summary>
        /// 退出会话
        /// </summary>
        public void SessionClose()
        {
            if (IsSession && Status == 2)
            {
                MqttClient?.Request(OtherCommidStr, Package_0x04());
                Status = 0;
                IsSession = false;
            }
        }
        /// <summary>
        /// 设置压缩率
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="level"></param>
        public void SetCompressLevel(bool flag, int level)
        {
            if (IsSession && Status == 2)
            {
                MqttClient?.Request(OtherCommidStr, Package_0x03(flag, level));
            }
        }



    }
}
