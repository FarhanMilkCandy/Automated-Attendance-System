using Automated_Attendance_System.Controller;
using Automated_Attendance_System.Entity;
using Automated_Attendance_System.Entity.Model;
using Automated_Attendance_System.Helper;
using Automated_Attendance_System.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using zkemkeeper;

namespace Automated_Attendance_System.ZKTeco
{
    public class ZKTeco_Clients : IZKEM
    {
        #region Instantiating Class and Device object.
        private List<string> upFailed = new List<string>();
        private static readonly AttendanceController _controller = new AttendanceController();
        private static readonly UpdateController _updateCcontroller = new UpdateController();
        private readonly ConnectionHelper _connectionHelper = new ConnectionHelper();
        public bool connectionFlag = false;
        private static BSS_ATTENDANCE_ZK errorEnroll = new BSS_ATTENDANCE_ZK();
        private readonly List<BSS_ATTENDANCE_DEVICES> _deviceList = new List<BSS_ATTENDANCE_DEVICES>();
        private EmailHelper emailHelper = new EmailHelper();
        //private TimeSpan lastSendTime = DateTime.Now.TimeOfDay;
        private readonly int _deviceCount = _controller.GetAttendanceDeviceCount();
        Action<object, string> RaiseDeviceEvent;
        private SMSHelper _smsHelper;
        public ZKTeco_Clients(Action<object, string> RaiseDeviceEvent)
        {
            this.RaiseDeviceEvent = RaiseDeviceEvent;
        }

        CZKEM objCZKEM = new CZKEM();
        #endregion

        #region Used Methods

        #region Ping Device
        public bool PingDevice(string IPAdd, int Port)
        {
            return _connectionHelper.PingTheDevice(IPAdd);
        }
        #endregion

        #region Connect Device

        //Connect to device
        public bool Connect_Net(string IPAdd, int Port)
        {
            if (objCZKEM.Connect_Net(IPAdd, Port))
            {
                ConnectionHelper.connectedDeviceCount++;
                //65535, 32767
                if (objCZKEM.RegEvent(1, 65535))
                {
                    // [ Register your events here ]
                    // [ Go through the _IZKEMEvents_Event class for a complete list of events ]
                    this.objCZKEM.OnDisConnected += objCZKEM_OnDisConnected;
                    this.objCZKEM.OnAttTransactionEx += zkemClient_OnAttTransactionEx;
                    this.objCZKEM.OnGeneralEvent += new _IZKEMEvents_OnGeneralEventEventHandler(ObjCZKEM_OnGeneralEvent);

                    int machineNumber = 0;
                    string productCode = string.Empty;
                    this.GetDeviceInfo(1, 2, ref machineNumber);
                    objCZKEM.MachineNumber = machineNumber;
                    objCZKEM.SetDeviceTime(objCZKEM.MachineNumber);
                    objCZKEM.GetProductCode(machineNumber, out productCode);
                    Console.BackgroundColor = ConsoleColor.Yellow; Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine($"\n>>Device {machineNumber} @{IPAdd}; Model: {productCode} is connected successfully!");
                    //if (productCode != "MB560-VL/ID" && !connectionFlag)
                    //{
                    ObjCZKEM_OnConnected(machineNumber);
                    GetStdUpdate(machineNumber, _updateCcontroller.GetStudentUpdates()).GetAwaiter();
                    //}
                    GetHRUpdate(machineNumber, _updateCcontroller.GetHRUpdates()).GetAwaiter();
                    if (upFailed != null && upFailed.Count > 0)
                    {
                        Log.Error($"Enrollment ID : {string.Join(", ", upFailed.Distinct().ToList())} was not synced properly with device {machineNumber}");
                        bool emailFlag = emailHelper.SendEmail("Error", "Latest Data Sync Failed", $"Enrollment ID : {string.Join(", ", upFailed.Distinct().ToList())} was not synced properly with device {machineNumber}");
                        upFailed.Clear();
                        if (emailFlag)
                        {
                            Log.Information($"Enrollment sync failure email sent successfully");
                        }
                        else
                        {
                            Log.Error("Error sending enrollment sync failure email");
                            Log.Information($"\"Trying Backup email.\n");
                            bool bkpMailFlag = emailHelper.SendEmailBackup("Error", "Latest Data Sync Failed", $"Enrollment ID : {string.Join(", ", upFailed.Distinct().ToList())} was not synced properly with device {machineNumber}");
                            if (bkpMailFlag)
                            {
                                Log.Information($"\"Device Connection Failed\" email sent successfully using backup mail.\n");
                                Console.WriteLine($"\"Device Connection Failed\" email sent successfully using backup mail.\n");
                            }
                            else
                            {
                                Log.Fatal($"\"Device Connection Failed\" email sending unsuccessful even with backup mail.\n");
                                Console.WriteLine($"\"Device Connection Failed\" email sending unsuccessful even with backup mail.\n");
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }

        //Reconnect to device
        public bool Reconnect_Net(string IPAdd, int Port)
        {
            if (objCZKEM.Connect_Net(IPAdd, Port))
            {
                //65535, 32767
                if (objCZKEM.RegEvent(1, 65535))
                {
                    int machineNumber = 0;
                    string productCode = string.Empty;
                    this.GetDeviceInfo(1, 2, ref machineNumber);
                    objCZKEM.MachineNumber = machineNumber;
                    objCZKEM.SetDeviceTime(objCZKEM.MachineNumber);
                    objCZKEM.GetProductCode(machineNumber, out productCode);
                    ObjCZKEM_OnConnected(machineNumber);

                }
                return true;
            }
            return false;
        }

        #endregion

        public async Task GetHRUpdate(int machineNumber, List<HR_EMPLOYEE> hrList)
        {
            bool syncCard = false;
            bool syncInfo = false;
            int temp = 0;
            string dwpassword = string.Empty;
            string dwname = string.Empty;
            int dwprivillege = 0;
            bool dwenabled = false;
            List<HR_EMPLOYEE> empList = hrList.ToList();
            if (empList != null && empList.Count > 0)
            {
                Log.Information($"Employee data updates found. Total: {empList.Count}. Data will sync to Device {machineNumber}\n");
                foreach (HR_EMPLOYEE emp in _updateCcontroller.GetHRUpdates())
                {
                    if (int.TryParse("2200" + emp.EMP_ID.Substring(emp.EMP_ID.Length - 4), out temp))
                    {
                        if (this.SSR_GetUserInfo(machineNumber, temp.ToString(), out dwname, out dwpassword, out dwprivillege, out dwenabled))
                        {
                            await Task.Run(() =>
                            {
                                syncCard = this.SetStrCardNumber(emp.PUNCH_CARD_ID);
                                syncInfo = this.SSR_SetUserInfo(machineNumber, temp.ToString(), emp.EMP_FIRST_NAME + " " + emp.EMP_MIDDLE_NAME + " " + emp.EMP_LAST_NAME, dwpassword, dwprivillege, dwenabled);
                                if (syncCard && syncInfo)
                                {
                                    if (_deviceCount == ConnectionHelper.connectedDeviceCount)
                                    {
                                        Log.Information($"Latest updated data upload complete. Uploaded to all {ConnectionHelper.connectedDeviceCount}/{_deviceCount} devices\n");
                                        int flag = _updateCcontroller.SetHRSyncStatus(emp);
                                        if (flag <= 0)
                                        {
                                            Log.Error($"Could not update database flag for latest data of Employee ID : {temp}\n");
                                            upFailed.Add(temp.ToString());
                                            Console.BackgroundColor = ConsoleColor.Red; Console.ForegroundColor = ConsoleColor.Black;
                                            Console.WriteLine($"\n>> Could not upload latest data of Employee ID : {temp}");
                                        }
                                        else
                                        {
                                            empList.Clear();
                                        }
                                    }
                                }
                            });
                        }
                        else
                        {
                            await Task.Run(() =>
                            {
                                syncCard = this.SetStrCardNumber(emp.PUNCH_CARD_ID);
                                syncInfo = this.SSR_SetUserInfo(machineNumber, "2200" + emp.EMP_ID.Substring(emp.EMP_ID.Length - 4), emp.EMP_FIRST_NAME + " " + emp.EMP_MIDDLE_NAME + " " + emp.EMP_LAST_NAME, string.Empty, 0, true);
                                if (syncCard && syncInfo)
                                {
                                    if (_deviceCount == ConnectionHelper.connectedDeviceCount)
                                    {
                                        Log.Information($"Latest updated data upload complete. Uploaded to all {ConnectionHelper.connectedDeviceCount}/{_deviceCount} devices\n");
                                        int flag = _updateCcontroller.SetHRSyncStatus(emp);
                                        if (flag <= 0)
                                        {
                                            Log.Error($"Could not upload latest data of Employee ID : {temp}\n");
                                            upFailed.Add(temp.ToString());
                                            Console.BackgroundColor = ConsoleColor.Red; Console.ForegroundColor = ConsoleColor.Black;
                                            Console.WriteLine($"\n>> Could not upload latest data of Employee ID : {temp}");
                                        }
                                        else
                                        {
                                            empList.Clear();
                                        }
                                    }
                                }
                                else
                                {
                                    Log.Error($"Could not upload latest data of Employee ID : {temp}. Card sync: {syncCard} & Information sync: {syncInfo}\n");
                                }
                            });
                        }
                    }
                    else
                    {
                        Log.Error($"Could not parse Employee ID : {"2200" + emp.EMP_ID.Substring(emp.EMP_ID.Length - 4)}\n");
                        Console.BackgroundColor = ConsoleColor.Red; Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"\n>> Could not parse Employee ID : {"2200" + emp.EMP_ID.Substring(emp.EMP_ID.Length - 4)}");
                    }
                }
            }
            else
            {
                Log.Information($"No new employee updatable data found");
            }
        }

        public async Task GetStdUpdate(int machineNumber, List<BSS_STUDENT> studentList)
        {
            bool syncCard = false;
            bool syncInfo = false;
            int temp = 0;
            string dwpassword = string.Empty;
            string dwname = string.Empty;
            int dwprivillege = 0;
            bool dwenabled = false;
            List<BSS_STUDENT> stdList = studentList.ToList();
            if (stdList != null && stdList.Count > 0)
            {
                Log.Information($"Student data updates found. Total: {stdList.Count}. Data will sync to Device {machineNumber}\n");
                foreach (BSS_STUDENT std in _updateCcontroller.GetStudentUpdates())
                {
                    if (int.TryParse("1100" + std.STUDENT_ID.Substring(std.STUDENT_ID.Length - 4), out temp))
                    {
                        if (this.SSR_GetUserInfo(machineNumber, temp.ToString(), out dwname, out dwpassword, out dwprivillege, out dwenabled))
                        {
                            await Task.Run(() =>
                            {
                                syncCard = this.SetStrCardNumber(std.PROXIMITY_NUM);
                                syncInfo = this.SSR_SetUserInfo(machineNumber, temp.ToString(), std.FIRST_NAME + " " + std.MIDDLE_NAME + " " + std.LAST_NAME, dwpassword, dwprivillege, dwenabled);
                                if (syncCard && syncInfo)
                                {
                                    if (_deviceCount == ConnectionHelper.connectedDeviceCount)
                                    {
                                        Log.Information($"Latest updated data upload complete. Uploaded to all {ConnectionHelper.connectedDeviceCount}/{_deviceCount} devices\n");
                                        int flag = _updateCcontroller.SetStudentSyncStatus(std);
                                        if (flag <= 0)
                                        {
                                            Log.Error($"Could not sync latest data of Student ID : {temp} \n");
                                            upFailed.Add(temp.ToString());
                                            Console.BackgroundColor = ConsoleColor.Red; Console.ForegroundColor = ConsoleColor.Black;
                                            Console.WriteLine($"\n>> Could not upload latest data of Student ID : {temp}");
                                        }
                                        else
                                        {
                                            stdList.Clear();
                                        }
                                    }
                                }
                                else
                                {
                                    Log.Error($"Could not upload latest data of Student ID : {temp}. Card sync: {syncCard} & Information sync: {syncInfo}\n");
                                }
                            });
                        }
                        else
                        {
                            await Task.Run(() =>
                            {
                                syncCard = this.SetStrCardNumber(std.PROXIMITY_NUM);
                                syncInfo = this.SSR_SetUserInfo(machineNumber, "2200" + std.STUDENT_ID.Substring(std.STUDENT_ID.Length - 4), std.FIRST_NAME + " " + std.MIDDLE_NAME + " " + std.LAST_NAME, string.Empty, 0, true);
                                if (syncCard && syncInfo)
                                {
                                    if (_deviceCount == ConnectionHelper.connectedDeviceCount)
                                    {
                                        Log.Information($"Latest updated data upload complete. Uploaded to all {ConnectionHelper.connectedDeviceCount}/{_deviceCount} devices\n");
                                        int flag = _updateCcontroller.SetStudentSyncStatus(std);
                                        if (flag <= 0)
                                        {
                                            Log.Error($"Could not upload latest data of Student ID : {temp}\n");
                                            upFailed.Add(temp.ToString());
                                            Console.BackgroundColor = ConsoleColor.Red; Console.ForegroundColor = ConsoleColor.Black;
                                            Console.WriteLine($"\n>> Could not upload latest data of Student ID : {temp}");
                                        }
                                        else
                                        {
                                            stdList.Clear();
                                        }
                                    }
                                }
                                else
                                {
                                    Log.Error($"Could not upload latest data of Student ID : {temp}. Card sync: {syncCard} & Information sync: {syncInfo}\n");
                                }
                            });
                        }
                    }
                    else
                    {
                        Log.Error($"Could not parse Student ID : {"2200" + std.STUDENT_ID.Substring(std.STUDENT_ID.Length - 4)}\n");
                        Console.BackgroundColor = ConsoleColor.Red; Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"\n>> Could not parse Student ID : {"2200" + std.STUDENT_ID.Substring(std.STUDENT_ID.Length - 4)}");
                    }
                }
            }
            else
            {
                Log.Information($"No new updatable student data found\n");
            }
        }

        private async void ObjCZKEM_OnConnected(int machineNumber)
        {
            //Remove Task Run if any thread safe error is thrown
            await Task.Run(async () =>
            {
                try
                {
                    #region Variables
                    string dwEnrollNumber = string.Empty;
                    int dwVerifyMode = 0;
                    int dwInOutMode = 0;
                    int dwYear = 0;
                    int dwMonth = 0;
                    int dwDay = 0;
                    int dwHour = 0;
                    int dwMinute = 0;
                    int dwSecond = 0;
                    int dwWorkCode = 0;
                    #endregion
                    if (!connectionFlag)
                    {
                        objCZKEM.SetDeviceTime(machineNumber);
                        objCZKEM.ReadAllGLogData(machineNumber);
                        #region push attendance to DB
                        List<BSS_ATTENDANCE_ZK> attendances = new List<BSS_ATTENDANCE_ZK>();
                        while (objCZKEM.SSR_GetGeneralLogData(machineNumber, out dwEnrollNumber, out dwVerifyMode, out dwInOutMode, out dwYear, out dwMonth, out dwDay, out dwHour, out dwMinute, out dwSecond, ref dwWorkCode))
                        {
                            DateTime PunchDate = new DateTime(dwYear, dwMonth, dwDay);
                            TimeSpan PunchTime = new TimeSpan(dwHour, dwMinute, dwSecond);
                            BSS_ATTENDANCE_ZK dtObj = new BSS_ATTENDANCE_ZK
                            {
                                Machine_Number = machineNumber,
                                Enrollment_Number = int.TryParse(dwEnrollNumber, out int temp) ? temp : -1,
                                Verify_Method = dwVerifyMode,
                                Punch_Date = PunchDate,
                                Punch_Time = PunchTime,
                                Work_Code = dwWorkCode,
                                Sync_Status = false
                            };
                            attendances.Add(dtObj);
                        }
                        bool flag = false;
                        if (attendances.Count > 0)
                        {

                            flag = await _controller.RecordPreviousAttendance(attendances);
                        }
                        #endregion

                        bool clearFlag = objCZKEM.ClearData(machineNumber, 1);
                        //bool clearFlag = true;
                        if (!flag)
                        {
                            Log.Fatal($"Error storing {errorEnroll} attendance data to DB after system wake up.\n");
                            bool emailFlag = emailHelper.SendEmail("error", "Error in Automated Attendance System", $"Exception storing {errorEnroll} attendance data to DB after system wake up.");
                            if (!emailFlag)
                            {
                                Log.Error($"Error sending email for data recording after wakeup\n");
                            }
                            else
                            {
                                Log.Information($"Sending email for data recording after wakeup was success\n");
                                Log.Information($"\"Trying Backup email.\n");
                                bool bkpMailFlag = emailHelper.SendEmailBackup("error", "Error in Automated Attendance System", $"Exception storing {errorEnroll} attendance data to DB after system wake up.");
                                if (bkpMailFlag)
                                {
                                    Log.Information($"\"Device Connection Failed\" email sent successfully using backup mail.\n");
                                    Console.WriteLine($"\"Device Connection Failed\" email sent successfully using backup mail.\n");
                                }
                                else
                                {
                                    Log.Fatal($"\"Device Connection Failed\" email sending unsuccessful even with backup mail.\n");
                                    Console.WriteLine($"\"Device Connection Failed\" email sending unsuccessful even with backup mail.\n");
                                }
                            }
                        }
                        else
                        {
                            Log.Information($"No error while wake-up data recording to device.\n");
                        }

                        #region Console and Log
                        Log.Information($"Read Data successfull from device {objCZKEM.MachineNumber}\n");
                        Console.BackgroundColor = ConsoleColor.Green;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine($"\n>>Read Data successfull from device {objCZKEM.MachineNumber}");
                        Console.WriteLine($"\n>>Clearing device {objCZKEM.MachineNumber}");
                        #endregion


                        if (clearFlag)
                        {
                            #region Console
                            Log.Information($"Clear device {objCZKEM.MachineNumber} was success\n");
                            Console.BackgroundColor = ConsoleColor.Green;
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.WriteLine($"\n>>Clear device {objCZKEM.MachineNumber} was success.\n");
                            #endregion
                        }
                        else
                        {
                            #region Console
                            Log.Error($"Could not clear data from device {objCZKEM.MachineNumber}. The device may not have any data or unknown error occured\n");
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.WriteLine($"\n>>Could not clear data from device {objCZKEM.MachineNumber}. The device may not have any data or unknown error occured.\n");
                            #endregion
                        }
                    }
                }
                catch (Exception ex)
                {
                    #region Console and log
                    Log.Error($"Error while acquiring data from device {objCZKEM.MachineNumber}. Exception: {ex.Message}.\r\nStackTrace:\r\n{ex.StackTrace}\n");
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine($"\n>>Error while acquiring data from device {objCZKEM.MachineNumber}. Exception: {ex.Message}");
                    bool emailFlag = emailHelper.SendEmail("Error", "Exception while recording device data on wakeup", $"Error while acquiring data from device {objCZKEM.MachineNumber}. Exception: {ex.Message}.</br>StackTrace:</br><code>{ex.StackTrace}</code>");
                    #endregion
                }
            });
        }

        public async void zkemClient_OnAttTransactionEx(string EnrollNumber, int IsInValid, int AttState, int VerifyMethod, int Year, int Month, int Day, int Hour, int Minute, int Second, int WorkCode)
        {
            await RealTimePush(EnrollNumber, IsInValid, AttState, VerifyMethod, Year, Month, Day, Hour, Minute, Second, WorkCode);
            await RealTimeMessageSend(EnrollNumber, Hour, Minute, Second);
        }

        private async Task RealTimePush(string EnrollNumber, int IsInValid, int AttState, int VerifyMethod, int Year, int Month, int Day, int Hour, int Minute, int Second, int WorkCode)
        {
            DateTime PunchDate = new DateTime(Year, Month, Day);
            TimeSpan PunchTime = new TimeSpan(Hour, Minute, Second);
            try
            {
                if (!string.IsNullOrEmpty(EnrollNumber) && !string.IsNullOrEmpty(objCZKEM.MachineNumber.ToString()) && PunchDate != null && PunchTime != null)
                {
                    //Console.WriteLine("\n>>Transaction happened");
                    errorEnroll = await Task.Run(()=> _controller.RecordAttendance(objCZKEM.MachineNumber, EnrollNumber, VerifyMethod, PunchDate, PunchTime, WorkCode));
                }
                if (errorEnroll != null)
                {
                    Log.Fatal($"Exception storing attendance data to DB in real time for: {errorEnroll}.\n Data could not be inserted even after retry.\n");
                    emailHelper.SendEmail("error", "Error in Automated Attendance System", $"Exception storing attendance data to DB in real time for: {errorEnroll}\n Data could not be inserted even after retry.\n");
                    Console.WriteLine($"\n>>Error mail sent at {DateTime.Now}\n");
                    errorEnroll = null;
                }
            }
            catch (Exception ex)
            {
                await ExceptionHandler(objCZKEM.MachineNumber, ex);
            }
        }

        private async Task RealTimeMessageSend(string idNumber, int Hour, int Minute, int Second)
        {
            TimeSpan PunchTime = new TimeSpan(Hour, Minute, Second);
            _smsHelper = new SMSHelper();
            await _smsHelper.SendSMS(idNumber, PunchTime.ToString(""));
        }

        private async Task ExceptionHandler(int MachineNumber, Exception ex) => await Task.Run(() =>
        {
            #region Console and log
            Log.Error($"Exception while recording realtime data from device {MachineNumber}. Exception: {ex.Message}.\r\nStackTrace:\r\n{ex.StackTrace}\n");
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine($"\n>>Error while recodring realtime data from device {MachineNumber}. Exception: {ex.Message}");
            bool emailFlag = emailHelper.SendEmail("Error", "Exception while recording realtime data on wakeup", $"Error while acquiring data from device {MachineNumber}. Exception: {ex.Message}.</br>StackTrace:</br><code>{ex.StackTrace}</code>");
            #endregion
        });

        void objCZKEM_OnDisConnected()
        {
            // Implementing the Event
            RaiseDeviceEvent(this, "Disconnected");
        }

        public bool ReadSuperLogData(int dwMachineNumber)
        {
            return objCZKEM.ReadSuperLogData(dwMachineNumber);
        }
        public bool ReadGeneralLogData(int dwMachineNumber)
        {
            return objCZKEM.ReadGeneralLogData(dwMachineNumber);
        }

        public bool ReadAllGLogData(int dwMachineNumber)
        {
            return objCZKEM.ReadAllGLogData(dwMachineNumber);
        }
        public bool GetDeviceStatus(int dwMachineNumber, int dwStatus, ref int dwValue)
        {
            return objCZKEM.GetDeviceStatus(dwMachineNumber, dwStatus, ref dwValue);
        }

        public bool GetDeviceInfo(int dwMachineNumber, int dwInfo, ref int dwValue)
        {
            var x = objCZKEM.GetDeviceInfo(dwMachineNumber, dwInfo, ref dwValue);
            return x;
        }
        public bool SetDeviceTime(int dwMachineNumber)
        {
            return objCZKEM.SetDeviceTime(dwMachineNumber);
        }
        public bool GetEnrollData(int dwMachineNumber, int dwEnrollNumber, int dwEMachineNumber, int dwBackupNumber, ref int dwMachinePrivilege, ref int dwEnrollData, ref int dwPassWord)
        {
            return objCZKEM.GetEnrollData(dwMachineNumber, dwEnrollNumber, dwEMachineNumber, dwBackupNumber, ref dwMachinePrivilege, ref dwEnrollData, ref dwPassWord);
        }

        public bool SetEnrollData(int dwMachineNumber, int dwEnrollNumber, int dwEMachineNumber, int dwBackupNumber, int dwMachinePrivilege, ref int dwEnrollData, int dwPassWord)
        {
            return objCZKEM.SetEnrollData(dwMachineNumber, dwEnrollNumber, dwEMachineNumber, dwBackupNumber, dwMachinePrivilege, ref dwEnrollData, dwPassWord);
        }

        public bool GetDeviceTime(int dwMachineNumber, ref int dwYear, ref int dwMonth, ref int dwDay, ref int dwHour, ref int dwMinute, ref int dwSecond)
        {
            return GetDeviceTime(dwMachineNumber, ref dwYear, ref dwMonth, ref dwDay, ref dwHour, ref dwMinute, ref dwSecond);
        }

        public bool GetGeneralLogData(int dwMachineNumber, ref int dwTMachineNumber, ref int dwEnrollNumber, ref int dwEMachineNumber, ref int dwVerifyMode, ref int dwInOutMode, ref int dwYear, ref int dwMonth, ref int dwDay, ref int dwHour, ref int dwMinute)
        {
            return objCZKEM.GetGeneralLogData(dwMachineNumber, ref dwTMachineNumber, ref dwEnrollNumber, ref dwEMachineNumber, ref dwVerifyMode, ref dwInOutMode, ref dwYear, ref dwMonth, ref dwDay, ref dwHour, ref dwMinute);
        }
        public bool ReadAllUserID(int dwMachineNumber)
        {
            return objCZKEM.ReadAllUserID(dwMachineNumber);
        }

        public bool GetAllUserID(int dwMachineNumber, ref int dwEnrollNumber, ref int dwEMachineNumber, ref int dwBackupNumber, ref int dwMachinePrivilege, ref int dwEnable)
        {
            return objCZKEM.GetAllUserID(dwMachineNumber, dwEnrollNumber, dwEMachineNumber, dwBackupNumber, dwMachinePrivilege, dwEnable);
        }

        public bool GetSerialNumber(int dwMachineNumber, out string dwSerialNumber)
        {
            return objCZKEM.GetSerialNumber(dwMachineNumber, out dwSerialNumber);
        }
        public int GetBackupNumber(int dwMachineNumber)
        {
            return objCZKEM.GetBackupNumber(dwMachineNumber);
        }

        public bool GetProductCode(int dwMachineNumber, out string lpszProductCode)
        {
            return objCZKEM.GetProductCode(dwMachineNumber, out lpszProductCode);
        }

        public bool GetFirmwareVersion(int dwMachineNumber, ref string strVersion)
        {
            return objCZKEM.GetFirmwareVersion(dwMachineNumber, ref strVersion);
        }

        public bool GetSDKVersion(ref string strVersion)
        {
            return objCZKEM.GetSDKVersion(ref strVersion);
        }

        public bool ClearGLog(int dwMachineNumber)
        {
            return objCZKEM.ClearGLog(dwMachineNumber);
        }
        public void Disconnect()
        {
            objCZKEM.Disconnect();
        }
        public bool SetUserInfo(int dwMachineNumber, int dwEnrollNumber, string Name, string Password, int Privilege, bool Enabled)
        {
            return objCZKEM.SetUserInfo(dwMachineNumber, dwEnrollNumber, Name, Password, Privilege, Enabled);
        }

        public bool GetUserInfo(int dwMachineNumber, int dwEnrollNumber, ref string Name, ref string Password, ref int Privilege, ref bool Enabled)
        {
            return objCZKEM.GetUserInfo(dwMachineNumber, dwEnrollNumber, ref Name, ref Password, ref Privilege, ref Enabled);
        }
        public bool GetDeviceIP(int dwMachineNumber, ref string IPAddr)
        {
            return objCZKEM.GetDeviceIP(dwMachineNumber, ref IPAddr);
        }
        public bool GetAllUserInfo(int dwMachineNumber, ref int dwEnrollNumber, ref string Name, ref string Password, ref int Privilege, ref bool Enabled)
        {
            return objCZKEM.GetAllUserInfo(dwMachineNumber, ref dwEnrollNumber, ref Name, ref Password, ref Privilege, ref Enabled);
        }
        public bool RefreshData(int dwMachineNumber)
        {
            return objCZKEM.RefreshData(dwMachineNumber);
        }

        public bool GetEnrollDataStr(int dwMachineNumber, int dwEnrollNumber, int dwEMachineNumber, int dwBackupNumber, ref int dwMachinePrivilege, ref string dwEnrollData, ref int dwPassWord)
        {
            return objCZKEM.GetEnrollDataStr(dwMachineNumber, dwEnrollNumber, dwEMachineNumber, dwBackupNumber, ref dwMachinePrivilege, ref dwEnrollData, ref dwPassWord);
        }
        public bool UpdateFirmware(string FirmwareFile)
        {
            return objCZKEM.UpdateFirmware(FirmwareFile);
        }
        public bool WriteLCD(int Row, int Col, string Text)
        {
            return objCZKEM.WriteLCD(2, 2, "Test");
        }
        public bool PlayVoiceByIndex(int Index)
        {
            return objCZKEM.PlayVoiceByIndex(Index);
        }
        public bool ReadAllTemplate(int dwMachineNumber)
        {
            return objCZKEM.ReadAllTemplate(dwMachineNumber);
        }

        public bool GetDeviceMAC(int dwMachineNumber, ref string sMAC)
        {
            return objCZKEM.GetDeviceMAC(dwMachineNumber, sMAC);
        }
        public bool GetWiegandFmt(int dwMachineNumber, ref string sWiegandFmt)
        {
            return objCZKEM.GetWiegandFmt(dwMachineNumber, sWiegandFmt);
        }
        public bool GetVendor(ref string strVendor)
        {
            return objCZKEM.GetVendor(strVendor);
        }
        public bool BeginBatchUpdate(int dwMachineNumber, int UpdateFlag)
        {
            return objCZKEM.BeginBatchUpdate(dwMachineNumber, UpdateFlag);
        }

        public bool BatchUpdate(int dwMachineNumber)
        {
            return objCZKEM.BatchUpdate(dwMachineNumber);
        }

        public bool ClearData(int dwMachineNumber, int DataFlag)
        {
            return objCZKEM.ClearData(dwMachineNumber, DataFlag);
        }
        public bool GetGeneralExtLogData(int dwMachineNumber, ref int dwEnrollNumber, ref int dwVerifyMode, ref int dwInOutMode, ref int dwYear, ref int dwMonth, ref int dwDay, ref int dwHour, ref int dwMinute, ref int dwSecond, ref int dwWorkCode, ref int dwReserved)
        {
            return objCZKEM.GetGeneralExtLogData(dwMachineNumber, ref dwEnrollNumber, ref dwVerifyMode, ref dwInOutMode, ref dwYear, ref dwMonth, ref dwDay, ref dwHour, ref dwMinute, ref dwSecond, ref dwWorkCode, ref dwReserved);
        }

        public bool SSR_GetGeneralLogData(int dwMachineNumber, out string dwEnrollNumber, out int dwVerifyMode, out int dwInOutMode, out int dwYear, out int dwMonth, out int dwDay, out int dwHour, out int dwMinute, out int dwSecond, ref int dwWorkCode)
        {
            return objCZKEM.SSR_GetGeneralLogData(dwMachineNumber, out dwEnrollNumber, out dwVerifyMode, out dwInOutMode, out dwYear, out dwMonth, out dwDay, out dwHour, out dwMinute, out dwSecond, ref dwWorkCode);
        }

        public bool SSR_GetAllUserInfo(int dwMachineNumber, out string dwEnrollNumber, out string Name, out string Password, out int Privilege, out bool Enabled)
        {
            return objCZKEM.SSR_GetAllUserInfo(dwMachineNumber, out dwEnrollNumber, out Name, out Password, out Privilege, out Enabled);
        }

        public bool SSR_GetUserInfo(int dwMachineNumber, string dwEnrollNumber, out string Name, out string Password, out int Privilege, out bool Enabled)
        {
            return objCZKEM.SSR_GetUserInfo(dwMachineNumber, dwEnrollNumber, out Name, out Password, out Privilege, out Enabled);
        }
        public bool SSR_GetUserTmpStr(int dwMachineNumber, string dwEnrollNumber, int dwFingerIndex, out string TmpData, out int TmpLength)
        {
            return objCZKEM.SSR_GetUserTmpStr(dwMachineNumber, dwEnrollNumber, dwFingerIndex, out TmpData, out TmpLength);
        }
        public bool SSR_SetUserInfo(int dwMachineNumber, string dwEnrollNumber, string Name, string Password, int Privilege, bool Enabled)
        {
            return objCZKEM.SSR_SetUserInfo(dwMachineNumber, dwEnrollNumber, Name, Password, Privilege, Enabled);
        }

        public bool SSR_SetUserTmp(int dwMachineNumber, string dwEnrollNumber, int dwFingerIndex, ref byte TmpData)
        {
            return objCZKEM.SSR_SetUserTmp(dwMachineNumber, dwEnrollNumber, dwFingerIndex, ref TmpData);
        }

        public bool SSR_SetUserTmpStr(int dwMachineNumber, string dwEnrollNumber, int dwFingerIndex, string TmpData)
        {
            return objCZKEM.SSR_SetUserTmpStr(dwMachineNumber, dwEnrollNumber, dwFingerIndex, TmpData);
        }
        public bool SetStrCardNumber(string ACardNumber)
        {
            return objCZKEM.SetStrCardNumber(ACardNumber);
        }

        public bool RegEvent(int dwMachineNumber, int EventMask)
        {
            return objCZKEM.RegEvent(dwMachineNumber, EventMask);
        }
        public bool StartEnrollEx(string UserID, int FingerID, int Flag)
        {
            return objCZKEM.StartEnrollEx(UserID, FingerID, Flag);
        }

        public bool GetUserTmpExStr(int dwMachineNumber, string dwEnrollNumber, int dwFingerIndex, out int Flag, out string TmpData, out int TmpLength)
        {
            return objCZKEM.GetUserTmpExStr(dwMachineNumber, dwEnrollNumber, dwFingerIndex, out Flag, out TmpData, out TmpLength);
        }
        public int PINWidth => objCZKEM.PINWidth;

        public int MachineNumber { get => objCZKEM.MachineNumber; set => objCZKEM.MachineNumber = value; }

        public int BatchDataMode { get => objCZKEM.BatchDataMode; set => objCZKEM.BatchDataMode = value; }

        public bool SSR_GetGeneralLogDataWithMask(int dwMachineNumber, out string dwEnrollNumber, out int dwVerifyMode, out int dwInOutMode, out int dwYear, out int dwMonth, out int dwDay, out int dwHour, out int dwMinute, out int dwSecond, ref int dwWorkCode, out int dwMask, out string dwTemperature)
        {
            return objCZKEM.SSR_GetGeneralLogDataWithMask(dwMachineNumber, out dwEnrollNumber, out dwVerifyMode, out dwInOutMode, out dwYear, out dwMonth, out dwDay, out dwHour, out dwMinute, out dwSecond, ref dwWorkCode, out dwMask, out dwTemperature);
        }

        public bool SaveThermalImage(int dwMachineNumber)
        {
            return objCZKEM.SaveThermalImage(dwMachineNumber);
        }

        public bool SendFileByProduce(int dwMachineNumber, string FileName)
        {
            return objCZKEM.SendFileByProduce(dwMachineNumber, FileName);
        }

        public bool SSR_GetGeneralLogDataWithMaskEx(int dwMachineNumber, out string dwEnrollNumber, out int dwVerifyMode, out int dwInOutMode, out int dwYear, out int dwMonth, out int dwDay, out int dwHour, out int dwMinute, out int dwSecond, ref int dwWorkCode, out int dwMask, out string dwTemperature, out int dwhelmelhat)
        {
            return objCZKEM.SSR_GetGeneralLogDataWithMaskEx(dwMachineNumber, out dwEnrollNumber, out dwVerifyMode, out dwInOutMode, out dwYear, out dwMonth, out dwDay, out dwHour, out dwMinute, out dwSecond, ref dwWorkCode, out dwMask, out dwTemperature, out dwhelmelhat);
        }

        public bool SaveThermalImage_V2(int dwMachineNumber, string Reserved)
        {
            return objCZKEM.SaveThermalImage_V2(dwMachineNumber, Reserved);
        }

        public bool GetUserFacePhotoByNameEx(int dwMachineNumber, string PhotoName, out string PhotoData, out int PhotoLength)
        {
            return objCZKEM.GetUserFacePhotoByNameEx(dwMachineNumber, PhotoName, out PhotoData, out PhotoLength);
        }

        public bool UploadUserPhotoDataStr(int dwMachineNumber, string FileName, string FileDataStr, int DataLen)
        {
            return objCZKEM.UploadUserPhotoDataStr(dwMachineNumber, FileName, FileDataStr, DataLen);
        }

        public bool DownloadUserPhotoDataStr(int dwMachineNumber, string FileName, out string FileDataStr, out int DataLen)
        {
            return objCZKEM.DownloadUserPhotoDataStr(dwMachineNumber, FileName, out FileDataStr, out DataLen);
        }

        #endregion

        #region SSR_ZOne

        public bool SSR_GetUserTmp(int dwMachineNumber, string dwEnrollNumber, int dwFingerIndex, out byte TmpData, out int TmpLength)
        {
            throw new NotImplementedException();
        }

        public bool SSR_DeleteEnrollData(int dwMachineNumber, string dwEnrollNumber, int dwBackupNumber)
        {
            throw new NotImplementedException();
        }

        public bool SSR_DelUserTmp(int dwMachineNumber, string dwEnrollNumber, int dwFingerIndex)
        {
            throw new NotImplementedException();
        }

        public bool SSR_OutPutHTMLRep(int dwMachineNumber, string dwEnrollNumber, string AttFile, string UserFile, string DeptFile, string TimeClassFile, string AttruleFile, int BYear, int BMonth, int BDay, int BHour, int BMinute, int BSecond, int EYear, int EMonth, int EDay, int EHour, int EMinute, int ESecond, string TempPath, string OutFileName, int HTMLFlag, int resv1, string resv2)
        {
            throw new NotImplementedException();
        }

        public bool SSR_SetUnLockGroup(int dwMachineNumber, int CombNo, int Group1, int Group2, int Group3, int Group4, int Group5)
        {
            throw new NotImplementedException();
        }

        public bool SSR_GetUnLockGroup(int dwMachineNumber, int CombNo, ref int Group1, ref int Group2, ref int Group3, ref int Group4, ref int Group5)
        {
            throw new NotImplementedException();
        }

        public bool SSR_SetGroupTZ(int dwMachineNumber, int GroupNo, int Tz1, int Tz2, int Tz3, int VaildHoliday, int VerifyStyle)
        {
            throw new NotImplementedException();
        }

        public bool SSR_GetGroupTZ(int dwMachineNumber, int GroupNo, ref int Tz1, ref int Tz2, ref int Tz3, ref int VaildHoliday, ref int VerifyStyle)
        {
            throw new NotImplementedException();
        }

        public bool SSR_GetHoliday(int dwMachineNumber, int HolidayID, ref int BeginMonth, ref int BeginDay, ref int EndMonth, ref int EndDay, ref int TimeZoneID)
        {
            throw new NotImplementedException();
        }

        public bool SSR_SetHoliday(int dwMachineNumber, int HolidayID, int BeginMonth, int BeginDay, int EndMonth, int EndDay, int TimeZoneID)
        {
            throw new NotImplementedException();
        }

        public bool GetPlatform(int dwMachineNumber, ref string Platform)
        {
            throw new NotImplementedException();
        }

        public bool SSR_SetUserSMS(int dwMachineNumber, string dwEnrollNumber, int SMSID)
        {
            throw new NotImplementedException();
        }

        public bool SSR_DeleteUserSMS(int dwMachineNumber, string dwEnrollNumber, int SMSID)
        {
            throw new NotImplementedException();
        }

        public bool IsTFTMachine(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool SSR_EnableUser(int dwMachineNumber, string dwEnrollNumber, bool bFlag)
        {
            throw new NotImplementedException();
        }

        public bool SSR_SetUserTmpExt(int dwMachineNumber, int IsDeleted, string dwEnrollNumber, int dwFingerIndex, ref byte TmpData)
        {
            throw new NotImplementedException();
        }

        public bool SSR_DelUserTmpExt(int dwMachineNumber, string dwEnrollNumber, int dwFingerIndex)
        {
            throw new NotImplementedException();
        }

        public bool SSR_DeleteEnrollDataExt(int dwMachineNumber, string dwEnrollNumber, int dwBackupNumber)
        {
            throw new NotImplementedException();
        }

        public bool SSR_GetWorkCode(int AWorkCode, out string Name)
        {
            throw new NotImplementedException();
        }

        public bool SSR_SetWorkCode(int AWorkCode, string Name)
        {
            throw new NotImplementedException();
        }

        public bool SSR_DeleteWorkCode(int PIN)
        {
            throw new NotImplementedException();
        }

        public bool SSR_ClearWorkCode()
        {
            throw new NotImplementedException();
        }

        public bool SSR_GetSuperLogData(int MachineNumber, out int Number, out string Admin, out string User, out int Manipulation, out string Time, out int Params1, out int Params2, out int Params3)
        {
            throw new NotImplementedException();
        }

        public bool SSR_SetShortkey(int ShortKeyID, int ShortKeyFun, int StateCode, string StateName, int StateAutoChange, string StateAutoChangeTime)
        {
            throw new NotImplementedException();
        }

        public bool SSR_GetShortkey(int ShortKeyID, ref int ShortKeyFun, ref int StateCode, ref string StateName, ref int AutoChange, ref string AutoChangeTime)
        {
            throw new NotImplementedException();
        }

        public bool SSR_GetGeneralLogDataEx(int dwMachineNumber, out string dwEnrollNumber, out int dwVerifyMode, out int dwInOutMode, out int dwYear, out int dwMonth, out int dwDay, out int dwHour, out int dwMinute, out int dwSecond, out string dwWorkCode)
        {
            throw new NotImplementedException();
        }

        public bool SSR_SetWorkCodeExBatch(int dwMachineNumber, string Datas)
        {
            throw new NotImplementedException();
        }

        public bool SSR_GetWorkCodeExBatch(int dwMachineNumber, out string Buffer, int BufferSize)
        {
            throw new NotImplementedException();
        }

        public bool SSR_GetWorkCodeExByID(int dwMachineNumber, int WorkCodeID, out string WorkCodeNum, out string Name)
        {
            throw new NotImplementedException();
        }

        public bool SSR_GetWorkCodeIDByName(int dwMachineNumber, string workcodeName, out int WorkCodeID)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Unused Codes


        public void ObjCZKEM_OnFinger()
        {
            throw new NotImplementedException();
        }

        public void ObjCZKEM_OnKeyPress(int key)
        {
            throw new NotImplementedException();
        }

        public void ObjCZKEM_OnGeneralEvent(string dataString)
        {
            throw new NotImplementedException();
        }

        public void ObjCZKEM_OnEnrollFinger(int EnrollNumber, int FingerIndex, int ActionResult, int TemplateLength)
        {
            throw new NotImplementedException();
        }

        public bool ClearAdministrators(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool DeleteEnrollData(int dwMachineNumber, int dwEnrollNumber, int dwEMachineNumber, int dwBackupNumber)
        {
            throw new NotImplementedException();
        }

        public bool ReadAllSLogData(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool EnableUser(int dwMachineNumber, int dwEnrollNumber, int dwEMachineNumber, int dwBackupNumber, bool bFlag)
        {
            throw new NotImplementedException();
        }

        public bool EnableDevice(int dwMachineNumber, bool bFlag)
        {
            throw new NotImplementedException();
        }

        public bool SetDeviceInfo(int dwMachineNumber, int dwInfo, int dwValue)
        {
            throw new NotImplementedException();
        }

        public void PowerOnAllDevice()
        {
            throw new NotImplementedException();
        }

        public bool PowerOffDevice(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool ModifyPrivilege(int dwMachineNumber, int dwEnrollNumber, int dwEMachineNumber, int dwBackupNumber, int dwMachinePrivilege)
        {
            throw new NotImplementedException();
        }

        public void GetLastError(ref int dwErrorCode)
        {
            throw new NotImplementedException();
        }

        public bool GetSuperLogData(int dwMachineNumber, ref int dwTMachineNumber, ref int dwSEnrollNumber, ref int Params4, ref int Params1, ref int Params2, ref int dwManipulation, ref int Params3, ref int dwYear, ref int dwMonth, ref int dwDay, ref int dwHour, ref int dwMinute)
        {
            throw new NotImplementedException();
        }

        public bool GetAllSLogData(int dwMachineNumber, ref int dwTMachineNumber, ref int dwSEnrollNumber, ref int Params4, ref int Params1, ref int Params2, ref int dwManipulation, ref int Params3, ref int dwYear, ref int dwMonth, ref int dwDay, ref int dwHour, ref int dwMinute)
        {
            throw new NotImplementedException();
        }

        public bool GetAllGLogData(int dwMachineNumber, ref int dwTMachineNumber, ref int dwEnrollNumber, ref int dwEMachineNumber, ref int dwVerifyMode, ref int dwInOutMode, ref int dwYear, ref int dwMonth, ref int dwDay, ref int dwHour, ref int dwMinute)
        {
            throw new NotImplementedException();
        }

        public void ConvertPassword(int dwSrcPSW, ref int dwDestPSW, int dwLength)
        {
            throw new NotImplementedException();
        }

        public bool ClearKeeperData(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public int GetFPTempLength(ref byte dwEnrollData)
        {
            throw new NotImplementedException();
        }

        public bool Connect_Com(int ComPort, int MachineNumber, int BaudRate)
        {
            throw new NotImplementedException();
        }

        public bool SetDeviceIP(int dwMachineNumber, string IPAddr)
        {
            throw new NotImplementedException();
        }

        public bool GetUserTmp(int dwMachineNumber, int dwEnrollNumber, int dwFingerIndex, ref byte TmpData, ref int TmpLength)
        {
            throw new NotImplementedException();
        }

        public bool SetUserTmp(int dwMachineNumber, int dwEnrollNumber, int dwFingerIndex, ref byte TmpData)
        {
            throw new NotImplementedException();
        }

        public bool DelUserTmp(int dwMachineNumber, int dwEnrollNumber, int dwFingerIndex)
        {
            throw new NotImplementedException();
        }

        public bool FPTempConvert(ref byte TmpData1, ref byte TmpData2, ref int Size)
        {
            throw new NotImplementedException();
        }

        public bool SetCommPassword(int CommKey)
        {
            throw new NotImplementedException();
        }

        public bool GetUserGroup(int dwMachineNumber, int dwEnrollNumber, ref int UserGrp)
        {
            throw new NotImplementedException();
        }

        public bool SetUserGroup(int dwMachineNumber, int dwEnrollNumber, int UserGrp)
        {
            throw new NotImplementedException();
        }

        public bool GetTZInfo(int dwMachineNumber, int TZIndex, ref string TZ)
        {
            throw new NotImplementedException();
        }

        public bool SetTZInfo(int dwMachineNumber, int TZIndex, string TZ)
        {
            throw new NotImplementedException();
        }

        public bool GetUnlockGroups(int dwMachineNumber, ref string Grps)
        {
            throw new NotImplementedException();
        }

        public bool SetUnlockGroups(int dwMachineNumber, string Grps)
        {
            throw new NotImplementedException();
        }

        public bool GetGroupTZs(int dwMachineNumber, int GroupIndex, ref int TZs)
        {
            throw new NotImplementedException();
        }

        public bool SetGroupTZs(int dwMachineNumber, int GroupIndex, ref int TZs)
        {
            throw new NotImplementedException();
        }

        public bool GetUserTZs(int dwMachineNumber, int dwEnrollNumber, ref int TZs)
        {
            throw new NotImplementedException();
        }

        public bool SetUserTZs(int dwMachineNumber, int dwEnrollNumber, ref int TZs)
        {
            throw new NotImplementedException();
        }

        public bool ACUnlock(int dwMachineNumber, int Delay)
        {
            throw new NotImplementedException();
        }

        public bool GetACFun(ref int ACFun)
        {
            throw new NotImplementedException();
        }

        public bool GetGeneralLogDataStr(int dwMachineNumber, ref int dwEnrollNumber, ref int dwVerifyMode, ref int dwInOutMode, ref string TimeStr)
        {
            throw new NotImplementedException();
        }

        public bool GetUserTmpStr(int dwMachineNumber, int dwEnrollNumber, int dwFingerIndex, ref string TmpData, ref int TmpLength)
        {
            throw new NotImplementedException();
        }

        public bool SetUserTmpStr(int dwMachineNumber, int dwEnrollNumber, int dwFingerIndex, string TmpData)
        {
            throw new NotImplementedException();
        }

        public bool SetEnrollDataStr(int dwMachineNumber, int dwEnrollNumber, int dwEMachineNumber, int dwBackupNumber, int dwMachinePrivilege, string dwEnrollData, int dwPassWord)
        {
            throw new NotImplementedException();
        }

        public bool GetGroupTZStr(int dwMachineNumber, int GroupIndex, ref string TZs)
        {
            throw new NotImplementedException();
        }

        public bool SetGroupTZStr(int dwMachineNumber, int GroupIndex, string TZs)
        {
            throw new NotImplementedException();
        }

        public bool GetUserTZStr(int dwMachineNumber, int dwEnrollNumber, ref string TZs)
        {
            throw new NotImplementedException();
        }

        public bool SetUserTZStr(int dwMachineNumber, int dwEnrollNumber, string TZs)
        {
            throw new NotImplementedException();
        }

        public bool FPTempConvertStr(string TmpData1, ref string TmpData2, ref int Size)
        {
            throw new NotImplementedException();
        }

        public int GetFPTempLengthStr(string dwEnrollData)
        {
            throw new NotImplementedException();
        }

        public bool GetUserInfoByPIN2(int dwMachineNumber, ref string Name, ref string Password, ref int Privilege, ref bool Enabled)
        {
            throw new NotImplementedException();
        }

        public bool GetUserInfoByCard(int dwMachineNumber, ref string Name, ref string Password, ref int Privilege, ref bool Enabled)
        {
            throw new NotImplementedException();
        }

        public bool CaptureImage(bool FullImage, ref int Width, ref int Height, ref byte Image, string ImageFile)
        {
            throw new NotImplementedException();
        }

        public bool StartEnroll(int UserID, int FingerID)
        {
            throw new NotImplementedException();
        }

        public bool StartVerify(int UserID, int FingerID)
        {
            throw new NotImplementedException();
        }

        public bool StartIdentify()
        {
            throw new NotImplementedException();
        }

        public bool CancelOperation()
        {
            throw new NotImplementedException();
        }

        public bool QueryState(ref int State)
        {
            throw new NotImplementedException();
        }

        public bool BackupData(string DataFile)
        {
            throw new NotImplementedException();
        }

        public bool RestoreData(string DataFile)
        {
            throw new NotImplementedException();
        }

        public bool ClearLCD()
        {
            throw new NotImplementedException();
        }

        public bool Beep(int DelayMS)
        {
            throw new NotImplementedException();
        }

        public bool PlayVoice(int Position, int Length)
        {
            throw new NotImplementedException();
        }

        public bool EnableClock(int Enabled)
        {
            throw new NotImplementedException();
        }

        public bool GetUserIDByPIN2(int PIN2, ref int UserID)
        {
            throw new NotImplementedException();
        }

        public bool GetPIN2(int UserID, ref int PIN2)
        {
            throw new NotImplementedException();
        }

        public bool FPTempConvertNew(ref byte TmpData1, ref byte TmpData2, ref int Size)
        {
            throw new NotImplementedException();
        }

        public bool FPTempConvertNewStr(string TmpData1, ref string TmpData2, ref int Size)
        {
            throw new NotImplementedException();
        }

        public bool DisableDeviceWithTimeOut(int dwMachineNumber, int TimeOutSec)
        {
            throw new NotImplementedException();
        }

        public bool SetDeviceTime2(int dwMachineNumber, int dwYear, int dwMonth, int dwDay, int dwHour, int dwMinute, int dwSecond)
        {
            throw new NotImplementedException();
        }

        public bool ClearSLog(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool RestartDevice(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool SetDeviceMAC(int dwMachineNumber, string sMAC)
        {
            throw new NotImplementedException();
        }

        public bool SetWiegandFmt(int dwMachineNumber, string sWiegandFmt)
        {
            throw new NotImplementedException();
        }

        public bool ClearSMS(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool GetSMS(int dwMachineNumber, int ID, ref int Tag, ref int ValidMinutes, ref string StartTime, ref string Content)
        {
            throw new NotImplementedException();
        }

        public bool SetSMS(int dwMachineNumber, int ID, int Tag, int ValidMinutes, string StartTime, string Content)
        {
            throw new NotImplementedException();
        }

        public bool DeleteSMS(int dwMachineNumber, int ID)
        {
            throw new NotImplementedException();
        }

        public bool SetUserSMS(int dwMachineNumber, int dwEnrollNumber, int SMSID)
        {
            throw new NotImplementedException();
        }

        public bool DeleteUserSMS(int dwMachineNumber, int dwEnrollNumber, int SMSID)
        {
            throw new NotImplementedException();
        }

        public bool GetCardFun(int dwMachineNumber, ref int CardFun)
        {
            throw new NotImplementedException();
        }

        public bool ClearUserSMS(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool SetDeviceCommPwd(int dwMachineNumber, int CommKey)
        {
            throw new NotImplementedException();
        }

        public bool GetDoorState(int MachineNumber, ref int State)
        {
            throw new NotImplementedException();
        }

        public bool GetSensorSN(int dwMachineNumber, ref string SensorSN)
        {
            throw new NotImplementedException();
        }

        public bool ReadCustData(int dwMachineNumber, ref string CustData)
        {
            throw new NotImplementedException();
        }

        public bool WriteCustData(int dwMachineNumber, string CustData)
        {
            throw new NotImplementedException();
        }

        public bool GetDataFile(int dwMachineNumber, int DataFlag, string FileName)
        {
            throw new NotImplementedException();
        }

        public bool WriteCard(int dwMachineNumber, int dwEnrollNumber, int dwFingerIndex1, ref byte TmpData1, int dwFingerIndex2, ref byte TmpData2, int dwFingerIndex3, ref byte TmpData3, int dwFingerIndex4, ref byte TmpData4)
        {
            throw new NotImplementedException();
        }

        public bool EmptyCard(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool GetDeviceStrInfo(int dwMachineNumber, int dwInfo, out string Value)
        {
            throw new NotImplementedException();
        }

        public bool GetSysOption(int dwMachineNumber, string Option, out string Value)
        {
            throw new NotImplementedException();
        }

        public bool SetUserInfoEx(int dwMachineNumber, int dwEnrollNumber, int VerifyStyle, ref byte Reserved)
        {
            return objCZKEM.SetUserInfoEx(dwMachineNumber, dwEnrollNumber, VerifyStyle, ref Reserved);
        }

        public bool GetUserInfoEx(int dwMachineNumber, int dwEnrollNumber, out int VerifyStyle, out byte Reserved)
        {
            throw new NotImplementedException();
        }

        public bool DeleteUserInfoEx(int dwMachineNumber, int dwEnrollNumber)
        {
            throw new NotImplementedException();
        }

        public bool SetWorkCode(int WorkCodeID, int AWorkCode)
        {
            throw new NotImplementedException();
        }

        public bool GetWorkCode(int WorkCodeID, out int AWorkCode)
        {
            throw new NotImplementedException();
        }

        public bool DeleteWorkCode(int WorkCodeID)
        {
            throw new NotImplementedException();
        }

        public bool ClearWorkCode()
        {
            throw new NotImplementedException();
        }

        public bool ReadAttRule(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool ReadDPTInfo(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool SaveTheDataToFile(int dwMachineNumber, string TheFilePath, int FileFlag)
        {
            throw new NotImplementedException();
        }

        public bool ReadTurnInfo(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool ReadAOptions(string AOption, out string AValue)
        {
            throw new NotImplementedException();
        }

        public bool ReadRTLog(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool GetRTLog(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool GetHIDEventCardNumAsStr(out string strHIDEventCardNum)
        {
            return objCZKEM.GetHIDEventCardNumAsStr(out strHIDEventCardNum);
        }

        public bool GetStrCardNumber(out string ACardNumber)
        {
            throw new NotImplementedException();
        }

        public bool CancelBatchUpdate(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool SetSysOption(int dwMachineNumber, string Option, string Value)
        {
            throw new NotImplementedException();
        }

        public bool Connect_Modem(int ComPort, int MachineNumber, int BaudRate, string Telephone)
        {
            throw new NotImplementedException();
        }

        public bool UseGroupTimeZone()
        {
            throw new NotImplementedException();
        }

        public bool SetHoliday(int dwMachineNumber, string Holiday)
        {
            throw new NotImplementedException();
        }

        public bool GetHoliday(int dwMachineNumber, ref string Holiday)
        {
            throw new NotImplementedException();
        }

        public bool SetDaylight(int dwMachineNumber, int Support, string BeginTime, string EndTime)
        {
            throw new NotImplementedException();
        }

        public bool GetDaylight(int dwMachineNumber, ref int Support, ref string BeginTime, ref string EndTime)
        {
            throw new NotImplementedException();
        }

        public bool SendCMDMsg(int dwMachineNumber, int Param1, int Param2)
        {
            throw new NotImplementedException();
        }

        public bool SendFile(int dwMachineNumber, string FileName)
        {
            throw new NotImplementedException();
        }

        public bool SetLanguageByID(int dwMachineNumber, int LanguageID, string Language)
        {
            throw new NotImplementedException();
        }

        public bool ReadFile(int dwMachineNumber, string FileName, string FilePath)
        {
            throw new NotImplementedException();
        }

        public bool SetLastCount(int count)
        {
            throw new NotImplementedException();
        }

        public bool SetCustomizeAttState(int dwMachineNumber, int StateID, int NewState)
        {
            throw new NotImplementedException();
        }

        public bool DelCustomizeAttState(int dwMachineNumber, int StateID)
        {
            throw new NotImplementedException();
        }

        public bool EnableCustomizeAttState(int dwMachineNumber, int StateID, int Enable)
        {
            throw new NotImplementedException();
        }

        public bool SetCustomizeVoice(int dwMachineNumber, int VoiceID, string FileName)
        {
            throw new NotImplementedException();
        }

        public bool DelCustomizeVoice(int dwMachineNumber, int VoiceID)
        {
            throw new NotImplementedException();
        }

        public bool EnableCustomizeVoice(int dwMachineNumber, int VoiceID, int Enable)
        {
            throw new NotImplementedException();
        }

        public bool Connect_USB(int MachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool GetSuperLogData2(int dwMachineNumber, ref int dwTMachineNumber, ref int dwSEnrollNumber, ref int Params4, ref int Params1, ref int Params2, ref int dwManipulation, ref int Params3, ref int dwYear, ref int dwMonth, ref int dwDay, ref int dwHour, ref int dwMinute, ref int dwSecs)
        {
            throw new NotImplementedException();
        }

        public bool GetUserFace(int dwMachineNumber, string dwEnrollNumber, int dwFaceIndex, ref byte TmpData, ref int TmpLength)
        {
            throw new NotImplementedException();
        }

        public bool SetUserFace(int dwMachineNumber, string dwEnrollNumber, int dwFaceIndex, ref byte TmpData, int TmpLength)
        {
            throw new NotImplementedException();
        }

        public bool DelUserFace(int dwMachineNumber, string dwEnrollNumber, int dwFaceIndex)
        {
            throw new NotImplementedException();
        }

        public bool GetUserFaceStr(int dwMachineNumber, string dwEnrollNumber, int dwFaceIndex, ref string TmpData, ref int TmpLength)
        {
            throw new NotImplementedException();
        }

        public bool SetUserFaceStr(int dwMachineNumber, string dwEnrollNumber, int dwFaceIndex, string TmpData, int TmpLength)
        {
            throw new NotImplementedException();
        }

        public bool GetUserTmpEx(int dwMachineNumber, string dwEnrollNumber, int dwFingerIndex, out int Flag, out byte TmpData, out int TmpLength)
        {
            throw new NotImplementedException();
        }

        public bool SetUserTmpEx(int dwMachineNumber, string dwEnrollNumber, int dwFingerIndex, int Flag, ref byte TmpData)
        {
            throw new NotImplementedException();
        }

        public bool SetUserTmpExStr(int dwMachineNumber, string dwEnrollNumber, int dwFingerIndex, int Flag, string TmpData)
        {
            throw new NotImplementedException();
        }

        public bool MergeTemplate(IntPtr Templates, int FingerCount, ref byte TemplateDest, ref int FingerSize)
        {
            throw new NotImplementedException();
        }

        public bool SplitTemplate(ref byte Template, IntPtr Templates, ref int FingerCount, ref int FingerSize)
        {
            throw new NotImplementedException();
        }

        public bool ReadUserAllTemplate(int dwMachineNumber, string dwEnrollNumber)
        {
            throw new NotImplementedException();
        }

        public bool UpdateFile(string FileName)
        {
            throw new NotImplementedException();
        }

        public bool ReadLastestLogData(int dwMachineNumber, int NewLog, int dwYear, int dwMonth, int dwDay, int dwHour, int dwMinute, int dwSecond)
        {
            throw new NotImplementedException();
        }

        public bool SetOptionCommPwd(int dwMachineNumber, string CommKey)
        {
            throw new NotImplementedException();
        }

        public bool ReadSuperLogDataEx(int dwMachineNumber, int dwYear_S, int dwMonth_S, int dwDay_S, int dwHour_S, int dwMinute_S, int dwSecond_S, int dwYear_E, int dwMonth_E, int dwDay_E, int dwHour_E, int dwMinute_E, int dwSecond_E, int dwLogIndex)
        {
            throw new NotImplementedException();
        }

        public bool GetSuperLogDataEx(int dwMachineNumber, ref string EnrollNumber, ref int Params4, ref int Params1, ref int Params2, ref int dwManipulation, ref int Params3, ref int dwYear, ref int dwMonth, ref int dwDay, ref int dwHour, ref int dwMinute, ref int dwSecond)
        {
            throw new NotImplementedException();
        }

        public bool GetPhotoByName(int dwMachineNumber, string PhotoName, out byte PhotoData, out int PhotoLength)
        {
            throw new NotImplementedException();
        }

        public bool GetPhotoNamesByTime(int dwMachineNumber, int iFlag, string sTime, string eTime, out string AllPhotoName)
        {
            throw new NotImplementedException();
        }

        public bool ClearPhotoByTime(int dwMachineNumber, int iFlag, string sTime, string eTime)
        {
            throw new NotImplementedException();
        }

        public bool GetPhotoCount(int dwMachineNumber, out int count, int iFlag)
        {
            throw new NotImplementedException();
        }

        public bool ClearDataEx(int dwMachineNumber, string TableName)
        {
            throw new NotImplementedException();
        }

        public bool GetDataFileEx(int dwMachineNumber, string SourceFileName, string DestFileName)
        {
            throw new NotImplementedException();
        }

        public bool SSR_SetDeviceData(int dwMachineNumber, string TableName, string Datas, string Options)
        {
            throw new NotImplementedException();
        }

        public bool SSR_GetDeviceData(int dwMachineNumber, out string Buffer, int BufferSize, string TableName, string FiledNames, string Filter, string Options)
        {
            throw new NotImplementedException();
        }

        public bool UpdateLogo(int dwMachineNumber, string FileName)
        {
            return objCZKEM.UpdateLogo(dwMachineNumber, FileName);
        }

        public bool SetCommuTimeOut(int timeOut)
        {
            throw new NotImplementedException();
        }

        public bool SendFileByType(int dwMachineNumber, string FileName, int iType)
        {
            throw new NotImplementedException();
        }

        public bool SetCommProType(int proType)
        {
            throw new NotImplementedException();
        }

        public bool SetCompatOldFirmware(int nCompatOkdFirmware)
        {
            throw new NotImplementedException();
        }

        public bool Connect_P4P(string uid)
        {
            throw new NotImplementedException();
        }

        public bool SetDeviceTableData(int dwMachineNumber, string TableName, string Datas, string Options, out int count)
        {
            throw new NotImplementedException();
        }

        public bool GetConnectStatus(ref int dwErrorCode)
        {
            throw new NotImplementedException();
        }

        public bool SetManufacturerData(int dwMachineNumber, string Name, string Value)
        {
            throw new NotImplementedException();
        }

        public int GetDeviceStatusEx(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public void CancelByUser()
        {
            throw new NotImplementedException();
        }

        public int SSR_GetDeviceDataCount(string TableName, string Filter, string Options)
        {
            throw new NotImplementedException();
        }

        public bool SSR_DeleteDeviceData(int dwMachineNumber, string TableName, string Datas, string Options)
        {
            throw new NotImplementedException();
        }

        public bool ReadTimeGLogData(int dwMachineNumber, string sTime, string eTime)
        {
            throw new NotImplementedException();
        }

        public bool DeleteAttlogBetweenTheDate(int dwMachineNumber, string sTime, string eTime)
        {
            throw new NotImplementedException();
        }

        public bool DeleteAttlogByTime(int dwMachineNumber, string sTime)
        {
            throw new NotImplementedException();
        }

        public bool ReadNewGLogData(int dwMachineNumber)
        {
            return objCZKEM.ReadNewGLogData(dwMachineNumber);
        }

        public bool IsNewFirmwareMachine(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool UploadUserPhoto(int dwMachineNumber, string FileName)
        {
            throw new NotImplementedException();
        }

        public bool DownloadUserPhoto(int dwMachineNumber, string FileName, string FilePath)
        {
            throw new NotImplementedException();
        }

        public bool DeleteUserPhoto(int dwMachineNumber, string FileName)
        {
            throw new NotImplementedException();
        }

        public bool GetAllUserPhoto(int dwMachineNumber, string dlDir)
        {
            throw new NotImplementedException();
        }

        public bool SetBellSchDataEx(int dwMachineNumber, int weekDay, int Index, int Enable, int Hour, int min, int voice, int way, int InerBellDelay, int ExtBellDelay)
        {
            throw new NotImplementedException();
        }

        public bool GetBellSchDataEx(int dwMachineNumber, int weekDay, int Index, out int Enable, out int Hour, out int min, out int voice, out int way, out int InerBellDelay, out int ExtBellDelay)
        {
            throw new NotImplementedException();
        }

        public bool GetDayBellSchCount(int dwMachineNumber, out int DayBellCnt)
        {
            throw new NotImplementedException();
        }

        public bool GetMaxBellIDInBellSchData(int dwMachineNumber, out int MaxBellID)
        {
            throw new NotImplementedException();
        }

        public bool ReadAllBellSchData(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool GetEachBellInfo(int dwMachineNumber, out int weekDay, out int Index, out int Enable, out int Hour, out int min, out int voice, out int way, out int InerBellDelay, out int ExtBellDelay)
        {
            throw new NotImplementedException();
        }

        public bool SetUserValidDate(int dwMachineNumber, string UserID, int Expires, int ValidCount, string StartDate, string EndDate)
        {
            throw new NotImplementedException();
        }

        public bool GetUserValidDate(int dwMachineNumber, string UserID, out int Expires, out int ValidCount, out string StartDate, out string EndDate)
        {
            throw new NotImplementedException();
        }

        public bool SetUserValidDateBatch(int dwMachineNumber, string Datas)
        {
            throw new NotImplementedException();
        }

        public bool GetUserValidDateBatch(int dwMachineNumber, out string Buffer, int BufferSize)
        {
            throw new NotImplementedException();
        }

        public bool SetUserVerifyStyle(int dwMachineNumber, string dwEnrollNumber, int VerifyStyle, ref byte Reserved)
        {
            throw new NotImplementedException();
        }

        public bool GetUserVerifyStyle(int dwMachineNumber, string dwEnrollNumber, out int VerifyStyle, out byte Reserved)
        {
            throw new NotImplementedException();
        }

        public bool SetUserVerifyStyleBatch(int dwMachineNumber, string Datas, ref byte Reserved)
        {
            throw new NotImplementedException();
        }

        public bool GetUserVerifyStyleBatch(int dwMachineNumber, out string Buffer, int BufferSize, out byte Reserved)
        {
            throw new NotImplementedException();
        }

        public bool GetDeviceFirmwareVersion(int dwMachineNumber, ref string strVersion)
        {
            throw new NotImplementedException();
        }

        public bool SendFileEx(int dwMachineNumber, string FileName, string FilePath)
        {
            throw new NotImplementedException();
        }

        public bool UploadTheme(int dwMachineNumber, string FileName, string InDevName)
        {
            throw new NotImplementedException();
        }

        public bool UploadPicture(int dwMachineNumber, string FileName, string InDevName)
        {
            throw new NotImplementedException();
        }

        public bool DeletePicture(int dwMachineNumber, string FileName)
        {
            throw new NotImplementedException();
        }

        public bool DownloadPicture(int dwMachineNumber, string FileName, string FilePath)
        {
            throw new NotImplementedException();
        }

        public bool TurnOffAlarm(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool CloseAlarm(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        public bool SSR_SetWorkCodeEx(int dwMachineNumber, string WorkCodeNum, string Name)
        {
            throw new NotImplementedException();
        }

        public bool SSR_GetWorkCodeEx(int dwMachineNumber, string WorkCodeNum, out string Name)
        {
            throw new NotImplementedException();
        }

        public bool SSR_DeleteWorkCodeEx(int dwMachineNumber, string WorkCodeNum)
        {
            throw new NotImplementedException();
        }

        public bool SetEventMode(int nType)
        {
            throw new NotImplementedException();
        }

        public bool GetAllSFIDName(int dwMachineNumber, out string ShortkeyIDName, int BufferSize1, out string FunctionIDName, int BufferSize2)
        {
            throw new NotImplementedException();
        }

        public bool SetShortkey(int dwMachineNumber, int ShortKeyID, string ShortKeyName, string FunctionName, int ShortKeyFun, int StateCode, string StateName, string Description, int StateAutoChange, string StateAutoChangeTime)
        {
            throw new NotImplementedException();
        }

        public bool GetShortkey(int dwMachineNumber, int ShortKeyID, ref string ShortKeyName, ref string FunctionName, ref int ShortKeyFun, ref int StateCode, ref string StateName, ref string Description, ref int AutoChange, ref string AutoChangeTime)
        {
            throw new NotImplementedException();
        }

        public bool GetAllAppFun(int dwMachineNumber, out string AppName, out string FunofAppName)
        {
            throw new NotImplementedException();
        }

        public bool GetAllRole(int dwMachineNumber, out string RoleName)
        {
            throw new NotImplementedException();
        }

        public bool GetAppOfRole(int dwMachineNumber, int Permission, out string AppName)
        {
            throw new NotImplementedException();
        }

        public bool GetFunOfRole(int dwMachineNumber, int Permission, out string FunName)
        {
            throw new NotImplementedException();
        }

        public bool SetPermOfAppFun(int dwMachineNumber, int Permission, string AppName, string FunName)
        {
            throw new NotImplementedException();
        }

        public bool DeletePermOfAppFun(int dwMachineNumber, int Permission, string AppName, string FunName)
        {
            throw new NotImplementedException();
        }

        public bool IsUserDefRoleEnable(int dwMachineNumber, int Permission, out bool Enable)
        {
            throw new NotImplementedException();
        }

        public bool SearchDevice(string commType, string address, out string DevBuffer, int DevBufferSize)
        {
            throw new NotImplementedException();
        }

        public bool SetUserIDCardInfo(int dwMachineNumber, string strEnrollNumber, ref byte IDCardData, int DataLen)
        {
            return objCZKEM.SetUserIDCardInfo(1, "566", ref IDCardData, 8);
        }

        public bool GetUserIDCardInfo(int dwMachineNumber, string strEnrollNumber, out byte IDCardData, ref int DataLen)
        {
            throw new NotImplementedException();
        }

        public bool DelUserIDCardInfo(int dwMachineNumber, string strEnrollNumber)
        {
            throw new NotImplementedException();
        }

        public bool GetPhotoByNameToFile(int dwMachineNumber, string PhotoName, string LocalFileName)
        {
            throw new NotImplementedException();
        }

        public bool SendUserFacePhoto(int dwMachineNumber, string FileName)
        {
            throw new NotImplementedException();
        }

        public bool GetUserFacePhotoNames(int dwMachineNumber, out string AllPhotoName)
        {
            throw new NotImplementedException();
        }

        public bool GetUserFacePhotoCount(int dwMachineNumber, out int count)
        {
            throw new NotImplementedException();
        }

        public bool GetUserFacePhotoByName(int dwMachineNumber, string PhotoName, out byte PhotoData, out int PhotoLength)
        {
            throw new NotImplementedException();
        }

        public bool SetUserInfoPR(int dwMachineNumber, bool IsSameUser, string dwEnrollNumber, string Name, string Remark, string Rank, string Photo)
        {
            throw new NotImplementedException();
        }

        public bool ClearDram(int dwMachineNumber)
        {
            throw new NotImplementedException();
        }

        //public bool SSR_GetGeneralLogDataWithMask(int dwMachineNumber, ref string dwEnrollNumber, ref int dwVerifyMode, ref int dwInOutMode, ref int dwYear, ref int dwMonth, ref int dwDay, ref int dwHour, ref int dwMinute, ref int dwSecond, ref int dwWorkCode, ref int dwMask, ref string dwTemperature)
        //{
        //    throw new NotImplementedException();
        //}

        public bool ReadMark { get => objCZKEM.ReadMark; set => objCZKEM.ReadMark = value; }
        public int CommPort { get => objCZKEM.CommPort; set => objCZKEM.CommPort = value; }
        public int ConvertBIG5 { get => objCZKEM.ConvertBIG5; set => objCZKEM.ConvertBIG5 = value; }
        public int BASE64 { get => objCZKEM.BASE64; set => objCZKEM.BASE64 = value; }
        public uint PIN2 { get => objCZKEM.PIN2; set => objCZKEM.PIN2 = value; }

        public int AccGroup { get => Get_AccGroup(); set => Set_AccGroup(value); }
        public int Get_AccGroup()
        {
            return objCZKEM.AccGroup;
        }

        public void Set_AccGroup(int value)
        {
            objCZKEM.AccGroup = value;
        }

        //public int AccTimeZones { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public void set_AccTimeZones(int Index, int pVal)
        {
            throw new NotImplementedException();
        }
        public int get_AccTimeZones(int Index)
        {
            throw new NotImplementedException();
        }

        //public int CardNumber { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public void set_CardNumber(int Index, int pVal)
        {
            throw new NotImplementedException();
        }
        public int get_CardNumber(int Index)
        {
            throw new NotImplementedException();
        }

        //public string STR_CardNumber { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public void set_STR_CardNumber(int Index, string pVal)
        {
            throw new NotImplementedException();
        }
        public string get_STR_CardNumber(int Index)
        {
            throw new NotImplementedException();
        }
        public int SSRPin => objCZKEM.SSRPin;

        public int PullMode { get => objCZKEM.PullMode; set => objCZKEM.PullMode = value; }

        public int MaxP4PConnect => objCZKEM.MaxP4PConnect;



        #endregion
    }
}
