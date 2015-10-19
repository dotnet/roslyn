// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
{
    // The native project system requires that project sites implement IVsEditorFactoryNotify, and
    // the project system stores an internal list of the interface pointers to them. The old editor
    // factory would then call each implementation from it's own IVsEditorFactoryNotify methods.
    // Since we now supply our own editor factory that doesn't do this, these methods will never be
    // called. Still, we must implement the interface or we'll never load at all.
    internal partial class CSharpProjectShim : IVsEditorFactoryNotify
    {
        public int NotifyDependentItemSaved(IVsHierarchy hier, uint itemidParent, string documentParentMoniker, uint itemidDpendent, string documentDependentMoniker)
        {
            throw new NotSupportedException();
        }

        public int NotifyItemAdded(uint grfEFN, IVsHierarchy hier, uint itemid, string documentId)
        {
            throw new NotSupportedException();
        }

        public int NotifyItemRenamed(IVsHierarchy hier, uint itemid, string documentOldMoniker, string documentNewMoniker)
        {
            throw new NotSupportedException();
        }
    }
}
