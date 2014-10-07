using System;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal static partial class ExceptionHelpers
    {
        /// <summary>
        /// Methods for wrapping delegates in FailFast error reporting code.
        /// Note that the methods in this class are modified by a post-build tool
        /// to insert exception filters - a feature supported by the CLR, but not
        /// exposed in C#.
        /// </summary>
        public static class ExceptionFilter
        {
            /// <summary>
            /// We never actually throw an exception of this type. It's used as
            /// a placeholder so that the rewriting tool can identify catch
            /// blocks that need to be replaced.
            /// </summary>
            public class SpecialExceptionFilterException : Exception
            {
            }

            /// <summary>
            /// Method to be invoked by the exception handler
            /// </summary>
            /// <param name="ex">The exception being thrown. Note that this is of type object,
            /// not Exception since the CLR supports throwing exceptions of any type.</param>
            /// <returns>false always which indicates that the catch block should not be entered and
            /// exception handling should continue as normal. In a fail-fast case, of course, the return
            /// value is moot because process dies before leaving this method.</returns>
            public static bool Filter(object ex)
            {
                CrashIfBadException(ex as Exception);
                return false;
            }

            public static void ExecuteWithErrorReporting(Action action)
            {
                try
                {
                    action();
                }
                catch (SpecialExceptionFilterException)
                {
                    throw;
                }
            }

            public static void ExecuteWithErrorReporting(Action<Task> action, Task task)
            {
                try
                {
                    action(task);
                }
                catch (SpecialExceptionFilterException)
                {
                    throw;
                }
            }

            public static TResult ExecuteWithErrorReporting<TResult>(Func<TResult> func)
            {
                try
                {
                    return func();
                }
                catch (SpecialExceptionFilterException)
                {
                    throw;
                }
            }

            public static TResult ExecuteWithErrorReporting<TResult>(Func<Task, TResult> func, Task task)
            {
                try
                {
                    return func(task);
                }
                catch (SpecialExceptionFilterException)
                {
                    throw;
                }
            }
        }
    }
}
