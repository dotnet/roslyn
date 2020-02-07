using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.Utilities
{
    internal static class TestRenameSymbolAnnotation
    {
        public static async Task ValidateRenameSymbolAnnotationsAsync(Solution originalSolution, Solution newSolution, ImmutableDictionary<string, string> expectedSymbolChanges, CancellationToken cancellationToken = default)
        {
            var changes = newSolution.GetChanges(originalSolution);
            var changedDocumentIds = changes.GetProjectChanges().SelectMany(p => p.GetChangedDocuments());

            var changedSymbols = await Rename.RenameSymbolAnnotation.GatherChangedSymbolsInDocumentsAsync(changedDocumentIds, newSolution, originalSolution, cancellationToken);

            Assert.Equal(expectedSymbolChanges.Count(), changedSymbols.Length);

            foreach (var (originalSymbol, currentSymbol) in changedSymbols)
            {
                Assert.True(expectedSymbolChanges.ContainsKey(originalSymbol.Name));
                Assert.Equal(expectedSymbolChanges[originalSymbol.Name], currentSymbol.Name);
            }
        }
    }
}
