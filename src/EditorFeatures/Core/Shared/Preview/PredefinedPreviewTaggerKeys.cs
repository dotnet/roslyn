// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor.Shared.Preview;

internal static class PredefinedPreviewTaggerKeys
{
    public static readonly object DefinitionHighlightingSpansKey = new();
    public static readonly object ReferenceHighlightingSpansKey = new();
    public static readonly object WrittenReferenceHighlightingSpansKey = new();
    public static readonly object ConflictSpansKey = new();
    public static readonly object WarningSpansKey = new();
    public static readonly object SuppressDiagnosticsSpansKey = new();
    public static readonly object StaticClassificationSpansKey = new();
}
