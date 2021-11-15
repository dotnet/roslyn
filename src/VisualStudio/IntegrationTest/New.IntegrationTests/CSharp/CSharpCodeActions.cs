// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpCodeActions : AbstractEditorTest
    {
        public CSharpCodeActions()
            : base(nameof(CSharpCodeActions))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task AddUsingExactMatchBeforeRenameTracking()
        {
            await SetUpEditorAsync(@"
public class Program
{
    static void Main(string[] args)
    {
        P2$$ p;
    }
}

public class P2 { }", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKey.Backspace, VirtualKey.Backspace, "Stream");
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.EventHookup, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Rename, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.RenameTracking, HangMitigatingCancellationToken);

            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var expectedItems = new[]
            {
                "using System.IO;",
                "Rename 'P2' to 'Stream'",
                "System.IO.Stream",
                "Generate class 'Stream' in new file",
                "Generate class 'Stream'",
                "Generate nested class 'Stream'",
                "Generate new type...",
                "Remove unused variable",
                "Suppress or Configure issues",
                "Suppress CS0168",
                "in Source",
                "Configure CS0168 severity",
                "None",
                "Silent",
                "Suggestion",
                "Warning",
                "Error",
            };

            await TestServices.EditorVerifier.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync("using System.IO;", cancellationToken: HangMitigatingCancellationToken);
        }
    }
}
