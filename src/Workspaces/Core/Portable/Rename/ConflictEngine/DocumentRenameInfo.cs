// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine;

/// <summary>
/// Contains all the immutable information to rename and perform conflict checking for a document.
/// </summary>
internal partial record DocumentRenameInfo(
    ImmutableDictionary<TextSpan, LocationRenameContext> TextSpanToLocationContexts,
    ImmutableDictionary<SymbolKey, RenamedSymbolContext> RenamedSymbolContexts,
    ImmutableDictionary<TextSpan, ImmutableHashSet<StringAndCommentRenameContext>> TextSpanToStringAndCommentRenameContexts,
    ImmutableHashSet<string> AllReplacementTexts,
    ImmutableHashSet<string> AllOriginalText,
    // Contains Strings like Bar -> BarAttribute ; Property Bar -> Bar , get_Bar, set_Bar
    ImmutableHashSet<string> AllPossibleConflictNames);
