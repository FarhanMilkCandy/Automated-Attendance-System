using Automated_Attendance_System.Helper;
using Automated_Attendance_System.Helpers;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Threading;

namespace Automated_Attendance_System
{
    internal class Program
    {
        public static ConnectionHelper _connector = new ConnectionHelper();
        public static EmailHelper _emailHelper = new EmailHelper();
        //DateTime endTime;
        static void Main(string[] args)
        {
            #region Logger Settings

            //Created the "appsettings.json" file only for the logger. Sketchy solution, but hey, it works...
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            #endregion


            try
            {
                Log.Information($"++-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-++");
                Log.Information("Process Started\n");
                Log.Information("Connecting to devices\n");

                while (true)
                {
                    DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Today.Day, 7, 39, 59, millisecond: 999);
                    if (TimeSpan.Compare(DateTime.Now.TimeOfDay, startTime.TimeOfDay) == 1)
                    {
                        #region Console and log
                        Console.BackgroundColor = ConsoleColor.Green;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"\n>>Automated Attendance System Started > {DateTime.Now.ToString("dddd dd, MMMM yyyy")}");
                        Console.WriteLine($"\n>> Connecting to Devices.");
                        Log.Information($"Automated Attendance System Started > {DateTime.Now.ToString("dddd dd, MMMM yyyy")}");
                        Log.Information($"Connecting to Devices");
                        #endregion

                        _connector.EstablishConnections();
                        Thread timerThread = new Thread(new ThreadStart(Program.ApplicationExitTime));
                        timerThread.IsBackground = false;
                        timerThread.Start();
                        break;
                    }
                    else
                    {
                        int secs = Convert.ToInt32(startTime.TimeOfDay.Subtract(DateTime.Now.TimeOfDay).TotalSeconds);
                        #region Console and log
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"\n>>Automated Attendance System will wait {TimeSpan.FromSeconds(secs).TotalMinutes} Mins");
                        Log.Information($"Realtime attendance recording time should be {startTime.ToShortTimeString()}\n");
                        Log.Information($"Automated Attendance System will wait {TimeSpan.FromSeconds(secs).TotalMinutes} Minutes\n");
                        #endregion
                        Thread.Sleep(secs * 1000);
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Fatal($"Exception in the application: {ex.Message}\n");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
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
                #region Console and log
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine("\n>>Exiting\n");
                Log.Information("Application is Exiting\n");
                Log.Information($"\r\n++-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+--+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-++\n");
                #endregion
                _emailHelper.SendLogEmail();
                Thread.Sleep(3000);
                Log.CloseAndFlush();
                Environment.Exit(0);
            }
            else
            {
                int milisecs = Convert.ToInt32(endTime.TimeOfDay.Subtract(DateTime.Now.TimeOfDay).TotalMilliseconds);
                TimeSpan calculatedExitTime = TimeSpan.FromMilliseconds(milisecs);
                Console.WriteLine($"\n>> Time is now {DateTime.Now.TimeOfDay} || Application will close after {calculatedExitTime.Hours} hours and {calculatedExitTime.Minutes} minutes.");
                Log.Information($"Application termination time is set to > {endTime.TimeOfDay}");
                Log.Information($"Time is now {DateTime.Now.TimeOfDay} || Application will close after {calculatedExitTime.Hours} hours and {calculatedExitTime.Minutes} minutes.");
                Thread.Sleep(milisecs);
                goto Exitng;
            }
            //}
        }
    }
}
