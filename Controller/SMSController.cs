using Automated_Attendance_System.Entity;
using Automated_Attendance_System.Entity.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Automated_Attendance_System.Controller
{
    public class SMSController
    {
        public Entities _db = new Entities();
        public static SMSDTO SmsDtoList = new SMSDTO();

        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public async Task<List<string>> GetBSSITIds()
        {
            await _semaphore.WaitAsync(1);
            try
            {
                return await _db.HR_EMPLOYEE.Where(w => w.DEPT_ID.Value.Equals(5114)).Select(s => "2200" + s.EMP_ID.Substring(s.EMP_ID.Length - 4)).ToListAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal($"Fetching BSSIT employee ids failed. Excetion Details: {ex.Message} SMSController.cs: 30.");
                return null;
            }
            finally { _semaphore.Release(); }
        }

        public async Task<SMSDTO> GetSMSDTO(string enrollmentNumber)
        {
            await _semaphore.WaitAsync(1);
            try
            {
                if (enrollmentNumber.StartsWith("1100"))
                {
                    string studentId = enrollmentNumber.Substring(enrollmentNumber.Length - 4);
                    SmsDtoList = _db.BSS_STUDENT_VW.Where(w => w.STATUS == true && w.STUDENT_ID.EndsWith(studentId)).AsEnumerable().Select(s => new SMSDTO
                    {
                        Name = s.STUDENT_NAME,
                        EnrollmentNumber = enrollmentNumber,
                        PhoneNumber = s.MOBILE,
                        SMSCount = 0
                    }).FirstOrDefault();
                }
                else
                {
                    string empId = enrollmentNumber.Substring(enrollmentNumber.Length - 4);
                    SmsDtoList = _db.HR_EMPLOYEE_VW.Where(w => w.STATUS == true && w.EMP_ID.EndsWith(empId)).AsEnumerable().Select(s => new SMSDTO
                    {
                        Name = s.NAME,
                        EnrollmentNumber = enrollmentNumber,
                        PhoneNumber = s.MOBILE,
                        SMSCount = 0
                    }).FirstOrDefault();
                }
                return SmsDtoList;
            }
            catch (Exception ex)
            {
                Log.Fatal($"Bulk insertion into database failed. Excetion Details: {ex.Message} SMSController.cs: 66.");
                return null;
            }
            finally { _semaphore.Release(); }
        }

        public async Task<int> SaveSMSDTOHistory(SMSDTO obj)
        {
            await _semaphore.WaitAsync(1);

            BSS_SMS_HISTORY _smsHistory = new BSS_SMS_HISTORY
            {
                SENT_TO = obj.EnrollmentNumber,
                ATTRIBUTE1 = obj.PhoneNumber,
                SEND_COUNT = obj.SMSCount,
                IS_SEND = (obj.SMSCount > 0),
                CREATION_DATE = DateTime.Now.Date
            };
            try
            {

                await _db.BSS_SMS_HISTORY.SingleInsertAsync(_smsHistory);
                return await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal($"Insertion of SMS entity of {obj.EnrollmentNumber} into DB failed. Excetion Details: {ex.Message} SMSController.cs: 93.");
                return 0;
            }
        }

        public async Task<bool> SMSEligible(string enrollmentNumber)
        {
            await _semaphore.WaitAsync(1);
            try
            {
                DateTime date = DateTime.Today;
                var enrollSmsHistory = await _db.BSS_SMS_HISTORY.FirstOrDefaultAsync(a => a.SENT_TO.Equals(enrollmentNumber) && a.CREATION_DATE.Value == date);

                if (enrollSmsHistory != null)
                {
                    if (enrollSmsHistory.SEND_COUNT >= 1)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal($"Eligible checking for {enrollmentNumber} failed. Excetion Details: {ex.Message} SMSController.cs: 124.");
                return false;
            }
            finally { _semaphore.Release(); };
        }
    }
}
