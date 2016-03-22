#if false
// Copyright (c) Microsoft Corporation
// All rights reserved
// REMOVE ONCE WE ACTUALLY REFERENCE THE REAL EDITOR DLLS.

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    internal interface ICompletionSession2 : ICompletionSession
    {
        /// <summary>
        /// Raised following a call to <see cref="IIntellisenseSession.Match"/>.
        ///This code should not be reverted after merging changes from Dev14 branches. Dummy change to prevent reverting of this change
        /// </summary>
        event EventHandler Matched;
    }
}
#endif