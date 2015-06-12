// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.AnalyzerPowerPack.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace Microsoft.AnalyzerPowerPack.Design
{
    /// <summary>
    /// CA1008: Enums should have zero value
    /// </summary>
    public abstract class CA1008CodeFixProviderBase : CodeFixProviderBase
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CA1008DiagnosticAnalyzer.RuleId); }
        }

        protected sealed override string GetCodeFixDescription(Diagnostic diagnostic)
        {
            foreach (var customTag in diagnostic.Descriptor.CustomTags)
            {
                switch (customTag)
                {
                    case CA1008DiagnosticAnalyzer.RuleRenameCustomTag:
                        return AnalyzerPowerPackFixersResources.EnumsShouldZeroValueFlagsRenameCodeFix;

                    case CA1008DiagnosticAnalyzer.RuleMultipleZeroCustomTag:
                        return AnalyzerPowerPackFixersResources.EnumsShouldZeroValueFlagsMultipleZeroCodeFix;

                    case CA1008DiagnosticAnalyzer.RuleNoZeroCustomTag:
                        return AnalyzerPowerPackFixersResources.EnumsShouldZeroValueNotFlagsNoZeroValueCodeFix;
                }
            }

            throw new InvalidOperationException("This program location is thought to be unreachable.");
        }

        private static SyntaxNode GetDeclaration(ISymbol symbol)
        {
            return (symbol.DeclaringSyntaxReferences.Length > 0) ? symbol.DeclaringSyntaxReferences[0].GetSyntax() : null;
        }

        private SyntaxNode GetExplicitlyAssignedField(IFieldSymbol originalField, SyntaxNode declaration, SyntaxGenerator generator)
        {
            var originalInitializer = generator.GetExpression(declaration);
            if (originalInitializer != null || !originalField.HasConstantValue)
            {
                return declaration;
            }

            return generator.WithExpression(declaration, generator.LiteralExpression(originalField.ConstantValue));
        }

        private async Task<Document> GetUpdatedDocumentForRuleNameRenameAsync(Document document, IFieldSymbol field, CancellationToken cancellationToken)
        {
            var newSolution = await CodeAnalysis.Rename.Renamer.RenameSymbolAsync(document.Project.Solution, field, "None", null, cancellationToken).ConfigureAwait(false);
            return newSolution.GetDocument(document.Id);
        }

        private async Task ApplyRuleNameMultipleZeroAsync(SymbolEditor editor, INamedTypeSymbol enumType, CancellationToken cancellationToken)
        {
            // Diagnostic: Remove all members that have the value zero from '{0}' except for one member that is named 'None'.
            // Fix: Remove all members that have the value zero except for one member that is named 'None'.

            bool needsNewZeroValuedNoneField = true;
            var set = CA1008DiagnosticAnalyzer.GetZeroValuedFields(enumType).ToSet();

            bool makeNextFieldExplicit = false;
            foreach (IFieldSymbol field in enumType.GetMembers().Where(m => m.Kind == SymbolKind.Field))
            {
                var isZeroValued = set.Contains(field);
                var isZeroValuedNamedNone = isZeroValued && CA1008DiagnosticAnalyzer.IsMemberNamedNone(field);

                if (!isZeroValued || isZeroValuedNamedNone)
                {
                    if (makeNextFieldExplicit)
                    {
                        await editor.EditOneDeclarationAsync(field, (e, d) => e.ReplaceNode(d, GetExplicitlyAssignedField(field, d, e.Generator)), cancellationToken).ConfigureAwait(false);
                        makeNextFieldExplicit = false;
                    }

                    if (isZeroValuedNamedNone)
                    {
                        needsNewZeroValuedNoneField = false;
                    }
                }
                else
                {
                    await editor.EditOneDeclarationAsync(field, (e, d) => e.RemoveNode(d), cancellationToken).ConfigureAwait(false); // removes the field declaration
                    makeNextFieldExplicit = true;
                }
            }

            if (needsNewZeroValuedNoneField)
            {
                await editor.EditOneDeclarationAsync(enumType, (e, d) => e.InsertMembers(d, 0, new[] { e.Generator.EnumMember("None") }), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ApplyRuleNameNoZeroValueAsync(SymbolEditor editor, INamedTypeSymbol enumType, CancellationToken cancellationToken)
        {
            // remove any non-zero member named 'None'
            foreach (IFieldSymbol field in enumType.GetMembers().Where(m => m.Kind == SymbolKind.Field))
            {
                if (CA1008DiagnosticAnalyzer.IsMemberNamedNone(field))
                {
                    await editor.EditOneDeclarationAsync(field, (e, d) => e.RemoveNode(d), cancellationToken).ConfigureAwait(false);
                }
            }

            // insert zero-valued member 'None' to top
            await editor.EditOneDeclarationAsync(enumType, (e, d) => e.InsertMembers(d, 0, new[] { e.Generator.EnumMember("None") }), cancellationToken).ConfigureAwait(false);
        }

        protected virtual SyntaxNode GetParentNodeOrSelfToFix(SyntaxNode nodeToFix)
        {
            return nodeToFix;
        }

        private Document GetUpdatedDocumentWithFix(Document document, SyntaxNode root, SyntaxNode nodeToFix, IList<SyntaxNode> newFields, CancellationToken cancellationToken)
        {
            nodeToFix = GetParentNodeOrSelfToFix(nodeToFix);
            var g = SyntaxGenerator.GetGenerator(document);
            var newEnumSyntax = g.AddMembers(nodeToFix, newFields);
            var newRoot = root.ReplaceNode(nodeToFix, newEnumSyntax);
            return document.WithSyntaxRoot(newRoot);
        }

        internal sealed override async Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            ISymbol declaredSymbol = model.GetDeclaredSymbol(nodeToFix, cancellationToken);
            Debug.Assert(declaredSymbol != null);

            var editor = SymbolEditor.Create(document);

            foreach (var customTag in diagnostic.Descriptor.CustomTags)
            {
                switch (customTag)
                {
                    case CA1008DiagnosticAnalyzer.RuleRenameCustomTag:
                        return await GetUpdatedDocumentForRuleNameRenameAsync(document, (IFieldSymbol)declaredSymbol, cancellationToken).ConfigureAwait(false);

                    case CA1008DiagnosticAnalyzer.RuleMultipleZeroCustomTag:
                        await ApplyRuleNameMultipleZeroAsync(editor, (INamedTypeSymbol)declaredSymbol, cancellationToken).ConfigureAwait(false);
                        return editor.GetChangedDocuments().First();

                    case CA1008DiagnosticAnalyzer.RuleNoZeroCustomTag:
                        await ApplyRuleNameNoZeroValueAsync(editor, (INamedTypeSymbol)declaredSymbol, cancellationToken).ConfigureAwait(false);
                        return editor.GetChangedDocuments().First();
                }
            }

            return document;
        }
    }
}
