using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automated_Attendance_System.Entity.Model
{
    public class SMSDTO
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string EnrollmentNumber { get; set; }
        public string SMSSubject { get; set; } = "Attendance";
        public string SMSContent { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorText { get; set; }
        public int SMSCount { get; set; }
        public DateTime? SendDate { get; set;}
    }
}
