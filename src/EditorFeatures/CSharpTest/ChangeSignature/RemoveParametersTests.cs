// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task RemoveParameters1()
        {
            var markup = @"
static class Ext
{
    /// <summary>
    /// This is a summary of <see cref=""M(object, int, string, bool, int, string, int[])""/>
    /// </summary>
    /// <param name=""o""></param>
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    /// <param name=""x""></param>
    /// <param name=""y""></param>
    /// <param name=""p""></param>
    static void $$M(this object o, int a, string b, bool c, int x = 0, string y = ""Zero"", params int[] p)
    {
        object t = new object();

        M(t, 1, ""two"", true, 3, ""four"", new[] { 5, 6 });
        M(t, 1, ""two"", true, 3, ""four"", 5, 6);
        t.M(1, ""two"", true, 3, ""four"", new[] { 5, 6 });
        t.M(1, ""two"", true, 3, ""four"", 5, 6);

        M(t, 1, ""two"", true, 3, ""four"");
        M(t, 1, ""two"", true, 3);
        M(t, 1, ""two"", true);

        M(t, 1, ""two"", c: true);
        M(t, 1, ""two"", true, 3, y: ""four"");

        M(t, 1, ""two"", true, 3, p: new[] { 5 });
        M(t, 1, ""two"", true, p: new[] { 5 });
        M(t, 1, ""two"", true, y: ""four"");
        M(t, 1, ""two"", true, x: 3);

        M(t, 1, ""two"", true, y: ""four"", x: 3);
        M(t, 1, y: ""four"", x: 3, b: ""two"", c: true);
        M(t, y: ""four"", x: 3, c: true, b: ""two"", a: 1);
        M(t, p: new[] { 5 }, y: ""four"", x: 3, c: true, b: ""two"", a: 1);
        M(p: new[] { 5 }, y: ""four"", x: 3, c: true, b: ""two"", a: 1, o: t);
    }
}";
            var updatedSignature = new[] { 0, 2, 5 };
            var updatedCode = @"
static class Ext
{
    /// <summary>
    /// This is a summary of <see cref=""M(object, string, string)""/>
    /// </summary>
    /// <param name=""o""></param>
    /// <param name=""b""></param>
    /// <param name=""y""></param>
    /// 
    /// 
    /// 
    /// 
    static void M(this object o, string b, string y = ""Zero"")
    {
        object t = new object();

        M(t, ""two"", ""four"");
        M(t, ""two"", ""four"");
        t.M(""two"", ""four"");
        t.M(""two"", ""four"");

        M(t, ""two"", ""four"");
        M(t, ""two"");
        M(t, ""two"");

        M(t, ""two"");
        M(t, ""two"", y: ""four"");

        M(t, ""two"");
        M(t, ""two"");
        M(t, ""two"", y: ""four"");
        M(t, ""two"");

        M(t, ""two"", y: ""four"");
        M(t, y: ""four"", b: ""two"");
        M(t, y: ""four"", b: ""two"");
        M(t, y: ""four"", b: ""two"");
        M(y: ""four"", b: ""two"", o: t);
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task RemoveParameters_GenericParameterType()
        {
            var markup = @"
class DA
{
    void M(params int[] i) { }

    void B()
    {
        DP20<int>.D d = new DP20<int>.D(M);

        /*DA19*/$$d(0);
        d();
        d(0, 1);
    }
}
public class DP20<T>
{
    public delegate void /*DP20*/D(params T[] t);
    public event D E1;
    public event D E2;

    public void M1(params T[] t) { }
    public void M2(params T[] t) { }
    public void M3(params T[] t) { }

    void B()
    {
        D d = new D(M1);
        E1 += new D(M2);
        E2 -= new D(M3);
    }
}";
            var updatedSignature = Array.Empty<int>();
            var updatedCode = @"
class DA
{
    void M() { }

    void B()
    {
        DP20<int>.D d = new DP20<int>.D(M);

        /*DA19*/d();
        d();
        d();
    }
}
public class DP20<T>
{
    public delegate void /*DP20*/D();
    public event D E1;
    public event D E2;

    public void M1() { }
    public void M2() { }
    public void M3() { }

    void B()
    {
        D d = new D(M1);
        E1 += new D(M2);
        E2 -= new D(M3);
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        [WorkItem(1102830)]
        [WorkItem(784, "https://github.com/dotnet/roslyn/issues/784")]
        public async Task RemoveParameters_ExtensionMethodInAnotherFile()
        {
            var workspaceXml = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSharpAssembly"" CommonReferences=""true"">";

            for (int i = 0; i <= 4; i++)
            {
                workspaceXml += $@"
<Document FilePath = ""C{i}.cs"">
class C{i}
{{
    void M()
    {{
        C5 c = new C5();
        c.Ext(1, ""two"");
    }}
}}
</Document>";
            }

            workspaceXml += @"
<Document FilePath = ""C5.cs"">
public class C5
{
}

public static class C5Ext
{
    public void $$Ext(this C5 c, int i, string s)
    {
    }
}
</Document>";

            for (int i = 6; i <= 9; i++)
            {
                workspaceXml += $@"
<Document FilePath = ""C{i}.cs"">
class C{i}
{{
    void M()
    {{
        C5 c = new C5();
        c.Ext(1, ""two"");
    }}
}}
</Document>";
            }

            workspaceXml += @"
    </Project>
</Workspace>";

            // Ext(this F f, int i, string s) --> Ext(this F f, string s)
            // If a reference does not bind correctly, it will believe Ext is not an extension
            // method and remove the string argument instead of the int argument.

            var updatedSignature = new[] { 0, 2 };

            using (var testState = await ChangeSignatureTestState.CreateAsync(XElement.Parse(workspaceXml)))
            {
                testState.TestChangeSignatureOptionsService.IsCancelled = false;
                testState.TestChangeSignatureOptionsService.UpdatedSignature = updatedSignature;
                var result = testState.ChangeSignature();

                Assert.True(result.Succeeded);
                Assert.Null(testState.ErrorMessage);

                foreach (var updatedDocument in testState.Workspace.Documents.Select(d => result.UpdatedSolution.GetDocument(d.Id)))
                {
                    if (updatedDocument.Name == "C5.cs")
                    {
                        Assert.Contains("void Ext(this C5 c, string s)", updatedDocument.GetTextAsync(CancellationToken.None).Result.ToString());
                    }
                    else
                    {
                        Assert.Contains(@"c.Ext(""two"");", updatedDocument.GetTextAsync(CancellationToken.None).Result.ToString());
                    }
                }
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void ChangeSignatureCommandDisabledInSubmission()
        {
            var exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(typeof(InteractiveDocumentSupportsFeatureService)));

            using (var workspace = TestWorkspaceFactory.CreateWorkspace(XElement.Parse(@"
                <Workspace>
                    <Submission Language=""C#"" CommonReferences=""true"">  
                        class C
                        {
                            void M$$(int x)
                            {
                            }
                        }
                    </Submission>
                </Workspace> "),
                workspaceKind: WorkspaceKind.Interactive,
                exportProvider: exportProvider))
            {
                // Force initialization.
                workspace.GetOpenDocumentIds().Select(id => workspace.GetTestDocument(id).GetTextView()).ToList();

                var textView = workspace.Documents.Single().GetTextView();

                var handler = new ChangeSignatureCommandHandler();
                var delegatedToNext = false;
                Func<CommandState> nextHandler = () =>
                {
                    delegatedToNext = true;
                    return CommandState.Unavailable;
                };

                var state = handler.GetCommandState(new Commands.RemoveParametersCommandArgs(textView, textView.TextBuffer), nextHandler);
                Assert.True(delegatedToNext);
                Assert.False(state.IsAvailable);

                delegatedToNext = false;
                state = handler.GetCommandState(new Commands.ReorderParametersCommandArgs(textView, textView.TextBuffer), nextHandler);
                Assert.True(delegatedToNext);
                Assert.False(state.IsAvailable);
            }
        }
    }
}
