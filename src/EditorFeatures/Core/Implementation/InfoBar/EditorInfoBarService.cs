// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    [ExportWorkspaceService(typeof(IInfoBarService)), Shared]
    internal class EditorInfoBarService : IInfoBarService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorInfoBarService()
        {
        }

        public void ShowInfoBarInActiveView(string message, params InfoBarUI[] items)
            => ShowInfoBarInGlobalView(message, items);

        public void ShowInfoBarInGlobalView(string message, params InfoBarUI[] items)
            => Logger.Log(FunctionId.Extension_InfoBar, message);
    }
}
