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
    /// <summary>Information about derived types of a closed type.</summary>
    [Experimental(RoslynExperiments.PreviewLanguageFeatureApi, UrlFormat = "https://github.com/dotnet/roslyn/issues/83717")]
    public readonly struct ClosedDerivedTypeInfo
    {
        /// <summary>
        /// Possible direct derived types of the closed type.
        /// </summary>
        public ImmutableArray<INamedTypeSymbol> ClosedDerivedTypes { get; }

        /// <summary>
        /// Indicates whether <see cref="ClosedDerivedTypes" /> represents all possible derived types (i.e. it is a complete set).
        /// This will be false, for example, when a generic closed type has an unspeakable derived type.
        /// </summary>
        public bool IsComplete { get; }

        internal ClosedDerivedTypeInfo(ImmutableArray<INamedTypeSymbol> closedDerivedTypes, bool isComplete)
        {
            Debug.Assert(!closedDerivedTypes.IsDefault);
            ClosedDerivedTypes = closedDerivedTypes;
            IsComplete = isComplete;
        }
    }
}
