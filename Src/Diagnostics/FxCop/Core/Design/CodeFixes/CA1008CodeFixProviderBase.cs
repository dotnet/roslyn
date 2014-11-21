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

        internal abstract SyntaxNode GetFieldInitializer(IFieldSymbol field);
        internal abstract SyntaxNode CreateConstantValueInitializer(SyntaxNode constantValueExpression);

        private IFieldSymbol GetExplicitlyAssignedField(IFieldSymbol originalField, SyntaxGenerator syntaxFactoryService)
        {
            var originalInitializer = GetFieldInitializer(originalField);
            if (originalInitializer != null || !originalField.HasConstantValue)
            {
                return originalField;
            }

            var constantValueExpression = syntaxFactoryService.LiteralExpression(originalField.ConstantValue);
            var newInitializer = CreateConstantValueInitializer(constantValueExpression);

            return CodeGenerationSymbolFactory.CreateFieldSymbol(originalField.GetAttributes(), originalField.DeclaredAccessibility, originalField.GetSymbolModifiers(),
                originalField.Type, originalField.Name, originalField.HasConstantValue, originalField.ConstantValue, newInitializer);
        }

        private async Task<Document> GetUpdatedDocumentForRuleNameRenameAsync(Document document, IFieldSymbol field, CancellationToken cancellationToken)
        {
            var newSolution = await Rename.Renamer.RenameSymbolAsync(document.Project.Solution, field, "None", null).ConfigureAwait(false);
            return newSolution.GetDocument(document.Id);
        }

        private IList<ISymbol> GetNewFieldsForRuleNameMultipleZero(INamedTypeSymbol enumType, IEnumerable<IFieldSymbol> zeroValuedFields, SyntaxGenerator syntaxFactoryService)
        {
            // Diagnostic: Remove all members that have the value zero from '{0}' except for one member that is named 'None'.
            // Fix: Remove all members that have the value zero except for one member that is named 'None'.

            bool needsNewZeroValuedNoneField = true;
            var set = zeroValuedFields.ToSet();

            bool makeNextFieldExplicit = false;
            var newFields = new List<ISymbol>();
            foreach (IFieldSymbol field in enumType.GetMembers().Where(m => m.Kind == SymbolKind.Field))
            {
                var isZeroValued = set.Contains(field);
                var isZeroValuedNamedNone = isZeroValued && CA1008DiagnosticAnalyzer.IsMemberNamedNone(field);

                if (!isZeroValued || isZeroValuedNamedNone)
                {
                    var newField = field;
                    if (makeNextFieldExplicit)
                    {
                        newField = GetExplicitlyAssignedField(field, syntaxFactoryService);
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
                var constantValueExpression = syntaxFactoryService.LiteralExpression(firstZeroValuedField.ConstantValue);
                var newInitializer = CreateConstantValueInitializer(constantValueExpression);
                var newField = CodeGenerationSymbolFactory.CreateFieldSymbol(firstZeroValuedField.GetAttributes(), firstZeroValuedField.DeclaredAccessibility, firstZeroValuedField.GetSymbolModifiers(),
                    firstZeroValuedField.Type, "None", firstZeroValuedField.HasConstantValue, firstZeroValuedField.ConstantValue, newInitializer);
                newFields.Insert(0, newField);
            }

            return newFields;
        }

        private Document GetUpdatedDocumentForRuleNameMultipleZero(Document document, SyntaxNode root, SyntaxNode nodeToFix, INamedTypeSymbol enumType, IEnumerable<IFieldSymbol> zeroValuedFields, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(zeroValuedFields.Count() > 1);

            var syntaxFactoryService = document.GetLanguageService<SyntaxGenerator>();
            var newFields = GetNewFieldsForRuleNameMultipleZero(enumType, zeroValuedFields, syntaxFactoryService);
            return GetUpdatedDocumentWithFix(document, root, nodeToFix, newFields, cancellationToken);
        }

        private IList<ISymbol> GetNewFieldsForRuleNameNoZeroValue(INamedTypeSymbol enumType, SyntaxGenerator syntaxFactoryService)
        {
            // Diagnostic: Add a member to '{0}' that has a value of zero with a suggested name of 'None'.
            // Fix: Add a zero-valued member 'None' to enum.

            var newFields = new List<ISymbol>();
            var constantValueExpression = syntaxFactoryService.LiteralExpression(0);
            var newInitializer = CreateConstantValueInitializer(constantValueExpression);
            var newField = CodeGenerationSymbolFactory.CreateFieldSymbol(SpecializedCollections.EmptyList<AttributeData>(), Accessibility.Public,
                    default(DeclarationModifiers), enumType.EnumUnderlyingType, "None", true, 0, newInitializer);
            newFields.Add(newField);

            foreach (var member in enumType.GetMembers())
            {
                if (!CA1008DiagnosticAnalyzer.IsMemberNamedNone(member))
                {
                    newFields.Add(member);
                }
            }

            return newFields;
        }

        private Document GetUpdatedDocumentForRuleNameNoZeroValue(Document document, SyntaxNode root, SyntaxNode nodeToFix, INamedTypeSymbol enumType, CancellationToken cancellationToken)
        {
            var syntaxFactoryService = document.GetLanguageService<SyntaxGenerator>();
            var newFields = GetNewFieldsForRuleNameNoZeroValue(enumType, syntaxFactoryService);
            return GetUpdatedDocumentWithFix(document, root, nodeToFix, newFields, cancellationToken);
        }

        protected virtual SyntaxNode GetParentNodeOrSelfToFix(SyntaxNode nodeToFix)
        {
            return nodeToFix;
        }

        private Document GetUpdatedDocumentWithFix(Document document, SyntaxNode root, SyntaxNode nodeToFix, IList<ISymbol> newFields, CancellationToken cancellationToken)
        {
            nodeToFix = GetParentNodeOrSelfToFix(nodeToFix);
            var newEnumSyntax = CodeGenerator.UpdateDeclarationMembers(nodeToFix, document.Project.Solution.Workspace, newFields, cancellationToken: cancellationToken)
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