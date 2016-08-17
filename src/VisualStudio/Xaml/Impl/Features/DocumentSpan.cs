// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Xaml.Features
{
    internal struct DocumentSpan
    {
        public Document Document { get; }
        public TextSpan TextSpan { get; }

        public DocumentSpan(Document document, TextSpan textSpan) : this()
        {
            this.Document = document;
            this.TextSpan = textSpan;
        }
    }
}
