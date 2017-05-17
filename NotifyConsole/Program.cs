using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading;
using Screeps.Notify;

namespace Screeps.NotifyConsole
{
    class Program
    {
        static Grabber grabber = null;
        static SendHttp http = null;
        static int Interval, DeleteLimit = 0;
        static string ScreepsUsername, ScreepsPassword, ApiKey, HttpUrl, HttpUser, HttpPassword;
        static int ThrobberPos = 0;

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
                grabber = new Grabber(ScreepsUsername, ScreepsPassword);
                grabber.Interval = Interval;
                grabber.OnNotification += Grabber_OnNotification;
                http = new SendHttp(HttpUrl);
                http.ApiKey = ApiKey;
                http.HttpUser = HttpUser;
                http.HttpPassword = HttpPassword;
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

        private static void Grabber_OnNotification(int tick, string message)
        {
            Console.WriteLine("\r{0}: {1}", tick, message);
            http.Send(new
            {
                tick,
                message, 
                user = ScreepsUsername
            });
        }

        static void Poll()
        {
            while (true)
            {
                int count = grabber.Poll();
                if (count == 0) Throb();
                Thread.Sleep(Interval);
            }
        }
        
        static void Throb()
        {
            string throbber = "-\\|/";
            ThrobberPos = ++ThrobberPos % throbber.Length;
            Console.Write("\rPolling... " + throbber[ThrobberPos]);
        }
        
    }
}