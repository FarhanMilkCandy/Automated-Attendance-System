using Automated_Attendance_System.Helper;
using Automated_Attendance_System.Helpers;
using System;
using System.Threading;

namespace Automated_Attendance_System
{
    internal class Program
    {
        public static ConnectionHelper _connector = new ConnectionHelper();
        public static EmailHelper _emailHelper = new EmailHelper();
        public static LogHelper _logger = LogHelper.GetInstance();
        //DateTime endTime;
        static void Main(string[] args)
        {
            _logger.Log($"++-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+- {DateTime.Today.ToString("MMMM dd, dddd, yyyy")} -+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-++");
            _logger.Log("Process Started");
            _logger.Log("Connecting to devices");
            while (true)
            {
                DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Today.Day, 8, 00, 0, millisecond:001);
                if (TimeSpan.Compare(DateTime.Now.TimeOfDay, startTime.TimeOfDay) == 1)
                {
                    #region Console
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine($"\n>>Automated Attendance System Started > {DateTime.Now.ToString("dddd dd, MMMM yyyy")}");
                    Console.WriteLine($"\n>> Connecting to Devices.");
                    _logger.Log($"Automated Attendance System Started > {DateTime.Now.ToString("dddd dd, MMMM yyyy")}");
                    _logger.Log($"Connecting to Devices");
                    #endregion

                    _connector.EstablishConnections();
                    Thread timerThread = new Thread(new ThreadStart(Program.ApplicationExitTime));
                    timerThread.Start();
                    break;
                }
                else
                {
                    int secs = Convert.ToInt32(startTime.TimeOfDay.Subtract(DateTime.Now.TimeOfDay).TotalSeconds);
                    #region Console
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine($"\n>>Automated Attendance System will wait {TimeSpan.FromSeconds(secs).TotalMinutes} Mins");
                    _logger.Log($"Realtime attendance recording time should be {startTime.ToShortTimeString()}");
                    _logger.Log($"Automated Attendance System will wait {TimeSpan.FromSeconds(secs).TotalMinutes} Minutes");
                    #endregion
                    Thread.Sleep(secs * 1000);
                }
            }
        }

        public static void ApplicationExitTime()
        {
        //while (true)
        //{
        Exitng:
            DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Today.Day, 19, 59, 59, 999);
            if (TimeSpan.Compare(DateTime.Now.TimeOfDay, endTime.TimeOfDay) == 1)
            {
                _connector.BreakConnection();
                #region Console
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("\n>>Exiting");
                _logger.Log("Application is Exiting");
                _logger.Log($"\r\n++-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+--+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-++");
                #endregion
                _emailHelper.SendLogEmail();
                Thread.Sleep(3000);
                Environment.Exit(0);
            }
            else
            {
                int milisecs = Convert.ToInt32(endTime.TimeOfDay.Subtract(DateTime.Now.TimeOfDay).TotalMilliseconds);
                Console.WriteLine($"\n>> Time is now {DateTime.Now.TimeOfDay} || Application will close after {TimeSpan.FromSeconds(milisecs)} hours.");
                _logger.Log($"Application termination time is set to > {endTime.TimeOfDay}");
                _logger.Log($"Time is now {DateTime.Now.TimeOfDay} || Application will close after {TimeSpan.FromMilliseconds(milisecs)}");
                Thread.Sleep(milisecs);
                goto Exitng;
            }
            //}
        }
    }
}
