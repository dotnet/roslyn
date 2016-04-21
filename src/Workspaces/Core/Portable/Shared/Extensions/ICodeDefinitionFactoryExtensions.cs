// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ICodeDefinitionFactoryExtensions
    {
        public static SyntaxNode CreateThrowNotImplementStatement(
            this SyntaxGenerator codeDefinitionFactory,
            Compilation compilation)
        {
            return codeDefinitionFactory.ThrowStatement(
                codeDefinitionFactory.ObjectCreationExpression(
                    compilation.NotImplementedExceptionType(),
                    SpecializedCollections.EmptyList<SyntaxNode>()));
        }

        public static IList<SyntaxNode> CreateThrowNotImplementedStatementBlock(
            this SyntaxGenerator codeDefinitionFactory,
            Compilation compilation)
        {
            return new[] { CreateThrowNotImplementStatement(codeDefinitionFactory, compilation) };
        }

        public static IList<SyntaxNode> CreateArguments(
            this SyntaxGenerator factory,
            ImmutableArray<IParameterSymbol> parameters)
        {
            return parameters.Select(p => CreateArgument(factory, p)).ToList();
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
                attributes: null,
                accessibility: Accessibility.Public,
                modifiers: new DeclarationModifiers(),
                typeName: typeName,
                parameters: constructor.Parameters,
                statements: null,
                baseConstructorArguments: constructor.Parameters.Length == 0 ? null : factory.CreateArguments(constructor.Parameters));
        }

        public static IEnumerable<ISymbol> CreateFieldDelegatingConstructor(
            this SyntaxGenerator factory,
            string typeName,
            INamedTypeSymbol containingTypeOpt,
            IList<IParameterSymbol> parameters,
            IDictionary<string, ISymbol> parameterToExistingFieldMap,
            IDictionary<string, string> parameterToNewFieldMap,
            CancellationToken cancellationToken)
        {
            var fields = factory.CreateFieldsForParameters(parameters, parameterToNewFieldMap);
            var statements = factory.CreateAssignmentStatements(parameters, parameterToExistingFieldMap, parameterToNewFieldMap)
                                    .Select(s => s.WithAdditionalAnnotations(Simplifier.Annotation));

            foreach (var field in fields)
            {
                yield return field;
            }

            yield return CodeGenerationSymbolFactory.CreateConstructorSymbol(
                attributes: null,
                accessibility: Accessibility.Public,
                modifiers: new DeclarationModifiers(),
                typeName: typeName,
                parameters: parameters,
                statements: statements.ToList(),
                thisConstructorArguments: GetThisConstructorArguments(containingTypeOpt, parameterToExistingFieldMap));
        }

        private static IList<SyntaxNode> GetThisConstructorArguments(
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
                    return new List<SyntaxNode>();
                }
            }

            return null;
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
                    string fieldName;
                    if (TryGetValue(parameterToNewFieldMap, parameterName, out fieldName))
                    {
                        yield return CodeGenerationSymbolFactory.CreateFieldSymbol(
                            attributes: null,
                            accessibility: Accessibility.Private,
                            modifiers: default(DeclarationModifiers),
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
            ISymbol symbol;
            if (dictionary != null && dictionary.TryGetValue(key, out symbol))
            {
                value = symbol.Name;
                return true;
            }

            return false;
        }

        public static IEnumerable<SyntaxNode> CreateAssignmentStatements(
            this SyntaxGenerator factory,
            IList<IParameterSymbol> parameters,
            IDictionary<string, ISymbol> parameterToExistingFieldMap,
            IDictionary<string, string> parameterToNewFieldMap)
        {
            foreach (var parameter in parameters)
            {
                var refKind = parameter.RefKind;
                var parameterType = parameter.Type;
                var parameterName = parameter.Name;

                if (refKind == RefKind.Out)
                {
                    // If it's an out param, then don't create a field for it.  Instead, assign
                    // assign the default value for that type (i.e. "default(...)") to it.
                    var assignExpression = factory.AssignmentStatement(
                        factory.IdentifierName(parameterName),
                        factory.DefaultExpression(parameterType));
                    var statement = factory.ExpressionStatement(assignExpression);
                    yield return statement;
                }
                else
                {
                    // For non-out parameters, create a field and assign the parameter to it. 
                    // TODO: I'm not sure that's what we really want for ref parameters. 
                    string fieldName;
                    if (TryGetValue(parameterToExistingFieldMap, parameterName, out fieldName) ||
                        TryGetValue(parameterToNewFieldMap, parameterName, out fieldName))
                    {
                        var assignExpression = factory.AssignmentStatement(
                            factory.MemberAccessExpression(
                                factory.ThisExpression(),
                                factory.IdentifierName(fieldName)),
                            factory.IdentifierName(parameterName));
                        var statement = factory.ExpressionStatement(assignExpression);
                        yield return statement;
                    }
                }
            }
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
                getBody = codeFactory.CreateThrowNotImplementStatement(compilation);
                setBody = getBody;
            }
            else if (overriddenProperty.IsIndexer() && document.Project.Language == LanguageNames.CSharp)
            {
                // Indexer: return or set base[]. Only in C#, since VB must refer to these by name.
                getBody = codeFactory.ReturnStatement(
                    codeFactory.ElementAccessExpression(
                        codeFactory.BaseExpression(),
                        codeFactory.CreateArguments(overriddenProperty.Parameters)));

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
                    var getName = overriddenProperty.GetMethod != null ? overriddenProperty.GetMethod.Name : null;
                    var setName = overriddenProperty.SetMethod != null ? overriddenProperty.SetMethod.Name : null;

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
                        codeFactory.InvocationExpression(
                        codeFactory.MemberAccessExpression(
                            codeFactory.BaseExpression(),
                            codeFactory.IdentifierName(overriddenProperty.Name)), codeFactory.CreateArguments(overriddenProperty.Parameters)));
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
                    codeFactory.MemberAccessExpression(
                        codeFactory.BaseExpression(),
                        codeFactory.IdentifierName(overriddenProperty.Name)));
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
                    statements: new[] { getBody },
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
                    statements: new[] { setBody },
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

        public static IEventSymbol OverrideEvent(
            this SyntaxGenerator codeFactory,
            IEventSymbol overriddenEvent,
            DeclarationModifiers modifiers,
            INamedTypeSymbol newContainingType)
        {
            return CodeGenerationSymbolFactory.CreateEventSymbol(
                overriddenEvent,
                attributes: null,
                accessibility: overriddenEvent.ComputeResultantAccessibility(newContainingType),
                modifiers: modifiers,
                explicitInterfaceSymbol: null,
                name: overriddenEvent.Name);
        }

        public static async Task<IMethodSymbol> OverrideMethodAsync(
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
                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                    overriddenMethod,
                    accessibility: overriddenMethod.ComputeResultantAccessibility(newContainingType),
                    modifiers: modifiers,
                    statements: new[] { codeFactory.CreateThrowNotImplementStatement(compilation) });
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

                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                    method: overriddenMethod,
                    accessibility: overriddenMethod.ComputeResultantAccessibility(newContainingType),
                    modifiers: modifiers,
                    statements: overriddenMethod.ReturnsVoid
                        ? new SyntaxNode[] { codeFactory.ExpressionStatement(body) }
                        : new SyntaxNode[] { codeFactory.ReturnStatement(body) });
            }
        }
    }
}
