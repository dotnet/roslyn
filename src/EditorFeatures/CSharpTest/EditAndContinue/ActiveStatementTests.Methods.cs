// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class ActiveStatementTests_Methods : EditingTestBase
    {
        #region Methods

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/740443")]
        public void Method_Delete_Leaf1()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo(1);</AS:1>
    }

    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            var src2 = @"
<AS:0>class C</AS:0>
{
    static void Main(string[] args)
    {
        <AS:1>Goo(1);</AS:1>
    }
}
";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            EditAndContinueValidation.VerifySemantics(
                new[] { edits },
                new[]
                {
                    DocumentResults(
                        active,
                        diagnostics: new[] { Diagnostic(RudeEditKind.DeleteActiveStatement, "class C", DeletedSymbolDisplay(FeaturesResources.method, "Goo(int a)")) })
                });
        }

        [Fact]
        public void Method_Rename_Leaf1()
        {
            var src1 = @"
class C
{
    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Boo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}
";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            EditAndContinueValidation.VerifySemantics(
                new[] { edits },
                new[]
                {
                    DocumentResults(
                        active,
                        diagnostics: new[] {Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "static void Boo(int a)", FeaturesResources.method) })
                });
        }

        [Fact]
        public void Method_Body_Delete1()
        {
            var src1 = "class C { int M() { <AS:0>return 1;</AS:0> } }";
            var src2 = "class C { <AS:0>extern int M();</AS:0> }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ModifiersUpdate, "extern int M()", FeaturesResources.method));
        }

        [Fact]
        public void Method_ExpressionBody_Delete1()
        {
            var src1 = "class C { int M() => <AS:0>1</AS:0>; }";
            var src2 = "class C { <AS:0>extern int M();</AS:0> }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ModifiersUpdate, "extern int M()", FeaturesResources.method));
        }

        [Fact]
        public void Method_ExpressionBodyToBlockBody1()
        {
            var src1 = "class C { int M() => <AS:0>1</AS:0>; }";
            var src2 = "class C { int M() <AS:0>{</AS:0> return 1; } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Method_BlockBodyToExpressionBody1()
        {
            var src1 = "class C { int M() { <AS:0>return 1;</AS:0> } }";
            var src2 = "class C { int M() => <AS:0>1</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        // Generics
        [Fact]
        public void Update_Inner_GenericMethod()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        C c = new C();
        int a = 5;
        int b = 10;
        <AS:1>c.Swap(ref a, ref b);</AS:1>
    }

    void Swap<T>(ref T lhs, ref T rhs) where T : System.IComparable<T>
    {
        <AS:0>Console.WriteLine(""hello"");</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        while (true)
        {
            C c = new C();
            int a = 5;
            int b = 10;
            <AS:1>c.Swap(ref b, ref a);</AS:1>
        }
    }

    void Swap<T>(ref T lhs, ref T rhs) where T : System.IComparable<T>
    {
        <AS:0>Console.WriteLine(""hello"");</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "c.Swap(ref b, ref a);"));
        }

        [Fact]
        public void Update_Inner_ParameterType_GenericMethod()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Swap(5,6);</AS:1>
    }

    static void Swap<T>(T lhs, T rhs) where T : System.IComparable<T>
    {
        <AS:0>Console.WriteLine(""hello"");</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        while (true)
        {
            <AS:1>Swap(null, null);</AS:1>
        }
    }

    static void Swap<T>(T lhs, T rhs) where T : System.IComparable<T>
    {
        <AS:0>Console.WriteLine(""hello"");</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Swap(null, null);"));
        }

        [Fact]
        public void Update_Leaf_GenericMethod()
        {
            var src1 = @"
class C
{
    static void Main()
    {
        <AS:1>Swap(5,6);</AS:1>
    }

    static void Swap<T>(T lhs, T rhs) where T : System.IComparable<T>
    {
        <AS:0>Console.WriteLine(""hello"");</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main()
    {
        <AS:1>Swap(5,6);</AS:1>
    }

    static void Swap<T>(T lhs, T rhs) where T : System.IComparable<T>
    {
        <AS:0>Console.WriteLine(""hello world!"");</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(
                active,
                diagnostics: new[] { Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "static void Swap<T>(T lhs, T rhs)", GetResource("method")) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(
                active,
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Swap"), preserveLocalVariables: true) },
                capabilities: EditAndContinueCapabilities.GenericUpdateMethod);
        }

        // Async
        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/749458")]
        public void Update_Leaf_AsyncMethod()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        Test f = new Test();
        <AS:1>string result = f.WaitAsync().Result;</AS:1>
    }

    public async Task<string> WaitAsync()
    {
        <AS:0>await Task.Delay(1000);</AS:0>
        return ""Done"";
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        Test f = new Test();
        <AS:1>string result = f.WaitAsync().Result;</AS:1>
    }

    public async Task<string> WaitAsync()
    {
        <AS:0>await Task.Delay(100);</AS:0>
        return ""Done"";
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                capabilities: EditAndContinueCapabilities.AddInstanceFieldToExistingType);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/749440")]
        public void Update_Inner_AsyncMethod()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        Test f = new Test();
        <AS:1>string result = f.WaitAsync(5).Result;</AS:1>
    }

    public async Task<string> WaitAsync(int millis)
    {
        <AS:0>await Task.Delay(millis);</AS:0>
        return ""Done"";
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        Test f = new Test();
        <AS:1>string result = f.WaitAsync(6).Result;</AS:1>
    }

    public async Task<string> WaitAsync(int millis)
    {
        <AS:0>await Task.Delay(millis);</AS:0>
        return ""Done"";
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "string result = f.WaitAsync(6).Result;"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/749440")]
        public void Update_Initializer_MultipleVariables1()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        <AS:1>int a = F()</AS:1>, b = G();
    }

    public int F()
    {
        <AS:0>return 1;</AS:0>
    }

    public int G()
    {
        return 2;
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        <AS:1>int a = G()</AS:1>, b = F();
    }

    public int F()
    {
        <AS:0>return 1;</AS:0>
    }

    public int G()
    {
        return 2;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "int a = G()"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/749440")]
        public void Update_Initializer_MultipleVariables2()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        int a = F(), <AS:1>b = G()</AS:1>;
    }

    public int F()
    {
        <AS:0>return 1;</AS:0>
    }

    public int G()
    {
        return 2;
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        int a = G(), <AS:1>b = F()</AS:1>;
    }

    public int F()
    {
        <AS:0>return 1;</AS:0>
    }

    public int G()
    {
        return 2;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "b = F()"));
        }

        [Fact]
        public void MethodUpdateWithLocalVariables()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        int <N:0.0>a = 1</N:0.0>;
        int <N:0.1>b = 2</N:0.1>;
        <AS:0>System.Console.WriteLine(a + b);</AS:0>
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        int <N:0.1>b = 2</N:0.1>;
        int <N:0.0>a = 1</N:0.0>;
        <AS:0>System.Console.WriteLine(a + b);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);
            var syntaxMap = GetSyntaxMap(src1, src2);

            edits.VerifySemantics(
                active,
                new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Main"), syntaxMap[0]) });
        }

        #endregion

        #region Constuctors

        [Fact]
        public void Constructor_ExpressionBodyToBlockBody1()
        {
            var src1 = "class C { int x; C() => <AS:0>x = 1</AS:0>; }";
            var src2 = "class C { int x; <AS:0>C()</AS:0> { x = 1; } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Constructor_BlockBodyToExpressionBody1()
        {
            var src1 = "class C { int x; C() <AS:0>{</AS:0> x = 1; } }";
            var src2 = "class C { int x; C() => <AS:0>x = 1</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Constructor_BlockBodyToExpressionBody2()
        {
            var src1 = "class C { int x; <AS:0>C()</AS:0> { x = 1; } }";
            var src2 = "class C { int x; <AS:0>C()</AS:0> => x = 1; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Constructor_BlockBodyToExpressionBody3()
        {
            var src1 = "class C { int x; C() : <AS:0>base()</AS:0> { x = 1; } }";
            var src2 = "class C { int x; <AS:0>C()</AS:0> => x = 1; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region Properties

        [Fact]
        public void Property_ExpressionBodyToBlockBody1()
        {
            var src1 = "class C { int P => <AS:0>1</AS:0>; }";
            var src2 = "class C { int P { get <AS:0>{</AS:0> return 1; } } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Property_ExpressionBodyToBlockBody2()
        {
            var src1 = "class C { int P => <AS:0>1</AS:0>; }";
            var src2 = "class C { int P { get <AS:0>{</AS:0> return 1; } set { } } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(
                active,
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Property_ExpressionBodyToBlockBody3()
        {
            var src1 = "class C { int P => <AS:0>1</AS:0>; }";
            var src2 = "class C { int P { set { } get <AS:0>{</AS:0> return 1; } } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(
                active,
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Property_ExpressionBodyToBlockBody_NonLeaf()
        {
            var src1 = @"
class C 
{ 
    int P => <AS:1>M()</AS:1>; 
    int M() { <AS:0>return 1;</AS:0> } 
}
";
            var src2 = @"
class C 
{ 
    int P { get <AS:1>{</AS:1> return M(); } } 
    int M() { <AS:0>return 1;</AS:0> } 
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "get", FeaturesResources.code));
        }

        [Fact]
        public void Property_ExpressionBody_NonLeaf()
        {
            var src1 = @"
class C 
{ 
    int P => <AS:1>M()</AS:1>; 
    int M() { <AS:0>return 1;</AS:0> } 
}
";
            var src2 = @"
class C 
{ 
    int P => <AS:1>M()</AS:1>; 
    int M() { <AS:0>return 2;</AS:0> } 
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Property_BlockBodyToExpressionBody1()
        {
            var src1 = "class C { int P { get { <AS:0>return 1;</AS:0> } } }";
            var src2 = "class C { int P => <AS:0>1</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Property_BlockBodyToExpressionBody2()
        {
            var src1 = "class C { int P { set { } get { <AS:0>return 1;</AS:0> } } }";
            var src2 = "class C { int P => <AS:0>1</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_P")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_P"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Property_BlockBodyToExpressionBody_NonLeaf()
        {
            var src1 = @"
class C 
{ 
    int P { get { <AS:1>return M();</AS:1> } } 
    int M() { <AS:0>return 1;</AS:0> } 
}
";
            var src2 = @"
class C 
{ 
    int P => <AS:1>M()</AS:1>; 
    int M() { <AS:0>return 1;</AS:0> } 
}
";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // Can be improved with https://github.com/dotnet/roslyn/issues/22696
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "int P", FeaturesResources.code));
        }

        #endregion

        #region Indexers

        [Fact]
        public void Indexer_ExpressionBodyToBlockBody1()
        {
            var src1 = "class C { int this[int a] => <AS:0>1</AS:0>; }";
            var src2 = "class C { int this[int a] { get <AS:0>{</AS:0> return 1; } } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Indexer_ExpressionBodyToBlockBody2()
        {
            var src1 = "class C { int this[int a] => <AS:0>1</AS:0>; }";
            var src2 = "class C { int this[int a] { get <AS:0>{</AS:0> return 1; } set { } } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(
                active,
                capabilities: EditAndContinueCapabilities.AddMethodToExistingType);
        }

        [Fact]
        public void Indexer_BlockBodyToExpressionBody1()
        {
            var src1 = "class C { int this[int a] { get { <AS:0>return 1;</AS:0> } } }";
            var src2 = "class C { int this[int a] => <AS:0>1</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Indexer_BlockBodyToExpressionBody2()
        {
            var src1 = "class C { int this[int a] { get { <AS:0>return 1;</AS:0> } set { } } }";
            var src2 = "class C { int this[int a] => <AS:0>1</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemantics(
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item")),
                    SemanticEdit(SemanticEditKind.Delete, c => c.GetMember("C.set_Item"), deletedSymbolContainerProvider: c => c.GetMember("C")),
                },
                capabilities: EditAndContinueCapabilities.Baseline);
        }

        [Fact]
        public void Indexer_ExpressionBody_NonLeaf()
        {
            var src1 = @"
class C 
{ 
    int this[int index] => <AS:1>M()</AS:1>; 
    int M() { <AS:0>return 1;</AS:0> } 
}
";
            var src2 = @"
class C 
{ 
    int this[int index] => <AS:1>M()</AS:1>; 
    int M() { <AS:0>return 2;</AS:0> } 
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Update_Leaf_Indexers1()
        {
            var src1 = @"
class Test
{
    static void Main()
    {
        var c = new C<int>();
        <AS:1>c[0] = 1;</AS:1>
    }
}

class C<T>
{
    public T this[int i]
    {
        get => 0;
        set { <AS:0>value = i;</AS:0> }
    }
}";
            var src2 = @"
class Test
{
    static void Main()
    {
        var c = new C<int>();
        <AS:1>c[0] = 1;</AS:1>
    }
}

class C<T>
{
    public T this[int i]
    {
        get => 0;
        set { <AS:0>value = i + 1;</AS:0> }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                diagnostics: new[] { Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "set", GetResource("indexer setter")) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(active,
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item"), preserveLocalVariables: true) },
                capabilities: EditAndContinueCapabilities.GenericUpdateMethod);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/750244")]
        public void Update_Inner_Indexers_Setter()
        {
            var src1 = @"
using System;
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        <AS:1>stringCollection[0] = ""hello"";</AS:1>
        Console.WriteLine(stringCollection[0]);
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { return arr[i]; }
        set { <AS:0>arr[i] = value;</AS:0> }
    }
}";
            var src2 = @"
using System;
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        <AS:1>stringCollection[1] = ""hello"";</AS:1>
        Console.WriteLine(stringCollection[0]);
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { return arr[i]; }
        set { <AS:0>arr[i+1] = value;</AS:0> }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // Rude edits of active statements (AS:1) are not reported if the top-level edits are rude.
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "stringCollection[1] = \"hello\";"));
        }

        [Fact]
        public void Update_Leaf_Indexers_Getter()
        {
            var src1 = @"
class Test
{
    static void Main()
    {
        var c = new C<int>();
        <AS:1>Console.WriteLine(c[0]);</AS:1>
    }
}

class C<T>
{
    public T this[int i]
    {
        get { <AS:0>return 0;</AS:0> }
        set { }
    }
}";
            var src2 = @"
class Test
{
    static void Main()
    {
        var c = new C<int>();
        <AS:1>Console.WriteLine(c[0]);</AS:1>
    }
}

class C<T>
{
    public T this[int i]
    {
        get { <AS:0>return 1;</AS:0> }
        set { }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                diagnostics: new[] { Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "get", GetResource("indexer getter")) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(active,
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: true) },
                capabilities: EditAndContinueCapabilities.GenericUpdateMethod);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/750244")]
        public void Update_Inner_Indexers2()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[0] = ""hello"";
        <AS:1>Console.WriteLine(stringCollection[0]);</AS:1>
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { <AS:0>return arr[i];</AS:0> }
        set { arr[i] = value; }
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[0] = ""hello"";
        <AS:1>Console.WriteLine(stringCollection[1]);</AS:1>
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { <AS:0>return arr[0];</AS:0> }
        set { arr[i] = value; }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // Rude edits of active statements (AS:1) are not reported if the top-level edits are rude.
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Console.WriteLine(stringCollection[1]);"));
        }

        [Fact]
        public void Deleted_Leaf_Indexers_Setter()
        {
            var src1 = @"
class Test
{
    static void Main()
    {
        var c = new C<int>();
        <AS:1>c[0] = 1;</AS:1>
    }
}

class C<T>
{
    public T this[int i]
    {
        get => 0;
        set { <AS:0>throw null;</AS:0> }
    }
}";
            var src2 = @"
class Test
{
    static void Main()
    {
        var c = new C<int>();
        <AS:1>c[0] = 1;</AS:1>
    }
}

class C<T>
{
    public T this[int i]
    {
        get => 0;
        set { <AS:0>}</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                diagnostics: new[] { Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "set", GetResource("indexer setter")) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(active,
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.set_Item"), preserveLocalVariables: true) },
                capabilities: EditAndContinueCapabilities.GenericUpdateMethod);
        }

        [Fact]
        public void Deleted_Inner_Indexers_Setter()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        <AS:1>stringCollection[0] = ""hello"";</AS:1>
        Console.WriteLine(stringCollection[0]);
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { return arr[i]; }
        set { <AS:0>arr[i] = value;</AS:0> }
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        <AS:1>Console.WriteLine(stringCollection[0]);</AS:1>
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { return arr[i]; }
        set { <AS:0>arr[i] = value;</AS:0> }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{", FeaturesResources.code));
        }

        [Fact]
        public void Deleted_Leaf_Indexers_Getter()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        var c = new C<int>();
        <AS:1>Console.WriteLine(c[0]);</AS:1>
    }
}

class C<T>
{
    public T this[int i]
    {
        get { <AS:0>return 1;</AS:0> }
        set { }
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        var c = new C<int>();
        <AS:1>Console.WriteLine(c[0]);</AS:1>
    }
}

class C<T>
{
    public T this[int i]
    {
        get { <AS:0>}</AS:0>
        set { }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                diagnostics: new[] { Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "get", GetResource("indexer getter")) },
                capabilities: EditAndContinueCapabilities.Baseline);

            edits.VerifySemantics(active,
                semanticEdits: new[] { SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.get_Item"), preserveLocalVariables: true) },
                capabilities: EditAndContinueCapabilities.GenericUpdateMethod);
        }

        [Fact]
        public void Deleted_Inner_Indexers_Getter()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[0] = ""hello"";
        <AS:1>Console.WriteLine(stringCollection[0]);</AS:1>
    }
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { <AS:0>return arr[i];</AS:0> }
        set { arr[i] = value; }
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        SampleCollection<string> stringCollection = new SampleCollection<string>();
        stringCollection[0] = ""hello"";
    <AS:1>}</AS:1>
}

class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { <AS:0>return arr[i];</AS:0> }
        set { arr[i] = value; }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{", FeaturesResources.code));
        }

        #endregion

        #region Operators

        [Fact]
        public void Operator_ExpressionBodyToBlockBody1()
        {
            var src1 = "class C { public static C operator +(C t1, C t2) => <AS:0>null</AS:0>; }";
            var src2 = "class C { public static C operator +(C t1, C t2) <AS:0>{</AS:0> return null; } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Operator_ExpressionBodyToBlockBody2()
        {
            var src1 = "class C { public static explicit operator D(C t) => <AS:0>null</AS:0>; }";
            var src2 = "class C { public static explicit operator D(C t) <AS:0>{</AS:0> return null; } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Operator_BlockBodyToExpressionBody1()
        {
            var src1 = "class C { public static C operator +(C t1, C t2) { <AS:0>return null;</AS:0> } }";
            var src2 = "class C { public static C operator +(C t1, C t2) => <AS:0>null</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Operator_BlockBodyToExpressionBody2()
        {
            var src1 = "class C { public static explicit operator D(C t) { <AS:0>return null;</AS:0> } }";
            var src2 = "class C { public static explicit operator D(C t) => <AS:0>null</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754274")]
        public void Update_Leaf_OverloadedOperator()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        Test t1 = new Test(5);
        Test t2 = new Test(5);
        <AS:1>Test t3 = t1 + t2;</AS:1>
    }
    public static Test operator +(Test t1, Test t2)
    {
        <AS:0>return new Test(t1.a + t2.a);</AS:0>
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        Test t1 = new Test(5);
        Test t2 = new Test(5);
        <AS:1>Test t3 = t1 + t2;</AS:1>
    }
    public static Test operator +(Test t1, Test t2)
    {
        <AS:0>return new Test(t1.a + 2 * t2.a);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/754274")]
        public void Update_Inner_OverloadedOperator()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        Test t1 = new Test(5);
        Test t2 = new Test(5);
        <AS:1>Test t3 = t1 + t2;</AS:1>
    }
    public static Test operator +(Test t1, Test t2)
    {
        <AS:0>return new Test(t1.a + t2.a);</AS:0>
    }
    public static Test operator *(Test t1, Test t2)
    {
        return new Test(t1.a * t2.a);
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        Test t1 = new Test(5);
        Test t2 = new Test(5);
        <AS:1>Test t3 = t1 * t2;</AS:1>
    }
    public static Test operator +(Test t1, Test t2)
    {
        <AS:0>return new Test(t1.a + t2.a);</AS:0>
    }
    public static Test operator *(Test t1, Test t2)
    {
        return new Test(t1.a * t2.a);
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Test t3 = t1 * t2;"));
        }

        #endregion
    }
}
