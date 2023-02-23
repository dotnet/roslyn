// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Configuration
{
    internal partial class DidChangeConfigurationNotificationHandler
    {
        private static readonly ImmutableArray<IOption2> s_supportedOptions = ImmutableArray.Create<IOption2>(
            // Code Action options
            SymbolSearchOptionsStorage.SearchReferenceAssemblies,
            ImplementTypeOptionsStorage.InsertionBehavior,
            ImplementTypeOptionsStorage.PropertyGenerationBehavior);
    }
}
