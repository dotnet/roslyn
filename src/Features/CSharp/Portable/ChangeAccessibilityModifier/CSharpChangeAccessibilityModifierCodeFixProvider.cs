// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeAccessibilityModifier;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.CSharp.ChangeAccessibilityModifier
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ChangeAccessibilityModifier), Shared]
    internal class CSharpChangeAccessibilityModifierCodeFixProvider : AbstractChangeAccessibilityModifierCodeFixProvider
    {
        /// <summary>
        /// 'identifier': cannot change access modifiers when overriding 'accessibility' inherited member 'modifier'
        /// </summary>
        private const string CS0507 = nameof(CS0507);
        /// <summary>
        /// 'identifier': virtual or abstract members cannot be private
        /// </summary>
        private const string CS0621 = nameof(CS0621);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpChangeAccessibilityModifierCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS0621);

        protected override string GetText(Accessibility accessibility)
            => SyntaxFacts.GetText(accessibility);
    }
}
