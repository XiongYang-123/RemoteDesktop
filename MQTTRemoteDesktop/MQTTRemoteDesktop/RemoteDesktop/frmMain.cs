using RemoteClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteDesktop
{
    public partial class frmMain : Form
    {
        DesktopClient client;
        frmFirst ff;
        public frmMain(DesktopClient client,frmFirst ff)
        {
            InitializeComponent();
            this.client = client;
            client.RcvImageEvent += Client_RcvImageEvent;
            this.ff = ff;
        }

        private void Client_RcvImageEvent(Bitmap bitmap)
        {
            this.Invoke((Action)delegate
            {
                pictureBox1.Image = bitmap;
            });
        }

        private void FrmMain_KeyDown(object sender, KeyEventArgs e)
        {
            client.SendKey((byte)e.KeyValue, 0);
        }

        private void FrmMain_KeyUp(object sender, KeyEventArgs e)
        {
            client.SendKey((byte)e.KeyValue, 1);
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            int type = 0x00;
            if (e.Button == MouseButtons.Left)
                type = 0x0002;
            else if (e.Button == MouseButtons.Right)
                type = 0x0008;
            else if (e.Button == MouseButtons.Middle)
                type = 0x0020;
            client.SendMouseMove(type, e.X, e.Y, 0, 0);
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            int type = 0x00;
            if (e.Button == MouseButtons.Left)
                type = 0x0004;
            else if (e.Button == MouseButtons.Right)
                type = 0x0010;
            else if (e.Button == MouseButtons.Middle)
                type = 0x0040;
            client.SendMouseMove(type, e.X, e.Y, 0, 0);
        }

        DateTime LastRcvTime = DateTime.Now;
        int x = 0; int y = 0;

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            x = e.X;
            y = e.Y;
            if ((DateTime.Now - LastRcvTime).TotalMilliseconds >= 500)
            {
                Console.WriteLine($"X:{e.X},Y:{e.Y}");
                client.SendMouseMove(0x0001, e.X, e.Y, 0, 0);
                LastRcvTime = DateTime.Now;
                timer1.Enabled = true;
            }

        }

        private void Button1_Click(object sender, EventArgs e)
        {
            client.SetCompressLevel(true, trackBar1.Value);
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            client.SessionClose();
            ff.Show();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if ((DateTime.Now - LastRcvTime).TotalMilliseconds >= 500)
            {
                client.SendMouseMove(0x0001, x, y, 0, 0);
                LastRcvTime = DateTime.Now;
            }
        }
    }
}
