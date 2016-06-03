// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpIntelliSense : EditorTestFixture
    {
        public CSharpIntelliSense(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpIntelliSense))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AtNamespaceLevel()
        {
            SetUpEditor(@"$$");

            SendKeys("usi");
            VerifyCompletionItemExists("using");

            SendKeys(VirtualKey.Tab);
            VerifyCurrentLineText("using$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SpeculativeTInList()
        {
            SetUpEditor(@"
class C
{
    $$
}");

            SendKeys("pub");
            VerifyCompletionItemExists("public");

            SendKeys(' ');
            VerifyCurrentLineText("public $$");

            SendKeys('t');
            VerifyCompletionItemExists("T");

            SendKeys(' ');
            SendKeys("Foo<T>() { }");

            VerifyTextContains(@"
class C
{
    public T Foo<T>() { }$$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void VerifyCompletionListMembersOnStaticTypesAndCompleteThem()
        {
            SetUpEditor(@"
public class Program
{
    static void Main(string[] args)
    {
        NavigateTo$$
    }
}

public static class NavigateTo
{
    public static void Search(string s){ }
    public static void Navigate(int i){ }
}");

            SendKeys('.');
            VerifyCompletionItemExists("Search", "Navigate");

            SendKeys('S', VirtualKey.Tab);
            VerifyCurrentLineText("NavigateTo.Search$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlAltSpace()
        {
            SetUpEditor("$$");

            DisableSuggestionMode();

            SendKeys("nam Foo", VirtualKey.Enter);
            SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            SendKeys("pu cla Program", VirtualKey.Enter);
            SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            SendKeys("pub stati voi Main(string[] args)", VirtualKey.Enter);
            SendKeys('{', VirtualKey.Enter, '}', VirtualKey.Up, VirtualKey.Enter);
            SendKeys("System.Console.writeline();");
            VerifyCurrentLineText("System.Console.WriteLine();$$");
            SendKeys(VirtualKey.Home, KeyPress(VirtualKey.End, ShiftState.Shift), VirtualKey.Delete);

            ExecuteCommand(WellKnownCommandNames.ToggleCompletionMode);

            SendKeys("System.Console.writeline();");
            VerifyCurrentLineText("System.Console.writeline();$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlAltSpaceOption()
        {
            SetUpEditor("$$");
            DisableSuggestionMode();

            SendKeys("nam Foo");
            VerifyCurrentLineText("namespace Foo$$");

            SetUpEditor("$$");
            EnableSuggestionMode();

            SendKeys("nam Foo");
            VerifyCurrentLineText("nam Foo$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlSpace()
        {
            SetUpEditor("class c { void M() {$$ } }");
            SendKeys(KeyPress(VirtualKey.Space, ShiftState.Ctrl));
            VerifyCompletionItemExists("System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NavigatingWithDownKey()
        {
            SetUpEditor("class c { void M() {$$ } }");
            SendKeys('c');
            VerifyCurrentCompletionItem("c");
            VerifyCompletionItemExists("c");

            SendKeys(VirtualKey.Down);
            VerifyCurrentCompletionItem("char");
            VerifyCompletionItemExists("char");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void XmlDocCommentIntelliSense()
        {
            SetUpEditor(@"
class Class1
{
    ///$$
    void Main(string[] args)
    {
    
    }
}");

            SendKeys("<s");
            VerifyCompletionItemExists("see", "seealso", "summary");

            SendKeys(VirtualKey.Enter);
            VerifyCurrentLineText("///<see cref=\"$$\"/>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void XmlTagCompletion()
        {
            SetUpEditor(@"/// $$
class C { }
");

            SendKeys("<summary>");
            VerifyCurrentLineText("/// <summary>$$</summary>");

            SetUpEditor(@"/// <summary>$$
class C { }
");

            SendKeys("</");
            VerifyCurrentLineText("/// <summary></summary>$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SignatureHelpShowsUp()
        {
            SetUpEditor(@"class Class1
{
    void Main(string[] args)
    {
        $$
    }
}");

            DisableSuggestionMode();

            SendKeys("Mai(");

            // TODO: Finish
        }
    }
}