// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.CSharp.Debugging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Debugging
{
    public partial class ProximityExpressionsGetterTests
    {
        private SyntaxTree GetTree()
        {
            return SyntaxFactory.ParseSyntaxTree(Resources.ProximityExpressionsGetterTestFile);
        }

        private SyntaxTree GetTreeFromCode(string code)
        {
            return SyntaxFactory.ParseSyntaxTree(code);
        }

        public async Task GenerateBaseline()
        {
            Console.WriteLine(typeof(FactAttribute));

            var text = Resources.ProximityExpressionsGetterTestFile;
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(text))
            {
                var languageDebugInfo = new CSharpLanguageDebugInfoService();

                var hostdoc = workspace.Documents.First();
                var snapshot = hostdoc.TextBuffer.CurrentSnapshot;
                var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

                var builder = new StringBuilder();
                var statements = (await document.GetSyntaxRootAsync(CancellationToken.None)).DescendantTokens().Select(t => t.GetAncestor<StatementSyntax>()).Distinct().WhereNotNull();

                // Try to get proximity expressions at every token position and the start of every
                // line.
                var index = 0;
                foreach (var statement in statements)
                {
                    builder.AppendLine("[WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]");
                    builder.AppendLine("public void TestAtStartOfStatement_" + index + "()");
                    builder.AppendLine("{");

                    var token = statement.GetFirstToken();
                    var line = snapshot.GetLineFromPosition(token.SpanStart);

                    builder.AppendLine("    //// Line " + (line.LineNumber + 1));
                    builder.AppendLine();
                    if (line.LineNumber > 0)
                    {
                        builder.AppendLine("    //// " + snapshot.GetLineFromLineNumber(line.LineNumber - 1).GetText());
                    }

                    builder.AppendLine("    //// " + line.GetText());
                    var charIndex = token.SpanStart - line.Start;
                    builder.AppendLine("    //// " + new string(' ', charIndex) + "^");
                    builder.AppendLine("    var tree = GetTree(\"ProximityExpressionsGetterTestFile.cs\");");
                    builder.AppendLine("    var terms = CSharpProximityExpressionsService.Do(tree, " + token.SpanStart + ");");

                    var proximityExpressionsGetter = new CSharpProximityExpressionsService();
                    var terms = await proximityExpressionsGetter.GetProximityExpressionsAsync(document, token.SpanStart, CancellationToken.None);
                    if (terms == null)
                    {
                        builder.AppendLine("    Assert.Null(terms);");
                    }
                    else
                    {
                        builder.AppendLine("    Assert.NotNull(terms);");

                        var termsString = terms.Select(t => "\"" + t + "\"").Join(", ");
                        builder.AppendLine("    AssertEx.Equal(new[] { " + termsString + " }, terms);");
                    }

                    builder.AppendLine("}");
                    builder.AppendLine();
                    index++;
                }

                var str = builder.ToString();
                Console.WriteLine(str);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestWithinStatement_1()
        {
            var tree = GetTreeFromCode(@"using System;
using System.Collections.Generic;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var xx = true;
            var yy = new List<bool>();
            yy.Add(xx?true:false);
        }
    }
}");
            var terms = CSharpProximityExpressionsService.Do(tree, 245);
            Assert.NotNull(terms);
            AssertEx.Equal(new[] { "yy", "xx" }, terms);
        }

        private async Task TestProximityExpressionGetterAsync(
            string markup,
            Func<CSharpProximityExpressionsService, Document, int, Task> continuation)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(markup))
            {
                var testDocument = workspace.Documents.Single();
                var caretPosition = testDocument.CursorPosition.Value;
                var snapshot = testDocument.TextBuffer.CurrentSnapshot;
                var languageDebugInfo = new CSharpLanguageDebugInfoService();
                var document = workspace.CurrentSolution.GetDocument(testDocument.Id);

                var proximityExpressionsGetter = new CSharpProximityExpressionsService();

                await continuation(proximityExpressionsGetter, document, caretPosition);
            }
        }

        private async Task TestTryDoAsync(string input, params string[] expectedTerms)
        {
            await TestProximityExpressionGetterAsync(input, async (getter, document, position) =>
            {
                var actualTerms = await getter.GetProximityExpressionsAsync(document, position, CancellationToken.None);

                Assert.Equal(expectedTerms.Length == 0, actualTerms == null);
                if (expectedTerms.Length > 0)
                {
                    AssertEx.Equal(expectedTerms, actualTerms);
                }
            });
        }

        private async Task TestIsValidAsync(string input, string expression, bool expectedValid)
        {
            await TestProximityExpressionGetterAsync(input, async (getter, semanticSnapshot, position) =>
            {
                var actualValid = await getter.IsValidAsync(semanticSnapshot, position, expression, CancellationToken.None);
                Assert.Equal(expectedValid, actualValid);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestTryDo1()
        {
            await TestTryDoAsync("class Class { void Method() { string local;$$ } }", "local", "this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestNoParentToken()
        {
            await TestTryDoAsync("$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestIsValid1()
        {
            await TestIsValidAsync("class Class { void Method() { string local;$$ } }", "local", true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestIsValidWithDiagnostics()
        {
            // local doesn't exist in this context
            await TestIsValidAsync("class Class { void Method() { string local; } $$}", "local", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestIsValidReferencingLocalBeforeDeclaration()
        {
            await TestIsValidAsync("class Class { void Method() { $$int i; int j; } }", "j", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestIsValidReferencingUndefinedVariable()
        {
            await TestIsValidAsync("class Class { void Method() { $$int i; int j; } }", "k", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestIsValidNoTypeSymbol()
        {
            await TestIsValidAsync("namespace Namespace$$ { }", "foo", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestIsValidLocalAfterPosition()
        {
            await TestIsValidAsync("class Class { void Method() { $$ int i; string local; } }", "local", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestThis()
        {
            await TestTryDoAsync(@"
class Class 
{
    public Class() : this(true) 
    {
        base.ToString();
        this.ToString()$$;
    }
}", "this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestArrayCreationExpression()
        {
            await TestTryDoAsync(@"
class Class 
{
    void Method()
    {
        int[] i = new int[] { 3 }$$;
    }
}", "i", "this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestPostfixUnaryExpressionSyntax()
        {
            await TestTryDoAsync(@"
class Class 
{
    void Method()
    {
        int i = 3;
        i++$$;
    }
}", "i", "this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestLabeledStatement()
        {
            await TestTryDoAsync(@"
class Class 
{
    void Method()
    {
        label: int i = 3;
        label2$$: i++;
    }
}", "i", "this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestThrowStatement()
        {
            await TestTryDoAsync(@"
class Class 
{
    static void Method()
    {
        e = new Exception();
        thr$$ow e;
    }
}", "e");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestDoStatement()
        {
            await TestTryDoAsync(@"
class Class 
{
    static void Method()
    {
        do$$ { } while (true);
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestLockStatement()
        {
            await TestTryDoAsync(@"
class Class 
{
    static void Method()
    {
        lock(typeof(Cl$$ass)) { };
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestWhileStatement()
        {
            await TestTryDoAsync(@"
class Class 
{
    static void Method()
    {
        while(DateTime.Now <$$ DateTime.Now) { };
    }
}", "DateTime", "DateTime.Now");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestForStatementWithDeclarators()
        {
            await TestTryDoAsync(@"
class Class 
{
    static void Method()
    {
        for(int i = 0; i < 10; i$$++) { }
    }
}", "i");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestForStatementWithInitializers()
        {
            await TestTryDoAsync(@"
class Class 
{
    static void Method()
    {
        int i = 0;
        for(i = 1; i < 10; i$$++) { }
    }
}", "i");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestUsingStatement()
        {
            await TestTryDoAsync(@"
class Class 
{
    void Method()
    {
        using (FileStream fs = new FileStream($$)) { }
    }
}", "this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        [WorkItem(538879)]
        public async Task TestValueInPropertySetter()
        {
            await TestTryDoAsync(@"
class Class 
{
    string Name
    {
        get { return """"; }
        set { $$ }
    }
}", "this", "value");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestValueInEventAdd()
        {
            await TestTryDoAsync(@"
class Class 
{
    event Action Event
    {
        add { $$ }
        set { }
    }
}", "this", "value");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestValueInEventRemove()
        {
            await TestTryDoAsync(@"
class Class 
{
    event Action Event
    {
        add { }
        remove { $$ }
    }
}", "this", "value");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        [WorkItem(538880)]
        public async Task TestValueInIndexerSetter()
        {
            await TestTryDoAsync(@"
class Class 
{
    string this[int index]
    {
        get { return """"; }
        set { $$ }
    }
}", "index", "this", "value");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        [WorkItem(538881)]
        public async Task TestCatchBlock()
        {
            await TestTryDoAsync(@"
class Class 
{
    void Method()
    {
        try { }
        catch(Exception ex) { int $$ }
    }
}", "ex", "this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        [WorkItem(538881)]
        public async Task TestCatchBlockEmpty_OpenBrace()
        {
            await TestTryDoAsync(@"
class Class 
{
    void Method()
    {
        try { }
        catch(Exception ex) { $$ }
    }
}", "ex", "this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task TestCatchBlockEmpty_CloseBrace()
        {
            await TestTryDoAsync(@"
class Class 
{
    void Method()
    {
        try { }
        catch(Exception ex) { } $$ 
    }
}", "this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        [WorkItem(538874)]
        public async Task TestObjectCreation()
        {
            await TestTryDoAsync(@"
class Class 
{
    void Method()
    {
        $$Foo(new Bar(a).Baz);
    }
}", "a", "new Bar(a).Baz", "Foo", "this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        [WorkItem(538874)]
        public async Task Test2()
        {
            await TestIsValidAsync(@"
class D
{
   private static int x;
}

class Class 
{
    void Method()
    {
        $$Foo(D.x);
    }
}", "D.x", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        [WorkItem(538890)]
        public async Task TestArrayCreation()
        {
            await TestTryDoAsync(@"
class Class 
{
    int a;
    void Method()
    {
        $$new int[] { a };
    }
}", "this");
        }

        [WorkItem(751141)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task Bug751141()
        {
            await TestTryDoAsync(@"
class Program
{
    double m_double = 1.1;
    static void Main(string[] args)
    {
        new Program().M();
    }
    void M()
    {
        int local_int = (int)m_double;
        $$System.Diagnostics.Debugger.Break();
    }
}
", "System.Diagnostics.Debugger", "local_int", "m_double", "(int)m_double", "this");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ForLoopExpressionsInFirstStatementOfLoop1()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        for(int i = 0; i < 5; i++)
        {
            $$var x = 8;
        }
    }
}", "i", "x");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ForLoopExpressionsInFirstStatementOfLoop2()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int i = 0, j = 0, k = 0, m = 0, n = 0;

        for(i = 0; j < 5; k++)
        {
            $$m = 8;
            n = 7;
        }
    }
}", "m", "i", "j", "k");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ForLoopExpressionsInFirstStatementOfLoop3()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int i = 0, j = 0, k = 0, m = 0;

        for(i = 0; j < 5; k++)
        {
            var m = 8;
            $$var n = 7;
        }
    }
}", "m", "n");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ForLoopExpressionsInFirstStatementOfLoop4()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int i = 0, j = 0, k = 0, m = 0;

        for(i = 0; j < 5; k++)
            $$m = 8;
    }
}", "m", "i", "j", "k");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ForEachLoopExpressionsInFirstStatementOfLoop1()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        foreach (var x in new int[] { 1, 2, 3 })
        {
            $$var z = 0;
        }
    }
}", "x", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ForEachLoopExpressionsInFirstStatementOfLoop2()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        foreach (var x in new int[] { 1, 2, 3 })
            $$var z = 0;
    }
}", "x", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterForLoop1()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0;

        for (a = 5; b < 1; b++)
        {
            c = 8;
            d = 9; // included
        }
        
        $$var z = 0;
    }
}", "a", "b", "d", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterForLoop2()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0;

        for (a = 5; b < 1; b++)
        {
            c = 8;
            int d = 9; // not included
        }
        
        $$var z = 0;
    }
}", "a", "b", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterForEachLoop()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0;

        foreach (var q in new int[] {1, 2, 3})
        {
            c = 8;
            d = 9; // included
        }
        
        $$var z = 0;
    }
}", "q", "d", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterNestedForLoop()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;

        for (a = 5; b < 1; b++)
        {
            c = 8;
            d = 9;
            for (a = 7; b < 9; b--)
            {
                e = 8;
                f = 10; // included
            }
        }
        
        $$var z = 0;
    }
}", "a", "b", "f", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterCheckedStatement()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;

        checked
        {
            a = 7;
            b = 0; // included
        }
        
        $$var z = 0;
    }
}", "b", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterUncheckedStatement()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;

        unchecked
        {
            a = 7;
            b = 0; // included
        }
        
        $$var z = 0;
    }
}", "b", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterIfStatement()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;

        if (a == 0)
        {
            c = 8; 
            d = 9; // included
        }

        $$var z = 0;
    }
}", "a", "d", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterIfStatementWithElse()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;

        if (a == 0)
        {
            c = 8; 
            d = 9; // included
        }
        else
        {
            e = 1;
            f = 2; // included
        }

        $$var z = 0;
    }
}", "a", "d", "f", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterLockStatement()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;

        lock (new object())
        {
            a = 2;
            b = 3; // included
        }

        $$var z = 0;
    }
}", "b", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterSwitchStatement()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0, g = 0;

        switch(a)
        {
            case 1:
                b = 7;
                c = 8; // included
                break;
            case 2:
                d = 9;
                e = 10; // included
                break;
            default:
                f = 1;
                g = 2; // included
                break;
        }

        $$var z = 0;
    }
}", "a", "c", "e", "g", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterTryStatement()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0, g = 0;

        try
        {
            a = 2;
            b = 3; // included
        }
        catch (System.DivideByZeroException)
        {
            c = 2;
            d = 5; // included
        }
        catch (System.EntryPointNotFoundException)
        {
            e = 8;
            f = 9; // included
        }

        $$var z = 0;
    }
}", "b", "d", "f", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterTryStatementWithFinally()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0, g = 0;

        try
        {
            a = 2;
            b = 3;
        }
        catch (System.DivideByZeroException)
        {
            c = 2;
            d = 5;
        }
        catch (System.EntryPointNotFoundException)
        {
            e = 8;
            f = 9;
        }
        finally
        {
            g = 2; // included
        }

        $$var z = 0;
    }
}", "g", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterUsingStatement()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0, g = 0;

        using (null as System.IDisposable)
        {
            a = 4;
            b = 8; // Included
        }

        $$var z = 0;
    }
}", "b", "z");
        }

        [WorkItem(775161)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsAfterWhileStatement()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0, g = 0;

        while (a < 5)
        {
            a++;
            b = 8; // Included
        }

        $$var z = 0;
    }
}", "a", "b", "z");
        }

        [WorkItem(778215)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public async Task ExpressionsInParenthesizedExpressions()
        {
            await TestTryDoAsync(@"class Program
{
    static void Main(string[] args)
    {
        int i = 0, j = 0, k = 0, m = 0;
        int flags = 7;

        if((flags & i) == k)
        {
            $$ m = 8;
        }
    }
}", "m", "flags", "i", "k");
        }
    }
}
