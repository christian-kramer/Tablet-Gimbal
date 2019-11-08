using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Timers;

namespace MyFirstSensorProject
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyFirstSensorContext());
        }
    }

    internal class MyFirstSensorContext : ApplicationContext
    {
        private NotifyIcon _notifyIcon;
        private bool state = true;
        private Icon enabledIcon;
        private Icon disabledIcon;

        private System.Timers.Timer myTimer;
        public MyFirstSensorContext()
        {
            var exitMenuItem = new MenuItem("Exit", OnExitClick);

            Stream enabledIconStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("MyFirstSensorProject.enabled_icon.ico");

            Stream disabledIconStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("MyFirstSensorProject.disabled_icon.ico");

            enabledIcon = new Icon(enabledIconStream);
            disabledIcon = new Icon(disabledIconStream);
            _notifyIcon = new NotifyIcon
            {
                Icon = enabledIcon,
                ContextMenu = new ContextMenu(new[] { exitMenuItem }),
                Visible = true
            };

            _notifyIcon.Click += new EventHandler(_notifyIconClick);

            myTimer = new System.Timers.Timer(500);
            myTimer.Elapsed += OnTimedEvent;
            myTimer.AutoReset = true;
            myTimer.Enabled = true;
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Debug.WriteLine("Timer Function Hit at {0:HH:mm:ss.fff}", e.SignalTime);
        }

        private void _notifyIconClick(object sender, EventArgs e)
        {
            Debug.WriteLine(sender.ToString());
            state = !state;
            myTimer.Enabled = state;
            if (state)
            {
                Debug.WriteLine("Gimbal On");
                _notifyIcon.Icon = enabledIcon;
            }
            else
            {
                Debug.WriteLine("Gimbal Off");
                _notifyIcon.Icon = disabledIcon;
            }
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            _notifyIcon.Visible = false; //TODO: Do this on other types of closing
            state = false;
            Debug.WriteLine("Gimbal Off, Exiting...");
            Application.Exit();
        }
    }
}
