﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
