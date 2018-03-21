using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AD.Core.Exceptions
{
    public class RepoException : Exception
    {
        private string message;
        public RepoException()
        {

        }
        public RepoException(RepoExceptionType type)
        {
            message = string.Format("[{0}]", type.ToString());
        }
        public RepoException(RepoExceptionType type, string message)
        {
            message = string.Format("[{0}] {1}", type.ToString(), message);
        }
        public override string Message
        {
            get
            {
                return message;
            }
        }
    }
    public enum RepoExceptionType
    {
        General,
        ItemNotFound
    }
}
