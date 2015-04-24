using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace MQWeb
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
           
        }

         public MessageQueue mq;

        //自訂訊息內容(要發送/接收的資料格式)
        public class MyData
        {
            public string text;
            public DateTime now;
            public double unm
            { get; set; }
        }    

        public void Createqueue(string queuePath)
        {
            try
            {
                if (!MessageQueue.Exists(queuePath))
                {
                    MessageQueue.Create(queuePath);
                }
                else
                {
                    Console.WriteLine("que exist.");
                }
            }
            catch (MessageQueueException e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        public void SendMessage(string QueuePath)
        {
            try
            {
                //MessageQueue myQueue = new MessageQueue(".\\private$\\myQueue");
                //System.Messaging.Message myMessage = new System.Messaging.Message();            
                //myMessage.Body = "消息内容";

                //myMessage.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });
                //myQueue.Send(myMessage);

                ////建立一個MSMQ
                //if (!MessageQueue.Exists(QueuePath))
                //{
                //    MessageQueue.Create(QueuePath);
                //}
                ////將MessageQueue物件指向 指定的MSMQ
                //mq = new MessageQueue(QueuePath);
                //mq.Formatter = new System.Messaging.XmlMessageFormatter();
                ////mq.ReceiveCompleted += MQ_ReceiveCompleted;
                //mq.Refresh();

                //mq.Send(myMessage);

                //string queuePath = @"FormatName:DIRECT=TCP:192.168.1.1\private$\myqueue";// 使用遠程IP指定訊息佇列位置
                string queuePath = @".\private$\myqueue";//使用本機方式指定訊息佇列位置

                if (!MessageQueue.Exists(queuePath))//判斷 myqueue訊息佇列是否存在
                {
                    MessageQueue.Create(queuePath);//建立用來接受/發送的訊息佇列
                }
                MessageQueue myQueue = new MessageQueue(queuePath);

                //要發送的內容
                MyData data = new MyData();
                data.text = "Holle";
                data.now = DateTime.Now;
                data.unm = DateTime.Now.Second;

                //發送訊息
                myQueue.Send(data, "MY--"+DateTime.Now.ToShortDateString());
              
            }
            catch (MessageQueueException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        protected void btnRec_Click(object sender, EventArgs e)
        {
            string queuePath = @".\private$\myqueue";//使用本機方式指定訊息佇列位置
            MessageQueue myQueue = new MessageQueue(queuePath);

            myQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(MyData) });//設定接收訊息內容的型別
            System.Messaging.Message message = myQueue.Receive();//接收訊息佇列內的訊息
            MyData data = (MyData)message.Body;//將訊息內容轉成正確型別

            txt.Text += data;
        }

        protected void btnSend_Click(object sender, EventArgs e)
        {
            Createqueue(".\\private$\\myQueue");
            SendMessage(".\\private$\\myQueue");
        }
    }
}