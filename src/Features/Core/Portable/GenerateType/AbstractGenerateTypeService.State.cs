// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateType
{
    internal abstract partial class AbstractGenerateTypeService<TService, TSimpleNameSyntax, TObjectCreationExpressionSyntax, TExpressionSyntax, TTypeDeclarationSyntax, TArgumentSyntax>
    {
        protected class State
        {
            public string Name { get; private set; }
            public bool NameIsVerbatim { get; private set; }

            // The name node that we're on.  Will be used to the name the type if it's
            // generated.
            public TSimpleNameSyntax SimpleName { get; private set; }

            // The entire expression containing the name, not including the creation.  i.e. "X.Foo"
            // in "new X.Foo()".
            public TExpressionSyntax NameOrMemberAccessExpression { get; private set; }

            // The object creation node if we have one.  i.e. if we're on the 'Foo' in "new X.Foo()".
            public TObjectCreationExpressionSyntax ObjectCreationExpressionOpt { get; private set; }

            // One of these will be non null.  It's also possible for both to be non null. For
            // example, if you have "class C { Foo f; }", then "Foo" can be generated inside C or
            // inside the global namespace.  The namespace can be null or the type can be null if the
            // user has something like "ExistingType.NewType" or "ExistingNamespace.NewType".  In
            // that case they're being explicit about what they want to generate into.
            public INamedTypeSymbol TypeToGenerateInOpt { get; private set; }
            public string NamespaceToGenerateInOpt { get; private set; }

            // If we can infer a base type or interface for this type. 
            // 
            // i.e.: "IList<int> foo = new MyList();"
            public INamedTypeSymbol BaseTypeOrInterfaceOpt { get; private set; }
            public bool IsInterface { get; private set; }
            public bool IsStruct { get; private set; }
            public bool IsAttribute { get; private set; }
            public bool IsException { get; private set; }
            public bool IsMembersWithModule { get; private set; }
            public bool IsTypeGeneratedIntoNamespaceFromMemberAccess { get; private set; }
            public bool IsSimpleNameGeneric { get; private set; }
            public bool IsPublicAccessibilityForTypeGeneration { get; private set; }
            public bool IsInterfaceOrEnumNotAllowedInTypeContext { get; private set; }
            public IMethodSymbol DelegateMethodSymbol { get; private set; }
            public bool IsDelegateAllowed { get; private set; }
            public bool IsEnumNotAllowed { get; private set; }
            public Compilation Compilation { get; }
            public bool IsDelegateOnly { get; private set; }
            public bool IsClassInterfaceTypes { get; private set; }
            public List<TSimpleNameSyntax> PropertiesToGenerate { get; private set; }

            private State(Compilation compilation)
            {
                Compilation = compilation;
            }

            public static async Task<State> GenerateAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode node,
                CancellationToken cancellationToken)
            {
                var state = new State(document.SemanticModel.Compilation);
                if (!await state.TryInitializeAsync(service, document, node, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private async Task<bool> TryInitializeAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode node,
                CancellationToken cancellationToken)
            {
                if (!(node is TSimpleNameSyntax))
                {
                    return false;
                }

                this.SimpleName = (TSimpleNameSyntax)node;
                string name;
                int arity;
                var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                syntaxFacts.GetNameAndArityOfSimpleName(this.SimpleName, out name, out arity);

                this.Name = name;
                this.NameIsVerbatim = syntaxFacts.IsVerbatimIdentifier(this.SimpleName.GetFirstToken());
                if (string.IsNullOrWhiteSpace(this.Name))
                {
                    return false;
                }

                // We only support simple names or dotted names.  i.e. "(some + expr).Foo" is not a
                // valid place to generate a type for Foo.
                GenerateTypeServiceStateOptions generateTypeServiceStateOptions;
                if (!service.TryInitializeState(document, this.SimpleName, cancellationToken, out generateTypeServiceStateOptions))
                {
                    return false;
                }

                if (char.IsLower(name[0]) && !document.SemanticModel.Compilation.IsCaseSensitive)
                {
                    // It's near universal in .Net that types start with a capital letter.  As such,
                    // if this name starts with a lowercase letter, don't even bother to offer 
                    // "generate type".  The user most likely wants to run 'Add Import' (which will
                    // then fix up a case where they typed an existing type name in lowercase, 
                    // intending the fix to case correct it).
                    return false;
                }

                this.NameOrMemberAccessExpression = generateTypeServiceStateOptions.NameOrMemberAccessExpression;
                this.ObjectCreationExpressionOpt = generateTypeServiceStateOptions.ObjectCreationExpressionOpt;

                var semanticModel = document.SemanticModel;
                var info = semanticModel.GetSymbolInfo(this.SimpleName, cancellationToken);
                if (info.Symbol != null)
                {
                    // This bound, so no need to generate anything.
                    return false;
                }

                var semanticFacts = document.Project.LanguageServices.GetService<ISemanticFactsService>();
                if (!semanticFacts.IsTypeContext(semanticModel, this.NameOrMemberAccessExpression.SpanStart, cancellationToken) &&
                    !semanticFacts.IsExpressionContext(semanticModel, this.NameOrMemberAccessExpression.SpanStart, cancellationToken) &&
                    !semanticFacts.IsStatementContext(semanticModel, this.NameOrMemberAccessExpression.SpanStart, cancellationToken) &&
                    !semanticFacts.IsNameOfContext(semanticModel, this.NameOrMemberAccessExpression.SpanStart, cancellationToken) &&
                    !semanticFacts.IsNamespaceContext(semanticModel, this.NameOrMemberAccessExpression.SpanStart, cancellationToken))
                {
                    return false;
                }

                // If this isn't something that can be created, then don't bother offering to create
                // it.
                if (info.CandidateReason == CandidateReason.NotCreatable)
                {
                    return false;
                }

                if (info.CandidateReason == CandidateReason.Inaccessible ||
                    info.CandidateReason == CandidateReason.NotReferencable ||
                    info.CandidateReason == CandidateReason.OverloadResolutionFailure)
                {
                    // We bound to something inaccessible, or overload resolution on a 
                    // constructor call failed.  Don't want to offer GenerateType here.
                    return false;
                }

                if (this.ObjectCreationExpressionOpt != null)
                {
                    // If we're new'ing up something illegal, then don't offer generate type.
                    var typeInfo = semanticModel.GetTypeInfo(this.ObjectCreationExpressionOpt, cancellationToken);
                    if (typeInfo.Type.IsModuleType())
                    {
                        return false;
                    }
                }

                await DetermineNamespaceOrTypeToGenerateInAsync(service, document, cancellationToken).ConfigureAwait(false);

                // Now, try to infer a possible base type for this new class/interface.
                this.InferBaseType(service, document, cancellationToken);
                this.IsInterface = GenerateInterface(service, cancellationToken);
                this.IsStruct = GenerateStruct(service, semanticModel, cancellationToken);
                this.IsAttribute = this.BaseTypeOrInterfaceOpt != null && this.BaseTypeOrInterfaceOpt.Equals(semanticModel.Compilation.AttributeType());
                this.IsException = this.BaseTypeOrInterfaceOpt != null && this.BaseTypeOrInterfaceOpt.Equals(semanticModel.Compilation.ExceptionType());
                this.IsMembersWithModule = generateTypeServiceStateOptions.IsMembersWithModule;
                this.IsTypeGeneratedIntoNamespaceFromMemberAccess = generateTypeServiceStateOptions.IsTypeGeneratedIntoNamespaceFromMemberAccess;
                this.IsInterfaceOrEnumNotAllowedInTypeContext = generateTypeServiceStateOptions.IsInterfaceOrEnumNotAllowedInTypeContext;
                this.IsDelegateAllowed = generateTypeServiceStateOptions.IsDelegateAllowed;
                this.IsDelegateOnly = generateTypeServiceStateOptions.IsDelegateOnly;
                this.IsEnumNotAllowed = generateTypeServiceStateOptions.IsEnumNotAllowed;
                this.DelegateMethodSymbol = generateTypeServiceStateOptions.DelegateCreationMethodSymbol;
                this.IsClassInterfaceTypes = generateTypeServiceStateOptions.IsClassInterfaceTypes;
                this.IsSimpleNameGeneric = service.IsGenericName(this.SimpleName);
                this.PropertiesToGenerate = generateTypeServiceStateOptions.PropertiesToGenerate;

                if (this.IsAttribute && this.TypeToGenerateInOpt.GetAllTypeParameters().Any())
                {
                    this.TypeToGenerateInOpt = null;
                }

                return this.TypeToGenerateInOpt != null || this.NamespaceToGenerateInOpt != null;
            }

            private void InferBaseType(
                TService service,
                SemanticDocument document,
                CancellationToken cancellationToken)
            {
                // See if we can find a possible base type for the type being generated.
                // NOTE(cyrusn): I currently limit this to when we have an object creation node.
                // That's because that's when we would have an expression that could be converted to
                // something else.  i.e. if the user writes "IList<int> list = new Foo()" then we can
                // infer a base interface for 'Foo'.  However, if they write "IList<int> list = Foo"
                // then we don't really want to infer a base type for 'Foo'.

                // However, there are a few other cases were we can infer a base type.
                var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                if (service.IsInCatchDeclaration(this.NameOrMemberAccessExpression))
                {
                    this.BaseTypeOrInterfaceOpt = document.SemanticModel.Compilation.ExceptionType();
                }
                else if (syntaxFacts.IsAttributeName(this.NameOrMemberAccessExpression))
                {
                    this.BaseTypeOrInterfaceOpt = document.SemanticModel.Compilation.AttributeType();
                }
                else if (
                    service.IsArrayElementType(this.NameOrMemberAccessExpression) ||
                    service.IsInVariableTypeContext(this.NameOrMemberAccessExpression) ||
                    this.ObjectCreationExpressionOpt != null)
                {
                    var expr = this.ObjectCreationExpressionOpt ?? this.NameOrMemberAccessExpression;
                    var typeInference = document.Project.LanguageServices.GetService<ITypeInferenceService>();
                    var baseType = typeInference.InferType(document.SemanticModel, expr, objectAsDefault: true, cancellationToken: cancellationToken) as INamedTypeSymbol;
                    SetBaseType(baseType);
                }
            }

            private void SetBaseType(INamedTypeSymbol baseType)
            {
                if (baseType == null)
                {
                    return;
                }

                // A base type need to be non class or interface type.  Also, being 'object' is
                // redundant as the base type.  
                if (baseType.IsSealed || baseType.IsStatic || baseType.SpecialType == SpecialType.System_Object)
                {
                    return;
                }

                if (baseType.TypeKind != TypeKind.Class && baseType.TypeKind != TypeKind.Interface)
                {
                    return;
                }

                this.BaseTypeOrInterfaceOpt = baseType;
            }

            private bool GenerateStruct(TService service, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                return service.IsInValueTypeConstraintContext(semanticModel, this.NameOrMemberAccessExpression, cancellationToken);
            }

            private bool GenerateInterface(
                TService service,
                CancellationToken cancellationToken)
            {
                if (!this.IsAttribute &&
                    !this.IsException &&
                    this.Name.LooksLikeInterfaceName() &&
                    this.ObjectCreationExpressionOpt == null &&
                    (this.BaseTypeOrInterfaceOpt == null || this.BaseTypeOrInterfaceOpt.TypeKind == TypeKind.Interface))
                {
                    return true;
                }

                return service.IsInInterfaceList(this.NameOrMemberAccessExpression);
            }

            private async Task DetermineNamespaceOrTypeToGenerateInAsync(
                TService service,
                SemanticDocument document,
                CancellationToken cancellationToken)
            {
                DetermineNamespaceOrTypeToGenerateInWorker(service, document.SemanticModel, cancellationToken);

                // Can only generate into a type if it's a class and it's from source.
                if (this.TypeToGenerateInOpt != null)
                {
                    if (this.TypeToGenerateInOpt.TypeKind != TypeKind.Class &&
                        this.TypeToGenerateInOpt.TypeKind != TypeKind.Module)
                    {
                        this.TypeToGenerateInOpt = null;
                    }
                    else
                    {
                        var symbol = await SymbolFinder.FindSourceDefinitionAsync(this.TypeToGenerateInOpt, document.Project.Solution, cancellationToken).ConfigureAwait(false);
                        if (symbol == null ||
                            !symbol.IsKind(SymbolKind.NamedType) ||
                            !symbol.Locations.Any(loc => loc.IsInSource))
                        {
                            this.TypeToGenerateInOpt = null;
                            return;
                        }

                        var sourceTreeToBeGeneratedIn = symbol.Locations.First(loc => loc.IsInSource).SourceTree;
                        var documentToBeGeneratedIn = document.Project.Solution.GetDocument(sourceTreeToBeGeneratedIn);

                        if (documentToBeGeneratedIn == null)
                        {
                            this.TypeToGenerateInOpt = null;
                            return;
                        }

                        // If the 2 documents are in different project then we must have Public Accessibility.
                        // If we are generating in a website project, we also want to type to be public so the 
                        // designer files can access the type.
                        if (documentToBeGeneratedIn.Project != document.Project ||
                            service.GeneratedTypesMustBePublic(documentToBeGeneratedIn.Project))
                        {
                            this.IsPublicAccessibilityForTypeGeneration = true;
                        }

                        this.TypeToGenerateInOpt = (INamedTypeSymbol)symbol;
                    }
                }

                if (this.TypeToGenerateInOpt != null)
                {
                    if (!CodeGenerator.CanAdd(document.Project.Solution, this.TypeToGenerateInOpt, cancellationToken))
                    {
                        this.TypeToGenerateInOpt = null;
                    }
                }
            }

            private bool DetermineNamespaceOrTypeToGenerateInWorker(
                TService service,
                SemanticModel semanticModel,
                CancellationToken cancellationToken)
            {
                // If we're on the right of a dot, see if we can figure out what's on the left.  If
                // it doesn't bind to a type or a namespace, then we can't proceed.
                if (this.SimpleName != this.NameOrMemberAccessExpression)
                {
                    return DetermineNamespaceOrTypeToGenerateIn(
                        service, semanticModel,
                        service.GetLeftSideOfDot(this.SimpleName), cancellationToken);
                }
                else
                {
                    // The name is standing alone.  We can either generate the type into our
                    // containing type, or into our containing namespace.
                    //
                    // TODO(cyrusn): We need to make this logic work if the type is in the
                    // base/interface list of a type.
                    var format = SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);
                    this.TypeToGenerateInOpt = service.DetermineTypeToGenerateIn(semanticModel, this.SimpleName, cancellationToken);
                    if (this.TypeToGenerateInOpt != null)
                    {
                        this.NamespaceToGenerateInOpt = this.TypeToGenerateInOpt.ContainingNamespace.ToDisplayString(format);
                    }
                    else
                    {
                        var namespaceSymbol = semanticModel.GetEnclosingNamespace(this.SimpleName.SpanStart, cancellationToken);
                        if (namespaceSymbol != null)
                        {
                            this.NamespaceToGenerateInOpt = namespaceSymbol.ToDisplayString(format);
                        }
                    }
                }

                return true;
            }

            private bool DetermineNamespaceOrTypeToGenerateIn(
                TService service,
                SemanticModel semanticModel,
                TExpressionSyntax leftSide,
                CancellationToken cancellationToken)
            {
                var leftSideInfo = semanticModel.GetSymbolInfo(leftSide, cancellationToken);

                if (leftSideInfo.Symbol != null)
                {
                    var symbol = leftSideInfo.Symbol;

                    if (symbol is INamespaceSymbol)
                    {
                        this.NamespaceToGenerateInOpt = symbol.ToNameDisplayString();
                        return true;
                    }
                    else if (symbol is INamedTypeSymbol)
                    {
                        // TODO: Code coverage
                        this.TypeToGenerateInOpt = (INamedTypeSymbol)symbol.OriginalDefinition;
                        return true;
                    }

                    // We bound to something other than a namespace or named type.  Can't generate a
                    // type inside this.
                    return false;
                }
                else
                {
                    // If it's a dotted name, then perhaps it's a namespace.  i.e. the user wrote
                    // "new Foo.Bar.Baz()".  In this case we want to generate a namespace for
                    // "Foo.Bar".
                    IList<string> nameParts;
                    if (service.TryGetNameParts(leftSide, out nameParts))
                    {
                        this.NamespaceToGenerateInOpt = string.Join(".", nameParts);
                        return true;
                    }
                }

                return false;
            }
        }

        protected class GenerateTypeServiceStateOptions
        {
            public TExpressionSyntax NameOrMemberAccessExpression { get; set; }
            public TObjectCreationExpressionSyntax ObjectCreationExpressionOpt { get; set; }
            public IMethodSymbol DelegateCreationMethodSymbol { get; set; }
            public List<TSimpleNameSyntax> PropertiesToGenerate { get; }
            public bool IsMembersWithModule { get; set; }
            public bool IsTypeGeneratedIntoNamespaceFromMemberAccess { get; set; }
            public bool IsInterfaceOrEnumNotAllowedInTypeContext { get; set; }
            public bool IsDelegateAllowed { get; set; }
            public bool IsEnumNotAllowed { get; set; }
            public bool IsDelegateOnly { get; internal set; }
            public bool IsClassInterfaceTypes { get; internal set; }

            public GenerateTypeServiceStateOptions()
            {
                NameOrMemberAccessExpression = null;
                ObjectCreationExpressionOpt = null;
                DelegateCreationMethodSymbol = null;
                IsMembersWithModule = false;
                PropertiesToGenerate = new List<TSimpleNameSyntax>();
                IsTypeGeneratedIntoNamespaceFromMemberAccess = false;
                IsInterfaceOrEnumNotAllowedInTypeContext = false;
                IsDelegateAllowed = true;
                IsEnumNotAllowed = false;
                IsDelegateOnly = false;
            }
        }
    }
}
