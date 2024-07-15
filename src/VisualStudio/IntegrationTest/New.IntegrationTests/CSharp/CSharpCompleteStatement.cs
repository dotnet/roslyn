// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpCompleteStatement : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpCompleteStatement()
            : base(nameof(CSharpCompleteStatement))
        {
        }

        [IdeFact]
        public async Task UndoRestoresCaretPosition1()
        {
            await SetUpEditorAsync(@"
public class Test
{
    private object f;

    public void Method()
    {
        f.ToString($$)
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(';', HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString();$$", assertCaretPosition: true);

            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString($$)", assertCaretPosition: true);
        }

        [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/43400")]
        public async Task UndoRestoresCaretPosition2()
        {
            await SetUpEditorAsync(@"
public class Test
{
    private object f;

    public void Method()
    {
        Method(condition ? whenTrue $$)
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(';', HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Method(condition ? whenTrue );$$", assertCaretPosition: true);

            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Method(condition ? whenTrue $$)", assertCaretPosition: true);
        }

        [IdeFact]
        public async Task UndoRestoresFormatBeforeRestoringCaretPosition()
        {
            await SetUpEditorAsync(@"
public class Test
{
    private object f;

    public void Method()
    {
        f.ToString($$ )
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(';', HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString();$$", assertCaretPosition: true);

            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString( );$$", assertCaretPosition: true);

            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString($$ )", assertCaretPosition: true);
        }
    }
}
