// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseAutoProperty
{
    internal abstract class AbstractUseAutoPropertyAnalyzer<
        TSyntaxKind,
        TPropertyDeclaration,
        TFieldDeclaration,
        TVariableDeclarator,
        TExpression> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct, Enum
        where TPropertyDeclaration : SyntaxNode
        where TFieldDeclaration : SyntaxNode
        where TVariableDeclarator : SyntaxNode
        where TExpression : SyntaxNode
    {
        protected AbstractUseAutoPropertyAnalyzer()
            : base(IDEDiagnosticIds.UseAutoPropertyDiagnosticId,
                   EnforceOnBuildValues.UseAutoProperty,
                   CodeStyleOptions2.PreferAutoProperties,
                   new LocalizableResourceString(nameof(AnalyzersResources.Use_auto_property), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Use_auto_property), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        /// <summary>
        /// A method body edit anywhere in a type will force us to reanalyze the whole type.
        /// </summary>
        /// <returns></returns>
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected abstract TSyntaxKind PropertyDeclarationKind { get; }
        protected abstract bool SupportsReadOnlyProperties(Compilation compilation);
        protected abstract bool SupportsPropertyInitializer(Compilation compilation);
        protected abstract bool CanExplicitInterfaceImplementationsBeFixed();
        protected abstract TExpression? GetFieldInitializer(TVariableDeclarator variable, CancellationToken cancellationToken);
        protected abstract TExpression? GetGetterExpression(IMethodSymbol getMethod, CancellationToken cancellationToken);
        protected abstract TExpression? GetSetterExpression(IMethodSymbol setMethod, SemanticModel semanticModel, CancellationToken cancellationToken);
        protected abstract SyntaxNode GetFieldNode(TFieldDeclaration fieldDeclaration, TVariableDeclarator variableDeclarator);

        protected abstract void RegisterIneligibleFieldsAction(
            HashSet<string> fieldNames, ConcurrentSet<IFieldSymbol> ineligibleFields, SemanticModel semanticModel, SyntaxNode codeBlock, CancellationToken cancellationToken);

        protected abstract void RegisterNonConstructorFieldWrites(
            HashSet<string> fieldNames, ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> fieldWrites, SemanticModel semanticModel, SyntaxNode codeBlock, CancellationToken cancellationToken);

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSymbolStartAction(context =>
            {
                var namedType = (INamedTypeSymbol)context.Symbol;
                if (namedType.TypeKind is not TypeKind.Class and not TypeKind.Struct and not TypeKind.Module)
                    return;

                var analysisResults = new ConcurrentQueue<AnalysisResult>();
                context.RegisterSyntaxNodeAction(context => AnalyzePropertyDeclaration(context, namedType, analysisResults), PropertyDeclarationKind);

                var ineligibleFields = new ConcurrentSet<IFieldSymbol>();
                var nonConstructorFieldWrites = new ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>>();

                var fieldNames = namedType.GetMembers().OfType<IFieldSymbol>().Select(f => f.Name).ToHashSet();
                context.RegisterCodeBlockStartAction<TSyntaxKind>(context =>
                {
                    RegisterIneligibleFieldsAction(fieldNames, ineligibleFields, context.SemanticModel, context.CodeBlock, context.CancellationToken);
                    RegisterNonConstructorFieldWrites(fieldNames, nonConstructorFieldWrites, context.SemanticModel, context.CodeBlock, context.CancellationToken);
                });

                context.RegisterSymbolEndAction(context =>
                    Process(analysisResults, ineligibleFields, nonConstructorFieldWrites, context));
            }, SymbolKind.NamedType);

        private void AnalyzePropertyDeclaration(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol containingType,
            ConcurrentQueue<AnalysisResult> analysisResults)
        {
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;
            var compilation = semanticModel.Compilation;

            var propertyDeclaration = (TPropertyDeclaration)context.Node;
            if (semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken) is not IPropertySymbol property)
                return;

            if (!containingType.Equals(property.ContainingType))
                return;

            if (property.IsIndexer)
                return;

            // The property can't be virtual.  We don't know if it is overridden somewhere.  If it 
            // is, then calls to it may not actually assign to the field.
            if (property.IsVirtual || property.IsOverride || property.IsSealed)
                return;

            if (property.IsWithEvents)
                return;

            if (property.Parameters.Length > 0)
                return;

            // Need at least a getter.
            if (property.GetMethod == null)
                return;

            if (!CanExplicitInterfaceImplementationsBeFixed() && property.ExplicitInterfaceImplementations.Length != 0)
                return;

            // Serializable types can depend on fields (and their order).  Don't report these
            // properties in that case.
            if (containingType.IsSerializable)
                return;

            var preferAutoProps = context.GetAnalyzerOptions().PreferAutoProperties;
            if (!preferAutoProps.Value)
                return;

            // Avoid reporting diagnostics when the feature is disabled. This primarily avoids reporting the hidden
            // helper diagnostic which is not otherwise influenced by the severity settings.
            var severity = preferAutoProps.Notification.Severity;
            if (severity == ReportDiagnostic.Suppress)
                return;

            var getterField = GetGetterField(semanticModel, property.GetMethod, cancellationToken);
            if (getterField == null)
                return;

            // Only support this for private fields.  It limits the scope of hte program
            // we have to analyze to make sure this is safe to do.
            if (getterField.DeclaredAccessibility != Accessibility.Private)
                return;

            // If the user made the field readonly, we only want to convert it to a property if we
            // can keep it readonly.
            if (getterField.IsReadOnly && !SupportsReadOnlyProperties(compilation))
                return;

            // Field and property have to be in the same type.
            if (!containingType.Equals(getterField.ContainingType))
                return;

            // Property and field have to agree on type.
            if (!property.Type.Equals(getterField.Type))
                return;

            // Mutable value type fields are mutable unless they are marked read-only
            if (!getterField.IsReadOnly && getterField.Type.IsMutableValueType() != false)
                return;

            // Don't want to remove constants and volatile fields.
            if (getterField.IsConst || getterField.IsVolatile)
                return;

            // Field and property should match in static-ness
            if (getterField.IsStatic != property.IsStatic)
                return;

            var fieldReference = getterField.DeclaringSyntaxReferences[0];
            if (fieldReference.GetSyntax(cancellationToken) is not TVariableDeclarator { Parent.Parent: TFieldDeclaration fieldDeclaration } variableDeclarator)
                return;

            // A setter is optional though.
            var setMethod = property.SetMethod;
            if (setMethod != null)
            {
                var setterField = GetSetterField(semanticModel, setMethod, cancellationToken);
                // If there is a getter and a setter, they both need to agree on which field they are 
                // writing to.
                if (setterField != getterField)
                    return;
            }

            var initializer = GetFieldInitializer(variableDeclarator, cancellationToken);
            if (initializer != null && !SupportsPropertyInitializer(compilation))
                return;

            // Can't remove the field if it has attributes on it.
            var attributes = getterField.GetAttributes();
            var suppressMessageAttributeType = compilation.SuppressMessageAttributeType();
            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass != suppressMessageAttributeType)
                    return;
            }

            if (!CanConvert(property))
                return;

            // Looks like a viable property/field to convert into an auto property.
            analysisResults.Enqueue(new AnalysisResult(property, getterField, propertyDeclaration, fieldDeclaration, variableDeclarator, severity));
        }

        protected virtual bool CanConvert(IPropertySymbol property)
            => true;

        private IFieldSymbol? GetSetterField(SemanticModel semanticModel, IMethodSymbol setMethod, CancellationToken cancellationToken)
            => CheckFieldAccessExpression(semanticModel, GetSetterExpression(setMethod, semanticModel, cancellationToken), cancellationToken);

        private IFieldSymbol? GetGetterField(SemanticModel semanticModel, IMethodSymbol getMethod, CancellationToken cancellationToken)
            => CheckFieldAccessExpression(semanticModel, GetGetterExpression(getMethod, cancellationToken), cancellationToken);

        private static IFieldSymbol? CheckFieldAccessExpression(SemanticModel semanticModel, TExpression? expression, CancellationToken cancellationToken)
        {
            if (expression == null)
                return null;

            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            return symbolInfo.Symbol is IFieldSymbol { DeclaringSyntaxReferences.Length: 1 } field
                ? field
                : null;
        }

        private void Process(
            ConcurrentQueue<AnalysisResult> analysisResults,
            ConcurrentSet<IFieldSymbol> ineligibleFields,
            ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> nonConstructorFieldWrites,
            SymbolAnalysisContext context)
        {
            foreach (var result in analysisResults)
            {
                // C# specific check.
                if (ineligibleFields.Contains(result.Field))
                    continue;

                // VB specific check.
                //
                // if the property doesn't have a setter currently.check all the types the field is declared in.  If the
                // field is written to outside of a constructor, then this field Is Not eligible for replacement with an
                // auto prop.  We'd have to make the autoprop read/write, And that could be opening up the property
                // widely (in accessibility terms) in a way the user would not want.
                if (result.Property.DeclaredAccessibility != Accessibility.Private &&
                    result.Property.SetMethod is null &&
                    nonConstructorFieldWrites.TryGetValue(result.Field, out var writeLocations) &&
                    writeLocations.Any(loc => !loc.Ancestors().Contains(result.PropertyDeclaration)))
                {
                    continue;
                }

                Process(result, context);
            }
        }

        private void Process(AnalysisResult result, SymbolAnalysisContext context)
        {
            var propertyDeclaration = result.PropertyDeclaration;
            var variableDeclarator = result.VariableDeclarator;
            var fieldNode = GetFieldNode(result.FieldDeclaration, variableDeclarator);

            // Now add diagnostics to both the field and the property saying we can convert it to 
            // an auto property.  For each diagnostic store both location so we can easily retrieve
            // them when performing the code fix.
            var additionalLocations = ImmutableArray.Create(
                propertyDeclaration.GetLocation(),
                variableDeclarator.GetLocation());

            // Place the appropriate marker on the field depending on the user option.
            var diagnostic1 = DiagnosticHelper.Create(
                Descriptor,
                fieldNode.GetLocation(),
                result.Severity,
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

        private sealed record AnalysisResult(
            IPropertySymbol Property,
            IFieldSymbol Field,
            TPropertyDeclaration PropertyDeclaration,
            TFieldDeclaration FieldDeclaration,
            TVariableDeclarator VariableDeclarator,
            ReportDiagnostic Severity);
    }
}
