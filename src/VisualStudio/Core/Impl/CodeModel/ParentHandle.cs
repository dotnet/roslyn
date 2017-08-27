// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal struct ParentHandle<T>
    {
        private readonly ComHandle<object, object> _comHandle;

        public ParentHandle(T parent)
        {
            _comHandle = new ComHandle<object, object>(parent);
        }

        public T Value
        {
            get { return (T)_comHandle.Object; }
        }
    }
}
