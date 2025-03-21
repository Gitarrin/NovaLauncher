using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NovaLauncher
{
    public static class Config
    {
        public static string baseUrl = "http://novarin.cc/client/setup";
        public static string downloadCompleteUrl = "https://novarin.cc/app/downloaded";
        public static string client = "2016";
        public static string installPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Novarizz\\" + client;
        public static bool doSha256Check = false;
        public static string RevName = "Novarin 2016";
        public static string Protocol = "novarin16";
    }
}
