// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

#if CODE_STYLE
using DeclarationModifiers = Microsoft.CodeAnalysis.Internal.Editing.DeclarationModifiers;
#else
using DeclarationModifiers = Microsoft.CodeAnalysis.Editing.DeclarationModifiers;
#endif

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class SyntaxGeneratorExtensions
{
    private const string EqualsName = "Equals";
    private const string DefaultName = "Default";
    private const string ObjName = "obj";
    public const string OtherName = "other";

    public static SyntaxNode CreateThrowNotImplementedStatement(
        this SyntaxGenerator codeDefinitionFactory, Compilation compilation)
    {
        return codeDefinitionFactory.ThrowStatement(
           CreateNewNotImplementedException(codeDefinitionFactory, compilation));
    }

    public static SyntaxNode CreateThrowNotImplementedExpression(
        this SyntaxGenerator codeDefinitionFactory, Compilation compilation)
    {
        return codeDefinitionFactory.ThrowExpression(
           CreateNewNotImplementedException(codeDefinitionFactory, compilation));
    }

    private static SyntaxNode CreateNewNotImplementedException(SyntaxGenerator codeDefinitionFactory, Compilation compilation)
    {
        var notImplementedExceptionTypeSyntax = compilation.NotImplementedExceptionType() is INamedTypeSymbol symbol
            ? codeDefinitionFactory.TypeExpression(symbol, addImport: false)
            : codeDefinitionFactory.QualifiedName(codeDefinitionFactory.IdentifierName(nameof(System)), codeDefinitionFactory.IdentifierName(nameof(NotImplementedException)));

        return codeDefinitionFactory.ObjectCreationExpression(
            notImplementedExceptionTypeSyntax,
            arguments: []);
    }

    public static ImmutableArray<SyntaxNode> CreateThrowNotImplementedStatementBlock(
        this SyntaxGenerator codeDefinitionFactory, Compilation compilation)
        => [CreateThrowNotImplementedStatement(codeDefinitionFactory, compilation)];

    public static ImmutableArray<SyntaxNode> CreateArguments(
        this SyntaxGenerator factory,
        ImmutableArray<IParameterSymbol> parameters)
    {
        return parameters.SelectAsArray(p => CreateArgument(factory, p));
    }

    private static SyntaxNode CreateArgument(
        this SyntaxGenerator factory,
        IParameterSymbol parameter)
    {
        return factory.Argument(parameter.RefKind, factory.IdentifierName(parameter.Name));
    }

    public static SyntaxNode GetDefaultEqualityComparer(
        this SyntaxGenerator factory,
        SyntaxGeneratorInternal generatorInternal,
        Compilation compilation,
        ITypeSymbol type)
    {
        var equalityComparerType = compilation.EqualityComparerOfTType();
        var typeExpression = equalityComparerType == null
            ? factory.GenericName(nameof(EqualityComparer<int>), type)
            : generatorInternal.Type(equalityComparerType.Construct(type), typeContext: false);

        return factory.MemberAccessExpression(typeExpression, factory.IdentifierName(DefaultName));
    }

    private static ITypeSymbol GetType(Compilation compilation, ISymbol symbol)
        => symbol switch
        {
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            _ => compilation.GetSpecialType(SpecialType.System_Object),
        };

    public static SyntaxNode IsPatternExpression(this SyntaxGeneratorInternal generator, SyntaxNode expression, SyntaxNode pattern)
        => generator.IsPatternExpression(expression, isToken: default, pattern);

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
    public static SyntaxNode CreateNullCheckExpression(this SyntaxGenerator factory, SemanticModel semanticModel, string identifierName)
    {
        var identifier = factory.IdentifierName(identifierName);
        var nullExpr = factory.NullLiteralExpression();
        var condition = factory.SyntaxGeneratorInternal.SupportsPatterns(semanticModel.SyntaxTree.Options)
            ? factory.SyntaxGeneratorInternal.IsPatternExpression(identifier, factory.SyntaxGeneratorInternal.ConstantPattern(nullExpr))
            : factory.ReferenceEqualsExpression(identifier, nullExpr);
        return condition;
    }

    public static SyntaxNode CreateThrowArgumentNullExceptionStatement(this SyntaxGenerator factory, Compilation compilation, IParameterSymbol parameter)
        => factory.ThrowStatement(CreateNewArgumentNullException(factory, compilation, parameter));
}
