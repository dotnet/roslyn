// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.Debugger;
using Roslyn.Utilities;

#if !EXPRESSIONCOMPILER
using Microsoft.CodeAnalysis.ErrorReporting;
#endif

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class ExpressionEvaluatorFatalError
    {
        private const string RegistryKey = @"Software\Microsoft\ExpressionEvaluator";
        private const string RegistryValue = "EnableFailFast";
        internal static bool IsFailFastEnabled;

        static ExpressionEvaluatorFatalError()
        {
            try
            {
                // Microsoft.Win32.Registry is not supported on OneCore/CoreSystem,
                // so we have to check to see if it's there at runtime.
                var registryType = typeof(object).GetTypeInfo().Assembly.GetType("Microsoft.Win32.Registry");
                if (registryType != null)
                {
                    var hKeyCurrentUserField = registryType.GetTypeInfo().GetDeclaredField("CurrentUser");
                    if (hKeyCurrentUserField != null && hKeyCurrentUserField.IsStatic)
                    {
                        using (var currentUserKey = (IDisposable)hKeyCurrentUserField.GetValue(null))
                        {
                            var openSubKeyMethod = currentUserKey.GetType().GetTypeInfo().GetDeclaredMethod("OpenSubKey", new Type[] { typeof(string), typeof(bool) });
                            using (var eeKey = (IDisposable)openSubKeyMethod.Invoke(currentUserKey, new object[] { RegistryKey, /*writable*/ false }))
                            {
                                if (eeKey != null)
                                {
                                    var getValueMethod = eeKey.GetType().GetTypeInfo().GetDeclaredMethod("GetValue", new Type[] { typeof(string) });
                                    var value = getValueMethod.Invoke(eeKey, new object[] { RegistryValue });
                                    if ((value != null) && (value is int))
                                    {
                                        IsFailFastEnabled = ((int)value == 1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false, "Failure checking registry key: " + ex.ToString());
            }
        }

        internal static bool CrashIfFailFastEnabled(Exception exception)
        {
            if (!IsFailFastEnabled)
            {
                return false;
            }

            if (exception is NotImplementedException)
            {
                // This is part of the dispatcher mechanism.  A NotImplementedException indicates
                // that another component should handle the call.
                return false;
            }

            var dkmException = exception as DkmException;
            if (dkmException != null)
            {
                switch (dkmException.Code)
                {
                    case DkmExceptionCode.E_PROCESS_DESTROYED:
                    case DkmExceptionCode.E_XAPI_REMOTE_CLOSED:
                    case DkmExceptionCode.E_XAPI_REMOTE_DISCONNECTED:
                    case DkmExceptionCode.E_XAPI_COMPONENT_DLL_NOT_FOUND:
                        return false;
                }
            }

            return FatalError.Report(exception);
        }

        internal delegate bool NonFatalExceptionHandler(Exception exception, string implementationName);

        internal static bool ReportNonFatalException(Exception exception, NonFatalExceptionHandler handler)
        {
            if (CrashIfFailFastEnabled(exception))
            {
                throw ExceptionUtilities.Unreachable;
            }

            // Ignore the return value, because we always want to continue after reporting the Exception.
            handler(exception, nameof(ExpressionEvaluatorFatalError));

            return true;
        }
    }
}