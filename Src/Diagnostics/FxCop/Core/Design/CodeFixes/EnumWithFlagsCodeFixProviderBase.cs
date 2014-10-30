// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1027: Mark enums with FlagsAttribute
    /// CA2217: Do not mark enums with FlagsAttribute
    /// </summary>
    public abstract class EnumWithFlagsCodeFixProviderBase : CodeFixProviderBase
    {
        private readonly ImmutableArray<string> diagnosticIds = ImmutableArray.Create(EnumWithFlagsDiagnosticAnalyzer.RuleIdMarkEnumsWithFlags,
                                                                                   EnumWithFlagsDiagnosticAnalyzer.RuleIdDoNotMarkEnumsWithFlags);

        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return diagnosticIds;
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
            Contract.ThrowIfNull(flagsAttributeType);

            var workspace = document.Project.Solution.Workspace;
            var newEnumBlockSyntax = diagnostic.Id == EnumWithFlagsDiagnosticAnalyzer.RuleIdMarkEnumsWithFlags ?
                AddFlagsAttribute(workspace, nodeToFix, flagsAttributeType, cancellationToken) :
                RemoveFlagsAttribute(workspace, model, nodeToFix, flagsAttributeType, cancellationToken);

            var newRoot = GetUpdatedRoot(root, nodeToFix, newEnumBlockSyntax);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private static SyntaxNode AddFlagsAttribute(Workspace workspace, SyntaxNode enumTypeSyntax, INamedTypeSymbol flagsAttributeType, CancellationToken cancellationToken)
        {
            var attr = CodeGenerationSymbolFactory.CreateAttributeData(flagsAttributeType);
            return CodeGenerator.AddAttributes(enumTypeSyntax, workspace, SpecializedCollections.SingletonEnumerable(attr))
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static SyntaxNode RemoveFlagsAttribute(Workspace workspace, SemanticModel model, SyntaxNode enumTypeSyntax, INamedTypeSymbol flagsAttributeType, CancellationToken cancellationToken)
        {
            var enumType = model.GetDeclaredSymbol(enumTypeSyntax) as INamedTypeSymbol;
            Contract.ThrowIfNull(enumType);

            var flagsAttribute = enumType.GetAttributes().First(a => a.AttributeClass == flagsAttributeType);
            return CodeGenerator.RemoveAttribute(enumTypeSyntax, workspace, flagsAttribute, CodeGenerationOptions.Default, cancellationToken);
        }
    }
}