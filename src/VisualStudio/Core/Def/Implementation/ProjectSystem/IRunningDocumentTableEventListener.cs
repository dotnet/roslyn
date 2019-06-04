// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// Defines the methods that get called by the <see cref="RunningDocumentTableEventTracker"/>
    /// for getting notified about running document table events.
    /// </summary>
    interface IRunningDocumentTableEventListener
    {
        void OnCloseDocument(uint docCookie, string moniker);

        void OnRefreshDocumentContext(uint docCookie, string moniker);

        void OnReloadDocumentData(uint docCookie, string moniker);

        void OnBeforeOpenDocument(uint docCookie, string moniker, ITextBuffer textBuffer);

        void OnInitializedDocument(uint docCookie, string moniker, ITextBuffer textBuffer);

        void OnRenameDocument(uint docCookie, string newMoniker, string oldMoniker);
    }
}
