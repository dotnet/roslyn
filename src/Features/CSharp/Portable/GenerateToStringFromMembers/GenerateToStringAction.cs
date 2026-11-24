// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateToStringFromMembers;

internal sealed partial class GenerateToStringFromMembersCodeRefactoringProvider
{
    private sealed partial class GenerateToStringAction(
        Document document,
        SyntaxNode typeDeclaration,
        INamedTypeSymbol containingType,
        ImmutableArray<ISymbol> selectedMembers,
        bool generateToString,
        bool generatePrintMembers) : CodeAction
    {
        private readonly bool _generateToString = generateToString;
        private readonly bool _generatePrintMembers = generatePrintMembers;
        private readonly Document _document = document;
        private readonly SyntaxNode _typeDeclaration = typeDeclaration;
        private readonly INamedTypeSymbol _containingType = containingType;
        private readonly ImmutableArray<ISymbol> _selectedMembers = selectedMembers;

        public override string EquivalenceKey => Title;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<IMethodSymbol>.GetInstance(out var methods);

            var compilation = await _document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var generator = _document.GetRequiredLanguageService<SyntaxGenerator>();
            var generatorInternal = _document.GetRequiredLanguageService<SyntaxGeneratorInternal>();

            // The synthesized ToString is readonly exactly when the synthesized
            // PrintMembers is readonly (for example, in record structs), so mirror
            // the readonly-ness of the PrintMembers the record currently has.
            var isReadOnly = IsPrintMembersReadOnly(_containingType);

            if (_generateToString)
            {
                methods.Add(generator.CreateToStringMethod(
                    generatorInternal, compilation, _containingType, isReadOnly));
            }

            if (_generatePrintMembers)
            {
                methods.Add(generator.CreatePrintMembersMethod(
                    compilation, _containingType, _selectedMembers, isReadOnly));
            }

            var info = await _document.GetCodeGenerationInfoAsync(CodeGenerationContext.Default, cancellationToken).ConfigureAwait(false);
            var formattingOptions = await _document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

            var newTypeDeclaration = info.Service.AddMembers(_typeDeclaration, methods, info, cancellationToken);

            var newDocument = await UpdateDocumentAndAddImportsAsync(
                _typeDeclaration, newTypeDeclaration, cancellationToken).ConfigureAwait(false);

            return await Formatter.FormatAsync(newDocument, formattingOptions, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsPrintMembersReadOnly(INamedTypeSymbol containingType)
            => containingType.GetMembers(WellKnownMemberNames.PrintMembersMethodName)
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m is { Parameters.Length: 1, IsStatic: false })?.IsReadOnly == true;

        private async Task<Document> UpdateDocumentAndAddImportsAsync(SyntaxNode oldType, SyntaxNode newType, CancellationToken cancellationToken)
        {
            var oldRoot = await _document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newDocument = _document.WithSyntaxRoot(oldRoot.ReplaceNode(oldType, newType));
            var addImportOptions = await _document.GetAddImportPlacementOptionsAsync(cancellationToken).ConfigureAwait(false);

            newDocument = await ImportAdder.AddImportsFromSymbolAnnotationAsync(newDocument, addImportOptions, cancellationToken).ConfigureAwait(false);
            return newDocument;
        }

        public override string Title
            => GetTitle(_generateToString, _generatePrintMembers);

        internal static string GetTitle(bool generateToString, bool generatePrintMembers)
            => generateToString
                ? generatePrintMembers
                    ? FeaturesResources.Generate_ToString_and_PrintMembers
                    : FeaturesResources.Generate_ToString
                : FeaturesResources.Generate_PrintMembers;
    }
}
