using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NovaLauncher
{
    public static class Config
    {
        public static string baseUrl = "http://n.termy.lol/client/setup";
        public static string downloadCompleteUrl = "https://n.termy.lol/app/downloaded";
#if CLIENT
        public static string client = CLIENT;
        public static string RevName = "Novarin "+CLIENT;
#else
        public static string client = "2016";
        public static string RevName = "Novarin 2016";
#endif
#if PROTOCOL
        public static string Protocol = PROTOCOL;
#else
        public static string Protocol = "novarin16";
#endif
        public static string installPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Novarizz\\" + client;
        public static bool doSha256Check = false;
    }
}
