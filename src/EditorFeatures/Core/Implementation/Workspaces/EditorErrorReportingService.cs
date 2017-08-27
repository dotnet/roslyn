// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    internal class EditorErrorReportingService : IErrorReportingService
    {
        public void ShowDetailedErrorInfo(Exception exception)
        {
            Logger.Log(FunctionId.Extension_Exception, exception.StackTrace);
        }

        public void ShowErrorInfoInActiveView(string message, params InfoBarUI[] items)
        {
            ShowGlobalErrorInfo(message, items);
        }

        public void ShowGlobalErrorInfo(string message, params InfoBarUI[] items)
        {
            Logger.Log(FunctionId.Extension_Exception, message);
        }
    }
}
