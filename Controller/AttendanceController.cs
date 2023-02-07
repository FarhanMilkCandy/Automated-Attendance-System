using Automated_Attendance_System.Entity;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Automated_Attendance_System.Controller
{
    public class AttendanceController
    {
        private List<string> exceptionList = new List<string>();

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
                Log.Fatal($"Exception: {ex.Message} occured while fetching device count.");
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
                Log.Fatal($"Exception: {ex.Message} occured while fetching attendance devices.");
                return null;
            }
        }

        //Inserts Attendance to Database
        public async Task<List<string>> RecordAttendance(int machineNumber, string enrollmentNumber, int verifyMethod, DateTime punchDate, TimeSpan punchTime, int workCode)
        {
            try
            {
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

                _db.BSS_ATTENDANCE_ZK.Add(attendance);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine($"\n>>Exception storing {enrollmentNumber} attendance data to DB. Exception: {ex.Message}");
                Log.Fatal($"Exception {ex.Message} occured while inserting {enrollmentNumber} attendance data to DB.");
                exceptionList.Add(enrollmentNumber);
            }

            return exceptionList;
        }
    }
}
