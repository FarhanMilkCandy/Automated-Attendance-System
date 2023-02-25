using Automated_Attendance_System.Controller;
using Automated_Attendance_System.Entity.Model;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Serilog;
using Newtonsoft.Json;

namespace Automated_Attendance_System.Helper
{
    public class SMSHelper
    {
        private SMSController _smsController = new SMSController();
        private readonly object _lock = new object();
        private SMSDTO _smsDTOs = new SMSDTO();

        public async Task<SMSDTO> GetDTO(string enrollmentNumber)
        {
            try
            {
               await Task.Run(() => { _smsDTOs = _smsController.GetSMSDTO(enrollmentNumber).Result; });
            }
            catch (Exception ex)
            {
                Log.Error($"Could not retreive details for {enrollmentNumber} from DB. Exception: {ex.Message} \n");
            }
           return _smsDTOs;
        }

        public async Task sendSMS(string idNumber, string punchTime)
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    HttpWebRequest request;
                    SMSDTO smsObj = GetDTO(idNumber).Result;
                    string smsBody = $"We are pleased to notify you that Your child({smsObj.Name}, ID: {idNumber.Substring(idNumber.Length - 4)}) has been marked present today: {DateTime.Now.Date} at {punchTime}.\n\nRegards, BSS.";
                    if (smsObj.PhoneNumber.Contains("01772980203"))
                    {
                        request = (HttpWebRequest)WebRequest.Create(@"https://powersms.banglaphone.net.bd/httpapi/sendsms?userId=bss1&password=Bss123&smsText=" + smsBody + "&commaSeperatedReceiverNumbers=" + smsObj.PhoneNumber);
                    }
                    else
                    {
                        request = (HttpWebRequest)WebRequest.Create(@"https://www.bing.com/");
                    }
                    request.Method = WebRequestMethods.Http.Get;
                    request.Accept = "application /json";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    string content = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted)
                    {
                        _smsDTOs.Status = (int)response.StatusCode;
                        _smsDTOs.StatusText = content;
                        _smsDTOs.SMSCount += 1;
                    }
                    else
                    {
                        _smsDTOs.ErrorCode = (int)response.StatusCode;
                        _smsDTOs.ErrorText = content;
                    }
                    _smsDTOs.SMSContent = smsBody;
                    _smsDTOs.SendDate = DateTime.Now;

                    _smsController.SaveSMSDTOHistory(_smsDTOs);
                }
            });
        }
    }
}
