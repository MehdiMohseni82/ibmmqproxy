using System;
using System.Threading.Tasks;
using IBM.WMQ;
using IBMMqClientApp;
using IBMMqClientApp.Events;

namespace DotNetTalk.IbmMqProxy.Console
{
    public class MessageToSend
    {
        public string Message { get; set; }

        public int Number { get; set; }
    }

    class Program
    {
        private static IBMMqClientApp.IbmMqProxy _ibmMqProxy;

        static void Main(string[] args)
        {
            var connection = new IbmMqProxyConnectionConfiguration
            {
                UserId = "app",
                Password = "passw0rd",
                Port = 1414,
                Channel = "DEV.APP.SVRCONN",
                HostName = "localhost(1414)"
            };

            _ibmMqProxy = new IBMMqClientApp.IbmMqProxy(connection);

            var msgToSend = new MessageToSend
            {
                Message = "This is a test",
                Number = 255
            };

            _ibmMqProxy.SendMessageToQueue("QM1", "DEV.QUEUE.1", msgToSend);

            Task.Run(() => _ibmMqProxy.ListenToTopic("QM1", "DEV.QUEUE.2", MessageHandler, ConnectionStatusChangedHandler));

            while (true) { }
        }

        private static void ConnectionStatusChangedHandler(ConnectionStatus connectionStatus)
        {
            System.Console.WriteLine(connectionStatus);
        }

        private static void MessageHandler(MQMessage message)
        {
            var msg = message.ReadString(message.DataLength);
            System.Console.WriteLine(msg);
        }
    }
}