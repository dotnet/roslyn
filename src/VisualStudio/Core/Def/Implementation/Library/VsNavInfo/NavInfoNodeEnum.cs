// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.VsNavInfo
{
    internal class NavInfoNodeEnum : IVsEnumNavInfoNodes
    {
        private readonly ImmutableArray<NavInfoNode> _nodes;
        private int _index;

        public NavInfoNodeEnum(ImmutableArray<NavInfoNode> nodes)
        {
            _nodes = nodes;
        }

        public int Clone(out IVsEnumNavInfoNodes ppEnum)
        {
            ppEnum = new NavInfoNodeEnum(_nodes);
            return VSConstants.S_OK;
        }

        public int Next(uint celt, IVsNavInfoNode[] rgelt, out uint pceltFetched)
        {
            var i = 0;
            for (; i < celt && _index < _nodes.Length; i++, _index++)
            {
                rgelt[i] = _nodes[_index];
            }

            pceltFetched = (uint)i;

            return i < celt
                ? VSConstants.S_FALSE
                : VSConstants.S_OK;
        }

        public int Reset()
        {
            _index = 0;
            return VSConstants.S_OK;
        }

        public int Skip(uint celt)
        {
            _index += (int)celt;
            return VSConstants.S_OK;
        }
    }
}
