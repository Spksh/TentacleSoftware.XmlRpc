using System;
using System.Collections.Generic;

namespace TentacleSoftware.XmlRpc.Owin
{
    public static class ExceptionExtensions
    {
        public static IEnumerable<Exception> Enumerate(this Exception exception)
        {
            if (exception == null)
            {
                yield break;
            }

            yield return exception;

            AggregateException aggregate = exception as AggregateException;

            if (aggregate != null)
            {
                foreach (Exception a in aggregate.Flatten().InnerExceptions)
                {
                    foreach (Exception ex in a.Enumerate())
                    {
                        yield return ex;
                    }
                }
            }

            Exception inner = exception.InnerException;

            if (inner != null)
            {
                foreach (Exception ex in inner.Enumerate())
                {
                    yield return ex;
                }
            }
        }
    }
}
