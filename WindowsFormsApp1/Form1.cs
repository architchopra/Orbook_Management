using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using tt_net_sdk;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Security.Cryptography;
using Serilog;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private TradeSubscription m_instrumentTradeSubscription = null;
        private IReadOnlyCollection<Account> m_accounts = null;

        private bool m_isOrderBookDownloaded = false;
        private bool m_isOrdersSynced = false;
        private object m_Lock = new object();
        private Order_Action_Functions Order_Action_Functions = null;


        private object m_Hold = new object();
        private object m_Resume = new object();
        private object m_delete = new object();
        private object m_OrderAdded = new object();
        private object m_OrderUpdated = new object();
        private object m_OrderDeleted = new object();
        private object m_OrderFilled = new object();
        private object m_Orders_Storing = new object();
        private object m_Orders_Routing = new object();
        private object m_Spike_Action = new object();
       

        private TTAPI m_api = null;
        private InstrumentLookup m_instrLookupRequest = null;
        private PriceSubscription m_priceSubsciption = null;
        private tt_net_sdk.WorkerDispatcher m_disp = null;
        private readonly string account_name = "XGJRE_CME"; // Enter your Account In
        private readonly int account_idx = 1;
        TradeSubscriptionTTAccountFilter tsiAF;

        ConcurrentDictionary<string, Order> or_orders = new ConcurrentDictionary<string, Order>();
       ConcurrentDictionary<string, Order> qs_orders = new ConcurrentDictionary<string, Order>();
       ConcurrentDictionary<string, Order> ase_or_orders = new ConcurrentDictionary<string, Order>();
       ConcurrentDictionary<string, Order> ase_qs_orders = new ConcurrentDictionary<string, Order>();
       public ConcurrentDictionary<string, Order> adl_orders = new ConcurrentDictionary<string, Order>();

        public ConcurrentDictionary<string, Order> or_orders_copy = null; // newConcurrentDictionary<string, Order>();
        public ConcurrentDictionary<string, Order> qs_orders_copy = null; // newConcurrentDictionary<string, Order>();
        public ConcurrentDictionary<string, Order> ase_or_orders_copy = null; // newConcurrentDictionary<string, Order>();
        public ConcurrentDictionary<string, Order> ase_qs_orders_copy = null; // newConcurrentDictionary<string, Order>();        



        // Instrument Information 
        private string m_market = "";
        private string m_product = "";
        private string m_prodType = "Synthetic";
        private string m_alias = "";
        private string pause_time = null;
        private string delete_time = null;
        private string delete_time_1 = null;
        private string resume_time = null;
        private string order_add_time = null;
        private DateTime pause_date = new DateTime();
        private DateTime delete_date = new DateTime();
        private DateTime delete_date_1 = new DateTime();
        private DateTime resume_date = new DateTime();
        private DateTime order_add_date = new DateTime();
        private DateTime order_added_from_file_time = new DateTime();


        private bool pause_play = false;
        private bool delete_add= false;
        private bool delete_only= false;
        private bool file_add= false;

        private bool or_pause = false;
        private bool adl_pause = false;
        private bool qs_pause = false;
        private bool ase_or_pause = false;
        private bool ase_qs_pause = false;

        private bool or_delete = false;
        private bool qs_delete = false;
        private bool ase_or_delete = false; 
        private bool ase_qs_delete = false;

        private bool or_delete_1 = false;
        private bool qs_delete_1 = false;
        private bool ase_or_delete_1 = false;
        private bool ase_qs_delete_1 = false;
        private bool or_add = false;
        private bool qs_add = false;
        private bool ase_or_add = false;
        private bool ase_qs_add = false;

        private bool or_orders_on_hold = false;
        private bool adl_orders_on_hold = false;
        private bool qs_orders_on_hold = false;
        private bool ase_or_orders_on_hold = false;
        private bool ase_qs_orders_on_hold = false;

        private bool or_orders_stored = false;
        private bool qs_orders_stored = false;
        private bool ase_or_orders_stored = false;
        private bool ase_qs_orders_stored = false;

        private string OR_Orders_File_Path = @"C:\tt\order_details\or_orders.csv";
        private string QS_Orders_File_Path = @"C:\tt\order_details\qs_orders.csv";
        private string ASE_OR_Orders_File_Path = @"C:\tt\order_details\ase_or_orders.csv";
        private string ASE_QS_Orders_File_Path = @"C:\tt\order_details\ase_qs_orders.csv";
        private string OR_Orders_File_Path_i = @"C:\tt\order_details\or_orders_copy.csv";
        private string QS_Orders_File_Path_i = @"C:\tt\order_details\qs_orders_copy.csv";
        private string ASE_OR_Orders_File_Path_i = @"C:\tt\order_details\ase_or_orders_copy.csv";
        private string ASE_QS_Orders_File_Path_i = @"C:\tt\order_details\ase_qs_orders_copy.csv";
         

        System.Timers.Timer m_timer = null;
        System.Timers.Timer m_timer_2 = null;

        public void Start(tt_net_sdk.TTAPIOptions apiConfig)
        {
            m_disp = tt_net_sdk.Dispatcher.AttachWorkerDispatcher();
            m_disp.DispatchAction(() =>
            {
                Init(apiConfig);
            });

            m_disp.Run();
        }
        

        public void Init(tt_net_sdk.TTAPIOptions apiConfig)
        {
            ApiInitializeHandler apiInitializeHandler = new ApiInitializeHandler(ttNetApiInitHandler);
            TTAPI.ShutdownCompleted += TTAPI_ShutdownCompleted;
            TTAPI.CreateTTAPI(tt_net_sdk.Dispatcher.Current, apiConfig, apiInitializeHandler);
        }

        public void ttNetApiInitHandler(TTAPI api, ApiCreationException ex)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.File(@"LogFile.log").CreateLogger();
            if (ex == null)
            {
                Log.Information("TT.NET SDK Initialization Complete");

                // Authenticate your credentials
                m_api = api;
                m_api.TTAPIStatusUpdate += new EventHandler<TTAPIStatusUpdateEventArgs>(m_api_TTAPIStatusUpdate);
                m_api.Start();
            }
            else if (ex.IsRecoverable)
            {
                // this is in informational update from the SDK
                Log.Information("TT.NET SDK Initialization Message: {0}", ex.Message);
                if (ex.Code == ApiCreationException.ApiCreationError.NewAPIVersionAvailable)
                {
                    // a newer version of the SDK is available - notify someone to upgrade
                }
            }
            else
            {
                Log.Information("TT.NET SDK Initialization Failed: {0}", ex.Message);
                if (ex.Code == ApiCreationException.ApiCreationError.NewAPIVersionRequired)
                {
                    // do something to upgrade the SDK package since it will not start until it is upgraded 
                    // to the minimum version noted in the exception message
                }
                Dispose();
            }
        }

        public void m_api_TTAPIStatusUpdate(object sender, TTAPIStatusUpdateEventArgs e)
        {
            Log.Information("TTAPIStatusUpdate: {0}", e);
            if (e.IsReady == false)
            {
                // TODO: Do any connection lost processing here
                return;
            }
          
            //
            if (object.ReferenceEquals(m_instrLookupRequest, null) == false)
                return;

            // Status is up and we have not started a subscription yet

            // connection to TT is established
            Log.Information("TT.NET SDK Authenticated");
            /*
            MarketId marketKey = Market.GetMarketIdFromName(m_market);
            
            */
            Order_Action_Functions = new Order_Action_Functions();
            Order_Action_Functions.dispatcher = tt_net_sdk.Dispatcher.Current;
            m_instrumentTradeSubscription = new TradeSubscription(tt_net_sdk.Dispatcher.Current);
            tsiAF = new TradeSubscriptionTTAccountFilter(account_name, false, "Acct Filter");
            m_instrumentTradeSubscription.SetFilter(tsiAF);

            m_instrumentTradeSubscription.OrderUpdated += new EventHandler<OrderUpdatedEventArgs>(m_instrumentTradeSubscription_OrderUpdated);
            m_instrumentTradeSubscription.OrderAdded += new EventHandler<OrderAddedEventArgs>(m_instrumentTradeSubscription_OrderAdded);
            m_instrumentTradeSubscription.OrderDeleted += new EventHandler<OrderDeletedEventArgs>(m_instrumentTradeSubscription_OrderDeleted);
            m_instrumentTradeSubscription.OrderFilled += new EventHandler<OrderFilledEventArgs>(m_instrumentTradeSubscription_OrderFilled);
            m_instrumentTradeSubscription.OrderRejected += new EventHandler<OrderRejectedEventArgs>(m_instrumentTradeSubscription_OrderRejected);
            m_instrumentTradeSubscription.OrderBookDownload += new EventHandler<OrderBookDownloadEventArgs>(m_instrumentTradeSubscription_OrderBookDownload);
            m_instrumentTradeSubscription.Start();
            m_timer = new System.Timers.Timer()
            {
                Interval = 1000,
                Enabled = true,
                AutoReset = true
            };
            m_timer.Elapsed += new ElapsedEventHandler(m_pause_play_UpdateHandler);
            m_timer.Start();
            m_accounts = m_api.Accounts;
            comboBox1.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"," " });
            comboBox2.Items.AddRange(new string[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20",
                                                    "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40",
                                                    "41", "42", "43", "44", "45", "46", "47", "48", "49", "50","51", "52", "53", "54", "55", "56", "57", "58", "59", "60"," "});
            comboBox3.Items.AddRange(new string[] { "AM", "PM" });

            comboBox4.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", " " });
            comboBox5.Items.AddRange(new string[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20",
                                                    "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40",
                                                    "41", "42", "43", "44", "45", "46", "47", "48", "49", "50","51", "52", "53", "54", "55", "56", "57", "58", "59", "60"," "});
            comboBox6.Items.AddRange(new string[] { "AM", "PM" });

            comboBox7.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" });
            comboBox8.Items.AddRange(new string[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20",
                                                    "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40",
                                                    "41", "42", "43", "44", "45", "46", "47", "48", "49", "50","51", "52", "53", "54", "55", "56", "57", "58", "59", "60"});
            comboBox9.Items.AddRange(new string[] { "AM", "PM" });
            comboBox10.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" });
            comboBox11.Items.AddRange(new string[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20",
                                                    "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40",
                                                    "41", "42", "43", "44", "45", "46", "47", "48", "49", "50","51", "52", "53", "54", "55", "56", "57", "58", "59", "60"});
            comboBox12.Items.AddRange(new string[] { "AM", "PM" });
            comboBox13.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" });
            comboBox14.Items.AddRange(new string[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20",
                                                    "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40",
                                                    "41", "42", "43", "44", "45", "46", "47", "48", "49", "50","51", "52", "53", "54", "55", "56", "57", "58", "59", "60"});
            comboBox15.Items.AddRange(new string[] { "AM", "PM" });
            comboBox16.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" });
            comboBox17.Items.AddRange(new string[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20",
                                                    "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "40",
                                                    "41", "42", "43", "44", "45", "46", "47", "48", "49", "50","51", "52", "53", "54", "55", "56", "57", "58", "59", "60"});
            comboBox18.Items.AddRange(new string[] { "AM", "PM" });
            m_timer_2 = new System.Timers.Timer()
            {
                Interval = 1000,
                Enabled = true,
                AutoReset = true
            };
            m_timer_2.Elapsed += new ElapsedEventHandler(m_delete_order_handler);
            m_timer_2.Start();

        }
        private void m_pause_play_UpdateHandler(object sender, EventArgs e)
        {
            if (m_isOrderBookDownloaded)
            {
                
                /* if (or_orders != null & or_pause & !or_cr_checked)
                 {
                     foreach (string order_key in or_orders.Keys)
                     {
                         if (m_instrumentTradeSubscription.Orders.ContainsKey(order_key))
                         {
                             Log.Information("O/R Is On Hold: " + or_orders[order_key].IsOnHold);
                             Log.Information("O/R Status: " + or_orders[order_key].OrdStatus.ToString());
                             Log.Information("O/R Status7x: " + or_orders[order_key].Status7x.ToString());
                         }
                         else
                         {
                             Log.Information("OR Noooo");
                         }
                     }
                 }
                 if (qs_orders != null & qs_checked & !qs_cr_checked)
                 {
                     foreach (string order_key in qs_orders.Keys)
                     {
                         if (m_instrumentTradeSubscription.Orders.ContainsKey(order_key))
                         {
                             Log.Information("QS Is On Hold: " + qs_orders[order_key].IsOnHold);
                             Log.Information("QS Status: " + qs_orders[order_key].OrdStatus.ToString());
                             Log.Information("QS Status7x: " + qs_orders[order_key].Status7x.ToString());
                         }
                         else
                         {
                             Log.Information("QS Noooo");
                         }
                     }
                 }
                 if (ase_or_orders != null & ase_or_checked & !ase_or_cr_checked)
                 {
                     foreach (string order_key in ase_or_orders.Keys)
                     {
                         if (m_instrumentTradeSubscription.Orders.ContainsKey(order_key))
                         {
                             Log.Information("ASE O/R Is On Hold: " + ase_or_orders[order_key].IsOnHold);
                         }
                         else
                         {
                             Log.Information("ASE OR Noooo");
                         }
                     }
                 }
                 if (ase_qs_orders != null & ase_qs_checked & !ase_qs_cr_checked)
                 {
                     foreach (string order_key in ase_qs_orders.Keys)
                     {
                         if (m_instrumentTradeSubscription.Orders.ContainsKey(order_key))
                         {
                             Log.Information("ASE QS Is On Hold: " + ase_qs_orders[order_key].IsOnHold);
                         }
                         else
                         {
                             Log.Information("ASE QS Noooo");
                         }
                     }
                 }*/
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                // Actual Order Management:
                if (pause_play)
                {
                    lock (m_Hold)
                    {
                        // For Holding O/R Orders:
                        if (DateTime.Today.Date == pause_date & DateTime.Now.ToString("h:mm tt") == pause_time & adl_orders != null & !adl_orders_on_hold & adl_pause)
                        {
                            adl_orders_on_hold = true;
                            /*textBox5.Invoke((MethodInvoker)delegate
                            {
                                // Running on the UI thread
                                textBox5.BackColor = Color.Green;
                            });*/
                            Log.Information("In adl delete");
                            //form2.richTextBox1.AppendText("In O/R Hold");                        
                            Order_Action_Functions.Delete_Orders_without_storing(adl_orders, "ADL", m_instrumentTradeSubscription);
                            pause_play = false;

                        }
                        if (DateTime.Today.Date == pause_date & DateTime.Now.ToString("h:mm tt") == pause_time & or_orders != null & !or_orders_on_hold & or_pause)
                        {
                            or_orders_on_hold = true;
                            /*textBox5.Invoke((MethodInvoker)delegate
                            {
                                // Running on the UI thread
                                textBox5.BackColor = Color.Green;
                            });*/
                            Log.Information("In O/R Hold");
                            //form2.richTextBox1.AppendText("In O/R Hold");                        
                            Order_Action_Functions.Hold_Orders(or_orders, "O/R", m_instrumentTradeSubscription);
                            pause_play = false;

                        }
                        Log.Information(Convert.ToString(DateTime.Today.Date == pause_date));
                        Log.Information(Convert.ToString(DateTime.Now.ToString("h:mm tt") == pause_time));
                        
                        // For Holding QS Orders:
                        if (DateTime.Today.Date == pause_date & DateTime.Now.ToString("h:mm tt") == pause_time & qs_orders != null & !qs_orders_on_hold & qs_pause)
                        {
                            qs_orders_on_hold = true;
                            /* textBox5.Invoke((MethodInvoker)delegate
                             {
                                 // Running on the UI thread
                                 textBox5.BackColor = Color.Green;
                             });*/
                            Log.Information("In QS Hold");
                            //form2.richTextBox1.AppendText("In QS Hold");
                            Order_Action_Functions.Hold_Orders(qs_orders, "QS", m_instrumentTradeSubscription);
                            pause_play = false;

                        }

                        // For Holding ASE O/R Orders:
                        if (DateTime.Today.Date == pause_date & DateTime.Now.ToString("h:mm tt") == pause_time & ase_or_orders != null & !ase_or_orders_on_hold & ase_or_pause)
                        {
                            ase_or_orders_on_hold = true;
                            /* textBox5.Invoke((MethodInvoker)delegate
                             {
                                 // Running on the UI thread
                                 textBox5.BackColor = Color.Green;
                             });*/
                            Log.Information("In ASE O/R Hold");
                            //form2.richTextBox1.AppendText("In ASE O/R Hold");
                            Order_Action_Functions.Hold_Orders(ase_or_orders, "ASE O/R", m_instrumentTradeSubscription);
                            pause_play = false;

                        }

                        // For Holding ASE QS Orders:
                        if (DateTime.Today.Date == pause_date & DateTime.Now.ToString("h:mm tt") == pause_time & ase_qs_orders != null & !ase_qs_orders_on_hold & ase_qs_pause)
                        {
                            ase_qs_orders_on_hold = true;
                            /* textBox5.Invoke((MethodInvoker)delegate
                             {
                                 // Running on the UI thread
                                 textBox5.BackColor = Color.Green;
                             });*/
                            Log.Information("In ASE QS Hold");
                            //form2.richTextBox1.AppendText("In ASE QS Hold");
                            Order_Action_Functions.Hold_Orders(ase_qs_orders, "ASE QS", m_instrumentTradeSubscription);
                            pause_play = false;
                        }
                        if (or_orders_on_hold || qs_orders_on_hold || ase_or_orders_on_hold || ase_qs_orders_on_hold||adl_orders_on_hold)
                        {
                            Environment.Exit(420);
                        }

                    }
                    
                }
                lock (m_Resume)
                {
                    // For Resuming O/R Orders:
                  /*  Log.Information(DateTime.Now.ToString("h:mm tt"));
                    Log.Information(DateTime.Today.Date == resume_date);
                    Log.Information(DateTime.Now.ToString("h:mm tt") == resume_time);
                    Log.Information(or_orders != null);
                    Log.Information(or_orders_on_hold);
                    Log.Information(or_pause);*/
                    if (DateTime.Today.Date == resume_date & DateTime.Now.ToString("h:mm tt") == resume_time & or_orders != null & or_pause)
                    {
                        
                        /*textBox5.Invoke((MethodInvoker)delegate
                        {
                            // Running on the UI thread
                            textBox5.BackColor = Color.Red;
                        });*/
                        Log.Information("In O/R Resume");
                        //form2.richTextBox1.AppendText("In O/R Resume");                        
                        Order_Action_Functions.Resume_Orders(or_orders, "O/R", m_instrumentTradeSubscription);
                        or_pause= false;
                    }

                    // For Resuming QS Orders:
                    if (DateTime.Today.Date == resume_date & DateTime.Now.ToString("h:mm tt") == resume_time & qs_orders != null  & qs_pause)
                    {
                        
                        /*textBox5.Invoke((MethodInvoker)delegate
                        {
                            // Running on the UI thread
                            textBox5.BackColor = Color.Red;
                        });*/
                        Log.Information("In QS Resume");
                        //form2.richTextBox1.AppendText("In QS Resume");
                        Order_Action_Functions.Resume_Orders(qs_orders, "QS", m_instrumentTradeSubscription);
                        qs_pause= false;
                    }

                    // For Resuming ASE O/R Orders:
                    if (DateTime.Today.Date == resume_date & DateTime.Now.ToString("h:mm tt") == resume_time & ase_or_orders != null  & ase_or_pause)
                    {
                       
                        /*textBox5.Invoke((MethodInvoker)delegate
                        {
                            // Running on the UI thread
                            textBox5.BackColor = Color.Red;
                        });*/
                        Log.Information("In ASE O/R Resume");
                        //form2.richTextBox1.AppendText("In ASE O/R Resume");
                        Order_Action_Functions.Resume_Orders(ase_or_orders, "ASE O/R", m_instrumentTradeSubscription);
                        ase_or_pause= false;
                    }

                    // For Resuming ASE QS Orders:
                    if (DateTime.Today.Date == resume_date & DateTime.Now.ToString("h:mm tt") == resume_time & ase_qs_orders != null  & ase_qs_pause)
                    {
                       
                        Log.Information("In ASE QS Resume");
                        //form2.richTextBox1.AppendText("In ASE QS Resume");
                        Order_Action_Functions.Resume_Orders(ase_qs_orders, "ASE QS", m_instrumentTradeSubscription);
                        ase_qs_pause= false;
                    }
                }
            }
        }
        private void m_delete_order_handler(object sender, EventArgs e)
        {
            if (m_isOrderBookDownloaded & (delete_add | file_add))
            {
                lock (m_Orders_Storing)
                {
                    /*Log.Information("\n inside delete function{0},{1},{2},{3},{4}", DateTime.Today.Date == delete_date, DateTime.Now.ToString("h:mm tt") == delete_time, or_orders != null, or_delete, !or_orders_stored);*/
                    // For Storing O/R Orders:
                    if (DateTime.Today.Date == delete_date & DateTime.Now.ToString("h:mm tt") == delete_time & or_orders != null & !or_orders_stored & or_delete)
                    {
                        or_orders_stored = true;
                   
                        or_orders_copy = new ConcurrentDictionary<string, Order>(or_orders);
                        Log.Information("In O/R Storing");
                       
                        Order_Action_Functions.Write_in_Txt_File(or_orders_copy, OR_Orders_File_Path);
                        Order_Action_Functions.Delete_Orders_without_storing(or_orders, "O/R", m_instrumentTradeSubscription);
                    }

                    // For Storing QS Orders:
                    if (DateTime.Today.Date == delete_date & DateTime.Now.ToString("h:mm tt") == delete_time & qs_orders != null & !qs_orders_stored & qs_delete)
                    {
                        qs_orders_stored = true;
                     
                        qs_orders_copy = new ConcurrentDictionary<string, Order>(qs_orders);
                        Log.Information("In QS Storing");
                        Log.Information("QS Copy Count: " + qs_orders_copy.Count());
                        if (File.Exists(QS_Orders_File_Path))
                        {
                            File.Delete(QS_Orders_File_Path);
                        }
                        Order_Action_Functions.Write_in_Txt_File(qs_orders_copy, QS_Orders_File_Path);
                        Order_Action_Functions.Delete_Orders_without_storing(qs_orders, "QS", m_instrumentTradeSubscription);
                    }

                    // For Storing ASE O/R Orders:
                    if (DateTime.Today.Date == delete_date & DateTime.Now.ToString("h:mm tt") == delete_time & ase_or_orders != null & !ase_or_orders_stored & ase_or_delete)
                    {
                        ase_or_orders_stored = true;
                    
                        ase_or_orders_copy = new ConcurrentDictionary<string, Order>(ase_or_orders);
                        Log.Information("In ASE O/R Storing");
                        if (File.Exists(ASE_OR_Orders_File_Path))
                        {
                            File.Delete(ASE_OR_Orders_File_Path);
                        }
                        Order_Action_Functions.Write_in_Txt_File(ase_or_orders_copy, ASE_OR_Orders_File_Path);
                        Order_Action_Functions.Delete_Orders_without_storing(ase_or_orders, "ASE O/R", m_instrumentTradeSubscription);
                    }

                    // For Storing ASE QS Orders:
                    if (DateTime.Today.Date == delete_date & DateTime.Now.ToString("h:mm tt") == delete_time & ase_qs_orders != null & !ase_qs_orders_stored & ase_qs_delete)
                    {
                        ase_qs_orders_stored = true;
                        ase_qs_orders_copy = new ConcurrentDictionary<string, Order>(ase_qs_orders);
                        Log.Information("In ASE QS Storing");
                        if (File.Exists(ASE_QS_Orders_File_Path))
                        {
                            File.Delete(ASE_QS_Orders_File_Path);
                        }
                        Order_Action_Functions.Write_in_Txt_File(ase_qs_orders_copy, ASE_QS_Orders_File_Path);
                        Order_Action_Functions.Delete_Orders_without_storing(ase_qs_orders, "ASE QS", m_instrumentTradeSubscription);
                    }
                }
            
                lock (m_Orders_Routing)
                {
                    /*Log.Information("\n inside add function{0},{1},{2},{3},{4}", DateTime.Today.Date == order_add_date, DateTime.Now.ToString("h:mm tt") == order_add_time, or_orders_stored, (or_orders_copy != null | file_add), or_add);*/

                    // For Adding O/R Orders:

                    if (DateTime.Today.Date == order_add_date & DateTime.Now.ToString("h:mm tt") == order_add_time & ((or_orders_stored & or_orders_copy != null) | file_add) & or_add)
                    {
                        or_orders_stored = false;
                       /* textBox6.Invoke((MethodInvoker)delegate
                        {
                            // Running on the UI thread
                            textBox6.BackColor = Color.Red;
                        });*/
                        Log.Information("In O/R New Order");

                        if (!file_add)
                        {
                            if (Order_Action_Functions.or_orders_copy.Count != 0)
                            {
                                Order_Action_Functions.Send_Order_Normal(or_orders_copy, "O/R", m_instrumentTradeSubscription, m_accounts, account_idx);
                            }
                            else
                            {
                                Order_Action_Functions.Read_Txt_File(Order_Action_Functions.or_orders_copy, OR_Orders_File_Path, "OR", m_instrumentTradeSubscription, m_accounts, account_idx, false);
                                order_added_from_file_time = DateTime.Now;
                            }
                        }
                        else
                        {
                            if (Order_Action_Functions.or_orders_copy.Count != 0)
                            {
                                Log.Information("Dict Contains Data: Count: " + Order_Action_Functions.or_orders_copy.Count());
                                Order_Action_Functions.or_orders_copy.Clear();
                            }
                            Order_Action_Functions.Read_Txt_File(Order_Action_Functions.or_orders_copy, OR_Orders_File_Path, "OR", m_instrumentTradeSubscription, m_accounts, account_idx, false);
                            order_added_from_file_time = DateTime.Now;
                            file_add = false;
                        }
                      
                        if (File.Exists(OR_Orders_File_Path))
                        {
                            if (File.Exists(OR_Orders_File_Path_i))
                            {

                                File.Delete(OR_Orders_File_Path_i);
                            }
                            System.IO.FileInfo file = new System.IO.FileInfo(OR_Orders_File_Path_i);
                            file.Directory.Create();
                            File.Copy(OR_Orders_File_Path, OR_Orders_File_Path_i, true);
                            File.Delete(OR_Orders_File_Path);
                        }
                        or_add = false;
                    }

                    // For Adding QS Orders:
                    if (DateTime.Today.Date == order_add_date & DateTime.Now.ToString("h:mm tt") == order_add_time & ((qs_orders_stored & qs_orders_copy != null) | file_add) & qs_add)
                    {
                        qs_orders_stored = false;
                       /* textBox6.Invoke((MethodInvoker)delegate
                        {
                            // Running on the UI thread
                            textBox6.BackColor = Color.Red;
                        });*/
                        Log.Information("In QS New Order");

                        if (!file_add)
                        {
                            if (Order_Action_Functions.qs_orders_copy.Count != 0)
                            {
                                Order_Action_Functions.Send_Order_Normal(qs_orders_copy, "QS", m_instrumentTradeSubscription, m_accounts, account_idx);
                            }
                            else
                            {
                                Order_Action_Functions.Read_Txt_File(Order_Action_Functions.qs_orders_copy, QS_Orders_File_Path, "QS", m_instrumentTradeSubscription, m_accounts, account_idx, false);
                                order_added_from_file_time = DateTime.Now;
                            }
                        }
                        else
                        {
                            if (Order_Action_Functions.qs_orders_copy.Count != 0)
                            {
                                Log.Information("Dict Contains Data: Count: " + Order_Action_Functions.qs_orders_copy.Count());
                                Order_Action_Functions.qs_orders_copy.Clear();
                            }
                            Order_Action_Functions.Read_Txt_File(Order_Action_Functions.qs_orders_copy, QS_Orders_File_Path, "QS", m_instrumentTradeSubscription, m_accounts, account_idx, false);
                            order_added_from_file_time = DateTime.Now;
                            file_add = false;
                        }
                        if (File.Exists(QS_Orders_File_Path))
                        {
                            if (File.Exists(QS_Orders_File_Path_i))
                            {

                                File.Delete(QS_Orders_File_Path_i);
                            }
                            System.IO.FileInfo file = new System.IO.FileInfo(QS_Orders_File_Path_i);
                            file.Directory.Create();
                            File.Copy(QS_Orders_File_Path, QS_Orders_File_Path_i, true);
                            File.Delete(QS_Orders_File_Path);
                        }
                        qs_add = false;
                    }
                  

                    // For Adding ASE O/R Orders:
                    if (DateTime.Today.Date == order_add_date & DateTime.Now.ToString("h:mm tt") == order_add_time & ((ase_or_orders_stored & ase_or_orders_copy != null) | file_add) & ase_or_add)
                    {
                        ase_or_orders_stored = false;
                       /* textBox6.Invoke((MethodInvoker)delegate
                        {
                            // Running on the UI thread
                            textBox6.BackColor = Color.Red;
                        });*/
                        Log.Information("In ASE O/R New Order");

                        if (!file_add)
                        {
                            if (Order_Action_Functions.ase_or_orders_copy.Count != 0)
                            {
                                Order_Action_Functions.Send_Orders_Task_Creator(ase_or_orders_copy, "ASE O/R", m_instrumentTradeSubscription, m_accounts, account_idx);
                            }
                            else
                            {
                                Order_Action_Functions.Read_Txt_File(Order_Action_Functions.ase_or_orders_copy, ASE_OR_Orders_File_Path, "ASE OR", m_instrumentTradeSubscription, m_accounts, account_idx, true);
                                order_added_from_file_time = DateTime.Now;
                            }
                        }
                        else
                        {
                            if (Order_Action_Functions.ase_or_orders_copy.Count != 0)
                            {
                                Log.Information("Dict Contains Data: Count: " + Order_Action_Functions.ase_or_orders_copy.Count());
                                Order_Action_Functions.ase_or_orders_copy.Clear();
                            }
                            Order_Action_Functions.Read_Txt_File(Order_Action_Functions.ase_or_orders_copy, ASE_OR_Orders_File_Path, "ASE OR", m_instrumentTradeSubscription, m_accounts, account_idx, true);
                            order_added_from_file_time = DateTime.Now;
                        }
                        if (File.Exists(ASE_OR_Orders_File_Path))
                        {
                            if (File.Exists(ASE_OR_Orders_File_Path_i))
                            {

                                File.Delete(ASE_OR_Orders_File_Path_i);
                            }
                            System.IO.FileInfo file = new System.IO.FileInfo(ASE_OR_Orders_File_Path_i);
                            file.Directory.Create();
                            File.Copy(ASE_OR_Orders_File_Path, ASE_OR_Orders_File_Path_i, true);
                            File.Delete(ASE_OR_Orders_File_Path);
                        }
                        ase_or_add = false;
                    }

                    // For Adding ASE QS Orders:
                    if (DateTime.Today.Date == order_add_date & DateTime.Now.ToString("h:mm tt") == order_add_time & ((ase_qs_orders_stored & ase_qs_orders_copy != null) | file_add) & ase_qs_add)
                    {
                        ase_qs_orders_stored = false;
                     /*   textBox6.Invoke((MethodInvoker)delegate
                        {
                            // Running on the UI thread
                            textBox6.BackColor = Color.Red;
                        });*/
                        Log.Information("In ASE QS New Order");

                        if (!file_add)
                        {
                            if (Order_Action_Functions.ase_qs_orders_copy.Count != 0)
                            {
                                Order_Action_Functions.Send_Orders_Task_Creator(ase_qs_orders_copy, "ASE QS", m_instrumentTradeSubscription, m_accounts, account_idx);
                            }
                            else
                            {
                                Order_Action_Functions.Read_Txt_File(Order_Action_Functions.ase_qs_orders_copy, ASE_QS_Orders_File_Path, "ASE QS", m_instrumentTradeSubscription, m_accounts, account_idx, true);
                                order_added_from_file_time = DateTime.Now;
                            }
                        }
                        else
                        {
                            if (Order_Action_Functions.ase_qs_orders_copy.Count != 0)
                            {
                                Log.Information("Dict Contains Data: Count: " + Order_Action_Functions.ase_qs_orders_copy.Count());
                                Order_Action_Functions.ase_qs_orders_copy.Clear();
                            }
                            Order_Action_Functions.Read_Txt_File(Order_Action_Functions.ase_qs_orders_copy, ASE_QS_Orders_File_Path, "ASE QS", m_instrumentTradeSubscription, m_accounts, account_idx, true);
                            order_added_from_file_time = DateTime.Now;
                        }
                        if (File.Exists(ASE_QS_Orders_File_Path))
                        {
                            if (File.Exists(ASE_QS_Orders_File_Path_i))
                            {

                                File.Delete(ASE_QS_Orders_File_Path_i);
                            }
                            System.IO.FileInfo file = new System.IO.FileInfo(ASE_QS_Orders_File_Path_i);
                            file.Directory.Create();
                            File.Copy(ASE_QS_Orders_File_Path, ASE_QS_Orders_File_Path_i, true);
                            File.Delete(ASE_QS_Orders_File_Path);
                        }
                        ase_qs_add = false;

                    }
                }
            }
             if (m_isOrderBookDownloaded & delete_only)
            {
                lock (m_delete)
                {
                    
                    if (DateTime.Today.Date == delete_date_1 & DateTime.Now.ToString("h:mm tt") == delete_time_1 & or_orders != null & or_delete_1)
                    {
                       
                       
                        Log.Information("In O/R delete");
                                              
                        Order_Action_Functions.Delete_Orders_without_storing(or_orders, "O/R", m_instrumentTradeSubscription);
                        delete_only = false;
                    }

                    // For Holding QS Orders:
                    if (DateTime.Today.Date == delete_date_1 & DateTime.Now.ToString("h:mm tt") == delete_time_1 & qs_orders != null & qs_delete_1)
                    {
                       
                       
                        Log.Information("In QS Delete");
                        //form2.richTextBox1.AppendText("In QS Hold");
                        Order_Action_Functions.Delete_Orders_without_storing(qs_orders, "QS", m_instrumentTradeSubscription);
                        delete_only = false;

                    }

                    // For Holding ASE O/R Orders:
                    if (DateTime.Today.Date == delete_date_1 & DateTime.Now.ToString("h:mm tt") == delete_time_1 & ase_or_orders != null &  ase_or_delete_1)
                    {
                       
                        
                        Log.Information("In ASE O/R Delete");
                        //form2.richTextBox1.AppendText("In ASE O/R Hold");
                        Order_Action_Functions.Delete_Orders_without_storing(ase_or_orders, "ASE O/R", m_instrumentTradeSubscription);
                        delete_only = false;

                    }

                    // For Holding ASE QS Orders:
                    if (DateTime.Today.Date == delete_date_1 & DateTime.Now.ToString("h:mm tt") == delete_time_1 & ase_qs_orders != null & ase_qs_delete_1)
                    {
                       
                        Log.Information("In ASE QS delete");
                        //form2.richTextBox1.AppendText("In ASE QS Hold");
                        Order_Action_Functions.Delete_Orders_without_storing(ase_qs_orders, "ASE QS", m_instrumentTradeSubscription);
                        delete_only = false;

                    }
                }
            }
        }

        public void Set_Default_Values_Ord_Management()
        {
            Log.Information("aaya");
            DateTime market_pause_time = DateTime.Parse("10:52:00 PM");

            if (DateTime.Today.DayOfWeek == DayOfWeek.Sunday & DateTime.Now.TimeOfDay > market_pause_time.TimeOfDay)
            {
                // Adding Default Values:
                checkBox3.Checked = true;
                checkBox4.Checked = true;

                dateTimePicker3.Value = DateTime.Today;
                /*comboBox1.Text = "2";
                comboBox2.Text = "28";
                comboBox3.Text = "AM";*/
                comboBox4.Text = "11";
                comboBox5.Text = "01";
                comboBox6.Text = "PM";

                

                button1.PerformClick();
            }
        }

        void m_instrumentTradeSubscription_OrderBookDownload(object sender, OrderBookDownloadEventArgs e)
        {
            Log.Information("Orderbook downloaded...");
            //form2.richTextBox1.AppendText("\nOrderbook downloaded...");

            List<Order> all_orders_lst = e.Orders.ToList();
            foreach (Order ord in all_orders_lst)
            {
                /*
                    Log.Information(ord.Instrument);
                    Log.Information("ord.OrderSource");
                    Log.Information(ord.OrderSource);
                    Log.Information("ord.Algo");
                    Log.Information(ord.Algo);
                    Log.Information("ord.AlgoInstumentId");
                    Log.Information(ord.AlgoInstumentId);
                    Log.Information("ord.TradingStrategy");
                    Log.Information(ord.TradingStrategy);
                    Log.Information("ord.SyntheticType");
                    Log.Information(ord.SyntheticType);
                    Log.Information("ord.OwnerAlgoId");
                    Log.Information(ord.OwnerAlgoId);
                    Log.Information("ord.GroupId");
                    Log.Information(ord.GroupId);
                    Log.Information("ord.OrderTag");
                    Log.Information(ord.OrderTag);
                    Log.Information("ord.SyntheticStatus");
                    Log.Information(ord.SyntheticStatus);*/

                
                    if (ord.OrderSource == OrderSource.Ttd|| ord.OrderSource == OrderSource.DotnetApiClt)
                {
                    if (ord.AlgoInstumentId != 0)
                    {
                        adl_orders.TryAdd(ord.SiteOrderKey, ord);
                    }

                    else if (ord.OrderSource != OrderSource.Adl)
                    {
                        Log.Information(Convert.ToString(ord.Instrument));
                        if (ord.Instrument.Product.Type == ProductType.Future & !or_orders.ContainsKey(ord.SiteOrderKey) & ((ord.OrderSource != OrderSource.Ase) & (ord.OrderSource != OrderSource.PrimeAse)))
                        {
                            or_orders.TryAdd(ord.SiteOrderKey, ord);
                        }
                        if (ord.Instrument.Product.Type == ProductType.MultilegInstrument & !qs_orders.ContainsKey(ord.SiteOrderKey) & ((ord.OrderSource != OrderSource.Ase) & (ord.OrderSource != OrderSource.PrimeAse)))
                        {
                            qs_orders.TryAdd(ord.SiteOrderKey, ord);
                        }
                        if (ord.Instrument.Product.Type == ProductType.Synthetic & ord.Algo == null)
                        {
                            SpreadDetails sp_detail = ord.Instrument.GetSpreadDetails();
                            SpreadLegDetails sp_leg = sp_detail.GetLeg(0);

                            if (sp_leg.Instrument.Product.Type == ProductType.Future & !ase_or_orders.ContainsKey(ord.SiteOrderKey))
                            {
                                ase_or_orders.TryAdd(ord.SiteOrderKey, ord);
                            }
                            if (sp_leg.Instrument.Product.Type == ProductType.MultilegInstrument & !ase_qs_orders.ContainsKey(ord.SiteOrderKey))
                            {
                                ase_qs_orders.TryAdd(ord.SiteOrderKey, ord);
                            }
                        }
                    }
                }
            }



            m_isOrderBookDownloaded = true;
            Set_Default_Values_Ord_Management();
           /* if (!UI_Working)
            {
                UI_Working = true;
                Show_Order_Management_Inputs();
            }*/
        }
        void m_instrumentTradeSubscription_OrderRejected(object sender, OrderRejectedEventArgs e)
        {
            Log.Information("\nOrderRejected [{0}]", e.Order.SiteOrderKey);
            //form2.richTextBox1.AppendText("\n\nOrderRejected [{0}]" + e.Order.SiteOrderKey);
        }
        void m_instrumentTradeSubscription_OrderFilled(object sender, OrderFilledEventArgs e)
        {
            /*lock (m_OrderFilled)
            {*/
            if (e.FillType == tt_net_sdk.FillType.Full)
            {
                Log.Information("\nOrderFullyFilled [{0}]: {1}@{2}", e.Fill.SiteOrderKey, e.Fill.Quantity, e.Fill.MatchPrice);

                if (e.OldOrder.OrderSource == OrderSource.Ttd || e.OldOrder.OrderSource == OrderSource.DotnetApiClt)
                {
                    if (e.OldOrder.AlgoInstumentId != 0)
                    {
                        adl_orders.TryRemove(e.OldOrder.SiteOrderKey, out Order orders);
                    }

                    else if (e.OldOrder.OrderSource != OrderSource.Adl)
                    {
                        if (e.OldOrder.Instrument.Product.Type == ProductType.Future & or_orders.ContainsKey(e.OldOrder.SiteOrderKey)
                            & ((e.OldOrder.OrderSource != OrderSource.Ase) & (e.OldOrder.OrderSource != OrderSource.PrimeAse)))
                        {
                            Log.Information("Filled: O/R Contains Key: " + or_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                            //form2.richTextBox1.AppendText("\nFilled: O/R Contains Key: " + or_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                            or_orders.TryRemove(e.OldOrder.SiteOrderKey, out Order orders);
                            Log.Information("Filled: O/R Count: " + or_orders.Count());
                            //form2.richTextBox1.AppendText("\nFilled: O/R Count: " + or_orders.Count());
                        }
                        if (e.OldOrder.Instrument.Product.Type == ProductType.MultilegInstrument & qs_orders.ContainsKey(e.OldOrder.SiteOrderKey)
                            & ((e.OldOrder.OrderSource != OrderSource.Ase) & (e.OldOrder.OrderSource != OrderSource.PrimeAse)))
                        {
                            Log.Information("Filled: QS Contains Key: " + qs_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                            //form2.richTextBox1.AppendText("\nFilled: QS Contains Key: " + qs_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                            qs_orders.TryRemove(e.OldOrder.SiteOrderKey, out Order order);
                            Log.Information("Filled: QS Count: " + qs_orders.Count());
                            //form2.richTextBox1.AppendText("\nFilled: QS Count: " + qs_orders.Count());
                        }
                    }
                }
            }
            else
            {
                if (e.OldOrder.OrderSource == OrderSource.Ttd || e.OldOrder.OrderSource == OrderSource.DotnetApiClt)
                {
                    if (e.OldOrder.AlgoInstumentId != 0)
                    {
                        adl_orders[e.Fill.SiteOrderKey] = e.NewOrder;
                    }

                    else if (e.OldOrder.OrderSource != OrderSource.Adl)
                    {
                        Log.Information("\nOrderPartiallyFilled [{0}]: {1}@{2}", e.Fill.SiteOrderKey, e.Fill.Quantity, e.Fill.MatchPrice);
                        if (e.Fill.Instrument.Product.Type == ProductType.Future & ((e.OldOrder.OrderSource != OrderSource.Ase) & (e.OldOrder.OrderSource != OrderSource.PrimeAse)))
                        {
                            Log.Information("Partial Filled: O/R Contains Key: " + or_orders.ContainsKey(e.Fill.SiteOrderKey));
                            //form2.richTextBox1.AppendText("\nFilled: O/R Contains Key: " + or_orders.ContainsKey(e.Fill.SiteOrderKey));
                            or_orders[e.Fill.SiteOrderKey] = e.NewOrder;
                            Log.Information("Partial Filled: O/R Count: " + or_orders.Count());
                            //form2.richTextBox1.AppendText("\nFilled: O/R Count: " + or_orders.Count());
                        }
                        if (e.Fill.Instrument.Product.Type == ProductType.MultilegInstrument & ((e.OldOrder.OrderSource != OrderSource.Ase) & (e.OldOrder.OrderSource != OrderSource.PrimeAse)))
                        {
                            Log.Information("Partial Filled: QS Contains Key: " + qs_orders.ContainsKey(e.Fill.SiteOrderKey));
                            //form2.richTextBox1.AppendText("\nFilled: QS Contains Key: " + qs_orders.ContainsKey(e.Fill.SiteOrderKey));
                            qs_orders[e.Fill.SiteOrderKey] = e.NewOrder;
                            Log.Information("Partial Filled: QS Count: " + qs_orders.Count());
                            //form2.richTextBox1.AppendText("\nFilled: QS Count: " + qs_orders.Count());
                        }
                        if (e.Fill.Instrument.Product.Type == ProductType.Synthetic & e.NewOrder.Algo == null)
                        {
                            Log.Information("In Synthetic Partial Fills");
                            SpreadDetails sp_detail = e.Fill.Instrument.GetSpreadDetails();
                            SpreadLegDetails sp_leg = sp_detail.GetLeg(0);

                            if (sp_leg.Instrument.Product.Type == ProductType.Future & ase_or_orders.ContainsKey(e.Fill.SiteOrderKey))
                            {
                                Log.Information("Partial Filled: ASE O/R Contains Key: " + ase_or_orders.ContainsKey(e.Fill.SiteOrderKey));
                                //form2.richTextBox1.AppendText("\nFilled: ASE O/R Contains Key: " + ase_or_orders.ContainsKey(e.Fill.SiteOrderKey));
                                ase_or_orders[e.Fill.SiteOrderKey] = e.NewOrder;
                                Log.Information("Partial Filled: ASE O/R Count: " + ase_or_orders.Count());
                                //form2.richTextBox1.AppendText("\nFilled: ASE O/R Count: " + ase_or_orders.Count());
                            }
                            if (sp_leg.Instrument.Product.Type == ProductType.MultilegInstrument & ase_qs_orders.ContainsKey(e.Fill.SiteOrderKey))
                            {
                                Log.Information("Partial Filled: ASE QS Contains Key: " + ase_qs_orders.ContainsKey(e.Fill.SiteOrderKey));
                                //form2.richTextBox1.AppendText("\nFilled: ASE QS Contains Key: " + ase_qs_orders.ContainsKey(e.Fill.SiteOrderKey));
                                ase_qs_orders[e.Fill.SiteOrderKey] = e.NewOrder;
                                Log.Information("Partial Filled: ASE QS Count: " + ase_qs_orders.Count());
                                //form2.richTextBox1.AppendText("\nFilled: ASE QS Count: " + ase_qs_orders.Count());
                            }
                        }
                    }
                }
            }
        }
        void m_instrumentTradeSubscription_OrderDeleted(object sender, OrderDeletedEventArgs e)
        {
            /* lock (m_OrderDeleted)
             {*/
            Log.Information("\nOrderDeleted [{0}]", e.OldOrder.SiteOrderKey);

            if (e.OldOrder.OrderSource == OrderSource.Ttd || e.OldOrder.OrderSource == OrderSource.DotnetApiClt)
            {
                if (e.OldOrder.AlgoInstumentId != 0)
                {
                    adl_orders.TryRemove(e.OldOrder.SiteOrderKey, out Order orders);
                }

                else if (e.OldOrder.OrderSource != OrderSource.Adl)
                {
                    if (e.OldOrder.Instrument.Product.Type == ProductType.Future & ((e.OldOrder.OrderSource != OrderSource.Ase) & (e.OldOrder.OrderSource != OrderSource.PrimeAse)))
                    {
                        Log.Information("Deleted: O/R Contains Key: " + or_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                        //form2.richTextBox1.AppendText("\nDeleted: O/R Contains Key: " + or_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                        or_orders.TryRemove(e.OldOrder.SiteOrderKey, out Order orders);
                        Log.Information("Deleted: O/R Count: " + or_orders.Count());
                        foreach (string key in or_orders.Keys)
                        {
                            Log.Information(or_orders[key].Instrument + " " + or_orders[key].LimitPrice.ToString() + " " + or_orders[key].WorkingQuantity.ToString() + " " + key);
                            Log.Information(or_orders[key].BuySell.ToString() + " " + or_orders[key].Instrument.Product.Type.ToString());
                        }
                        //form2.richTextBox1.AppendText("\nDeleted: O/R Count: " + or_orders.Count());
                    }
                    if (e.OldOrder.Instrument.Product.Type == ProductType.MultilegInstrument & ((e.OldOrder.OrderSource != OrderSource.Ase) & (e.OldOrder.OrderSource != OrderSource.PrimeAse)))
                    {
                        Log.Information("Deleted: QS Contains Key: " + qs_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                        //form2.richTextBox1.AppendText("\nDeleted: QS Contains Key: " + qs_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                        qs_orders.TryRemove(e.OldOrder.SiteOrderKey, out Order orders);
                        Log.Information("Deleted: QS Count: " + qs_orders.Count());
                        //form2.richTextBox1.AppendText("\nDeleted: QS Count: " + qs_orders.Count());
                    }
                    if (e.OldOrder.Instrument.Product.Type == ProductType.Synthetic & e.OldOrder.Algo == null)
                    {
                        SpreadDetails sp_detail = e.OldOrder.Instrument.GetSpreadDetails();
                        SpreadLegDetails sp_leg = sp_detail.GetLeg(0);

                        if (sp_leg.Instrument.Product.Type == ProductType.Future)
                        {
                            Log.Information("Deleted: ASE O/R Contains Key: " + ase_or_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                            //form2.richTextBox1.AppendText("\nDeleted: ASE O/R Contains Key: " + ase_or_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                            ase_or_orders.TryRemove(e.OldOrder.SiteOrderKey, out Order orders);
                            Log.Information("Deleted: ASE O/R Count: " + ase_or_orders.Count());
                            //form2.richTextBox1.AppendText("\nDeleted: ASE O/R Count: " + ase_or_orders.Count());
                        }
                        if (sp_leg.Instrument.Product.Type == ProductType.MultilegInstrument)
                        {
                            Log.Information("Deleted: ASE QS Contains Key: " + ase_qs_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                            //form2.richTextBox1.AppendText("\nDeleted: ASE QS Contains Key: " + ase_qs_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                            ase_qs_orders.TryRemove(e.OldOrder.SiteOrderKey, out Order orders);
                            Log.Information("Deleted: ASE QS Count: " + ase_qs_orders.Count());
                            //form2.richTextBox1.AppendText("\nDeleted: ASE QS Count: " + ase_qs_orders.Count());
                        }

                    }
                }
            }
        }
        void m_instrumentTradeSubscription_OrderAdded(object sender, OrderAddedEventArgs e)
        {
            /*lock (m_OrderAdded)
            {*/
                Log.Information("\nOrderAdded [{0}] {1}: {2}", e.Order.SiteOrderKey, e.Order.BuySell, e.Order.ToString());
            if (e.Order.OrderSource == OrderSource.Ttd || e.Order.OrderSource == OrderSource.DotnetApiClt)
            {
                if (e.Order.AlgoInstumentId != 0)
                {
                    adl_orders.TryAdd(e.Order.SiteOrderKey, e.Order);
                }

                else if (e.Order.OrderSource != OrderSource.Adl)
                {
                    if (e.Order.Instrument.Product.Type == ProductType.Future & !or_orders.ContainsKey(e.Order.SiteOrderKey) & ((e.Order.OrderSource != OrderSource.Ase) & (e.Order.OrderSource != OrderSource.PrimeAse)))
                    {
                        or_orders.TryAdd(e.Order.SiteOrderKey, e.Order);
                        Log.Information("Order Added: O/R Count: " + or_orders.Count());
                        //form2.richTextBox1.AppendText("\nOrder Added: O/R Count: " + or_orders.Count());
                    }
                    if (e.Order.Instrument.Product.Type == ProductType.MultilegInstrument & !qs_orders.ContainsKey(e.Order.SiteOrderKey) & ((e.Order.OrderSource != OrderSource.Ase) & (e.Order.OrderSource != OrderSource.PrimeAse)))
                    {
                        qs_orders.TryAdd(e.Order.SiteOrderKey, e.Order);
                        Log.Information("Order Added: QS Count: " + qs_orders.Count());
                        //form2.richTextBox1.AppendText("\nOrder Added: QS Count: " + qs_orders.Count());
                    }
                    if (e.Order.Instrument.Product.Type == ProductType.Synthetic & e.Order.Algo == null)
                    {
                        SpreadDetails sp_detail = e.Order.Instrument.GetSpreadDetails();
                        SpreadLegDetails sp_leg = sp_detail.GetLeg(0);

                        if (sp_leg.Instrument.Product.Type == ProductType.Future & !ase_or_orders.ContainsKey(e.Order.SiteOrderKey))
                        {
                            ase_or_orders.TryAdd(e.Order.SiteOrderKey, e.Order);
                            Log.Information("Order Added: ASE O/R Count: " + ase_or_orders.Count());
                            //form2.richTextBox1.AppendText("\nOrder Added: ASE O/R Count: " + ase_or_orders.Count());
                        }
                        if (sp_leg.Instrument.Product.Type == ProductType.MultilegInstrument & !ase_qs_orders.ContainsKey(e.Order.SiteOrderKey))
                        {
                            ase_qs_orders.TryAdd(e.Order.SiteOrderKey, e.Order);
                            Log.Information("Order Added: ASE QS Count: " + ase_qs_orders.Count());
                            //form2.richTextBox1.AppendText("\nOrder Added: ASE QS Count: " + ase_qs_orders.Count());
                        }
                    }
                }
            }
        }
        void m_instrumentTradeSubscription_OrderUpdated(object sender, OrderUpdatedEventArgs e)
        {
            /*lock (m_OrderUpdated)
            {*/
                Log.Information("\nOrderUpdated [{0}] with price={1}", e.OldOrder.SiteOrderKey, e.OldOrder.LimitPrice);
            if (e.NewOrder.OrderSource == OrderSource.Ttd ||e.NewOrder.OrderSource == OrderSource.DotnetApiClt)
            {
                if (e.NewOrder.AlgoInstumentId != 0)
                {
                    adl_orders[e.OldOrder.SiteOrderKey] = e.NewOrder;
                }

                else if (e.NewOrder.OrderSource != OrderSource.Adl)
                {
                    if (e.OldOrder.Instrument.Product.Type == ProductType.Future & or_orders.ContainsKey(e.OldOrder.SiteOrderKey)
                        & ((e.OldOrder.OrderSource != OrderSource.Ase) & (e.OldOrder.OrderSource != OrderSource.PrimeAse)))
                    {
                        Log.Information("Updated: O/R Contains Key: " + or_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                        //form2.richTextBox1.AppendText("\nUpdated: O/R Contains Key: " + or_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                        or_orders[e.OldOrder.SiteOrderKey] = e.NewOrder;
                        Log.Information("Updated: O/R Count: " + or_orders.Count() + " For: " + e.OldOrder.SiteOrderKey);
                        //form2.richTextBox1.AppendText("\nUpdated: O/R Count: " + or_orders.Count());
                    }
                    if (e.OldOrder.Instrument.Product.Type == ProductType.MultilegInstrument & qs_orders.ContainsKey(e.OldOrder.SiteOrderKey)
                        & ((e.OldOrder.OrderSource != OrderSource.Ase) & (e.OldOrder.OrderSource != OrderSource.PrimeAse)))
                    {
                        Log.Information("Updated: QS Contains Key: " + qs_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                        //form2.richTextBox1.AppendText("\nUpdated: QS Contains Key: " + qs_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                        qs_orders[e.OldOrder.SiteOrderKey] = e.NewOrder;
                        Log.Information("Updated: QS Count: " + qs_orders.Count() + " For: " + e.OldOrder.SiteOrderKey);
                        //form2.richTextBox1.AppendText("\nUpdated: QS Count: " + qs_orders.Count());
                    }
                    if (e.OldOrder.Instrument.Product.Type == ProductType.Synthetic & e.OldOrder.Algo == null)
                    {
                        SpreadDetails sp_detail = e.OldOrder.Instrument.GetSpreadDetails();
                        SpreadLegDetails sp_leg = sp_detail.GetLeg(0);

                        if (sp_leg.Instrument.Product.Type == ProductType.Future & ase_or_orders.ContainsKey(e.OldOrder.SiteOrderKey))
                        {
                            Log.Information("Updated: ASE O/R Contains Key: " + ase_or_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                            //form2.richTextBox1.AppendText("\nUpdated: ASE O/R Contains Key: " + ase_or_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                            ase_or_orders[e.OldOrder.SiteOrderKey] = e.NewOrder;
                            Log.Information("Updated: ASE O/R Count: " + ase_or_orders.Count() + " For: " + e.OldOrder.SiteOrderKey);
                            //form2.richTextBox1.AppendText("\nUpdated: ASE O/R Count: " + ase_or_orders.Count());
                        }
                        if (sp_leg.Instrument.Product.Type == ProductType.MultilegInstrument & ase_qs_orders.ContainsKey(e.OldOrder.SiteOrderKey))
                        {
                            Log.Information("Updated: ASE QS Contains Key: " + ase_qs_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                            //form2.richTextBox1.AppendText("\nUpdated: ASE QS Contains Key: " + ase_qs_orders.ContainsKey(e.OldOrder.SiteOrderKey));
                            ase_qs_orders[e.OldOrder.SiteOrderKey] = e.NewOrder;
                            Log.Information("Updated: ASE QS Count: " + ase_qs_orders.Count() + " For: " + e.OldOrder.SiteOrderKey);
                            //form2.richTextBox1.AppendText("\nUpdated: ASE QS Count: " + ase_qs_orders.Count());
                        }
                    }
                    /*            }*//**/
                }
            }
        }

        public void Dispose()
        {
            if (object.ReferenceEquals(m_instrLookupRequest, null) == false)
                m_instrLookupRequest.Dispose();

            if (object.ReferenceEquals(m_priceSubsciption, null) == false)
                m_priceSubsciption.Dispose();

            TTAPI.Shutdown();
        }

        public void TTAPI_ShutdownCompleted(object sender, EventArgs e)
        {
            Log.Information("TTAPI Shutdown completed");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (m_isOrderBookDownloaded)
            {
                pause_time = comboBox1.Text + ":" + comboBox2.Text + " " + comboBox3.Text;
                resume_time = comboBox4.Text + ":" + comboBox5.Text + " " + comboBox6.Text;
                pause_date = dateTimePicker1.Value.Date;
                resume_date = dateTimePicker2.Value.Date;
                pause_play = true;
                adl_pause = true;
                or_pause=checkBox1.Checked;
                qs_pause = checkBox2.Checked;
                ase_or_pause=checkBox3.Checked;
                ase_qs_pause=checkBox4.Checked;
                Log.Information("\n PAUSE PLAY button clicked");
                or_orders_on_hold = false;
                adl_orders_on_hold = false;
                qs_orders_on_hold = false;
                ase_or_orders_on_hold = false;
                ase_qs_orders_on_hold = false;

            }
        }
    
        private void button3_Click(object sender, EventArgs e)
        {
            if(m_isOrderBookDownloaded)
            {
                delete_time_1 = comboBox13.Text + ":" + comboBox14.Text + " " + comboBox15.Text;
                
                delete_date_1 = dateTimePicker5.Value.Date;
               
                delete_only = true;
                or_delete_1 = checkBox9.Checked;
                qs_delete_1 = checkBox10.Checked;
                ase_or_delete_1 = checkBox11.Checked;
                ase_qs_delete_1 = checkBox12.Checked;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (m_isOrderBookDownloaded)
            {
                order_add_time = comboBox16.Text + ":" + comboBox17.Text + " " + comboBox18.Text;

                order_add_date = dateTimePicker6.Value.Date;

                file_add = true;
                or_add = checkBox13.Checked;
                qs_add = checkBox14.Checked;
                ase_or_add = checkBox15.Checked;
                ase_qs_add = checkBox16.Checked;
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (m_isOrderBookDownloaded)
            {
                delete_time = comboBox7.Text + ":" + comboBox8.Text + " " + comboBox9.Text;
                order_add_time = comboBox10.Text + ":" + comboBox11.Text + " " + comboBox12.Text;
                delete_date = dateTimePicker3.Value.Date;
                order_add_date = dateTimePicker4.Value.Date;
                delete_add = true;
                or_delete = checkBox5.Checked;
                qs_delete = checkBox6.Checked;
                ase_or_delete = checkBox7.Checked;
                ase_qs_delete = checkBox8.Checked;
                or_add = checkBox5.Checked;
                qs_add = checkBox6.Checked;
                ase_or_add = checkBox7.Checked;
                ase_qs_add = checkBox8.Checked;
                Log.Information("\n dlete add button clicked");
            }
        }

          
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Log.Information("In form closing");
           
                Dispose();
           
        }
    }
}
