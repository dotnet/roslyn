using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class VisualStudioErrorReportingService
    {
        private const string AsyncMethodPrefix = "async ";

        private static string GetFormattedExceptionStack(Exception exception)
        {
            var aggregate = exception as AggregateException;
            if (aggregate != null)
            {
                return GetStackForAggregateException(exception, aggregate);
            }

            return GetStackForException(exception, false);
        }

        private static string GetStackForAggregateException(Exception exception, AggregateException aggregate)
        {
            var text = GetStackForException(exception, true);
            for (int i = 0; i < aggregate.InnerExceptions.Count; i++)
            {
                text = string.Format(ServicesVSResources._0_1_InnerException_2_3_4_5, text,
                    Environment.NewLine, i, GetFormattedExceptionStack(aggregate.InnerExceptions[i]), "<---", Environment.NewLine);
            }

            return text;
        }

        private static string GetStackForException(Exception exception, bool includeMessageOnly)
        {
            var message = exception.Message;
            var className = exception.GetType().ToString();
            var stackText = message.Length <= 0
                ? className
                : className + " : " + message;
            var innerException = exception.InnerException;
            if (innerException != null)
            {
                if (includeMessageOnly)
                {
                    do
                    {
                        stackText += " ---> " + innerException.Message;
                        innerException = innerException.InnerException;
                    } while (innerException != null);
                }
                else
                {
                    stackText += " ---> " + GetFormattedExceptionStack(innerException) + Environment.NewLine +
                            "   " + ServicesVSResources.End_of_inner_exception_stack;
                }
            }
            
            return stackText + Environment.NewLine + GetAsyncStackTrace(exception);
        }

        private static string GetAsyncStackTrace(Exception exception)
        {
            var stackTrace = new StackTrace(exception, true);

            var stackFrames = stackTrace.GetFrames();
            if (stackFrames == null)
            {
                return string.Empty;
            }

            var firstFrame = true;
            var stringBuilder = new StringBuilder(255);

            foreach (var frame in stackFrames)
            {
                var method = frame.GetMethod();
                if (method == null)
                {
                    continue;
                }

                var declaringType = method.DeclaringType;
                if (declaringType != null && typeof(INotifyCompletion).IsAssignableFrom(declaringType))
                {
                    continue;
                }
                if (firstFrame)
                {
                    firstFrame = false;
                }
                else
                {
                    stringBuilder.Append(Environment.NewLine);
                }

                stringBuilder.AppendFormat("   {0} ", ServicesVSResources.at);
                var isAsync = FormatMethodName(stringBuilder, declaringType);
                if (!isAsync)
                {
                    stringBuilder.Append(method.Name);
                    var methodInfo = method as MethodInfo;
                    if (methodInfo != null && methodInfo.IsGenericMethod)
                    {
                        FormatGenericArguments(stringBuilder, methodInfo.GetGenericArguments());
                    }
                }
                else if (declaringType?.IsGenericType == true)
                {
                    FormatGenericArguments(stringBuilder, declaringType.GetGenericArguments());
                }

                stringBuilder.Append("(");
                if (isAsync)
                {
                    stringBuilder.Append(ServicesVSResources.Unknown_parameters);
                }
                else
                {
                    FormatParameters(stringBuilder, method);
                }

                stringBuilder.Append(")");
            }

            return stringBuilder.ToString();
        }

        private static bool FormatMethodName(StringBuilder stringBuilder, Type declaringType)
        {
            if (declaringType == null)
            {
                return false;
            }

            var isAsync = false;
            var fullName = declaringType.FullName.Replace('+', '.');
            if (typeof(IAsyncStateMachine).GetTypeInfo().IsAssignableFrom(declaringType))
            {
                isAsync = true;
                stringBuilder.Append(AsyncMethodPrefix);
                var start = fullName.LastIndexOf('<');
                var end = fullName.LastIndexOf('>');
                if (start >= 0 && end >= 0)
                {
                    stringBuilder.Append(fullName.Remove(start, 1).Substring(0, end - 1));
                }
                else
                {
                    stringBuilder.Append(fullName);
                }
            }
            else
            {
                stringBuilder.Append(fullName);
                stringBuilder.Append(".");
            }

            return isAsync;
        }

        private static void FormatGenericArguments(StringBuilder stringBuilder, Type[] genericTypeArguments)
        {
            if (genericTypeArguments.Length <= 0)
            {
                return;
            }

            stringBuilder.Append("[");
            var firstTypeParam = true;
            foreach (var genericArgument in genericTypeArguments)
            {
                if (!firstTypeParam)
                {
                    stringBuilder.Append(",");
                }
                else
                {
                    firstTypeParam = false;
                }

                stringBuilder.Append(genericArgument.Name);
            }

            stringBuilder.Append("]");
        }

        private static void FormatParameters(StringBuilder stringBuilder, MethodBase method)
        {
            var parameters = method.GetParameters();
            var firstParam = true;
            foreach (var t in parameters)
            {
                if (!firstParam)
                {
                    stringBuilder.Append(", ");
                }
                else
                {
                    firstParam = false;
                }
                var typeName = t.ParameterType?.Name ?? "<UnknownType>";

                stringBuilder.Append(typeName + " " + t.Name);
            }
        }
    }
}
