using Automated_Attendance_System.Controller;
using Automated_Attendance_System.Entity;
using Automated_Attendance_System.Helper;
using Automated_Attendance_System.ZKTeco;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace Automated_Attendance_System.Helpers
{
    public class ConnectionHelper
    {
        private ZKTeco_Clients clients;
        private EmailHelper emailHelper = new EmailHelper();
        private static readonly AttendanceController _controller = new AttendanceController();
        static List<BSS_ATTENDANCE_DEVICES> _deviceList = _controller.GetAttendanceDevices();
        static List<BSS_ATTENDANCE_DEVICES> unconnDevice = _controller.GetAttendanceDevices();
        public static int connectedDeviceCount = 0;
        private void RaiseDeviceEvent(object sender, string actionType)
        {
            switch (actionType)
            {
                case "Disconnected":
                    {
                        break;
                    }
                default:
                    break;
            }
        }

        private int retryCount = 1;

        public void EstablishConnections()
        {
            unconnDevice = _deviceList.ToList();
        unconnected:

            foreach (BSS_ATTENDANCE_DEVICES device in _deviceList)
            {
                if (ValidateIP(device.DeviceIP) && unconnDevice.Contains(device))
                {
                    Log.Information($"IP validated {device.DeviceIP}\n");
                    if (PingTheDevice(device.DeviceIP))
                    {
                        Log.Information($"Device pinged successfully\n");
                        int port = 0;
                        if (int.TryParse(device.DevicePort, out port))
                        {
                            Log.Information($"Device Port: {port} parsed successfully\n");
                            clients = new ZKTeco_Clients(RaiseDeviceEvent);
                            bool status = clients.Connect_Net(device.DeviceIP, port);
                            if (status)
                            {
                                Log.Information($"Connected Successfully with Device @ {device.DeviceIP} : {device.DevicePort}\n");
                                unconnDevice.Remove(device);
                            }
                            else
                            {
                                Log.Fatal($"Connection unsuccessful with Device @ {device.DeviceIP} : {device.DevicePort}\n");
                                Console.BackgroundColor = ConsoleColor.Red;
                                Console.ForegroundColor = ConsoleColor.Black;
                                Console.WriteLine($"\n>> Connected unsuccessfully to device with IP: {device.DeviceIP}. and Port: {device.DevicePort}.");
                            }
                        }
                        else
                        {
                            Log.Error($"Could not connect to device with IP: {device.DeviceIP}. Invalid Port: {device.DevicePort}\n");
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.WriteLine($"\n>> Could not connect to device with IP: {device.DeviceIP}. Invalid Port: {device.DevicePort}.");
                        }
                    }
                    else
                    {
                        Log.Error($"Could not connect to device with IP: {device.DeviceIP}. The device could not be pinged!\n");
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"\n>> Could not connect to device with IP: {device.DeviceIP}. The device could not be pinged!");
                    }
                }
                else
                {
                    Log.Error($"Could not connect to device with IP: {device.DeviceIP}. Invalid IP Address.\n");
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine($"\n>> Could not connect to device with IP: {device.DeviceIP}. Invalid IP Address.");
                }
            }
            if (unconnDevice.Count > 0)
            {
                Log.Information($"Total unconnected device(s) : {unconnDevice.Count}\n");
                while (true)
                {
                    if (unconnDevice.Count > 0 && retryCount <= 20)
                    {
                        retryCount++;
                        Log.Information($"Retrying to connect to unconnected device(s). Retry Attempt: {retryCount}\n");
                        goto unconnected;
                    }
                    else
                    {
                        if (unconnDevice.Count > 0)
                        {
                            Log.Fatal($"Device no {string.Join(", ", unconnDevice.Select(s => s.DeviceMachineNumber))} cannot be connected. System Retried {retryCount} times\n");
                            Console.BackgroundColor = ConsoleColor.Yellow;
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.WriteLine("\n>> Stopping retry.");
                            Log.Fatal("Stopping retry.\n");
                            bool mailFlag = emailHelper.SendEmail("error", "Device Connection Failed", $"Device no {string.Join(", ", unconnDevice.Select(s => s.DeviceMachineNumber))} cannot be connected. System Retried {retryCount} times");
                            if (mailFlag) { Log.Information($"\"Device Connection Failed\" email sent successfully.\n"); }
                            else
                            {
                                Log.Error($"\"Device Connection Failed\" email sending unsuccessful.\n");
                                Log.Information($"\"Trying Backup email.\n");
                                bool bkpMailFlag = emailHelper.SendEmailBackup("error", "Device Connection Failed", $"Device no {string.Join(", ", unconnDevice.Select(s => s.DeviceMachineNumber))} cannot be connected. System Retried {retryCount} times");
                                if (bkpMailFlag)
                                {
                                    Log.Information($"\"Device Connection Failed\" email sent successfully using backup mail.\n");
                                }
                                else
                                {
                                    Log.Fatal($"\"Device Connection Failed\" email sending unsuccessful even with backup mail.\n");
                                }
                            }
                        }
                        break;
                    }
                }
            }
            clients.connectionFlag = true;

            Thread ConnectionCheckThread = new Thread(new ThreadStart(this.CheckConnectivity));
            ConnectionCheckThread.IsBackground = false;
            ConnectionCheckThread.Start();
        }

        public void TestRTPush()
        {
            clients = new ZKTeco_Clients(RaiseDeviceEvent);
            clients.zkemClient_OnAttTransactionEx("22001123", 1, 0, 4, 2023, 02, 25, 10, 49, 00, 0);
            clients.zkemClient_OnAttTransactionEx("22001119", 1, 0, 4, 2023, 02, 25, 10, 49, 00, 0);
            clients.zkemClient_OnAttTransactionEx("22000030", 1, 0, 4, 2023, 02, 25, 10, 49, 00, 0);
            clients.zkemClient_OnAttTransactionEx("22001111", 1, 0, 4, 2023, 02, 25, 10, 49, 00, 0);
        }

        public void BreakConnection()
        {
            foreach (BSS_ATTENDANCE_DEVICES device in _deviceList)
            {
                int temp = 0;
                if (int.TryParse(device.DeviceMachineNumber, out temp))
                {
                    bool clearFlag = clients.ClearData(temp, 1);
                    if (clearFlag)
                    {
                        #region Console
                        Console.BackgroundColor = ConsoleColor.Green;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"\n>>Device {temp} data is erased.");
                        Log.Information($"Device {temp} data is erased.\n");
                        #endregion
                    }
                    else
                    {
                        #region Console
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"\n>>Device {temp} data could not be erased. Either the device has no data or Disconnected.");
                        Log.Fatal($"Device {temp} data could not be erased. Either the device has no data or Disconnected.\n");
                        #endregion
                    }
                }
            }
            Log.Information($"Disconnecting SDK.\n");
            clients.Disconnect(); //Testing needed
        }

        public bool ValidateIP(string addrString)
        {
            IPAddress address;
            if (IPAddress.TryParse(addrString, out address))
                return true;
            else
                return false;
        }

        public bool PingTheDevice(string ipAdd)
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(ipAdd);

                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted. 
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;
                PingReply reply = pingSender.Send(ipAddress, timeout, buffer, options);

                if (reply.Status == IPStatus.Success)
                    return true;
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void CheckConnectivity()
        {
            Log.Information("Checking device(s) connection status\n");
            unconnDevice.Clear();
            while (true)
            {
                foreach (BSS_ATTENDANCE_DEVICES device in _deviceList)
                {
                    int retryCounter = 0;
                    if (ValidateIP(device.DeviceIP))
                    {
                        Log.Information($"IP validated {device.DeviceIP}\n");
                        if (!PingTheDevice(device.DeviceIP) || unconnDevice.Contains(device))
                        {
                            Log.Information($"Cannot ping device @ {device.DeviceIP}\n");
                            int port = 0;
                            if (int.TryParse(device.DevicePort, out port))
                            {
                                Log.Information($"Device Port : {port} parsed successfuly for connection check\n");
                            //clients = new ZKTeco_Clients(RaiseDeviceEvent); //Raise Event Not Necessary
                            retry:
                                retryCounter++;
                                Log.Information($"Reconnect to device attempt: {retryCount}\n");
                                //bool status = clients.Reconnect_Net(device.DeviceIP, port); //Reconnect is not necessary
                                bool status = PingTheDevice(device.DeviceIP);
                                if (status)
                                {
                                    unconnDevice.RemoveAll(r => r == device);
                                    #region Console
                                    Log.Information($"Connected successfully to device with IP: {device.DeviceIP}. and Port: {device.DevicePort}. Retry count: {retryCounter}\n");
                                    Console.BackgroundColor = ConsoleColor.Blue;
                                    Console.ForegroundColor = ConsoleColor.Black;
                                    Console.WriteLine($"\n>> Connected successfully to device with IP: {device.DeviceIP}. and Port: {device.DevicePort}. Retry count: {retryCounter}.");
                                    bool emailFlag = emailHelper.SendEmail("Success", "Device Connection Established", $"<p style=\"color:green;\">Device no: {device.DeviceMachineNumber} @ IP: {device.DeviceIP} : {device.DevicePort} is connected after {retryCounter} times retrying.</p>");

                                    if (emailFlag)
                                    {
                                        Log.Information($"Notifying email sent successfully\n");
                                        Console.BackgroundColor = ConsoleColor.Black;
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("\n>> Notifying email sent successfully.");
                                    }
                                    else
                                    {
                                        Console.BackgroundColor = ConsoleColor.Red;
                                        Console.ForegroundColor = ConsoleColor.Black;
                                        Console.WriteLine("\n>> Email send unsuccessful. Check network connectivity or other error may have occured. Trying Backup email.");

                                        Log.Error($"Email send unsuccessful. Network connectivity or other error may have occured.\n");
                                        Log.Information($"\"Trying Backup email.\n");
                                        bool bkpMailFlag = emailHelper.SendEmailBackup("Success", "Device Connection Established", $"<p style=\"color:green;\">Device no: {device.DeviceMachineNumber} @ IP: {device.DeviceIP} : {device.DevicePort} is connected after {retryCounter} times retrying.</p>");
                                        if (bkpMailFlag)
                                        {
                                            Log.Information($"\"Device Connection Failed\" email sent successfully using backup mail.\n");
                                        }
                                        else
                                        {
                                            Log.Fatal($"\"Device Connection Failed\" email sending unsuccessful even with backup mail.\n");
                                            Console.BackgroundColor = ConsoleColor.Red;
                                            Console.ForegroundColor = ConsoleColor.Black;
                                            Console.WriteLine("\n>> Email send unsuccessful. Check network connectivity or other error may have occured. Backup email failed!!.");
                                        }

                                    }
                                    #endregion
                                }
                                else
                                {
                                    #region Console
                                    Log.Fatal($"Cannot connect to device with IP: {device.DeviceIP}. and Port: {device.DevicePort}. Retrying to connect. Attempt Remaining: {20 - retryCounter}\n");
                                    Console.BackgroundColor = ConsoleColor.Red;
                                    Console.ForegroundColor = ConsoleColor.Black;
                                    Console.WriteLine($"\n>> Cannot connect to device with IP: {device.DeviceIP}. and Port: {device.DevicePort}. Retrying to connect. Attempt Remaining: {20 - retryCounter}.");
                                    #endregion
                                    if (retryCounter < 20)
                                    {
                                        Log.Information($"Retrying again in 2 mins. Attempt {retryCounter} \n");
                                        Thread.Sleep(120 * 1000); //Sleep for 2 mins
                                        goto retry;
                                    }
                                    else
                                    {
                                        Log.Error($"Device no: {device.DeviceMachineNumber} @ IP: {device.DeviceIP} : {device.DevicePort}. Kindly check if the device is turned on or connected to the internet\n");
                                        unconnDevice.Add(device);
                                        bool emailFlag = emailHelper.SendEmail("Error", "Device Connection Lost", $"Device no: {device.DeviceMachineNumber} @ IP: {device.DeviceIP} : {device.DevicePort}. Kindly check if the device is turned on or connected to the internet.");

                                        if (emailFlag)
                                        {
                                            Log.Information($"Notifying email sent successfully\n");
                                            Console.BackgroundColor = ConsoleColor.Black;
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("\n>> Error Mail Sent Successfully.");
                                        }
                                        else
                                        {
                                            Log.Error($"Email send unsuccessful. Network connectivity or other error may have occured\n");
                                            Console.BackgroundColor = ConsoleColor.Red;
                                            Console.ForegroundColor = ConsoleColor.Black;
                                            Console.WriteLine("\n>> Error mail send unsuccessful. Check network connectivity or other error may have occured. Trying backup email.");

                                            Log.Information($"\"Trying Backup email.\n");
                                            bool bkpMailFlag = emailHelper.SendEmailBackup("Error", "Device Connection Lost", $"Device no: {device.DeviceMachineNumber} @ IP: {device.DeviceIP} : {device.DevicePort}. Kindly check if the device is turned on or connected to the internet.");
                                            if (bkpMailFlag)
                                            {
                                                Log.Information($"\"Device Connection Failed\" email sent successfully using backup mail.\n");
                                            }
                                            else
                                            {
                                                Log.Fatal($"\"Device Connection Failed\" email sending unsuccessful even with backup mail.\n");
                                                Console.BackgroundColor = ConsoleColor.Red;
                                                Console.ForegroundColor = ConsoleColor.Black;
                                                Console.WriteLine("\n>> Error mail send unsuccessful. Check network connectivity or other error may have occured. Backup email failed!!.");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                //30 mins check
                Thread.Sleep(1800 * 1000);
            }
        }
    }
}
