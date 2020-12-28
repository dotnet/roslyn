// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

            public bool CanMerge(ITextUndoPrimitive older) => true;
            public ITextUndoPrimitive Merge(ITextUndoPrimitive older) => older;
        }
    }
}
