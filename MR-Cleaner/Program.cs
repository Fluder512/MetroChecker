using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Principal;
using System.Diagnostics;

namespace MR_Cleaner
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            if (!IsRunAsAdmin())
            {
                RestartAsAdmin();
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static bool IsRunAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RestartAsAdmin()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Application.ExecutablePath;
            startInfo.Verb = "runas";
            try
            {
                Process.Start(startInfo);
            }
            catch
            {
                MessageBox.Show("Чекеру нужны админ права, без них он бесполезен", "err", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}