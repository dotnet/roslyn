// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1027: Mark enums with FlagsAttribute
    /// CA2217: Do not mark enums with FlagsAttribute
    /// </summary>
    public abstract class EnumWithFlagsCodeFixProviderBase : CodeFixProviderBase
    {
        private readonly ImmutableArray<string> _diagnosticIds = ImmutableArray.Create(EnumWithFlagsDiagnosticAnalyzer.RuleIdMarkEnumsWithFlags,
                                                                                   EnumWithFlagsDiagnosticAnalyzer.RuleIdDoNotMarkEnumsWithFlags);

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return _diagnosticIds; }
        }

        protected sealed override string GetCodeFixDescription(Diagnostic diagnostic)
        {
            return diagnostic.Id == EnumWithFlagsDiagnosticAnalyzer.RuleIdMarkEnumsWithFlags ?
                FxCopFixersResources.MarkEnumsWithFlagsCodeFix :
                FxCopFixersResources.DoNotMarkEnumsWithFlagsCodeFix;
        }

        internal virtual SyntaxNode GetUpdatedRoot(SyntaxNode root, SyntaxNode nodeToFix, SyntaxNode newEnumTypeSyntax)
        {
            return root.ReplaceNode(nodeToFix, newEnumTypeSyntax);
        }

        internal sealed override Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var flagsAttributeType = WellKnownTypes.FlagsAttribute(model.Compilation);
            Debug.Assert(flagsAttributeType != null);

            var workspace = document.Project.Solution.Workspace;
            var newEnumBlockSyntax = diagnostic.Id == EnumWithFlagsDiagnosticAnalyzer.RuleIdMarkEnumsWithFlags ?
                AddFlagsAttribute(workspace, nodeToFix, flagsAttributeType, cancellationToken) :
                RemoveFlagsAttribute(workspace, model, nodeToFix, flagsAttributeType, cancellationToken);

            var newRoot = GetUpdatedRoot(root, nodeToFix, newEnumBlockSyntax);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private static SyntaxNode AddFlagsAttribute(Workspace workspace, SyntaxNode enumTypeSyntax, INamedTypeSymbol flagsAttributeType, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(workspace, enumTypeSyntax.Language);
            return generator.AddAttributes(enumTypeSyntax, generator.Attribute(generator.TypeExpression(flagsAttributeType)));
        }

        private static SyntaxNode RemoveFlagsAttribute(Workspace workspace, SemanticModel model, SyntaxNode enumTypeSyntax, INamedTypeSymbol flagsAttributeType, CancellationToken cancellationToken)
        {
            var enumType = model.GetDeclaredSymbol(enumTypeSyntax, cancellationToken) as INamedTypeSymbol;
            Debug.Assert(enumType != null);

            var flagsAttribute = enumType.GetAttributes().First(a => a.AttributeClass == flagsAttributeType);
            var attributeNode = flagsAttribute.ApplicationSyntaxReference.GetSyntax(cancellationToken);
            var generator = SyntaxGenerator.GetGenerator(workspace, enumTypeSyntax.Language);

            return generator.RemoveNode(enumTypeSyntax, attributeNode);
        }
    }
}
