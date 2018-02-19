// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public class DocumentEventArgs : EventArgs
    {
        public Document Document { get; }

        public DocumentEventArgs(Document document)
        {
            Contract.ThrowIfNull(document);
            this.Document = document;
        }
    }
}
