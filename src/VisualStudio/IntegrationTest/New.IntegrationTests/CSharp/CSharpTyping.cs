// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using WindowsInput.Native;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    public class CSharpTyping : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpTyping()
            : base(nameof(CSharpTyping))
        {
        }

        [IdeFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/957250")]
        public async Task TypingInPartialType()
        {
            await SetUpEditorAsync(@"
public partial class Test
{
    private int f;

    static void Main(string[] args) { }
    public void Noop()
    {
        f = 1;$$
    }
}
", HangMitigatingCancellationToken);
            var secondPartialDecl = @"
public partial class Test
{
    int val1 = 1, val2 = 2;
    public void TestA()
    {
        TestB();
    }
}
";
            var thirdPartialDecl = @"
public partial class Test
{
    public void TestB()
    {
        int val1x = this.val1, val2x = this.val2;
    }
}";

            await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "PartialType2.cs", secondPartialDecl, open: false, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "PartialType3.cs", thirdPartialDecl, open: false, HangMitigatingCancellationToken);

            // Typing intermixed with explicit Wait operations to ensure that
            // we trigger multiple open file analyses along with cancellations.
            await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1));
            await TestServices.Input.SendAsync("f = 1;", HangMitigatingCancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1));
            await TestServices.Input.SendAsync(VirtualKeyCode.BACK, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync(VirtualKeyCode.BACK, HangMitigatingCancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(1));
            await TestServices.Input.SendAsync("2;", HangMitigatingCancellationToken);

            await TestServices.EditorVerifier.TextContainsAsync(
                @"
public partial class Test
{
    private int f;

    static void Main(string[] args) { }
    public void Noop()
    {
        f = 1;
        f = 2;
    }
}");
        }
    }
}
