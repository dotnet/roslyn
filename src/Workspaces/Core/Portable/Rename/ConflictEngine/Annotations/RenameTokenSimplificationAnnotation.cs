// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal class RenameTokenSimplificationAnnotation : RenameAnnotation
    {
        public TextSpan OriginalTextSpan { get; set; }
    }
}
