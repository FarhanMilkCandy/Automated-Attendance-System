using Automated_Attendance_System.Entity;
using Automated_Attendance_System.Entity.Model;
using Serilog;
using System.Linq;
using System.Threading;

namespace Automated_Attendance_System.Controller
{
    public class ServiceController
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        Entities _db = new Entities();
        public string ServiceShortName { get; set; } = "AAttS";

        public ServiceDTO GetServiceInfo()
        {
            _semaphore.Wait(1);
            try
            {
                return _db.BSS_SERVICE_SETTINGS.Where(w => w.SERVICE_SHORT_NAME.ToLower().Equals(ServiceShortName.ToLower())).Select(s => new ServiceDTO
                {
                    ServiceName = s.SERVICE_NAME,
                    PrimaryMailId = s.EMAIL,
                    AlternativeMailId = s.ALTERNATIVE_EMAIL,
                    Status = s.STATUS
                }).FirstOrDefault();
            }
            catch (System.Exception ex)
            {
                Log.Fatal($"Retreaving Service Information failed. Excetion Details: {ex.Message} ServiceController.cs: 31.");
                return null;
            }
            finally { _semaphore.Release(); }
        }
    }
}
