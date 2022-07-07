// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
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
                // since we can't have any classes inherit this one, if a class is abstract it probably is inherited
                // by a non-record type, so we should fail
                type.IsAbstract)
            {
                return;
            }

            var changedDoc = await TryConvertToRecordAsync(document, type, classDeclaration, context.Options, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactoring(CodeAction.Create(
                "placeholder title",
                cancellationToken => TryConvertToRecordAsync(document, type, classDeclaration, context.Options, cancellationToken),
                "placeholder key"));
        }

        private static async Task<Document> TryConvertToRecordAsync(
            Document document,
            INamedTypeSymbol originalType,
            SyntaxNode originalDeclarationNode,
            CleanCodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();
            var propertiesToAddAsParams = originalType.GetMembers()
                .SelectAsArray(predicate: m => ShouldConvertProperty(m, originalType),
                    selector: m => CodeGenerationSymbolFactory.CreateParameterSymbol(m.GetMemberType()!, m.Name));

            // remove properties and any constructor with the same params
            var membersToKeep = originalType.GetMembers()
                .WhereAsArray(member =>
                    !(ShouldConvertProperty(member, originalType) ||
                     (member is IMethodSymbol method &&
                        method.IsConstructor() &&
                        method.Parameters.Equals(propertiesToAddAsParams))));

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

        private static bool ShouldConvertProperty(ISymbol member, INamedTypeSymbol containingType)
        {
            if (member is not IPropertySymbol property)
            {
                return false;
            }

            var propAccessibility = property.DeclaredAccessibility;
            var typeAccessibility = containingType.DeclaredAccessibility;
            return !(propAccessibility == Accessibility.Private ||
                propAccessibility == Accessibility.Protected ||
                (typeAccessibility == Accessibility.Public && propAccessibility == Accessibility.Internal) ||
                property.IsWriteOnly);
        }
    }
}
