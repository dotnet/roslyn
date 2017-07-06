// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal class TaskItemsEnum<T> : IVsEnumTaskItems where T : IVsTaskItem
    {
        private readonly T[] _items;
        private int _next;

        public TaskItemsEnum(T[] immutableItems)
        {
            _items = immutableItems;
            _next = 0;
        }

        int IVsEnumTaskItems.Next(uint celt, IVsTaskItem[] rgelt, uint[] pceltFetched)
        {
            checked
            {
                int i;
                for (i = 0; i < celt && _next + i < _items.Length; i++)
                {
                    rgelt[i] = _items[_next + i];
                }

                _next += i;

                if (pceltFetched != null)
                {
                    pceltFetched[0] = (uint)i;
                }

                return (i == celt) ? VSConstants.S_OK : VSConstants.S_FALSE;
            }
        }

        int IVsEnumTaskItems.Skip(uint celt)
        {
            checked
            {
                _next += (int)celt;
            }

            return VSConstants.S_OK;
        }

        int IVsEnumTaskItems.Reset()
        {
            _next = 0;
            return VSConstants.S_OK;
        }

        int IVsEnumTaskItems.Clone(out IVsEnumTaskItems taskItemsEnum)
        {
            taskItemsEnum = new TaskItemsEnum<T>(_items);
            return VSConstants.S_OK;
        }
    }
}
