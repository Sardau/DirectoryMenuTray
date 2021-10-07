using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DirectoryMenuTray
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			//Application.Run(new SettingsDialog());
			var path = args!=null&&args.Length>0?args[0]:Properties.Settings.Default.Path;
			if (Directory.Exists(path))
			{
				AddToStartup(path,true);
				Application.Run(new DirectoryMenuApplicationContext(path));
			}
			else
				MessageBox.Show($"Directory {path} does not exist!!!", "Directory Menu tray", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private static string AppName = "Directory Menu Tray";
		private static void AddToStartup(string path,bool add)
		{
#if !DEBUG
			RegistryKey rk = Registry.CurrentUser.OpenSubKey
					("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

			if (add)
				rk.SetValue(AppName, $"{Application.ExecutablePath} \"{path}\"");
			else
				rk.DeleteValue(AppName, false);
#endif
		}


	}
}
