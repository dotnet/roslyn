// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToRecord
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToRecord), Shared)]
    internal class ConvertToRecordRefactoringProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConvertToRecordRefactoringProvider()
        {
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
                type.BaseType != null ||
                type.Interfaces.IsEmpty ||
                // records can't be static and so if the class is static we probably shouldn't convert it
                type.IsStatic ||
                // since we can't have any classes inherit this one, if a class is abstract it probably is inherited
                // by a non-record type, so we should fail
                type.IsAbstract)
            {
                return;
            }

            var codeAction = TryConvertToRecordAsync(document, type, cancellationToken);
        }

        private static Task<CodeAction?> TryConvertToRecordAsync(Document document, INamedTypeSymbol originalType, CancellationToken cancellationToken)
        {
            var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();
            var changedRecordType = CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                originalType.GetAttributes(),
                originalType.DeclaredAccessibility,
                originalType.IsReadOnly
                    ? DeclarationModifiers.ReadOnly
                    : DeclarationModifiers.None,
                isRecord: true,
                originalType.TypeKind,
                originalType.Name);
            //TODO
            //codeGenerationService.CreateNamedTypeDeclaration(changedRecordType, CodeGenerationDestination.CompilationUnit, CodeGenerationContextInfo)
            return null;
        }
    }
}
