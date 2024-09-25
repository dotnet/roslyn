// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class SyntaxGeneratorExtensions
{
    public static IMethodSymbol CreateBaseDelegatingConstructor(
        this SyntaxGenerator factory,
        IMethodSymbol constructor,
        string typeName)
    {
        // Create a constructor that calls the base constructor.  Note: if there are no
        // parameters then don't bother writing out "base()" it's automatically implied.
        return CodeGenerationSymbolFactory.CreateConstructorSymbol(
            attributes: default,
            accessibility: Accessibility.Public,
            modifiers: new DeclarationModifiers(),
            typeName: typeName,
            parameters: constructor.Parameters,
            statements: default,
            baseConstructorArguments: constructor.Parameters.Length == 0
                ? default
                : factory.CreateArguments(constructor.Parameters));
    }

    public static ImmutableArray<ISymbol> CreateMemberDelegatingConstructor(
        this SyntaxGenerator factory,
        SemanticModel semanticModel,
        string typeName,
        INamedTypeSymbol? containingType,
        ImmutableArray<IParameterSymbol> parameters,
        Accessibility accessibility,
        ImmutableDictionary<string, ISymbol>? parameterToExistingMemberMap,
        ImmutableDictionary<string, string>? parameterToNewMemberMap,
        bool addNullChecks,
        bool preferThrowExpression,
        bool generateProperties,
        bool isContainedInUnsafeType)
    {
        var newMembers = generateProperties
            ? CreatePropertiesForParameters(parameters, parameterToNewMemberMap, isContainedInUnsafeType)
            : CreateFieldsForParameters(parameters, parameterToNewMemberMap, isContainedInUnsafeType);
        var statements = factory.CreateAssignmentStatements(
            semanticModel, parameters, parameterToExistingMemberMap, parameterToNewMemberMap,
            addNullChecks, preferThrowExpression).SelectAsArray(
                s => s.WithAdditionalAnnotations(Simplifier.Annotation));

        var constructor = CodeGenerationSymbolFactory.CreateConstructorSymbol(
            attributes: default,
            accessibility: accessibility,
            modifiers: new DeclarationModifiers(isUnsafe: !isContainedInUnsafeType && parameters.Any(static p => p.RequiresUnsafeModifier())),
            typeName: typeName,
            parameters: parameters,
            statements: statements,
            thisConstructorArguments: ShouldGenerateThisConstructorCall(containingType, parameterToExistingMemberMap)
                ? []
                : default);

        return newMembers.Concat(constructor);
    }

    private static bool ShouldGenerateThisConstructorCall(
        INamedTypeSymbol? containingType,
        IDictionary<string, ISymbol>? parameterToExistingFieldMap)
    {
        if (containingType?.TypeKind == TypeKind.Struct)
        {
            // Special case.  If we're generating a struct constructor, then we'll need
            // to initialize all fields in the struct, not just the ones we're creating.
            // If there is any field or auto-property not being set by a parameter, we
            // call the default constructor.

            return containingType.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(field => !field.IsStatic)
                .Select(field => field.AssociatedSymbol ?? field)
                .Except(parameterToExistingFieldMap?.Values ?? [])
                .Any();
        }

        return false;
    }

    public static ImmutableArray<ISymbol> CreateFieldsForParameters(
        ImmutableArray<IParameterSymbol> parameters, ImmutableDictionary<string, string>? parameterToNewFieldMap, bool isContainedInUnsafeType)
    {
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var result);
        foreach (var parameter in parameters)
        {
            // For non-out parameters, create a field and assign the parameter to it.
            if (parameter.RefKind != RefKind.Out &&
                TryGetValue(parameterToNewFieldMap, parameter.Name, out var fieldName))
            {
                result.Add(CodeGenerationSymbolFactory.CreateFieldSymbol(
                    attributes: default,
                    accessibility: Accessibility.Private,
                    modifiers: new DeclarationModifiers(isUnsafe: !isContainedInUnsafeType && parameter.RequiresUnsafeModifier()),
                    type: parameter.Type,
                    name: fieldName));
            }
        }

        return result.ToImmutableAndClear();
    }

    public static ImmutableArray<ISymbol> CreatePropertiesForParameters(
        ImmutableArray<IParameterSymbol> parameters, ImmutableDictionary<string, string>? parameterToNewPropertyMap, bool isContainedInUnsafeType)
    {
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var result);
        foreach (var parameter in parameters)
        {
            // For non-out parameters, create a property and assign the parameter to it.
            if (parameter.RefKind != RefKind.Out &&
                TryGetValue(parameterToNewPropertyMap, parameter.Name, out var propertyName))
            {
                result.Add(CodeGenerationSymbolFactory.CreatePropertySymbol(
                    attributes: default,
                    accessibility: Accessibility.Public,
                    modifiers: new DeclarationModifiers(isUnsafe: !isContainedInUnsafeType && parameter.RequiresUnsafeModifier()),
                    type: parameter.Type,
                    refKind: RefKind.None,
                    explicitInterfaceImplementations: [],
                    name: propertyName,
                    parameters: [],
                    getMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol(
                        attributes: default,
                        accessibility: default,
                        statements: default),
                    setMethod: null));
            }
        }

        return result.ToImmutableAndClear();
    }

    private static bool TryGetValue(IDictionary<string, string>? dictionary, string key, [NotNullWhen(true)] out string? value)
    {
        value = null;
        return
            dictionary != null &&
            dictionary.TryGetValue(key, out value);
    }

    private static bool TryGetValue(IDictionary<string, ISymbol>? dictionary, string key, [NotNullWhen(true)] out string? value)
    {
        value = null;
        if (dictionary != null && dictionary.TryGetValue(key, out var symbol))
        {
            value = symbol.Name;
            return true;
        }

        return false;
    }

    public static SyntaxNode CreateThrowArgumentNullExpression(this SyntaxGenerator factory, Compilation compilation, IParameterSymbol parameter)
        => factory.ThrowExpression(CreateNewArgumentNullException(factory, compilation, parameter));

    private static SyntaxNode CreateNewArgumentNullException(SyntaxGenerator factory, Compilation compilation, IParameterSymbol parameter)
    {
        var type = compilation.GetTypeByMetadataName(typeof(ArgumentNullException).FullName!);
        Contract.ThrowIfNull(type);
        return factory.ObjectCreationExpression(type,
            factory.NameOfExpression(
                factory.IdentifierName(parameter.Name))).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);
    }

    public static SyntaxNode CreateNullCheckAndThrowStatement(
        this SyntaxGenerator factory,
        SemanticModel semanticModel,
        IParameterSymbol parameter)
    {
        var condition = factory.CreateNullCheckExpression(semanticModel, parameter.Name);
        var throwStatement = factory.CreateThrowArgumentNullExceptionStatement(semanticModel.Compilation, parameter);

        // generates: if (s is null) { throw new ArgumentNullException(nameof(s)); }
        return factory.IfStatement(condition, [throwStatement]);
    }

    public static SyntaxNode CreateThrowArgumentNullExceptionStatement(this SyntaxGenerator factory, Compilation compilation, IParameterSymbol parameter)
        => factory.ThrowStatement(CreateNewArgumentNullException(factory, compilation, parameter));

    public static SyntaxNode CreateNullCheckExpression(this SyntaxGenerator factory, SemanticModel semanticModel, string identifierName)
    {
        var identifier = factory.IdentifierName(identifierName);
        var nullExpr = factory.NullLiteralExpression();
        var condition = factory.SyntaxGeneratorInternal.SupportsPatterns(semanticModel.SyntaxTree.Options)
            ? factory.SyntaxGeneratorInternal.IsPatternExpression(identifier, factory.SyntaxGeneratorInternal.ConstantPattern(nullExpr))
            : factory.ReferenceEqualsExpression(identifier, nullExpr);
        return condition;
    }

    public static ImmutableArray<SyntaxNode> CreateAssignmentStatements(
        this SyntaxGenerator factory,
        SemanticModel semanticModel,
        ImmutableArray<IParameterSymbol> parameters,
        IDictionary<string, ISymbol>? parameterToExistingFieldMap,
        IDictionary<string, string>? parameterToNewFieldMap,
        bool addNullChecks,
        bool preferThrowExpression)
    {
        var nullCheckStatements = ArrayBuilder<SyntaxNode>.GetInstance();
        var assignStatements = ArrayBuilder<SyntaxNode>.GetInstance();

        foreach (var parameter in parameters)
        {
            var refKind = parameter.RefKind;
            var parameterType = parameter.Type;
            var parameterName = parameter.Name;

            if (refKind == RefKind.Out)
            {
                // If it's an out param, then don't create a field for it.  Instead, assign
                // the default value for that type (i.e. "default(...)") to it.
                var assignExpression = factory.AssignmentStatement(
                    factory.IdentifierName(parameterName),
                    factory.DefaultExpression(parameterType));
                var statement = factory.ExpressionStatement(assignExpression);
                assignStatements.Add(statement);
            }
            else
            {
                // For non-out parameters, create a field and assign the parameter to it.
                // TODO: I'm not sure that's what we really want for ref parameters.
                if (TryGetValue(parameterToExistingFieldMap, parameterName, out var fieldName) ||
                    TryGetValue(parameterToNewFieldMap, parameterName, out fieldName))
                {
                    var fieldAccess = factory.MemberAccessExpression(factory.ThisExpression(), factory.IdentifierName(fieldName))
                                             .WithAdditionalAnnotations(Simplifier.Annotation);

                    factory.AddAssignmentStatements(
                        semanticModel, parameter, fieldAccess,
                        addNullChecks, preferThrowExpression,
                        nullCheckStatements, assignStatements);
                }
            }
        }

        return nullCheckStatements.ToImmutableAndFree().Concat(assignStatements.ToImmutableAndFree());
    }

    public static void AddAssignmentStatements(
         this SyntaxGenerator factory,
         SemanticModel semanticModel,
         IParameterSymbol parameter,
         SyntaxNode fieldAccess,
         bool addNullChecks,
         bool preferThrowExpression,
         ArrayBuilder<SyntaxNode> nullCheckStatements,
         ArrayBuilder<SyntaxNode> assignStatements)
    {
        // Don't want to add a null check for something of the form `int?`.  The type was
        // already declared as nullable to indicate that null is ok.  Adding a null check
        // just disallows something that should be allowed.
        var shouldAddNullCheck = addNullChecks && parameter.Type.CanAddNullCheck() && !parameter.Type.IsNullable();

        if (shouldAddNullCheck && preferThrowExpression && factory.SupportsThrowExpression())
        {
            // Generate: this.x = x ?? throw ...
            assignStatements.Add(CreateAssignWithNullCheckStatement(
                factory, semanticModel.Compilation, parameter, fieldAccess));
        }
        else
        {
            if (shouldAddNullCheck)
            {
                // generate: if (x == null) throw ...
                nullCheckStatements.Add(
                    factory.CreateNullCheckAndThrowStatement(semanticModel, parameter));
            }

            // generate: this.x = x;
            assignStatements.Add(
                factory.ExpressionStatement(
                    factory.AssignmentStatement(
                        fieldAccess,
                        factory.IdentifierName(parameter.Name))));
        }
    }

    public static SyntaxNode CreateAssignWithNullCheckStatement(
        this SyntaxGenerator factory, Compilation compilation, IParameterSymbol parameter, SyntaxNode fieldAccess)
    {
        return factory.ExpressionStatement(factory.AssignmentStatement(
            fieldAccess,
            factory.CoalesceExpression(
                factory.IdentifierName(parameter.Name),
                factory.CreateThrowArgumentNullExpression(compilation, parameter))));
    }

    public static async Task<IPropertySymbol> OverridePropertyAsync(
        this SyntaxGenerator codeFactory,
        IPropertySymbol overriddenProperty,
        DeclarationModifiers modifiers,
        INamedTypeSymbol containingType,
        Document document,
        CancellationToken cancellationToken)
    {
        var getAccessibility = overriddenProperty.GetMethod.ComputeResultantAccessibility(containingType);
        var setAccessibility = overriddenProperty.SetMethod.ComputeResultantAccessibility(containingType);

        SyntaxNode? getBody;
        SyntaxNode? setBody;
        // Implement an abstract property by throwing not implemented in accessors.
        if (overriddenProperty.IsAbstract)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var statement = codeFactory.CreateThrowNotImplementedStatement(compilation);

            getBody = statement;
            setBody = statement;
        }
        else if (overriddenProperty.IsIndexer() && document.Project.Language == LanguageNames.CSharp)
        {
            // Indexer: return or set base[]. Only in C#, since VB must refer to these by name.

            getBody = codeFactory.ReturnStatement(
                WrapWithRefIfNecessary(codeFactory, overriddenProperty,
                    codeFactory.ElementAccessExpression(
                        codeFactory.BaseExpression(),
                        codeFactory.CreateArguments(overriddenProperty.Parameters))));

            setBody = codeFactory.ExpressionStatement(
                codeFactory.AssignmentStatement(
                codeFactory.ElementAccessExpression(
                    codeFactory.BaseExpression(),
                    codeFactory.CreateArguments(overriddenProperty.Parameters)),
                codeFactory.IdentifierName("value")));
        }
        else if (overriddenProperty.GetParameters().Any())
        {
            // Call accessors directly if C# overriding VB
            if (document.Project.Language == LanguageNames.CSharp
                && await SymbolFinder.FindSourceDefinitionAsync(overriddenProperty, document.Project.Solution, cancellationToken).ConfigureAwait(false) is { Language: LanguageNames.VisualBasic })
            {
                var getName = overriddenProperty.GetMethod?.Name;
                var setName = overriddenProperty.SetMethod?.Name;

                getBody = getName == null
                    ? null
                    : codeFactory.ReturnStatement(
                codeFactory.InvocationExpression(
                    codeFactory.MemberAccessExpression(
                        codeFactory.BaseExpression(),
                        codeFactory.IdentifierName(getName)),
                    codeFactory.CreateArguments(overriddenProperty.Parameters)));

                setBody = setName == null
                    ? null
                    : codeFactory.ExpressionStatement(
                    codeFactory.InvocationExpression(
                        codeFactory.MemberAccessExpression(
                            codeFactory.BaseExpression(),
                            codeFactory.IdentifierName(setName)),
                        codeFactory.CreateArguments(overriddenProperty.SetMethod.GetParameters())));
            }
            else
            {
                getBody = codeFactory.ReturnStatement(
                    WrapWithRefIfNecessary(codeFactory, overriddenProperty,
                        codeFactory.InvocationExpression(
                            codeFactory.MemberAccessExpression(
                                codeFactory.BaseExpression(),
                                codeFactory.IdentifierName(overriddenProperty.Name)), codeFactory.CreateArguments(overriddenProperty.Parameters))));

                setBody = codeFactory.ExpressionStatement(
                    codeFactory.AssignmentStatement(
                        codeFactory.InvocationExpression(
                        codeFactory.MemberAccessExpression(
                        codeFactory.BaseExpression(),
                    codeFactory.IdentifierName(overriddenProperty.Name)), codeFactory.CreateArguments(overriddenProperty.Parameters)),
                    codeFactory.IdentifierName("value")));
            }
        }
        else
        {
            // Regular property: return or set the base property

            getBody = codeFactory.ReturnStatement(
                WrapWithRefIfNecessary(codeFactory, overriddenProperty,
                    codeFactory.MemberAccessExpression(
                        codeFactory.BaseExpression(),
                        codeFactory.IdentifierName(overriddenProperty.Name))));

            setBody = codeFactory.ExpressionStatement(
                codeFactory.AssignmentStatement(
                    codeFactory.MemberAccessExpression(
                    codeFactory.BaseExpression(),
                codeFactory.IdentifierName(overriddenProperty.Name)),
                codeFactory.IdentifierName("value")));
        }

        // Only generate a getter if the base getter is accessible.
        IMethodSymbol? accessorGet = null;
        if (overriddenProperty.GetMethod != null && overriddenProperty.GetMethod.IsAccessibleWithin(containingType))
        {
            accessorGet = CodeGenerationSymbolFactory.CreateMethodSymbol(
                overriddenProperty.GetMethod,
                accessibility: getAccessibility,
                statements: getBody != null ? [getBody] : [],
                modifiers: modifiers);
        }

        // Only generate a setter if the base setter is accessible.
        IMethodSymbol? accessorSet = null;
        if (overriddenProperty.SetMethod is { DeclaredAccessibility: not Accessibility.Private } &&
            overriddenProperty.SetMethod.IsAccessibleWithin(containingType))
        {
            accessorSet = CodeGenerationSymbolFactory.CreateMethodSymbol(
                overriddenProperty.SetMethod,
                accessibility: setAccessibility,
                statements: setBody != null ? [setBody] : [],
                modifiers: modifiers);
        }

        return CodeGenerationSymbolFactory.CreatePropertySymbol(
            overriddenProperty,
            accessibility: overriddenProperty.ComputeResultantAccessibility(containingType),
            modifiers: modifiers,
            name: overriddenProperty.Name,
            parameters: overriddenProperty.RemoveInaccessibleAttributesAndAttributesOfTypes(containingType).Parameters,
            isIndexer: overriddenProperty.IsIndexer(),
            getMethod: accessorGet,
            setMethod: accessorSet);
    }

    private static SyntaxNode WrapWithRefIfNecessary(SyntaxGenerator codeFactory, IPropertySymbol overriddenProperty, SyntaxNode body)
        => overriddenProperty.ReturnsByRef
            ? codeFactory.RefExpression(body)
            : body;

    public static IEventSymbol OverrideEvent(
        IEventSymbol overriddenEvent,
        DeclarationModifiers modifiers,
        INamedTypeSymbol newContainingType)
    {
        return CodeGenerationSymbolFactory.CreateEventSymbol(
            overriddenEvent,
            attributes: default,
            accessibility: overriddenEvent.ComputeResultantAccessibility(newContainingType),
            modifiers: modifiers,
            explicitInterfaceImplementations: default,
            name: overriddenEvent.Name);
    }

    public static async Task<ISymbol> OverrideAsync(
        this SyntaxGenerator generator,
        ISymbol symbol,
        INamedTypeSymbol containingType,
        Document document,
        DeclarationModifiers extraDeclarationModifiers = default,
        CancellationToken cancellationToken = default)
    {
        var modifiers = GetOverrideModifiers(symbol) + extraDeclarationModifiers;

        if (symbol is IMethodSymbol method)
        {
            return await generator.OverrideMethodAsync(method,
                modifiers, containingType, document, cancellationToken).ConfigureAwait(false);
        }
        else if (symbol is IPropertySymbol property)
        {
            return await generator.OverridePropertyAsync(property,
                modifiers, containingType, document, cancellationToken).ConfigureAwait(false);
        }
        else if (symbol is IEventSymbol ev)
        {
            return OverrideEvent(ev, modifiers, containingType);
        }
        else
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    private static DeclarationModifiers GetOverrideModifiers(ISymbol symbol)
        => symbol.GetSymbolModifiers()
                 .WithIsOverride(true)
                 .WithIsAbstract(false)
                 .WithIsVirtual(false);

    private static async Task<IMethodSymbol> OverrideMethodAsync(
        this SyntaxGenerator codeFactory,
        IMethodSymbol overriddenMethod,
        DeclarationModifiers modifiers,
        INamedTypeSymbol newContainingType,
        Document newDocument,
        CancellationToken cancellationToken)
    {
        // Required is not a valid modifier for methods, so clear it if the user typed it
        modifiers = modifiers.WithIsRequired(false);

        // Abstract: Throw not implemented
        if (overriddenMethod.IsAbstract)
        {
            var compilation = await newDocument.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var statement = codeFactory.CreateThrowNotImplementedStatement(compilation);

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                overriddenMethod,
                accessibility: overriddenMethod.ComputeResultantAccessibility(newContainingType),
                modifiers: modifiers,
                statements: [statement]);
        }
        else
        {
            // Otherwise, call the base method with the same parameters
            var typeParams = overriddenMethod.GetTypeArguments();
            var body = codeFactory.InvocationExpression(
                codeFactory.MemberAccessExpression(codeFactory.BaseExpression(),
                typeParams.IsDefaultOrEmpty
                    ? codeFactory.IdentifierName(overriddenMethod.Name)
                    : codeFactory.GenericName(overriddenMethod.Name, typeParams)),
                codeFactory.CreateArguments(overriddenMethod.GetParameters()));

            if (overriddenMethod.ReturnsByRef)
            {
                body = codeFactory.RefExpression(body);
            }

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                method: overriddenMethod.RemoveInaccessibleAttributesAndAttributesOfTypes(newContainingType),
                accessibility: overriddenMethod.ComputeResultantAccessibility(newContainingType),
                modifiers: modifiers,
                statements: overriddenMethod.ReturnsVoid
                    ? [codeFactory.ExpressionStatement(body)]
                    : [codeFactory.ReturnStatement(body)]);
        }
    }

    /// <summary>
    /// Generates a call to a method *through* an existing field or property symbol.
    /// </summary>
    /// <returns></returns>
    public static SyntaxNode GenerateDelegateThroughMemberStatement(
        this SyntaxGenerator generator, IMethodSymbol method, ISymbol throughMember)
    {
        var through = generator.MemberAccessExpression(
            CreateDelegateThroughExpression(generator, method, throughMember),
            method.IsGenericMethod
                ? generator.GenericName(method.Name, method.TypeArguments)
                : generator.IdentifierName(method.Name));

        var invocationExpression = generator.InvocationExpression(through, generator.CreateArguments(method.Parameters));
        return method.ReturnsVoid
            ? generator.ExpressionStatement(invocationExpression)
            : generator.ReturnStatement(invocationExpression);
    }

    public static SyntaxNode CreateDelegateThroughExpression(
        this SyntaxGenerator generator, ISymbol member, ISymbol throughMember)
    {
        var name = generator.IdentifierName(throughMember.Name);
        var through = throughMember.IsStatic
            ? GenerateContainerName(generator, throughMember)
            // If we're delegating through a primary constructor parameter, we cannot qualify the name at all.
            : throughMember is IParameterSymbol
                ? null
                : generator.ThisExpression();

        through = through is null ? name : generator.MemberAccessExpression(through, name);

        var throughMemberType = throughMember.GetMemberType();
        if (throughMemberType != null &&
            member.ContainingType is { TypeKind: TypeKind.Interface } interfaceBeingImplemented)
        {
            // In the case of 'implement interface through field / property', we need to know what
            // interface we are implementing so that we can insert casts to this interface on every
            // usage of the field in the generated code. Without these casts we would end up generating
            // code that fails compilation in certain situations.
            // 
            // For example consider the following code.
            //      class C : IReadOnlyList<int> { int[] field; }
            // When applying the 'implement interface through field' code fix in the above example,
            // we need to generate the following code to implement the Count property on IReadOnlyList<int>
            //      class C : IReadOnlyList<int> { int[] field; int Count { get { ((IReadOnlyList<int>)field).Count; } ...}
            // as opposed to the following code which will fail to compile (because the array field
            // doesn't have a property named .Count) -
            //      class C : IReadOnlyList<int> { int[] field; int Count { get { field.Count; } ...}
            //
            // The 'InterfaceTypes' property on the state object always contains only one item
            // in the case of C# i.e. it will contain exactly the interface we are trying to implement.
            // This is also the case most of the time in the case of VB, except in certain error conditions
            // (recursive / circular cases) where the span of the squiggle for the corresponding 
            // diagnostic (BC30149) changes and 'InterfaceTypes' ends up including all interfaces
            // in the Implements clause. For the purposes of inserting the above cast, we ignore the
            // uncommon case and optimize for the common one - in other words, we only apply the cast
            // in cases where we can unambiguously figure out which interface we are trying to implement.
            if (!throughMemberType.Equals(interfaceBeingImplemented))
            {
                through = generator.CastExpression(interfaceBeingImplemented,
                    through.WithAdditionalAnnotations(Simplifier.Annotation));
            }
            else if (throughMember is IPropertySymbol { IsStatic: false, ExplicitInterfaceImplementations: [var explicitlyImplementedProperty, ..] })
            {
                // If we are implementing through an explicitly implemented property, we need to cast 'this' to
                // the explicitly implemented interface type before calling the member, as in:
                //       ((IA)this).Prop.Member();
                //
                var explicitImplementationCast = generator.CastExpression(
                    explicitlyImplementedProperty.ContainingType,
                    generator.ThisExpression());

                through = generator.MemberAccessExpression(explicitImplementationCast,
                    generator.IdentifierName(explicitlyImplementedProperty.Name));

                through = through.WithAdditionalAnnotations(Simplifier.Annotation);
            }
        }

        return through.WithAdditionalAnnotations(Simplifier.Annotation);

        // local functions

        static SyntaxNode GenerateContainerName(SyntaxGenerator factory, ISymbol throughMember)
        {
            var classOrStructType = throughMember.ContainingType;
            return classOrStructType.IsGenericType
                ? factory.GenericName(classOrStructType.Name, classOrStructType.TypeArguments)
                : factory.IdentifierName(classOrStructType.Name);
        }
    }

    public static ImmutableArray<SyntaxNode> GetGetAccessorStatements(
        this SyntaxGenerator generator, Compilation compilation,
        IPropertySymbol property, ISymbol? throughMember, bool preferAutoProperties)
    {
        if (throughMember != null)
        {
            var throughExpression = CreateDelegateThroughExpression(generator, property, throughMember);
            var expression = property.IsIndexer
                ? throughExpression
                : generator.MemberAccessExpression(
                    throughExpression, generator.IdentifierName(property.Name));

            if (property.Parameters.Length > 0)
            {
                var arguments = generator.CreateArguments(property.Parameters);
                expression = generator.ElementAccessExpression(expression, arguments);
            }

            return [generator.ReturnStatement(expression)];
        }

        return preferAutoProperties ? default : generator.CreateThrowNotImplementedStatementBlock(compilation);
    }

    public static ImmutableArray<SyntaxNode> GetSetAccessorStatements(
        this SyntaxGenerator generator, Compilation compilation,
        IPropertySymbol property, ISymbol? throughMember, bool preferAutoProperties)
    {
        if (throughMember != null)
        {
            var throughExpression = CreateDelegateThroughExpression(generator, property, throughMember);
            var expression = property.IsIndexer
                ? throughExpression
                : generator.MemberAccessExpression(
                    throughExpression, generator.IdentifierName(property.Name));

            if (property.Parameters.Length > 0)
            {
                var arguments = generator.CreateArguments(property.Parameters);
                expression = generator.ElementAccessExpression(expression, arguments);
            }

            expression = generator.AssignmentStatement(expression, generator.IdentifierName("value"));

            return [generator.ExpressionStatement(expression)];
        }

        return preferAutoProperties
            ? default
            : generator.CreateThrowNotImplementedStatementBlock(compilation);
    }
}
