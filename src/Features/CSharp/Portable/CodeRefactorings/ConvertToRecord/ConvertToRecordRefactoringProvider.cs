// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToRecord
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToRecord), Shared]
    internal class ConvertToRecordRefactoringProvider : SyntaxEditorBasedCodeRefactoringProvider
    {

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertToRecordRefactoringProvider()
        {
        }

        protected override ImmutableArray<FixAllScope> SupportedFixAllScopes => ImmutableArray.Create(FixAllScope.Document, FixAllScope.Project, FixAllScope.Solution);

        protected override Task FixAllAsync(
            Document document,
            ImmutableArray<TextSpan> fixAllSpans,
            SyntaxEditor editor,
            CodeActionOptionsProvider optionsProvider,
            string? equivalenceKey,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            var classDeclaration = await context.TryGetRelevantNodeAsync<TypeDeclarationSyntax>().ConfigureAwait(false);
            // don't need to convert 
            if (classDeclaration == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol type ||
                // if type is some enum, interface, delegate, etc we don't want to refactor
                (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct) ||
                // records can't be inherited from normal classes and normal classes cant be interited from records.
                // so if it is currently a non-record and has a base class, that base class must be a non-record (and so we can't convert)
                (type.BaseType?.SpecialType != SpecialType.System_ValueType && type.BaseType?.SpecialType != SpecialType.System_Object) ||
                // records can't be static and so if the class is static we probably shouldn't convert it
                type.IsStatic ||
                // records can't be abstract
                type.IsAbstract ||
                // make sure that there is at least one positional parameter we can introduce
                !type.GetMembers().Any(ShouldConvertProperty, type))
            {
                return;
            }

            context.RegisterRefactoring(CodeAction.Create(
                "placeholder title",
                cancellationToken => ConvertToRecordAsync(document, type, classDeclaration, context.Options, cancellationToken),
                "placeholder key"));
        }

        private static async Task<Document> ConvertToRecordAsync(
            Document document,
            INamedTypeSymbol originalType,
            TypeDeclarationSyntax originalDeclarationNode,
            CleanCodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();
            var propertiesToAddAsParams = originalType.GetMembers()
                .SelectAsArray(predicate: m => ShouldConvertProperty(m, originalType),
                    selector: m => CodeGenerationSymbolFactory.CreateParameterSymbol(m.GetMemberType()!, m.Name));

            // remove properties and any constructor with the same params
            var membersToKeep = GetModifiedMembers(originalType, propertiesToAddAsParams);

            var changedTypeDeclaration = SyntaxFactory.RecordDeclaration(
                originalDeclarationNode.GetAttributeLists(),
                originalDeclarationNode.GetModifiers(),
                SyntaxFactory.Token(SyntaxKind.RecordKeyword),
                originalDeclarationNode.Identifier,
                originalDeclarationNode.TypeParameterList,
                SyntaxFactory.ParameterList(propertiesToAddAsParams),
                // TODO: inheritance
                default,
                originalDeclarationNode.ConstraintClauses,
                originalDeclarationNode.Members)

            var changedRecordType = CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                originalType.GetAttributes(),
                originalType.DeclaredAccessibility,
                DeclarationModifiers.From(originalType),
                isRecord: true,
                originalType.TypeKind,
                originalType.Name,
                typeParameters: originalType.TypeParameters,
                interfaces: originalType.Interfaces,
                members: membersToKeep,
                nullableAnnotation: originalType.NullableAnnotation,
                containingAssembly: originalType.ContainingAssembly);

            var context = new CodeGenerationContext(reuseSyntax: true);

            var options = await document.GetCodeGenerationOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            var info = options.GetInfo(context, document.Project);

            var destination = CodeGenerationDestination.Unspecified;
            if (originalType.ContainingType != null)
            {
                destination = originalType.ContainingType.TypeKind == TypeKind.Class
                    ? CodeGenerationDestination.ClassType
                    : CodeGenerationDestination.StructType;
            }
            else if (originalType.ContainingNamespace != null)
            {
                destination = CodeGenerationDestination.Namespace;
            }
            else
            {
                destination = CodeGenerationDestination.CompilationUnit;
            }

            var recordSyntaxWithoutParameters = codeGenerationService.CreateNamedTypeDeclaration(
                changedRecordType, destination, info, cancellationToken)
                as RecordDeclarationSyntax;

            Contract.ThrowIfNull(recordSyntaxWithoutParameters);

            var recordSyntax = codeGenerationService.AddParameters(recordSyntaxWithoutParameters, propertiesToAddAsParams, info, cancellationToken);
            var changedDocument = await document.ReplaceNodeAsync(originalDeclarationNode, recordSyntax, cancellationToken).ConfigureAwait(false);
            return changedDocument;
        }

        private static bool ShouldConvertProperty(MemberDeclarationSyntax member, INamedTypeSymbol containingType)
        {
            if (member is not PropertyDeclarationSyntax property)
            {
                return false;
            }

            var propModifiers = property.Modifiers.SelectAsArray(m => m.Kind());
            if (propAccessibility.Contains(SyntaxKind.PrivateKeyword) ||
                propAccessibility.Contains(SyntaxKind.ProtectedKeyword))
            {
                return false;
            }

            var typeAccessibility = containingType.DeclaredAccessibility;
            if (typeAccessibility == Accessibility.Public && propAccessibility.Contains(SyntaxKind.InternalKeyword))
            {
                return false;
            }

            // for class records and readonly struct records, properties should be get; init; only
            // for non-readonly structs, they should be get; set;
            // neither should have bodies (as it indicates more complex functionality)
            var correctAccessors = default(SyntaxList<AccessorDeclarationSyntax>);
            if (containingType.TypeKind == TypeKind.Class || containingType.IsReadOnly)
            {
                correctAccessors = new SyntaxList(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration), SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration));
            }
            else
            {
                correctAccessors = new SyntaxList(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration), SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration));
            }
            
            var accessors = property.AccessorList.Accessors;
            if (!accessors.Equals(correctAccesors))
            {
                return false;
            }

            return true;
        }

        private static ImmutableArray<SyntaxNode> GetStatements(IMethodSymbol method)
        {
            var declaringReference = method.DeclaringSyntaxReferences.FirstOrDefault();
            var root = await declaringReference.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var declarationNode = root.FindNode(declaringReference.Span) as BaseMethodDeclarationSyntax;
            // TODO: probably need more checks to make sure this works for all cases
            // for now just assert not null
            Contract.ThrowIfNull(declarationNode);

            // 3 major cases:
            // 1. No body (abstract method, default property method, etc) -> empty list
            // 2. Expression Body -> arrow clause syntax is the only element
            // 3. Block Body -> all statements
            var statements = ImmutableArray<SyntaxNode>.Empty;
            var expressionBody = declarationNode.GetExpressionBody();
            if (expressionBody != null)
            {
                statements = ImmutableArray.Create<SyntaxNode>(expressionBody);
            }
            else
            {
                var blockBody = declarationNode.Body;
                if (blockBody != null)
                {
                    statements = ImmutableArray.Create<SyntaxNode>(blockBody.Statements);
                }
            }

            return statements;
        }

        private static ImmutableArray<ISymbol> GetModifiedMembers(
            INamedTypeSymbol originalType,
            ImmutableArray<IParameterSymbol> positionalParams,
            CancellationToken cancellationToken)
        {
            var oldMembers = originalType.GetMembers();
            // for operators == and !=, we want to delete both if there niether mention an Equals method
            var shouldDeleteEqualityOperators = false;
            
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var modifiedMembers);
            for (var member in oldMembers)
            {
                if (!member.IsNonImplicitAndFromSource() ||
                    ShouldConvertProperty(member))
                {
                    // remove properties that we turn to positional parameters
                    continue;
                }

                if (member is IMethodSymbol method)
                {
                    if (method.IsConstructor())
                    {
                        // remove the constructor with the same parameter types in the same order as the positional parameters
                        if (method.Parameters.SelectAsArray(p => p.Type).Equals(positionalParams.SelectAsArray(p => p.Type)))
                        {
                            continue;
                        }

                        // TODO: Can potentially refactor statements which initialize properties with a simple exression (not using local variables) to be moved into the this args
                        var thisArgs = positionalParams.SelectAsArray(_ => SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression))

                        var modifiedConstructor = CodeGenerationSymbolFactory.CreateConstructorSymbol(
                            method.GetAttributes(),
                            method.DeclaredAccessibility,
                            method.GetSymbolModifiers(),
                            method.Name,
                            method.Parameters,
                            statements: statements,
                            thisConstructorArguments: thisArgs);

                        modifiedMembers.Add(modifiedConstructor);
                        continue;
                    }

                    var op = method.GetPredefinedOperator();
                    if (op == PredefinedOperator.Equality || op == PredefinedOperator.Inequality)
                    {
                        // TODO: search for an equals method call
                        // right now just assume we didn't find one so we keep the original method
                        modifiedMembers.Add(method);
                        continue;
                    }

                    if (method.Name("PrintMembers") &&
                        method.ReturnType == SpecialType.System_Boolean &&
                        method.Parameters.IsSingle() &&
                        method.Parameters.First().Type.Name.Equals("StringBuilder"))
                    {
                        // make sure it has the correct accessibility, otherwise change
                        if (originalType.TypeKind == TypeKind.Class)
                        {
                            if (method.DeclaredAccessibility == Accessibility.Protected)
                            {
                                modifiedMembers.Add(method);
                            }
                            else
                            {
                                modifiedMembers.Add(CodeGenerationSymbolFactory.CreateMethodSymbol(method, accessibility: Accessibility.Protected));
                            }
                            continue;
                        }
                    }
                }
            }
        }
    }
}
