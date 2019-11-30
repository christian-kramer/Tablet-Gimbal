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
using System.Text;
using System.Collections;

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
        /*
        private Kalman pitch_kalman;
        private Kalman roll_kalman;
        private Kalman yaw_kalman;
        */

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

            myTimer = new System.Timers.Timer(1);
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

                /*
                pitch_kalman = new Kalman(10, 100, 0.01);
                roll_kalman = new Kalman(45, 5, 0.01);
                yaw_kalman = new Kalman(10, 5, 3);
                */
            }
            else
            {
                Debug.WriteLine("No Inclinometer Detected!");
                Console.WriteLine("No Inclinometer Detected!");
            }


            Console.WriteLine("\n\n\n");

            void findgimballoop()
            {

                if (FindGimbalOnSystem())
                {
                    SetGimbalState(true);
                }
                else
                {
                    Console.WriteLine("No gimbal on system. Maybe retry?");
                    findgimballoop();
                }
            }

            findgimballoop();
        }

        private void ReadingChanged(object sender, InclinometerReadingChangedEventArgs e)
        {
            if (state)
            {
                InclinometerReading reading = e.Reading;

                pitch_raw = reading.PitchDegrees;
                //pitch_kalman.filter(reading.PitchDegrees);

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
            //Console.WriteLine("Pitch: {0,5:0.00}", pitch_raw);
            //This is where control logic comes in.
            /* This is where control logic comes in.
             * 
             * First, we know what angle we're at.
             * Second, we know what angle we need to get to.
             * Third, we need to figure out "how quickly does it make sense to turn in order to close the gap?"
             * 
             * Well, we know each step is 1.8 degrees.
             * We also know the stepper driver is capable of stepping 1.8, 0.9, 0.45, 0.225, and 0.1125 degrees.
             */

            var MS1 = 6;
            var MS2 = 5;
            var MS3 = 4;
            var Direction = 7;

            byte[] commandgimbal = { 0b00001000 };
            var bitArray = new BitArray(commandgimbal);
            //bitArray.Set(3, state);

            int target_angle = 90;
            double distanceFromTarget = pitch_raw - target_angle;
            if (distanceFromTarget < 0)
            {
                distanceFromTarget *= -1;
                bitArray.Set(Direction, true);
            }
            //Alright, so if our target is 90 and our pitch is 95.125, we'll come out with 5.125 degrees.
            //Likewise, if our target is 45 and our pitch is 31.983, we'll come out with -13.017
            //If our difference is greater than 10 degrees, let's full-step
            //If our difference is greater than 5 degrees, let's half-step
            //If our difference is greater than 2.5 degrees, let's quarter-step
            //If our difference is greater than 1.25 degrees, let's eighth-step
            //If our difference is greater than 0.55 degrees, let's sixteenth-step
            var full_step_degrees = 10;                    // 10
            var half_step_degrees = full_step_degrees / 2; // 5
            var quar_step_degrees = half_step_degrees / 2; // 2.5
            var eigh_step_degrees = quar_step_degrees / 2; // 1.25
            var sixt_step_degrees = eigh_step_degrees / 2; // 0.55

            if (distanceFromTarget > 0 && distanceFromTarget <= sixt_step_degrees)
            {
                //Between 0 and 0.55
                bitArray.Set(MS1, true);
                bitArray.Set(MS2, true);
                bitArray.Set(MS3, true);
            }
            else if (distanceFromTarget >= sixt_step_degrees && distanceFromTarget <= eigh_step_degrees)
            {
                //Between 0.55 and 1.25
                bitArray.Set(MS1, true);
                bitArray.Set(MS2, true);
                bitArray.Set(MS3, true);
            }
            else if (distanceFromTarget >= eigh_step_degrees && distanceFromTarget <= quar_step_degrees)
            {
                //Between 1.25 and 2.5
                bitArray.Set(MS1, true);
                bitArray.Set(MS2, true);
                bitArray.Set(MS3, false);
            }
            else if (distanceFromTarget >= quar_step_degrees && distanceFromTarget <= half_step_degrees)
            {
                //Between 2.5 and 5
                bitArray.Set(MS1, false);
                bitArray.Set(MS2, true);
                bitArray.Set(MS3, false);
            }
            else if (distanceFromTarget >= half_step_degrees && distanceFromTarget <= full_step_degrees)
            {
                //Between 5 and 10
                bitArray.Set(MS1, true);
                bitArray.Set(MS2, false);
                bitArray.Set(MS3, false);
            }
            else if (distanceFromTarget > 10)
            {
                //Greater than 10
                bitArray.Set(MS1, false);
                bitArray.Set(MS2, false);
                bitArray.Set(MS3, false);
            }

            bitArray.CopyTo(commandgimbal, 0);
            _serialPort.Write(commandgimbal, 0, 1);



            /*
            if (pitch_raw > 90)
            {
                //Console.WriteLine("Stepping Clockwise");
                byte[] bytetowrite = { 0b00001000 };
                _serialPort.Write(bytetowrite, 0, 1);
            }
            else if (pitch_raw < 90)
            {
                //Console.WriteLine("Stepping Counter-Clockwise");
                //_serialPort.Write(Encoding.UTF8.GetBytes("a"), 0, 1);
                byte[] bytetowrite = { 0b11111000 };
                _serialPort.Write(bytetowrite, 0, 1);
            }
            */
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

        private bool FindGimbalOnSystem()
        {
            bool result = false;
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
                            if (indata == "abcd")
                            {
                                Console.WriteLine("Magic string was written");
                                source.Cancel();
                            }
                            else
                            {
                                Console.WriteLine("Magic string was not written");
                            }
                        }

                        SerialDataReceivedEventHandler tempevent = new SerialDataReceivedEventHandler(DataReceivedHandler);
                        _serialPort.DataReceived += tempevent;

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
                            _serialPort.DataReceived -= tempevent;
                            _serialPort.Write(Encoding.UTF8.GetBytes("1"), 0, 1);
                            result = true;
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
            return result;

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

            byte[] commandgimbal = { 0b00000000 };
            var bitArray = new BitArray(commandgimbal);
            bitArray.Set(3, state);
            bitArray.CopyTo(commandgimbal, 0);
            _serialPort.Write(commandgimbal, 0, 1);
        }

        private void _notifyIconClick(object sender, EventArgs e)
        {
            //We can't enable the gimbal if we didn't find it on the system.
            //Check to see if a gimbal is present, here.
            SetGimbalState(!state);
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            _notifyIcon.Visible = false; //TODO: Do this on other types of closing
            state = false;
            Debug.WriteLine("Gimbal Off, Exiting...");
            SetGimbalState(false);
            Application.Exit();
        }
    }
}
