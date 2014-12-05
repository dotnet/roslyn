// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1008: Enums should have zero value
    /// </summary>
    public abstract class CA1008CodeFixProviderBase : CodeFixProviderBase
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(CA1008DiagnosticAnalyzer.RuleId);
        }

        protected sealed override string GetCodeFixDescription(Diagnostic diagnostic)
        {
            foreach (var customTag in diagnostic.Descriptor.CustomTags)
            {
                switch (customTag)
                {
                    case CA1008DiagnosticAnalyzer.RuleRenameCustomTag:
                        return FxCopFixersResources.EnumsShouldZeroValueFlagsRenameCodeFix;

                    case CA1008DiagnosticAnalyzer.RuleMultipleZeroCustomTag:
                        return FxCopFixersResources.EnumsShouldZeroValueFlagsMultipleZeroCodeFix;

                    case CA1008DiagnosticAnalyzer.RuleNoZeroCustomTag:
                        return FxCopFixersResources.EnumsShouldZeroValueNotFlagsNoZeroValueCodeFix;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static SyntaxNode GetDeclaration(ISymbol symbol)
        {
            return (symbol.DeclaringSyntaxReferences.Length > 0) ? symbol.DeclaringSyntaxReferences[0].GetSyntax() : null;
        }

        private SyntaxNode GetExplicitlyAssignedField(IFieldSymbol originalField, SyntaxGenerator generator)
        {
            var originalDeclaration = GetDeclaration(originalField);

            var originalInitializer = generator.GetInitializer(originalDeclaration);
            if (originalInitializer != null || !originalField.HasConstantValue)
            {
                return originalDeclaration;
            }

            return generator.WithInitializer(originalDeclaration, generator.LiteralExpression(originalField.ConstantValue));
        }

        private async Task<Document> GetUpdatedDocumentForRuleNameRenameAsync(Document document, IFieldSymbol field, CancellationToken cancellationToken)
        {
            var newSolution = await Rename.Renamer.RenameSymbolAsync(document.Project.Solution, field, "None", null).ConfigureAwait(false);
            return newSolution.GetDocument(document.Id);
        }

        private IList<SyntaxNode> GetNewFieldsForRuleNameMultipleZero(INamedTypeSymbol enumType, IEnumerable<IFieldSymbol> zeroValuedFields, SyntaxGenerator generator)
        {
            // Diagnostic: Remove all members that have the value zero from '{0}' except for one member that is named 'None'.
            // Fix: Remove all members that have the value zero except for one member that is named 'None'.

            bool needsNewZeroValuedNoneField = true;
            var set = zeroValuedFields.ToSet();

            bool makeNextFieldExplicit = false;
            var newFields = new List<SyntaxNode>();
            foreach (IFieldSymbol field in enumType.GetMembers().Where(m => m.Kind == SymbolKind.Field))
            {
                var isZeroValued = set.Contains(field);
                var isZeroValuedNamedNone = isZeroValued && CA1008DiagnosticAnalyzer.IsMemberNamedNone(field);

                if (!isZeroValued || isZeroValuedNamedNone)
                {
                    var newField = GetDeclaration(field);
                    if (makeNextFieldExplicit)
                    {
                        newField = GetExplicitlyAssignedField(field, generator);
                        makeNextFieldExplicit = false;
                    }

                    newFields.Add(newField);

                    if (isZeroValuedNamedNone)
                    {
                        needsNewZeroValuedNoneField = false;
                    }
                }
                else
                {
                    makeNextFieldExplicit = true;
                }
            }

            if (needsNewZeroValuedNoneField)
            {
                var firstZeroValuedField = zeroValuedFields.First();
                var newField = generator.EnumMember("None");
                newFields.Insert(0, newField);
            }

            return newFields;
        }

        private Document GetUpdatedDocumentForRuleNameMultipleZero(Document document, SyntaxNode root, SyntaxNode nodeToFix, INamedTypeSymbol enumType, IEnumerable<IFieldSymbol> zeroValuedFields, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(zeroValuedFields.Count() > 1);

            var generator = SyntaxGenerator.GetGenerator(document);
            var newFields = GetNewFieldsForRuleNameMultipleZero(enumType, zeroValuedFields, generator);
            return GetUpdatedDocumentWithFix(document, root, nodeToFix, newFields, cancellationToken);
        }

        private IList<SyntaxNode> GetNewFieldsForRuleNameNoZeroValue(INamedTypeSymbol enumType, SyntaxGenerator generator)
        {
            // Diagnostic: Add a member to '{0}' that has a value of zero with a suggested name of 'None'.
            // Fix: Add a zero-valued member 'None' to enum.

            var newFields = new List<SyntaxNode>();
            var newField = generator.EnumMember("None");
            newFields.Add(newField);

            foreach (var member in enumType.GetMembers().Where(m => m.Kind == SymbolKind.Field))
            {
                if (!CA1008DiagnosticAnalyzer.IsMemberNamedNone(member))
                {
                    var decl = GetDeclaration(member);
                    if (decl != null)
                    {
                        newFields.Add(decl);
                    }
                }
            }

            return newFields;
        }

        private Document GetUpdatedDocumentForRuleNameNoZeroValue(Document document, SyntaxNode root, SyntaxNode nodeToFix, INamedTypeSymbol enumType, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var newFields = GetNewFieldsForRuleNameNoZeroValue(enumType, generator);
            return GetUpdatedDocumentWithFix(document, root, nodeToFix, newFields, cancellationToken);
        }

        protected virtual SyntaxNode GetParentNodeOrSelfToFix(SyntaxNode nodeToFix)
        {
            return nodeToFix;
        }

        private Document GetUpdatedDocumentWithFix(Document document, SyntaxNode root, SyntaxNode nodeToFix, IList<SyntaxNode> newFields, CancellationToken cancellationToken)
        {
            nodeToFix = GetParentNodeOrSelfToFix(nodeToFix);
            var g = SyntaxGenerator.GetGenerator(document);
            var newEnumSyntax = g.WithMembers(nodeToFix, newFields)
                                 .WithAdditionalAnnotations(Formatting.Formatter.Annotation);
            var newRoot = root.ReplaceNode(nodeToFix, newEnumSyntax);
            return document.WithSyntaxRoot(newRoot);
        }

        internal sealed override async Task<Document> GetUpdatedDocumentAsync(Document document, SemanticModel model, SyntaxNode root, SyntaxNode nodeToFix, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            ISymbol declaredSymbol = model.GetDeclaredSymbol(nodeToFix, cancellationToken);
            Contract.ThrowIfNull(declaredSymbol);

            foreach (var customTag in diagnostic.Descriptor.CustomTags)
            {
                switch (customTag)
                {
                    case CA1008DiagnosticAnalyzer.RuleRenameCustomTag:
                        return await GetUpdatedDocumentForRuleNameRenameAsync(document, (IFieldSymbol)declaredSymbol, cancellationToken).ConfigureAwait(false);

                    case CA1008DiagnosticAnalyzer.RuleMultipleZeroCustomTag:
                        var enumType = (INamedTypeSymbol)declaredSymbol;
                        var zeroValuedFields = CA1008DiagnosticAnalyzer.GetZeroValuedFields(enumType);
                        return GetUpdatedDocumentForRuleNameMultipleZero(document, root, nodeToFix, enumType, zeroValuedFields, cancellationToken);

                    case CA1008DiagnosticAnalyzer.RuleNoZeroCustomTag:
                        return GetUpdatedDocumentForRuleNameNoZeroValue(document, root, nodeToFix, (INamedTypeSymbol)declaredSymbol, cancellationToken);
                }
            }

            return document;
        }
    }
}