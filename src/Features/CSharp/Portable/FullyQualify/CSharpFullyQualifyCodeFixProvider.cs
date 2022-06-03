// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.FullyQualify;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.FullyQualify
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.FullyQualify), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
    internal class CSharpFullyQualifyCodeFixProvider : AbstractFullyQualifyCodeFixProvider
    {
        /// <summary>
        /// name does not exist in context
        /// </summary>
        private const string CS0103 = nameof(CS0103);

        /// <summary>
        /// 'reference' is an ambiguous reference between 'identifier' and 'identifier'
        /// </summary>
        private const string CS0104 = nameof(CS0104);

        /// <summary>
        /// type or namespace could not be found
        /// </summary>
        private const string CS0246 = nameof(CS0246);

        /// <summary>
        /// wrong number of type args
        /// </summary>
        private const string CS0305 = nameof(CS0305);

        /// <summary>
        /// The non-generic type 'A' cannot be used with type arguments
        /// </summary>
        private const string CS0308 = nameof(CS0308);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpFullyQualifyCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS0103, CS0104, CS0246, CS0305, CS0308, IDEDiagnosticIds.UnboundIdentifierId); }
        }

        protected override bool IgnoreCase => false;

        protected override bool CanFullyQualify(Diagnostic diagnostic, ref SyntaxNode node)
        {
            if (node is not SimpleNameSyntax simpleName)
            {
                return false;
            }

            if (!simpleName.LooksLikeStandaloneTypeName())
            {
                return false;
            }

            if (!simpleName.CanBeReplacedWithAnyName())
            {
                return false;
            }

            return true;
        }

        protected override async Task<SyntaxNode> ReplaceNodeAsync(SyntaxNode node, string containerName, bool resultingSymbolIsType, CancellationToken cancellationToken)
        {
            var simpleName = (SimpleNameSyntax)node;

            var leadingTrivia = simpleName.GetLeadingTrivia();
            var newName = simpleName.WithLeadingTrivia(SyntaxTriviaList.Empty);

            var qualifiedName = SyntaxFactory.QualifiedName(
                SyntaxFactory.ParseName(containerName), newName);

            qualifiedName = qualifiedName.WithLeadingTrivia(leadingTrivia);
            qualifiedName = qualifiedName.WithAdditionalAnnotations(Formatter.Annotation);

            var syntaxTree = simpleName.SyntaxTree;
            var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // If the name is a type that is part of a using directive, eg. "using Math" then we can go further and
            // instead of just changing to "using System.Math", we can make it "using static System.Math" and avoid the
            // CS0138 that would result from the former.  Don't do this for using aliases though as `static` and using
            // aliases cannot be combined.
            if (resultingSymbolIsType &&
                node.Parent is UsingDirectiveSyntax { Alias: null, StaticKeyword.RawKind: 0 } usingDirective)
            {
                var newUsingDirective = usingDirective
                    .WithStaticKeyword(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                    .WithName(qualifiedName);

                return root.ReplaceNode(usingDirective, newUsingDirective);
            }

            return root.ReplaceNode(simpleName, qualifiedName);
        }
    }
}
