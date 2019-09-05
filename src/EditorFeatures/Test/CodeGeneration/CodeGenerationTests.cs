// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration
{
    [UseExportProvider]
    public partial class CodeGenerationTests
    {
        internal static async Task TestAddNamespaceAsync(
            string initial,
            string expected,
            string name = "N",
            IList<ISymbol> imports = null,
            IList<INamespaceOrTypeSymbol> members = null,
            CodeGenerationOptions codeGenerationOptions = null)
        {
            using var context = await TestContext.CreateAsync(initial, expected);
            var @namespace = CodeGenerationSymbolFactory.CreateNamespaceSymbol(name, imports, members);
            context.Result = await context.Service.AddNamespaceAsync(context.Solution, (INamespaceSymbol)context.GetDestination(), @namespace, codeGenerationOptions);
        }

        internal static async Task TestAddFieldAsync(
            string initial,
            string expected,
            Func<SemanticModel, ITypeSymbol> type = null,
            string name = "F",
            Accessibility accessibility = Accessibility.Public,
            Editing.DeclarationModifiers modifiers = default,
            CodeGenerationOptions codeGenerationOptions = null,
            bool hasConstantValue = false,
            object constantValue = null,
            bool addToCompilationUnit = false)
        {
            using var context = await TestContext.CreateAsync(initial, expected);
            var typeSymbol = type != null ? type(context.SemanticModel) : null;
            var field = CodeGenerationSymbolFactory.CreateFieldSymbol(
                attributes: default,
                accessibility,
                modifiers,
                typeSymbol,
                name,
                hasConstantValue,
                constantValue);
            if (!addToCompilationUnit)
            {
                context.Result = await context.Service.AddFieldAsync(context.Solution, (INamedTypeSymbol)context.GetDestination(), field, codeGenerationOptions);
            }
            else
            {
                var newRoot = context.Service.AddField(await context.Document.GetSyntaxRootAsync(), field, codeGenerationOptions);
                context.Result = context.Document.WithSyntaxRoot(newRoot);
            }
        }

        internal static async Task TestAddConstructorAsync(
            string initial,
            string expected,
            string name = "C",
            Accessibility accessibility = Accessibility.Public,
            Editing.DeclarationModifiers modifiers = default,
            ImmutableArray<Func<SemanticModel, IParameterSymbol>> parameters = default,
            ImmutableArray<SyntaxNode> statements = default,
            ImmutableArray<SyntaxNode> baseArguments = default,
            ImmutableArray<SyntaxNode> thisArguments = default,
            CodeGenerationOptions codeGenerationOptions = default)
        {
            using var context = await TestContext.CreateAsync(initial, expected);
            var parameterSymbols = GetParameterSymbols(parameters, context);
            var ctor = CodeGenerationSymbolFactory.CreateConstructorSymbol(
                attributes: default,
                accessibility,
                modifiers,
                name,
                parameterSymbols,
                statements,
                baseConstructorArguments: baseArguments,
                thisConstructorArguments: thisArguments);
            context.Result = await context.Service.AddMethodAsync(context.Solution, (INamedTypeSymbol)context.GetDestination(), ctor, codeGenerationOptions);
        }

        internal static async Task TestAddMethodAsync(
            string initial,
            string expected,
            string name = "M",
            Accessibility accessibility = Accessibility.Public,
            Editing.DeclarationModifiers modifiers = default,
            Type returnType = null,
            Func<SemanticModel, ImmutableArray<IMethodSymbol>> getExplicitInterfaces = null,
            ImmutableArray<ITypeParameterSymbol> typeParameters = default,
            ImmutableArray<Func<SemanticModel, IParameterSymbol>> parameters = default,
            string statements = null,
            ImmutableArray<SyntaxNode> handlesExpressions = default,
            CodeGenerationOptions codeGenerationOptions = default)
        {
            if (statements != null)
            {
                expected = expected.Replace("$$", statements);
            }

            using var context = await TestContext.CreateAsync(initial, expected);
            var parameterSymbols = GetParameterSymbols(parameters, context);
            var parsedStatements = context.ParseStatements(statements);
            var explicitInterfaceImplementations = GetMethodSymbols(getExplicitInterfaces, context);
            var method = CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: default,
                accessibility,
                modifiers,
                GetTypeSymbol(returnType)(context.SemanticModel),
                RefKind.None,
                explicitInterfaceImplementations,
                name,
                typeParameters,
                parameterSymbols,
                parsedStatements,
                handlesExpressions: handlesExpressions);
            context.Result = await context.Service.AddMethodAsync(context.Solution, (INamedTypeSymbol)context.GetDestination(), method, codeGenerationOptions);
        }

        internal static async Task TestAddOperatorsAsync(
            string initial,
            string expected,
            CodeGenerationOperatorKind[] operatorKinds,
            Accessibility accessibility = Accessibility.Public,
            Editing.DeclarationModifiers modifiers = default,
            Type returnType = null,
            ImmutableArray<Func<SemanticModel, IParameterSymbol>> parameters = default,
            string statements = null,
            CodeGenerationOptions codeGenerationOptions = default)
        {
            if (statements != null)
            {
                while (expected.IndexOf("$$", StringComparison.Ordinal) != -1)
                {
                    expected = expected.Replace("$$", statements);
                }
            }

            using var context = await TestContext.CreateAsync(initial, expected);
            var parameterSymbols = GetParameterSymbols(parameters, context);
            var parsedStatements = context.ParseStatements(statements);

            var methods = operatorKinds.Select(kind => CodeGenerationSymbolFactory.CreateOperatorSymbol(
                attributes: default,
                accessibility,
                modifiers,
                GetTypeSymbol(returnType)(context.SemanticModel),
                kind,
                parameterSymbols,
                parsedStatements));

            context.Result = await context.Service.AddMembersAsync(context.Solution, (INamedTypeSymbol)context.GetDestination(), methods.ToArray(), codeGenerationOptions);
        }

        internal static async Task TestAddUnsupportedOperatorAsync(
            string initial,
            CodeGenerationOperatorKind operatorKind,
            Accessibility accessibility = Accessibility.Public,
            Editing.DeclarationModifiers modifiers = default,
            Type returnType = null,
            ImmutableArray<Func<SemanticModel, IParameterSymbol>> parameters = default,
            string statements = null,
            CodeGenerationOptions codeGenerationOptions = default)
        {
            using var context = await TestContext.CreateAsync(initial, initial, ignoreResult: true);
            var parameterSymbols = GetParameterSymbols(parameters, context);
            var parsedStatements = context.ParseStatements(statements);

            var method = CodeGenerationSymbolFactory.CreateOperatorSymbol(
                attributes: default,
                accessibility,
                modifiers,
                GetTypeSymbol(returnType)(context.SemanticModel),
                operatorKind,
                parameterSymbols,
                parsedStatements);

            ArgumentException exception = null;
            try
            {
                await context.Service.AddMethodAsync(context.Solution, (INamedTypeSymbol)context.GetDestination(), method, codeGenerationOptions);
            }
            catch (ArgumentException e)
            {
                exception = e;
            }

            var expectedMessage = string.Format(WorkspacesResources.Cannot_generate_code_for_unsupported_operator_0, method.Name);
            Assert.True(exception != null && exception.Message.StartsWith(expectedMessage, StringComparison.Ordinal),
                string.Format("\r\nExpected exception: {0}\r\nActual exception: {1}\r\n", expectedMessage, exception == null ? "no exception" : exception.Message));
        }

        internal static async Task TestAddConversionAsync(
            string initial,
            string expected,
            Type toType,
            Func<SemanticModel, IParameterSymbol> fromType,
            bool isImplicit = false,
            Accessibility accessibility = Accessibility.Public,
            Editing.DeclarationModifiers modifiers = default,
            string statements = null,
            CodeGenerationOptions codeGenerationOptions = default)
        {
            if (statements != null)
            {
                expected = expected.Replace("$$", statements);
            }

            using var context = await TestContext.CreateAsync(initial, expected);
            var parsedStatements = context.ParseStatements(statements);
            var method = CodeGenerationSymbolFactory.CreateConversionSymbol(
                attributes: default,
                accessibility,
                modifiers,
                GetTypeSymbol(toType)(context.SemanticModel),
                fromType(context.SemanticModel),
                isImplicit,
                parsedStatements);

            context.Result = await context.Service.AddMethodAsync(context.Solution, (INamedTypeSymbol)context.GetDestination(), method, codeGenerationOptions);
        }

        internal static async Task TestAddStatementsAsync(
            string initial,
            string expected,
            string statements,
            CodeGenerationOptions codeGenerationOptions = default)
        {
            if (statements != null)
            {
                expected = expected.Replace("$$", statements);
            }

            using var context = await TestContext.CreateAsync(initial, expected);
            var parsedStatements = context.ParseStatements(statements);
            var oldSyntax = context.GetSelectedSyntax<SyntaxNode>(true);
            var newSyntax = context.Service.AddStatements(oldSyntax, parsedStatements, codeGenerationOptions);
            context.Result = context.Document.WithSyntaxRoot((await context.Document.GetSyntaxRootAsync()).ReplaceNode(oldSyntax, newSyntax));
        }

        internal static async Task TestAddParametersAsync(
            string initial,
            string expected,
            ImmutableArray<Func<SemanticModel, IParameterSymbol>> parameters,
            CodeGenerationOptions codeGenerationOptions = default)
        {
            using var context = await TestContext.CreateAsync(initial, expected);
            var parameterSymbols = GetParameterSymbols(parameters, context);
            var oldMemberSyntax = context.GetSelectedSyntax<SyntaxNode>(true);
            var newMemberSyntax = context.Service.AddParameters(oldMemberSyntax, parameterSymbols, codeGenerationOptions);
            context.Result = context.Document.WithSyntaxRoot((await context.Document.GetSyntaxRootAsync()).ReplaceNode(oldMemberSyntax, newMemberSyntax));
        }

        internal static async Task TestAddDelegateTypeAsync(
            string initial,
            string expected,
            string name = "D",
            Accessibility accessibility = Accessibility.Public,
            Editing.DeclarationModifiers modifiers = default,
            Type returnType = null,
            ImmutableArray<ITypeParameterSymbol> typeParameters = default,
            ImmutableArray<Func<SemanticModel, IParameterSymbol>> parameters = default,
            CodeGenerationOptions codeGenerationOptions = default)
        {
            using var context = await TestContext.CreateAsync(initial, expected);
            var parameterSymbols = GetParameterSymbols(parameters, context);
            var type = CodeGenerationSymbolFactory.CreateDelegateTypeSymbol(
                attributes: default,
                accessibility,
                modifiers,
                GetTypeSymbol(returnType)(context.SemanticModel),
                RefKind.None,
                name,
                typeParameters,
                parameterSymbols);
            context.Result = await context.Service.AddNamedTypeAsync(context.Solution, (INamedTypeSymbol)context.GetDestination(), type, codeGenerationOptions);
        }

        internal static async Task TestAddEventAsync(
            string initial,
            string expected,
            string name = "E",
            ImmutableArray<AttributeData> attributes = default,
            Accessibility accessibility = Accessibility.Public,
            Editing.DeclarationModifiers modifiers = default,
            ImmutableArray<Func<SemanticModel, IParameterSymbol>> parameters = default,
            Type type = null,
            Func<SemanticModel, ImmutableArray<IEventSymbol>> getExplicitInterfaceImplementations = null,
            IMethodSymbol addMethod = null,
            IMethodSymbol removeMethod = null,
            IMethodSymbol raiseMethod = null,
            CodeGenerationOptions codeGenerationOptions = default)
        {
            using var context = await TestContext.CreateAsync(initial, expected);
            type ??= typeof(Action);

            var parameterSymbols = GetParameterSymbols(parameters, context);
            var typeSymbol = GetTypeSymbol(type)(context.SemanticModel);
            var @event = CodeGenerationSymbolFactory.CreateEventSymbol(
                attributes,
                accessibility,
                modifiers,
                typeSymbol,
                getExplicitInterfaceImplementations?.Invoke(context.SemanticModel) ?? default,
                name,
                addMethod,
                removeMethod,
                raiseMethod);
            context.Result = await context.Service.AddEventAsync(context.Solution, (INamedTypeSymbol)context.GetDestination(), @event, codeGenerationOptions);
        }

        internal static async Task TestAddPropertyAsync(
            string initial,
            string expected,
            string name = "P",
            Accessibility defaultAccessibility = Accessibility.Public,
            Accessibility setterAccessibility = Accessibility.Public,
            Editing.DeclarationModifiers modifiers = default,
            string getStatements = null,
            string setStatements = null,
            Type type = null,
            ImmutableArray<IPropertySymbol> explicitInterfaceImplementations = default,
            ImmutableArray<Func<SemanticModel, IParameterSymbol>> parameters = default,
            bool isIndexer = false,
            CodeGenerationOptions codeGenerationOptions = default,
            IDictionary<OptionKey, object> options = null)
        {
            // This assumes that tests will not use place holders for get/set statements at the same time
            if (getStatements != null)
            {
                expected = expected.Replace("$$", getStatements);
            }

            if (setStatements != null)
            {
                expected = expected.Replace("$$", setStatements);
            }

            using var context = await TestContext.CreateAsync(initial, expected);
            if (options != null)
            {
                foreach (var kvp in options)
                {
                    context.Workspace.Options = context.Workspace.Options.WithChangedOption(kvp.Key, kvp.Value);
                }
            }

            var typeSymbol = GetTypeSymbol(type)(context.SemanticModel);
            var getParameterSymbols = GetParameterSymbols(parameters, context);
            var setParameterSymbols = getParameterSymbols == null
                ? default
                : getParameterSymbols.Add(Parameter(type, "value")(context.SemanticModel));
            var getAccessor = CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: default,
                defaultAccessibility,
                new Editing.DeclarationModifiers(isAbstract: getStatements == null),
                typeSymbol,
                RefKind.None,
                explicitInterfaceImplementations: default,
                "get_" + name,
                typeParameters: default,
                getParameterSymbols,
                statements: context.ParseStatements(getStatements));
            var setAccessor = CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: default,
                setterAccessibility,
                new Editing.DeclarationModifiers(isAbstract: setStatements == null),
                GetTypeSymbol(typeof(void))(context.SemanticModel),
                RefKind.None,
                explicitInterfaceImplementations: default,
                "set_" + name,
                typeParameters: default,
                setParameterSymbols,
                statements: context.ParseStatements(setStatements));

            // If get is provided but set isn't, we don't want an accessor for set
            if (getStatements != null && setStatements == null)
            {
                setAccessor = null;
            }

            // If set is provided but get isn't, we don't want an accessor for get
            if (getStatements == null && setStatements != null)
            {
                getAccessor = null;
            }

            var property = CodeGenerationSymbolFactory.CreatePropertySymbol(
                attributes: default,
                defaultAccessibility,
                modifiers,
                typeSymbol,
                RefKind.None,
                explicitInterfaceImplementations,
                name,
                getParameterSymbols,
                getAccessor,
                setAccessor,
                isIndexer);
            context.Result = await context.Service.AddPropertyAsync(context.Solution, (INamedTypeSymbol)context.GetDestination(), property, codeGenerationOptions);
        }

        internal static async Task TestAddNamedTypeAsync(
            string initial,
            string expected,
            string name = "C",
            Accessibility accessibility = Accessibility.Public,
            Editing.DeclarationModifiers modifiers = default,
            TypeKind typeKind = TypeKind.Class,
            ImmutableArray<ITypeParameterSymbol> typeParameters = default,
            INamedTypeSymbol baseType = null,
            ImmutableArray<INamedTypeSymbol> interfaces = default,
            SpecialType specialType = SpecialType.None,
            ImmutableArray<Func<SemanticModel, ISymbol>> members = default,
            CodeGenerationOptions codeGenerationOptions = default)
        {
            using var context = await TestContext.CreateAsync(initial, expected);
            var memberSymbols = GetSymbols(members, context);
            var type = CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                attributes: default, accessibility, modifiers, typeKind, name,
                typeParameters, baseType, interfaces, specialType, memberSymbols);
            context.Result = await context.Service.AddNamedTypeAsync(context.Solution, (INamespaceSymbol)context.GetDestination(), type, codeGenerationOptions);
        }

        internal static async Task TestAddAttributeAsync(
            string initial,
            string expected,
            Type attributeClass,
            SyntaxToken? target = null)
        {
            using var context = await TestContext.CreateAsync(initial, expected);
            var attr = CodeGenerationSymbolFactory.CreateAttributeData((INamedTypeSymbol)GetTypeSymbol(attributeClass)(context.SemanticModel));
            var oldNode = context.GetDestinationNode();
            var newNode = CodeGenerator.AddAttributes(oldNode, context.Document.Project.Solution.Workspace, new[] { attr }, target)
                                       .WithAdditionalAnnotations(Formatter.Annotation);
            context.Result = context.Document.WithSyntaxRoot(context.SemanticModel.SyntaxTree.GetRoot().ReplaceNode(oldNode, newNode));
        }

        internal static async Task TestRemoveAttributeAsync<T>(
            string initial,
            string expected,
            Type attributeClass,
            SyntaxToken? target = null) where T : SyntaxNode
        {
            using var context = await TestContext.CreateAsync(initial, expected);
            var attributeType = (INamedTypeSymbol)GetTypeSymbol(attributeClass)(context.SemanticModel);
            var taggedNode = context.GetDestinationNode();
            var attributeTarget = context.SemanticModel.GetDeclaredSymbol(taggedNode);
            var attribute = attributeTarget.GetAttributes().Single(attr => Equals(attr.AttributeClass, attributeType));
            var declarationNode = taggedNode.FirstAncestorOrSelf<T>();
            var newNode = CodeGenerator.RemoveAttribute(declarationNode, context.Document.Project.Solution.Workspace, attribute)
                                       .WithAdditionalAnnotations(Formatter.Annotation);
            context.Result = context.Document.WithSyntaxRoot(context.SemanticModel.SyntaxTree.GetRoot().ReplaceNode(declarationNode, newNode));
        }

        internal static async Task TestUpdateDeclarationAsync<T>(
            string initial,
            string expected,
            Accessibility? accessibility = null,
            IEnumerable<SyntaxToken> modifiers = null,
            Func<SemanticModel, ITypeSymbol> getType = null,
            ImmutableArray<Func<SemanticModel, ISymbol>> getNewMembers = default,
            bool? declareNewMembersAtTop = null,
            string retainedMembersKey = "RetainedMember") where T : SyntaxNode
        {
            using var context = await TestContext.CreateAsync(initial, expected);
            var declarationNode = context.GetDestinationNode().FirstAncestorOrSelf<T>();
            var updatedDeclarationNode = declarationNode;
            var workspace = context.Document.Project.Solution.Workspace;

            if (accessibility.HasValue)
            {
                updatedDeclarationNode = CodeGenerator.UpdateDeclarationAccessibility(declarationNode, workspace, accessibility.Value);
            }
            else if (modifiers != null)
            {
                updatedDeclarationNode = CodeGenerator.UpdateDeclarationModifiers(declarationNode, workspace, modifiers);
            }
            else if (getType != null)
            {
                updatedDeclarationNode = CodeGenerator.UpdateDeclarationType(declarationNode, workspace, getType(context.SemanticModel));
            }
            else if (getNewMembers != null)
            {
                var retainedMembers = context.GetAnnotatedDeclaredSymbols(retainedMembersKey, context.SemanticModel);
                var newMembersToAdd = GetSymbols(getNewMembers, context);
                var allMembers = new List<ISymbol>();
                if (declareNewMembersAtTop.HasValue && declareNewMembersAtTop.Value)
                {
                    allMembers.AddRange(newMembersToAdd);
                    allMembers.AddRange(retainedMembers);
                }
                else
                {
                    allMembers.AddRange(retainedMembers);
                    allMembers.AddRange(newMembersToAdd);
                }

                updatedDeclarationNode = CodeGenerator.UpdateDeclarationMembers(declarationNode, workspace, allMembers);
            }

            updatedDeclarationNode = updatedDeclarationNode.WithAdditionalAnnotations(Formatter.Annotation);
            context.Result = context.Document.WithSyntaxRoot(context.SemanticModel.SyntaxTree.GetRoot().ReplaceNode(declarationNode, updatedDeclarationNode));
        }

        internal static async Task TestGenerateFromSourceSymbolAsync(
            string symbolSource,
            string initial,
            string expected,
            bool onlyGenerateMembers = false,
            CodeGenerationOptions codeGenerationOptions = default,
            string forceLanguage = null)
        {
            using var context = await TestContext.CreateAsync(initial, expected, forceLanguage);
            var destSpan = new TextSpan();
            MarkupTestFile.GetSpan(symbolSource.NormalizeLineEndings(), out symbolSource, out destSpan);

            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var semanticModel = await context.Solution
                .AddProject(projectId, "GenerationSource", "GenerationSource", TestContext.GetLanguage(symbolSource))
                .AddDocument(documentId, "Source.cs", symbolSource)
                .GetDocument(documentId)
                .GetSemanticModelAsync();

            var symbol = context.GetSelectedSymbol<INamespaceOrTypeSymbol>(destSpan, semanticModel);
            var destination = context.GetDestination();
            if (destination.IsType)
            {
                var members = onlyGenerateMembers ? symbol.GetMembers().ToArray() : new[] { symbol };
                context.Result = await context.Service.AddMembersAsync(context.Solution, (INamedTypeSymbol)destination, members, codeGenerationOptions);
            }
            else
            {
                context.Result = await context.Service.AddNamespaceOrTypeAsync(context.Solution, (INamespaceSymbol)destination, symbol, codeGenerationOptions);
            }
        }

        internal static Func<SemanticModel, IParameterSymbol> Parameter(Type type, string name, bool hasDefaultValue = false, object defaultValue = null, bool isParams = false)
        {
            return s => CodeGenerationSymbolFactory.CreateParameterSymbol(
                attributes: default, RefKind.None, isParams, GetTypeSymbol(s.Compilation, type), name,
                isOptional: hasDefaultValue, hasDefaultValue: hasDefaultValue, defaultValue: defaultValue);
        }

        internal static Func<SemanticModel, IParameterSymbol> Parameter(string typeFullName, string parameterName, bool hasDefaultValue = false, object defaultValue = null, bool isParams = false, int typeArrayRank = 0)
        {
            return s => CodeGenerationSymbolFactory.CreateParameterSymbol(
                attributes: default, RefKind.None, isParams, GetTypeSymbol(s.Compilation, typeFullName, typeArrayRank), parameterName,
                isOptional: hasDefaultValue, hasDefaultValue: hasDefaultValue, defaultValue: defaultValue);
        }

        private static ITypeSymbol GetTypeSymbol(Compilation compilation, Type type)
        {
            return !type.IsArray ? GetTypeSymbol(compilation, type.FullName) : GetTypeSymbol(compilation, type.GetElementType().FullName, type.GetArrayRank());
        }

        private static ITypeSymbol GetTypeSymbol(Compilation compilation, string typeFullName, int arrayRank = 0)
        {
            return arrayRank == 0 ? (ITypeSymbol)compilation.GetTypeByMetadataName(typeFullName)
                : compilation.CreateArrayTypeSymbol(compilation.GetTypeByMetadataName(typeFullName), arrayRank);
        }

        internal static ImmutableArray<Func<SemanticModel, IParameterSymbol>> Parameters(params Func<SemanticModel, IParameterSymbol>[] p)
            => p.ToImmutableArray();

        internal static ImmutableArray<Func<SemanticModel, ISymbol>> Members(params Func<SemanticModel, ISymbol>[] m)
            => m.ToImmutableArray();

        internal static Func<SemanticModel, ITypeSymbol> CreateArrayType(Type type, int rank = 1)
        {
            return s => CodeGenerationSymbolFactory.CreateArrayTypeSymbol(GetTypeSymbol(type)(s), rank);
        }

        private static ImmutableArray<IParameterSymbol> GetParameterSymbols(ImmutableArray<Func<SemanticModel, IParameterSymbol>> parameters, TestContext context)
            => parameters.IsDefault
                ? default
                : parameters.SelectAsArray(p => p(context.SemanticModel));

        private static ImmutableArray<IMethodSymbol> GetMethodSymbols(
            Func<SemanticModel, ImmutableArray<IMethodSymbol>> explicitInterface, TestContext context)
        {
            return explicitInterface == null ? default : explicitInterface(context.SemanticModel);
        }

        private static ImmutableArray<ISymbol> GetSymbols(ImmutableArray<Func<SemanticModel, ISymbol>> members, TestContext context)
        {
            return members == null
                ? default
                : members.SelectAsArray(m => m(context.SemanticModel));
        }

        private static Func<SemanticModel, ISymbol> CreateEnumField(string name, object value)
        {
            return s => CodeGenerationSymbolFactory.CreateFieldSymbol(
                attributes: default, Accessibility.Public,
                new Editing.DeclarationModifiers(), GetTypeSymbol(typeof(int))(s), name, value != null, value);
        }

        internal static Func<SemanticModel, ISymbol> CreateField(Accessibility accessibility, Editing.DeclarationModifiers modifiers, Type type, string name)
        {
            return s => CodeGenerationSymbolFactory.CreateFieldSymbol(
                attributes: default, accessibility,
                modifiers, GetTypeSymbol(type)(s), name);
        }

        private static Func<SemanticModel, INamedTypeSymbol> GetTypeSymbol(Type type)
        {
            return GetTypeSymbol(type.FullName);
        }

        private static Func<SemanticModel, INamedTypeSymbol> GetTypeSymbol(string typeMetadataName)
        {
            return s => s?.Compilation.GetTypeByMetadataName(typeMetadataName);
        }

        internal static IEnumerable<SyntaxToken> CreateModifierTokens(Editing.DeclarationModifiers modifiers, string language)
        {
            if (language == LanguageNames.CSharp)
            {
                if (modifiers.IsAbstract)
                {
                    yield return CS.SyntaxFactory.Token(CS.SyntaxKind.AbstractKeyword);
                }

                if (modifiers.IsAsync)
                {
                    yield return CS.SyntaxFactory.Token(CS.SyntaxKind.AsyncKeyword);
                }

                if (modifiers.IsConst)
                {
                    yield return CS.SyntaxFactory.Token(CS.SyntaxKind.ConstKeyword);
                }

                if (modifiers.IsNew)
                {
                    yield return CS.SyntaxFactory.Token(CS.SyntaxKind.NewKeyword);
                }

                if (modifiers.IsOverride)
                {
                    yield return CS.SyntaxFactory.Token(CS.SyntaxKind.OverrideKeyword);
                }

                if (modifiers.IsPartial)
                {
                    yield return CS.SyntaxFactory.Token(CS.SyntaxKind.PartialKeyword);
                }

                if (modifiers.IsReadOnly)
                {
                    yield return CS.SyntaxFactory.Token(CS.SyntaxKind.ReadOnlyKeyword);
                }

                if (modifiers.IsSealed)
                {
                    yield return CS.SyntaxFactory.Token(CS.SyntaxKind.SealedKeyword);
                }

                if (modifiers.IsStatic)
                {
                    yield return CS.SyntaxFactory.Token(CS.SyntaxKind.StaticKeyword);
                }

                if (modifiers.IsUnsafe)
                {
                    yield return CS.SyntaxFactory.Token(CS.SyntaxKind.UnsafeKeyword);
                }

                if (modifiers.IsVirtual)
                {
                    yield return CS.SyntaxFactory.Token(CS.SyntaxKind.VirtualKeyword);
                }
            }
            else
            {
                if (modifiers.IsAbstract)
                {
                    yield return VB.SyntaxFactory.Token(VB.SyntaxKind.MustOverrideKeyword);
                }

                if (modifiers.IsAsync)
                {
                    yield return VB.SyntaxFactory.Token(VB.SyntaxKind.AsyncKeyword);
                }

                if (modifiers.IsConst)
                {
                    yield return VB.SyntaxFactory.Token(VB.SyntaxKind.ConstKeyword);
                }

                if (modifiers.IsNew)
                {
                    yield return VB.SyntaxFactory.Token(VB.SyntaxKind.NewKeyword);
                }

                if (modifiers.IsOverride)
                {
                    yield return VB.SyntaxFactory.Token(VB.SyntaxKind.OverridesKeyword);
                }

                if (modifiers.IsPartial)
                {
                    yield return VB.SyntaxFactory.Token(VB.SyntaxKind.PartialKeyword);
                }

                if (modifiers.IsReadOnly)
                {
                    yield return VB.SyntaxFactory.Token(VB.SyntaxKind.ReadOnlyKeyword);
                }

                if (modifiers.IsSealed)
                {
                    yield return VB.SyntaxFactory.Token(VB.SyntaxKind.NotInheritableKeyword);
                }

                if (modifiers.IsStatic)
                {
                    yield return VB.SyntaxFactory.Token(VB.SyntaxKind.StaticKeyword);
                }

                if (modifiers.IsVirtual)
                {
                    yield return VB.SyntaxFactory.Token(VB.SyntaxKind.OverridableKeyword);
                }
            }
        }

        internal class TestContext : IDisposable
        {
            private readonly string _expected;
            public readonly bool IsVisualBasic;
            public Document Document;
            public SemanticModel SemanticModel;
            public SyntaxTree SyntaxTree;
            public ICodeGenerationService Service;

            public Document Result;

            public readonly TestWorkspace Workspace;
            private readonly string _language;
            private readonly bool _ignoreResult;

            public TestContext(
                string initial,
                string expected,
                bool ignoreResult,
                string language,
                TestWorkspace workspace,
                SemanticModel semanticModel)
            {
                _expected = expected.NormalizeLineEndings();
                _language = language;
                this.IsVisualBasic = _language == LanguageNames.VisualBasic;
                _ignoreResult = ignoreResult;
                Workspace = workspace;
                this.Document = Workspace.CurrentSolution.Projects.Single().Documents.Single();
                this.SemanticModel = semanticModel;
                this.SyntaxTree = SemanticModel.SyntaxTree;
                this.Service = Document.Project.LanguageServices.GetService<ICodeGenerationService>();
            }

            public static async Task<TestContext> CreateAsync(string initial, string expected, string forceLanguage = null, bool ignoreResult = false)
            {
                var language = forceLanguage ?? GetLanguage(initial);
                var isVisualBasic = language == LanguageNames.VisualBasic;
                var workspace = CreateWorkspaceFromFile(initial.NormalizeLineEndings(), isVisualBasic, null, null);
                var semanticModel = await workspace.CurrentSolution.Projects.Single().Documents.Single().GetSemanticModelAsync();

                return new TestContext(initial, expected, ignoreResult, language, workspace, semanticModel);
            }

            public Solution Solution { get { return Workspace.CurrentSolution; } }

            public SyntaxNode GetDestinationNode()
            {
                var destSpan = Workspace.Documents.Single().SelectedSpans.Single();
                return SemanticModel.SyntaxTree.GetRoot().FindNode(destSpan, getInnermostNodeForTie: true);
            }

            public INamespaceOrTypeSymbol GetDestination()
            {
                var destSpan = Workspace.Documents.Single().SelectedSpans.Single();
                return GetSelectedSymbol<INamespaceOrTypeSymbol>(destSpan, this.SemanticModel);
            }

            public IEnumerable<ISymbol> GetAnnotatedDeclaredSymbols(string key, SemanticModel semanticModel)
            {
                var annotatedSpans = Workspace.Documents.Single().AnnotatedSpans[key];
                foreach (var span in annotatedSpans)
                {
                    yield return GetSelectedSymbol<ISymbol>(span, semanticModel);
                }
            }

            public T GetSelectedSymbol<T>(TextSpan selection, SemanticModel semanticModel)
                where T : class, ISymbol
            {
                var token = semanticModel.SyntaxTree.GetRoot().FindToken(selection.Start);

                var symbol = token.Parent.AncestorsAndSelf()
                    .Select(a => semanticModel.GetDeclaredSymbol(a))
                    .Where(s => s != null).FirstOrDefault() as T;

                return symbol;
            }

            public T GetSelectedSyntax<T>(bool fullSpanCoverage = false) where T : SyntaxNode
            {
                var destSpan = Workspace.Documents.Single().SelectedSpans.Single();
                var token = SemanticModel.SyntaxTree.GetRoot().FindToken(destSpan.Start);
                return token.Parent.AncestorsAndSelf().OfType<T>().FirstOrDefault(t => !fullSpanCoverage || t.Span.End >= destSpan.End);
            }

            public ImmutableArray<SyntaxNode> ParseStatements(string statements)
            {
                if (statements == null)
                {
                    return default;
                }

                using var listDisposer = ArrayBuilder<SyntaxNode>.GetInstance(out var list);
                var delimiter = IsVisualBasic ? "\r\n" : ";";
                var parts = statements.Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    if (IsVisualBasic)
                    {
                        list.Add(VB.SyntaxFactory.ParseExecutableStatement(p));
                    }
                    else
                    {
                        list.Add(CS.SyntaxFactory.ParseStatement(p + delimiter));
                    }
                }

                return list.ToImmutable();
            }

            public void Dispose()
            {
                try
                {
                    if (!_ignoreResult)
                    {
                        this.Document = this.Result;

                        var actual = Formatter.FormatAsync(Simplifier.ReduceAsync(this.Document, Simplifier.Annotation).Result, Formatter.Annotation).Result
                            .GetSyntaxRootAsync().Result.ToFullString();

                        Assert.Equal(_expected, actual);
                    }
                }
                finally
                {
                    Workspace.Dispose();
                }
            }

            public static string GetLanguage(string input)
            {
                return ContainsVisualBasicKeywords(input)
                    ? LanguageNames.VisualBasic : LanguageNames.CSharp;
            }

            private static bool ContainsVisualBasicKeywords(string input)
            {
                return
                    input.Contains("Module") ||
                    input.Contains("Class") ||
                    input.Contains("Structure") ||
                    input.Contains("Namespace") ||
                    input.Contains("Sub") ||
                    input.Contains("Function") ||
                    input.Contains("Dim") ||
                    input.Contains("Enum");
            }

            private static TestWorkspace CreateWorkspaceFromFile(string file, bool isVisualBasic, ParseOptions parseOptions, CompilationOptions compilationOptions)
            {
                return isVisualBasic ?
                    TestWorkspace.CreateVisualBasic(file, (VB.VisualBasicParseOptions)parseOptions, (VB.VisualBasicCompilationOptions)compilationOptions) :
                    TestWorkspace.CreateCSharp(file, (CS.CSharpParseOptions)parseOptions, (CS.CSharpCompilationOptions)compilationOptions);
            }
        }
    }
}
