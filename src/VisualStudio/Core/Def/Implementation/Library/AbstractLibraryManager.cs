// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            if (vsShell != null)
            {
                object varImageList;
                int hresult = vsShell.GetProperty((int)__VSSPROPID.VSSPROPID_ObjectMgrTypesImgList, out varImageList);
                if (ErrorHandler.Succeeded(hresult) && varImageList != null)
                {
                    _imageListPtr = (IntPtr)(int)varImageList;
                }
            }
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
