// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    /// <summary>
    /// Defines the methods that get called by the <see cref="RunningDocumentTableEventTracker"/>
    /// for getting notified about running document table events.
    /// </summary>
    internal interface IRunningDocumentTableEventListener
    {
        /// <summary>
        /// Triggered when a document is opened.
        /// </summary>
        /// <param name="moniker">the non-null moniker of the opened document.</param>
        /// <param name="textBuffer">the non-null text buffer of the opened document)</param>
        /// <param name="hierarchy">the hierarchy of the text buffer if available.</param>
        void OnOpenDocument(string moniker, ITextBuffer textBuffer, IVsHierarchy hierarchy);

        /// <summary>
        /// Triggered when a document is closed.
        /// </summary>
        /// <param name="moniker">the non-null moniker of the closed document.</param>
        void OnCloseDocument(string moniker);

        /// <summary>
        /// Triggered when a document context is refreshed with a new hierarchy.
        /// </summary>
        /// <param name="moniker">the non-null moniker of the document that changed.</param>
        /// <param name="hierarchy">the hierarchy of the text buffer if available.</param>
        void OnRefreshDocumentContext(string moniker, IVsHierarchy hierarchy);

        /// <summary>
        /// Triggered when a document moniker changes.
        /// </summary>
        /// <param name="newMoniker">the document's new moniker.</param>
        /// <param name="oldMoniker">the document's old moniker.</param>
        /// <param name="textBuffer">the document's buffer.</param>
        void OnRenameDocument(string newMoniker, string oldMoniker, ITextBuffer textBuffer);
    }
}
