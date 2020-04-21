using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
namespace RemoteServer
{
    class Program
    {
        static void Main(string[] args)
        {
            string CaCertPath = ConfigurationManager.AppSettings["CaCertPath"];
            string ServerIP = ConfigurationManager.AppSettings["ServerIP"];
            string ServerPort = ConfigurationManager.AppSettings["ServerPort"];
            string ServerEui = ConfigurationManager.AppSettings["ServerEui"];
            MqttServer mqttServer = new MqttServer(CaCertPath, ServerIP, ServerPort, ServerEui);
            Console.ReadLine();
        }
    }
}
