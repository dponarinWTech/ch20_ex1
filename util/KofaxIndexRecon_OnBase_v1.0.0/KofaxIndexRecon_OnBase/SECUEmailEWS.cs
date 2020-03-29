using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using log4net;
using Microsoft.Exchange.WebServices.Data;

namespace KofaxIndexRecon_OnBase
{
    public class SECUEmailEWS
    {
        private ILog log;
        private static ExchangeService service;
        private string spacer;

        #region // Properties

        /// <summary>
        /// 'From' email address; should be email corresponding to service account under which the application runs.
        /// Must be valid email, otherwise the application could not send emails. Default value is specified in config file.
        /// </summary>
        public string FromAddress { get; set; }

        /// <summary>
        /// List of recipients' emails; default value specified in config file
        /// </summary>
        public List<string> ToAddress { get; set; }

        /// <summary>
        /// Get or set the CC address; empty list by default
        /// </summary>
        public List<string> CcAddress { get; set; }

        /// <summary>
        /// Subject of the Email Message
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Body of the Email Message
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// List of attachment file names. Empty list by default.
        /// </summary>
        public List<string> Attachments { get; set; }

        /// <summary>
        /// Importance of the EWS email message
        /// </summary>
        private static Importance Importance { get; set; }

        /// <summary>
        /// Determines whether to call Autodiscover method to get Exchange web service URL
        /// </summary>
        public bool Autodiscover { get; set; }

        /// <summary>
        /// Get or set the Exchange username
        /// </summary>
        public string EwsUserName { get; set; }

        /// <summary>
        /// Get or set the Exchange password
        /// </summary>
        public string EwsPassword { get; set; }

        /// <summary>
        /// URI of the Exchange Web Service
        /// </summary>
        public string EWSUri { get; set; }

        /// <summary>
        /// Get or set the Exchange Server version. It will be set to the value from config file;
        /// default value is the most recent one: Exchange2013_SP1.
        /// </summary>
        public ExchangeVersion ExchVersion { get; private set; }

        /// <summary>
        /// Boolean flag, if true - the email must have valid attachment. False by default.
        /// </summary>
        public bool MustHaveAttachment { get; set; }

        /// <summary>
        /// Boolean flag, if true - the email must have non-blank subject. False by default.
        /// </summary>
        public bool MustHaveSubject { get; set; }

        /// <summary>
        /// Maximum allowed attachment size (bytes)
        /// </summary>
        public long MaxAttachmentSize { get; set; }

        /// <summary>
        /// Turns on Trace. Optional feature, only helps in console application.
        /// False by default.
        /// </summary>
        public bool TurnTraceOn { get; set; }

        /// <summary>
        /// Non-empty string indicates error and contains error information.
        /// </summary>
        public string ErrorMessage { get; private set; }
        #endregion // Properties

        public SECUEmailEWS(ILog logger, string spacer)
        {
            this.spacer = spacer;

            // private members default values
            log = logger;
            Body = "";
            Subject = Properties.Settings.Default.EmailErrorSubject;
            //service = new ExchangeService();
            EWSUri = Properties.Settings.Default.EWSUri;
            Importance = Importance.Normal;

            // public attributes default values
            Subject = String.Empty;
            Body = String.Empty;
            MustHaveSubject = false;
            MustHaveAttachment = false;
            ErrorMessage = String.Empty;
            TurnTraceOn = false;
            Autodiscover = true;  // Autodiscover is a preferred method
            MaxAttachmentSize = 10485760; // max attachment size for MultiScan

            string emailFrom = Properties.Settings.Default.EmailFrom;
            if (string.IsNullOrWhiteSpace(emailFrom))
            {
                emailFrom = emailFrom.TrimStart(new char[] { ';', ',', ' ' });
                emailFrom = emailFrom.TrimEnd(new char[] { ';', ',', ' ' });
                FromAddress = emailFrom;
            }
            Attachments = new List<string>();
            CcAddress = new List<string>();

            // set email recipients' addresses from config file value
            ToAddress = new List<string>();
            string emailTo = Properties.Settings.Default.EmailTo;
            if (!string.IsNullOrWhiteSpace(emailTo))
            {
                emailTo = emailTo.TrimStart(new char[] {';', ',', ' '});
                emailTo = emailTo.TrimEnd(new char[] { ';', ',', ' ' });
                ToAddress = emailTo.Split(new[] {";", ",", " "}, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }


        public void SendEmail()
        {
            log?.Info(spacer + "SendMail(): Start");
            if (VerifyFields())
            {
                // verify attachments. To skip this verification keep default setting MustHaveAttachment = 'false'
                if (MustHaveAttachment)
                {
                    foreach (string attch in Attachments)
                    {
                        if (!VerifyAttachment(attch, MustHaveAttachment))
                        {
                            log?.Error(spacer + "SendEmail(): Email message not sent because of invalid attachment");
                            log?.Info(spacer + "SendEmail(): End");
                            return;
                        }

                    }
                }

                try
                {
                    try
                    {
                        service = SetupEWSService();
                    }
                    catch (Exception ex)
                    {
                        log?.Error("SendEmail(): Failed to set up EWS service. Error: " + ex);
                        ErrorMessage = "SendEmail(): Failed to set up EWS service. Error: " + ex.Message;
                        log?.Info(spacer + "SendEmail(): End");
                        return;
                    }
                    if (string.IsNullOrEmpty(ErrorMessage))
                    {
                        SendEwsMessage(BuildEwsMessage());
                    }
                }
                catch (Exception e)
                {
                    log?.Error($"SendEmail(): Failed to send email. Exception: {e.ToString()}");
                    ErrorMessage = $"Exception caught while sending email: {e.Message}";
                }
            }
            log?.Info(spacer + "SendEmail(): End");
        }

        private ExchangeService SetupEWSService()
        {
            log?.Info(spacer + "    " + "SetupEWSService(): Start");
            SetExchangeServerVersion();

            ExchangeService myExchangeService = new ExchangeService(ExchVersion);

            // PreAutthenticate turns on caching mechanism that caches the connection credentials for a given domain in the active process 
            // and re-sends it on subsequent requests.
            myExchangeService.PreAuthenticate = true;
            myExchangeService.TraceEnabled = TurnTraceOn;
            myExchangeService.TraceFlags = TraceFlags.All;

            if (string.IsNullOrWhiteSpace(EwsUserName) || string.IsNullOrWhiteSpace(EwsPassword))
            {
                try
                {
                    myExchangeService.Credentials = CredentialCache.DefaultNetworkCredentials;
                }
                catch (Exception e)
                {
                    ErrorMessage = "Exception caught when setting DefaultNetworkCredentials for Exchange client.";
                    log?.Error($"SetupEWSService(): {ErrorMessage}  " + e.ToString());
                    return null;
                }
            }
            else
            {
                try
                {
                    myExchangeService.Credentials = new WebCredentials(EwsUserName, EwsPassword);
                    myExchangeService.UseDefaultCredentials = false;
                }
                catch (Exception e)
                {
                    ErrorMessage = $"Exception caught when setting up Exchange authentication using provided username '{EwsUserName}' and password.";
                    log?.Error($"SetupEWSService()(): {ErrorMessage}  " + e.ToString());
                    log?.Info(spacer + "    " + "SetupEWSService(): End");
                    return null;
                }
            }

            // get Exchange web service URL
            try
            {
                if (Autodiscover || string.IsNullOrEmpty(EWSUri))
                {
                    myExchangeService.AutodiscoverUrl(FromAddress, RedirectionUrlValidationCallback);
                    log?.Debug(spacer + "      " + "Performed autodiscover.");
                }
                else
                {
                    myExchangeService.Url = new Uri(EWSUri);

                    // if URL from app settings is invalid - run Autodiscover
                    if (myExchangeService.Url.Scheme != "https")
                    {
                        myExchangeService.AutodiscoverUrl(FromAddress, RedirectionUrlValidationCallback);
                    }
                }
                log?.Info(spacer + "      " + $"EWS Endpoint: {myExchangeService.Url}");
            }
            catch (AutodiscoverLocalException e1)
            {
                log?.Error("SetupEWSService(): Autodiscover service could not be contacted. " + e1.ToString());
                ErrorMessage = "Failed to autodiscover EWS URL. Check log file for more details.";
            }
            catch (Microsoft.Exchange.WebServices.Autodiscover.AutodiscoverRemoteException e2)
            {
                log?.Error("SetupEWSService(): The Autodiscover server returned an error.  " + e2.ToString());
                ErrorMessage = "Failed to autodiscover EWS URL: the Autodiscover server returned an error. Check log file for more details.";
            }
            catch (Exception e)
            {
                ErrorMessage = "Exception caught when setting EWS URL.";
                log?.Error($"SetupEWSService(): {ErrorMessage}  " + e.ToString());
            }

            log?.Info(spacer + "    " + "SetupEWSService(): End");
            return myExchangeService;
        }


        /// <summary>
        /// Builds an EWS EmailMessage using current fields values
        /// </summary>
        /// <returns>Returns EWS EmailMessage</returns>
        private EmailMessage BuildEwsMessage()
        {
            var ewsEmailMessage = new EmailMessage(service)
            {
                Subject = Subject,
                Body = new MessageBody(BodyType.Text, Body),   // message will be sent as plain text, not HTML
                Importance = Importance,
                From = FromAddress
            };

            foreach (string address in ToAddress)
            {
                ewsEmailMessage.ToRecipients.Add(address);
            }
            foreach (string address in CcAddress)
            {
                ewsEmailMessage.CcRecipients.Add(address);
            }

            foreach (string attch in Attachments)
            {
                ewsEmailMessage.Attachments.AddFileAttachment(attch);
            }

            return ewsEmailMessage;
        }

        private void SendEwsMessage(EmailMessage emailMessage)
        {
            log?.Info(spacer + "      " + $"SendEWSMessage(): Sending Message");
            try
            {
                emailMessage.Send();
                log?.Info(spacer + "      " + "SendEWSMessage(): Message Sent");
            }
            catch (Exception e)
            {
                log?.Error($"SendEwsMessage(): Failed to send email. Error: {e.ToString()}");
                ErrorMessage = "Exception caught while sending EWS email: " + e.Message;
            }
        }

        private static bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            Uri redirectionUri = new Uri(redirectionUrl);

            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 

            return redirectionUri.Scheme == "https";
        }


        #region // Helper methods
        /// <summary>
        /// Helper method that verifies that subject and body are not empty
        /// </summary>
        /// <returns>Returns true if verified, false otherwise</returns>
        private bool VerifyFields()
        {
            log?.Debug(spacer + "  " + "VerifyFields(): Start");

            if (string.IsNullOrWhiteSpace(FromAddress))
            {
                ErrorMessage = "Invalid 'EmailFrom' email address. This application wouldn't be able to send emails.";
                log?.Error($"    VerifyFields(): {ErrorMessage}");
                log?.Debug(spacer + "  " + "VerifyFields(): End");
                return false;
            }

            if (string.IsNullOrEmpty(Body))
            {
                ErrorMessage = "Email body is empty";
                log?.Error($"    VerifyFields(): {ErrorMessage}");
                log?.Debug(spacer + "  " + "VerifyFields(): End");
                return false;
            }
            else if (MustHaveSubject && string.IsNullOrEmpty(Subject))
            {
                log?.Error("    VerifyFields(): Subject is empty");
                ErrorMessage = "Email subject is empty";
                log?.Debug(spacer + "  " + "VerifyFields(): End");
                return false;
            }
            else if (ToAddress == null || ToAddress.Count == 0)
            {
                log?.Error("    VerifyFields(): No recipient email address was provided");
                ErrorMessage = "No recipient email address was provided";
                log?.Debug(spacer + "  " + "VerifyFields(): End");
                return false;
            }

            log?.Debug(spacer + "  " + "VerifyFields(): End");
            return true;
        }

        /// <summary>
        /// Verifies if attachment file exists and its size does not exceed the limit.
        /// </summary>
        /// <param name="attachmentFileName">Attachment file name (fully qualified)</param>
        /// <param name="mustHaveAttachment">Boolean flag, if true - email must have valid attachment</param>
        /// <param name="errormessage">Error message.</param>
        /// <returns></returns>
        private bool VerifyAttachment(string attachmentFileName, bool mustHaveAttachment)
        {
            if (!mustHaveAttachment) { return true; }
            log?.Debug(spacer + "  " + "VerifyAttachment(): Start");

            // attachment is required, but specified attachment file does not exist
            if (!File.Exists(attachmentFileName))
            {
                log?.Error($"VerifyAttachment(): Attachment file name is blank or the file [{attachmentFileName}] does not exist.");
                log?.Debug(spacer + "  " + "VerifyAttachment(): End");
                ErrorMessage = $"Attachment file name is blank or the attachment file [{attachmentFileName}] does not exist.";
                return false;
            }
            else
            {
                FileInfo fileInfo = new FileInfo(attachmentFileName);
                if (fileInfo.Length > MaxAttachmentSize)
                {
                    float sizeMB = fileInfo.Length / 1024f / 1024f;
                    float maxSize = MaxAttachmentSize / 1024f / 1024f;
                    string msg = $"Current batch PDF size is " + sizeMB.ToString("0.##") + "megabytes. This is greater than the max allowed attachment size of " +
                        maxSize.ToString("0.##") + " megabytes. The email will not be sent. " + $"File name is [{attachmentFileName}]";
                    log?.Error(msg);
                    ErrorMessage = msg;
                    return false;
                }
            }

            log?.Debug(spacer + "  " + "VerifyAttachment(): End");
            return true;
        }

        private void SetExchangeServerVersion()
        {
            switch (Properties.Settings.Default.ExchangeVersion)
            {
                case "Exchange2007_SP1":
                    ExchVersion = ExchangeVersion.Exchange2007_SP1;
                    break;
                case "Exchange2010":
                    ExchVersion = ExchangeVersion.Exchange2010;
                    break;
                case "Exchange2010_SP1":
                    ExchVersion = ExchangeVersion.Exchange2010_SP1;
                    break;
                case "Exchange2010_SP2":
                {
                    ExchVersion = ExchangeVersion.Exchange2010_SP2;
                    break;
                }
                case "Exchange2013":
                {
                    ExchVersion = ExchangeVersion.Exchange2013;
                    break;
                }
                case "Exchange2013_SP1":
                {
                    ExchVersion = ExchangeVersion.Exchange2013_SP1;
                    break;
                }
                default:
                {
                    ExchVersion = ExchangeVersion.Exchange2013_SP1;
                    break;
                }
            }
        }
        #endregion

    }
}
