// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.NavInfos
{
    internal class EnumNavInfoNodes : IVsEnumNavInfoNodes
    {
        private readonly ImmutableArray<IVsNavInfoNode> _nodeList;
        private int _currentIndex;

        public EnumNavInfoNodes(ImmutableArray<IVsNavInfoNode> nodeList)
        {
            _nodeList = nodeList;
        }

        public int Clone(out IVsEnumNavInfoNodes ppEnum)
        {
            ppEnum = new EnumNavInfoNodes(_nodeList);
            return VSConstants.S_OK;
        }

        public int Next(uint celt, IVsNavInfoNode[] rgelt, out uint pceltFetched)
        {
            var i = 0;
            for (; i < celt && _currentIndex < _nodeList.Length; i++, _currentIndex++)
            {
                rgelt[i] = _nodeList[_currentIndex];
            }

            pceltFetched = (uint)i;

            return i < celt
                ? VSConstants.S_FALSE
                : VSConstants.S_OK;
        }

        public int Reset()
        {
            _currentIndex = 0;
            return VSConstants.S_OK;
        }

        public int Skip(uint celt)
        {
            _currentIndex += (int)celt;
            return VSConstants.S_OK;
        }
    }
}
