// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor.Host
{
    /// <summary>
    /// API for hosts to provide if they can present FindReferences results in a streaming manner.
    /// i.e. if they support showing results as they are found instead of after all of the results
    /// are found.
    /// </summary>
    internal interface IStreamingFindReferencesPresenter
    {
        /// <summary>
        /// Tells the presenter that a search is starting.  The returned <see cref="FindReferencesContext"/>
        /// is used to push information about the search into.  i.e. when a reference is found
        /// <see cref="FindReferencesContext.OnReferenceFoundAsync"/> should be called.  When the
        /// search completes <see cref="FindReferencesContext.OnCompletedAsync"/> should be called. 
        /// etc. etc.
        /// </summary>
        FindReferencesContext StartSearch();
    }
}