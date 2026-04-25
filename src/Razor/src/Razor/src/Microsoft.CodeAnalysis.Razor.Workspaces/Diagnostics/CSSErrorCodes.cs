// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Diagnostics;

// Note: This type should be kept in sync with WTE's ErrorCodes.cs
internal static class CSSErrorCodes
{
    public const string UnrecognizedBlockType = "CSS002";
    public const string MissingClassNameAfterDot = "CSS008";
    public const string MissingOpeningBrace = "CSS023";
    public const string MissingPropertyName = "CSS024";
    public const string MissingPropertyValue = "CSS025";
    public const string MissingSelectorAfterCombinator = "CSS029";
    public const string MissingSelectorBeforeCombinatorCode = "CSS031";
}
