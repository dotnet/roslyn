// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Completion.Providers;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CSharp.Completion;

internal static class OmniSharpCompletionProviderNames
{
    internal static string ObjectCreationCompletionProvider = typeof(ObjectCreationCompletionProvider).FullName;
    internal static string OverrideCompletionProvider = typeof(OverrideCompletionProvider).FullName;
    internal static string PartialMethodCompletionProvider = typeof(PartialMethodCompletionProvider).FullName;
    internal static string InternalsVisibleToCompletionProvider = typeof(InternalsVisibleToCompletionProvider).FullName;
    internal static string TypeImportCompletionProvider = typeof(TypeImportCompletionProvider).FullName;
    internal static string ExtensionMethodImportCompletionProvider = typeof(ExtensionMethodImportCompletionProvider).FullName;
}
