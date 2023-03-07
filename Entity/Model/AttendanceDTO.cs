using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automated_Attendance_System.Entity.Model
{
    public class AttendanceDTO
    {
        public int MachineNumber { get; set; } = 0;

        public string EnrollNumber { get; set; } = string.Empty;

        public int VerifyMethod { get; set; } = 0;

        public DateTime PunchDate { get; set; } = DateTime.MinValue;

        public TimeSpan PunchTime { get; set; } = TimeSpan.Zero;

        public int WorkCode { get; set; } = 0;
    }
}
