using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace NovaLauncher
{
	internal class Logger
	{
		static bool useConsole = false;
		static bool useFile = false;

		static Queue<string> lQ = new Queue<string>();
		static object lQLock = new object(); // Lock for thread safety
		static Timer flushTimer;
		static string lastItem = "";

		static string logDir;
		static string logFile;
		static long startTime;

		public Logger()
		{
			startTime = DateTime.Now.Ticks;

			if (Helpers.App.IsWindows()) logDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
			else logDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			logFile = $"debug_{startTime}.log";
			if (Helpers.App.IsRunningFromInstall())
			{
				if (!Directory.Exists($@"{logDir}\logs")) Directory.CreateDirectory($@"{logDir}\logs");
				logDir = $@"{logDir}\logs";
			} else logFile = $"{Config.AppName.Replace(" ", "")}-{logFile}";

			flushTimer = new Timer(FlushLogs, null, TimeSpan.Zero, TimeSpan.FromSeconds(0.39));

			useConsole = Config.Debug; // Use Console Logging
			useFile = Config.Debug; // Use File Logging
		}

		public void Log(string msg)
		{
			DateTime dt = DateTime.Now;
			string logL = $"[{dt.ToShortDateString()} {dt.ToLongTimeString()}]: {msg}";
			if (useConsole)
			{
				Debug.WriteLine(logL);
			};
			if (useFile)
			{
				lQ.Enqueue(logL);
			}

			lastItem = logL.ToString();
		}

		static void FlushLogs(object state)
		{
			try
			{
				List<string> logsToWrite = new List<string>();
				lock (lQLock)
				{
					while (lQ.Count > 0)
					{
						logsToWrite.Add(lQ.Dequeue());
					}
				}

				if (logsToWrite.Count > 0)
				{
					using (StreamWriter writer = new StreamWriter(Path.Combine(logDir, logFile), true))
					{
						foreach (string lM in logsToWrite)
						{
							writer.WriteLine(lM);
						}
					}
				}
			}
			catch { };
		}

		public string LatestItem() => lastItem;

		public void Shutdown()
		{
			FlushLogs(null);
			flushTimer.Dispose();
		}
	}
}
