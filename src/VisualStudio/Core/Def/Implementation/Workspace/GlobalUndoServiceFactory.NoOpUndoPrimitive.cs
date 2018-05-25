// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class GlobalUndoServiceFactory
    {
        /// <summary>
        /// no op undo primitive
        /// </summary>
        private class NoOpUndoPrimitive : ITextUndoPrimitive
        {
            public ITextUndoTransaction Parent { get; set; }

            public bool CanRedo { get { return true; } }
            public bool CanUndo { get { return true; } }

            public void Do() { }
            public void Undo() { }

            public bool CanMerge(ITextUndoPrimitive older) { return true; }
            public ITextUndoPrimitive Merge(ITextUndoPrimitive older) { return older; }
        }
    }
}
