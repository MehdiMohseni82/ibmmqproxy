using System;
using System.Collections.Generic;
using System.Text;

namespace IBMMqClientApp.Events
{
    public enum Status
    {
        Connected,
        Disconnected
    }

    public class ConnectionStatus
    {
        public ConnectionStatus(string queueManager, string topicQueueName, Status status)
        {
            Status = status;
            QueueManager = queueManager;
            TopicQueueName = topicQueueName;
        }

        public Status Status { get; set; }

        public string QueueManager { get; set; }

        public string TopicQueueName { get; set; }

        public override string ToString()
        {
            return $"IBM MQ Proxy Connection Status Changed, Status: {Status}, QueueManager: {QueueManager}, TopicQueueName: {TopicQueueName}.";
        }
    }
}
