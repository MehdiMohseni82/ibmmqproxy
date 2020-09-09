using IBM.WMQ;
using IBMMqClientApp.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace IBMMqClientApp
{
    public class IbmMqProxy
    {
        #region Fields

        private Hashtable _queueManagerProperties = null;
        private readonly IbmMqProxyConnectionConfiguration _configuration = null;
        
        #endregion

        #region Events

        public delegate void MessageReceivedHandler(MQMessage message);

        public delegate void ConnectionStatusChangedHandler(Events.ConnectionStatus connectionStatus);

        #endregion

        #region Properties

        public ILogger Logger { get; set; }

        #endregion

        public IbmMqProxy(IbmMqProxyConnectionConfiguration configuration)
        {
            _configuration = configuration;
            InitializeQueueManagerConnectionProperties();
        }

        public bool SendMessageToQueue(string queueManagerName, string queueString, object objToSend)
        {
            try
            {
                var hearBeatConfirmationText = JsonConvert.SerializeObject(objToSend);

                var queueManager = new MQQueueManager(queueManagerName, _queueManagerProperties);
                var outboundTopic = queueManager.AccessQueue(queueString, MQC.MQOO_INPUT_AS_Q_DEF + MQC.MQOO_OUTPUT + MQC.MQOO_FAIL_IF_QUIESCING);

                var msg = new MQMessage { Persistence = MQC.MQPER_PERSISTENCE_AS_TOPIC_DEF };
                msg.WriteString(hearBeatConfirmationText);
                outboundTopic.Put(msg);

                return true;
            }
            catch (Exception e)
            {
                Logger?.LogError(e, $"Error in sending message to the queue {queueString}.");
                return false;
            }
        }

        public Task ListenToTopic(string queueManagerName, string topicString, MessageReceivedHandler messageHandler, ConnectionStatusChangedHandler connectionStatusChangedHandler)
        {
            return ListenToMq(queueManagerName, topicString, false, messageHandler, connectionStatusChangedHandler);
        }

        public Task ListenToQueue(string queueManagerName, string topicString, MessageReceivedHandler messageHandler, ConnectionStatusChangedHandler connectionStatusChangedHandler)
        {
            return ListenToMq(queueManagerName, topicString, true, messageHandler, connectionStatusChangedHandler);
        }

        private Task ListenToMq(string queueManagerName, string topicOrQueueString, bool isQueue, MessageReceivedHandler messageHandler, ConnectionStatusChangedHandler connectionStatusChangedHandler)
        {
            var openOptionsForGet = MQC.MQSO_CREATE | MQC.MQSO_FAIL_IF_QUIESCING | MQC.MQSO_MANAGED | MQC.MQSO_NON_DURABLE;
            MQDestination inboundDestination = null;

            var gmo = new MQGetMessageOptions();
            gmo.Options |= MQC.MQGMO_NO_WAIT | MQC.MQGMO_FAIL_IF_QUIESCING;

            try
            {
                var queueManager = new MQQueueManager(queueManagerName, _queueManagerProperties);

                if (isQueue)
                {
                    inboundDestination = queueManager.AccessQueue(topicOrQueueString, MQC.MQTOPIC_OPEN_AS_SUBSCRIPTION);
                }
                else
                {
                    inboundDestination = queueManager.AccessTopic(topicOrQueueString, null, MQC.MQTOPIC_OPEN_AS_SUBSCRIPTION, openOptionsForGet);
                }

                connectionStatusChangedHandler(new ConnectionStatus(queueManagerName, topicOrQueueString, Status.Connected));

                while (true)
                {
                    try
                    {
                        var msg = new MQMessage();
                        inboundDestination.Get(msg, gmo);

                        messageHandler(msg);
                    }
                    catch (MQException mqException)
                    {
                        if (mqException.Reason == MQC.MQRC_NO_MSG_AVAILABLE)
                        {
                            Thread.Sleep(200);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            catch (MQException mqException)
            {
                connectionStatusChangedHandler(new ConnectionStatus(queueManagerName, topicOrQueueString, Status.Disconnected));
            }

            return Task.CompletedTask;
        }

        private void InitializeQueueManagerConnectionProperties()
        {
            _queueManagerProperties = new Hashtable
            {
                { MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED },
                { MQC.HOST_NAME_PROPERTY, _configuration.HostName },
                { MQC.PORT_PROPERTY, _configuration.Port },
                { MQC.CHANNEL_PROPERTY, _configuration.Channel },
                { MQC.USER_ID_PROPERTY, _configuration.UserId },
                { MQC.PASSWORD_PROPERTY, _configuration.Password }
            };
        }
    }
}