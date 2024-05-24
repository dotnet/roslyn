// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Debugging;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Debugging;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
public partial class ProximityExpressionsGetterTests
{
    private static string s_lazyTestFileContent;

    private static string GetTestFileContent()
    {
        if (s_lazyTestFileContent == null)
        {
            using var stream = typeof(ProximityExpressionsGetterTests).Assembly.GetManifestResourceStream("Debugging/ProximityExpressionsGetterTestFile.cs");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            s_lazyTestFileContent = reader.ReadToEnd();
        }

        return s_lazyTestFileContent;
    }

    private static SyntaxTree GetTree()
        => SyntaxFactory.ParseSyntaxTree(GetTestFileContent());

    private static SyntaxTree GetTreeFromCode(string code)
        => SyntaxFactory.ParseSyntaxTree(code);

    [Fact]
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
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 245, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.Equal(new[] { "yy", "xx" }, terms);
    }

    private static async Task TestProximityExpressionGetterAsync(
        string markup,
        Func<CSharpProximityExpressionsService, Document, int, Task> continuation)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(markup);
        var testDocument = workspace.Documents.Single();
        var caretPosition = testDocument.CursorPosition.Value;
        var snapshot = testDocument.GetTextBuffer().CurrentSnapshot;
        var languageDebugInfo = new CSharpLanguageDebugInfoService();
        var document = workspace.CurrentSolution.GetDocument(testDocument.Id);

        var proximityExpressionsGetter = new CSharpProximityExpressionsService();

        await continuation(proximityExpressionsGetter, document, caretPosition);
    }

    private static async Task TestTryDoInMainAsync(string body, bool topLevelStatement, params string[] expectedTerms)
    {
        string input;
        if (topLevelStatement)
        {
            input = body;
        }
        else
        {
            input = $@"class Program
{{
    static void Main(string[] args)
    {{
{string.Join(Environment.NewLine, body.ReplaceLineEndings("\n").Split('\n').Select(line => line == "" ? line : $"        {line}"))}
    }}
}}";
        }

        await TestTryDoAsync(input, expectedTerms);
    }

    private static async Task TestTryDoAsync(string input, params string[] expectedTerms)
    {
        await TestProximityExpressionGetterAsync(input, async (getter, document, position) =>
        {
            var actualTerms = await getter.GetProximityExpressionsAsync(document, position, CancellationToken.None);
            Assert.True(actualTerms is null or { Count: > 0 });
            AssertEx.Equal(expectedTerms, actualTerms ?? Array.Empty<string>());
        });
    }

    private static async Task TestIsValidAsync(string input, string expression, bool expectedValid)
    {
        await TestProximityExpressionGetterAsync(input, async (getter, semanticSnapshot, position) =>
        {
            var actualValid = await getter.IsValidAsync(semanticSnapshot, position, expression, CancellationToken.None);
            Assert.Equal(expectedValid, actualValid);
        });
    }

    [Fact]
    public async Task TestTryDo1()
        => await TestTryDoAsync("class Class { void Method() { string local;$$ } }", "local", "this");

    [Fact]
    public async Task TestNoParentToken()
        => await TestTryDoAsync("$$");

    [Fact]
    public async Task TestIsValid1()
        => await TestIsValidAsync("class Class { void Method() { string local;$$ } }", "local", true);

    [Fact]
    public async Task TestIsValidWithDiagnostics()
    {
        // local doesn't exist in this context
        await TestIsValidAsync("class Class { void Method() { string local; } $$}", "local", false);
    }

    [Fact]
    public async Task TestIsValidReferencingLocalBeforeDeclaration()
        => await TestIsValidAsync("class Class { void Method() { $$int i; int j; } }", "j", false);

    [Fact]
    public async Task TestIsValidReferencingUndefinedVariable()
        => await TestIsValidAsync("class Class { void Method() { $$int i; int j; } }", "k", false);

    [Fact]
    public async Task TestIsValidNoTypeSymbol()
        => await TestIsValidAsync("namespace Namespace$$ { }", "goo", false);

    [Fact]
    public async Task TestIsValidLocalAfterPosition()
        => await TestIsValidAsync("class Class { void Method() { $$ int i; string local; } }", "local", false);

    [Fact]
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

    [Theory, CombinatorialData]
    public async Task TestArrayCreationExpression(bool topLevelStatement)
    {
        if (!topLevelStatement)
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

        await TestTryDoInMainAsync(@"
int[] i = new int[] { 3 }$$;
", topLevelStatement, "i", "args");
    }

    [Theory, CombinatorialData]
    public async Task TestPostfixUnaryExpressionSyntax(bool topLevelStatement)
    {
        if (!topLevelStatement)
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

        await TestTryDoInMainAsync(@"int i = 3;
i++$$;
", topLevelStatement, "i");
    }

    [Theory, CombinatorialData]
    public async Task TestLabeledStatement(bool topLevelStatement)
    {
        if (!topLevelStatement)
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

        await TestTryDoInMainAsync(@"label: int i = 3;
label2$$: i++;
", topLevelStatement, "i");
    }

    [Theory, CombinatorialData]
    public async Task TestThrowStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"e = new Exception();
thr$$ow e;
", topLevelStatement, "e");
    }

    [Theory, CombinatorialData]
    public async Task TestDoStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"do$$ { } while (true);
", topLevelStatement, "args");
    }

    [Theory, CombinatorialData]
    public async Task TestLockStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"lock(typeof(Cl$$ass)) { };
", topLevelStatement, "args");
    }

    [Theory, CombinatorialData]
    public async Task TestWhileStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"while(DateTime.Now <$$ DateTime.Now) { };
", topLevelStatement, "DateTime", "DateTime.Now", "args");
    }

    [Theory, CombinatorialData]
    public async Task TestForStatementWithDeclarators(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"for(int i = 0; i < 10; i$$++) { }
", topLevelStatement, "i", "args");
    }

    [Theory, CombinatorialData]
    public async Task TestForStatementWithInitializers(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int i = 0;
for(i = 1; i < 10; i$$++) { }
", topLevelStatement, "i");
    }

    [Theory, CombinatorialData]
    public async Task TestUsingStatement(bool topLevelStatement)
    {
        if (!topLevelStatement)
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

        await TestTryDoInMainAsync(@"using (FileStream fs = new FileStream($$)) { }
", topLevelStatement, "args");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538879")]
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48504")]
    public async Task TestValueInPropertyInit()
    {
        await TestTryDoAsync(@"
class Class
{
    string Name
    {
        get { return """"; }
        init { $$ }
    }
}", "this", "value");
    }

    [Fact]
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

    [Fact]
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538880")]
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

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538881")]
    public async Task TestCatchBlock(bool topLevelStatement)
    {
        if (!topLevelStatement)
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

        await TestTryDoInMainAsync(@"try { }
catch(Exception ex) { int $$ }
", topLevelStatement, "ex");
    }

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538881")]
    public async Task TestCatchBlockEmpty_OpenBrace(bool topLevelStatement)
    {
        if (!topLevelStatement)
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

        await TestTryDoInMainAsync(@"try { }
catch(Exception ex) { $$ }
", topLevelStatement, "ex");
    }

    [Theory, CombinatorialData]
    public async Task TestCatchBlockEmpty_CloseBrace(bool topLevelStatement)
    {
        if (!topLevelStatement)
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

        await TestTryDoInMainAsync(@"try { }
catch(Exception ex) { } $$ 
", topLevelStatement);
    }

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538874")]
    public async Task TestObjectCreation(bool topLevelStatement)
    {
        if (!topLevelStatement)
        {
            await TestTryDoAsync(@"
class Class 
{
    void Method()
    {
        $$Goo(new Bar(a).Baz);
    }
}", "a", "new Bar(a).Baz", "Goo", "this");
        }

        await TestTryDoInMainAsync(@"$$Goo(new Bar(a).Baz);
", topLevelStatement, "a", "new Bar(a).Baz", "Goo", "args");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538874")]
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
        $$Goo(D.x);
    }
}", "D.x", false);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538890")]
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/751141")]
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

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ForLoopExpressionsInFirstStatementOfLoop1(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"for(int i = 0; i < 5; i++)
{
    $$var x = 8;
}
", topLevelStatement, "i", "x");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ForLoopExpressionsInFirstStatementOfLoop2(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int i = 0, j = 0, k = 0, m = 0, n = 0;

for(i = 0; j < 5; k++)
{
    $$m = 8;
    n = 7;
}
", topLevelStatement, "m", "i", "j", "k");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ForLoopExpressionsInFirstStatementOfLoop3(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int i = 0, j = 0, k = 0, m = 0;

for(i = 0; j < 5; k++)
{
    var m = 8;
    $$var n = 7;
}
", topLevelStatement, "m", "n");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ForLoopExpressionsInFirstStatementOfLoop4(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int i = 0, j = 0, k = 0, m = 0;

for(i = 0; j < 5; k++)
    $$m = 8;
", topLevelStatement, "m", "i", "j", "k");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ForEachLoopExpressionsInFirstStatementOfLoop1(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"foreach (var x in new int[] { 1, 2, 3 })
{
    $$var z = 0;
}
", topLevelStatement, "x", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ForEachLoopExpressionsInFirstStatementOfLoop2(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"foreach (var x in new int[] { 1, 2, 3 })
    $$var z = 0;
", topLevelStatement, "x", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterForLoop1(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0;

for (a = 5; b < 1; b++)
{
    c = 8;
    d = 9; // included
}
        
$$var z = 0;
", topLevelStatement, "a", "b", "d", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterForLoop2(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0;

for (a = 5; b < 1; b++)
{
    c = 8;
    int d = 9; // not included
}
        
$$var z = 0;
", topLevelStatement, "a", "b", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterForEachLoop(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0;

foreach (var q in new int[] {1, 2, 3})
{
    c = 8;
    d = 9; // included
}
        
$$var z = 0;
", topLevelStatement, "q", "d", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterNestedForLoop(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;

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
", topLevelStatement, "a", "b", "f", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterCheckedStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;

checked
{
    a = 7;
    b = 0; // included
}
        
$$var z = 0;
", topLevelStatement, "b", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterUncheckedStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;

unchecked
{
    a = 7;
    b = 0; // included
}
        
$$var z = 0;
", topLevelStatement, "b", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterIfStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;

if (a == 0)
{
    c = 8; 
    d = 9; // included
}

$$var z = 0;
", topLevelStatement, "a", "d", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterIfStatementWithElse(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;

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
", topLevelStatement, "a", "d", "f", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterLockStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0;

lock (new object())
{
    a = 2;
    b = 3; // included
}

$$var z = 0;
", topLevelStatement, "b", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterSwitchStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0, g = 0;

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
", topLevelStatement, "a", "c", "e", "g", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterTryStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0, g = 0;

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
", topLevelStatement, "b", "d", "f", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterTryStatementWithFinally(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0, g = 0;

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
", topLevelStatement, "g", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterUsingStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0, g = 0;

using (null as System.IDisposable)
{
    a = 4;
    b = 8; // Included
}

$$var z = 0;
", topLevelStatement, "b", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775161"), CombinatorialData]
    public async Task ExpressionsAfterWhileStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 0, b = 0, c = 0, d = 0, e = 0, f = 0, g = 0;

while (a < 5)
{
    a++;
    b = 8; // Included
}

$$var z = 0;
", topLevelStatement, "a", "b", "z");
    }

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/778215"), CombinatorialData]
    public async Task ExpressionsInParenthesizedExpressions(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int i = 0, j = 0, k = 0, m = 0;
int flags = 7;

if((flags & i) == k)
{
    $$ m = 8;
}
", topLevelStatement, "m", "flags", "i", "k");
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/58337"), CombinatorialData]
    public async Task ExpressionsInTopLevelStatement(bool topLevelStatement)
    {
        await TestTryDoInMainAsync(@"int a = 1;
int b = 2;
$$ Console.WriteLine(""Hello, World!"");
", topLevelStatement, "Console", "b");
    }
}
