// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.AliasAmbiguousType;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.AliasAmbiguousType
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AliasAmbiguousType), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.FullyQualify)]
    internal class CSharpAliasAmbiguousTypeCodeFixProvider : AbstractAliasAmbiguousTypeCodeFixProvider
    {
        /// <summary>
        /// 'reference' is an ambiguous reference between 'identifier' and 'identifier'
        /// </summary>
        private const string CS0104 = nameof(CS0104);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpAliasAmbiguousTypeCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS0104);

        protected override string GetTextPreviewOfChange(string alias, ITypeSymbol typeSymbol)
            => $"using {alias} = {typeSymbol.ToNameDisplayString()};";
    }
}
