using Automated_Attendance_System.Entity;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Automated_Attendance_System.Controller
{
    public class AttendanceController
    {
        private static BSS_ATTENDANCE_ZK exceptionEntry = new BSS_ATTENDANCE_ZK();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly object _lock = new object();

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

        public async Task<BSS_ATTENDANCE_ZK> RecordPreviousAttendance(int machineNumber, string enrollmentNumber, int verifyMethod, DateTime punchDate, TimeSpan punchTime, int workCode)
        {
            await _semaphore.WaitAsync();
            int enrollNumber = int.TryParse(enrollmentNumber, out int temp) ? temp : -1;

            try
            {
                await _db.BSS_ATTENDANCE_ZK.InsertFromQueryAsync("BSS_ATTENDANCE_ZK", i => new BSS_ATTENDANCE_ZK
                {
                    Machine_Number = machineNumber,
                    Enrollment_Number = enrollNumber,
                    Verify_Method = verifyMethod,
                    Punch_Date = punchDate,
                    Punch_Time = punchTime,
                    Work_Code = workCode,
                    Sync_Status = false
                });

                return null;
            }
            catch (Exception ex)
            {
                BSS_ATTENDANCE_ZK errorEntry = new BSS_ATTENDANCE_ZK
                {
                    Machine_Number = machineNumber,
                    Enrollment_Number = enrollNumber,
                    Verify_Method = verifyMethod,
                    Punch_Date = punchDate,
                    Punch_Time = punchTime,
                    Work_Code = workCode,
                    Sync_Status = false
                };
                Console.WriteLine($"Realtime Push : {ex.Message}\n");
                bool successFlag = RetryDBEntry(errorEntry).GetAwaiter().GetResult();
                if (!successFlag)
                {
                    return errorEntry;
                }
                return null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        //Inserts Attendance to Database
        public async Task<BSS_ATTENDANCE_ZK> RecordAttendance(int machineNumber, string enrollmentNumber, int verifyMethod, DateTime punchDate, TimeSpan punchTime, int workCode)
        {
            lock (_lock)
            {
                int enrollNumber = int.TryParse(enrollmentNumber, out int temp) ? temp : -1;

                try
                {
                    _db.BSS_ATTENDANCE_ZK.InsertFromQueryAsync("BSS_ATTENDANCE_ZK", i => new BSS_ATTENDANCE_ZK
                    {
                        Machine_Number = machineNumber,
                        Enrollment_Number = enrollNumber,
                        Verify_Method = verifyMethod,
                        Punch_Date = punchDate,
                        Punch_Time = punchTime,
                        Work_Code = workCode,
                        Sync_Status = false
                    });

                    return null;
                }
                catch (Exception ex)
                {
                    BSS_ATTENDANCE_ZK errorEntry = new BSS_ATTENDANCE_ZK
                    {
                        Machine_Number = machineNumber,
                        Enrollment_Number = enrollNumber,
                        Verify_Method = verifyMethod,
                        Punch_Date = punchDate,
                        Punch_Time = punchTime,
                        Work_Code = workCode,
                        Sync_Status = false
                    };
                    Console.WriteLine($"Realtime Push : {ex.Message}\n");
                    bool successFlag = RetryDBEntry(errorEntry).GetAwaiter().GetResult();
                    if (!successFlag)
                    {
                        return errorEntry;
                    }
                    return null;
                }
            }
        }

        public async Task<bool> RetryDBEntry(BSS_ATTENDANCE_ZK errorEntry, int retryCount = 0)
        {
            lock (_lock)
            {
                using (var transaction = _db.Database.BeginTransaction())
                {
                    try
                    {
                        _db.BSS_ATTENDANCE_ZK.InsertFromQueryAsync("BSS_ATTENDANCE_ZK", i => new BSS_ATTENDANCE_ZK
                        {
                            Enrollment_Number = errorEntry.Enrollment_Number,
                            InOut_Mode = errorEntry.InOut_Mode,
                            Machine_Number = errorEntry.Machine_Number,
                            Punch_Date = errorEntry.Punch_Date,
                            Punch_Time = errorEntry.Punch_Time,
                            Sync_Status = errorEntry.Sync_Status,
                            Verify_Method = errorEntry.Verify_Method,
                            Work_Code = errorEntry.Work_Code
                        });
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Transaction for {errorEntry.Enrollment_Number} failed in Retry Entry to DB. Exception: {ex.Message}\n");
                        Log.Fatal($"Transaction for {errorEntry.Enrollment_Number} failed in Retry Entry to DB. Exception: {ex.Message}\n");
                        if (retryCount <= 10)
                        {
                            RetryDBEntry(errorEntry, retryCount += 1).GetAwaiter();
                            return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                // The old code has higher time complexity as well as SaveChangesAsync is slower
                #region Old Code

                //BSS_ATTENDANCE_ZK temp = null;
                //foreach (BSS_ATTENDANCE_ZK att in errorList)
                //{
                //    temp = att;
                //    await _db.BSS_ATTENDANCE_ZK.SingleInsertAsync(att);
                //}
                //int saveFlag = await _db.SaveChangesAsync();
                //if (saveFlag > 0)
                //{
                //    errorList.RemoveAll(r => r == temp);
                //    temp = null;
                //}

                #endregion
            }
        }
    }
}
