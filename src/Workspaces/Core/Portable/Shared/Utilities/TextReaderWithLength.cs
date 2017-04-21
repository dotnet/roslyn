// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal abstract class TextReaderWithLength : TextReader
    {
        public TextReaderWithLength(int length)
        {
            Length = length;
        }

        public int Length { get; }
    }
}
