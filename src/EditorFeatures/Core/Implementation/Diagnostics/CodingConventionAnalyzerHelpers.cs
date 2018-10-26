using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class CodingConventionAnalyzerHelpers
    {
        public static async Task<T> GetOptionOrDefaultAsync<T>(this SyntaxNodeAnalysisContext context, Option<T> option, Func<string, T, T> conventionValueParser, T @default)
        {
            var statement = context.Node;
            var cancellationToken = context.CancellationToken;

            // While running in VisualStudio this should always return an OptionSet.
            var optionSet = await context.Options.GetDocumentOptionSetAsync(statement.SyntaxTree, cancellationToken).ConfigureAwait(false);
            if (optionSet != null)
            {
                return optionSet.GetOption(option);
            }

            // This code path is for tests only. It relies on the test creating a .editorconfig in the current working directory.
            var conventionContext = await GetConventionContextAsync(statement.SyntaxTree.FilePath, cancellationToken).ConfigureAwait(false);
            return GetOptionFromConventionContextOrDefault(conventionContext, option, conventionValueParser, @default);
        }

        private static async Task<ICodingConventionContext> GetConventionContextAsync(string filePath, CancellationToken cancellationToken)
        {
            var conventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager();

            // Provide a rooted path when filePath is just the file's name.
            var fullPath = string.IsNullOrEmpty(Path.GetDirectoryName(filePath))
                ? Path.Combine(Environment.CurrentDirectory, filePath)
                : filePath;

            return await conventionsManager.GetConventionContextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        }

        private static T GetOptionFromConventionContextOrDefault<T>(ICodingConventionContext conventionContext, Option<T> option, Func<string, T, T> conventionValueParser, T @default)
        {
            // Find the correct key name from the .editorconfig storage location
            var storageLocation = option.StorageLocations
                .OfType<EditorConfigStorageLocation<T>>()
                .FirstOrDefault();

            if (storageLocation != null
                && conventionContext.CurrentConventions.TryGetConventionValue(storageLocation.KeyName, out string conventionValue))
            {
                return conventionValueParser(conventionValue, @default);
            }

            return @default;
        }
    }
}
