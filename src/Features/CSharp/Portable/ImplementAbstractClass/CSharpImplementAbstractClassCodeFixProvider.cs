// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ImplementAbstractClass;

namespace Microsoft.CodeAnalysis.CSharp.ImplementAbstractClass
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ImplementAbstractClass), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateType)]
    internal class CSharpImplementAbstractClassCodeFixProvider :
        AbstractImplementAbstractClassCodeFixProvider<TypeDeclarationSyntax>
    {
        private const string CS0534 = nameof(CS0534); // 'Program' does not implement inherited abstract member 'Goo.bar()'

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpImplementAbstractClassCodeFixProvider()
            : base(CS0534)
        {
        }

        protected override SyntaxToken GetClassIdentifier(TypeDeclarationSyntax classNode)
            => classNode.Identifier;
    }
}
