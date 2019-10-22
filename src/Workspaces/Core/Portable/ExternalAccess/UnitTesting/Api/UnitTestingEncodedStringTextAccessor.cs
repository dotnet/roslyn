// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingEncodedStringTextAccessor
    {
        public static SourceText Create(Stream stream, Encoding defaultEncoding)
            => EncodedStringText.Create(stream, defaultEncoding);
    }
}
