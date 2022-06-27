// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities.Formatting
{
    [UseExportProvider]
    public abstract class AbstractNewDocumentFormattingServiceTests
    {
        protected abstract string Language { get; }
        protected abstract TestWorkspace CreateTestWorkspace(string testCode, ParseOptions? parseOptions);

        internal Task TestAsync(string testCode, string expected)
        {
            return TestCoreAsync<object>(testCode, expected, options: null, parseOptions: null);
        }

        internal Task TestAsync<T>(string testCode, string expected, (PerLanguageOption2<T>, T)[]? options = null, ParseOptions? parseOptions = null)
        {
            return TestCoreAsync<T>(testCode,
                expected,
                options.Select(o => (new OptionKey(o.Item1, Language), o.Item2)).ToArray(),
                parseOptions);
        }

        internal Task TestAsync<T>(string testCode, string expected, (Option2<T>, T)[]? options = null, ParseOptions? parseOptions = null)
        {
            return TestCoreAsync<T>(testCode,
                expected,
                options.Select(o => (new OptionKey(o.Item1), o.Item2)).ToArray(),
                parseOptions);
        }

        internal Task TestAsync(string testCode, string expected, (OptionKey, object)[]? options = null, ParseOptions? parseOptions = null)
        {
            return TestCoreAsync(testCode,
                expected,
                options,
                parseOptions);
        }

        private async Task TestCoreAsync<T>(string testCode, string expected, (OptionKey, T)[]? options, ParseOptions? parseOptions)
        {
            using (var workspace = CreateTestWorkspace(testCode, parseOptions))
            {
                if (options is not null)
                {
                    var workspaceOptions = workspace.Options;
                    foreach (var option in options)
                    {
                        workspaceOptions = workspaceOptions.WithChangedOption(option.Item1, option.Item2);
                    }

                    workspace.SetOptions(workspaceOptions);
                }

                var document = workspace.CurrentSolution.Projects.First().Documents.First();

                var formattingService = document.GetRequiredLanguageService<INewDocumentFormattingService>();
                var formattedDocument = await formattingService.FormatNewDocumentAsync(document, hintDocument: null, CancellationToken.None);

                var actual = await formattedDocument.GetTextAsync();
                AssertEx.EqualOrDiff(expected, actual.ToString());
            }
        }
    }
}
