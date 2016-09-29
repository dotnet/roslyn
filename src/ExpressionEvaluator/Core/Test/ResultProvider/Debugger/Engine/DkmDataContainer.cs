// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Debugger
{
    public abstract class DkmDataContainer
    {
        private readonly Dictionary<Guid, object> _dataItems = new Dictionary<Guid, object>();

        public T GetDataItem<T>() where T : DkmDataItem
        {
            object value;
            if (_dataItems.TryGetValue(typeof(T).GUID, out value))
            {
                return value as T;
            }

            return null;
        }

        public void SetDataItem<T>(DkmDataCreationDisposition creationDisposition, T item) where T : DkmDataItem
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            Guid key = item.GetType().GUID;
            if (creationDisposition == DkmDataCreationDisposition.CreateNew)
            {
                if (_dataItems.ContainsKey(key))
                {
                    throw new ArgumentException("Data item already exists", nameof(item));
                }
            }

            _dataItems[key] = item;
        }
    }
}
