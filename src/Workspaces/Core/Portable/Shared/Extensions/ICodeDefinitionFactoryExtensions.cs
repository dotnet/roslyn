﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ICodeDefinitionFactoryExtensions
    {
        public static SyntaxNode CreateThrowNotImplementedStatement(
            this SyntaxGenerator codeDefinitionFactory,
            Compilation compilation)
        {
            return codeDefinitionFactory.ThrowStatement(
               codeDefinitionFactory.ObjectCreationExpression(
                   codeDefinitionFactory.TypeExpression(compilation.NotImplementedExceptionType(), addImport: false),
                   SpecializedCollections.EmptyList<SyntaxNode>()));
        }

        public static ImmutableArray<SyntaxNode> CreateThrowNotImplementedStatementBlock(
            this SyntaxGenerator codeDefinitionFactory, Compilation compilation)
            => ImmutableArray.Create(CreateThrowNotImplementedStatement(codeDefinitionFactory, compilation));

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

        public static IEnumerable<ISymbol> CreateFieldDelegatingConstructor(
            this SyntaxGenerator factory,
            Compilation compilation,
            string typeName,
            INamedTypeSymbol containingTypeOpt,
            ImmutableArray<IParameterSymbol> parameters,
            IDictionary<string, ISymbol> parameterToExistingFieldMap,
            IDictionary<string, string> parameterToNewFieldMap,
            bool addNullChecks,
            bool preferThrowExpression,
            CancellationToken cancellationToken)
        {
            var fields = factory.CreateFieldsForParameters(parameters, parameterToNewFieldMap);
            var statements = factory.CreateAssignmentStatements(
                compilation, parameters, parameterToExistingFieldMap, parameterToNewFieldMap, 
                addNullChecks, preferThrowExpression).SelectAsArray(
                    s => s.WithAdditionalAnnotations(Simplifier.Annotation));

            foreach (var field in fields)
            {
                yield return field;
            }

            yield return CodeGenerationSymbolFactory.CreateConstructorSymbol(
                attributes: default,
                accessibility: Accessibility.Public,
                modifiers: new DeclarationModifiers(),
                typeName: typeName,
                parameters: parameters,
                statements: statements,
                thisConstructorArguments: GetThisConstructorArguments(containingTypeOpt, parameterToExistingFieldMap));
        }

        private static ImmutableArray<SyntaxNode> GetThisConstructorArguments(
            INamedTypeSymbol containingTypeOpt,
            IDictionary<string, ISymbol> parameterToExistingFieldMap)
        {
            if (containingTypeOpt != null && containingTypeOpt.TypeKind == TypeKind.Struct)
            {
                // Special case.  If we're generating a struct constructor, then we'll need
                // to initialize all fields in the struct, not just the ones we're creating.  To
                // do that, we call the default constructor.
                var realFields = containingTypeOpt.GetMembers()
                                     .OfType<IFieldSymbol>()
                                     .Where(f => !f.IsStatic);
                var initializedFields = parameterToExistingFieldMap.Values
                                            .OfType<IFieldSymbol>()
                                            .Where(f => !f.IsImplicitlyDeclared && !f.IsStatic);
                if (initializedFields.Count() < realFields.Count())
                {
                    // We have less field assignments than actual fields.  Generate a call to the
                    // default constructor as well.
                    return ImmutableArray<SyntaxNode>.Empty;
                }
            }

            return default;
        }

        public static IEnumerable<IFieldSymbol> CreateFieldsForParameters(
            this SyntaxGenerator factory,
            IList<IParameterSymbol> parameters,
            IDictionary<string, string> parameterToNewFieldMap)
        {
            foreach (var parameter in parameters)
            {
                var refKind = parameter.RefKind;
                var parameterType = parameter.Type;
                var parameterName = parameter.Name;

                if (refKind != RefKind.Out)
                {
                    // For non-out parameters, create a field and assign the parameter to it. 
                    // TODO: I'm not sure that's what we really want for ref parameters. 
                    if (TryGetValue(parameterToNewFieldMap, parameterName, out var fieldName))
                    {
                        yield return CodeGenerationSymbolFactory.CreateFieldSymbol(
                            attributes: default,
                            accessibility: Accessibility.Private,
                            modifiers: default,
                            type: parameterType,
                            name: parameterToNewFieldMap[parameterName]);
                    }
                }
            }
        }

        private static bool TryGetValue(IDictionary<string, string> dictionary, string key, out string value)
        {
            value = null;
            return
                dictionary != null &&
                dictionary.TryGetValue(key, out value);
        }

        private static bool TryGetValue(IDictionary<string, ISymbol> dictionary, string key, out string value)
        {
            value = null;
            if (dictionary != null && dictionary.TryGetValue(key, out var symbol))
            {
                value = symbol.Name;
                return true;
            }

            return false;
        }

        public static SyntaxNode CreateThrowArgumentNullExpression(
            this SyntaxGenerator factory,
            Compilation compilation,
            IParameterSymbol parameter)
        {
            return factory.ThrowExpression(
                factory.ObjectCreationExpression(
                    compilation.GetTypeByMetadataName("System.ArgumentNullException"),
                    factory.NameOfExpression(
                        factory.IdentifierName(parameter.Name))));
        }

        public static SyntaxNode CreateIfNullThrowStatement(
            this SyntaxGenerator factory,
            Compilation compilation,
            IParameterSymbol parameter)
        {
            return factory.IfStatement(
                factory.ReferenceEqualsExpression(
                    factory.IdentifierName(parameter.Name),
                    factory.NullLiteralExpression()),
                SpecializedCollections.SingletonEnumerable(
                    factory.ExpressionStatement(
                        factory.CreateThrowArgumentNullExpression(compilation, parameter))));
        }

        public static ImmutableArray<SyntaxNode> CreateAssignmentStatements(
            this SyntaxGenerator factory,
            Compilation compilation,
            IList<IParameterSymbol> parameters,
            IDictionary<string, ISymbol> parameterToExistingFieldMap,
            IDictionary<string, string> parameterToNewFieldMap,
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
                            compilation, parameter, fieldAccess,
                            addNullChecks, preferThrowExpression,
                            nullCheckStatements, assignStatements);
                    }
                }
            }

            return nullCheckStatements.ToImmutableAndFree().Concat(assignStatements.ToImmutableAndFree());
        }

        public static void AddAssignmentStatements(
             this SyntaxGenerator factory,
             Compilation compilation,
             IParameterSymbol parameter,
             SyntaxNode fieldAccess,
             bool addNullChecks,
             bool preferThrowExpression,
             ArrayBuilder<SyntaxNode> nullCheckStatements,
             ArrayBuilder<SyntaxNode> assignStatements)
        {
            var shouldAddNullCheck = addNullChecks && parameter.Type.CanAddNullCheck();
            if (shouldAddNullCheck && preferThrowExpression)
            {
                // Generate: this.x = x ?? throw ...
                assignStatements.Add(CreateAssignWithNullCheckStatement(
                    factory, compilation, parameter, fieldAccess));
            }
            else
            {
                if (shouldAddNullCheck)
                {
                    // generate: if (x == null) throw ...
                    nullCheckStatements.Add(
                        factory.CreateIfNullThrowStatement(compilation, parameter));
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

            SyntaxNode getBody = null;
            SyntaxNode setBody = null;

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
                    && (await SymbolFinder.FindSourceDefinitionAsync(overriddenProperty, document.Project.Solution, cancellationToken).ConfigureAwait(false))
                        .Language == LanguageNames.VisualBasic)
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
            IMethodSymbol accessorGet = null;
            if (overriddenProperty.GetMethod != null && overriddenProperty.GetMethod.IsAccessibleWithin(containingType))
            {
                accessorGet = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    overriddenProperty.GetMethod,
                    accessibility: getAccessibility,
                    statements: ImmutableArray.Create(getBody),
                    modifiers: modifiers);
            }

            // Only generate a setter if the base setter is accessible.
            IMethodSymbol accessorSet = null;
            if (overriddenProperty.SetMethod != null &&
                overriddenProperty.SetMethod.IsAccessibleWithin(containingType) &&
                overriddenProperty.SetMethod.DeclaredAccessibility != Accessibility.Private)
            {
                accessorSet = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    overriddenProperty.SetMethod,
                    accessibility: setAccessibility,
                    statements: ImmutableArray.Create(setBody),
                    modifiers: modifiers);
            }

            return CodeGenerationSymbolFactory.CreatePropertySymbol(
                overriddenProperty,
                accessibility: overriddenProperty.ComputeResultantAccessibility(containingType),
                modifiers: modifiers,
                name: overriddenProperty.Name,
                isIndexer: overriddenProperty.IsIndexer(),
                getMethod: accessorGet,
                setMethod: accessorSet);
        }

        private static SyntaxNode WrapWithRefIfNecessary(SyntaxGenerator codeFactory, IPropertySymbol overriddenProperty, SyntaxNode body)
            => overriddenProperty.ReturnsByRef
                ? codeFactory.RefExpression(body)
                : body;

        public static IEventSymbol OverrideEvent(
            this SyntaxGenerator codeFactory,
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
            DeclarationModifiers? modifiersOpt = null,
            CancellationToken cancellationToken = default)
        {
            var modifiers = modifiersOpt ?? GetOverrideModifiers(symbol);

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
                return generator.OverrideEvent(ev, modifiers, containingType);
            }
            else
            {
                throw ExceptionUtilities.Unreachable;
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
            // Abstract: Throw not implemented
            if (overriddenMethod.IsAbstract)
            {
                var compilation = await newDocument.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var statement = codeFactory.CreateThrowNotImplementedStatement(compilation);

                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                    overriddenMethod,
                    accessibility: overriddenMethod.ComputeResultantAccessibility(newContainingType),
                    modifiers: modifiers,
                    statements: ImmutableArray.Create(statement));
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
                    method: overriddenMethod,
                    accessibility: overriddenMethod.ComputeResultantAccessibility(newContainingType),
                    modifiers: modifiers,
                    statements: overriddenMethod.ReturnsVoid
                        ? ImmutableArray.Create(codeFactory.ExpressionStatement(body))
                        : ImmutableArray.Create(codeFactory.ReturnStatement(body)));
            }
        }
    }
}
