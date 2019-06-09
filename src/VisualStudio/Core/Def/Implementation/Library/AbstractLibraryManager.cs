// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library
{
    internal abstract partial class AbstractLibraryManager : IVsCoTaskMemFreeMyStrings
    {
        internal readonly Guid LibraryGuid;
        private readonly IServiceProvider _serviceProvider;
        private readonly IntPtr _imageListPtr;

        protected AbstractLibraryManager(Guid libraryGuid, IServiceProvider serviceProvider)
        {
            LibraryGuid = libraryGuid;
            _serviceProvider = serviceProvider;

            var vsShell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;
            vsShell?.TryGetPropertyValue(__VSSPROPID.VSSPROPID_ObjectMgrTypesImgList, out _imageListPtr);
        }

        public IServiceProvider ServiceProvider
        {
            get { return _serviceProvider; }
        }

        public IntPtr ImageListPtr
        {
            get { return _imageListPtr; }
        }
    }
}
