// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public EditorInfoBarService()
        {
        }

        public void ShowInfoBarInActiveView(string message, params InfoBarUI[] items)
            => ShowInfoBarInGlobalView(message, items);

        public void ShowInfoBarInGlobalView(string message, params InfoBarUI[] items)
            => Logger.Log(FunctionId.Extension_InfoBar, message);
    }
}
