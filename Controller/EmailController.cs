using Automated_Attendance_System.Entity;
using Automated_Attendance_System.Entity.Model;
using Automated_Attendance_System.Helper;
using Serilog;
using System.Linq;
using System.Threading;

namespace Automated_Attendance_System.Controller
{
    public class EmailController
    {
        Entities _db = new Entities();

        private readonly ServiceDTO _serviceObj = ServiceHelper.GetDTOInstance();
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public EmailDTO loadPrimaryEmail()
        {
            _semaphore.Wait(1);
            try
            {
                return _db.BSS_EMAIL_SETTINGS.Where(w => w.EMAIL_ID == _serviceObj.PrimaryMailId).Select(s => new EmailDTO
                {
                    EmailAddress = s.EMAIL_NAME,
                    Password = s.SMTP_AUTHENTICATE_PASSWORD
                }).FirstOrDefault();
            }
            catch (System.Exception ex)
            {
                Log.Fatal($"Loading primary email failed. Excetion Details: {ex.Message} EmailController.cs: 30.");
                return null;
            }
            finally { _semaphore.Release(); }
        }

        public EmailDTO loadBackupEmail()
        {
            _semaphore.Wait(1);
            try
            {
                return _db.BSS_EMAIL_SETTINGS.Where(w => w.EMAIL_ID == _serviceObj.AlternativeMailId).Select(s => new EmailDTO
                {
                    EmailAddress = s.EMAIL_NAME,
                    Password = s.SMTP_AUTHENTICATE_PASSWORD
                }).FirstOrDefault();
            }
            catch (System.Exception ex)
            {
                Log.Fatal($"Bulk insertion into database failed. Excetion Details: {ex.Message} EmailController.cs: 49.");
                return null;
            }
            finally { _semaphore.Release(); }
        }
    }
}
