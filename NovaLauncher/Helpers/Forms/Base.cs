using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace NovaLauncher.Helpers.Forms
{
	internal class Base
	{
		internal Main instance;
		public Installer installer;
		public InstallCompleted installCompleted;
		public Uninstaller uninstaller;

		public Base(Form f)
		{
			instance = (f as Main);
			installer = new Installer();
			installCompleted = new InstallCompleted();
			uninstaller = new Uninstaller();
		}

		internal void Close()
		{
			if (instance != null) DoThingsWInvoke(() => instance.Close());
			else Application.Exit();
		}

		internal void DoThingsWInvoke(Action f, Control c = null)
		{
			if (c != null && c.InvokeRequired) c.Invoke(f);
			else if (instance != null && instance.InvokeRequired) instance.Invoke(f);
			else f();
		}

		internal void CreateBackgroundTask(DoWorkEventHandler doWorkHandler, RunWorkerCompletedEventHandler finishWorkHandler)
		{
			BackgroundWorker bgWorker = new BackgroundWorker();
			bgWorker.DoWork += (s, e) => doWorkHandler(s, e);
			bgWorker.RunWorkerCompleted += (s, e) => DoThingsWInvoke(() => finishWorkHandler(s, e));
			bgWorker.RunWorkerAsync();
		}

		internal void UpdateTextWithLog(Control c, string text)
		{
			DoThingsWInvoke(() =>
			{
				c.Text = text;
				Program.logger.Log($"{c.Name}: {text}");
			}, c);
		}
	}
}
