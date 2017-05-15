using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading;
using ScreepsApi;

namespace Screeps_Notify
{
    class Program
    {
        static Client client = null;
        static int Interval, DeleteLimit = 0;
        static string ScreepsUsername, ScreepsPassword, ApiKey, HttpUrl, HttpUser, HttpPassword;
        static int ThrobberPos = 0;
        static Http http = null;

        static void Main(string[] args)
        {
            GetSettings();
            if (!Connect()) 
                Console.ReadLine();
            else Poll();
        }

        static void GetSettings()
        {
            Interval = Convert.ToInt32(ConfigurationManager.AppSettings["interval"]);
            ScreepsUsername = ConfigurationManager.AppSettings["screeps_username"];
            ScreepsPassword = ConfigurationManager.AppSettings["screeps_password"];
            ApiKey = ConfigurationManager.AppSettings["api-key"];
            HttpUrl = ConfigurationManager.AppSettings["http"];
            HttpUser = ConfigurationManager.AppSettings["http_user"];
            HttpPassword = ConfigurationManager.AppSettings["http_password"];

            if (string.IsNullOrWhiteSpace(ScreepsUsername))
            {
                Console.Write("Please enter your screeps account email: ");
                ScreepsUsername = Console.ReadLine();
            }
            if (string.IsNullOrWhiteSpace(ScreepsPassword))
            {
                Console.Write("Please enter your screeps account password: ");
                ScreepsPassword = Console.ReadLine();
            }
        }

        static bool Connect()
        {
            Console.WriteLine();
            Console.Write("Connecting... ");

            bool connected;
            try
            {
                client = new Client(ScreepsUsername, ScreepsPassword);
                connected = true;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Ok");
                Console.ResetColor();
                Console.WriteLine();
            }
            catch (Exception e)
            {
                connected = false;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAILED!");
                Console.ResetColor();
            }

            return connected;
        }

        static void Poll()
        {
            while (true)
            {
                if( ProcessNotifications() > 0 )
                    ClearNotifications();
                Thread.Sleep(Interval);
            }
        }

        static int ProcessNotifications()
        {
            int count = 0;
            dynamic memory = client.UserMemoryGet("__notify");
            foreach (dynamic notification in memory.data)
            {
                int tick = notification.tick;
                ProcessNotification(tick, notification.message);
                if (DeleteLimit < tick) DeleteLimit = tick;
                count++;
            }
            if (count == 0) Throb();
            return count;
        }

        static void Throb()
        {
            string throbber = "-\\|/";
            ThrobberPos = ++ThrobberPos % throbber.Length;
            Console.Write("\rPolling... " + throbber[ThrobberPos]);
        }

        static void ProcessNotification(int tick, string message)
        {
            Console.WriteLine("\r{0}: {1}", tick, message);
            SendHttp(tick, message);
        }

        static void SendHttp(int tick, string message)
        {
            if (string.IsNullOrEmpty(HttpUrl))
                return;

            if (http == null)
            {
                http = new Http();
                http.UserAgent = "screeps_notify";
                if (!string.IsNullOrEmpty(ApiKey))
                    http.SetHeader("x-api-key", ApiKey);
                if (!string.IsNullOrEmpty(HttpUser))
                    http.SetCredential(HttpUser, HttpPassword);
            }

            Console.Write("Sending http... ");
            dynamic response = http.Post(HttpUrl, new
            {
                tick,
                message,
                user = ScreepsUsername
            });
            Console.WriteLine(response);
        }

        static void ClearNotifications()
        {
            string command = string.Format("(()=>{{Memory.__notify=Memory.__notify.filter((notification)=>notification.tick>{0});return \"Notifications collected\";}})();", DeleteLimit);
            client.UserConsole(command);
        }
    }
}