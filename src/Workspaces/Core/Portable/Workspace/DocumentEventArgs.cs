﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
