using Automated_Attendance_System.Entity;
using Automated_Attendance_System.Entity.Model;
using System.Linq;

namespace Automated_Attendance_System.Controller
{
    public class ServiceController
    {
        Entities _db = new Entities();
        public string ServiceShortName { get; set; } = "AAttS";

        public ServiceDTO GetServiceInfo()
        {
            return _db.BSS_SERVICE_SETTINGS.Where(w => w.SERVICE_SHORT_NAME.ToLower().Equals(ServiceShortName.ToLower())).Select(s => new ServiceDTO
            {
                ServiceName = s.SERVICE_NAME,
                PrimaryMailId = s.EMAIL,
                AlternativeMailId = s.ALTERNATIVE_EMAIL,
                Status = s.STATUS
            }).FirstOrDefault();
        }

    }
}
