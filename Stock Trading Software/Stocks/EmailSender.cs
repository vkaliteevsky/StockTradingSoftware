using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Stocks
{
    public class EmailSender
    {
        private const string Host = "smtp.gmail.com";
        private const int Port = 587;
        private const string RobotUserName = "traderobot9000@gmail.com";
        private const string RobotPassword = "PolnayaParashaBlyad'3";
        private static DBInputOutput.DBWriter dbWriter = new DBInputOutput.DBWriter();
        private static void WriteToDBLog(string funcName, string message)
        {
            //dbWriter.InsertGeneral("EmailSender." + funcName + ": Sent message: " + message, "");
        }
        //private const string Recipients = "mikhailov-d93@yandex.ru""george.kraychik@gmail.com";

        public static void SendEmail(string message)
        {
            SmtpClient client = new SmtpClient(Host, Port)
            {
                Credentials = new NetworkCredential(RobotUserName, RobotPassword),
                EnableSsl = true
            };
            String mailSubject = "There is a message for you";
            //String mailBody = "Message:\n" + message;
            String mailBody = message;
            //client.Send(RobotUserName, "mikhailov-d93@yandex.ru", mailSubject, mailBody);
            WriteToDBLog("SendEmail", message);
            client.Send(RobotUserName, "george.kraychik@gmail.com", mailSubject, mailBody);
        }
        public static void SendEmail(string heading, string message)
        {
            SmtpClient client = new SmtpClient(Host, Port)
            {
                Credentials = new NetworkCredential(RobotUserName, RobotPassword),
                EnableSsl = true
            };
            String mailSubject = heading;
            String mailBody = message;
            //client.Send(RobotUserName, "mikhailov-d93@yandex.ru", mailSubject, mailBody);
            WriteToDBLog("SendEmail", heading + ". " + message);
            client.Send(RobotUserName, "george.kraychik@gmail.com", mailSubject, mailBody);
        }
        public static void SendEmail(Exception exception)
        {
            SmtpClient client = new SmtpClient(Host, Port)
            {
                Credentials = new NetworkCredential(RobotUserName, RobotPassword),
                EnableSsl = true
            };
            String mailSubject = "Some exception is thrown";
            String mailBody = "Message:\n" + exception.Message + "\n******************************\n" +
                "StackTrace: \n" + exception.StackTrace;
            //client.Send(RobotUserName, "mikhailov-d93@yandex.ru", mailSubject, mailBody);
            WriteToDBLog("SendEmail", exception.Message);
            client.Send(RobotUserName, "george.kraychik@gmail.com", mailSubject, mailBody);
        }
    }
}
