// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    // Used to aid the investigation of https://github.com/dotnet/roslyn/issues/7364
    internal class NullTextBufferException : Exception
    {
        private readonly Document _document;

        public NullTextBufferException(Document document)
            : base("Cannot retrieve textbuffer from document.")
        {
            _document = document;
        }
    }
}
