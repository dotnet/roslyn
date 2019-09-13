using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests
{
    [UseExportProvider]
    public class DocumentRefactoringServiceTests
    {
        private const string OriginalFileName = "OriginalClassName";

        private async Task TestDocumentRefactoring(string markup, bool expectSuccess = true)
        {
            var testWorkspace = TestWorkspace.CreateCSharp(markup);
            var project = testWorkspace.CurrentSolution.Projects.Single();

            var document = project.Documents.Single();
            var refactorService = testWorkspace.GetService<IDocumentRefactoringService>();

            await refactorService.UpdateAfterInfoChangeAsync(document, document);

            if (expectSuccess)
            {
                document = testWorkspace.CurrentSolution.GetDocument(document.Id)!;
                var syntaxRoot = await document.GetSyntaxRootAsync().ConfigureAwait(false);

                var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>()!;
                IEnumerable<(SyntaxNode Node, string Name)> typeDeclarationPairs = syntaxRoot
                    .DescendantNodes()
                    .Where(syntaxFactsService.IsTypeDeclaration)
                    .Select(n => (n, syntaxFactsService.GetDisplayName(n, DisplayNameOptions.None)));

                Assert.True(typeDeclarationPairs.Any());

                var matchingTypeDeclarationPair = typeDeclarationPairs.FirstOrDefault(p => p.Name == Path.GetFileNameWithoutExtension(document.FilePath));
                Assert.NotEqual(default, matchingTypeDeclarationPair);
            }
        }

        [Fact]
        public Task TestRefactorSingleClass_RenamesClass()
        => TestDocumentRefactoring(
@"class OriginalClassName
{
}");

        [Fact]
        public Task TestRefactorMultipleClasses_RenamesClass()
        => TestDocumentRefactoring(
@"class OriginalClassName
{
}

class OtherClassName
{
}");
    }
}
