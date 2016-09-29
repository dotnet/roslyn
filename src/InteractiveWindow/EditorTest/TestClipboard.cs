// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    internal sealed class TestClipboard : InteractiveWindowClipboard
    {
        private DataObject _data = null;

        internal void Clear() => _data = null;

        internal override bool ContainsData(string format) => _data?.GetData(format) != null;

        internal override object GetData(string format) => _data?.GetData(format);

        internal override bool ContainsText() => _data != null ? _data.ContainsText() : false;

        internal override string GetText() => _data?.GetText();

        internal override void SetDataObject(object data, bool copy) => _data = (DataObject)data;

        internal override IDataObject GetDataObject() => _data;
    }
}
