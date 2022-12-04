using Automated_Attendance_System.Entity;
using System.Collections.Generic;
using System.Linq;

namespace Automated_Attendance_System.Controller
{
    public class UpdateController
    {
        private List<string> exceptionList = new List<string>();

        public Entities _db = new Entities();

        public List<HR_EMPLOYEE> GetHRUpdates()
        {
            return _db.HR_EMPLOYEE.Where(w => w.ATTRIBUTE14.ToLower().Equals("u") && w.STATUS == true).ToList();
        }

        public List<BSS_STUDENT> GetStudentUpdates()
        {
            return _db.BSS_STUDENT.Where(w => w.ATTRIBUTE14.ToLower().Equals("u") && w.STATUS == true).ToList();
        }

        public int SetHRSyncStatus(HR_EMPLOYEE emp)
        {
            HR_EMPLOYEE result = _db.HR_EMPLOYEE.FirstOrDefault(f => f.EMP_HEADER_ID == emp.EMP_HEADER_ID);
            if (result != null)
            {
                result.ATTRIBUTE14 = "s";
            }
            return _db.SaveChanges();
        }

        public int SetStudentSyncStatus(BSS_STUDENT std)
        {
            BSS_STUDENT result = _db.BSS_STUDENT.FirstOrDefault(f => f.STUDENT_HEADER_ID == std.STUDENT_HEADER_ID);
            if (result != null)
            {
                result.ATTRIBUTE14 = "s";
            }
            return _db.SaveChanges();
        }
    }
}
