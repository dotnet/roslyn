// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
                        
namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal abstract class InteractiveWindowClipboard
    {
        internal abstract bool ContainsData(string format);

        internal abstract object GetData(string format);

        internal abstract bool ContainsText();

        internal abstract string GetText();

        internal abstract void SetDataObject(object data, bool copy);
    }
}
