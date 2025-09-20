// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

internal sealed partial class CodeModelProjectCache
{
    private readonly struct CacheEntry
    {
        // NOTE: The logic here is a little bit tricky.  We can't just keep a WeakReference to
        // something like a ComHandle, since it's not something that our clients keep alive.
        // instead, we keep a weak reference to the inner managed object, which we know will
        // always be alive if the outer aggregate is alive.  We can't just keep a WeakReference
        // to the RCW for the outer object either, since in cases where we have a DCOM or native
        // client, the RCW will be cleaned up, even though there is still a native reference
        // to the underlying native outer object.
        //
        // Instead we make use of an implementation detail of the way the CLR's COM aggregation 
        // works.  Namely, if all references to the aggregated object are released, the CLR 
        // responds to QI's for IUnknown with a different object.  So, we store the original
        // value, when we know that we have a client, and then we use that to compare to see
        // if we still have a client alive.
        //
        // NOTE: This is _NOT_ AddRef'd.  We use it just to store the integer value of the
        // IUnknown for comparison purposes.
        private readonly WeakComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> _fileCodeModelWeakComHandle;

        public CacheEntry(ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel> handle)
            => _fileCodeModelWeakComHandle = new WeakComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>(handle);

        public EnvDTE80.FileCodeModel2 FileCodeModelRcw
        {
            get
            {
                return _fileCodeModelWeakComHandle.ComAggregateObject;
            }
        }

        internal bool TryGetFileCodeModelInstanceWithoutCaringWhetherRcwIsAlive(out FileCodeModel fileCodeModel)
            => _fileCodeModelWeakComHandle.TryGetManagedObjectWithoutCaringWhetherNativeObjectIsAlive(out fileCodeModel);

        public ComHandle<EnvDTE80.FileCodeModel2, FileCodeModel>? ComHandle
        {
            get
            {
                return _fileCodeModelWeakComHandle.ComHandle;
            }
        }
    }
}
