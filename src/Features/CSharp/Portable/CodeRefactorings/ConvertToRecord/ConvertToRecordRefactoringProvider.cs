sues// Licensed to the .NET Foundation under one or more agreements.
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

            if (classDeclaration == null)
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol type ||
                // if type is some enum, interface, delegate, etc we don't want to refactor
                (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct) ||
                // No need to convert a record to a record
                type.IsRecord ||
                // records can't be inherited from normal classes and normal classes cant be interited from records.
                // so if it is currently a non-record and has a base class, that base class must be a non-record (and so we can't convert)
                (type.BaseType?.SpecialType != SpecialType.System_ValueType && type.BaseType?.SpecialType != SpecialType.System_Object) ||
                // records can't be static and so if the class is static we probably shouldn't convert it
                type.IsStatic ||
                // records can't be abstract
                type.IsAbstract ||
                // make sure that there is at least one positional parameter we can introduce
                !classDeclaration.Members.Any(m => ShouldConvertProperty(m, type)))
            {
                return;
            }

            context.RegisterRefactoring(CodeAction.Create(
                "placeholder title",
                cancellationToken => ConvertToRecordAsync(document, type, classDeclaration, cancellationToken),
                "placeholder key"));
        }

        private static async Task<Document> ConvertToRecordAsync(
            Document document,
            INamedTypeSymbol originalType,
            TypeDeclarationSyntax originalDeclarationNode,
            CancellationToken cancellationToken)
        {
            var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();
            var propertiesToAddAsParams = originalDeclarationNode.Members.AsImmutable().SelectAsArray(
                predicate: m => ShouldConvertProperty(m, originalType),
                selector: m =>
                {
                    var p = (PropertyDeclarationSyntax)m;
                    return SyntaxFactory.Parameter(p.AttributeLists, modifiers: default, p.Type, p.Identifier, @default: null);
                });

            // remove properties and any constructor with the same params
            var membersToKeep = GetModifiedMembers(originalType, originalDeclarationNode, propertiesToAddAsParams);

            var changedTypeDeclaration = SyntaxFactory.RecordDeclaration(
                originalType.TypeKind == TypeKind.Class
                    ? SyntaxKind.RecordDeclaration
                    : SyntaxKind.RecordStructDeclaration,
                originalDeclarationNode.GetAttributeLists(),
                originalDeclarationNode.GetModifiers(),
                SyntaxFactory.Token(SyntaxKind.RecordKeyword),
                originalType.TypeKind == TypeKind.Class
                    ? default
                    : SyntaxFactory.Token(SyntaxKind.StructKeyword),
                originalDeclarationNode.Identifier,
                originalDeclarationNode.TypeParameterList,
                SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(propertiesToAddAsParams)),
                // TODO: inheritance
                null,
                originalDeclarationNode.ConstraintClauses,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                SyntaxFactory.List(membersToKeep),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                default);

            var changedDocument = await document.ReplaceNodeAsync(originalDeclarationNode, changedTypeDeclaration, cancellationToken).ConfigureAwait(false);
            return changedDocument;
        }

        private static bool ShouldConvertProperty(MemberDeclarationSyntax member, INamedTypeSymbol containingType)
        {
            if (member is not PropertyDeclarationSyntax property)
            {
                return false;
            }

            if (property.Initializer != null || property.ExpressionBody != null)
            {
                return false;
            }

            var propAccessibility = CSharpAccessibilityFacts.Instance.GetAccessibility(member);

            // more restrictive than internal (protected, private, private protected, or unspecified (private by default))
            if (propAccessibility < Accessibility.Internal)
            {
                return false;
            }

            // no accessors declared
            if (property.AccessorList == null)
            {
                return false;
            }

            // for class records and readonly struct records, properties should be get; init; only
            // for non-readonly structs, they should be get; set;
            // neither should have bodies (as it indicates more complex functionality)
            var accessors = property.AccessorList.Accessors;

            if (accessors.Any(a => a.Body != null || a.ExpressionBody != null) &&
                !accessors.Any(a => a.Kind() == SyntaxKind.GetAccessorDeclaration))
            {
                return false;
            }

            if (containingType.TypeKind == TypeKind.Class || containingType.IsReadOnly)
            {
                if (!accessors.Any(a => a.Kind() == SyntaxKind.InitAccessorDeclaration))
                {
                    return false;
                }
            }
            else
            {
                if (!accessors.Any(a => a.Kind() == SyntaxKind.SetAccessorDeclaration))
                {
                    return false;
                }
            }

            return true;
        }

        private static ImmutableArray<MemberDeclarationSyntax> GetModifiedMembers(
            INamedTypeSymbol type,
            TypeDeclarationSyntax classDeclaration,
            ImmutableArray<ParameterSyntax> positionalParams)
        {
            var oldMembers = classDeclaration.Members;
            // for operators == and !=, we want to delete both if there niether mention an Equals method
            var shouldDeleteEqualityOperators = false;

            using var _ = ArrayBuilder<MemberDeclarationSyntax>.GetInstance(out var modifiedMembers);
            foreach (var member in oldMembers)
            {
                if (ShouldConvertProperty(member, type))
                {
                    // remove properties that we turn to positional parameters
                    continue;
                }

                if (member is ConstructorDeclarationSyntax constructor)
                {
                    // remove the constructor with the same parameter types in the same order as the positional parameters
                    var constructorParamTypes = constructor.ParameterList.Parameters.SelectAsArray(p => p.Type!.ToString());
                    var positionalParamTypes = positionalParams.SelectAsArray(p => p.Type!.ToString());
                    if (constructorParamTypes.SequenceEqual(positionalParamTypes))
                    {
                        continue;
                    }

                    // TODO: Can potentially refactor statements which initialize properties with a simple exression (not using local variables) to be moved into the this args
                    var thisArgs = SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            Enumerable.Repeat(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)),
                                positionalParams.Length)));

                    // TODO: look to see if there is already initializer (base or this), part of inheritance as well

                    var modifiedConstructor = constructor.WithInitializer(SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer, thisArgs));

                    modifiedMembers.Add(modifiedConstructor);
                    continue;
                }

                if (member is OperatorDeclarationSyntax op)
                {
                    var opKind = op.OperatorToken.Kind();
                    if (opKind == SyntaxKind.EqualsEqualsToken || opKind == SyntaxKind.ExclamationEqualsToken)
                    {
                        // TODO: search for an equals method call
                        // right now just assume we didn't find one so we keep the original method

                        modifiedMembers.Add(op);
                        continue;
                    }
                }

                if (member is MethodDeclarationSyntax method)
                {
                    // TODO: Ensure the single param is an StringBuilder
                    if (method.Identifier.Text == "PrintMembers" &&
                        method.ReturnType == SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)) &&
                        method.ParameterList.Parameters.IsSingle())
                    {
                        if (type.TypeKind == TypeKind.Class && !type.IsSealed)
                        {
                            // ensure virtual and protected modifiers
                            modifiedMembers.Add(method.WithModifiers(SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                                SyntaxFactory.Token(SyntaxKind.VirtualKeyword))));
                        }
                        else
                        {
                            // ensure private member
                            modifiedMembers.Add(method.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))));
                        }

                        continue;
                    }
                }
                // any other members we didn't change or modify we just keep
                modifiedMembers.Add(member);
            }

            return modifiedMembers.ToImmutable();
        }
    }
}
