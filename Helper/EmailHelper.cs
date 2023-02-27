using Automated_Attendance_System.Controller;
using Automated_Attendance_System.Entity.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace Automated_Attendance_System.Helper
{
    /// <summary>
    /// Only contains codes to send various emails
    /// </summary>
    public class EmailHelper
    {

        /// <summary>
        /// Static variables to reduce instantiation in every thread.
        /// The objects are only used for reading. Thus, does not require to be thread-safe.
        /// </summary>
        private static EmailController _emailController = new EmailController();

        private static EmailDTO _primaryEmail = _emailController.loadPrimaryEmail();
        private static EmailDTO _backupEmail = _emailController.loadBackupEmail();


        /// <summary>
        /// Dev Email List generator.
        /// Hardcoded emails given.
        /// </summary>
        /// <returns>
        /// List of Dev Email addresses
        /// </returns>
        public List<string> GetDevMails()
        {
            List<string> devMails = new List<string>
            {
                "shopon@bssitbd.com",
                "anikrahman70945@gmail.com"
            };

            return devMails;
        }

        /// <summary>
        /// Send general emails.
        /// Does not contain body, but contains meesage about error or success.
        /// </summary>
        /// <param name="status"> Supposed for error or general mail flag. Currently not in use.</param>
        /// <param name="subject"> Subject of the Email.</param>
        /// <param name="msg"> Message of the email.</param>
        /// <returns>
        /// True or false depending on the email being sent.
        /// </returns>
        public bool SendEmail(string status, string subject, string msg)
        {
            string _userName = _primaryEmail.EmailAddress.ToLower();
            string _password = _primaryEmail.Password;
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(_userName, _password);
            MailAddress from = new MailAddress(_userName, "BSS");
            MailAddress to = new MailAddress("farhan.bssit.bd@gmail.com");
            MailMessage message = new MailMessage(from, to);
            foreach (string mailAddress in GetDevMails())
            {
                if (!string.IsNullOrEmpty(mailAddress) && mailAddress.Trim() != "")
                {
                    message.CC.Add(mailAddress);
                }
            }

            message.Subject = subject;
            message.IsBodyHtml = true;
            message.Body = EmailBody(status: status, message: msg);
            try
            {
                client.Send(message);
                return true;
            }

            catch (Exception ex)
            {
                string err = ex.ToString();
            }

            return true;
        }

        public bool SendEmailBackup(string status, string subject, string msg)
        {
            string _userName = _backupEmail.EmailAddress.ToLower();
            string _password = _backupEmail.Password;
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(_userName, _password);
            MailAddress from = new MailAddress(_userName, "BSS");
            MailAddress to = new MailAddress("farhan.bssit.bd@gmail.com");
            MailMessage message = new MailMessage(from, to);
            foreach (string mailAddress in GetDevMails())
            {
                if (!string.IsNullOrEmpty(mailAddress) && mailAddress.Trim() != "")
                {
                    message.CC.Add(mailAddress);
                }
            }

            message.Subject = subject;
            message.IsBodyHtml = true;
            message.Body = EmailBody(status: status, message: msg);
            try
            {
                client.Send(message);
                return true;
            }

            catch (Exception ex)
            {
                string err = ex.ToString();
            }

            return true;
        }

        private string EmailBody(string status, string message)
        {
            #region CSS
            string css = string.Empty;
            css += "<style>";
            css += ".wrapper{ display: flex; align - content: center; justify-content: center; margin: 10px;}";

            css += ".card { box-shadow: 0 4px 8px 0 rgba(0, 0, 0, 0.2); transition: 0.3s; border-radius: 5px; display: flex; flex-wrap: wrap;justify-content: center;}";

            css += ".card-header-success { display: flex; justify-content: center;align - content: center;width: 100 %;color: #001a00;background - color: #99ff66; border-radius: 5px 5px 0 0;}";

            css += ".card-header-error{ display: flex; justify-content: center; align-content: center; width: 100 %; background-color: #ff6666; color: #ffe6e6; border-radius: 5px 5px 0 0;}";

            css += ".card-body {border - radius: 0px 0px 5px 5px;}";
            css += "</style>";
            #endregion

            #region html basic body

            //string html = string.Empty;
            //html += css;
            //html += "<div class='wrapper'>";
            //html += "<div class='card'>";
            //html += $"<div class='card-header-{status}'>";
            //html += "<h3>Error In Automated Attendance System</h3>";
            //html += "</div>";
            //html += "<div class='card-body'>";
            //html += $"<p>{message}</p>";
            //html += "</div>";
            //html += "</div>";
            //html += "</div>";
            #endregion

            #region html dynamic body


            string html = "<!DOCTYPE html>\r\n\r\n<html lang=\"en\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:v=\"urn:schemas-microsoft-com:vml\">\r\n<head>\r\n<title></title>\r\n<meta content=\"text/html; charset=utf-8\" http-equiv=\"Content-Type\"/>\r\n<meta content=\"width=device-width, initial-scale=1.0\" name=\"viewport\"/>\r\n<!--[if mso]><xml><o:OfficeDocumentSettings><o:PixelsPerInch>96</o:PixelsPerInch><o:AllowPNG/></o:OfficeDocumentSettings></xml><![endif]-->\r\n<!--[if !mso]><!-->\r\n<link href=\"https://fonts.googleapis.com/css?family=Ubuntu\" rel=\"stylesheet\" type=\"text/css\"/>\r\n<!--<![endif]-->\r\n<style>\r\n\t\t* {\r\n\t\t\tbox-sizing: border-box;\r\n\t\t}\r\n\r\n\t\tbody {\r\n\t\t\tmargin: 0;\r\n\t\t\tpadding: 0;\r\n\t\t}\r\n\r\n\t\ta[x-apple-data-detectors] {\r\n\t\t\tcolor: inherit !important;\r\n\t\t\ttext-decoration: inherit !important;\r\n\t\t}\r\n\r\n\t\t#MessageViewBody a {\r\n\t\t\tcolor: inherit;\r\n\t\t\ttext-decoration: none;\r\n\t\t}\r\n\r\n\t\tp {\r\n\t\t\tline-height: inherit\r\n\t\t}\r\n\r\n\t\t.desktop_hide,\r\n\t\t.desktop_hide table {\r\n\t\t\tmso-hide: all;\r\n\t\t\tdisplay: none;\r\n\t\t\tmax-height: 0px;\r\n\t\t\toverflow: hidden;\r\n\t\t}\r\n\r\n\t\t@media (max-width:570px) {\r\n\t\t\t.desktop_hide table.icons-inner {\r\n\t\t\t\tdisplay: inline-block !important;\r\n\t\t\t}\r\n\r\n\t\t\t.icons-inner {\r\n\t\t\t\ttext-align: center;\r\n\t\t\t}\r\n\r\n\t\t\t.icons-inner td {\r\n\t\t\t\tmargin: 0 auto;\r\n\t\t\t}\r\n\r\n\t\t\t.image_block img.big,\r\n\t\t\t.row-content {\r\n\t\t\t\twidth: 100% !important;\r\n\t\t\t}\r\n\r\n\t\t\t.mobile_hide {\r\n\t\t\t\tdisplay: none;\r\n\t\t\t}\r\n\r\n\t\t\t.stack .column {\r\n\t\t\t\twidth: 100%;\r\n\t\t\t\tdisplay: block;\r\n\t\t\t}\r\n\r\n\t\t\t.mobile_hide {\r\n\t\t\t\tmin-height: 0;\r\n\t\t\t\tmax-height: 0;\r\n\t\t\t\tmax-width: 0;\r\n\t\t\t\toverflow: hidden;\r\n\t\t\t\tfont-size: 0px;\r\n\t\t\t}\r\n\r\n\t\t\t.desktop_hide,\r\n\t\t\t.desktop_hide table {\r\n\t\t\t\tdisplay: table !important;\r\n\t\t\t\tmax-height: none !important;\r\n\t\t\t}\r\n\r\n\t\t\t.row-2 .column-2 .block-2.heading_block td.pad {\r\n\t\t\t\tpadding: 10px !important;\r\n\t\t\t}\r\n\r\n\t\t\t.row-2 .column-2 .block-2.heading_block h3 {\r\n\t\t\t\tfont-size: 20px !important;\r\n\t\t\t}\r\n\r\n\t\t\t.row-1 .column-1 .block-1.paragraph_block td.pad>div {\r\n\t\t\t\tfont-size: 11px !important;\r\n\t\t\t}\r\n\t\t}\r\n\t</style>\r\n</head>\r\n<body style=\"background-color: #FFFFFF; margin: 0; padding: 0; -webkit-text-size-adjust: none; text-size-adjust: none;\">\r\n<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"nl-container\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; background-color: #FFFFFF;\" width=\"100%\">\r\n<tbody>\r\n<tr>\r\n<td>\r\n<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"row row-1\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tbody>\r\n<tr>\r\n<td>\r\n<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"row-content stack\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 550px;\" width=\"550\">\r\n<tbody>\r\n<tr>\r\n<td class=\"column column-1\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; vertical-align: top; padding-top: 5px; padding-bottom: 5px; border-top: 0px; border-right: 0px; border-bottom: 0px; border-left: 0px;\" width=\"100%\">\r\n<table border=\"0\" cellpadding=\"20\" cellspacing=\"0\" class=\"paragraph_block block-1\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;\" width=\"100%\">\r\n<tr>\r\n<td class=\"pad\">\r\n<div style=\"color:#ffa1a1;direction:ltr;font-family:'Ubuntu', Tahoma, Verdana, Segoe, sans-serif;font-size:14px;font-weight:400;letter-spacing:0px;line-height:200%;text-align:center;mso-line-height-alt:28px;\">\r\n<p style=\"margin: 0;\">This email is generated automatically. Do not reply.</p>\r\n</div>\r\n</td>\r\n</tr>\r\n</table>\r\n</td>\r\n</tr>\r\n</tbody>\r\n</table>\r\n</td>\r\n</tr>\r\n</tbody>\r\n</table>\r\n<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"row row-2\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tbody>\r\n<tr>\r\n<td>\r\n<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"row-content stack\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; border-radius: 0; width: 550px;\" width=\"550\">\r\n<tbody>\r\n<tr>\r\n<td class=\"column column-1\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; vertical-align: middle; border-top: 0px; border-right: 0px; border-bottom: 0px; border-left: 0px;\" width=\"25%\">\r\n<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"image_block block-2\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tr>\r\n<td class=\"pad\" style=\"width:100%;padding-right:0px;padding-left:0px;padding-top:5px;padding-bottom:5px;\">\r\n<div align=\"center\" class=\"alignment\" style=\"line-height:10px\"><img src=\"https://drive.google.com/uc?export=view&id=106De-FQdoYr9vXV9RRIpSvshuBrzHMDT\" style=\"display: block; height: auto; border: 0; width: 110px; max-width: 100%;\" width=\"110\"/></div>\r\n</td>\r\n</tr>\r\n</table>\r\n</td>\r\n<td class=\"column column-2\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; vertical-align: middle; border-top: 0px; border-right: 0px; border-bottom: 0px; border-left: 0px;\" width=\"75%\">\r\n<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"heading_block block-2\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tr>\r\n<td class=\"pad\" style=\"width:100%;text-align:center;padding-top:20px;padding-right:15px;padding-bottom:20px;padding-left:15px;\">\r\n<h3 style=\"margin: 0; color: #fc6363; font-size: 24px; font-family: 'Ubuntu', Tahoma, Verdana, Segoe, sans-serif; line-height: 200%; text-align: center; direction: ltr; font-weight: 700; letter-spacing: normal; margin-top: 0; margin-bottom: 0;\"><span class=\"tinyMce-placeholder\">BSSIT Error Reporting System</span></h3>\r\n</td>\r\n</tr>\r\n</table>\r\n</td>\r\n</tr>\r\n</tbody>\r\n</table>\r\n</td>\r\n</tr>\r\n</tbody>\r\n</table>\r\n<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"row row-3\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tbody>\r\n<tr>\r\n<td>\r\n<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"row-content stack\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 550px;\" width=\"550\">\r\n<tbody>\r\n<tr>\r\n<td class=\"column column-1\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; vertical-align: top; padding-top: 5px; padding-bottom: 5px; border-top: 0px; border-right: 0px; border-bottom: 0px; border-left: 0px;\" width=\"100%\">\r\n<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"image_block block-1\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tr>\r\n<td class=\"pad\" style=\"width:50%;padding-right:0px;padding-left:0px;\">\r\n<div align=\"center\" class=\"alignment\" style=\"line-height:10px\"><img alt=\"Image\" class=\"big\" src=\"https://media4.giphy.com/media/3o7WTDH9gYo71TurPq/giphy.gif?cid=20eb4e9dgysh5npxa6321yjlzt7t4auhxxuz0kw1n66yey08&rid=giphy.gif&ct=g\" style=\"display: block; height: auto; border: 0; width: 240px; max-width: 500%;\" title=\"Image\" width=\"240\"/></div>\r\n</td>\r\n</tr>\r\n</table>\r\n<table border=\"0\" cellpadding=\"10\" cellspacing=\"0\" class=\"text_block block-2\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;\" width=\"100%\">\r\n<tr>\r\n<td class=\"pad\">\r\n<div style=\"font-family: sans-serif\">\r\n<div class=\"\" style=\"font-size: 12px; font-family: Arial, Helvetica Neue, Helvetica, sans-serif; mso-line-height-alt: 14.399999999999999px; color: #f54c4c; line-height: 1.2;\">\r\n<p style=\"margin: 0; font-size: 16px; text-align: left; mso-line-height-alt: 19.2px;\"><strong><span style=\"font-size:12px;\">Error occurred in Automated Attendance System</span></strong></p>\r\n</div>\r\n</div>\r\n</td>\r\n</tr>\r\n</table>\r\n<table border=\"0\" cellpadding=\"10\" cellspacing=\"0\" class=\"divider_block block-3\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tr>\r\n<td class=\"pad\">\r\n<div align=\"center\" class=\"alignment\">\r\n<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tr>\r\n<td class=\"divider_inner\" style=\"font-size: 1px; line-height: 1px; border-top: 1px solid #BBBBBB;\"><span> </span></td>\r\n</tr>\r\n</table>\r\n</div>\r\n</td>\r\n</tr>\r\n</table>\r\n<table border=\"0\" cellpadding=\"10\" cellspacing=\"0\" class=\"text_block block-4\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;\" width=\"100%\">\r\n<tr>\r\n<td class=\"pad\">\r\n<div style=\"font-family: sans-serif\">\r\n<div class=\"\" style=\"font-size: 12px; mso-line-height-alt: 14.399999999999999px; color: #555555; line-height: 1.2; font-family: Arial, Helvetica Neue, Helvetica, sans-serif;\">\r\n<p style=\"margin: 0; font-size: 12px; mso-line-height-alt: 14.399999999999999px;\"><span style=\"font-size:10px;\">Automated Attendance System ran into this following error(s):</span></p>\r\n</div>\r\n</div>\r\n</td>\r\n</tr>\r\n</table>\r\n<table border=\"0\" cellpadding=\"10\" cellspacing=\"0\" class=\"divider_block block-5\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tr>\r\n<td class=\"pad\">\r\n<div align=\"center\" class=\"alignment\">\r\n<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tr>\r\n<td class=\"divider_inner\" style=\"font-size: 1px; line-height: 1px; border-top: 2px dashed #F54C4C;\"><span> </span></td>\r\n</tr>\r\n</table>\r\n</div>\r\n</td>\r\n</tr>\r\n</table>\r\n<table border=\"0\" cellpadding=\"10\" cellspacing=\"0\" class=\"paragraph_block block-6\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; word-break: break-word;\" width=\"100%\">\r\n<tr>\r\n<td class=\"pad\">\r\n<div style=\"color:#fc6767;font-size:16px;font-family:Arial, Helvetica Neue, Helvetica, sans-serif;font-weight:400;line-height:150%;text-align:left;direction:ltr;letter-spacing:0px;mso-line-height-alt:24px;\">\r\n<p style=\"margin: 0;\">" + message + "</p>\r\n</div>\r\n</td>\r\n</tr>\r\n</table>\r\n</td>\r\n</tr>\r\n</tbody>\r\n</table>\r\n</td>\r\n</tr>\r\n</tbody>\r\n</table>\r\n<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"row row-4\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tbody>\r\n<tr>\r\n<td>\r\n<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"row-content stack\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; border-radius: 0; width: 550px;\" width=\"550\">\r\n<tbody>\r\n<tr>\r\n<td class=\"column column-1\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; vertical-align: top; padding-top: 5px; padding-bottom: 5px; border-top: 0px; border-right: 0px; border-bottom: 0px; border-left: 0px;\" width=\"100%\">\r\n<table border=\"0\" cellpadding=\"10\" cellspacing=\"0\" class=\"divider_block block-1\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tr>\r\n<td class=\"pad\">\r\n<div align=\"center\" class=\"alignment\">\r\n<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tr>\r\n<td class=\"divider_inner\" style=\"font-size: 1px; line-height: 1px; border-top: 2px dashed #F53D3D;\"><span> </span></td>\r\n</tr>\r\n</table>\r\n</div>\r\n</td>\r\n</tr>\r\n</table>\r\n<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"image_block block-3\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tr>\r\n<td class=\"pad\" style=\"width:100%;padding-top:70px;padding-right:10px;padding-bottom:10px;padding-left:10px;\">\r\n<div align=\"center\" class=\"alignment\" style=\"line-height:10px\"><img src=\"https://drive.google.com/uc?export=view&id=1_FgF03PbVUbq1DWbWzlozJVGySdJvUg5\" style =\"display: block; height: auto; border: 0; width: 106px; max-width: 100%;\" width=\"106\"/></div>\r\n</td>\r\n</tr>\r\n</table>\r\n</td>\r\n</tr>\r\n</tbody>\r\n</table>\r\n</td>\r\n</tr>\r\n</tbody>\r\n</table>\r\n<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"row row-5\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tbody>\r\n<tr>\r\n<td>\r\n<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"row-content stack\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; color: #000000; width: 550px;\" width=\"550\">\r\n<tbody>\r\n<tr>\r\n<td class=\"column column-1\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; font-weight: 400; text-align: left; vertical-align: top; padding-top: 5px; padding-bottom: 5px; border-top: 0px; border-right: 0px; border-bottom: 0px; border-left: 0px;\" width=\"100%\">\r\n<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" class=\"icons_block block-1\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tr>\r\n<td class=\"pad\" style=\"vertical-align: middle; color: #9d9d9d; font-family: inherit; font-size: 15px; padding-bottom: 5px; padding-top: 5px; text-align: center;\">\r\n<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt;\" width=\"100%\">\r\n<tr>\r\n<td class=\"alignment\" style=\"vertical-align: middle; text-align: center;\">\r\n<!--[if vml]><table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"display:inline-block;padding-left:0px;padding-right:0px;mso-table-lspace: 0pt;mso-table-rspace: 0pt;\"><![endif]-->\r\n<!--[if !vml]><!-->\r\n<table cellpadding=\"0\" cellspacing=\"0\" class=\"icons-inner\" role=\"presentation\" style=\"mso-table-lspace: 0pt; mso-table-rspace: 0pt; display: inline-block; margin-right: -4px; padding-left: 0px; padding-right: 0px;\">\r\n<!--<![endif]-->\r\n</table>\r\n</td>\r\n</tr>\r\n</table>\r\n</td>\r\n</tr>\r\n</table>\r\n</td>\r\n</tr>\r\n</tbody>\r\n</table>\r\n</td>\r\n</tr>\r\n</tbody>\r\n</table>\r\n</td>\r\n</tr>\r\n</tbody>\r\n</table><!-- End -->\r\n</body>\r\n</html>";
            #endregion

            return html;
        }

        public bool SendLogEmail()
        {
            string _userName = _primaryEmail.EmailAddress.ToLower();
            string _password = _primaryEmail.Password;
            SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(_userName, _password);
            MailAddress from = new MailAddress(_userName, "BSS");
            MailAddress to = new MailAddress("farhan.bssit.bd@gmail.com");
            MailMessage message = new MailMessage(from, to);

            message.Subject = "Automated Attendance Log File";
            message.IsBodyHtml = true;
            message.Body = $"Log file of Automated Attendance System for {DateTime.Today.Date}";
            Attachment attachment;
            //attachment = new System.Net.Mail.Attachment($"AutomatedAttendanceSystemLogger_{DateTime.Now.Month}_{DateTime.Now.Year}.txt"); //For Testing
            attachment = new Attachment($"H:\\ProcessLogFile\\AutomatedAttendanceSystem\\AutomatedAttendanceSystemLog-{DateTime.Now:yyyy}{DateTime.Now:MM}.log"); //For deploy
            message.Attachments.Add(attachment);

            try
            {
                client.Send(message);
                return true;
            }

            catch (Exception ex)
            {
                string err = ex.ToString();
            }

            return true;
        }

        #region Unused Code

        //public bool SendLogExceptionEmail(string status, string subject, string msg)
        //{
        //    string _userName = _primaryEmail.EmailAddress.ToLower();
        //    string _password = _primaryEmail.Password;
        //    SmtpClient client = new SmtpClient("smtp.gmail.com", 587);
        //    client.EnableSsl = true;
        //    client.Credentials = new NetworkCredential(_userName, _password);
        //    MailAddress from = new MailAddress(_userName, "BSS");
        //    MailAddress to = new MailAddress("farhan.bssit.bd@gmail.com");
        //    MailMessage message = new MailMessage(from, to);

        //    message.Subject = subject;
        //    message.IsBodyHtml = true;
        //    message.Body = EmailBody(status: status, message: msg);
        //    try
        //    {
        //        client.Send(message);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        string err = ex.ToString();
        //    }

        //    return true;
        //}

        #endregion
    }
}