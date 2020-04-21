using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace RemoteModel
{
    public class MQTTManBase
    {
        protected MqttClient _client = null;
        public string ServerId = "";
        /// <summary>
        /// 此通讯代理相关的服务器角色

        public MQTTManBase(string ServerEui, string brokerHostName, int brokerPort, X509Certificate caCert, MqttSslProtocols protocols)
        {
            RemoteCertificateValidationCallback certValidationCallBack = RemoteCertificateValidate;
            _client = new MqttClient(brokerHostName, brokerPort, caCert != null, caCert, null, protocols, certValidationCallBack);
            //register to message received 
            _client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            _client.ConnectionClosed += Client_ConnectionClosed;
            ServerId = ServerEui;
        }

        public MqttClient.MqttMsgPublishEventHandler PublishedEventHandler { get; set; }

        public void Start()
        {
            if (!_client.IsConnected) this.Connect();

        }

        public void Stop()
        {
            if (_client != null && _client.IsConnected)
            {
                _client.Disconnect();
            }
        }

        public void Dispose()
        {
            try
            {
                this.Stop();
            }
            catch (Exception ex) { }
            Thread.Sleep(1000);
            if (_client.IsConnected == false) _client = null;
        }

        public void Request(string eui, byte[] data)
        {
            this.Publish(eui, data);
        }
        public void Request(byte[] data)
        {
            this.Publish(ServerId, data);
        }
        public bool IsConnected
        {
            get
            {
                if (_client != null) return _client.IsConnected;
                else return false;
            }
        }
        public string ClientID
        {
            get
            {
                return _client?.ClientId;
            }
        }
        /// <summary>
        /// 连接MQTT Broker，每次连接都会重新订阅所有网关的上行主题
        /// </summary>
        public virtual void Connect()
        {
            string clientId = Guid.NewGuid().ToString();
            try
            {
                _client.Connect(clientId);
            }
            catch (Exception ex) { }
        }

        protected bool RemoteCertificateValidate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            //解决自签名证书无法成功验证，总是返回true
            return true;
        }

        public virtual void Subscribe(string topic)
        {
            // subscribe to the topic "test" with QoS 2 
            if (_client != null) _client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });

        }

        public virtual void Unsubscribe(string topic)
        {
            if (_client != null) _client.Unsubscribe(new string[] { topic });

        }
        /// <summary>
        /// 异步发布字节数组
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="strValue"></param>
        public virtual void Publish(string topic, byte[] bytes)
        {
            if (_client != null && topic != null && bytes != null) _client.Publish(topic, bytes, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }
        protected virtual void Client_ConnectionClosed(object sender, EventArgs e)
        {
            //断线重连
            while (_client != null && !this.IsConnected)
            {
                Thread.Sleep(1000);
                this.Connect();
            }
        }

        protected virtual void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {


        }


    }
}

