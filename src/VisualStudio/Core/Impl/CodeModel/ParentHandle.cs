// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal readonly struct ParentHandle<T>
    {
        private readonly ComHandle<object, object> _comHandle;

        public ParentHandle(T parent)
            => _comHandle = new ComHandle<object, object>(parent);

        public T Value
        {
            get { return (T)_comHandle.Object; }
        }
    }
}
