public class LogHelper
{
    //private static LogHelper _instance;
    ////private string _fileName = Environment.CurrentDirectory + $"\\AutomatedAttendanceSystemLogger_{DateTime.Now.Month}_{DateTime.Now.Year}.txt"; //For Test
    //private string _fileName = $"H:\\ProcessLogFile\\AutomatedAttendanceSystem\\AutomatedAttendanceSystemLogger_{DateTime.Now.Month}_{DateTime.Now.Year}.txt"; //For Deploy
    //private readonly EmailHelper _emailHelper = new EmailHelper();

    //public static LogHelper GetInstance()
    //{
    //    if (_instance == null)
    //    {
    //        return _instance = new LogHelper();
    //    }
    //    return _instance;
    //}

    //public void Log(string logMessage)
    //{
    //    try
    //    {
    //        //Console.WriteLine(_fileName);
    //        DateTime logTime = DateTime.Now;
    //        using (StreamWriter w = File.AppendText(_fileName))
    //        {
    //            w.WriteLine($"\r\n{logTime} : {logMessage};");
    //            w.Close();
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        //Console.WriteLine($"Exception in logging sytem: {ex.Message}\nStackTrace: {ex.StackTrace}");
    //        _emailHelper.SendLogExceptionEmail("Error", "Exception in loggin system", $"Exception in logging sytem: {ex.Message}<br>StackTrace: {ex.StackTrace}");
    //    }

    //}

    //public static void DumpLog(StreamReader r)
    //{
    //    string line;
    //    while ((line = r.ReadLine()) != null)
    //    {
    //        Console.WriteLine(line);
    //    }
    //}
}
