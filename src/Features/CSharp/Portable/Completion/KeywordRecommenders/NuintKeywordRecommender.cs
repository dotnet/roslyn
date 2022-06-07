// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal sealed class NuintKeywordRecommender : AbstractNativeIntegerKeywordRecommender
    {
        /// <summary>
        /// We set the <see cref="MatchPriority"/> of this item less than the default value so that completion selects
        /// the <see langword="null"/> keyword over it as the user starts typing.  Being able to type <see
        /// langword="null"/> with just <c>nu</c> is ingrained in muscle memory and is more important to maintain versus
        /// strict adherence to our normal textual matching procedure.  The user can always still get this item simply
        /// by typing one additional character and unambiguously referring to <c>nui</c>.
        /// </summary>
        protected override RecommendedKeyword Keyword => new("nuint", matchPriority: MatchPriority.Default - 1);
    }
}
