// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Classification;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Classification;
#endif

internal static class FSharpClassificationTags
{
    public static string GetClassificationTypeName(string textTag) => textTag.ToClassificationTypeName();
}
