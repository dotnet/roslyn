// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [WorkItem(8333, "https://github.com/dotnet/roslyn/issues/8333")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInExpressionBody()
        {
            var markup = @"
class Ext
{
    void Goo(int a, int b) => [||]0;
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(1905, "https://github.com/dotnet/roslyn/issues/1905")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestAfterSemicolonForInvocationInExpressionStatement_ViaCommand()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        M1(1, 2);$$
        M2(1, 2, 3);
    }

    static void M1(int x, int y) { }

    static void M2(int x, int y, int z) { }
}";
            var expectedCode = @"
class Program
{
    static void Main(string[] args)
    {
        M1(2, 1);
        M2(1, 2, 3);
    }

    static void M1(int y, int x) { }

    static void M2(int x, int y, int z) { }
}";

            await TestChangeSignatureViaCommandAsync(
                LanguageNames.CSharp,
                markup: markup,
                updatedSignature: new[] { 1, 0 },
                expectedUpdatedInvocationDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestOnLambdaWithTwoDiscardParameters_ViaCommand()
        {
            var markup = @"
class Program
{
    static void M()
    {
        System.Func<int, string, int> f = $$(int _, string _) => 1;
    }
}";
            var expectedCode = @"
class Program
{
    static void M()
    {
        System.Func<int, string, int> f = (string _, int _) => 1;
    }
}";

            await TestChangeSignatureViaCommandAsync(
                LanguageNames.CSharp,
                markup: markup,
                updatedSignature: new[] { 1, 0 },
                expectedUpdatedInvocationDocumentCode: expectedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestOnAnonymousMethodWithTwoParameters_ViaCommand()
        {
            var markup = @"
class Program
{
    static void M()
    {
        System.Func<int, string, int> f = [||]delegate(int x, string y) { return 1; };
    }
}";
            await TestMissingAsync(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestOnAnonymousMethodWithTwoDiscardParameters_ViaCommand()
        {
            var markup = @"
class Program
{
    static void M()
    {
        System.Func<int, string, int> f = [||]delegate(int _, string _) { return 1; };
    }
}";
            await TestMissingAsync(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestAfterSemicolonForInvocationInExpressionStatement_ViaCodeAction()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        M1(1, 2);[||]
        M2(1, 2, 3);
    }

    static void M1(int x, int y) { }

    static void M2(int x, int y, int z) { }
}";

            await TestMissingAsync(markup);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingWhitespace()
        {
            var markup = @"
class Ext
{
    [||]
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingTrivia()
        {
            var markup = @"
class Ext
{
    // [||]
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingTrivia2()
        {
            var markup = @"
class Ext
{
    [||]//
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingDocComment()
        {
            var markup = @"
class Ext
{
    /// [||]
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingDocComment2()
        {
            var markup = @"
class Ext
{
    [||]///
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingAttributes1()
        {
            var markup = @"
class Ext
{
    [||][X]
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingAttributes2()
        {
            var markup = @"
class Ext
{
    [[||]X]
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInLeadingAttributes3()
        {
            var markup = @"
class Ext
{
    [X][||]
    void Goo(int a, int b)
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestNotInConstraints()
        {
            var markup = @"
class Ext
{
    void Goo<T>(int a, int b) where [||]T : class
    {
    };
}";

            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WorkItem(963225, "https://dev.azure.com/devdiv/DevDiv/_workitems/edit/963225")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task RemoveParameters_ReferenceInUnchangeableDocument()
        {
            var workspaceXml = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""CSharpAssembly"" CommonReferences=""true"">
        <Document FilePath = ""C1.cs"">
public class C1
{
    public static bool $$M(int x, int y)
    {
        return x > y;
    }
}</Document>
        <Document FilePath = ""C2.cs"">
public class C2
{
    bool _x = C1.M(1, 2); 
}</Document>
        <Document FilePath = ""C3.cs"" CanApplyChange=""false"">
public class C3
{
    bool _x = C1.M(1, 2); 
}</Document>
    </Project>
</Workspace>";

            var updatedSignature = new[] { 1, 0 };

            using (var testState = ChangeSignatureTestState.Create(XElement.Parse(workspaceXml)))
            {
                testState.TestChangeSignatureOptionsService.IsCancelled = false;
                testState.TestChangeSignatureOptionsService.UpdatedSignature = updatedSignature;
                var result = testState.ChangeSignature();

                Assert.True(result.Succeeded);
                Assert.Null(testState.ErrorMessage);

                foreach (var updatedDocument in testState.Workspace.Documents.Select(d => result.UpdatedSolution.GetDocument(d.Id)))
                {
                    if (updatedDocument.Name == "C1.cs")
                    {
                        // declaration should be changed
                        Assert.Contains("public static bool M(int y, int x)", (await updatedDocument.GetTextAsync(CancellationToken.None)).ToString());
                    }
                    else if (updatedDocument.Name == "C2.cs")
                    {
                        // changeable document should be changed
                        Assert.Contains("bool _x = C1.M(2, 1);", (await updatedDocument.GetTextAsync(CancellationToken.None)).ToString());
                    }
                    else if (updatedDocument.Name == "C3.cs")
                    {
                        // shouldn't change unchangeable document
                        Assert.Contains("bool _x = C1.M(1, 2);", (await updatedDocument.GetTextAsync(CancellationToken.None)).ToString());
                    }
                }
            }
        }
    }
}
