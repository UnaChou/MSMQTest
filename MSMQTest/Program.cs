using MQWForm.Model;
using Serilog;
using SimpleMsmqLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Messaging;
using System.Configuration;
using System.Threading;
using Newtonsoft.Json;
using System.IO;

namespace MQWForm
{
    static class Program
    {
        static string configFilePath = System.AppDomain.CurrentDomain.BaseDirectory + "MQBroker_Config.json";
        static MQConfig config = new MQConfig();
        static object configlock = new object(); //更新 config 時的鎖定物件
        //static int sleepMilliseconds = 10000; //停止時間
        static Dictionary<string, SimpleMsmq<MainProduct.Savebar.PMEDataModel>> mqDic = new Dictionary<string, SimpleMsmq<MainProduct.Savebar.PMEDataModel>>();

        //static CounterHelper<MQBroker_PerfCounterTypes> counterHelper = PerformanceHelper.CreateCounterHelper<MQBroker_PerfCounterTypes>();

        static ILogger ExceptionLogger = null;

        static void Main(string[] args)
        {
            var seqSrv = ConfigurationManager.AppSettings["seq"].ToString();
            ExceptionLogger = new LoggerConfiguration().WriteTo.Seq(seqSrv, apiKey: "HPyw6WMycjOJoxCB6eKQ").CreateLogger();
            Log.Logger = new LoggerConfiguration().WriteTo.ColoredConsole().CreateLogger();

            init();

            ReloadConfig();

            #region MQ Rolence Version
            //ReceiveMQ();
            ////Retry.Do(ReceiveMQ, TimeSpan.FromMinutes(1), 10);

            ////按鈕離開程式
            //ConsoleKeyInfo k;
            //do
            //{
            //    k = Console.ReadKey();
            //    if (k.Key == ConsoleKey.R)
            //    {
            //        //重新載入設定檔
            //        ReloadConfig();
            //    }
            //    if (k.Key == ConsoleKey.S)
            //    {
            //        //counterHelper.RawValue(MQBroker_PerfCounterTypes.BrokerBeginReceiveException, 0);
            //        //counterHelper.RawValue(MQBroker_PerfCounterTypes.BrokerReceiveException, 0);
            //    }
            //    Console.WriteLine(DateTime.Now.ToString());
            //} while (k.Key != ConsoleKey.Q);
            #endregion


            //#region MQ Spring Version
            //var mq = new SimpleMsmqA<MainProduct.Savebar.PMEDataModel>(config.from.qname, config.from.ip);
            //mq.MessageListener = new PMEMessageListener(ref config);
            //mq.ExceptionHandler = new PMEExceptionHandler();
            //mq.Start(true);
            //#endregion

        }

        static void ReceiveMQ()
        {
            //SimpleMsmq接收端範例程式
            var mq = new SimpleMsmq<MainProduct.Savebar.PMEDataModel>(config.from.qname, config.from.ip);

            Console.BackgroundColor = ConsoleColor.Black;

            mq.OnBeginReceiveError = (ex) =>
            {
                ExceptionLogger.Error(ex, "OnBeginReceiveError");
            };

            mq.OnReceiveError = (ex, msg) =>
            {
                MessageQueueException mqex = ex as MessageQueueException; //轉化成msmq exception

                MainProduct.Savebar.PMEDataModel body = null;
                if (msg.Body != null)
                {
                    body = mq.DecodeMsgBody(msg);
                }

                ExceptionLogger.Error(mqex, "OnReceiveError, Msg body:{@body}, error code:{code}", body, mqex.MessageQueueErrorCode);

                Thread.Sleep(60 * 1000);
                mq.BeginReceive(true);//重開啟
            };

            mq.Receive = (catalog, pme) =>
            {
                //counterHelper.Increment(MQBroker_PerfCounterTypes.Receive_Rate);
                //var originColor = Console.ForegroundColor; mark by rolnece
                //Console.ForegroundColor = ConsoleColor.Green;
                //Console.WriteLine(string.Format("Receive: {0}", catalog));
                //Console.ForegroundColor = ConsoleColor.DarkYellow;
                //Console.WriteLine(string.Format("   Data: {0}", msg.ToString()));

                //lock (configlock)

                //if (pme.After!=null  && pme.After.NewPGC_str != null && pme.After.NewPGC_str.Count > 0)
                //{
                //    Console.ForegroundColor = ConsoleColor.Yellow;
                //    Console.WriteLine("NewPGC: {0}", pme.After.NewPGC_str.Aggregate((a, b) => { return a + "," + b; }));
                //    Console.ForegroundColor = ConsoleColor.White;
                //}


                //Route Operation
                var flagFieldDifferent = false;
                if (pme.Before != null)
                {
                    if (pme.After != null)
                    {
                        //pme.Before != null && pme.After != null
                        //異動
                        if (pme.After.Name != pme.Before.Name) { flagFieldDifferent = true; goto PMEEdit; }
                        if (pme.After.Price != pme.Before.Price) { flagFieldDifferent = true; goto PMEEdit; }
                        if (pme.After.HyperLink != pme.Before.HyperLink) { flagFieldDifferent = true; goto PMEEdit; }
                        if (pme.After.ImageUrl != pme.Before.ImageUrl) { flagFieldDifferent = true; goto PMEEdit; }
                        if (pme.After.Category_str != null && pme.Before.Category_str != null)
                        {
                            if (pme.After.Category_str.SequenceEqual(pme.Before.Category_str) == false) { flagFieldDifferent = true; goto PMEEdit; }
                        }
                        else if (pme.After.Category_str == null && pme.Before.Category_str != null)
                        {
                            flagFieldDifferent = true; goto PMEEdit;
                        }
                        else if (pme.After.Category_str != null && pme.Before.Category_str == null)
                        {
                            flagFieldDifferent = true; goto PMEEdit;
                        }

                        if (pme.After.PGCatalog_str != null && pme.Before.PGCatalog_str != null)
                        {
                            if (pme.After.PGCatalog_str.SequenceEqual(pme.Before.PGCatalog_str) == false) { flagFieldDifferent = true; goto PMEEdit; }
                        }
                        else if (pme.After.PGCatalog_str != null && pme.Before.PGCatalog_str == null)
                        {
                            flagFieldDifferent = true; goto PMEEdit;
                        }
                        else if (pme.After.PGCatalog_str == null && pme.Before.PGCatalog_str != null)
                        {
                            flagFieldDifferent = true; goto PMEEdit;
                        }

                    PMEEdit:
                        if (flagFieldDifferent == true)
                        {
                            foreach (var item in config.opUpdate)
                            {
                                if (item.enable == true)
                                {
                                    mqDic[string.Format("{0}:{1}", item.ip, item.qname)].Send(item.label, pme, System.Messaging.MessagePriority.Normal, false, item.compress, true);
                                    //Console.ForegroundColor = ConsoleColor.Yellow;
                                    //Console.WriteLine("異動");
                                    Log.Information("[異動],{@mqsetting}", item);
                                }
                            }
                        }
                    }
                    else
                    {
                        //pme.Before != null && pme.After == null
                        //刪除
                        foreach (var item in config.opDelete)
                        {
                            if (item.enable == true)
                            {
                                mqDic[string.Format("{0}:{1}", item.ip, item.qname)].Send(item.label, pme, System.Messaging.MessagePriority.Normal, false, item.compress, true);
                                //Console.ForegroundColor = ConsoleColor.Red;
                                //Console.WriteLine("刪除");
                                Log.Information("[刪除],{@mqsetting}", item);
                            }
                        }
                    }
                }
                else
                {
                    if (pme.After != null)
                    {
                        //新增
                        foreach (var item in config.opAdd)
                        {
                            if (item.enable == true)
                            {
                                mqDic[string.Format("{0}:{1}", item.ip, item.qname)].Send(item.label, pme, System.Messaging.MessagePriority.Normal, false, item.compress, true);
                                //Console.ForegroundColor = ConsoleColor.Green;
                                //Console.WriteLine("新增");
                                Log.Information("[新增],{@mqsetting}", item);
                            }
                        }
                    }
                    else
                    {
                        //pme.Before == null && pme.After == null
                        ExceptionLogger.Error("資料格式不正確！{@pme}", pme);
                        throw new Exception("NewProduct: 資料不正確！\n" + JsonConvert.SerializeObject(pme));
                    }
                }

                //Console.WriteLine("Task Done!");
                //Console.ForegroundColor = originColor;
                //顯示指令
                PrintInfomation();
                return true;
            };
            mq.BeginReceive(true);
        }

        static void PrintInfomation()
        {
            Console.WriteLine("1: Reload config");
            Console.WriteLine("q: Stop and Exit");
        }

        /// <summary>
        /// 重新載入mq 設定檔
        /// </summary>
        static void ReloadConfig()
        {
            lock (configlock)
            {
                //更新設定檔
                var json = System.IO.File.ReadAllText(configFilePath);
                config = Newtonsoft.Json.JsonConvert.DeserializeObject<MQConfig>(json);

                mqDic.Clear();

                //重建 msmq
                foreach (var item in config.opAdd)
                {
                    var key = string.Format("{0}:{1}", item.ip, item.qname);
                    var ips = item.ip.Trim().Split(",|".ToCharArray());
                    var sendmode = item.sendmode == MQSendMode.Broadcast ?
                        SimpleMsmqLibrary.SendMode.Broadcast :
                        SimpleMsmqLibrary.SendMode.Loadbalance;

                    if (mqDic.ContainsKey(key) != true)
                    {
                        mqDic.Add(key, new SimpleMsmq<MainProduct.Savebar.PMEDataModel>(item.qname, ips, sendmode));
                    }
                }

                foreach (var item in config.opDelete)
                {
                    var key = string.Format("{0}:{1}", item.ip, item.qname);
                    var ips = item.ip.Trim().Split(",|".ToCharArray());
                    var sendmode = item.sendmode == MQSendMode.Broadcast ?
                        SimpleMsmqLibrary.SendMode.Broadcast :
                        SimpleMsmqLibrary.SendMode.Loadbalance;
                    if (mqDic.ContainsKey(key) != true)
                    {
                        mqDic.Add(key, new SimpleMsmq<MainProduct.Savebar.PMEDataModel>(item.qname, ips, sendmode));
                    }
                }
                foreach (var item in config.opUpdate)
                {
                    var key = string.Format("{0}:{1}", item.ip, item.qname);
                    var ips = item.ip.Trim().Split(",|".ToCharArray());
                    var sendmode = item.sendmode == MQSendMode.Broadcast ?
                        SimpleMsmqLibrary.SendMode.Broadcast :
                        SimpleMsmqLibrary.SendMode.Loadbalance;
                    if (mqDic.ContainsKey(key) != true)
                    {
                        mqDic.Add(key, new SimpleMsmq<MainProduct.Savebar.PMEDataModel>(item.qname, ips, sendmode));
                    }
                }
            }
        }

        #region Console Initial
        static void init()
        {
            //if (RunningInstance() == true) Environment.Exit(1);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject.ToString() == "")
            {
                Thread.Sleep(10000);
                ReceiveMQ();
            }
            else
            {
                ExceptionLogger.Warning("未處理的例外錯誤！{@exception}", e.ExceptionObject);
                //Console.WriteLine(e.ExceptionObject.ToString());
                //LogMessage(e.ExceptionObject.ToString()); Rolence mark
                //MessageBox.Show((e.ExceptionObject as Exception).Message, "Unhandled UI Exception");
                // here you can log the exception ...
                //Environment.Exit(1);
            }
        }

        internal static bool RunningInstance()
        {
            //取得目前的程序
            System.Diagnostics.Process current = System.Diagnostics.Process.GetCurrentProcess();
            //取得其他同名稱的程序
            System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName(current.ProcessName);

            foreach (System.Diagnostics.Process process in processes)
            {
                //判斷是不是目前的執行緒
                if (process.Id != current.Id)
                {
                    //確定一下是不是從同一個執行
                    if (System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("/", "\\") == current.MainModule.FileName)
                    {
                        //找到~ 回傳 true
                        return true;
                    }
                }
            }

            //如果都沒有，則回傳 false
            return false;
        }

        internal static void LogMessage(string messageText)
        {
            string filename = System.AppDomain.CurrentDomain.BaseDirectory + "log\\execute_broker.log";

            try
            {
                FileInfo fi = new FileInfo(filename);
                if (!fi.Directory.Exists) Directory.CreateDirectory(fi.Directory.FullName);

                using (StreamWriter Log = File.AppendText(filename))
                {
                    Log.WriteLine("{0}: {1}", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"), messageText);
                }
            }
            catch (Exception ex)
            {
                using (StreamWriter Log = File.AppendText(filename))
                {
                    Log.WriteLine("{0}: Exception - {1}", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"), ex.Message);
                }
            }
        }
        #endregion
    }
}
