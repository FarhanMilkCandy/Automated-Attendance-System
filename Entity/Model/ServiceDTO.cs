namespace Automated_Attendance_System.Entity.Model
{
    public class ServiceDTO
    {
        public string ServiceName { get; set; }

        public int PrimaryMailId { get; set; }

        public int? AlternativeMailId { get; set; }

        public bool Status { get; set; }
    }
}
