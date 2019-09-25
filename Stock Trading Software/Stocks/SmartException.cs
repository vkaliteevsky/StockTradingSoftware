using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stocks
{
    public class SmartException : Exception
    {
        public ExceptionImportanceLevel ImportanceLevel { get; set; }
        public string ImportanceString
        {
            get
            {
                if (ImportanceLevel == ExceptionImportanceLevel.HIGH) return ("High");
                else if (ImportanceLevel == ExceptionImportanceLevel.MEDIUM) return ("Medium");
                else return ("Low");
            }
        }
        public bool IsCritical { get { return (ImportanceLevel == ExceptionImportanceLevel.HIGH); } }
        public string MethodName { get; set; }
        public string ClassName { get; set; }
        public override string Message { get; }
        public int Code { get; set; }
        
        public SmartException(ExceptionImportanceLevel level, string methodName, string className, string message)
            : base()
        {
            ImportanceLevel = level;
            MethodName = methodName;
            ClassName = className;
            Message = message;
            Code = -1;
            EmailSender.SendEmail("Smart Exception - " + ImportanceString + " Level", ClassName + "." + MethodName + ": " + Message);
        }

        public SmartException(ExceptionImportanceLevel level, string methodName, string className, string message, int code)
            : base()
        {
            ImportanceLevel = level;
            MethodName = methodName;
            ClassName = className;
            Message = message;
            Code = code;
            EmailSender.SendEmail("Smart Exception - " + ImportanceString + " Level", ClassName + "." + MethodName + ": Code = " + Code + ": " + Message);
        }
        public SmartException(Exception e) : base()
        {
            ImportanceLevel = ExceptionImportanceLevel.MEDIUM;
            EmailSender.SendEmail("Smart Exception - " + ImportanceString + " Level", e.Message + "\r\n" + e.StackTrace);
        }
    }
}
