﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateDeconstructMethod
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateDeconstructMethod), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateEnumMember)]
    internal class GenerateDeconstructMethodCodeFixProvider : CodeFixProvider
    {
        private const string CS8129 = nameof(CS8129); // No suitable Deconstruct instance or extension method was found...

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public GenerateDeconstructMethodCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS8129);

        public override FixAllProvider GetFixAllProvider()
        {
            // Fix All is not supported by this code fix
            return null;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // Not supported in REPL
            if (context.Document.Project.IsSubmission)
            {
                return;
            }

            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var span = context.Span;
            var token = root.FindToken(span.Start);

            var deconstruction = token.GetAncestors<SyntaxNode>()
                .FirstOrDefault(n => n.Kind() is SyntaxKind.SimpleAssignmentExpression or SyntaxKind.ForEachVariableStatement or SyntaxKind.PositionalPatternClause);

            if (deconstruction is null)
            {
                Debug.Fail("The diagnostic can only be produced in context of a deconstruction-assignment, deconstruction-foreach or deconstruction-positionalpattern");
                return;
            }

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            DeconstructionInfo info;
            ITypeSymbol type;
            SyntaxNode target;
            switch (deconstruction)
            {
                case ForEachVariableStatementSyntax @foreach:
                    info = model.GetDeconstructionInfo(@foreach);
                    type = model.GetForEachStatementInfo(@foreach).ElementType;
                    target = @foreach.Variable;
                    break;
                case AssignmentExpressionSyntax assignment:
                    info = model.GetDeconstructionInfo(assignment);
                    type = model.GetTypeInfo(assignment.Right).Type;
                    target = assignment.Left;
                    break;
                case PositionalPatternClauseSyntax positionalPattern:
                    info = default;
                    type = model.GetTypeInfo(deconstruction.Parent).Type;
                    target = deconstruction;
                    break;
                default:
                    throw ExceptionUtilities.Unreachable();
            }

            if (type?.Kind != SymbolKind.NamedType)
            {
                return;
            }

            if (info.Method != null || !info.Nested.IsEmpty)
            {
                // There is already a Deconstruct method, or we have a nesting situation
                return;
            }

            // Checking that Subpatterns of deconstruction are ConstantPatternSyntax because for override of TryMakeParameters in CSharpGenerateDeconstructMethodService
            // Subpatterns are cast to ConstantPatternSyntax for use of GenerateNameForExpression and GetTypeInfo
            if (deconstruction is PositionalPatternClauseSyntax positionalPatternClause && positionalPatternClause.Subpatterns.Any(p => p.Pattern is not ConstantPatternSyntax))
            {
                return;
            }

            var service = document.GetLanguageService<IGenerateDeconstructMemberService>();
            var codeActions = await service.GenerateDeconstructMethodAsync(document, target, (INamedTypeSymbol)type, context.Options, cancellationToken).ConfigureAwait(false);

            Debug.Assert(!codeActions.IsDefault);
            context.RegisterFixes(codeActions, context.Diagnostics);
        }
    }
}
