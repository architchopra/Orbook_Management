using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using tt_net_sdk;

namespace WindowsFormsApp1
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (Dispatcher disp = Dispatcher.AttachUIDispatcher())
            {
                Application.EnableVisualStyles();

                // Add Your API key below
                string appSecretKey = "e1573049-84d0-b8c0-2916-ab4ec4f17822:864d9f29-3d1f-68b2-7273-3c8cfea6fd14";
                tt_net_sdk.ServiceEnvironment environment = tt_net_sdk.ServiceEnvironment.ProdLive;
                Form1 myApp = new Form1();
                tt_net_sdk.TTAPIOptions apiConfig = new tt_net_sdk.TTAPIOptions(environment, appSecretKey, 5000);
                ApiInitializeHandler handler = new ApiInitializeHandler(myApp.ttNetApiInitHandler);
                TTAPI.CreateTTAPI(disp, apiConfig, handler);
                Application.Run(myApp);
               
            }
        }
    }
}
