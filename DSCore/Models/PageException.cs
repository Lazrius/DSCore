using System;
using DSCore.Utilities;

namespace DSCore.Models
{
    public class PageException
    {
        public Exception Exception { get; set; }
        public Errors Error { get; set; }

        public PageException(Exception exception, Errors error)
        {
            Exception = exception;
            Error = error;
        }
    }
}