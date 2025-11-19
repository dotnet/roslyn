// Licensed to the .NET Foundation under one or more agreements.
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

using static UseAutoPropertiesHelpers;

internal abstract partial class AbstractUseAutoPropertyAnalyzer<
    TSyntaxKind,
    TPropertyDeclaration,
    TConstructorDeclaration,
    TFieldDeclaration,
    TVariableDeclarator,
    TExpression,
    TIdentifierName>(ISemanticFacts semanticFacts)
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(IDEDiagnosticIds.UseAutoPropertyDiagnosticId,
           EnforceOnBuildValues.UseAutoProperty,
           CodeStyleOptions2.PreferAutoProperties,
           new LocalizableResourceString(nameof(AnalyzersResources.Use_auto_property), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
           new LocalizableResourceString(nameof(AnalyzersResources.Use_auto_property), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
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

    /// <summary>
    /// Not static as this has different semantics around case sensitivity for C# and VB.
    /// </summary>
    private readonly ObjectPool<HashSet<string>> _fieldNamesPool = new(() => new(semanticFacts.SyntaxFacts.StringComparer));

    protected static void AddFieldUsage(ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> fieldWrites, IFieldSymbol field, SyntaxNode location)
        => fieldWrites.GetOrAdd(field, static _ => s_nodeSetPool.Allocate()).Add(location);

    private static void ClearAndFree(ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> multiMap)
    {
        foreach (var (_, nodeSet) in multiMap)
            s_nodeSetPool.ClearAndFree(nodeSet);

        s_fieldToUsageLocationPool.ClearAndFree(multiMap);
    }

    /// <summary>
    /// A method body edit anywhere in a type will force us to reanalyze the whole type.
    /// </summary>
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    private ISemanticFacts SemanticFacts { get; } = semanticFacts;
    private ISyntaxFacts SyntaxFacts => SemanticFacts.SyntaxFacts;

    protected abstract TSyntaxKind PropertyDeclarationKind { get; }

    protected abstract bool CanExplicitInterfaceImplementationsBeFixed { get; }
    protected abstract bool SupportsFieldAttributesOnProperties { get; }

    protected abstract bool SupportsReadOnlyProperties(Compilation compilation);
    protected abstract bool SupportsPropertyInitializer(Compilation compilation);

    protected abstract TExpression? GetFieldInitializer(TVariableDeclarator variable, CancellationToken cancellationToken);
    protected abstract TExpression? GetGetterExpression(IMethodSymbol getMethod, CancellationToken cancellationToken);
    protected abstract TExpression? GetSetterExpression(SemanticModel semanticModel, IMethodSymbol setMethod, CancellationToken cancellationToken);
    protected abstract SyntaxNode GetFieldNode(TFieldDeclaration fieldDeclaration, TVariableDeclarator variableDeclarator);
    protected abstract void AddAccessedFields(
        SemanticModel semanticModel, IMethodSymbol accessor, HashSet<string> fieldNames, HashSet<IFieldSymbol> result, CancellationToken cancellationToken);

    protected abstract void RecordIneligibleFieldLocations(
        HashSet<string> fieldNames, ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> ineligibleFieldUsageIfOutsideProperty, SemanticModel semanticModel, SyntaxNode codeBlock, CancellationToken cancellationToken);

    protected abstract bool IsStaticConstructor(TConstructorDeclaration constructorDeclaration);

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterSymbolStartAction(context =>
        {
            var namedType = (INamedTypeSymbol)context.Symbol;
            if (!ShouldAnalyze(context, namedType))
                return;

            // Results of our analysis pass that we will use to determine which fields and properties to offer to fixup.
            var analysisResults = s_analysisResultPool.Allocate();

            // Fields whose usage may disqualify them from being removed (depending on the usage location). For example,
            // a field taken by ref normally can't be converted (as a property can't be taken by ref).  However, this
            // doesn't apply within the property itself (as it can refer to `field` after the rewrite).
            var ineligibleFieldUsageIfOutsideProperty = s_fieldToUsageLocationPool.Allocate();

            // Locations where this field is read or written.  If it is read or written outside of hte property being
            // changed, and the property getter/setter is non-trivial, then we cannot use 'field' for it, as that would 
            // change the semantics in those locations.
            var fieldReads = s_fieldToUsageLocationPool.Allocate();
            var fieldWrites = s_fieldToUsageLocationPool.Allocate();

            // Record the names of all the private fields in this type.  We can use this to greatly reduce the amount of
            // binding we need to perform when looking for restrictions in the type.
            var fieldNames = _fieldNamesPool.Allocate();
            foreach (var member in namedType.GetMembers())
            {
                if (member is IFieldSymbol
                    {
                        // Can only convert fields that are private, as otherwise we don't know how they may be used
                        // outside of this type.
                        DeclaredAccessibility: Accessibility.Private,
                        // Only care about actual user-defined fields, not compiler generated ones.
                        CanBeReferencedByName: true,
                        // Will never convert a constant into an auto-prop
                        IsConst: false,
                        // Can't preserve volatile semantics on a property.
                        IsVolatile: false,
                        // Can't have an autoprop that returns by-ref. 
                        RefKind: RefKind.None,
                        // To make processing later on easier, limit to well-behaved fields (versus having multiple
                        // fields merged together in error recoery scenarios).
                        DeclaringSyntaxReferences.Length: 1,
                    } field)
                {
                    fieldNames.Add(field.Name);
                }
            }

            // Examine each property-declaration we find within this named type to see if it looks like it can be converted.
            context.RegisterSyntaxNodeAction(
                context => AnalyzePropertyDeclaration(context, namedType, fieldNames, analysisResults),
                PropertyDeclarationKind);

            // Concurrently, examine the usages of the fields of this type within itself to see how those may impact if
            // a field/prop pair can actually be converted.
            context.RegisterCodeBlockStartAction<TSyntaxKind>(context =>
            {
                RecordIneligibleFieldLocations(fieldNames, ineligibleFieldUsageIfOutsideProperty, context.SemanticModel, context.CodeBlock, context.CancellationToken);
                RecordAllFieldReferences(fieldNames, fieldReads, fieldWrites, context.SemanticModel, context.CodeBlock, context.CancellationToken);
            });

            context.RegisterSymbolEndAction(context =>
            {
                try
                {
                    Process(analysisResults, ineligibleFieldUsageIfOutsideProperty, fieldReads, fieldWrites, context);
                }
                finally
                {
                    // Cleanup after doing all our work.
                    _fieldNamesPool.ClearAndFree(fieldNames);
                    s_analysisResultPool.ClearAndFree(analysisResults);

                    ClearAndFree(ineligibleFieldUsageIfOutsideProperty);
                    ClearAndFree(fieldReads);
                    ClearAndFree(fieldWrites);
                }
            });

            bool ShouldAnalyze(SymbolStartAnalysisContext context, INamedTypeSymbol namedType)
            {
                if (namedType.TypeKind is not TypeKind.Class and not TypeKind.Struct and not TypeKind.Module)
                    return false;

                // Serializable types can depend on fields (and their order).  Don't report these
                // properties in that case.
                if (namedType.IsSerializable)
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

    private void RecordAllFieldReferences(
        HashSet<string> fieldNames,
        ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> fieldReads,
        ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> fieldWrites,
        SemanticModel semanticModel,
        SyntaxNode codeBlock,
        CancellationToken cancellationToken)
    {
        var semanticFacts = this.SemanticFacts;
        var syntaxFacts = this.SyntaxFacts;
        foreach (var identifierName in codeBlock.DescendantNodesAndSelf().OfType<TIdentifierName>())
        {
            var identifier = syntaxFacts.GetIdentifierOfIdentifierName(identifierName);
            if (!fieldNames.Contains(identifier.ValueText))
                continue;

            if (semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol is not IFieldSymbol field)
                continue;

            if (semanticFacts.IsOnlyWrittenTo(semanticModel, identifierName, cancellationToken))
            {
                AddFieldUsage(fieldWrites, field, identifierName);
            }
            else if (semanticFacts.IsWrittenTo(semanticModel, identifierName, cancellationToken))
            {
                AddFieldUsage(fieldWrites, field, identifierName);
                AddFieldUsage(fieldReads, field, identifierName);
            }
            else
            {
                AddFieldUsage(fieldReads, field, identifierName);
            }
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

        if (!this.SyntaxFacts.SupportsFieldExpression(semanticModel.SyntaxTree.Options))
            return AccessedFields.Empty;

        using var _ = PooledHashSet<IFieldSymbol>.GetInstance(out var set);
        AddAccessedFields(semanticModel, getMethod, fieldNames, set, cancellationToken);

        return new(TrivialField: null, [.. set]);
    }

    private AccessedFields GetSetterFields(
        SemanticModel semanticModel, IMethodSymbol setMethod, HashSet<string> fieldNames, CancellationToken cancellationToken)
    {
        var trivialFieldExpression = GetSetterExpression(semanticModel, setMethod, cancellationToken);
        if (trivialFieldExpression != null)
            return new(CheckFieldAccessExpression(semanticModel, trivialFieldExpression, fieldNames, cancellationToken));

        if (!this.SyntaxFacts.SupportsFieldExpression(semanticModel.SyntaxTree.Options))
            return AccessedFields.Empty;

        using var _ = PooledHashSet<IFieldSymbol>.GetInstance(out var set);
        AddAccessedFields(semanticModel, setMethod, fieldNames, set, cancellationToken);

        return new(TrivialField: null, [.. set]);
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
        var name = expression;
        if (syntaxFacts.IsMemberAccessExpression(expression))
            name = (TExpression)SyntaxFacts.GetNameOfMemberAccessExpression(expression);

        return TryGetDirectlyAccessedFieldSymbol(semanticModel, name as TIdentifierName, fieldNames, cancellationToken);
    }

    private static bool TryGetSyntax(
        IFieldSymbol field,
        [NotNullWhen(true)] out TFieldDeclaration? fieldDeclaration,
        [NotNullWhen(true)] out TVariableDeclarator? variableDeclarator,
        CancellationToken cancellationToken)
    {
        if (field.DeclaringSyntaxReferences is [var fieldReference])
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

        // To make processing later on easier, limit to well-behaved properties (versus having multiple
        // properties merged together in error recovery scenarios).
        if (property.DeclaringSyntaxReferences.Length != 1)
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

        if (property.RefKind != RefKind.None)
            return;

        if (!CanExplicitInterfaceImplementationsBeFixed && property.ExplicitInterfaceImplementations.Length != 0)
            return;

        var preferAutoProps = context.GetAnalyzerOptions().PreferAutoProperties;
        if (!preferAutoProps.Value)
            return;

        // Avoid reporting diagnostics when the feature is disabled. This primarily avoids reporting the hidden
        // helper diagnostic which is not otherwise influenced by the severity settings.
        var notification = preferAutoProps.Notification;
        if (notification.Severity == ReportDiagnostic.Suppress)
            return;

        // If the property already contains a `field` expression, then we can't do anything more here.
        if (this.SyntaxFacts.SupportsFieldExpression(propertyDeclaration.SyntaxTree.Options) &&
            propertyDeclaration.DescendantNodes().Any(this.SyntaxFacts.IsFieldExpression))
        {
            return;
        }

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
                if (attributes.Length > 0 && !@this.SupportsFieldAttributesOnProperties)
                    return false;

                return true;
            },
            (this, compilation, containingType, property, cancellationToken));

        if (getterFields.IsEmpty)
            return;

        var isTrivialSetAccessor = false;

        // A setter is optional though.
        if (property.SetMethod != null)
        {
            // Figure out all the fields written to in the setter.
            var setterFields = GetSetterFields(semanticModel, property.SetMethod, fieldNames, cancellationToken);

            // Intersect these to determine which fields both the getter and setter write to.
            getterFields = getterFields.Where(
                static (field, setterFields) => setterFields.Contains(field),
                setterFields);

            // If there is a getter and a setter, they both need to agree on which field they are writing to.
            if (getterFields.IsEmpty)
                return;

            isTrivialSetAccessor = setterFields.TrivialField != null;
        }

        if (getterFields.Count > 1)
        {
            // Multiple fields we could convert here.  Check if any of the fields end with the property name.  If
            // so, it's likely that that's the field to use.
            getterFields = getterFields.Where(
                static (field, property) => field.Name.EndsWith(property.Name, StringComparison.OrdinalIgnoreCase),
                property);
        }

        // If we have multiple fields that could be converted, don't offer.  We don't know which field/prop pair would
        // be best.
        if (getterFields.Count != 1)
            return;

        var getterField = getterFields.TrivialField ?? getterFields.NonTrivialFields.Single();
        var isTrivialGetAccessor = getterFields.TrivialField == getterField;

        Contract.ThrowIfFalse(TryGetSyntax(getterField, out var fieldDeclaration, out var variableDeclarator, cancellationToken));

        analysisResults.Push(new AnalysisResult(
            property, getterField,
            propertyDeclaration, fieldDeclaration, variableDeclarator,
            notification,
            isTrivialGetAccessor,
            isTrivialSetAccessor));
    }

    protected virtual bool CanConvert(IPropertySymbol property)
        => true;

    protected IFieldSymbol? TryGetDirectlyAccessedFieldSymbol(
        SemanticModel semanticModel,
        TIdentifierName? identifierName,
        HashSet<string> fieldNames,
        CancellationToken cancellationToken)
    {
        if (identifierName is null)
            return null;

        var syntaxFacts = this.SyntaxFacts;

        // Quick check to avoid costly binding.  Only look at identifiers that match the name of a private field in
        // the containing type.
        if (!fieldNames.Contains(syntaxFacts.GetIdentifierOfIdentifierName(identifierName).ValueText))
            return null;

        TExpression expression = identifierName;
        if (this.SyntaxFacts.IsNameOfAnyMemberAccessExpression(expression))
            expression = (TExpression)expression.GetRequiredParent();

        var operation = semanticModel.GetOperation(expression, cancellationToken);
        if (operation is not IFieldReferenceOperation
            {
                // Instance has to be 'null' (a static reference) or through `this.` Anything else is not a direct
                // reference that can be updated to `field`.
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

    private void Process(
        ConcurrentStack<AnalysisResult> analysisResults,
        ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> ineligibleFieldUsageIfOutsideProperty,
        ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> fieldReads,
        ConcurrentDictionary<IFieldSymbol, ConcurrentSet<SyntaxNode>> fieldWrites,
        SymbolAnalysisContext context)
    {
        using var _1 = PooledHashSet<IFieldSymbol>.GetInstance(out var reportedFields);
        using var _2 = PooledHashSet<IPropertySymbol>.GetInstance(out var reportedProperties);

        foreach (var result in analysisResults)
        {
            // Check If we had any invalid field usage outside of the property we're converting.
            if (ineligibleFieldUsageIfOutsideProperty.TryGetValue(result.Field, out var ineligibleFieldUsages))
            {
                if (!ineligibleFieldUsages.All(loc => loc.Ancestors().Contains(result.PropertyDeclaration)))
                    continue;

                // All the usages were inside the property.  This is ok if we support the `field` keyword as those
                // usages will be updated to that form.
                if (!this.SyntaxFacts.SupportsFieldExpression(result.PropertyDeclaration.SyntaxTree.Options))
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
                    fieldWrites.TryGetValue(result.Field, out var writeLocations1) &&
                    NonConstructorLocations(writeLocations1).Any(loc => !loc.Ancestors().Contains(result.PropertyDeclaration)))
                {
                    continue;
                }
            }

            // C# specific check.
            //
            // If this was an `init` property, and there was a write to the field, then we can't support this. That's
            // because we can't still keep this `init` as that write will not be allowed, and we can't make it a
            // `setter` as that would allow arbitrary writing outside the type, despite the original `init` semantics.
            if (result.Property.SetMethod is { IsInitOnly: true } &&
                fieldWrites.TryGetValue(result.Field, out var writeLocations2) &&
                NonConstructorLocations(writeLocations2).Any(loc => !loc.Ancestors().Contains(result.PropertyDeclaration)))
            {
                continue;
            }

            // If the property is static with no setter, and the field is written in an instance constructor, we can't
            // convert it. A static property can only be assigned in a static constructor, not an instance constructor.
            if (result.Property.IsStatic &&
                result.Property.SetMethod is null &&
                fieldWrites.TryGetValue(result.Field, out var writeLocations3) &&
                InstanceConstructorLocations(writeLocations3).Any(loc => !loc.Ancestors().Contains(result.PropertyDeclaration)))
            {
                continue;
            }

            // If we have a non-trivial getter, then we can't convert this if the field is read outside of the property.
            // The read will go through the property getter now, which may change semantics.
            if (!result.IsTrivialGetAccessor &&
                fieldReads.TryGetValue(result.Field, out var specificFieldReads) &&
                NotWithinProperty(specificFieldReads, result.PropertyDeclaration))
            {
                continue;
            }

            // If we have a non-trivial getter, then we can't convert this if the field is written outside of the
            // property. The write will go through the property setter now, which may change semantics.
            if (result.Property.SetMethod != null &&
                !result.IsTrivialSetAccessor &&
                fieldWrites.TryGetValue(result.Field, out var specificFieldWrites) &&
                NotWithinProperty(specificFieldWrites, result.PropertyDeclaration))
            {
                continue;
            }

            // Only report a use-auto-prop message at most once for any field or property. Note: we could be smarter
            // here.  The set of fields and properties form a bipartite graph.  In an ideal world, we'd determine the
            // maximal matching between those two bipartite sets (see
            // https://en.wikipedia.org/wiki/Hopcroft%E2%80%93Karp_algorithm) and use that to offer the most matches as
            // possible.
            //
            // We can see if the simple greedy approach of just taking the matches as we find them and returning those
            // is insufficient in the future.
            if (reportedFields.Contains(result.Field) || reportedProperties.Contains(result.Property))
                continue;

            reportedFields.Add(result.Field);
            reportedProperties.Add(result.Property);

            ReportDiagnostics(result);
        }

        static bool NotWithinProperty(IEnumerable<SyntaxNode> nodes, TPropertyDeclaration propertyDeclaration)
        {
            foreach (var node in nodes)
            {
                if (!node.AncestorsAndSelf().Contains(propertyDeclaration))
                    return true;
            }

            return false;
        }

        static IEnumerable<SyntaxNode> NonConstructorLocations(IEnumerable<SyntaxNode> nodes)
            => nodes.Where(n => n.FirstAncestorOrSelf<TConstructorDeclaration>() is null);

        IEnumerable<SyntaxNode> InstanceConstructorLocations(IEnumerable<SyntaxNode> nodes)
            => nodes.Where(n => n.FirstAncestorOrSelf<TConstructorDeclaration>() is TConstructorDeclaration ctor && !IsStaticConstructor(ctor));

        void ReportDiagnostics(AnalysisResult result)
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

            var properties = ImmutableDictionary<string, string?>.Empty;
            if (result.IsTrivialGetAccessor)
                properties = properties.Add(IsTrivialGetAccessor, IsTrivialGetAccessor);

            if (result.IsTrivialSetAccessor)
                properties = properties.Add(IsTrivialSetAccessor, IsTrivialSetAccessor);

            // Place the appropriate marker on the field depending on the user option.
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                fieldNode.GetLocation(),
                result.Notification,
                context.Options,
                additionalLocations,
                properties));

            // Also, place a hidden marker on the property.  If they bring up a lightbulb there, they'll be able to see that
            // they can convert it to an auto-prop.
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptor, propertyDeclaration.GetLocation(),
                additionalLocations,
                properties));
        }
    }
}
