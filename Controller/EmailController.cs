using Automated_Attendance_System.Entity;
using Automated_Attendance_System.Entity.Model;
using Automated_Attendance_System.Helper;
using System.Linq;

namespace Automated_Attendance_System.Controller
{
    public class EmailController
    {
        Entities _db = new Entities();

        private readonly ServiceDTO _serviceObj = ServiceHelper.GetDTOInstance();

        public EmailDTO loadPrimaryEmail()
        {
            return _db.BSS_EMAIL_SETTINGS.Where(w => w.EMAIL_ID == _serviceObj.PrimaryMailId).Select(s => new EmailDTO
            {
                EmailAddress = s.EMAIL_NAME,
                Password = s.SMTP_AUTHENTICATE_PASSWORD
            }).FirstOrDefault();
        }

        public EmailDTO loadBackupEmail()
        {
            return _db.BSS_EMAIL_SETTINGS.Where(w => w.EMAIL_ID == _serviceObj.AlternativeMailId).Select(s => new EmailDTO
            {
                EmailAddress = s.EMAIL_NAME,
                Password = s.SMTP_AUTHENTICATE_PASSWORD
            }).FirstOrDefault();
        }
    }
}
