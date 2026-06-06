// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [Experimental(RoslynExperiments.PreviewLanguageFeatureApi, UrlFormat = "https://github.com/dotnet/roslyn/issues/83717")]
    public readonly struct ClosedSubtypeInfo
    {
        /// <summary>
        /// Possible subtypes of the closed type.
        /// </summary>
        public ImmutableArray<INamedTypeSymbol> ClosedSubtypes { get; }

        /// <summary>
        /// Indicates whether <see cref="ClosedSubtypes" /> represents all possible subtypes (i.e. it is a complete set).
        /// This will be false, for example, when a generic closed type has an unspeakable subtype.
        /// </summary>
        public bool IsComplete { get; }

        internal ClosedSubtypeInfo(ImmutableArray<INamedTypeSymbol> closedSubtypes, bool isComplete)
        {
            ClosedSubtypes = closedSubtypes;
            IsComplete = isComplete;
        }
    }
}
