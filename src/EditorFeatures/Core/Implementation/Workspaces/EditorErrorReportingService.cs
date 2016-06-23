// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    internal class EditorErrorReportingService : IErrorReportingService
    {
        public void ShowErrorInfoForCodeFix(string codefixName, Action OnEnable, Action OnEnableAndIgnore, Action OnClose)
        {
            ShowErrorInfo($"{codefixName} crashed");
        }

        public void ShowErrorInfo(string message, params ErrorReportingUI[] items)
        {
            Logger.Log(FunctionId.Extension_Exception, message);
        }
    }
}
