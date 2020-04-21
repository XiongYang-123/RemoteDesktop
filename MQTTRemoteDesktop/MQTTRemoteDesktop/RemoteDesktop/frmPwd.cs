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
    public partial class frmPwd : Form
    {
        DesktopClient client;
        public frmPwd(DesktopClient client)
        {
            InitializeComponent();
            this.client = client;
            client.Conn2Event += Client_Conn2Event;

        }

        private void Client_Conn2Event(byte b)
        {
            this.Invoke((Action)delegate
            {
                if (b == 0x01) MessageBox.Show("密码错误");
                else this.Close();
            });
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text.Trim()))
                client.Conn(textBox1.Text.Trim());
        }
    }
}
