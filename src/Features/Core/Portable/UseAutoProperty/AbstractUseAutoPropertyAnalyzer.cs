// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseAutoProperty
{
    internal abstract class AbstractUseAutoPropertyAnalyzer<
        TPropertyDeclaration, TFieldDeclaration, TVariableDeclarator, TExpression> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TPropertyDeclaration : SyntaxNode
        where TFieldDeclaration : SyntaxNode
        where TVariableDeclarator : SyntaxNode
        where TExpression : SyntaxNode
    {
        private static readonly LocalizableString s_title =
            new LocalizableResourceString(nameof(FeaturesResources.Use_auto_property),
                FeaturesResources.ResourceManager, typeof(FeaturesResources));

        protected AbstractUseAutoPropertyAnalyzer()
            : base(IDEDiagnosticIds.UseAutoPropertyDiagnosticId, CodeStyleOptions.PreferAutoProperties, s_title, s_title)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected abstract void AnalyzeCompilationUnit(SemanticModelAnalysisContext context, SyntaxNode root, List<AnalysisResult> analysisResults);
        protected abstract bool SupportsReadOnlyProperties(Compilation compilation);
        protected abstract bool SupportsPropertyInitializer(Compilation compilation);
        protected abstract bool CanExplicitInterfaceImplementationsBeFixed();
        protected abstract TExpression GetFieldInitializer(TVariableDeclarator variable, CancellationToken cancellationToken);
        protected abstract TExpression GetGetterExpression(IMethodSymbol getMethod, CancellationToken cancellationToken);
        protected abstract TExpression GetSetterExpression(IMethodSymbol setMethod, SemanticModel semanticModel, CancellationToken cancellationToken);
        protected abstract SyntaxNode GetNodeToFade(TFieldDeclaration fieldDeclaration, TVariableDeclarator variableDeclarator);

        protected abstract void RegisterIneligibleFieldsAction(
            List<AnalysisResult> analysisResults, HashSet<IFieldSymbol> ineligibleFields,
            Compilation compilation, CancellationToken cancellationToken);

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSemanticModelAction(AnalyzeSemanticModel);

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;

            // Don't even bother doing the analysis if the user doesn't even want auto-props.
            var option = context.Options.GetOptionAsync(
                CodeStyleOptions.PreferAutoProperties, semanticModel.Language, semanticModel.SyntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (!option.Value)
            {
                return;
            }

            var analysisResults = new List<AnalysisResult>();
            var ineligibleFields = new HashSet<IFieldSymbol>();

            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
            AnalyzeCompilationUnit(context, root, analysisResults);

            RegisterIneligibleFieldsAction(
                analysisResults, ineligibleFields,
                semanticModel.Compilation, cancellationToken);
            Process(analysisResults, ineligibleFields, context);
        }

        protected void AnalyzeProperty(
            SemanticModelAnalysisContext context, TPropertyDeclaration propertyDeclaration, List<AnalysisResult> analysisResults)
        {
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;

            if (!(semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken) is IPropertySymbol property))
            {
                return;
            }

            if (property.IsIndexer)
            {
                return;
            }

            // The property can't be virtual.  We don't know if it is overridden somewhere.  If it 
            // is, then calls to it may not actually assign to the field.
            if (property.IsVirtual || property.IsOverride || property.IsSealed)
            {
                return;
            }

            if (property.IsWithEvents)
            {
                return;
            }

            if (property.Parameters.Length > 0)
            {
                return;
            }

            // Need at least a getter.
            if (property.GetMethod == null)
            {
                return;
            }

            if (!CanExplicitInterfaceImplementationsBeFixed() && property.ExplicitInterfaceImplementations.Length != 0)
            {
                return;
            }

            var containingType = property.ContainingType;
            if (containingType == null)
            {
                return;
            }

            var declarations = property.DeclaringSyntaxReferences;
            if (declarations.Length != 1)
            {
                return;
            }

            var getterField = GetGetterField(semanticModel, property.GetMethod, cancellationToken);
            if (getterField == null)
            {
                return;
            }

            if (getterField.DeclaredAccessibility != Accessibility.Private)
            {
                // Only support this for private fields.  It limits the scope of hte program
                // we have to analyze to make sure this is safe to do.
                return;
            }

            // If the user made the field readonly, we only want to convert it to a property if we
            // can keep it readonly.
            if (getterField.IsReadOnly && !SupportsReadOnlyProperties(semanticModel.Compilation))
            {
                return;
            }

            if (!containingType.Equals(getterField.ContainingType))
            {
                // Field and property have to be in the same type.
                return;
            }

            // Property and field have to agree on type.
            if (!property.Type.Equals(getterField.Type))
            {
                return;
            }

            // Mutable value type fields are mutable unless they are marked read-only
            if (!getterField.IsReadOnly && getterField.Type.IsMutableValueType() != false)
            {
                return;
            }

            // Don't want to remove constants and volatile fields.
            if (getterField.IsConst || getterField.IsVolatile)
            {
                return;
            }

            if (getterField.DeclaringSyntaxReferences.Length != 1)
            {
                return;
            }

            // Field and property should match in static-ness
            if (getterField.IsStatic != property.IsStatic)
            {
                return;
            }

            // A setter is optional though.
            var setMethod = property.SetMethod;
            if (setMethod != null)
            {
                var setterField = GetSetterField(semanticModel, containingType, setMethod, cancellationToken);
                if (setterField != getterField)
                {
                    // If there is a getter and a setter, they both need to agree on which field they are 
                    // writing to.
                    return;
                }
            }

            var fieldReference = getterField.DeclaringSyntaxReferences[0];
            if (!(fieldReference.GetSyntax(cancellationToken) is TVariableDeclarator variableDeclarator))
            {
                return;
            }

            var initializer = GetFieldInitializer(variableDeclarator, cancellationToken);
            if (initializer != null && !SupportsPropertyInitializer(semanticModel.Compilation))
            {
                return;
            }

            if (!(variableDeclarator?.Parent?.Parent is TFieldDeclaration fieldDeclaration))
            {
                return;
            }

            // Can't remove the field if it has attributes on it.
            if (getterField.GetAttributes().Length > 0)
            {
                return;
            }

            if (!CanConvert(property))
            {
                return;
            }

            // Looks like a viable property/field to convert into an auto property.
            analysisResults.Add(new AnalysisResult(property, getterField, propertyDeclaration,
                fieldDeclaration, variableDeclarator, property.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        protected virtual bool CanConvert(IPropertySymbol property)
            => true;

        private IFieldSymbol GetSetterField(
            SemanticModel semanticModel, ISymbol containingType, IMethodSymbol setMethod, CancellationToken cancellationToken)
        {
            return CheckFieldAccessExpression(semanticModel, GetSetterExpression(setMethod, semanticModel, cancellationToken));
        }

        private IFieldSymbol GetGetterField(
            SemanticModel semanticModel, IMethodSymbol getMethod, CancellationToken cancellationToken)
        {
            return CheckFieldAccessExpression(semanticModel, GetGetterExpression(getMethod, cancellationToken));
        }

        private IFieldSymbol CheckFieldAccessExpression(SemanticModel semanticModel, TExpression expression)
        {
            if (expression == null)
            {
                return null;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(expression);
            if (symbolInfo.Symbol == null || symbolInfo.Symbol.Kind != SymbolKind.Field)
            {
                return null;
            }

            var field = (IFieldSymbol)symbolInfo.Symbol;
            if (field.DeclaringSyntaxReferences.Length > 1)
            {
                return null;
            }

            return field;
        }

        private void Process(
            List<AnalysisResult> analysisResults,
            HashSet<IFieldSymbol> ineligibleFields,
            SemanticModelAnalysisContext context)
        {
            var ineligibleFieldsSet = new HashSet<IFieldSymbol>(ineligibleFields);
            foreach (var result in analysisResults)
            {
                var field = result.Field;
                if (ineligibleFieldsSet.Contains(field))
                {
                    continue;
                }

                Process(result, context);
            }
        }

        private void Process(AnalysisResult result, SemanticModelAnalysisContext context)
        {
            // Check if there are additional reasons we think this field might be ineligible for 
            // replacing with an auto prop.
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;
            var compilation = semanticModel.Compilation;

            if (!IsEligibleHeuristic(result.Field, result.PropertyDeclaration, compilation, cancellationToken))
            {
                return;
            }

            var propertyDeclaration = result.PropertyDeclaration;
            var variableDeclarator = result.VariableDeclarator;
            var nodeToFade = GetNodeToFade(result.FieldDeclaration, variableDeclarator);

            // Now add diagnostics to both the field and the property saying we can convert it to 
            // an auto property.  For each diagnostic store both location so we can easily retrieve
            // them when performing the code fix.
            var additionalLocations = ImmutableArray.Create(
                propertyDeclaration.GetLocation(), variableDeclarator.GetLocation());

            var option = context.Options.GetOptionAsync(
                CodeStyleOptions.PreferAutoProperties, propertyDeclaration.Language, result.FieldDeclaration.SyntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (option.Notification.Severity == ReportDiagnostic.Suppress)
            {
                // Avoid reporting diagnostics when the feature is disabled. This primarily avoids reporting the hidden
                // helper diagnostic which is not otherwise influenced by the severity settings.
                return;
            }

            // Place the appropriate marker on the field depending on the user option.
            var diagnostic1 = DiagnosticHelper.Create(
                UnnecessaryWithSuggestionDescriptor,
                nodeToFade.GetLocation(),
                option.Notification.Severity,
                additionalLocations: additionalLocations,
                properties: null);

            // Also, place a hidden marker on the property.  If they bring up a lightbulb
            // there, they'll be able to see that they can convert it to an auto-prop.
            var diagnostic2 = Diagnostic.Create(
                Descriptor, propertyDeclaration.GetLocation(),
                additionalLocations: additionalLocations);

            context.ReportDiagnostic(diagnostic1);
            context.ReportDiagnostic(diagnostic2);
        }

        protected virtual bool IsEligibleHeuristic(
            IFieldSymbol field, TPropertyDeclaration propertyDeclaration,
            Compilation compilation, CancellationToken cancellationToken)
        {
            return true;
        }

        internal class AnalysisResult
        {
            public readonly IPropertySymbol Property;
            public readonly IFieldSymbol Field;
            public readonly TPropertyDeclaration PropertyDeclaration;
            public readonly TFieldDeclaration FieldDeclaration;
            public readonly TVariableDeclarator VariableDeclarator;
            public readonly string SymbolEquivalenceKey;

            public AnalysisResult(
                IPropertySymbol property,
                IFieldSymbol field,
                TPropertyDeclaration propertyDeclaration,
                TFieldDeclaration fieldDeclaration,
                TVariableDeclarator variableDeclarator,
                string symbolEquivalenceKey)
            {
                Property = property;
                Field = field;
                PropertyDeclaration = propertyDeclaration;
                FieldDeclaration = fieldDeclaration;
                VariableDeclarator = variableDeclarator;
                SymbolEquivalenceKey = symbolEquivalenceKey;
            }
        }
    }
}
