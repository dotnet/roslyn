// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToRecord
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToRecord), Shared]
    internal class CSharpConvertToRecordRefactoringProvider : SyntaxEditorBasedCodeRefactoringProvider
    {

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConvertToRecordRefactoringProvider()
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
                // TODO: when adding other options it might make sense to convert a postional record to a non-positional one and vice versa
                // but for now we don't convert records
                type.IsRecord ||
                // records can't be static and so if the class is static we probably shouldn't convert it
                type.IsStatic ||
                // make sure that there is at least one positional parameter we can introduce
                !classDeclaration.Members.Any(m => ShouldConvertProperty(m, type)))
            {
                return;
            }

            context.RegisterRefactoring(CodeAction.Create(
                "placeholder title",
                cancellationToken => ConvertToPositionalRecordAsync(document, type, classDeclaration, cancellationToken),
                "placeholder key"));
        }

        private static async Task<Document> ConvertToPositionalRecordAsync(
            Document document,
            INamedTypeSymbol originalType,
            TypeDeclarationSyntax originalDeclarationNode,
            CancellationToken cancellationToken)
        {
            var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();
            // TODO: grab documentation comments (and other comments) to move to params
            var propertiesToAddAsParams = originalDeclarationNode.Members.AsImmutable().SelectAsArray(
                predicate: m => ShouldConvertProperty(m, originalType),
                selector: m =>
                {
                    var p = (PropertyDeclarationSyntax)m;
                    return SyntaxFactory.Parameter(
                        new SyntaxList<AttributeListSyntax>(p.AttributeLists.SelectAsArray(attributeList =>
                            attributeList.Target == null
                            ? attributeList.WithTarget(SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.PropertyKeyword)))
                            : attributeList)),
                        modifiers: default,
                        p.Type,
                        p.Identifier,
                        @default: null);
                });

            // remove converted properties and reformat other methods
            var membersToKeep = GetModifiedMembersForPositionalRecord(originalType, originalDeclarationNode, propertiesToAddAsParams);

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
                baseList: null,
                originalDeclarationNode.ConstraintClauses,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                SyntaxFactory.List(membersToKeep),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                semicolonToken: default);

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

        private static ImmutableArray<MemberDeclarationSyntax> GetModifiedMembersForPositionalRecord(
            INamedTypeSymbol type,
            TypeDeclarationSyntax classDeclaration,
            ImmutableArray<ParameterSyntax> positionalParams)
        {
            var oldMembers = classDeclaration.Members;
            // create capture variables for the equality operators, since we want to check both of them at the same time
            // to make sure they can be deleted
            OperatorDeclarationSyntax? equals = null;
            OperatorDeclarationSyntax? notEquals = null;

            using var _ = ArrayBuilder<MemberDeclarationSyntax>.GetInstance(out var modifiedMembers);
            foreach (var member in oldMembers)
            {
                if (ShouldConvertProperty(member, type))
                {
                    // remove properties that we turn to positional parameters
                    continue;
                }

                switch (member)
                {
                    case ConstructorDeclarationSyntax constructor:
                        {
                            // remove the constructor with the same parameter types in the same order as the positional parameters
                            // TODO: search to see if there are side effects and consider not deleting
                            /* Disabling for now, can re-enable pending design meeting.
                            var constructorParamTypes = constructor.ParameterList.Parameters.SelectAsArray(p => p.Type!.ToString());
                            var positionalParamTypes = positionalParams.SelectAsArray(p => p.Type!.ToString());
                            if (constructorParamTypes.SequenceEqual(positionalParamTypes))
                            {
                                continue;
                            }*/

                            // TODO: Can potentially refactor statements which initialize properties with a simple expression (not using local variables) to be moved into the this args
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

                    case OperatorDeclarationSyntax op:
                        {
                            var opKind = op.OperatorToken.Kind();
                            if (opKind == SyntaxKind.EqualsEqualsToken)
                            {
                                equals = op;
                            }
                            else if (opKind == SyntaxKind.ExclamationEqualsToken)
                            {
                                notEquals = op;
                            }

                            if (equals != null && notEquals != null)
                            {
                                if (!ContainsEqualsInvocation(equals) && !ContainsEqualsInvocation(notEquals))
                                {
                                    // add both back in
                                    // TODO: maintain declared order so the diff doesn't look weird
                                    modifiedMembers.Add(equals);
                                    modifiedMembers.Add(notEquals);
                                }
                            }

                            break;
                        }

                    case MethodDeclarationSyntax method:
                        {
                            /* TODO: Enable this along with other signature change after design review, for now leave even if error
                             * // TODO: Ensure the single param is an StringBuilder
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
                            }*/

                            break;
                        }
                }

                // any other members we didn't change or modify we just keep
                modifiedMembers.Add(member);
            }

            return modifiedMembers.ToImmutable();
        }

        /// <summary>
        /// Returns true if the method contents contain a call to the Equals method
        /// indicating that the contents are similar to what would be generated
        /// </summary>
        private static bool ContainsEqualsInvocation(OperatorDeclarationSyntax expression)
        {
            // TODO: search for a call to an equals method or comparison of the properties we would move
            // reserved for an extension of the feature
            // for now we return false, indicating we aren't confident enough to delete the method
            return false;
        }
    }
}
