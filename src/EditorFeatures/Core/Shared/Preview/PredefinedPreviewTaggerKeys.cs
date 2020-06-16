// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor.Shared.Preview
{
    internal static class PredefinedPreviewTaggerKeys
    {
        public static readonly object DefinitionHighlightingSpansKey = new object();
        public static readonly object ReferenceHighlightingSpansKey = new object();
        public static readonly object WrittenReferenceHighlightingSpansKey = new object();
        public static readonly object ConflictSpansKey = new object();
        public static readonly object WarningSpansKey = new object();
        public static readonly object SuppressDiagnosticsSpansKey = new object();
        public static readonly object StaticClassificationSpansKey = new object();
    }
}
