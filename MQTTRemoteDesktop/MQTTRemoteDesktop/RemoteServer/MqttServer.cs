using RemoteModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using System.Collections.Concurrent;
namespace RemoteServer
{
    public class MqttServer
    {
        public ConcurrentDictionary<string, MClient> Clients = new ConcurrentDictionary<string, MClient>();
        public ConcurrentDictionary<string, string> Commids = new ConcurrentDictionary<string, string>();
        public MQTTMan MqttClient;
        public MqttServer(string CaCertPath, string ServerIP, string ServerPort, string ServerEui)
        {
            X509Certificate CaCert = X509Certificate.CreateFromCertFile(CaCertPath); //注意证书文件不存在会导致相关Man初始化失败
            MqttClient = new MQTTMan(ServerEui, ServerEui, ServerIP, int.Parse(ServerPort), CaCert, MqttSslProtocols.TLSv1_2);
            MqttClient.Start();
            MqttClient.RcvDataEvent += MqttClient_RcvDataEvent;
        }

        private void MqttClient_RcvDataEvent(byte[] data)
        {
            try
            {
                switch (data[0])
                {
                    case 0x00:
                        byte[] Devid = data.SubArray(1, 16);
                        byte[] Commid = data.SubArray(17, 4);
                        string pwd = data.SubArray(22, data[21]).ToASCIIString();
                        MClient mc = Clients.GetOrAdd(Devid.ToHexString(), (k) =>
                        {
                            MClient m = new MClient();
                            m.Devid = Devid;
                            byte[] commid = Utils.RandomBytes(4);
                            if (Commids.ContainsKey(commid.ToHexString()))
                            {
                                commid = Utils.RandomBytes(4);
                            }
                            Commids.TryAdd(commid.ToHexString(), Devid.ToHexString());
                            m.Commid = commid;
                            if (string.IsNullOrEmpty(pwd))
                                pwd = Utils.RandomPassword();
                            m.Password = pwd;
                            return m;
                        });
                        mc.LastRcvTime = DateTime.Now;
                        if (!string.IsNullOrEmpty(pwd) && pwd != mc.Password) mc.Password = pwd;
                        MqttClient.Request(Devid.ToHexString(), Package_0x00(mc.Commid, mc.Password));
                        break;
                    case 0x01:
                        string commidx = data.SubArray(5, 4).ToHexString();
                        if (Commids.TryGetValue(commidx, out string dev) && Clients.TryGetValue(dev, out MClient mx) && (DateTime.Now - mx.LastRcvTime).TotalSeconds < 5)
                            MqttClient.Request(data.SubArray(1, 4).GetInt().ToString(), Package_0x01(0));
                        else
                            MqttClient.Request(data.SubArray(1, 4).GetInt().ToString(), Package_0x01(1));
                        break;

                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message + "******" + ex.StackTrace); }
        }

        public byte[] Package_0x00(byte[] Commid, string pwd)
        {
            List<byte> lb = new List<byte>();
            lb.Add(0x00);
            lb.AddRange(Commid);
            byte[] pwds = pwd.ToAsciiArray();
            lb.Add((byte)pwds.Length);
            lb.AddRange(pwds);
            return lb.ToArray();
        }

        public byte[] Package_0x01(byte flag)
        {
            List<byte> lb = new List<byte>();
            lb.Add(0x01);
            lb.Add(flag);
            return lb.ToArray();
        }

    }
    public class MClient
    {
        public DateTime LastRcvTime { get; set; }

        public byte[] Devid { get; set; }

        public byte[] Commid { get; set; }

        public string Password { get; set; }
    }
}
