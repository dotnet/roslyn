// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpTyping : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpTyping(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(CSharpTyping))
        {
        }

        [WpfFact, WorkItem(957250, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/957250")]
        public void TypingInPartialType()
        {
            SetUpEditor(@"
public partial class Test
{
    private int f;

    static void Main(string[] args) { }
    public void Noop()
    {
        f = 1;$$
    }
}
");
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
            VisualStudio.SolutionExplorer.AddFile(new ProjectUtils.Project(ProjectName), "PartialType2.cs", secondPartialDecl, open: false);
            VisualStudio.SolutionExplorer.AddFile(new ProjectUtils.Project(ProjectName), "PartialType3.cs", thirdPartialDecl, open: false);

            // Typing intermixed with explicit Wait operations to ensure that
            // we trigger multiple open file analyses along with cancellations.
            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            Wait(seconds: 1);
            VisualStudio.Editor.SendKeys("f = 1;");
            Wait(seconds: 1);
            VisualStudio.Editor.SendKeys(VirtualKey.Backspace);
            VisualStudio.Editor.SendKeys(VirtualKey.Backspace);
            Wait(seconds: 1);
            VisualStudio.Editor.SendKeys("2;");

            VisualStudio.Editor.Verify.TextContains(
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
