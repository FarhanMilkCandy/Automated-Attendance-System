using Automated_Attendance_System.Controller;
using Automated_Attendance_System.Entity.Model;

namespace Automated_Attendance_System.Helper
{
    public class ServiceHelper
    {
        private static ServiceDTO _serviceDTO;
        private static readonly ServiceController _serviceController = new ServiceController();

        public static ServiceDTO GetDTOInstance()
        {
            if (_serviceDTO == null)
            {
                _serviceDTO = _serviceController.GetServiceInfo();
            }
            return _serviceDTO;
        }
    }
}
