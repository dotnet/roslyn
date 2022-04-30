// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Completion.Providers;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal sealed class NintKeywordRecommender : AbstractNativeIntegerKeywordRecommender
    {
        protected override RecommendedKeyword Keyword => new("nint");
    }
}
