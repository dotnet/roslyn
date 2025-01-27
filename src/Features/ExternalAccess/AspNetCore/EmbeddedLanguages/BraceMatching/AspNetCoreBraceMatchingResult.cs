// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages
{
    /// <inheritdoc cref="BraceMatchingResult"/>
    internal readonly record struct AspNetCoreBraceMatchingResult(
        TextSpan LeftSpan,
        TextSpan RightSpan)
    {
        internal BraceMatchingResult ToBraceMatchingResult()
            => new(LeftSpan, RightSpan);
    }
}
