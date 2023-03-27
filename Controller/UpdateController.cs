using Automated_Attendance_System.Entity;
using Serilog;
using System.Collections.Generic;
using System.IdentityModel.Protocols.WSTrust;
using System.Linq;
using System.Threading;

namespace Automated_Attendance_System.Controller
{
    public class UpdateController
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public Entities _db = new Entities();

        public List<HR_EMPLOYEE> GetHRUpdates()
        {
            _semaphore.Wait(1);
            try
            {
                return _db.HR_EMPLOYEE.Where(w => w.ATTRIBUTE14.ToLower().Equals("u") && w.STATUS == true).ToList();
            }
            catch (System.Exception ex)
            {
                Log.Fatal($"Fetching data for HR EMPLOYEE UPDATION failed. Excetion Details: {ex.Message} UpdateController.cs: 26.");
                return null;
            }
            finally { _semaphore.Release(); }
        }

        public List<BSS_STUDENT> GetStudentUpdates()
        {
            _semaphore.Wait(1);
            try
            {
                return _db.BSS_STUDENT.Where(w => w.ATTRIBUTE14.ToLower().Equals("u") && w.STATUS == true).ToList();
            }
            catch (System.Exception ex)
            {
                Log.Fatal($"Fetching data for STUDENT UPDATION failed. Excetion Details: {ex.Message} UpdateController.cs: 41.");
                return null;
            }
            finally { _semaphore.Release(); }
        }

        public int SetHRSyncStatus(HR_EMPLOYEE emp)
        {
            _semaphore.Wait(1);
            try
            {
                HR_EMPLOYEE result = _db.HR_EMPLOYEE.FirstOrDefault(f => f.EMP_HEADER_ID == emp.EMP_HEADER_ID);
                if (result != null)
                {
                    result.ATTRIBUTE14 = "s";
                }
                return _db.SaveChanges();
            }
            catch (System.Exception ex)
            {
                Log.Fatal($"Updating HR Employee Status failed. Excetion Details: {ex.Message} UpdateController.cs: 61.");
                return -1;
            }
            finally { _semaphore.Release(); }
        }

        public int SetStudentSyncStatus(BSS_STUDENT std)
        {
            _semaphore.Wait(1);
            try
            {
                BSS_STUDENT result = _db.BSS_STUDENT.FirstOrDefault(f => f.STUDENT_HEADER_ID == std.STUDENT_HEADER_ID);
                if (result != null)
                {
                    result.ATTRIBUTE14 = "s";
                }
                return _db.SaveChanges();
            }
            catch (System.Exception ex)
            {
                Log.Fatal($"Updating STUDENT Status failed. Excetion Details: {ex.Message} UpdateController.cs: 81.");
                return -1;
            }
            finally { _semaphore.Release(); }
        }
    }
}
