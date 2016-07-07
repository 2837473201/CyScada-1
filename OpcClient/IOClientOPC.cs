using System;
using System.Collections.Generic;
using System.Data;
using System.Net.NetworkInformation;
using System.Threading;
using Opc.Da;

/*
 * OPC客户端的处理方式：利用DATACHANGE事件获取变化点的数据，将所有变化数据放入ENGINE队列
 * 1.一个客户端一个OPC连接
 * 2.写数据时，是由ENGINE将要写的数据放入OPC客户端写数据队列，客户端只要有要写的数据则进行写操作
 */



namespace CyScada.Web.OpcClient
{
    public class IOClientOPC :IOClient
    {

        //读取的OPC服务器实例
        Server opcserver;

        ISubscription opcSub;

        Queue<object[]> WriteValues = new Queue<object[]>(10);


        private volatile bool ThreadFlag = true;


        bool EnqueueFlag = false;

        /// <summary>
        /// OPC状态测试
        /// </summary>
        Thread StateThread;

        /// <summary>
        /// 写值线程
        /// </summary>
        Thread WriteThread;


        public IOClientOPC()
        {
        }




        public void WriteValue(string ItemName, string ItemValueType, object value)
        {
            WriteValues.Enqueue(new object[] { ItemName, value });
        }




        private void DoWrite()
        {
            while (ThreadFlag)
            {
                if (WriteValues.Count == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }


                object[] values = WriteValues.Dequeue();
                ItemValue[] valueArray = new ItemValue[1];
                valueArray[0] = new ItemValue();
                valueArray[0].ItemName = values[0].ToString();
                valueArray[0].Value = values[1];
                Opc.IdentifiedResult[] writeresult = opcserver.Write(valueArray);
                if (!writeresult[0].ResultID.Succeeded())
                {
                    EnqueueLog("点[" + values[0].ToString() + "]写数据时失败:" + writeresult[0].ResultID.ToString());
                    //LogClass.Logs.Enqueue("点[" + values[0].ToString() + "]写数据时失败:" + writeresult[0].ResultID.ToString());
                }
                else
                {
                    //EnqueueLog("点[" + values[0].ToString() + "]=[" + values[1].ToString() + "]写数据时成功:" + writeresult[0].ResultID.ToString());
                    //LogClass.Logs.Enqueue("点[" + values[0].ToString() + "]=[" + values[1].ToString() + "]写数据时成功:" + writeresult[0].ResultID.ToString());
                }

                Thread.Sleep(100);
            }
        }






        private void SetServerState()
        {
            /*
             *1.每隔1000MS获取服务器状态，如果正常，则设置运行状态
             *2.如果服务器状态不正确，则运行连接操作
             *  1).PING IP
             *  2).连接OPCServer
             */

            while (ThreadFlag)
            {
                try
                {
                    ServerStatus ss = opcserver.GetStatus();
                    if (ss.ServerState == serverState.running)
                    {
                        _ServerStatus = "Connected";
                    }
                    else
                    {
                        Reconnect();
                    }

                }
                catch (Exception ex)
                {
                    _ErrorMessage = ex.Message;
                    Reconnect();
                }

                Thread.Sleep(1000);
                continue;
            }
        }


        private void Reconnect()
        {

            if (opcserver != null)
            {
                DisConnectServer();
            }

            EnqueueLog("正在尝试重新连接OPC服务器...");
            //LogClass.Logs.Enqueue("正在尝试重新连接OPC服务器...");
            if (ConnectServer())
            {
                EnqueueLog("连接成功！正在重新加载点表...");
                //LogClass.Logs.Enqueue("连接成功！正在重新加载点表...");
                ReloadItems();
                _ServerStatus = "Connected";
            }
            else
            {
                string ErrorStr = "连接OPC服务器失败，原因为:" + _ErrorMessage;
                if (_ServerStatus != "NotConnected")
                {
                    EnqueueLog(ErrorStr);
                    //LogClass.Logs.Enqueue(ErrorStr);
                }
                _ServerStatus = "NotConnected";
            }

        }

        private DataSet LoadConfigFile()
        {
            DataSet dsItems = new DataSet();
            dsItems.ReadXml(_ItemConfigPath);
            dsItems.Tables[0].Rows.RemoveAt(0);
            return dsItems;

        }


        private void ReloadItems()
        {
            DataSet dsConfig = LoadConfigFile();
            DataRow[] drItems = dsConfig.Tables[0].Select("ServerAddress='" + _ServerAddress + "' and ServerType = 'OPC'");
            Item[] Items = new Item[drItems.Length];

            try
            {
                List<string> ItemNames = new List<string>();
                foreach (DataRow drtemp in drItems)
                {
                    ItemNames.Add(drtemp["ItemName"].ToString());
                    //bool result = false;
                    //result = this.AddItem(drtemp["ItemName"].ToString());
                    //if (!result)
                    //{
                    //    EnqueueLog("加载点[" + drtemp["ItemName"].ToString() + "]失败");
                    //    //LogClass.Logs.Enqueue("加载点[" + drtemp["ItemName"].ToString() + "]失败");
                    //}
                }

                this.AddItems(ItemNames.ToArray());

            }
            catch (Exception ex)
            {
                EnqueueLog("重新加载点出现错误:原因为:" + ex.Message);
                //LogClass.Logs.Enqueue("重新加载点出现错误:原因为:" + ex.Message);
                return;
            }
        }



        public bool AddItem(string ItemName)
        {
            Item[] items = new Item[1];
            items[0] = new Item();
            items[0].ItemName = ItemName;
            ItemResult[] results = opcSub.AddItems(items);
            if (!results[0].ResultID.Succeeded())
            {
                EnqueueLog("添加点出现错误:点[" + results[0].ItemName + "]添加失败,原因为:" + results[0].ResultID.ToString());
                object[,] BadValues = new object[1, 5];

                BadValues[0, 0] = ItemName;
                BadValues[0, 1] = null;
                BadValues[0, 2] = DateTime.Now;
                BadValues[0, 3] = "Bad";
                BadValues[0, 4] = string.Format(@"OPC://{0}/{1}", this._ServerAddress, ItemName);
                EnqueueRec(BadValues);
                return false;
            }


            ItemValueResult[] values = opcserver.Read(items);


            object[,] Values = new object[values.Length, 5];
            for (int i = 0; i < values.Length; i++)
            {
                Values[i, 0] = values[i].ItemName;
                Values[i, 1] = values[i].Value;
                Values[i, 2] = values[i].Timestamp;
                Values[i, 3] = values[i].Quality.ToString();
                Values[i, 4] = string.Format(@"OPC://{0}/{1}", this._ServerAddress, values[i].ItemName);
            }

            EnqueueRec(Values);




            return true;

        }


        public void AddItems(string[] ItemNames, string[] ProxyStrings)
        {
            this.AddItems(ItemNames);
        }

        public void AddItems(string[] ItemNames)
        {
            Item[] items = new Item[ItemNames.Length];
            for (int i = 0; i < ItemNames.Length; i++)
            {
                items[i] = new Item();
                items[i].ItemName = ItemNames[i];
            }
            ItemResult[] results = opcSub.AddItems(items);
            for (int i = 0; i < results.Length; i++)
            {
                if (!results[i].ResultID.Succeeded())
                {
                    EnqueueLog("添加点出现错误:点[" + results[i].ItemName + "]添加失败,原因为:" + results[i].ResultID.ToString());
                    //LogClass.Logs.Enqueue("批量添加点出现错误:点[" + results[i].ItemName + "]添加失败,原因为:" + results[i].ResultID.ToString());
                }
            }


            ItemValueResult[] values = opcserver.Read(items);


            object[,] Values = new object[values.Length, 5];
            for (int i = 0; i < values.Length; i++)
            {
                Values[i, 0] = values[i].ItemName;
                Values[i, 1] = values[i].Value;
                Values[i, 2] = values[i].Timestamp;
                Values[i, 3] = values[i].Quality.ToString();
                Values[i, 4] = string.Format(@"OPC://{0}/{1}", this._ServerAddress, values[i].ItemName);
            }

            EnqueueRec(Values);
        }




        /// <summary>
        /// 删除组中的点
        /// </summary>
        /// <param name="ItemName">点名</param>
        public void RemoveItem(string ItemName)
        {
            Opc.ItemIdentifier[] itemid = new Opc.ItemIdentifier[1];
            itemid[0] = new Opc.ItemIdentifier(ItemName);
            opcSub.RemoveItems(itemid);
        }


        /// <summary>
        /// 删除组中的点
        /// </summary>
        /// <param name="ItemName">点名</param>
        public void RemoveItem(string[] ItemNames)
        {
            Opc.ItemIdentifier[] itemids = new Opc.ItemIdentifier[ItemNames.Length];
            for (int i = 0; i < ItemNames.Length; i++)
            {
                itemids[i] = new Opc.ItemIdentifier(ItemNames[i]);
            }
            opcSub.RemoveItems(itemids);
        }




        /// <summary>
        /// 开始客户端读取数据
        /// </summary>
        public void StartRead()
        {
            StateThread = new Thread(new ThreadStart(SetServerState));
            StateThread.IsBackground = true;
            StateThread.Start();

            WriteThread = new Thread(new ThreadStart(DoWrite));
            WriteThread.IsBackground = true;
            WriteThread.Start();

            ThreadFlag = true;
            EnqueueFlag = true;
        }

        void opcSub_DataChanged(object subscriptionHandle, object requestHandle, ItemValueResult[] values)
        {
            try
            {
                if (EnqueueFlag)
                {
                    //直接修改好还是进入队列好？？
                    object[,] Values = new object[values.Length, 5];
                    for (int i = 0; i < values.Length; i++)
                    {
                        Values[i, 0] = values[i].ItemName;
                        Values[i, 1] = values[i].Value;
                        Values[i, 2] = values[i].Timestamp;
                        Values[i, 3] = values[i].Quality.ToString();
                        Values[i, 4] = string.Format(@"OPC://{0}/{1}", this._ServerAddress,values[i].ItemName);
                    }

                    EnqueueRec(Values);
                    //IODriverEngine.EnqueueRec(Values);
                }
            }
            catch (Exception ex)
            {
                EnqueueLog(ex.Message);
                //LogClass.Logs.Enqueue(ex.Message);
            }
        }

        /// <summary>
        /// 停止读取数据
        /// </summary>
        public void StopRead()
        {
            ThreadFlag = false;
            Thread.Sleep(100);
            StateThread.Abort();
            WriteThread.Abort();
            EnqueueFlag = false;
        }



        public void DisConnectServer()
        {
            try
            {
                opcserver.CancelSubscription(opcSub);
            }
            catch { }

            try
            {

                opcserver.Disconnect();
            }
            catch { }

            opcserver.Dispose();
        }



        /// <summary>
        /// 连接OPC服务器
        /// </summary>
        /// <returns></returns>
        public bool ConnectServer()
        {
            _ServerStatus = "Connecting";

            //先PING地址
            string strIP = _ServerAddress.Split('/')[0];
            if (strIP.ToLower() != "127.0.0.1" && strIP.ToLower() != "localhost")
            {
                Ping ping = new Ping();
                PingReply pr = ping.Send(strIP);
                if (pr.Status != IPStatus.Success)
                {
                    EnqueueLog("网络出现错误:" + pr.Status.ToString());
                    //LogClass.Logs.Enqueue("网络出现错误:" + pr.Status.ToString());
                    return false;
                }
            }

            OpcCom.Factory fac = new OpcCom.Factory(false);
            opcserver = new Server(fac, new Opc.URL(string.Format(@"opcda://{0}", _ServerAddress)));

            try
            {
                opcserver.Connect();
                opcserver.ServerShutdown += new Opc.ServerShutdownEventHandler(opcserver_ServerShutdown);

                SubscriptionState ss = new SubscriptionState();
                ss.Active = true;
                opcSub = opcserver.CreateSubscription(ss);
                _ServerStatus = "Connected";
                opcSub.DataChanged += new Opc.Da.DataChangedEventHandler(opcSub_DataChanged);

                return true;
            }
            catch (Exception ex)
            {
                EnqueueLog("连接OPC服务器出现错误:" + ex.Message);
                //LogClass.Logs.Enqueue("连接OPC服务器出现错误:" + ex.Message);
                _ErrorMessage = ex.Message;
                _ServerStatus = "NotConnected";
                return false;
            }
        }

        void opcserver_ServerShutdown(string reason)
        {
            EnqueueLog("IOClientOPC:OPC服务器被关闭,原因为:" + reason);
            //LogClass.Logs.Enqueue("IOClientOPC:OPC服务器被关闭,原因为:" + reason);
            //NewLog("IOClientOPC:OPC服务器被关闭,原因为:" + reason);
            //StopRead();
            //DisConnectServer();
        }


        private string _ErrorMessage = "";

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage
        {
            get { return _ErrorMessage; }
        }

        private string _ServerAddress = "";

        /// <summary>
        /// OPC服务器地址：(eg. 127.0.0.1/KepServer.V4)
        /// </summary>
        public string ServerAddress
        {
            get
            {
                return _ServerAddress;
            }
            set
            {
                _ServerAddress = value;
            }
        }

        private string _ServerStatus = "NotConnected";

        /// <summary>
        /// 服务器状态
        /// </summary>
        public string ServerStatus
        {
            get { return _ServerStatus; }
        }



        public string ServerType
        {
            get { return "OPC"; }
        }


        public bool AddItem(string ItemName, string ProxyString)
        {
            return AddItem(ItemName);
        }



        public void SetProxyFormat(string ProxyFormat)
        {
            return;
        }


        public event LogHandler EnqueueLog;

        public event RecHandler EnqueueRec;

        private string _ItemConfigPath = "";

        public string ItemConfigPath
        {
            get
            {
                return _ItemConfigPath;
            }
            set
            {
                _ItemConfigPath = value;
            }
        }

    }
}
