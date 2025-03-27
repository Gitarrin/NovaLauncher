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
        public static string client = "2012";
        public static string RevName = "Novarin 2012";
        public static string Protocol = "novarin12";
        public static string installPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Novarizz\\" + client;
        public static bool doSha256Check = false;
    }
}
