// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// Some of the known <see cref="QuickInfoProvider"/> names in use.
    /// Names are used for ordering providers with the <see cref="ExtensionOrderAttribute"/>.
    /// </summary>
    internal static class QuickInfoProviderNames
    {
        public const string Semantic = nameof(Semantic);
        public const string Syntactic = nameof(Syntactic);
    }
}
