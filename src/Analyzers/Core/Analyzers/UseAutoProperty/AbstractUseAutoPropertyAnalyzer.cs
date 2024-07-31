﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseAutoProperty;

internal readonly record struct AccessedFields(
    IFieldSymbol? TrivialField,
    ImmutableArray<IFieldSymbol> NonTrivialFields)
{
    public AccessedFields(IFieldSymbol? trivialField) : this(trivialField, [])
    {
    }

    public bool IsEmpty => TrivialField is null && NonTrivialFields.IsDefaultOrEmpty;

    public AccessedFields Where<TArg>(Func<IFieldSymbol, TArg, bool> predicate, TArg arg)
        => new(TrivialField != null && predicate(TrivialField, arg) ? TrivialField : null,
               NonTrivialFields.NullToEmpty().WhereAsArray(predicate, arg));

    public bool Contains(IFieldSymbol field)
        => Equals(TrivialField, field) || NonTrivialFields.NullToEmpty().Contains(field);
}

internal abstract class AbstractUseAutoPropertyAnalyzer<
    TSyntaxKind,
    TPropertyDeclaration,
    TConstructorDeclaration,
    TFieldDeclaration,
    TVariableDeclarator,
    TExpression,
    TIdentifierName> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TSyntaxKind : struct, Enum
    where TPropertyDeclaration : SyntaxNode
    where TConstructorDeclaration : SyntaxNode
    where TFieldDeclaration : SyntaxNode
    where TVariableDeclarator : SyntaxNode
    where TExpression : SyntaxNode
    where TIdentifierName : TExpression
{
    /// <summary>
    /// ConcurrentStack as that's the only concurrent collection that supports 'Clear' in netstandard2.
    /// </summary>
    private static readonly ObjectPool<ConcurrentStack<AnalysisResult>> s_analysisResultPool = new(() => new());
    private static readonly ObjectPool<ConcurrentSet<SyntaxNode>> s_nodeSetPool = new(() => []);

    private static readonly ObjectPool<ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>>> s_fieldToUsageLocationPool = new(() => []);

    private static readonly Func<IFieldSymbol, ConcurrentSet<SyntaxNode>> s_createFieldUsageSet = _ => s_nodeSetPool.Allocate();

    /// <summary>
    /// Not static as this has different semantics around case sensitivity for C# and VB.
    /// </summary>
    private readonly ObjectPool<HashSet<string>> _fieldNamesPool;

    protected AbstractUseAutoPropertyAnalyzer()
        : base(IDEDiagnosticIds.UseAutoPropertyDiagnosticId,
               EnforceOnBuildValues.UseAutoProperty,
               CodeStyleOptions2.PreferAutoProperties,
               new LocalizableResourceString(nameof(AnalyzersResources.Use_auto_property), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
               new LocalizableResourceString(nameof(AnalyzersResources.Use_auto_property), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
        _fieldNamesPool = new(() => new(this.SyntaxFacts.StringComparer));
    }

    protected static void AddFieldUsage(ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> fieldWrites, IFieldSymbol field, SyntaxNode location)
        => fieldWrites.GetOrAdd(field, s_createFieldUsageSet).Add(location);

    private static void ClearAndFree(ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> multiMap)
    {
        foreach (var (_, nodeSet) in multiMap)
            s_nodeSetPool.ClearAndFree(nodeSet);

        s_fieldToUsageLocationPool.ClearAndFree(multiMap);
    }

    /// <summary>
    /// A method body edit anywhere in a type will force us to reanalyze the whole type.
    /// </summary>
    /// <returns></returns>
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected abstract ISemanticFacts SemanticFacts { get; }
    protected ISyntaxFacts SyntaxFacts => this.SemanticFacts.SyntaxFacts;

    protected abstract TSyntaxKind PropertyDeclarationKind { get; }

    protected abstract bool SupportsReadOnlyProperties(Compilation compilation);
    protected abstract bool SupportsPropertyInitializer(Compilation compilation);
    protected abstract bool SupportsSemiAutoProperty(Compilation compilation);

    protected abstract bool CanExplicitInterfaceImplementationsBeFixed();
    protected abstract TExpression? GetFieldInitializer(TVariableDeclarator variable, CancellationToken cancellationToken);
    protected abstract TExpression? GetGetterExpression(IMethodSymbol getMethod, CancellationToken cancellationToken);
    protected abstract TExpression? GetSetterExpression(SemanticModel semanticModel, IMethodSymbol setMethod, CancellationToken cancellationToken);
    protected abstract SyntaxNode GetFieldNode(TFieldDeclaration fieldDeclaration, TVariableDeclarator variableDeclarator);
    protected abstract void AddAccessedFields(
        SemanticModel semanticModel, IMethodSymbol accessor, HashSet<string> fieldNames, HashSet<IFieldSymbol> result, CancellationToken cancellationToken);

    protected abstract void RegisterIneligibleFieldsAction(
        HashSet<string> fieldNames, ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> ineligibleFields, SemanticModel semanticModel, SyntaxNode codeBlock, CancellationToken cancellationToken);

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterSymbolStartAction(context =>
        {
            var namedType = (INamedTypeSymbol)context.Symbol;
            if (!ShouldAnalyze(context, namedType))
                return;

            var fieldNames = _fieldNamesPool.Allocate();
            var analysisResults = s_analysisResultPool.Allocate();
            var ineligibleFields = s_fieldToUsageLocationPool.Allocate();
            var nonConstructorFieldWrites = s_fieldToUsageLocationPool.Allocate();

            // Record the names of all the private fields in this type.  We can use this to greatly reduce the amount of
            // binding we need to perform when looking for restrictions in the type.
            foreach (var member in namedType.GetMembers())
            {
                if (member is IFieldSymbol
                    {
                        DeclaredAccessibility: Accessibility.Private,
                        CanBeReferencedByName: true,
                    } field)
                {
                    fieldNames.Add(field.Name);
                }
            }

            context.RegisterSyntaxNodeAction(context =>
            {
                AnalyzePropertyDeclaration(context, namedType, fieldNames, analysisResults);
            }, PropertyDeclarationKind);

            context.RegisterCodeBlockStartAction<TSyntaxKind>(context =>
            {
                RegisterIneligibleFieldsAction(fieldNames, ineligibleFields, context.SemanticModel, context.CodeBlock, context.CancellationToken);
                RegisterNonConstructorFieldWrites(fieldNames, nonConstructorFieldWrites, context.SemanticModel, context.CodeBlock, context.CancellationToken);
            });

            context.RegisterSymbolEndAction(context =>
            {
                try
                {
                    Process(analysisResults, ineligibleFields, nonConstructorFieldWrites, context);
                }
                finally
                {
                    // Cleanup after doing all our work.
                    _fieldNamesPool.ClearAndFree(fieldNames);

                    s_analysisResultPool.ClearAndFree(analysisResults);

                    ClearAndFree(ineligibleFields);
                    ClearAndFree(nonConstructorFieldWrites);
                }
            });

            bool ShouldAnalyze(SymbolStartAnalysisContext context, INamedTypeSymbol namedType)
            {
                if (namedType.TypeKind is not TypeKind.Class and not TypeKind.Struct and not TypeKind.Module)
                    return false;

                // Don't bother running on this type unless at least one of its parts has the 'prefer auto props' option
                // on, and the diagnostic is not suppressed.
                if (!namedType.DeclaringSyntaxReferences.Select(d => d.SyntaxTree).Distinct().Any(tree =>
                {
                    var preferAutoProps = context.Options.GetAnalyzerOptions(tree).PreferAutoProperties;
                    return preferAutoProps.Value && !ShouldSkipAnalysis(tree, context.Options, context.Compilation.Options, preferAutoProps.Notification, context.CancellationToken);
                }))
                {
                    return false;
                }

                // If we are analyzing a sub-span (lightbulb case), then check if the filter span
                // has a field/property declaration where a diagnostic could be reported.
                if (context.FilterSpan.HasValue)
                {
                    Contract.ThrowIfNull(context.FilterTree);
                    var shouldAnalyze = false;
                    var analysisRoot = context.GetAnalysisRoot(findInTrivia: false);
                    foreach (var node in analysisRoot.DescendantNodes())
                    {
                        if (node is TPropertyDeclaration or TFieldDeclaration && context.ShouldAnalyzeSpan(node.Span, node.SyntaxTree))
                        {
                            shouldAnalyze = true;
                            break;
                        }
                    }

                    if (!shouldAnalyze && analysisRoot.FirstAncestorOrSelf<SyntaxNode>(node => node is TPropertyDeclaration or TFieldDeclaration) == null)
                        return false;
                }

                return true;
            }
        }, SymbolKind.NamedType);

    private void RegisterNonConstructorFieldWrites(
        HashSet<string> fieldNames,
        ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> fieldWrites,
        SemanticModel semanticModel,
        SyntaxNode codeBlock,
        CancellationToken cancellationToken)
    {
        if (codeBlock.FirstAncestorOrSelf<TConstructorDeclaration>() != null)
            return;

        var semanticFacts = this.SemanticFacts;
        var syntaxFacts = this.SyntaxFacts;
        foreach (var identifierName in codeBlock.DescendantNodesAndSelf().OfType<TIdentifierName>())
        {
            var identifier = syntaxFacts.GetIdentifierOfIdentifierName(identifierName);
            if (!fieldNames.Contains(identifier.ValueText))
                continue;

            if (semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol is not IFieldSymbol field)
                continue;

            if (!semanticFacts.IsWrittenTo(semanticModel, identifierName, cancellationToken))
                continue;

            AddFieldUsage(fieldWrites, field, identifierName);
        }
    }

    private AccessedFields GetGetterFields(
        SemanticModel semanticModel,
        IMethodSymbol getMethod,
        HashSet<string> fieldNames,
        CancellationToken cancellationToken)
    {
        var trivialFieldExpression = GetGetterExpression(getMethod, cancellationToken);
        if (trivialFieldExpression != null)
            return new(CheckFieldAccessExpression(semanticModel, trivialFieldExpression, fieldNames, cancellationToken));

        if (!this.SupportsSemiAutoProperty(semanticModel.Compilation))
            return default;

        using var _ = PooledHashSet<IFieldSymbol>.GetInstance(out var set);
        AddAccessedFields(semanticModel, getMethod, fieldNames, set, cancellationToken);

        return new(TrivialField: null, set.ToImmutableArray());
    }

    private AccessedFields GetSetterFields(
        SemanticModel semanticModel, IMethodSymbol setMethod, HashSet<string> fieldNames, CancellationToken cancellationToken)
    {
        var trivialFieldExpression = GetSetterExpression(semanticModel, setMethod, cancellationToken);
        if (trivialFieldExpression != null)
            return new(CheckFieldAccessExpression(semanticModel, trivialFieldExpression, fieldNames, cancellationToken));

        if (!this.SupportsSemiAutoProperty(semanticModel.Compilation))
            return default;

        using var _ = PooledHashSet<IFieldSymbol>.GetInstance(out var set);
        AddAccessedFields(semanticModel, setMethod, fieldNames, set, cancellationToken);

        return new(TrivialField: null, set.ToImmutableArray());
    }

    private static bool TryGetSyntax(
        IFieldSymbol field,
        [NotNullWhen(true)] out TFieldDeclaration? fieldDeclaration,
        [NotNullWhen(true)] out TVariableDeclarator? variableDeclarator,
        CancellationToken cancellationToken)
    {
        if (field.DeclaringSyntaxReferences is [var fieldReference, ..])
        {
            variableDeclarator = fieldReference.GetSyntax(cancellationToken) as TVariableDeclarator;
            fieldDeclaration = variableDeclarator?.Parent?.Parent as TFieldDeclaration;
            return fieldDeclaration != null && variableDeclarator != null;
        }

        fieldDeclaration = null;
        variableDeclarator = null;
        return false;
    }

    private void AnalyzePropertyDeclaration(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol containingType,
        HashSet<string> fieldNames,
        ConcurrentStack<AnalysisResult> analysisResults)
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
        var notification = preferAutoProps.Notification;
        if (notification.Severity == ReportDiagnostic.Suppress)
            return;

        var getterFields = GetGetterFields(semanticModel, property.GetMethod, fieldNames, cancellationToken);
        getterFields = getterFields.Where(
            static (getterField, args) =>
            {
                var (@this, compilation, containingType, property, cancellationToken) = args;

                // Only support this for private fields.  It limits the scope of hte program
                // we have to analyze to make sure this is safe to do.
                if (getterField.DeclaredAccessibility != Accessibility.Private)
                    return false;

                // Don't want to remove constants and volatile fields.
                if (getterField.IsConst || getterField.IsVolatile)
                    return false;

                // If the user made the field readonly, we only want to convert it to a property if we
                // can keep it readonly.
                if (getterField.IsReadOnly && !@this.SupportsReadOnlyProperties(compilation))
                    return false;

                // Mutable value type fields are mutable unless they are marked read-only
                if (!getterField.IsReadOnly && getterField.Type.IsMutableValueType() != false)
                    return false;

                // Field and property have to be in the same type.
                if (!containingType.Equals(getterField.ContainingType))
                    return false;

                // Field and property should match in static-ness
                if (getterField.IsStatic != property.IsStatic)
                    return false;

                // Property and field have to agree on type.
                if (!property.Type.Equals(getterField.Type))
                    return false;

                if (!TryGetSyntax(getterField, out _, out var variableDeclarator, cancellationToken))
                    return false;

                var initializer = @this.GetFieldInitializer(variableDeclarator, cancellationToken);
                if (initializer != null && !@this.SupportsPropertyInitializer(compilation))
                    return false;

                if (!@this.CanConvert(property))
                    return false;

                // Can't remove the field if it has attributes on it.
                var attributes = getterField.GetAttributes();
                var suppressMessageAttributeType = compilation.SuppressMessageAttributeType();
                foreach (var attribute in attributes)
                {
                    if (attribute.AttributeClass != suppressMessageAttributeType)
                        return false;
                }

                return true;
            },
            (this, compilation, containingType, property, cancellationToken));

        if (getterFields.IsEmpty)
            return;

        // A setter is optional though.
        var setMethod = property.SetMethod;
        if (setMethod != null)
        {
            var setterFields = GetSetterFields(semanticModel, setMethod, fieldNames, cancellationToken);
            var sharedFields = setterFields.Where(
                static (field, getterFields) => getterFields.Contains(field),
                getterFields);

            // If there is a getter and a setter, they both need to agree on which field they are writing to.  Or, if
            // this language supports semi-auto-properties, we can still update the getter to use `field` and keep the
            // setter as-is.
            if (sharedFields.IsEmpty)
            {
                if (!this.SupportsSemiAutoProperty(compilation))
                    return;

                // Intentionally keep getterFields as is.  We'll just choose any of the viable fields to remove here.
            }
            else
            {
                // They share certain fields.  Prefer one of those fields as the one to remove.
                getterFields = sharedFields;
            }
        }

        var getterField = getterFields.TrivialField ?? getterFields.NonTrivialFields.First();

        // Looks like a viable property/field to convert into an auto property.
        Contract.ThrowIfFalse(TryGetSyntax(getterField, out var fieldDeclaration, out var variableDeclarator, cancellationToken));

        analysisResults.Push(new AnalysisResult(
            property, getterField,
            propertyDeclaration, fieldDeclaration, variableDeclarator,
            notification,
            IsSimpleProperty: getterField == getterFields.TrivialField));
    }

    protected virtual bool CanConvert(IPropertySymbol property)
        => true;

    protected static IFieldSymbol? TryGetDirectlyAccessedFieldSymbol(
        SemanticModel semanticModel, TExpression expression, CancellationToken cancellationToken)
    {
        var operation = semanticModel.GetOperation(expression, cancellationToken);
        if (operation is not IFieldReferenceOperation
            {
                Instance: null or IInstanceReferenceOperation
                {
                    ReferenceKind: InstanceReferenceKind.ContainingTypeInstance,
                },
                Field.DeclaringSyntaxReferences.Length: 1,
            } fieldReference)
        {
            return null;
        }

        return fieldReference.Field;
    }

    private IFieldSymbol? CheckFieldAccessExpression(
        SemanticModel semanticModel,
        TExpression? expression,
        HashSet<string> fieldNames,
        CancellationToken cancellationToken)
    {
        if (expression == null)
            return null;

        // needs to be of the form `x` or `this.x`.
        var syntaxFacts = this.SyntaxFacts;
        if (syntaxFacts.IsMemberAccessExpression(expression))
            expression = (TExpression)SyntaxFacts.GetNameOfMemberAccessExpression(expression);

        if (!syntaxFacts.IsIdentifierName(expression))
            return null;

        // Avoid binding identifiers that couldn't possibly bind to a field we care about.
        if (!fieldNames.Contains(syntaxFacts.GetIdentifierOfIdentifierName(expression).ValueText))
            return null;

        return TryGetDirectlyAccessedFieldSymbol(semanticModel, expression, cancellationToken);
    }

    private void Process(
        ConcurrentStack<AnalysisResult> analysisResults,
        ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> ineligibleFields,
        ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> nonConstructorFieldWrites,
        SymbolAnalysisContext context)
    {
        foreach (var result in analysisResults)
        {
            // C# specific check.
            if (ineligibleFields.TryGetValue(result.Field, out var ineligibleFieldUsages))
            {
                // We had a usage of the field outside of the property in a way that disqualifies it.
                if (!ineligibleFieldUsages.All(loc => loc.Ancestors().Contains(result.PropertyDeclaration)))
                    continue;

                // All the usages were inside the property.  This is ok if we support the `field` keyword as those
                // usages will be updated to that form.
                if (!this.SupportsSemiAutoProperty(context.Compilation))
                    continue;
            }

            // VB specific check.
            //
            // if the property doesn't have a setter currently.check all the types the field is declared in.  If the
            // field is written to outside of a constructor, then this field Is Not eligible for replacement with an
            // auto prop.  We'd have to make the autoprop read/write, And that could be opening up the property
            // widely (in accessibility terms) in a way the user would not want.
            if (result.Property.Language == LanguageNames.VisualBasic)
            {
                if (result.Property.DeclaredAccessibility != Accessibility.Private &&
                    result.Property.SetMethod is null &&
                    nonConstructorFieldWrites.TryGetValue(result.Field, out var writeLocations1) &&
                    writeLocations1.Any(loc => !loc.Ancestors().Contains(result.PropertyDeclaration)))
                {
                    continue;
                }
            }

            // If this was an `init` property, and there was a write to the field, then we can't support this.
            // That's because we can't still keep this `init` as that write will not be allowed, and we can't make
            // it a `setter` as that would allow arbitrary writing outside the type, despite the original `init`
            // semantics.
            if (result.Property.SetMethod is { IsInitOnly: true } &&
                nonConstructorFieldWrites.TryGetValue(result.Field, out var writeLocations2) &&
                writeLocations2.Any(loc => !loc.Ancestors().Contains(result.PropertyDeclaration)))
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
            result.Notification,
            context.Options,
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
        NotificationOption2 Notification,
        bool IsSimpleProperty);
}
