// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class ActiveStatementTests_Methods : RudeEditTestBase
    {
        #region Methods

        [WorkItem(740443)]
        [WpfFact]
        public void Method_Delete_Leaf1()
        {
            string src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }

    static void Foo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            string src2 = @"
<AS:0>class C</AS:0>
{
    static void Main(string[] args)
    {
        <AS:1>Foo(1);</AS:1>
    }
}
";

            // TODO (bug 755959): better deleted active statement span
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.Delete, "class C", FeaturesResources.Method));
        }

        [WpfFact]
        public void Method_Body_Delete1()
        {
            var src1 = "class C { int M() { <AS:0>return 1;</AS:0> } }";
            var src2 = "class C { <AS:0>int M();</AS:0> }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.MethodBodyDelete, "int M()", FeaturesResources.Method));
        }

        [WpfFact]
        public void Method_ExpressionBody_Delete1()
        {
            var src1 = "class C { int M() => <AS:0>1</AS:0>; }";
            var src2 = "class C { <AS:0>int M();</AS:0> }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.MethodBodyDelete, "int M()", FeaturesResources.Method));
        }

        [WpfFact]
        public void Method_ExpressionBodyToBlockBody1()
        {
            var src1 = "class C { int M() => <AS:0>1</AS:0>; }";
            var src2 = "class C { int M() <AS:0>{</AS:0> return 1; } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WpfFact]
        public void Method_BlockBodyToExpressionBody1()
        {
            var src1 = "class C { int M() { <AS:0>return 1;</AS:0> } }";
            var src2 = "class C { int M() => <AS:0>1</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        // Generics
        [WpfFact]
        public void Update_Inner_GenericMethod()
        {
            string src1 = @"
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
            string src2 = @"
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

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "c.Swap(ref b, ref a);"));
        }

        [WpfFact]
        public void Update_Inner_ParameterType_GenericMethod()
        {
            string src1 = @"
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
            string src2 = @"
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

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Swap(null, null);"));
        }

        [WpfFact]
        public void Update_Leaf_GenericMethod()
        {
            string src1 = @"
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
            string src2 = @"
class C
{
    static void Main(string[] args)
    {
        while (true)
        {
            <AS:1>Swap(5,6);</AS:1>
        }
    }

    static void Swap<T>(T lhs, T rhs) where T : System.IComparable<T>
    {
        <AS:0>Console.WriteLine(""hello world!"");</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.GenericMethodUpdate, "static void Swap<T>(T lhs, T rhs)", FeaturesResources.Method));
        }

        // Async
        [WorkItem(749458)]
        [WpfFact]
        public void Update_Leaf_AsyncMethod()
        {
            string src1 = @"
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
            string src2 = @"
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

            edits.VerifyRudeDiagnostics(active);
        }

        [WorkItem(749440)]
        [WpfFact]
        public void Update_Inner_AsyncMethod()
        {
            string src1 = @"
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
            string src2 = @"
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

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "string result = f.WaitAsync(6).Result;"));
        }

        [WorkItem(749440)]
        [WpfFact]
        public void Update_Initializer_MultipleVariables1()
        {
            string src1 = @"
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
            string src2 = @"
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

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "int a = G()"));
        }

        [WorkItem(749440)]
        [WpfFact]
        public void Update_Initializer_MultipleVariables2()
        {
            string src1 = @"
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
            string src2 = @"
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

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "b = F()"));
        }

        [WpfFact]
        public void MethodUpdateWithLocalVariables()
        {
            string src1 = @"
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
            string src2 = @"
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

        #region Properties

        [WpfFact]
        public void Property_ExpressionBodyToBlockBody1()
        {
            var src1 = "class C { int P => <AS:0>1</AS:0>; }";
            var src2 = "class C { int P { get <AS:0>{</AS:0> return 1; } } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WpfFact]
        public void Property_ExpressionBodyToBlockBody2()
        {
            var src1 = "class C { int P => <AS:0>1</AS:0>; }";
            var src2 = "class C { int P { get <AS:0>{</AS:0> return 1; } set { } } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WpfFact]
        public void Property_ExpressionBodyToBlockBody3()
        {
            var src1 = "class C { int P => <AS:0>1</AS:0>; }";
            var src2 = "class C { int P { set { } get <AS:0>{</AS:0> return 1; } } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WpfFact]
        public void Property_ExpressionBodyToBlockBody_Internal()
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

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "get"));
        }

        [WpfFact]
        public void Property_BlockBodyToExpressionBody1()
        {
            var src1 = "class C { int P { get { <AS:0>return 1;</AS:0> } } }";
            var src2 = "class C { int P => <AS:0>1</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WpfFact]
        public void Property_BlockBodyToExpressionBody2()
        {
            var src1 = "class C { int P { set { } get { <AS:0>return 1;</AS:0> } } }";
            var src2 = "class C { int P => <AS:0>1</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.Delete, "int P", CSharpFeaturesResources.PropertySetter));
        }

        [WpfFact]
        public void Property_BlockBodyToExpressionBody_Internal()
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

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "=>       M()"));
        }

        #endregion

        #region Indexers

        [WpfFact]
        public void Indexer_ExpressionBodyToBlockBody1()
        {
            var src1 = "class C { int this[int a] => <AS:0>1</AS:0>; }";
            var src2 = "class C { int this[int a] { get <AS:0>{</AS:0> return 1; } } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WpfFact]
        public void Indexer_ExpressionBodyToBlockBody2()
        {
            var src1 = "class C { int this[int a] => <AS:0>1</AS:0>; }";
            var src2 = "class C { int this[int a] { get <AS:0>{</AS:0> return 1; } set { } } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WpfFact]
        public void Indexer_BlockBodyToExpressionBody1()
        {
            var src1 = "class C { int this[int a] { get { <AS:0>return 1;</AS:0> } } }";
            var src2 = "class C { int this[int a] => <AS:0>1</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WpfFact]
        public void Indexer_BlockBodyToExpressionBody2()
        {
            var src1 = "class C { int this[int a] { get { <AS:0>return 1;</AS:0> } set { } } }";
            var src2 = "class C { int this[int a] => <AS:0>1</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.Delete, "int this[int a]", CSharpFeaturesResources.IndexerSetter));
        }

        [WpfFact]
        public void Update_Leaf_Indexers1()
        {
            string src1 = @"
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
            string src2 = @"
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
        set { <AS:0>arr[i+1] = value;</AS:0> }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.GenericTypeUpdate, "set", CSharpFeaturesResources.IndexerSetter));
        }

        [WorkItem(750244)]
        [WpfFact]
        public void Update_Inner_Indexers1()
        {
            string src1 = @"
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
            string src2 = @"
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

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, @"stringCollection[1] = ""hello"";"),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "set", CSharpFeaturesResources.IndexerSetter));
        }

        [WpfFact]
        public void Update_Leaf_Indexers2()
        {
            string src1 = @"
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
            string src2 = @"
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
        get { <AS:0>return arr[0];</AS:0> }
        set { arr[i] = value; }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.GenericTypeUpdate, "get", CSharpFeaturesResources.IndexerGetter));
        }

        [WorkItem(750244)]
        [WpfFact]
        public void Update_Inner_Indexers2()
        {
            string src1 = @"
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
            string src2 = @"
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

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Console.WriteLine(stringCollection[1]);"),
                Diagnostic(RudeEditKind.GenericTypeUpdate, "get", CSharpFeaturesResources.IndexerGetter));
        }

        [WpfFact]
        public void Deleted_Leaf_Indexers1()
        {
            string src1 = @"
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
            string src2 = @"
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
        set { <AS:0>}</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.GenericTypeUpdate, "set", CSharpFeaturesResources.IndexerSetter));
        }

        [WpfFact]
        public void Deleted_Inner_Indexers1()
        {
            string src1 = @"
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
            string src2 = @"
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

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{"));
        }

        [WpfFact]
        public void Deleted_Leaf_Indexers2()
        {
            string src1 = @"
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
            string src2 = @"
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
        get { <AS:0>}</AS:0>
        set { arr[i] = value; }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.GenericTypeUpdate, "get", CSharpFeaturesResources.IndexerGetter));
        }

        [WpfFact]
        public void Deleted_Inner_Indexers2()
        {
            string src1 = @"
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
            string src2 = @"
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

            edits.VerifyRudeDiagnostics(active,
                                Diagnostic(RudeEditKind.DeleteActiveStatement, "{"));
        }

        #endregion

        #region Operators

        [WpfFact]
        public void Operator_ExpressionBodyToBlockBody1()
        {
            var src1 = "class C { public static C operator +(C t1, C t2) => <AS:0>null</AS:0>; }";
            var src2 = "class C { public static C operator +(C t1, C t2) <AS:0>{</AS:0> return null; } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WpfFact]
        public void Operator_ExpressionBodyToBlockBody2()
        {
            var src1 = "class C { public static explicit operator D(C t) => <AS:0>null</AS:0>; }";
            var src2 = "class C { public static explicit operator D(C t) <AS:0>{</AS:0> return null; } }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WpfFact]
        public void Operator_BlockBodyToExpressionBody1()
        {
            var src1 = "class C { public static C operator +(C t1, C t2) { <AS:0>return null;</AS:0> } }";
            var src2 = "class C { public static C operator +(C t1, C t2) => <AS:0>null</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WpfFact]
        public void Operator_BlockBodyToExpressionBody2()
        {
            var src1 = "class C { public static explicit operator D(C t) { <AS:0>return null;</AS:0> } }";
            var src2 = "class C { public static explicit operator D(C t) => <AS:0>null</AS:0>; }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifyRudeDiagnostics(active);
        }

        [WorkItem(754274)]
        [WpfFact]
        public void Update_Leaf_OverloadedOperator()
        {
            string src1 = @"
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
            string src2 = @"
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

            edits.VerifyRudeDiagnostics(active);
        }

        [WorkItem(754274)]
        [WpfFact]
        public void Update_Inner_OverloadedOperator()
        {
            string src1 = @"
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
            string src2 = @"
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

            edits.VerifyRudeDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Test t3 = t1 * t2;"));
        }

        #endregion
    }
}
