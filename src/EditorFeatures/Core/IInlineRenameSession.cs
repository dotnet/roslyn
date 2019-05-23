// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Editor
{
    internal class InlineRenameSessionInfo
    {
        /// <summary>
        /// Whether or not the entity at the selected location can be renamed.
        /// </summary>
        public bool CanRename { get; }

        /// <summary>
        /// Provides the reason that can be displayed to the user if the entity at the selected 
        /// location cannot be renamed.
        /// </summary>
        public string LocalizedErrorMessage { get; }

        /// <summary>
        /// The session created if it was possible to rename the entity.
        /// </summary>
        public IInlineRenameSession Session { get; }

        internal InlineRenameSessionInfo(string localizedErrorMessage)
        {
            this.CanRename = false;
            this.LocalizedErrorMessage = localizedErrorMessage;
        }

        internal InlineRenameSessionInfo(IInlineRenameSession session)
        {
            this.CanRename = true;
            this.Session = session;
        }
    }

    internal interface IInlineRenameSession
    {
        /// <summary>
        /// Cancels the rename session, and undoes any edits that had been performed by the session.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Dismisses the rename session, completing the rename operation across all files.
        /// </summary>
        void Commit(bool previewChanges = false);
    }
}
