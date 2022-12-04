using Automated_Attendance_System.Controller;
using Automated_Attendance_System.Entity;
using Automated_Attendance_System.Helper;
using Automated_Attendance_System.ZKTeco;
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
        public LogHelper _logger = LogHelper.GetInstance();
        List<BSS_ATTENDANCE_DEVICES> _deviceList = _controller.GetAttendanceDevices();
        List<BSS_ATTENDANCE_DEVICES> unconnDevice = _controller.GetAttendanceDevices();
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
                    _logger.Log($"IP validated {device.DeviceIP}");
                    if (PingTheDevice(device.DeviceIP))
                    {
                        _logger.Log($"Device pinged successfully");
                        int port = 0;
                        if (int.TryParse(device.DevicePort, out port))
                        {
                            _logger.Log($"Device Port: {port} parsed successfully");
                            clients = new ZKTeco_Clients(RaiseDeviceEvent);
                            bool status = clients.Connect_Net(device.DeviceIP, port);
                            if (status)
                            {
                                _logger.Log($"Connected Successfully with Device @ {device.DeviceIP} : {device.DevicePort}");
                                //Console.BackgroundColor = ConsoleColor.Blue;
                                //Console.ForegroundColor = ConsoleColor.Black;
                                //Console.WriteLine($"\n>> Connected successfully to device with IP: {device.DeviceIP}. and Port: {device.DevicePort}.");
                                unconnDevice.Remove(device);
                            }
                            else
                            {
                                _logger.Log($"Connection unsuccessful with Device @ {device.DeviceIP} : {device.DevicePort}");
                                Console.BackgroundColor = ConsoleColor.Red;
                                Console.ForegroundColor = ConsoleColor.Black;
                                Console.WriteLine($"\n>> Connected unsuccessfully to device with IP: {device.DeviceIP}. and Port: {device.DevicePort}.");
                            }
                        }
                        else
                        {
                            _logger.Log($"Could not connect to device with IP: {device.DeviceIP}. Invalid Port: {device.DevicePort}.");
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.WriteLine($"\n>> Could not connect to device with IP: {device.DeviceIP}. Invalid Port: {device.DevicePort}.");
                        }
                    }
                    else
                    {
                        _logger.Log($"Could not connect to device with IP: {device.DeviceIP}. The device could not be pinged!");
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"\n>> Could not connect to device with IP: {device.DeviceIP}. The device could not be pinged!");
                    }
                }
                else
                {
                    _logger.Log($"Could not connect to device with IP: {device.DeviceIP}. Invalid IP Address.");
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine($"\n>> Could not connect to device with IP: {device.DeviceIP}. Invalid IP Address.");
                }
            }
            if (unconnDevice.Count > 0)
            {
                _logger.Log($"Total unconnected device(s) : {unconnDevice.Count}");
                while (true)
                {
                    if (unconnDevice.Count > 0 && retryCount <= 20)
                    {
                        retryCount++;
                        _logger.Log($"Retrying to connect to unconnected device(s). Retry Attempt: {retryCount}");
                        goto unconnected;
                    }
                    else
                    {
                        if (unconnDevice.Count > 0)
                        {
                            _logger.Log($"Device no {string.Join(", ", unconnDevice.Select(s => s.DeviceMachineNumber))} cannot be connected. System Retried {retryCount} times");
                            Console.BackgroundColor = ConsoleColor.Yellow;
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.WriteLine("\n>> Stopping retry.");
                            _logger.Log("Stopping retry.");
                            bool mailFlag = emailHelper.SendEmail("error", "Device Connection Failed", $"Device no {string.Join(", ", unconnDevice.Select(s => s.DeviceMachineNumber))} cannot be connected. System Retried {retryCount} times");
                            if (mailFlag) { _logger.Log($"\"Device Connection Failed\" email sent successfully."); }
                            else
                            {
                                _logger.Log($"\"Device Connection Failed\" email sending unsuccessful.");
                            }
                        }
                        break;
                    }
                }
            }
            clients.connectionFlag = true;

            Thread ConnectionCheckThread = new Thread(new ThreadStart(this.CheckConnectivity));
            ConnectionCheckThread.Start();
        }

        public void BreakConnection()
        {
            foreach (BSS_ATTENDANCE_DEVICES device in _deviceList)
            {
                //clients.Connect_Net(device.DeviceIP, int.Parse(device.DevicePort));
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
                        _logger.Log($"Device {temp} data is erased.");
                        #endregion
                    }
                    else
                    {
                        #region Console
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"\n>>Device {temp} data could not be erased. Either the device has no data or Disconnected.");
                        _logger.Log($"Device {temp} data could not be erased. Either the device has no data or Disconnected.");
                        #endregion
                    }
                }
            }
            _logger.Log($"Disconnecting SDK");
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
            _logger.Log("Checking device(s) connection status");
            unconnDevice.Clear();
            while (true)
            {
                foreach (BSS_ATTENDANCE_DEVICES device in _deviceList)
                {
                    int retryCounter = 0;
                    if (ValidateIP(device.DeviceIP))
                    {
                        _logger.Log($"IP validated {device.DeviceIP}");
                        if (!PingTheDevice(device.DeviceIP) || unconnDevice.Contains(device))
                        {
                            _logger.Log($"Cannot ping device @ {device.DeviceIP}");
                            int port = 0;
                            if (int.TryParse(device.DevicePort, out port))
                            {
                                _logger.Log($"Device Port : {port} parsed successfuly for connection check");
                            //clients = new ZKTeco_Clients(RaiseDeviceEvent); //Raise Event Not Necessary
                            retry:
                                retryCounter++;
                                _logger.Log($"Reconnect to device attempt: {retryCount}");
                                //bool status = clients.Reconnect_Net(device.DeviceIP, port); //Reconnect is not necessary
                                bool status = PingTheDevice(device.DeviceIP);
                                if (status)
                                {
                                    unconnDevice.RemoveAll(r => r == device);
                                    #region Console
                                    _logger.Log($"Connected successfully to device with IP: {device.DeviceIP}. and Port: {device.DevicePort}. Retry count: {retryCounter}");
                                    Console.BackgroundColor = ConsoleColor.Blue;
                                    Console.ForegroundColor = ConsoleColor.Black;
                                    Console.WriteLine($"\n>> Connected successfully to device with IP: {device.DeviceIP}. and Port: {device.DevicePort}. Retry count: {retryCounter}.");
                                    bool emailFlag = emailHelper.SendEmail("Success", "Device Connection Established", $"<p style=\"color:green;\">Device no: {device.DeviceMachineNumber} @ IP: {device.DeviceIP} : {device.DevicePort} is connected after {retryCounter} times retrying.</p>");

                                    if (emailFlag)
                                    {
                                        _logger.Log($"Notifying email sent successfully");
                                        Console.BackgroundColor = ConsoleColor.Black;
                                        Console.ForegroundColor = ConsoleColor.Green;
                                        Console.WriteLine("\n>> Notifying email sent successfully.");
                                    }
                                    else
                                    {
                                        _logger.Log($"Email send unsuccessful. Network connectivity or other error may have occured");
                                        Console.BackgroundColor = ConsoleColor.Green;
                                        Console.ForegroundColor = ConsoleColor.Black;
                                        Console.WriteLine("\n>> Email send unsuccessful. Check network connectivity or other error may have occured.");
                                    }
                                    #endregion
                                }
                                else
                                {
                                    #region Console
                                    _logger.Log($"Cannot connect to device with IP: {device.DeviceIP}. and Port: {device.DevicePort}. Retrying to connect. Attempt Remaining: {20 - retryCounter}");
                                    Console.BackgroundColor = ConsoleColor.Red;
                                    Console.ForegroundColor = ConsoleColor.Black;
                                    Console.WriteLine($"\n>> Cannot connect to device with IP: {device.DeviceIP}. and Port: {device.DevicePort}. Retrying to connect. Attempt Remaining: {20 - retryCounter}.");
                                    #endregion
                                    if (retryCounter < 20)
                                    {
                                        _logger.Log($"Retrying again in 2 mins. Attempt {retryCounter}");
                                        Thread.Sleep(120 * 1000); //Sleep for 2 mins
                                        goto retry;
                                    }
                                    else
                                    {
                                        _logger.Log($"Device no: {device.DeviceMachineNumber} @ IP: {device.DeviceIP} : {device.DevicePort}. Kindly check if the device is turned on or connected to the internet");
                                        unconnDevice.Add(device);
                                        bool emailFlag = emailHelper.SendEmail("Error", "Device Connection Lost", $"Device no: {device.DeviceMachineNumber} @ IP: {device.DeviceIP} : {device.DevicePort}. Kindly check if the device is turned on or connected to the internet.");

                                        if (emailFlag)
                                        {
                                            _logger.Log($"Notifying email sent successfully");
                                            Console.BackgroundColor = ConsoleColor.Black;
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.WriteLine("\n>> Error Mail Sent Successfully.");
                                        }
                                        else
                                        {
                                            _logger.Log($"Email send unsuccessful. Network connectivity or other error may have occured");
                                            Console.BackgroundColor = ConsoleColor.Red;
                                            Console.ForegroundColor = ConsoleColor.Black;
                                            Console.WriteLine("\n>> Error mail send unsuccessful. Check network connectivity or other error may have occured.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                _logger.Log($"Reconnect thread will go to sleep for {TimeSpan.FromMilliseconds(1800 * 1000).TotalMinutes} Minutes");
                //30 mins check
                Thread.Sleep(1800 * 1000);
            }
        }
    }
}
