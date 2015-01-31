// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
{
    internal class NavInfo : IVsNavInfo
    {
        private readonly ObjectList _objectList;

        public NavInfo(ObjectList objectList)
        {
            _objectList = objectList;
        }

        public ObjectList CreateObjectList()
        {
            return _objectList;
        }

        int IVsNavInfo.EnumCanonicalNodes(out IVsEnumNavInfoNodes ppEnum)
        {
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsNavInfo.EnumPresentationNodes(uint dwFlags, out IVsEnumNavInfoNodes ppEnum)
        {
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsNavInfo.GetLibGuid(out Guid pGuid)
        {
            pGuid = Guid.Empty;
            return VSConstants.E_NOTIMPL;
        }

        int IVsNavInfo.GetSymbolType(out uint pdwType)
        {
            pdwType = 0;
            return VSConstants.E_NOTIMPL;
        }
    }
}
