using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RemoteClient;
using RemoteModel;

namespace RemoteDesktop
{
    public partial class frmFirst : Form
    {
        public DesktopClient Client { get; set; }
        public frmFirst()
        {
            InitializeComponent();
            string CaCertPath = ConfigurationManager.AppSettings["CaCertPath"];
            string ServerIP = ConfigurationManager.AppSettings["ServerIP"];
            string ServerPort = ConfigurationManager.AppSettings["ServerPort"];
            string ServerEui = ConfigurationManager.AppSettings["ServerEui"];
            Client = new DesktopClient(CaCertPath, ReaderPc.GetCpuID(), ServerEui, ServerIP, ServerPort);
            Client.Start();
            Client.SelectServerEvent += Client_SelectServerEvent;
            Client.Conn1Event += Client_Conn1Event;
            Client.Conn2Event += Client_Conn2Event;
            Client.RcvKeyEvent += Client_RcvKeyEvent;
            Client.MouseMoveEvent += Client_MouseMoveEvent;
        }

        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        //移动鼠标 
        const int MOUSEEVENTF_MOVE = 0x0001;
        //模拟鼠标左键按下 
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        //模拟鼠标左键抬起 
        const int MOUSEEVENTF_LEFTUP = 0x0004;
        //模拟鼠标右键按下 
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        //模拟鼠标右键抬起 
        const int MOUSEEVENTF_RIGHTUP = 0x0010;
        //模拟鼠标中键按下 
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        //模拟鼠标中键抬起 
        const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        //标示是否采用绝对坐标 
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        //模拟鼠标滚轮滚动操作，必须配合dwData参数
        const int MOUSEEVENTF_WHEEL = 0x0800;

        private void Client_MouseMoveEvent(int type,int x, int y,int dw ,int info)
        {
            Console.WriteLine($"X:{x},Y:{y}");
            mouse_event(type|MOUSEEVENTF_ABSOLUTE, x * 65535 / Screen.PrimaryScreen.Bounds.Width, y * 65535 / Screen.PrimaryScreen.Bounds.Height, dw, info);
        }

        [DllImport("user32.dll", EntryPoint = "keybd_event", SetLastError = true)]
        public static extern void keybd_event(Keys bVk, byte bScan, uint dwFlags, uint dwExtraInfo);
        private void Client_RcvKeyEvent(byte key, byte flag)
        {
            keybd_event((Keys)key, 0, (uint)(flag == 0 ? 0 : 2), 0);
        }

        private void Client_Conn2Event(byte b)
        {
            if (b == 0x00)
                this.Invoke((Action)delegate
                {
                    this.Hide();
                    new frmMain(Client,this).Show();
                });
        }

        private void Client_Conn1Event()
        {
            MessageBox.Show("正在远程控制。。。");
        }

        private void Client_SelectServerEvent(byte b)
        {
            if (b == 0x00)
                new frmPwd(Client).ShowDialog();
            else MessageBox.Show("设备未找到或者设备未上线");
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            lb_id.Text = Client.CommID.GetInt().ToString();
            tb_pwd.Text = Client.Password;
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            if (uint.TryParse(textBox1.Text.Trim(), out uint i))
            {
                Client.Check(i);
            }
        }
        private void FrmFirst_FormClosing(object sender, FormClosingEventArgs e)
        {
            Client.SessionClose();
            Client.Close();
        }
    }
}
