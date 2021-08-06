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
        protected abstract TestWorkspace CreateTestWorkspace(string testCode);

        internal Task TestAsync<T>(string testCode, string expected, params (PerLanguageOption2<T>, T)[] options)
        {
            return TestCoreAsync<T>(testCode,
                expected,
                options.Select(o => (new OptionKey(o.Item1, Language), o.Item2)).ToArray());
        }

        internal Task TestAsync<T>(string testCode, string expected, params (Option2<T>, T)[] options)
        {
            return TestCoreAsync<T>(testCode,
                expected,
                options.Select(o => (new OptionKey(o.Item1), o.Item2)).ToArray());
        }

        private async Task TestCoreAsync<T>(string testCode, string expected, (OptionKey, T)[] options)
        {
            using (var workspace = CreateTestWorkspace(testCode))
            {
                var workspaceOptions = workspace.Options;
                foreach (var option in options)
                {
                    workspaceOptions = workspaceOptions.WithChangedOption(option.Item1, option.Item2);
                }

                workspace.SetOptions(workspaceOptions);

                var document = workspace.CurrentSolution.Projects.First().Documents.First();

                var formattingService = document.GetRequiredLanguageService<INewDocumentFormattingService>();
                var formattedDocument = await formattingService.FormatNewDocumentAsync(document, hintDocument: null, CancellationToken.None);

                // Format to match what AbstractEditorFactory does
                formattedDocument = await Formatter.FormatAsync(formattedDocument);

                var actual = await formattedDocument.GetTextAsync();
                AssertEx.EqualOrDiff(expected, actual.ToString());
            }
        }
    }
}
