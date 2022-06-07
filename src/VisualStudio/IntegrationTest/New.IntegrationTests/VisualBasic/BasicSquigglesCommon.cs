// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    public abstract class BasicSquigglesCommon : AbstractEditorTest
    {
        protected BasicSquigglesCommon(string projectTemplate)
            : base(nameof(BasicSquigglesCommon), projectTemplate)
        {
        }

        protected override string LanguageName => LanguageNames.VisualBasic;

        [WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [IdeFact]
        public virtual async Task VerifySyntaxErrorSquiggles()
        {
            await TestServices.Editor.SetTextAsync(@"Class A
      Shared Sub S()
        Dim x = 1 +
      End Sub
End Class", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.ErrorTagsAsync(
                new[] { "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'\\r'[50-51]" },
                HangMitigatingCancellationToken);
        }

        [WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [IdeFact]
        public virtual async Task VerifySemanticErrorSquiggles()
        {
            await TestServices.Editor.SetTextAsync(@"Class A
      Shared Sub S(b as Bar)
        Console.WriteLine(b)
      End Sub
End Class", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.ErrorTagsAsync(
                new[] { "Microsoft.VisualStudio.Text.Tagging.ErrorTag:'Bar'[33-36]" },
                HangMitigatingCancellationToken);
        }
    }
}
