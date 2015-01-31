// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ErrorListDiagnosticsPackage
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "This is OK here.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1119:StatementMustNotUseUnnecessaryParenthesis", Justification = "Reviewed.")]
    internal class MyEnumTaskItems : IVsEnumTaskItems
    {
        private uint _index = 0;
        private List<IVsTaskItem> _items = new List<IVsTaskItem>();

        public MyEnumTaskItems(params IVsTaskItem[] items)
        {
            _items = new List<IVsTaskItem>(items);
        }

        public MyEnumTaskItems(IEnumerable<IVsTaskItem> items)
        {
            _items = new List<IVsTaskItem>(items);
        }

        public int Clone(out IVsEnumTaskItems ppenum)
        {
            ppenum = new MyEnumTaskItems(_items);

            return VSConstants.S_OK;
        }

        public int Next([ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")]uint celt, IVsTaskItem[] rgelt, [ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")]uint[] pceltFetched)
        {
            uint fetched = 0;

            while ((_index < _items.Count) && (fetched < celt) && (fetched < ((uint)(rgelt.Length))))
            {
                rgelt[fetched++] = _items[((int)(_index++))];
            }

            pceltFetched[0] = fetched;

            return (fetched == celt) ? VSConstants.S_OK : VSConstants.S_FALSE;
        }

        public int Reset()
        {
            _index = 0;
            return VSConstants.S_OK;
        }

        public int Skip([ComAliasName("Microsoft.VisualStudio.OLE.Interop.ULONG")]uint celt)
        {
            _index += celt;
            return VSConstants.S_OK;
        }
    }
}
