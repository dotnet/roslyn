// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.UseAutoProperty
{
    internal static class Constants
    {
        public const string SymbolEquivalenceKey = nameof(SymbolEquivalenceKey);
    }

    internal abstract class AbstractUseAutoPropertyAnalyzer<TPropertyDeclaration, TFieldDeclaration, TVariableDeclarator, TExpression> :
        AbstractCodeStyleDiagnosticAnalyzer
        where TPropertyDeclaration : SyntaxNode
        where TFieldDeclaration : SyntaxNode
        where TVariableDeclarator : SyntaxNode
        where TExpression : SyntaxNode
    {
        private static readonly LocalizableString s_title =
            new LocalizableResourceString(nameof(FeaturesResources.Use_auto_property), FeaturesResources.ResourceManager, typeof(FeaturesResources));

        protected AbstractUseAutoPropertyAnalyzer()
            : base(IDEDiagnosticIds.UseAutoPropertyDiagnosticId, s_title, s_title)
        {
        }

        public override bool OpenFileOnly(Workspace workspace) => false;
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.ProjectAnalysis;

        protected abstract void RegisterIneligibleFieldsAction(CompilationStartAnalysisContext context, ConcurrentBag<IFieldSymbol> ineligibleFields);
        protected abstract bool SupportsReadOnlyProperties(Compilation compilation);
        protected abstract bool SupportsPropertyInitializer(Compilation compilation);
        protected abstract TExpression GetFieldInitializer(TVariableDeclarator variable, CancellationToken cancellationToken);
        protected abstract TExpression GetGetterExpression(IMethodSymbol getMethod, CancellationToken cancellationToken);
        protected abstract TExpression GetSetterExpression(IMethodSymbol setMethod, SemanticModel semanticModel, CancellationToken cancellationToken);
        protected abstract SyntaxNode GetNodeToFade(TFieldDeclaration fieldDeclaration, TVariableDeclarator variableDeclarator);

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(csac =>
               {
                   var analysisResults = new ConcurrentBag<AnalysisResult>();
                   var ineligibleFields = new ConcurrentBag<IFieldSymbol>();

                   csac.RegisterSymbolAction(sac => AnalyzeProperty(analysisResults, sac), SymbolKind.Property);
                   RegisterIneligibleFieldsAction(csac, ineligibleFields);

                   csac.RegisterCompilationEndAction(cac => Process(analysisResults, ineligibleFields, cac));
               });

        private void AnalyzeProperty(ConcurrentBag<AnalysisResult> analysisResults, SymbolAnalysisContext symbolContext)
        {
            var property = (IPropertySymbol)symbolContext.Symbol;
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

            var cancellationToken = symbolContext.CancellationToken;
            var propertyDeclaration = property.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken).FirstAncestorOrSelf<TPropertyDeclaration>();
            if (propertyDeclaration == null)
            {
                return;
            }

            var semanticModel = symbolContext.Compilation.GetSemanticModel(propertyDeclaration.SyntaxTree);
            var getterField = GetGetterField(semanticModel, property.GetMethod, cancellationToken);
            if (getterField == null)
            {
                return;
            }

            // If the user made the field readonly, we only want to convert it to a property if we
            // can keep it readonly.
            if (getterField.IsReadOnly && !SupportsReadOnlyProperties(symbolContext.Compilation))
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

            // Don't want to remove constants.
            if (getterField.IsConst)
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
            var variableDeclarator = fieldReference.GetSyntax(symbolContext.CancellationToken) as TVariableDeclarator;
            if (variableDeclarator == null)
            {
                return;
            }

            var initializer = GetFieldInitializer(variableDeclarator, cancellationToken);
            if (initializer != null && !SupportsPropertyInitializer(symbolContext.Compilation))
            {
                return;
            }

            var fieldDeclaration = variableDeclarator?.Parent?.Parent as TFieldDeclaration;
            if (fieldDeclaration == null)
            {
                return;
            }

            // Can't remove the field if it has attributes on it.
            if (getterField.GetAttributes().Length > 0)
            {
                return;
            }

            // Looks like a viable property/field to convert into an auto property.
            analysisResults.Add(new AnalysisResult(property, getterField, propertyDeclaration, fieldDeclaration, variableDeclarator,
                property.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        private IFieldSymbol GetSetterField(
            SemanticModel semanticModel, ISymbol containingType, IMethodSymbol setMethod, CancellationToken cancellationToken)
        {
            return CheckFieldAccessExpression(semanticModel, GetSetterExpression(setMethod, semanticModel, cancellationToken));
        }

        private IFieldSymbol GetGetterField(SemanticModel semanticModel, IMethodSymbol getMethod, CancellationToken cancellationToken)
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
            ConcurrentBag<AnalysisResult> analysisResults,
            ConcurrentBag<IFieldSymbol> ineligibleFields,
            CompilationAnalysisContext compilationContext)
        {
            var ineligibleFieldsSet = new HashSet<IFieldSymbol>(ineligibleFields);
            foreach (var result in analysisResults)
            {
                var field = result.Field;
                if (ineligibleFieldsSet.Contains(field))
                {
                    continue;
                }

                Process(result, compilationContext);
            }
        }

        private void Process(AnalysisResult result, CompilationAnalysisContext compilationContext)
        {
            // Check if there are additional reasons we think this field might be ineligible for 
            // replacing with an auto prop.
            if (!IsEligibleHeuristic(result.Field, result.PropertyDeclaration, compilationContext.Compilation, compilationContext.CancellationToken))
            {
                return;
            }

            var propertyDeclaration = result.PropertyDeclaration;
            var variableDeclarator = result.VariableDeclarator;
            var nodeToFade = GetNodeToFade(result.FieldDeclaration, variableDeclarator);

            var properties = ImmutableDictionary<string, string>.Empty.Add(
                Constants.SymbolEquivalenceKey, result.SymbolEquivalenceKey);

            var cancellationToken = compilationContext.CancellationToken;
            var optionSet = compilationContext.Options.GetDocumentOptionSetAsync(
                result.FieldDeclaration.SyntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            // Always offer the conversion if possible.  But only let the user know (by 
            // fading/suggestions/squiggles) if their option is set to see this.

            var option = optionSet.GetOption(CodeStyleOptions.PreferAutoProperties, propertyDeclaration.Language);
            if (option.Value)
            {
                // Fade out the field/variable we are going to remove.
                var diagnostic1 = Diagnostic.Create(UnnecessaryWithoutSuggestionDescriptor, nodeToFade.GetLocation());
                compilationContext.ReportDiagnostic(diagnostic1);
            }

            // Now add diagnostics to both the field and the property saying we can convert it to 
            // an auto property.  For each diagnostic store both location so we can easily retrieve
            // them when performing the code fix.
            var additionalLocations = ImmutableArray.Create(
                propertyDeclaration.GetLocation(), variableDeclarator.GetLocation());

            var diagnostic2 = Diagnostic.Create(HiddenDescriptor, propertyDeclaration.GetLocation(),
                additionalLocations: additionalLocations, properties: properties);
            compilationContext.ReportDiagnostic(diagnostic2);

            // Place the appropriate marker on the field depending on the user option.
            var diagnostic3 = Diagnostic.Create(
                GetFieldDescriptor(option), nodeToFade.GetLocation(),
                additionalLocations: additionalLocations, properties: properties);
            compilationContext.ReportDiagnostic(diagnostic3);
        }

        private DiagnosticDescriptor GetFieldDescriptor(CodeStyleOption<bool> styleOption)
        {
            if (styleOption.Value)
            {
                switch (styleOption.Notification.Value)
                {
                    case DiagnosticSeverity.Error: return ErrorDescriptor;
                    case DiagnosticSeverity.Warning: return WarningDescriptor;
                    case DiagnosticSeverity.Info: return InfoDescriptor;
                }
            }

            return HiddenDescriptor;
        }

        protected virtual bool IsEligibleHeuristic(IFieldSymbol field, TPropertyDeclaration propertyDeclaration, Compilation compilation, CancellationToken cancellationToken)
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
