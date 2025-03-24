using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;

namespace NovaLauncher
{
    public class LatestClientInfo
    {
        public string Version { get; set; }
        public string Url { get; set; }
        public string Sha256 { get; set; }
        public long Size { get; set; }
    }
    public class LaunchData
    {
        public string JoinScript { get; set; }
        public string Ticket { get; set; }
        public string LaunchType { get; set; }
        public string LaunchToken { get; set; }
        public string PlaceId { get; set; }
        public string JobId { get; set; }
    }
    public class VersionJSON
    {
        public string Version { get; set; }
    }
    public sealed class CLIArgs
    {
        [Option('t', "token", Required = false, HelpText = "Used in launching roblox. Authentication.")]
        public string Token { get; set; }
        [Option('u', "update", Required = false, HelpText = "Forces an update.")]
        public bool Update { get; set; }
        [Option("uninstall", Required = false, HelpText = "Uninstalls the program :(")]
        public bool Uninstall { get; set; }
        [Option("tempzippathbase64", Required = false)]
        public string TempZipPath { get; set; } // Used to get the path of the zip file
        [Option("updateinfojsonbase64", Required = false)]
        public string UpdateInfo { get; set; } // Used to get the update info

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}