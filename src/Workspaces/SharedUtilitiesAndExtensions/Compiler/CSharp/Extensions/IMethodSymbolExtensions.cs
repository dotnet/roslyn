// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Shared.Extensions
{
    internal static class IMethodSymbolExtensions
    {
        private static readonly ImmutableArray<string> _entryPointMethodNames
            = ImmutableArray.Create(WellKnownMemberNames.EntryPointMethodName, WellKnownMemberNames.TopLevelStatementsEntryPointMethodName);

        public static bool IsCSharpEntryPoint(this IMethodSymbol methodSymbol, INamedTypeSymbol? taskType, INamedTypeSymbol? genericTaskType)
            => methodSymbol.IsEntryPoint(
                CSharpSyntaxFacts.Instance.StringComparer,
                _entryPointMethodNames,
                taskType,
                genericTaskType);
    }
}
