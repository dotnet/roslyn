// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices
{
    internal interface IInvisibleEditor : IDisposable
    {
        ITextBuffer TextBuffer { get; }
    }
}
