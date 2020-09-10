// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Extensions
{
    internal interface IInfoBarService : IWorkspaceService
    {
        /// <summary>
        /// Show an info bar in the current active view.
        ///
        /// Different hosts can have different definitions on what active view means.
        /// </summary>
        void ShowInfoBarInActiveView(string message, params InfoBarUI[] items);

        /// <summary>
        /// Show global info bar
        /// </summary>
        void ShowInfoBarInGlobalView(string message, params InfoBarUI[] items);
    }

    internal struct InfoBarUI
    {
        public readonly string? Title;
        public readonly UIKind Kind;
        public readonly Action Action;
        public readonly bool CloseAfterAction;

        public InfoBarUI(string title, UIKind kind, Action action, bool closeAfterAction = true)
        {
            Contract.ThrowIfNull(title);

            Title = title;
            Kind = kind;
            Action = action;
            CloseAfterAction = closeAfterAction;
        }

        [MemberNotNullWhen(false, nameof(Title))]
        public bool IsDefault => Title == null;

        internal enum UIKind
        {
            Button,
            HyperLink,
            Close
        }
    }
}
