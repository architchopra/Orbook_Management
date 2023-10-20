using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tt_net_sdk;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using CsvHelper.Configuration;
using System.IO;
using System.Windows.Forms;
using System.Collections.Concurrent;
using Serilog;

namespace WindowsFormsApp1
{
    internal class Order_Action_Functions
    {
        public tt_net_sdk.Dispatcher dispatcher = null;
        public ConcurrentDictionary<string, Orders_Data_1> or_orders_copy = new ConcurrentDictionary<string, Orders_Data_1>(); // newConcurrentDictionary<string, Order>();
        public ConcurrentDictionary<string, Orders_Data_1> qs_orders_copy = new ConcurrentDictionary<string, Orders_Data_1>(); // newConcurrentDictionary<string, Order>();
        public ConcurrentDictionary<string, Orders_Data_1> ase_or_orders_copy = new ConcurrentDictionary<string, Orders_Data_1>(); // newConcurrentDictionary<string, Order>();
        public ConcurrentDictionary<string, Orders_Data_1> ase_qs_orders_copy = new ConcurrentDictionary<string, Orders_Data_1>(); // newConcurrentDictionary<string, Order>();
        public ConcurrentDictionary<string, Order> pause = new ConcurrentDictionary<string, Order>(); // newConcurrentDictionary<string, Order>();

        private InstrumentLookup m_instrLookupRequest = null;
        private tt_net_sdk.Instrument instr = null;
        private object m_Hold = new object();
        public void Hold_Orders(ConcurrentDictionary<string, Order> dict, string dict_name, TradeSubscription trade_subscription)
        {
            lock (m_Hold)
            {
                pause = dict;
                foreach (string order_key in pause.Keys)
                {
                    if (trade_subscription.Orders.ContainsKey(order_key))
                    {
                        OrderProfile op = trade_subscription.Orders[order_key].GetOrderProfile();
                        op.Action = OrderAction.Hold;
                        //op.IsOnHold = false;
                        if (!trade_subscription.SendOrder(op))
                        {
                            Log.Information("Error while putting " + dict_name + " orders on Hold " + op.SiteOrderKey);
                            //form2.richTextBox1.AppendText("\nError while putting " + dict_name +  " orders on Hold " + op.SiteOrderKey);
                        }
                        else
                        {
                            Log.Information(dict_name + " order on Hold " + op.SiteOrderKey);
                            //form2.richTextBox1.AppendText("\n" + dict_name + " order on Hold " + op.SiteOrderKey);
                        }
                    }
                    else
                    {
                        Log.Information(dict_name + " Key not found : " + order_key);
                        //form2.richTextBox1.AppendText("\n" + dict_name + " Key not found : " + order_key);
                    }
                }
            }
        }

        public void Resume_Orders(ConcurrentDictionary<string, Order> dict, string dict_name, TradeSubscription trade_subscription)
        {
            foreach (string order_key in dict.Keys)
            {
                if (trade_subscription.Orders.ContainsKey(order_key))
                {
                    OrderProfile op = trade_subscription.Orders[order_key].GetOrderProfile();
                    op.Action = OrderAction.Resume;
                    //op.IsOnHold = true;
                    if (!trade_subscription.SendOrder(op))
                    {
                       Log.Information("Error while Resuming " + dict_name + " orders " + op.SiteOrderKey);
                        //form2.richTextBox1.AppendText("\nError while Resuming " + dict_name + " orders " + op.SiteOrderKey);
                    }
                    else
                    {
                        Log.Information(dict_name + " Order Resumed " + op.SiteOrderKey);
                        //form2.richTextBox1.AppendText("\n" + dict_name + " Order Resumed " + op.SiteOrderKey);
                    }
                }
                else
                {
                    Log.Information(dict_name + " Key not found : " + order_key);
                    //form2.richTextBox1.AppendText("\n" + dict_name + " Key not found : " + order_key);
                }
            }
        }

        public void Delete_Orders_without_storing(ConcurrentDictionary<string, Order> dict, string dict_name, TradeSubscription trade_subscription)
        {
            foreach (string order_key in dict.Keys)
            {
                if (trade_subscription.Orders.ContainsKey(order_key))
                {
                    OrderProfile op = trade_subscription.Orders[order_key].GetOrderProfile();
                    op.Action = OrderAction.Delete;
                    //op.IsOnHold = false;
                    if (!trade_subscription.SendOrder(op))
                    {
                        Log.Information("Error while putting " + dict_name + " orders to delete " + op.SiteOrderKey);
                        //form2.richTextBox1.AppendText("\nError while putting " + dict_name +  " orders on Hold " + op.SiteOrderKey);
                    }
                    else
                    {
                        Log.Information(dict_name + " order deleted " + op.SiteOrderKey);
                        //form2.richTextBox1.AppendText("\n" + dict_name + " order on Hold " + op.SiteOrderKey);
                    }
                }
                else
                {
                    Log.Information(dict_name + " Key not found : " + order_key);
                    //form2.richTextBox1.AppendText("\n" + dict_name + " Key not found : " + order_key);
                }
            }
        }
        public void Write_in_Txt_File(ConcurrentDictionary<string, Order> dict, string file_name)
        {
            Log.Information("Storing In File: " + file_name);
            // Create file directory if not existing:
            System.IO.FileInfo file = new System.IO.FileInfo(file_name);
            file.Directory.Create();           
            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);
            using (var stream = File.Open(file_name, FileMode.Create, FileAccess.Write, FileShare.Write))
            using (var writer = new StreamWriter(stream))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteHeader<Orders_Data>();
                csv.NextRecord();
                foreach (KeyValuePair<string, Order> kvp in dict)
                {
                    Orders_Data orders_Data = new Orders_Data()
                    {
                        TimeStamp = DateTime.Now,
                        MarketID = kvp.Value.InstrumentKey.MarketId.ToString(),
                        ProdType = kvp.Value.Instrument.Product.Type.ToString(),
                        ProdName = kvp.Value.Instrument.Product.Name,
                        Alias = kvp.Value.InstrumentKey.Alias,
                        BuySell = kvp.Value.BuySell.ToString(),
                        OrderType = kvp.Value.OrderType.ToString(),
                        LimitPrice = kvp.Value.LimitPrice.Value,
                        OrderQuantity = kvp.Value.OrderQuantity.Value,
                        FilledQuantity = kvp.Value.FillQuantity.Value,
                        DisclosedQuantity = kvp.Value.DisclosedQuantity.Value,
                        TimeInForce = kvp.Value.TimeInForce.ToString(),
                        TextA = kvp.Value.TextA,
                        SiteOrderKey = kvp.Value.SiteOrderKey,
                        OrderSource = kvp.Value.OrderSource.ToString(),
    };
                    csv.WriteRecord(orders_Data);
                    csv.NextRecord();
                }
            }
        }
        public void Read_Txt_File(ConcurrentDictionary<string, Orders_Data_1> dict, string file_name, string dict_name,
            TradeSubscription trade_subscription, IReadOnlyCollection<Account> m_account, int acc_idx, bool check_reload)
        {
            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);
            using (var stream = File.Open(file_name, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvReader(reader, config))
            {
                var dataBase = csv.GetRecords<Orders_Data>();
                foreach (var data in dataBase)
                {
                    Orders_Data_1 orders_Data_1 = new Orders_Data_1();
                    MarketId marketKey = Market.GetMarketIdFromName(data.MarketID);
                    ProductType productType = tt_net_sdk.Product.GetProductTypeFromName(data.ProdType);
                    // lookup an instrument:
                    m_instrLookupRequest = new InstrumentLookup(this.dispatcher, marketKey, productType, data.ProdName, data.Alias);
                    Log.Information(data.Alias);
                    ProductDataEvent productDataEvent = m_instrLookupRequest.Get();
                    if (productDataEvent == ProductDataEvent.Found)
                    {
                        instr = m_instrLookupRequest.Instrument;
                        Log.Information("Found Instrument: " + instr);
                        orders_Data_1.Instrument = instr;

                        // Checking Buysell:
                        if (data.BuySell == "Buy")
                        {
                            orders_Data_1.BuySell = BuySell.Buy;
                        }
                        else
                        {
                            orders_Data_1.BuySell = BuySell.Sell;
                        }

                        // Checking OrderType:
                        if (data.OrderType == "Limit")
                        {
                            orders_Data_1.OrderType = OrderType.Limit;
                        }
                        else if (data.OrderType == "Market")
                        {
                            orders_Data_1.OrderType = OrderType.Market;
                        }
                        else if (data.OrderType == "StopLimit")
                        {
                            orders_Data_1.OrderType = OrderType.StopLimit;
                        }
                        else if (data.OrderType == "Stop")
                        {
                            orders_Data_1.OrderType = OrderType.Stop;
                        }
                        else if (data.OrderType == "StopMarketToLimit")
                        {
                            orders_Data_1.OrderType = OrderType.StopMarketToLimit;
                        }
                        else if (data.OrderType == "MarketCloseToday")
                        {
                            orders_Data_1.OrderType = OrderType.MarketCloseToday;
                        }
                        else if (data.OrderType == "NotSet")
                        {
                            orders_Data_1.OrderType = OrderType.NotSet;
                        }
                        else
                        {
                            orders_Data_1.OrderType = OrderType.NotSet;
                        }

                        // Adding Price:
                        orders_Data_1.LimitPrice = Price.FromDecimal(instr, data.LimitPrice);

                        // Adding OrderQuamtity:
                        orders_Data_1.OrderQuantity = Quantity.FromDecimal(instr, data.OrderQuantity);

                        // Adding FillQuamtity:
                        orders_Data_1.FilledQuantity = Quantity.FromDecimal(instr, data.FilledQuantity);

                        // Adding WorkingQuamtity:
                        orders_Data_1.DisclosedQuantity = Quantity.FromDecimal(instr, data.DisclosedQuantity);

                        // Checking TIF:
                        if (data.TimeInForce == "GoodTillCancel")
                        {
                            orders_Data_1.TimeInForce = TimeInForce.GoodTillCancel;
                        }
                        else if (data.TimeInForce == "Day")
                        {
                            orders_Data_1.TimeInForce = TimeInForce.Day;
                        }
                        else if (data.TimeInForce == "FillOrKill")
                        {
                            orders_Data_1.TimeInForce = TimeInForce.FillOrKill;
                        }
                        else
                        {
                            orders_Data_1.TimeInForce = TimeInForce.Unknown;
                        }

                        // Adding TextA:
                        orders_Data_1.TextA = data.TextA;

                        // Adding SiteOrderKey:
                        orders_Data_1.SiteOrderKey = data.SiteOrderKey;
                        orders_Data_1.OrderSource = data.OrderSource;

                    }
                    else
                    {
                        Log.Information("Instrument Not Found: " + m_instrLookupRequest.ToString());
                    }

                    // Storing inConcurrentDictionary:
                    if (orders_Data_1.LimitPrice.IsValid & orders_Data_1.OrderQuantity.IsValid & orders_Data_1.DisclosedQuantity.IsValid & orders_Data_1.FilledQuantity.IsValid)
                    {
                        if(orders_Data_1.OrderSource== "Adl")
                        {
                            Log.Information("adl order");

                        }
                        else
                        {
                            dict.TryAdd(data.SiteOrderKey, orders_Data_1);

                        }
                    }
                    else
                    {
                        MessageBox.Show("Price Invalid for: " + orders_Data_1.Instrument.Name + " " + orders_Data_1.BuySell.ToString() + " " + orders_Data_1.LimitPrice.ToString()
                             + " " + orders_Data_1.OrderQuantity.ToString() + " " + orders_Data_1.DisclosedQuantity.ToString() + " " + orders_Data_1.SiteOrderKey);
                    }
                }
            }
            Log.Information("Count: " + dict.Count());

            /*Log.Information("SOK: " + dict.Values.ElementAt(0).SiteOrderKey);
            Log.Information("Instrument: " + dict.Values.ElementAt(0).Instrument);
            Log.Information("BuySell: " + dict.Values.ElementAt(0).BuySell);
            Log.Information("OrdQty: " + dict.Values.ElementAt(0).OrderQuantity);
            Log.Information("FillQty: " + dict.Values.ElementAt(0).FilledQuantity);
            Log.Information("Workqty: " + dict.Values.ElementAt(0).WorkingQuantity);

            Log.Information("SOK: " + dict.Values.ElementAt(1).SiteOrderKey);
            Log.Information("Instrument: " + dict.Values.ElementAt(1).Instrument);
            Log.Information("BuySell: " + dict.Values.ElementAt(1).BuySell);
            Log.Information("OrdQty: " + dict.Values.ElementAt(1).OrderQuantity);
            Log.Information("FillQty: " + dict.Values.ElementAt(1).FilledQuantity);
            Log.Information("Workqty: " + dict.Values.ElementAt(1).WorkingQuantity);*/

            // Sending Order:
            if (check_reload)
            {
                Send_Orders_Task_Creator(dict, dict_name, trade_subscription, m_account, acc_idx);
            }
            else
            {
                Send_Order_Normal(dict, dict_name, trade_subscription, m_account, acc_idx);
            }
        }
        public OrderProfile Send_Order(Instrument instrument, BuySell buysell, Price price, Quantity quantity, OrderType orderType, tt_net_sdk.TimeInForce TIF,
            IReadOnlyCollection<Account> m_account, int acc_idx, string text)
        {
            OrderProfile op = new OrderProfile(instrument)
            {
                BuySell = buysell,
                OrderType = orderType,
                TimeInForce = TIF,
                Account = m_account.ElementAt(acc_idx),
                LimitPrice = price,
                OrderQuantity = quantity,
                TextA = text
            };
           

            return op;
        }
        public void Send_Order_Normal(ConcurrentDictionary<string, Order> dict, string dict_name, TradeSubscription trade_subscription, IReadOnlyCollection<Account> m_account, int acc_idx)
        {
            foreach (string order_key in dict.Keys)
            {
                if (dict[order_key].OrderSource.ToString() != "Adl")
                {
                    Instrument instr = dict[order_key].Instrument;
                    BuySell buysell = dict[order_key].BuySell;
                    OrderType orderType = dict[order_key].OrderType;
                    Price price = dict[order_key].LimitPrice;
                    Quantity disclosed_quantity = dict[order_key].DisclosedQuantity;
                    tt_net_sdk.TimeInForce TIF = dict[order_key].TimeInForce;
                    string text = dict[order_key].TextA;

                    OrderProfile op = Send_Order(instr, buysell, price, disclosed_quantity, orderType, TIF, m_account, acc_idx, text);
                    if (!trade_subscription.SendOrder(op))
                    {
                        Log.Information("Error while Adding " + dict_name + " orders " + op.SiteOrderKey);
                        //form2.richTextBox1.AppendText("\nError while Adding " + dict_name + " orders " + op.SiteOrderKey);
                    }
                    else
                    {
                        Log.Information(dict_name + " order Added " + op.SiteOrderKey);
                        //form2.richTextBox1.AppendText("\n" + dict_name + " order Added " + op.SiteOrderKey);
                    }
                }
                else
                {
                    Log.Information("ADL ORDER");
                }
            }
        }

        public void Send_Order_Normal(ConcurrentDictionary<string, Orders_Data_1> dict, string dict_name, TradeSubscription trade_subscription, IReadOnlyCollection<Account> m_account, int acc_idx)
        {
            foreach (string order_key in dict.Keys)
            {
                if (dict[order_key].OrderSource.ToString() != "Adl")
                {
                    Instrument instr = dict[order_key].Instrument;
                    BuySell buysell = dict[order_key].BuySell;
                    OrderType orderType = dict[order_key].OrderType;
                    Price price = dict[order_key].LimitPrice;
                    Quantity disclosed_quantity = dict[order_key].DisclosedQuantity;
                    tt_net_sdk.TimeInForce TIF = dict[order_key].TimeInForce;
                    string text = dict[order_key].TextA;

                    OrderProfile op = Send_Order(instr, buysell, price, disclosed_quantity, orderType, TIF, m_account, acc_idx, text);
                    if (!trade_subscription.SendOrder(op))
                    {
                        Log.Information("Error while Adding " + dict_name + " orders " + op.SiteOrderKey);
                        //form2.richTextBox1.AppendText("\nError while Adding " + dict_name + " orders " + op.SiteOrderKey);
                    }
                    else
                    {
                        Log.Information(dict_name + " order Added " + op.SiteOrderKey);
                        //form2.richTextBox1.AppendText("\n" + dict_name + " order Added " + op.SiteOrderKey);
                    }
                }
                else
                {
                    Log.Information("ADL ORDER");
                }
            }
        }
        public void Send_Orders_Task_Creator(ConcurrentDictionary<string, Order> dict, string dict_name, TradeSubscription trade_subscription, IReadOnlyCollection<Account> m_account, int acc_idx)
        {
            Task[] taskArray = new Task[dict.Count];
            int i = 0;
            foreach (string order_key in dict.Keys)
            {
                if (dict[order_key].OrderSource.ToString() != "Adl")
                {
                    Log.Information(Convert.ToString(dict[order_key].DisclosedQuantity));
                    Log.Information(Convert.ToString(dict[order_key].OrderQuantity));
                    taskArray[i] = Task.Run(() => this.Send_Order_Reload_Check(dict, order_key, dict_name, trade_subscription, m_account, acc_idx));
                }
                else
                {
                    Log.Information("ADL ORDER");
                }
                /*taskArray[i] = new Task(() => this.Send_Order_Reload_Check(dict, order_key, dict_name, trade_subscription, m_account, acc_idx));
                taskArray[i].Start();*/
                i++;

            }
        }
        public void Send_Orders_Task_Creator(ConcurrentDictionary<string, Orders_Data_1> dict, string dict_name, TradeSubscription trade_subscription, IReadOnlyCollection<Account> m_account, int acc_idx)
        {
            Task[] taskArray = new Task[dict.Count];
            int i = 0;
            foreach (string order_key in dict.Keys)
            {
                if (dict[order_key].OrderSource.ToString() != "Adl")
                {
                    Log.Information(Convert.ToString(dict[order_key]));
                    Log.Information(Convert.ToString(dict[order_key].DisclosedQuantity));
                    Log.Information(Convert.ToString(dict[order_key].OrderQuantity));
                    taskArray[i] = Task.Run(() => this.Send_Order_Reload_Check(dict, order_key, dict_name, trade_subscription, m_account, acc_idx));
                    /* taskArray[i] = new Task(() => this.Send_Order_Reload_Check(dict, order_key, dict_name, trade_subscription, m_account, acc_idx));
                     taskArray[i].Start();*/
                }
                else
                {
                    Log.Information("ADL ORDER");
                }
                i++;
            }
        }
        public void Send_Order_Reload_Check(ConcurrentDictionary<string, Order> dict, string order_key, string dict_name,
            TradeSubscription trade_subscription, IReadOnlyCollection<Account> m_account, int acc_idx)
        {
            Instrument instr = dict[order_key].Instrument;
            BuySell buysell = dict[order_key].BuySell;
            OrderType orderType = dict[order_key].OrderType;
            Price price = dict[order_key].LimitPrice;
            Quantity order_quantity = dict[order_key].OrderQuantity;
            Quantity disclosed_quantity = dict[order_key].DisclosedQuantity;
            Quantity fill_quantity = dict[order_key].FillQuantity;
            tt_net_sdk.TimeInForce TIF = dict[order_key].TimeInForce;
            string text = dict[order_key].TextA;

            // Checking for reload:
            bool is_reload = false;
            if (order_quantity - fill_quantity != disclosed_quantity)
            {
                is_reload = true;
            }
            else
            {
                is_reload = false;
            }
            Log.Information("Is Reload: " + is_reload);
            //form2.richTextBox1.AppendText("Is Reload: " + is_reload);

            // Setting Spread for Reload:
           /* bool reload_set = false;
            if (is_reload)
            {
                SpreadDetails sp = instr.GetSpreadDetails();

                sp.ReloadFlag = is_reload;
                if (is_reload)
                {
                    Log.Information("Setting Reload Quanity to: " + working_quantity);
                    sp.DefaultReloadQty = int.Parse(working_quantity.ToString());
                }
                ASReturnCodes rtnCode;
                instr = AutospreaderManager.UpdateSpreadDetails(sp, out rtnCode);
                Log.Information("Rtn Code:" + rtnCode.ToString());
                System.Diagnostics.Debug.Assert(rtnCode == ASReturnCodes.Success);
                reload_set = true;
            }
            else
            {
                reload_set = true;
            }*/

            // Sending Order:
           /* if (reload_set)
            {*/
                Quantity quantity = order_quantity - fill_quantity;
                OrderProfile op = Send_Order(instr, buysell, price, quantity, orderType, TIF, m_account, acc_idx, text);
            op.DisclosedQuantity = disclosed_quantity;
                if (!trade_subscription.SendOrder(op))
                {
                    Log.Information("Error while Adding ASE " + dict_name + " orders " + op.SiteOrderKey);
                    //form2.richTextBox1.AppendText("\nError while Adding ASE " + dict_name + " orders " + op.SiteOrderKey);
                }
                else
                {
                    Log.Information(dict_name + " order Added " + op.SiteOrderKey);
                    //form2.richTextBox1.AppendText("\n" + dict_name + " order Added " + op.SiteOrderKey);
                   /* if (is_reload)
                    {
                        SpreadDetails sp = instr.GetSpreadDetails();

                        sp.ReloadFlag = false;
                        ASReturnCodes rtnCode;
                        instr = AutospreaderManager.UpdateSpreadDetails(sp, out rtnCode);
                        Log.Information("Rtn Code:" + rtnCode.ToString());
                        System.Diagnostics.Debug.Assert(rtnCode == ASReturnCodes.Success);
                        Log.Information("ASE Default Reload set to False");
                    }*/
                }
           /* }
            else
            {
                if (is_reload)
                {
                    Log.Information("Failed to Save ASE");
                }
            }*/
        }
        public void Send_Order_Reload_Check(ConcurrentDictionary<string, Orders_Data_1> dict, string order_key, string dict_name,
            TradeSubscription trade_subscription, IReadOnlyCollection<Account> m_account, int acc_idx)
        {
            Instrument instr = dict[order_key].Instrument;
            BuySell buysell = dict[order_key].BuySell;
            OrderType orderType = dict[order_key].OrderType;
            Price price = dict[order_key].LimitPrice;
            Quantity order_quantity = dict[order_key].OrderQuantity;
            Quantity disclosed_quantity = dict[order_key].DisclosedQuantity;
            Quantity fill_quantity = dict[order_key].FilledQuantity;
            tt_net_sdk.TimeInForce TIF = dict[order_key].TimeInForce;
            string text = dict[order_key].TextA;

            // Checking for reload:
            bool is_reload = false;
            if (order_quantity - fill_quantity != disclosed_quantity)
            {
                is_reload = true;
            }
            else
            {
                is_reload = false;
            }
            Log.Information("Is Reload: " + is_reload);
            //form2.richTextBox1.AppendText("Is Reload: " + is_reload);

            // Setting Spread for Reload:
/*            bool reload_set = false;
            if (is_reload)
            {
                SpreadDetails sp = instr.GetSpreadDetails();

                sp.ReloadFlag = is_reload;
                if (is_reload)
                {
                    Log.Information("Setting Reload Quanity to: " + working_quantity);
                    sp.DefaultReloadQty = int.Parse(working_quantity.ToString());
                }
                ASReturnCodes rtnCode;
                instr = AutospreaderManager.UpdateSpreadDetails(sp, out rtnCode);
                Log.Information("Rtn Code:" + rtnCode.ToString());
                System.Diagnostics.Debug.Assert(rtnCode == ASReturnCodes.Success);
                reload_set = true;
            }
            else
            {
                reload_set = true;
            }*/

            // Sending Order:
           /* if (reload_set)
            {*/
                Quantity quantity = order_quantity - fill_quantity;
                OrderProfile op = Send_Order(instr, buysell, price, quantity, orderType, TIF, m_account, acc_idx, text);
            op.DisclosedQuantity = disclosed_quantity;
                if (!trade_subscription.SendOrder(op))
                {
                    Log.Information("Error while Adding ASE " + dict_name + " orders " + op.SiteOrderKey);
                    //form2.richTextBox1.AppendText("\nError while Adding ASE " + dict_name + " orders " + op.SiteOrderKey);
                }
                else
                {
                    Log.Information(dict_name + " order Added " + op.SiteOrderKey);
                    //form2.richTextBox1.AppendText("\n" + dict_name + " order Added " + op.SiteOrderKey);
                   /* if (is_reload)
                    {
                        SpreadDetails sp = instr.GetSpreadDetails();

                        sp.ReloadFlag = false;
                        ASReturnCodes rtnCode;
                        instr = AutospreaderManager.UpdateSpreadDetails(sp, out rtnCode);
                        Log.Information("Rtn Code:" + rtnCode.ToString());
                        System.Diagnostics.Debug.Assert(rtnCode == ASReturnCodes.Success);
                        Log.Information("ASE Default Reload set to False");
                    }*/
                }
            
           
        }


        internal class Orders_Data
        {
            [Name(name: "TimeStamp")]
            public DateTime TimeStamp { get; set; }

            [Name(name: "MarketID")]
            public string MarketID { get; set; }

            [Name(name: "ProdType")]
            public string ProdType { get; set; }

            [Name(name: "ProdName")]
            public string ProdName { get; set; }

            [Name(name: "Alias")]
            public string Alias { get; set; }

            [Name(name: "BuySell")]
            public string BuySell { get; set; }

            [Name(name: "OrderType")]
            public string OrderType { get; set; }

            [Name(name: "LimitPrice")]
            public decimal LimitPrice { get; set; }

            [Name(name: "OrderQuantity")]
            public decimal OrderQuantity { get; set; }

            [Name(name: "FilledQuantity")]
            public decimal FilledQuantity { get; set; }

            [Name(name: "DisclosedQuantity")]
            public decimal DisclosedQuantity { get; set; }

            [Name(name: "TimeInForce")]
            public string TimeInForce { get; set; }

            [Name(name: "TextA")]
            public string TextA { get; set; }

            [Name(name: "SiteOrderKey")]
            public string SiteOrderKey { get; set; }

            [Name(name: "OrderSource")]
            public string OrderSource { get; set; }
        }
        internal class Order_Data_Map : ClassMap<Orders_Data>
        {
            public Order_Data_Map()
            {
                Map(m => m.TimeStamp).Index(0).Name("TimeStamp");
                Map(m => m.MarketID).Index(1).Name("MarketID");
                Map(m => m.ProdType).Index(2).Name("ProdType");
                Map(m => m.ProdName).Index(3).Name("ProdName");
                Map(m => m.Alias).Index(4).Name("Alias");
                Map(m => m.BuySell).Index(5).Name("BuySell");
                Map(m => m.OrderType).Index(6).Name("OrderType");
                Map(m => m.LimitPrice).Index(7).Name("LimitPrice");
                Map(m => m.OrderQuantity).Index(8).Name("OrderQuantity");
                Map(m => m.FilledQuantity).Index(9).Name("FilledQuantity");
                Map(m => m.DisclosedQuantity).Index(10).Name("DisclosedQuantity");
                Map(m => m.TimeInForce).Index(11).Name("TimeInForce");
                Map(m => m.TextA).Index(12).Name("TextA");
                Map(m => m.SiteOrderKey).Index(13).Name("SiteOrderKey");
                Map(m => m.OrderSource).Index(13).Name("OrderSource");

            }
        }

        public class Orders_Data_1
        {
            [Name(name: "TimeStamp")]
            public DateTime TimeStamp { get; set; }

            [Name(name: "Instrument")]
            public Instrument Instrument { get; set; }

            [Name(name: "BuySell")]
            public BuySell BuySell { get; set; }

            [Name(name: "OrderType")]
            public OrderType OrderType { get; set; }

            [Name(name: "LimitPrice")]
            public Price LimitPrice { get; set; }

            [Name(name: "OrderQuantity")]
            public Quantity OrderQuantity { get; set; }

            [Name(name: "FilledQuantity")]
            public Quantity FilledQuantity { get; set; }

            [Name(name: "DisclosedQuantity")]
            public Quantity DisclosedQuantity { get; set; }

            [Name(name: "TimeInForce")]
            public TimeInForce TimeInForce { get; set; }

            [Name(name: "TextA")]
            public string TextA { get; set; }

            [Name(name: "SiteOrderKey")]
            public string SiteOrderKey { get; set; }

            [Name(name: "OrderSource")]
            public string OrderSource { get; set; }
        }
        public class Order_Data_Map_1 : ClassMap<Orders_Data_1>
        {
            public Order_Data_Map_1()
            {
                Map(m => m.TimeStamp).Index(0).Name("TimeStamp");
                Map(m => m.Instrument).Index(1).Name("Instrument");
                Map(m => m.BuySell).Index(5).Name("BuySell");
                Map(m => m.OrderType).Index(6).Name("OrderType");
                Map(m => m.LimitPrice).Index(7).Name("LimitPrice");
                Map(m => m.OrderQuantity).Index(8).Name("OrderQuantity");
                Map(m => m.FilledQuantity).Index(9).Name("FilledQuantity");
                Map(m => m.DisclosedQuantity).Index(10).Name("DisclosedQuantity");
                Map(m => m.TimeInForce).Index(11).Name("TimeInForce");
                Map(m => m.TextA).Index(12).Name("TextA");
                Map(m => m.SiteOrderKey).Index(13).Name("SiteOrderKey");
                Map(m => m.OrderSource).Index(13).Name("OrderSource");
            }
        }

    }
}
