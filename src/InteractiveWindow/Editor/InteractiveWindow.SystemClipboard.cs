// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal partial class InteractiveWindow
    {
        private sealed class SystemClipboard : InteractiveWindowClipboard
        {
            internal override bool ContainsData(string format) => Clipboard.ContainsData(format);

            internal override object GetData(string format) => Clipboard.GetData(format);

            internal override bool ContainsText() => Clipboard.ContainsText();

            internal override string GetText() => Clipboard.GetText();

            internal override void SetDataObject(object data, bool copy) => Clipboard.SetDataObject(data, copy);
        }
    }
}
