// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
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

        public void GenerateBaseline()
        {
            Console.WriteLine(typeof(FactAttribute));

            var text = Resources.ProximityExpressionsGetterTestFile;
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(text))
            {
                var languageDebugInfo = new CSharpLanguageDebugInfoService();

                var hostdoc = workspace.Documents.First();
                var snapshot = hostdoc.TextBuffer.CurrentSnapshot;
                var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

                var builder = new StringBuilder();
                var statements = document.GetSyntaxRootAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None).DescendantTokens().Select(t => t.GetAncestor<StatementSyntax>()).Distinct().WhereNotNull();

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
                    var terms = proximityExpressionsGetter.GetProximityExpressionsAsync(document, token.SpanStart, CancellationToken.None).Result;
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

        private void TestProximityExpressionGetter(
            string markup,
            Action<CSharpProximityExpressionsService, Document, int> continuation)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(markup))
            {
                var testDocument = workspace.Documents.Single();
                var caretPosition = testDocument.CursorPosition.Value;
                var snapshot = testDocument.TextBuffer.CurrentSnapshot;
                var languageDebugInfo = new CSharpLanguageDebugInfoService();
                var document = workspace.CurrentSolution.GetDocument(testDocument.Id);

                var proximityExpressionsGetter = new CSharpProximityExpressionsService();

                continuation(proximityExpressionsGetter, document, caretPosition);
            }
        }

        private void TestTryDo(string input, params string[] expectedTerms)
        {
            TestProximityExpressionGetter(input, (getter, document, position) =>
            {
                var actualTerms = getter.GetProximityExpressionsAsync(document, position, CancellationToken.None).Result;

                Assert.Equal(expectedTerms.Length == 0, actualTerms == null);
                if (expectedTerms.Length > 0)
                {
                    AssertEx.Equal(expectedTerms, actualTerms);
                }
            });
        }

        private void TestIsValid(string input, string expression, bool expectedValid)
        {
            TestProximityExpressionGetter(input, (getter, semanticSnapshot, position) =>
            {
                var actualValid = getter.IsValidAsync(semanticSnapshot, position, expression, CancellationToken.None).Result;
                Assert.Equal(expectedValid, actualValid);
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestTryDo1()
        {
            TestTryDo("class Class { void Method() { string local;$$ } }", "local", "this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestNoParentToken()
        {
            TestTryDo("$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestIsValid1()
        {
            TestIsValid("class Class { void Method() { string local;$$ } }", "local", true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestIsValidWithDiagnostics()
        {
            // local doesn't exist in this context
            TestIsValid("class Class { void Method() { string local; } $$}", "local", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestIsValidReferencingLocalBeforeDeclaration()
        {
            TestIsValid("class Class { void Method() { $$int i; int j; } }", "j", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestIsValidReferencingUndefinedVariable()
        {
            TestIsValid("class Class { void Method() { $$int i; int j; } }", "k", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestIsValidNoTypeSymbol()
        {
            TestIsValid("namespace Namespace$$ { }", "foo", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestIsValidLocalAfterPosition()
        {
            TestIsValid("class Class { void Method() { $$ int i; string local; } }", "local", false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestThis()
        {
            TestTryDo(@"
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
        public void TestArrayCreationExpression()
        {
            TestTryDo(@"
class Class 
{
    void Method()
    {
        int[] i = new int[] { 3 }$$;
    }
}", "i", "this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestPostfixUnaryExpressionSyntax()
        {
            TestTryDo(@"
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
        public void TestLabeledStatement()
        {
            TestTryDo(@"
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
        public void TestThrowStatement()
        {
            TestTryDo(@"
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
        public void TestDoStatement()
        {
            TestTryDo(@"
class Class 
{
    static void Method()
    {
        do$$ { } while (true);
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestLockStatement()
        {
            TestTryDo(@"
class Class 
{
    static void Method()
    {
        lock(typeof(Cl$$ass)) { };
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestWhileStatement()
        {
            TestTryDo(@"
class Class 
{
    static void Method()
    {
        while(DateTime.Now <$$ DateTime.Now) { };
    }
}", "DateTime", "DateTime.Now");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestForStatementWithDeclarators()
        {
            TestTryDo(@"
class Class 
{
    static void Method()
    {
        for(int i = 0; i < 10; i$$++) { }
    }
}", "i");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestForStatementWithInitializers()
        {
            TestTryDo(@"
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
        public void TestUsingStatement()
        {
            TestTryDo(@"
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
        public void TestValueInPropertySetter()
        {
            TestTryDo(@"
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
        public void TestValueInEventAdd()
        {
            TestTryDo(@"
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
        public void TestValueInEventRemove()
        {
            TestTryDo(@"
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
        public void TestValueInIndexerSetter()
        {
            TestTryDo(@"
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
        public void TestCatchBlock()
        {
            TestTryDo(@"
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
        public void TestCatchBlockEmpty_OpenBrace()
        {
            TestTryDo(@"
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
        public void TestCatchBlockEmpty_CloseBrace()
        {
            TestTryDo(@"
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
        public void TestObjectCreation()
        {
            TestTryDo(@"
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
        public void Test2()
        {
            TestIsValid(@"
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
        public void TestArrayCreation()
        {
            TestTryDo(@"
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
        public void Bug751141()
        {
            TestTryDo(@"
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
        public void ForLoopExpressionsInFirstStatementOfLoop1()
        {
            TestTryDo(@"class Program
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
        public void ForLoopExpressionsInFirstStatementOfLoop2()
        {
            TestTryDo(@"class Program
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
        public void ForLoopExpressionsInFirstStatementOfLoop3()
        {
            TestTryDo(@"class Program
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
        public void ForLoopExpressionsInFirstStatementOfLoop4()
        {
            TestTryDo(@"class Program
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
        public void ForEachLoopExpressionsInFirstStatementOfLoop1()
        {
            TestTryDo(@"class Program
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
        public void ForEachLoopExpressionsInFirstStatementOfLoop2()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterForLoop1()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterForLoop2()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterForEachLoop()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterNestedForLoop()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterCheckedStatement()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterUncheckedStatement()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterIfStatement()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterIfStatementWithElse()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterLockStatement()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterSwitchStatement()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterTryStatement()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterTryStatementWithFinally()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterUsingStatement()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsAfterWhileStatement()
        {
            TestTryDo(@"class Program
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
        public void ExpressionsInParenthesizedExpressions()
        {
            TestTryDo(@"class Program
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
