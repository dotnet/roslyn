// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.VisualStudio.LanguageServices;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    internal partial class VisualStudioErrorReportingService
    {
        private static string GetFormattedExceptionStack(Exception exception)
        {
            if (exception is AggregateException aggregate)
            {
                return GetStackForAggregateException(exception, aggregate);
            }

            if (exception is RemoteInvocationException)
            {
                return exception.ToString();
            }

            return GetStackForException(exception, includeMessageOnly: false);
        }

        private static string GetStackForAggregateException(Exception exception, AggregateException aggregate)
        {
            var text = GetStackForException(exception, includeMessageOnly: true);
            for (var i = 0; i < aggregate.InnerExceptions.Count; i++)
            {
                text = $"{text}{Environment.NewLine}---> (Inner Exception #{i}) {GetFormattedExceptionStack(aggregate.InnerExceptions[i])} <--- {Environment.NewLine}";
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
            var stackTrace = new StackTrace(exception);
            var stackFrames = stackTrace.GetFrames();
            if (stackFrames == null)
            {
                return string.Empty;
            }

            var stackFrameLines = from frame in stackFrames
                                  let method = frame.GetMethod()
                                  let declaringType = method?.DeclaringType
                                  where ShouldShowFrame(declaringType)
                                  select FormatFrame(method, declaringType);
            var stringBuilder = new StringBuilder();
            return string.Join(Environment.NewLine, stackFrameLines);
        }

        private static bool ShouldShowFrame(Type declaringType)
            => !(declaringType != null && typeof(INotifyCompletion).IsAssignableFrom(declaringType));

        private static string FormatFrame(MethodBase method, Type declaringType)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("   at ");
            var isAsync = FormatMethodName(stringBuilder, declaringType);
            if (!isAsync)
            {
                stringBuilder.Append(method?.Name);
                var methodInfo = method as MethodInfo;
                if (methodInfo?.IsGenericMethod == true)
                {
                    FormatGenericArguments(stringBuilder, methodInfo.GetGenericArguments());
                }
            }
            else if (declaringType?.IsGenericType == true)
            {
                FormatGenericArguments(stringBuilder, declaringType.GetGenericArguments());
            }

            stringBuilder.Append('(');
            if (isAsync)
            {
                stringBuilder.Append(ServicesVSResources.Unknown_parameters);
            }
            else
            {
                FormatParameters(stringBuilder, method);
            }

            stringBuilder.Append(')');

            return stringBuilder.ToString();
        }

        private static bool FormatMethodName(StringBuilder stringBuilder, Type declaringType)
        {
            if (declaringType == null)
            {
                return false;
            }

            var fullName = declaringType.FullName.Replace('+', '.');
            if (typeof(IAsyncStateMachine).GetTypeInfo().IsAssignableFrom(declaringType))
            {
                stringBuilder.Append("async ");
                var start = fullName.LastIndexOf('<');
                var end = fullName.LastIndexOf('>');
                if (start >= 0 && end >= 0)
                {
                    stringBuilder.Append(fullName.Remove(start, 1)[..(end - 1)]);
                }
                else
                {
                    stringBuilder.Append(fullName);
                }

                return true;
            }
            else
            {
                stringBuilder.Append(fullName);
                stringBuilder.Append(".");
                return false;
            }
        }

        private static void FormatGenericArguments(StringBuilder stringBuilder, Type[] genericTypeArguments)
        {
            if (genericTypeArguments.Length <= 0)
            {
                return;
            }

            stringBuilder.Append("[" + string.Join(",", genericTypeArguments.Select(args => args.Name)) + "]");
        }

        private static void FormatParameters(StringBuilder stringBuilder, MethodBase method)
            => stringBuilder.Append(string.Join(",", method?.GetParameters().Select(t => (t.ParameterType?.Name ?? "<UnknownType>") + " " + t.Name) ?? Array.Empty<string>()));
    }
}
