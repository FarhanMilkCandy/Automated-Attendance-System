using Automated_Attendance_System.Entity;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace Automated_Attendance_System.Controller
{
    public class AttendanceController
    {
        private static List<BSS_ATTENDANCE_ZK> exceptionList = new List<BSS_ATTENDANCE_ZK>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public Entities _db = new Entities();

        int temp = 0;
        BSS_ATTENDANCE_ZK attendance;

        public int GetAttendanceDeviceCount()
        {
            try
            {
                return _db.BSS_ATTENDANCE_DEVICES.Where(w => w.Status == true).Count();
            }
            catch (Exception ex)
            {
                Log.Fatal($"Exception: {ex.Message} occured while fetching device count.\n");
                return -1;
            }
        }

        public List<BSS_ATTENDANCE_DEVICES> GetAttendanceDevices()
        {
            try
            {
                return _db.BSS_ATTENDANCE_DEVICES.Where(w => w.Status == true).ToList();
            }
            catch (Exception ex)
            {
                Log.Fatal($"Exception: {ex.Message} occured while fetching attendance devices.\n");
                return null;
            }
        }

        public async Task<List<BSS_ATTENDANCE_ZK>> RecordPreviousAttendance(int machineNumber, string enrollmentNumber, int verifyMethod, DateTime punchDate, TimeSpan punchTime, int workCode)
        {
            await _semaphore.WaitAsync();

            temp = 0;
            attendance = new BSS_ATTENDANCE_ZK
            {
                Machine_Number = machineNumber,
                Enrollment_Number = int.TryParse(enrollmentNumber, out temp) ? temp : -1,
                Verify_Method = verifyMethod,
                Punch_Date = punchDate,
                Punch_Time = punchTime,
                Work_Code = workCode,
                Sync_Status = false
            };

            _db.BSS_ATTENDANCE_ZK.AddOrUpdate(attendance);
            await _db.SaveChangesAsync();

            await PreviousExceptionRecorder(attendance);


            _semaphore.Release();


            return exceptionList;
        }

        private async Task PreviousExceptionRecorder(BSS_ATTENDANCE_ZK attendance)
        {
            await Task.Run(() =>
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine($"\n>>Error storing {attendance.Enrollment_Number} attendance data to DB.\n");
                Log.Fatal($"Error occured while inserting {attendance.Enrollment_Number} attendance data to DB.\n");
                exceptionList.Add(attendance);
            });
        }

        //Inserts Attendance to Database
        public async Task<List<BSS_ATTENDANCE_ZK>> RecordAttendance(int machineNumber, string enrollmentNumber, int verifyMethod, DateTime punchDate, TimeSpan punchTime, int workCode)
        {
            await _semaphore.WaitAsync();

            temp = 0;
            attendance = new BSS_ATTENDANCE_ZK
            {
                Machine_Number = machineNumber,
                Enrollment_Number = int.TryParse(enrollmentNumber, out temp) ? temp : -1,
                Verify_Method = verifyMethod,
                Punch_Date = punchDate,
                Punch_Time = punchTime,
                Work_Code = workCode,
                Sync_Status = false
            };

            //_db.BSS_ATTENDANCE_ZK.AddOrUpdate(attendance);
            int flag = /*await _db.SaveChangesAsync();*/ 1;
            if (flag < 0)
            {
                await ExceptionRecorder(attendance);
            }

            _semaphore.Release();

            return exceptionList;
        }

        private async Task ExceptionRecorder(BSS_ATTENDANCE_ZK attendance)
        {
            await Task.Run(() =>
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine($"\n>>Error storing {attendance.Enrollment_Number} attendance data to DB.\n");
                Log.Fatal($"Error occured while inserting {attendance.Enrollment_Number} attendance data to DB at {attendance.Punch_Date.Date} {attendance.Punch_Time}.\n");
                exceptionList.Add(attendance);
            });
        }

        public async Task<List<BSS_ATTENDANCE_ZK>> RetryDBEntry(List<BSS_ATTENDANCE_ZK> errorList)
        {
            await _semaphore.WaitAsync();
            BSS_ATTENDANCE_ZK temp = null;
            foreach (BSS_ATTENDANCE_ZK att in errorList)
            {
                temp = att;
                await _db.BSS_ATTENDANCE_ZK.SingleInsertAsync(att);
            }

            if (saveFlag > 0)
            {
                errorList.RemoveAll(r => r == temp);
                temp = null;
            }

            _semaphore.Release();
            return errorList;
        }

    }
}
