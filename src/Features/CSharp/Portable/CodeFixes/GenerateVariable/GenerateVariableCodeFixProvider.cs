// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.GenerateMember;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateVariable;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateVariable
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateVariable), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateMethod)]
    internal class GenerateVariableCodeFixProvider : AbstractGenerateMemberCodeFixProvider
    {
        private const string CS1061 = nameof(CS1061); // error CS1061: 'C' does not contain a definition for 'Foo' and no extension method 'Foo' accepting a first argument of type 'C' could be found
        private const string CS0103 = nameof(CS0103); // error CS0103: The name 'Foo' does not exist in the current context
        private const string CS0117 = nameof(CS0117); // error CS0117: 'TestNs.Program' does not contain a definition for 'blah'
        private const string CS0539 = nameof(CS0539); // error CS0539: 'Class.SomeProp' in explicit interface declaration is not a member of interface
        private const string CS0246 = nameof(CS0246); // error CS0246: The type or namespace name 'Version' could not be found

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS1061, CS0103, CS0117, CS0539, CS0246); }
        }

        protected override bool IsCandidate(SyntaxNode node, Diagnostic diagnostic)
        {
            return node is SimpleNameSyntax || node is PropertyDeclarationSyntax || node is MemberBindingExpressionSyntax;
        }

        protected override SyntaxNode GetTargetNode(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.MemberBindingExpression))
            {
                var nameNode = node.ChildNodes().FirstOrDefault(n => n.IsKind(SyntaxKind.IdentifierName));
                if (nameNode != null)
                {
                    return nameNode;
                }
            }

            return base.GetTargetNode(node);
        }

        protected override Task<IEnumerable<CodeAction>> GetCodeActionsAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IGenerateVariableService>();
            return service.GenerateVariableAsync(document, node, cancellationToken);
        }
    }
}
