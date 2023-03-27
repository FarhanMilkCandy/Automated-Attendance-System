using Automated_Attendance_System.Controller;
using Automated_Attendance_System.Entity.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Automated_Attendance_System.Helper
{
    public class SMSHelper
    {
        private SMSController _smsController = new SMSController();
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private SMSDTO _smsDTOs = new SMSDTO();
        private static List<string> _bssITEmp = new List<string>();

        public async Task<SMSDTO> GetDTO(string enrollmentNumber)
        {
            try
            {
                 _smsDTOs = await _smsController.GetSMSDTO(enrollmentNumber);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not retreive details for {enrollmentNumber} from DB. Exception: {ex.Message} \n");
            }
            return _smsDTOs;
        }

        public async Task SendSMS(string idNumber, string punchTime)
        {
            await _semaphore.WaitAsync(1);

            try
            {
                if (_bssITEmp.Count == 0)
                {
                    _bssITEmp = await _smsController.GetBSSITIds();
                }
                HttpWebRequest request;
                SMSDTO smsObj = await GetDTO(idNumber);
                string smsBody = $"Your attendance was recorded at {punchTime}.\nRegards,\nBSS";
                if (idNumber.StartsWith("2200"))
                {
                    smsBody = $"Hello, {smsObj.Name} (ID: {idNumber.Substring(idNumber.Length - 4)}) your attendance has been recorded: {DateTime.Now.Date.ToString("dd-MM-yyyy")} at {punchTime}.\n\nRegards,\nBSS.";
                }
                else if (idNumber.StartsWith("1100"))
                {
                    smsBody = $"We are pleased to notify you that your child ({smsObj.Name}, ID: {idNumber.Substring(idNumber.Length - 4)}) has been marked present today: {DateTime.Now.Date.ToString("dd-MM-yyyy")} at {punchTime}.\n\nRegards,\nBSS.";
                }
                bool eligible = _smsController.SMSEligible(idNumber).Result;
                if (eligible && _bssITEmp.Contains(idNumber))
                {
                    request = (HttpWebRequest)WebRequest.Create(@"https://powersms.banglaphone.net.bd/httpapi/sendsms?userId=bss1&password=Bss123&smsText=" + smsBody + "&commaSeperatedReceiverNumbers=" + smsObj.PhoneNumber);

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

                    await _smsController.SaveSMSDTOHistory(_smsDTOs);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal($"SMS Send failed. Excetion Details: {ex.Message} SMSHelper.cs: 80.");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
