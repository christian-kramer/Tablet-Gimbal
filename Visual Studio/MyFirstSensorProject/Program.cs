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
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.System.Threading;
using System.Management;
using System.Text.RegularExpressions;
using System.IO.Ports;
using System.Threading;

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

    class DeviceDescriptor
    {
        public string DeviceName { get; set; }
        public string DeviceID { get; set; }
    }

    internal class MyFirstSensorContext : ApplicationContext
    {
        private NotifyIcon _notifyIcon;
        private bool state = false;
        private Icon enabledIcon;
        private Icon disabledIcon;

        private System.Timers.Timer myTimer;

        private Inclinometer _inclinometer;
        private float pitch_raw;
        private Kalman pitch_kalman;
        private Kalman roll_kalman;
        private Kalman yaw_kalman;

        private SerialPort _serialPort;

        public MyFirstSensorContext()
        {
            var exitMenuItem = new MenuItem("Exit", OnExitClick);

            Stream enabledIconStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("MyFirstSensorProject.enabled_icon.ico");

            Stream disabledIconStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("MyFirstSensorProject.disabled_icon.ico");

            enabledIcon = new Icon(enabledIconStream);
            disabledIcon = new Icon(disabledIconStream);

            Icon defaultIcon = state ? enabledIcon : disabledIcon;

            _notifyIcon = new NotifyIcon
            {
                Icon = defaultIcon,
                ContextMenu = new ContextMenu(new[] { exitMenuItem }),
                Visible = true
            };

            _notifyIcon.Click += new EventHandler(_notifyIconClick);

            myTimer = new System.Timers.Timer(1000);
            myTimer.Elapsed += OnTimedEvent;
            myTimer.AutoReset = true;
            myTimer.Enabled = state;

            _inclinometer = Inclinometer.GetDefault();
            if (_inclinometer != null)
            {
                uint minReportInterval = _inclinometer.MinimumReportInterval;
                uint reportInterval = minReportInterval > 16 ? minReportInterval : 16;
                _inclinometer.ReportInterval = reportInterval;

                // Establish the event handler
                _inclinometer.ReadingChanged += new TypedEventHandler<Inclinometer, InclinometerReadingChangedEventArgs>(ReadingChanged);
                pitch_kalman = new Kalman(10, 100, 0.01);
                roll_kalman = new Kalman(45, 5, 0.01);
                yaw_kalman = new Kalman(10, 5, 3);
            }
            else
            {
                Debug.WriteLine("No Inclinometer Detected!");
                Console.WriteLine("No Inclinometer Detected!");
            }


            Console.WriteLine("\n\n\n");

            FindGimbalOnSystem();
        }

        private void ReadingChanged(object sender, InclinometerReadingChangedEventArgs e)
        {
            if (state)
            {
                InclinometerReading reading = e.Reading;

                pitch_raw = reading.PitchDegrees;
                pitch_kalman.filter(reading.PitchDegrees);

                /*
                Console.WriteLine("\n\n\n\nPitch: {0,5:0.00}", reading.PitchDegrees);
                Console.WriteLine("Roll: {0,5:0.00}", reading.RollDegrees);
                Console.WriteLine("Yaw: {0,5:0.00}", reading.YawDegrees);

                Console.WriteLine("Kalman Pitch: {0,5:0.00}", pitch_kalman.filter(reading.PitchDegrees));
                Console.WriteLine("Kalman Roll: {0,5:0.00}", roll_kalman.filter(reading.RollDegrees));
                Console.WriteLine("Kalman Yaw: {0,5:0.00}\n\n\n\n", yaw_kalman.filter(reading.YawDegrees));
                */
            }
        }


        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //Debug.WriteLine("Timer Function Hit at {0:HH:mm:ss.fff}", e.SignalTime);
            /*
            Kalman kalman = new Kalman(68, 2, 4);
            Debug.WriteLine(kalman.filter(75).ToString());
            Debug.WriteLine(kalman.filter(71).ToString());
            Debug.WriteLine(kalman.filter(70).ToString());
            Debug.WriteLine(kalman.filter(74).ToString());
            */

            Console.WriteLine("Pitch: {0,5:0.00}", pitch_raw);
            if (pitch_kalman != null)
            {
                Console.WriteLine("Kalman Pitch: {0,5:0.00}", pitch_kalman.estimate);
            }
        }

        /*
        private void GetAllBluetoothDevices()
        {
            ManagementObjectCollection ManObjReturn;
            ManagementObjectSearcher ManObjSearch;
            ManObjSearch = new ManagementObjectSearcher("Select * from Win32_PnPEntity");
            ManObjReturn = ManObjSearch.Get();
            int deviceCount = 0;

            foreach (ManagementObject ManObj in ManObjReturn)
            {
                if ((string)ManObj["ClassGUID"] == "{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}")
                {
                    // Do something with ManObj["Name"] such as Add to a <List>
                    Console.WriteLine("\n\n\n");
                    Console.WriteLine("The following is a bluetooth device:");
                    Console.WriteLine(ManObj["Name"] + "\n");
                    Console.WriteLine(ManObj["DeviceId"] + "\n");
                    /*
                    foreach (System.Management.PropertyData prop in ManObj.Properties)
                    {
                        Console.WriteLine("{0}: {1}", prop.Name, prop.Value);
                    }
                    */
                    /*
                    deviceCount++;
                }
            }

            if (deviceCount > 0)
            {
                Console.WriteLine("Found " + deviceCount + " Bluetooth Devices");
            }
            else
            {
                Debug.WriteLine("Found No Bluetooth Devices");
            }
        }
        */

        private void FindGimbalOnSystem()
        {
            List<DeviceDescriptor> BluetoothDevices = new List<DeviceDescriptor>();
            foreach (ManagementObject ManObj in GetAllDevicesByGUID("{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}"))
            {
                Regex idFinder = new Regex(@"(?!_)(?:.(?!_))+$");
                Match idMatch = idFinder.Match((string)ManObj["DeviceID"]);
                BluetoothDevices.Add(new DeviceDescriptor { DeviceName = (string)ManObj["Name"], DeviceID = idMatch.Value });
                Console.WriteLine("Found " + ManObj["Name"]);
            }

            //Now we have a list of bluetooth devices. Let's iterate over every COM port and see if any of the bluetooth devices' IDs match up.

            List<string> rawPortList = new List<string>();
            foreach (ManagementObject ManObj in GetAllDevicesByGUID("{4d36e978-e325-11ce-bfc1-08002be10318}"))
            {
                string COMPortID = (string)ManObj["DeviceID"];

                Console.WriteLine("Found Second Pass: " + ManObj["Name"]);

                foreach (DeviceDescriptor BluetoothDevice in BluetoothDevices)
                {
                    if (COMPortID.Contains(BluetoothDevice.DeviceID))
                    {
                        Console.WriteLine("Identified " + BluetoothDevice.DeviceName + " as a potential gimbal to listen to");
                        Regex portFinder = new Regex(@"(?!\()\w+(?=\))");
                        Match portMatch = portFinder.Match((string)ManObj["Caption"]);
                        rawPortList.Add(portMatch.Value);


                        /*
                        foreach (PropertyData prop in ManObj.Properties)
                        {
                            Console.WriteLine("{0}: {1}", prop.Name, prop.Value);
                        }
                        Console.WriteLine("\n\n\n");
                        */
                    }
                }
            }

            List<string> dedupedPortList = rawPortList.Distinct().ToList();

            foreach (string port in dedupedPortList)
            {
                Console.WriteLine("\n\nCOM Port Listen Attempt: " + port);

                _serialPort = new SerialPort();

                _serialPort.PortName = port;
                _serialPort.BaudRate = 9600;
                _serialPort.Parity = Parity.None;
                _serialPort.DataBits = 8;
                _serialPort.StopBits = StopBits.One;
                _serialPort.Handshake = Handshake.None;
                
                try
                {
                    _serialPort.Open();
                }
                catch (Exception)
                {
                    Console.WriteLine(port + " failed to open.");
                }

                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        Console.WriteLine(port + " is open, let's listen!");

                        string indata = null;

                        CancellationTokenSource source = new CancellationTokenSource();

                        var t = Task.Run(async delegate
                        {
                            await Task.Delay(TimeSpan.FromSeconds(10), source.Token);
                            return indata;
                        });

                        void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
                        {
                            SerialPort sp = (SerialPort)sender;
                            indata = sp.ReadExisting();
                            Console.Write(indata);
                            if (indata == "oof")
                            {
                                Console.WriteLine("Magic string was written");
                                source.Cancel();
                            }
                            else
                            {
                                Console.WriteLine("Magic string was not written");
                            }
                        }

                        _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                        try
                        {
                            t.Wait();
                        }
                        catch (AggregateException ae)
                        {
                            ae.Handle(ex =>
                            {
                                if (ex is OperationCanceledException)
                                {
                                    Console.WriteLine("Safe to assume task was canceled.");
                                }

                                return ex is OperationCanceledException;
                            });
                        }


                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            //Console.WriteLine("Result: {0}", t.Result);
                            Console.WriteLine("Yeah, nah. Let's move on.");
                            _serialPort.Close();
                        }
                        else
                        {
                            Console.WriteLine("Woo hoo! Found it!");
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine(port + " is not open.");
                }
            }
            
            Console.WriteLine("End of Foreach Loop");

            //This is where we decide what to return, based on what has happened. Then, let's enable the gimbal!
            //And, of course, if nothing happened and no gimbal is present, we need to have some sort of "try again" logic.
            //It has to be able to work in the middle of the runtime, so that if the gimbal battery gets switched, it reconnects.
            //Maybe another async task that this whole thing is enclosed in?
        }
        private List<ManagementObject> GetAllDevicesByGUID(string guid)
        {
            ManagementObjectCollection ManObjReturn;
            ManagementObjectSearcher ManObjSearch;
            ManObjSearch = new ManagementObjectSearcher("Select * from Win32_PnPEntity");
            ManObjReturn = ManObjSearch.Get();
            List<ManagementObject> devices = new List<ManagementObject>();

            foreach (ManagementObject ManObj in ManObjReturn)
            {
                if ((string)ManObj["ClassGUID"] == guid)
                {
                    devices.Add(ManObj);
                }
            }

            return devices;
        }

        private void SetGimbalState(bool setstate)
        {
            state = setstate;
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

        private void _notifyIconClick(object sender, EventArgs e)
        {
            SetGimbalState(!state);
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
