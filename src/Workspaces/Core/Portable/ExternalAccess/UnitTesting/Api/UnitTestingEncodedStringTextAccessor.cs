// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal static class UnitTestingEncodedStringTextAccessor
{
    public static SourceText Create(Stream stream, Encoding defaultEncoding)
        => EncodedStringText.Create(stream, defaultEncoding);
}
