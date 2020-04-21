using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace RemoteModel
{
    public class MQTTMan : MQTTManBase
    {
        private string ClientEui;

        public delegate void RcvDataHandle(byte[] data);
        public event RcvDataHandle RcvDataEvent;
        public MQTTMan(string ClientEui, string ServerEui, string brokerHostName, int brokerPort, X509Certificate caCert, MqttSslProtocols protocols)
            : base(ServerEui, brokerHostName, brokerPort, caCert, protocols)
        {
            this.ClientEui = ClientEui;
        }
        /// <summary>
        /// 连接MQTT Broker，每次连接都会重新订阅所有网关的上行主题
        /// </summary>
        public override void Connect()
        {
            base.Connect();
            Subscribe(ClientEui); //订阅自己的主题
        }
        /// <summary>
        /// 接收函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            RcvDataEvent?.BeginInvoke(e.Message, null, null);
        }
    }
}

