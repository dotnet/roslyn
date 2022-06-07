// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Classification
{
    internal static class FSharpClassificationTags
    {
        public static string GetClassificationTypeName(string textTag) => textTag.ToClassificationTypeName();
    }
}
