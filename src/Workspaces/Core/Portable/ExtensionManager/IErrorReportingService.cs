// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Extensions
{
    internal interface IErrorReportingService : IWorkspaceService
    {
        void ShowErrorInfoForCodeFix(string codefixName, Action OnEnable, Action OnEnableAndIgnore, Action OnClose);
        void ShowErrorInfo(string message, params ErrorReportingUI[] items);
    }

    internal struct ErrorReportingUI
    {
        public readonly string Title;
        public readonly UIKind Kind;
        public readonly Action Action;
        public readonly bool CloseAfterAction;

        public ErrorReportingUI(string title, UIKind kind, Action action, bool closeAfterAction = true)
        {
            Contract.ThrowIfNull(title);

            Title = title;
            Kind = kind;
            Action = action;
            CloseAfterAction = closeAfterAction;
        }

        public bool IsDefault => Title == null;

        internal enum UIKind
        {
            Button,
            HyperLink,
            Close
        }
    }
}
