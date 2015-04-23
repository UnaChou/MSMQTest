using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSMQTest.Model
{
    public class MQConfig
    {
        public MqSetting from { get; set; }
        public List<MqSetting> opAdd { get; set; }
        public List<MqSetting> opUpdate { get; set; }
        public List<MqSetting> opDelete { get; set; }
    }

    public class MqSetting
    {
        public string ip { get; set; }
        public string qname { get; set; }
        public string label { get; set; }
        public bool compress { get; set; }
        public MQSendMode sendmode { get; set; }
        public bool enable { get; set; }
    }

    public enum MQSendMode
    {
        Loadbalance,
        Broadcast,
        Backup
    }
}
