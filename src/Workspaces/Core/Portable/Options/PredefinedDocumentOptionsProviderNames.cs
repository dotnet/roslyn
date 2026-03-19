// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Options;

internal static class PredefinedDocumentOptionsProviderNames
{
    /// <summary>
    /// The name of the providers for .editorconfig. Both the current and legacy providers will use this name, so that way any other clients can
    /// order relative to the pair. The two factories are unordered themselves because only one ever actually gives a real provider.
    /// </summary>
    public const string EditorConfig = ".editorconfig";
}
