// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal interface IVSTypeScriptNavigateToSearchResult
    {
        string AdditionalInformation { get; }
        string Kind { get; }
        VSTypeScriptNavigateToMatchKind MatchKind { get; }
        bool IsCaseSensitive { get; }
        string Name { get; }
        ImmutableArray<TextSpan> NameMatchSpans { get; }
        string SecondarySort { get; }
        string Summary { get; }

        IVSTypeScriptNavigableItem NavigableItem { get; }
    }
}
