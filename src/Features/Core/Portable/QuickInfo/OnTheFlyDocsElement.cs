// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.QuickInfo;

internal sealed class OnTheFlyDocsElement(string descriptionText, ImmutableArray<string> symbolReferences)
{
    internal string DescriptionText { get; } = descriptionText;
    internal ImmutableArray<string> SymbolReferences { get; } = symbolReferences;
}
