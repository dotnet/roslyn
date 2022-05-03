// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class ActiveStatementTests : EditingTestBase
    {
        #region Update

        [Fact]
        public void Update_Inner()
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
class C
{
    static void Main(string[] args)
    {
        while (true)
        {
            <AS:1>Goo(2);</AS:1>
        }
    }

    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Goo(2);"));
        }

        [Fact]
        public void Update_Inner_NewCommentAtEndOfActiveStatement()
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
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo(1);</AS:1>//
    }

    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        /// <summary>
        /// CreateNewOnMetadataUpdate has no effect in presence of active statements (in break mode).
        /// </summary>
        [Fact]
        public void Update_Inner_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
class C
{
    static void Main()
    {
        <AS:1>Goo(1);</AS:1>
    }

    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            var src2 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
class C
{
    static void Main()
    {
        while (true)
        {
            <AS:1>Goo(2);</AS:1>
        }
    }

    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Goo(2);"));
        }

        [Fact]
        public void Update_Leaf()
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
class C
{
    static void Main(string[] args)
    {
        while (true)
        {
            <AS:1>Goo(1);</AS:1>
        }
    }

    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a + 1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Update_Leaf_NewCommentAtEndOfActiveStatement()
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
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo(1);</AS:1>
    }

    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>//
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        /// <summary>
        /// CreateNewOnMetadataUpdate has no effect in presence of active statements (in break mode).
        /// </summary>
        [Fact]
        public void Update_Leaf_Reloadable()
        {
            var src1 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
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
            var src2 = ReloadableAttributeSrc + @"
[CreateNewOnMetadataUpdate]
class C
{
    static void Main(string[] args)
    {
        while (true)
        {
            <AS:1>Goo(1);</AS:1>
        }
    }

    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a + 1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemantics(active,
                semanticEdits: new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Main"), preserveLocalVariables: true),
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("C.Goo"), preserveLocalVariables: true)
                });
        }

        [WorkItem(846588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/846588")]
        [Fact]
        public void Update_Leaf_Block()
        {
            var src1 = @"
class C : System.IDisposable
{
    public void Dispose() {}

    static void Main(string[] args)
    {
        using (<AS:0>C x = null</AS:0>) {}
    }
}";
            var src2 = @"
class C : System.IDisposable
{
    public void Dispose() {}

    static void Main(string[] args)
    {
        using (<AS:0>C x = new C()</AS:0>) {}
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region Delete in Method Body

        [Fact]
        public void Delete_Inner()
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
class C
{
    static void Main(string[] args)
    {
        while (true)
        {
        }
    <AS:1>}</AS:1>

    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{", FeaturesResources.code));
        }

        // TODO (tomat): considering a change
        [Fact]
        public void Delete_Inner_MultipleParents()
        {
            var src1 = @"
class C : IDisposable
{
    unsafe static void Main(string[] args)
    {
        {
            <AS:1>Goo(1);</AS:1>
        }

        if (true)
        {
            <AS:2>Goo(2);</AS:2>
        }
        else
        {
            <AS:3>Goo(3);</AS:3>
        }

        int x = 1;
        switch (x)
        {
            case 1:
            case 2:
                <AS:4>Goo(4);</AS:4>
                break;

            default:
                <AS:5>Goo(5);</AS:5>
                break;
        }

        checked
        {
            <AS:6>Goo(4);</AS:6>
        }

        unchecked
        {
            <AS:7>Goo(7);</AS:7>
        }

        while (true) <AS:8>Goo(8);</AS:8>
    
        do <AS:9>Goo(9);</AS:9> while (true);

        for (int i = 0; i < 10; i++) <AS:10>Goo(10);</AS:10>

        foreach (var i in new[] { 1, 2}) <AS:11>Goo(11);</AS:11>

        using (var z = new C()) <AS:12>Goo(12);</AS:12>

        fixed (char* p = ""s"") <AS:13>Goo(13);</AS:13>

        label: <AS:14>Goo(14);</AS:14>
    }

    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}";
            var src2 = @"
class C : IDisposable
{
    unsafe static void Main(string[] args)
    {
        {
        <AS:1>}</AS:1>

        if (true)
        { <AS:2>}</AS:2>
        else
        { <AS:3>}</AS:3>

        int x = 1;
        switch (x)
        {
            case 1:
            case 2:
                <AS:4>break;</AS:4>

            default:
                <AS:5>break;</AS:5>
        }

        checked
        {
        <AS:6>}</AS:6>

        unchecked
        {
        <AS:7>}</AS:7>

        <AS:8>while (true)</AS:8> { }
    
        do { } <AS:9>while (true);</AS:9>

        for (int i = 0; i < 10; <AS:10>i++</AS:10>) { }

        foreach (var i <AS:11>in</AS:11> new[] { 1, 2 }) { }

        using (<AS:12>var z = new C()</AS:12>) { }

        fixed (<AS:13>char* p = ""s""</AS:13>) { }

        label: <AS:14>{</AS:14> }
    }

    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "case 2:", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "default:", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "while (true)", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "do", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "for (int i = 0; i < 10;        i++        )", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "foreach (var i        in         new[] { 1, 2 })", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "using (       var z = new C()        )", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "fixed (       char* p = \"s\"        )", FeaturesResources.code),
                Diagnostic(RudeEditKind.DeleteActiveStatement, "label", FeaturesResources.code));
        }

        [Fact]
        public void Delete_Leaf1()
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
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo(1);</AS:1>
    }

    static void Goo(int a)
    {
    <AS:0>}</AS:0>
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Delete_Leaf2()
        {
            var src1 = @"
class C
{
    static void Goo(int a)
    {
        Console.WriteLine(1);
        Console.WriteLine(2);
        <AS:0>Console.WriteLine(3);</AS:0>
        Console.WriteLine(4);
    }
}";
            var src2 = @"
class C
{
    static void Goo(int a)
    {
        Console.WriteLine(1);
        Console.WriteLine(2);

        <AS:0>Console.WriteLine(4);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Delete_Leaf_InTry()
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
        try
        {
            <AS:0>Console.WriteLine(a);</AS:0>
        }
        catch
        {
        }
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo(1);</AS:1>
    }

    static void Goo(int a)
    {
        try
        {
        <AS:0>}</AS:0>
        catch
        {
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Delete_Leaf_InTry2()
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
        try
        {
            try
            {
                <AS:0>Console.WriteLine(a);</AS:0>
            }
            catch
            {
            }
        }
        catch
        {
        }
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo(1);</AS:1>
    }

    static void Goo(int a)
    {
        try
        {
            try
            {
            <AS:0>}</AS:0>
            catch
            {
            }
        }
        catch
        {
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Delete_Inner_CommentActiveStatement()
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
class C
{
    static void Main(string[] args)
    {
        //Goo(1);
    <AS:1>}</AS:1>

    static void Goo(int a)
    {
        <AS:0>Console.WriteLine(a);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{", FeaturesResources.code));
        }

        [WorkItem(755959, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755959")]
        [Fact]
        public void Delete_Leaf_CommentActiveStatement()
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
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo(1);</AS:1>
    }

    static void Goo(int a)
    {
        //Console.WriteLine(a);
    <AS:0>}</AS:0>
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Delete_EntireNamespace()
        {
            var src1 = @"
namespace N
{
    class C
    {
        static void Main(String[] args)
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}";
            var src2 = @"<AS:0></AS:0>";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.Delete, null, FeaturesResources.namespace_),
                Diagnostic(RudeEditKind.Delete, null, DeletedSymbolDisplay(FeaturesResources.class_, "N.C")));
        }

        #endregion

        #region Constructors

        [WorkItem(740949, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/740949")]
        [Fact]
        public void Updated_Inner_Constructor()
        {
            var src1 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Goo f = new Goo(5);</AS:1>
    }
}

class Goo
{
    int value;
    public Goo(int a)
    {
        <AS:0>this.value = a;</AS:0>
    }
}";
            var src2 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Goo f = new Goo(5*2);</AS:1>
    }
}

class Goo
{
    int value;
    public Goo(int a)
    {
        <AS:0>this.value = a;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Goo f = new Goo(5*2);"));
        }

        [WorkItem(741249, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/741249")]
        [Fact]
        public void Updated_Leaf_Constructor()
        {
            var src1 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Goo f = new Goo(5);</AS:1>
    }
}

class Goo
{
    int value;
    public Goo(int a)
    {
        <AS:0>this.value = a;</AS:0>
    }
}";
            var src2 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Goo f = new Goo(5);</AS:1>
    }
}

class Goo
{
    int value;
    public Goo(int a)
    {
        <AS:0>this.value = a*2;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [WorkItem(742334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/742334")]
        [Fact]
        public void Updated_Leaf_Constructor_Parameter()
        {
            var src1 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Goo f = new Goo(5);</AS:1>
    }
}

class Goo
{
    int value;
    <AS:0>public Goo(int a)</AS:0>
    {
        this.value = a;
    }
}";
            var src2 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Goo f = new Goo(5);</AS:1>
    }
}

class Goo
{
    int value;
    <AS:0>public Goo(int b)</AS:0>
    {
        this.value = b;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemantics(
                ActiveStatementsDescription.Empty,
                new[]
                {
                    SemanticEdit(SemanticEditKind.Update, c => c.GetMember("Goo..ctor"))
                },
                capabilities: EditAndContinueTestHelpers.Net6RuntimeCapabilities);
        }

        [WorkItem(742334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/742334")]
        [Fact]
        public void Updated_Leaf_Constructor_Parameter_DefaultValue()
        {
            var src1 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Goo f = new Goo(5);</AS:1>
    }
}

class Goo
{
    int value;
    <AS:0>public Goo(int a = 5)</AS:0>
    {
        this.value = a;
    }
}";
            var src2 = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        <AS:1>Goo f = new Goo(5);</AS:1>
    }
}

class Goo
{
    int value;
    <AS:0>public Goo(int a = 42)</AS:0>
    {
        this.value = a;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InitializerUpdate, "int a = 42", FeaturesResources.parameter));
        }

        [WorkItem(742334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/742334")]
        [Fact]
        public void Updated_Leaf_ConstructorChaining1()
        {
            var src1 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
       <AS:1>B b = new B(2, 3);</AS:1>
    }
}
class B : A
{
    public B(int x, int y) : <AS:0>base(x + y, x - y)</AS:0> { }
}
class A
{
    public A(int x, int y) : this(5 + x, y, 0) { }

    public A(int x, int y, int z) { }
}";
            var src2 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
       <AS:1>B b = new B(2, 3);</AS:1>
    }
}
class B : A
{
    public B(int x, int y) : <AS:0>base(x + y + 5, x - y)</AS:0> { }
}
class A
{
    public A(int x, int y) : this(x, y, 0) { }

    public A(int x, int y, int z) { }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [WorkItem(742334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/742334")]
        [Fact]
        public void Updated_Leaf_ConstructorChaining2()
        {
            var src1 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
       <AS:2>B b = new B(2, 3);</AS:2>
    }
}
class B : A
{
    public B(int x, int y) : <AS:1>base(x + y, x - y)</AS:1> { }
}
class A
{
    public A(int x, int y) : <AS:0>this(x, y, 0)</AS:0> { }

    public A(int x, int y, int z) { }
}";
            var src2 = @"
using System;

class Test
{
    static void Main(string[] args)
    {
       <AS:2>B b = new B(2, 3);</AS:2>
    }
}
class B : A
{
    public B(int x, int y) : <AS:1>base(x + y, x - y)</AS:1> { }
}
class A
{
    public A(int x, int y) : <AS:0>this(5 + x, y, 0)</AS:0> { }

    public A(int x, int y, int z) { }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [WorkItem(742334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/742334")]
        [Fact]
        public void InstanceConstructorWithoutInitializer()
        {
            var src1 = @"
class C
{
    int a = 5;

    <AS:0>public C(int a)</AS:0> { }

    static void Main(string[] args)
    {
        <AS:1>C c = new C(3);</AS:1>
    }
}";
            var src2 = @"
class C
{
    int a = 42;

    <AS:0>public C(int a)</AS:0> { }

    static void Main(string[] args)
    {
        <AS:1>C c = new C(3);</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstanceConstructorWithInitializer_Internal_Update1()
        {
            var src1 = @"
class D
{
    public D(int d) {}
}

class C : D
{
    int a = 5;

    public C(int a) : <AS:2>this(true)</AS:2> { }

    public C(bool b) : <AS:1>base(F())</AS:1> {}

    static int F()
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main(string[] args)
    {
        <AS:3>C c = new C(3);</AS:3>
    }
}";
            var src2 = @"
class D
{
    public D(int d) {}
}

class C : D
{
    int a = 5;

    public C(int a) : <AS:2>this(false)</AS:2> { }

    public C(bool b) : <AS:1>base(F())</AS:1> {}

    static int F()
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main(string[] args)
    {
        <AS:3>C c = new C(3);</AS:3>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "this(false)"));
        }

        [Fact]
        public void InstanceConstructorWithInitializer_Internal_Update2()
        {
            var src1 = @"
class D
{
    public D(int d) {}
}

class C : D
{
    public C() : <AS:1>base(F())</AS:1> {}

    static int F()
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var src2 = @"
class D
{
    public D(int d) {}
}

class C : D
{
    <AS:1>public C()</AS:1> {}

    static int F()
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "public C()"));
        }

        [Fact]
        public void InstanceConstructorWithInitializer_Internal_Update3()
        {
            var src1 = @"
class D
{
    public D(int d) <AS:0>{</AS:0> }
}

class C : D
{
    <AS:1>public C()</AS:1> {}
}";
            var src2 = @"
class D
{
    public D(int d) <AS:0>{</AS:0> }
}

class C : D
{
    public C() : <AS:1>base(1)</AS:1> {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "base(1)"));
        }

        [Fact]
        public void InstanceConstructorWithInitializer_Leaf_Update1()
        {
            var src1 = @"
class D
{
    public D(int d) { }
}

class C : D
{
    public C() : <AS:0>base(1)</AS:0> {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var src2 = @"
class D
{
    public D(int d) { }
}

class C : D
{
    public C() : <AS:0>base(2)</AS:0> {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstanceConstructorWithInitializer_Leaf_Update2()
        {
            var src1 = @"
class D
{
    public D() { }
    public D(int d) { }
}

class C : D
{
    public C() : <AS:0>base(2)</AS:0> {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var src2 = @"
class D
{
    public D() { }
    public D(int d) { }
}

class C : D
{
    <AS:0>public C()</AS:0> {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstanceConstructorWithInitializer_Leaf_Update3()
        {
            var src1 = @"
class D
{
    public D() { }
    public D(int d) { }
}

class C : D
{
    <AS:0>public C()</AS:0> {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var src2 = @"
class D
{
    public D() { }
    public D(int d) { }
}

class C : D
{
    public C() : <AS:0>base(2)</AS:0> {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstanceConstructorWithInitializerWithLambda_Update1()
        {
            var src1 = @"
class C
{
    public C() : this((a, b) => { <AS:0>Console.WriteLine(a + b);</AS:0> }) { }
}";
            var src2 = @"
class C
{
    public C() : base((a, b) => { <AS:0>Console.WriteLine(a - b);</AS:0> }) { }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstanceConstructorWithInitializerWithLambda_Update2()
        {
            var src1 = @"
class C
{
    public C() : <AS:1>this((a, b) => { <AS:0>Console.WriteLine(a + b);</AS:0> })</AS:1> { Console.WriteLine(1); }
}";
            var src2 = @"
class C
{
    public C() : <AS:1>this((a, b) => { <AS:0>Console.WriteLine(a + b);</AS:0> })</AS:1> { Console.WriteLine(2); }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstanceConstructorWithInitializerWithLambda_Update3()
        {
            var src1 = @"
class C
{
    public C() : <AS:1>this((a, b) => { <AS:0>Console.WriteLine(a + b);</AS:0> })</AS:1> { Console.WriteLine(1); }
}";
            var src2 = @"
class C
{
    public C() : <AS:1>this((a, b) => { <AS:0>Console.WriteLine(a - b);</AS:0> })</AS:1> { Console.WriteLine(1); }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void InstanceConstructor_DeleteParameterless(string typeKind)
        {
            var src1 = "partial " + typeKind + " C { public C() { <AS:0>System.Console.WriteLine(1);</AS:0> } }";
            var src2 = "<AS:0>partial " + typeKind + " C</AS:0> { }";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "partial " + typeKind + " C", DeletedSymbolDisplay(FeaturesResources.constructor, "C()")));
        }

        #endregion

        #region Field and Property Initializers

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void InstancePropertyInitializer_Leaf_Update(string typeKind)
        {
            var src1 = @"
" + typeKind + @" C
{
    int a { get; } = <AS:0>1</AS:0>;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var src2 = @"
" + typeKind + @" C
{
    int a { get; } = <AS:0>2</AS:0>;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void InstancePropertyInitializer_Leaf_Update_SynthesizedConstructor(string typeKind)
        {
            var src1 = @"
" + typeKind + @" C
{
    int a { get; } = <AS:0>1</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var src2 = @"
" + typeKind + @" C
{
    int a { get; } = <AS:0>2</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [WorkItem(742334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/742334")]

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void InstanceFieldInitializer_Leaf_Update1(string typeKind)
        {
            var src1 = @"
" + typeKind + @" C
{
    <AS:0>int a = 1</AS:0>, b = 2;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var src2 = @"
" + typeKind + @" C
{
    <AS:0>int a = 2</AS:0>, b = 2;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Theory]
        [InlineData("class ")]
        [InlineData("struct")]
        public void InstanceFieldInitializer_Leaf_Update1_SynthesizedConstructor(string typeKind)
        {
            var src1 = @"
" + typeKind + @" C
{
    <AS:0>int a = 1</AS:0>, b = 2;

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var src2 = @"
" + typeKind + @" C
{
    <AS:0>int a = 2</AS:0>, b = 2;

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstanceFieldInitializer_Internal_Update1()
        {
            var src1 = @"
class C
{
    <AS:1>int a = F(1)</AS:1>, b = F(2);

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var src2 = @"
class C
{
    <AS:1>int a = F(2)</AS:1>, b = F(2);

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "int a = F(2)"));
        }

        [Fact]
        public void InstanceFieldInitializer_Internal_Update2()
        {
            var src1 = @"
class C
{
    int a = F(1), <AS:1>b = F(2)</AS:1>;

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var src2 = @"
class C
{
    int a = F(1), <AS:1>b = F(3)</AS:1>;

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "b = F(3)"));
        }

        [Fact]
        public void InstancePropertyInitializer_Internal_Delete1()
        {
            var src1 = @"
class C
{
    int a { get; } = <AS:0>1</AS:0>;
    int b { get; } = 2;
}";
            var src2 = @"
class C
{
    int a { get { return 1; } }
    int b { get; } = <AS:0>2</AS:0>;
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstancePropertyInitializer_Internal_Delete2()
        {
            var src1 = @"
class C
{
    int a { get; } = <AS:0>1</AS:0>;
    static int s { get; } = 2;
    int b { get; } = 2;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var src2 = @"
class C
{
    int a { get; }
    static int s { get; } = 2;
    int b { get; } = <AS:0>3</AS:0>;

    public C() { }

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstanceFieldInitializer_Internal_Delete1()
        {
            var src1 = @"
class C
{
    <AS:1>int a = F(1)</AS:1>, b = F(2);

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var src2 = @"
class C
{
    int a, <AS:1>b = F(2)</AS:1>;

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstanceFieldInitializer_Internal_Delete2()
        {
            var src1 = @"
class C
{
    int a = F(1), <AS:1>b = F(2)</AS:1>;

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var src2 = @"
class C
{
    <AS:1>int a, b;</AS:1>

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstancePropertyAndFieldInitializers_Delete1()
        {
            var src1 = @"
class C
{
    int a { get; } = <AS:0>1</AS:0>;
    static int s { get; } = 2;
    int b = 2;
}";
            var src2 = @"
class C
{
    int a { get; }
    static int s { get; } = 2;
    <AS:0>int b = 3;</AS:0>
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstancePropertyAndFieldInitializers_Delete2()
        {
            var src1 = @"
class C
{
    int a = <AS:0>1</AS:0>;
    static int s { get; } = 2;
    int b { get; } = 2;

    public C() {}

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var src2 = @"
class C
{
    int a;
    static int s { get; } = 2;
    int b { get; } = <AS:0>3</AS:0>;

    public C() { }

    static void Main(string[] args)
    {
        <AS:1>C c = new C();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void InstanceFieldInitializer_SingleDeclarator()
        {
            var src1 = @"
class C
{
    <AS:1>public static readonly int a = F(1);</AS:1>

    public C() {}

    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
    }

    static void Main(string[] args)
    {
        <AS:2>C c = new C();</AS:2>
    }
}";
            var src2 = @"
class C
{
    <AS:1>public static readonly int <TS:1>a = F(1)</TS:1>;</AS:1>

    public C() {}

    public static int F(int a)
    {
        <TS:0><AS:0>return a + 1;</AS:0></TS:0>
    }

    static void Main(string[] args)
    {
        <TS:2><AS:2>C c = new C();</AS:2></TS:2>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_Lambda1()
        {
            var src1 = @"
class C
{
    Func<int, int> a = z => <AS:0>z + 1</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C().a(1);</AS:1>
    }
}";
            var src2 = @"
class C
{
    Func<int, int> a = F(z => <AS:0>z + 1</AS:0>);

    static void Main(string[] args)
    {
        <AS:1>new C().a(1);</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void PropertyInitializer_Lambda1()
        {
            var src1 = @"
class C
{
    Func<int, int> a { get; } = z => <AS:0>z + 1</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C().a(1);</AS:1>
    }
}";
            var src2 = @"
class C
{
    Func<int, int> a { get; } = F(z => <AS:0>z + 1</AS:0>);

    static void Main(string[] args)
    {
        <AS:1>new C().a(1);</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_Lambda2()
        {
            var src1 = @"
class C
{
    Func<int, Func<int>> a = z => () => <AS:0>z + 1</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C().a(1)();</AS:1>
    }
}";
            var src2 = @"
class C
{
    Func<int, Func<int>> a = z => () => <AS:0>z + 2</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C().a(1)();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void PropertyInitializer_Lambda2()
        {
            var src1 = @"
class C
{
    Func<int, Func<int>> a { get; } = z => () => <AS:0>z + 1</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C().a(1)();</AS:1>
    }
}";
            var src2 = @"
class C
{
    Func<int, Func<int>> a { get; } = z => () => <AS:0>z + 2</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C().a(1)();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_InsertConst1()
        {
            var src1 = @"
class C
{
    <AS:0>int a = 1</AS:0>;

    public C() {}
}";
            var src2 = @"
class C
{
    <AS:0>const int a = 1;</AS:0>

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);

            edits.VerifyEdits(
                "Update [int a = 1       ;]@24 -> [const int a = 1;]@24");

            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ModifiersUpdate, "const int a = 1", FeaturesResources.const_field));
        }

        [Fact]
        public void LocalInitializer_InsertConst1()
        {
            var src1 = @"
class C
{
    public void M()
    {
        <AS:0>int a = 1</AS:0>;
    }
}";
            var src2 = @"
class C
{
    public void M()
    {
        const int a = 1;
    <AS:0>}</AS:0>
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_InsertConst2()
        {
            var src1 = @"
class C
{
    int <AS:0>a = 1</AS:0>, b = 2;

    public C() {}
}";
            var src2 = @"
class C
{
    <AS:0>const int a = 1, b = 2;</AS:0>

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);

            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ModifiersUpdate, "const int a = 1, b = 2", FeaturesResources.const_field),
                Diagnostic(RudeEditKind.ModifiersUpdate, "const int a = 1, b = 2", FeaturesResources.const_field));
        }

        [Fact]
        public void LocalInitializer_InsertConst2()
        {
            var src1 = @"
class C
{
    public void M()
    {
        int <AS:0>a = 1</AS:0>, b = 2;
    }
}";
            var src2 = @"
class C
{
    public void M()
    {
        const int a = 1, b = 2;
    <AS:0>}</AS:0>
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_Delete1()
        {
            var src1 = @"
class C
{
    <AS:0>int a = 1;</AS:0>
    int b = 1;

    public C() {}
}";
            var src2 = @"
class C
{
    int a;
    <AS:0>int b = 1;</AS:0>

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void LocalInitializer_Delete1()
        {
            var src1 = @"
class C
{
      public void M() { <AS:0>int a = 1</AS:0>; }
}";
            var src2 = @"
class C
{
    public void M() { int a; <AS:0>}</AS:0> 
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_Delete2()
        {
            var src1 = @"
class C
{
    int b = 1;
    int c;
    <AS:0>int a = 1;</AS:0>

    public C() {}
}";
            var src2 = @"
class C
{
    <AS:0>int b = 1;</AS:0>
    int c;
    int a;

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void LocalInitializer_Delete2()
        {
            var src1 = @"
class C
{
    public void M() 
    {
        int b = 1;
        int c;
        <AS:0>int a = 1;</AS:0>
    }
}";
            var src2 = @"
class C
{
    public void M()
    { 
        int b = 1;
        int c;
        int a;
    <AS:0>}</AS:0>
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_Delete3()
        {
            var src1 = @"
class C
{
    int b = 1;
    int c;
    <AS:0>int a = 1;</AS:0>

    public C() {}
}";
            var src2 = @"
class C
{
    <AS:0>int b = 1;</AS:0>
    int c;

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.field, "a")));
        }

        [Fact]
        public void LocalInitializer_Delete3()
        {
            var src1 = @"
class C
{
    public void M() 
    {
        int b = 1;
        int c;
        <AS:0>int a = 1;</AS:0>
    }
}";
            var src2 = @"
class C
{
    public void M()
    { 
        int b = 1;
        int c;
    <AS:0>}</AS:0>
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_DeleteStaticInstance1()
        {
            var src1 = @"
class C
{
    <AS:0>int a = 1;</AS:0>
    static int b = 1;
    int c = 1;
    
    public C() {}
}";
            var src2 = @"
class C
{
    int a;
    static int b = 1;
    <AS:0>int c = 1;</AS:0>

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_DeleteStaticInstance2()
        {
            var src1 = @"
class C
{
    static int c = 1;
    <AS:0>static int a = 1;</AS:0>
    int b = 1;
    
    public C() {}
}";
            var src2 = @"
class C
{
    <AS:0>static int c = 1;</AS:0>
    static int a;
    int b = 1;

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_DeleteStaticInstance3()
        {
            var src1 = @"
class C
{
    <AS:0>static int a = 1;</AS:0>
    int b = 1;
    
    public C() {}
}";
            var src2 = @"
class C
{
    <AS:0>static int a;</AS:0>
    int b = 1;

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void FieldInitializer_DeleteMove1()
        {
            var src1 = @"
class C
{
    int b = 1;
    int c;
    <AS:0>int a = 1;</AS:0>

    public C() {}
}";
            var src2 = @"
class C
{
    int c;
    <AS:0>int b = 1;</AS:0>

    public C() {}
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.Move, "int c", FeaturesResources.field),
                Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.field, "a")));
        }

        [Fact]
        public void LocalInitializer_DeleteReorder1()
        {
            var src1 = @"
class C
{
    public void M() 
    {
        int b = 1;
        <AS:0>int a = 1;</AS:0>
        int c;
    }
}";
            var src2 = @"
class C
{
    public void M()
    { 
        int c;
        <AS:0>int b = 1;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void FieldToProperty1()
        {
            var src1 = @"
class C
{
    int a = <AS:0>1</AS:0>;
}";

            // The placement of the active statement is not ideal, but acceptable.
            var src2 = @"
<AS:0>class C</AS:0>
{
    int a { get; } = 1;
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.field, "a")));
        }

        [Fact]
        public void PropertyToField1()
        {
            var src1 = @"
class C
{
    int a { get; } = <AS:0>1</AS:0>;
}";

            // The placement of the active statement is not ideal, but acceptable.
            var src2 = @"
<AS:0>class C</AS:0>
{
    int a = 1;
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.Delete, "class C", DeletedSymbolDisplay(FeaturesResources.auto_property, "a")));
        }

        #endregion

        #region Lock Statement

        [Fact]
        public void LockBody_Update()
        {
            var src1 = @"
class Test
{
    private static object F() { <AS:0>return new object();</AS:0> }

    static void Main(string[] args)
    {
        <AS:1>lock (F())</AS:1>
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static object F() { <AS:0>return new object();</AS:0> }

    static void Main(string[] args)
    {
        <AS:1>lock (F())</AS:1>
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [WorkItem(755749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755749")]
        [Fact]
        public void Lock_Insert_Leaf()
        {
            var src1 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        <AS:0>System.Console.Write(5);</AS:0>
    }
}";
            var src2 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (lockThis)
        {
            <AS:0>System.Console.Write(5);</AS:0>
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "lock (lockThis)", CSharpFeaturesResources.lock_statement));
        }

        [WorkItem(755749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755749")]
        [Fact]
        public void Lock_Insert_Leaf2()
        {
            var src1 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        {
            System.Console.Write(5);
        <AS:0>}</AS:0>
    }
}";
            var src2 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (lockThis)
        {
            System.Console.Write(5);
        <AS:0>}</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "lock (lockThis)", CSharpFeaturesResources.lock_statement));
        }

        [Fact]
        public void Lock_Insert_Leaf3()
        {
            var src1 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        {
            System.Console.Write(5);
        }
        <AS:0>System.Console.Write(10);</AS:0>
    }
}";
            var src2 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (lockThis)
        {
            System.Console.Write(5);
        }
        <AS:0>System.Console.Write(5);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Lock_Insert_Leaf4()
        {
            var src1 = @"
class Test
{
    public static object a = new object();
    public static object b = new object();
    public static object c = new object();
    public static object d = new object();
    public static object e = new object();
    
    static void Main(string[] args)
    {
        lock (a)
        {
            lock (b)
            {
                lock (c)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var src2 = @"
class Test
{
    public static object a = new object();
    public static object b = new object();
    public static object c = new object();
    public static object d = new object();
    public static object e = new object();
    
    static void Main(string[] args)
    {
        lock (b)
        {
            lock (d)
            {
                lock (a)
                {
                    lock (e)
                    {
                        <AS:0>System.Console.Write();</AS:0>
                    }
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "lock (d)", CSharpFeaturesResources.lock_statement),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "lock (e)", CSharpFeaturesResources.lock_statement));
        }

        [Fact]
        public void Lock_Insert_Leaf5()
        {
            var src1 = @"
class Test
{
    public static object a = new object();
    public static object b = new object();
    public static object c = new object();
    public static object d = new object();
    public static object e = new object();
    
    static void Main(string[] args)
    {
        lock (a)
        {
            lock (c)
            {
                lock (b)
                {
                    lock (e)
                    {
                        <AS:0>System.Console.Write();</AS:0>
                    }
                }
            }
        }
    }
}";

            var src2 = @"
class Test
{
    public static object a = new object();
    public static object b = new object();
    public static object c = new object();
    public static object d = new object();
    public static object e = new object();
    
    static void Main(string[] args)
    {
        lock (b)
        {
            lock (d)
            {
                lock (a)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "lock (d)", CSharpFeaturesResources.lock_statement));
        }

        [WorkItem(755752, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755752")]
        [Fact]
        public void Lock_Update_Leaf()
        {
            var src1 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (lockThis)
        {
            <AS:0>System.Console.Write(5);</AS:0>
        }
    }
}";
            var src2 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (""test"")
        {
            <AS:0>System.Console.Write(5);</AS:0>
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "lock (\"test\")", CSharpFeaturesResources.lock_statement));
        }

        [Fact]
        public void Lock_Update_Leaf2()
        {
            var src1 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (lockThis)
        {
            System.Console.Write(5);
        }
        <AS:0>System.Console.Write(5);</AS:0>
    }
}";
            var src2 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (""test"")
        {
            System.Console.Write(5);
        }
        <AS:0>System.Console.Write(5);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Lock_Delete_Leaf()
        {
            var src1 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        lock (lockThis)
        {
            <AS:0>System.Console.Write(5);</AS:0>
        }
    }
}";
            var src2 = @"
class Test
{
    private static object lockThis = new object();
    static void Main(string[] args)
    {
        <AS:0>System.Console.Write(5);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Lock_Update_Lambda1()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        lock (F(a => a))
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        lock (F(a => a + 1))
        {
            <AS:0>Console.WriteLine(2);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Lock_Update_Lambda2()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        lock (F(a => a))
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        lock (G(a => a))
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "lock (G(a => a))", CSharpFeaturesResources.lock_statement));
        }

        #endregion

        #region Fixed Statement

        [Fact]
        public void FixedBody_Update()
        {
            var src1 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        unsafe
        {
            char* px2;
            fixed (<AS:1>char* pj = &F()</AS:1>)
            {
                System.Console.WriteLine(0);
            }
        }
    }
}";
            var src2 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        unsafe
        {
            char* px2;
            fixed (<AS:1>char* pj = &F()</AS:1>)
            {
                System.Console.WriteLine(1);
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [WorkItem(755742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755742")]
        [Fact]
        public void Fixed_Insert_Leaf()
        {
            var src1 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            <AS:0>px2 = null;</AS:0>
        }
    }
}";
            var src2 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* pj = &value)
            {
                <AS:0>px2 = null;</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "fixed (int* pj = &value)", CSharpFeaturesResources.fixed_statement));
        }

        [Fact]
        public void Fixed_Insert_Leaf2()
        {
            var src1 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
        <AS:0>}</AS:0>
    }
}";
            var src2 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* pj = &value)
            {
                px2 = null;
            }
        <AS:0>}</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [WorkItem(755742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755742")]
        [Fact]
        public void Fixed_Insert_Leaf3()
        {
            var src1 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            <AS:0>px2 = null;</AS:0>

            fixed (int* pj = &value)
            {
                
            }
        }
    }
}";
            var src2 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* pj = &value)
            {
                <AS:0>px2 = null;</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "fixed (int* pj = &value)", CSharpFeaturesResources.fixed_statement));
        }

        [WorkItem(755742, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755742")]
        [Fact]
        public void Fixed_Reorder_Leaf1()
        {
            var src1 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* a = &value)
            {
                fixed (int* b = &value)
                {
                    <AS:0>px2 = null;</AS:0>
                }
            }
        }
    }
}";
            var src2 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* b = &value)
            {
                fixed (int* a = &value)
                {
                    <AS:0>px2 = null;</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [WorkItem(755746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755746")]
        [Fact]
        public void Fixed_Update_Leaf1()
        {
            var src1 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* pj = &value)
            {
                <AS:0>px2 = null;</AS:0>
            }
        }
    }
}";
            var src2 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* p = &value)
            {
                <AS:0>px2 = null;</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "fixed (int* p = &value)", CSharpFeaturesResources.fixed_statement));
        }

        [WorkItem(755746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755746")]
        [Fact]
        public void Fixed_Update_Leaf2()
        {
            var src1 = @"
class Test
{
    public static int value1 = 10;
    public static int value2 = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* a = &value1)
            {
                fixed (int* b = &value1)
                {
                    fixed (int* c = &value1)
                    {
                        <AS:0>px2 = null;</AS:0>
                    }
                }
            }
        }
    }
}";
            var src2 = @"
class Test
{
    public static int value1 = 10;
    public static int value2 = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* c = &value1)
            {
                fixed (int* d = &value1)
                {
                    fixed (int* a = &value2)
                    {
                        fixed (int* e = &value1)
                        {
                            <AS:0>px2 = null;</AS:0>
                        }
                    }
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "fixed (int* a = &value2)", CSharpFeaturesResources.fixed_statement),
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "fixed (int* d = &value1)", CSharpFeaturesResources.fixed_statement),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "fixed (int* e = &value1)", CSharpFeaturesResources.fixed_statement));
        }

        [Fact]
        public void Fixed_Delete_Leaf()
        {
            var src1 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            fixed (int* pj = &value)
            {
                <AS:0>px2 = null;</AS:0>
            }
        }
    }
}";
            var src2 = @"
class Test
{
    static int value = 20;
    static void Main(string[] args)
    {
        unsafe
        {
            int* px2;
            <AS:0>px2 = null;</AS:0>
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Fixed_Update_Lambda1()
        {
            var src1 = @"
class C
{
    static unsafe void Main(string[] args)
    {
        fixed (byte* p = &F(a => a))
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var src2 = @"
class C
{
    static unsafe void Main(string[] args)
    {
        fixed (byte* p = &F(a => a + 1))
        {
            <AS:0>Console.WriteLine(2);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Fixed_Update_Lambda2()
        {
            var src1 = @"
class C
{
    static unsafe void Main(string[] args)
    {
        fixed (byte* p = &F(a => a))
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var src2 = @"
class C
{
    static unsafe void Main(string[] args)
    {
        fixed (byte* p = &G(a => a))
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "fixed (byte* p = &G(a => a))", CSharpFeaturesResources.fixed_statement));
        }

        #endregion

        #region ForEach Statement

        [Fact]
        public void ForEachBody_Update_ExpressionActive()
        {
            var src1 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (char c in <AS:1>F()</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (char c in <AS:1>F()</AS:1>)
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachVariableBody_Update_ExpressionActive()
        {
            var src1 = @"
class Test
{
    private static (string, int) F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach ((string s, int i) in <AS:1>F()</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static (string, int) F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach ((string s, int i) in <AS:1>F()</AS:1>)
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachBody_Update_InKeywordActive()
        {
            var src1 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (char c <AS:1>in</AS:1> F())
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (char c <AS:1>in</AS:1> F())
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachVariableBody_Update_InKeywordActive()
        {
            var src1 = @"
class Test
{
    private static (string, int) F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach ((string s, int i) <AS:1>in</AS:1> F())
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static (string, int) F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach ((string s, int i) <AS:1>in</AS:1> F())
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachBody_Update_VariableActive()
        {
            var src1 = @"
class Test
{
    private static string[] F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (<AS:1>string c</AS:1> in F())
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static string[] F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (<AS:1>string c</AS:1> in F())
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachVariableBody_Update_VariableActive()
        {
            var src1 = @"
class Test
{
    private static (string, int) F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (<AS:1>(string s, int i)</AS:1> in F())
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static (string, int) F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (<AS:1>(string s, int i)</AS:1> in F())
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachBody_Update_ForeachKeywordActive()
        {
            var src1 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        <AS:1>foreach</AS:1> (char c in F())
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static string F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        <AS:1>foreach</AS:1> (char c in F())
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachVariableBody_Update_ForeachKeywordActive()
        {
            var src1 = @"
class Test
{
    private static (string, int) F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        <AS:1>foreach</AS:1> ((string s, int i) in F())
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static (string, int) F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        <AS:1>foreach</AS:1> ((string s, int i) in F())
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachVariable_Update()
        {
            var src1 = @"
class Test
{
    private static string[] F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (<AS:1>string c</AS:1> in F())
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static string[] F() { <AS:0>return null;</AS:0> }

    static void Main(string[] args)
    {
        foreach (<AS:1>object c</AS:1> in F())
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // not ideal, but good enough:
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "object c"),
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "foreach (      object c        in F())", CSharpFeaturesResources.foreach_statement));
        }

        [Fact]
        public void ForEachDeconstructionVariable_Update()
        {
            var src1 = @"
class Test
{
    private static (int, (bool, double))[] F() { <AS:0>return new[] { (1, (true, 2.0)) };</AS:0> }

    static void Main(string[] args)
    {
        foreach (<AS:1>(int i, (bool b, double d))</AS:1> in F())
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static (int, (bool, double))[] F() { <AS:0>return new[] { (1, (true, 2.0)) };</AS:0> }

    static void Main(string[] args)
    {
        foreach (<AS:1>(int i, (var b, double d))</AS:1> in F())
        {
            System.Console.Write(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "(int i, (var b, double d))"),
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "foreach (      (int i, (var b, double d))        in F())", CSharpFeaturesResources.foreach_statement));
        }

        [Fact]
        public void ForEach_Reorder_Leaf()
        {
            var src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var b in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var b in e1)
        {
            foreach (var c in e1)
            {
                foreach (var a in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachVariable_Reorder_Leaf()
        {
            var src1 = @"
class Test
{
    public static (int, bool)[] e1 = new (int, bool)[1];
    public static (int, bool)[] e2 = new (int, bool)[1];
    
    static void Main(string[] args)
    {
        foreach ((var a1, var a2) in e1)
        {
            foreach ((int b1, bool b2) in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var src2 = @"
class Test
{
    public static (int, bool)[] e1 = new (int, bool)[1];
    public static (int, bool)[] e2 = new (int, bool)[1];
    
    static void Main(string[] args)
    {
        foreach ((int b1, bool b2) in e1)
        {
            foreach (var c in e1)
            {
                foreach ((var a1, var a2) in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEach_Update_Leaf()
        {
            var src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        <AS:0>System.Console.Write();</AS:0>
    }
}";
            var src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var b in e1)
        {
            foreach (var c in e1)
            {
                foreach (var a in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "foreach (var b in e1)", CSharpFeaturesResources.foreach_statement),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "foreach (var c in e1)", CSharpFeaturesResources.foreach_statement),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "foreach (var a in e1)", CSharpFeaturesResources.foreach_statement));
        }

        [Fact]
        public void ForEachVariable_Update_Leaf()
        {
            var src1 = @"
class Test
{
    public static (int, bool)[] e1 = new (int, bool)[1];
    public static (int, bool)[] e2 = new (int, bool)[1];
    
    static void Main(string[] args)
    {
        <AS:0>System.Console.Write();</AS:0>
    }
}";
            var src2 = @"
class Test
{
    public static (int, bool)[] e1 = new (int, bool)[1];
    public static (int, bool)[] e2 = new (int, bool)[1];
    
    static void Main(string[] args)
    {
        foreach ((int b1, bool b2) in e1)
        {
            foreach (var c in e1)
            {
                foreach ((var a1, var a2) in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "foreach (var c in e1)", CSharpFeaturesResources.foreach_statement),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "foreach ((int b1, bool b2) in e1)", CSharpFeaturesResources.foreach_statement),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "foreach ((var a1, var a2) in e1)", CSharpFeaturesResources.foreach_statement));
        }

        [Fact]
        public void ForEach_Delete_Leaf1()
        {
            var src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var b in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var b in e1)
            {
                <AS:0>System.Console.Write();</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachVariable_Delete_Leaf1()
        {
            var src1 = @"
class Test
{
    public static (int, bool)[] e1 = new (int, bool)[1];
    public static (int, bool)[] e2 = new (int, bool)[1];
    
    static void Main(string[] args)
    {
        foreach ((var a1, var a2) in e1)
        {
            foreach ((int b1, bool b2) in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var src2 = @"
class Test
{
    public static (int, bool)[] e1 = new (int, bool)[1];
    public static (int, bool)[] e2 = new (int, bool)[1];
    
    static void Main(string[] args)
    {
        foreach ((var a1, var a2) in e1)
        {
            foreach ((int b1, bool b2) in e1)
            {
                <AS:0>System.Console.Write();</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEach_Delete_Leaf2()
        {
            var src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var b in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var b in e1)
        {
            foreach (var c in e1)
            {
                <AS:0>System.Console.Write();</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachVariable_Delete_Leaf2()
        {
            var src1 = @"
class Test
{
    public static (int, bool)[] e1 = new (int, bool)[1];
    public static (int, bool)[] e2 = new (int, bool)[1];
    
    static void Main(string[] args)
    {
        foreach ((var a1, var a2) in e1)
        {
            foreach ((int b1, bool b2) in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var src2 = @"
class Test
{
    public static (int, bool)[] e1 = new (int, bool)[1];
    public static (int, bool)[] e2 = new (int, bool)[1];
    
    static void Main(string[] args)
    {
        foreach ((int b1, bool b2) in e1)
        {
            foreach (var c in e1)
            {
                <AS:0>System.Console.Write();</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEach_Delete_Leaf3()
        {
            var src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var b in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var a in e1)
        {
            foreach (var c in e1)
            {
                <AS:0>System.Console.Write();</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachVariable_Delete_Leaf3()
        {
            var src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach ((var a1, var a2) in e1)
        {
            foreach ((int b1, bool b2) in e1)
            {
                foreach (var c in e1)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach ((var a1, var a2) in e1)
        {
            foreach (var c in e1)
            {
                <AS:0>System.Console.Write();</AS:0>
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEach_Lambda1()
        {
            var src1 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        Action a = () =>
        {
            <AS:0>System.Console.Write();</AS:0>
        };

        <AS:1>a();</AS:1>
    }
}";
            var src2 = @"
class Test
{
    public static int[] e1 = new int[1];
    public static int[] e2 = new int[1];
    
    static void Main(string[] args)
    {
        foreach (var b in e1)
        {
            foreach (var c in e1)
            {
                Action a = () =>
                {                
                    foreach (var a in e1)
                    {
                        <AS:0>System.Console.Write();</AS:0>
                    }
                };
            }

            <AS:1>a();</AS:1>
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "foreach (var a in e1)", CSharpFeaturesResources.foreach_statement),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "foreach (var b in e1)", CSharpFeaturesResources.foreach_statement));
        }

        [Fact]
        public void ForEach_Update_Lambda1()
        {
            var src1 = @"
class C
{
    static unsafe void Main(string[] args)
    {
        foreach (var a in F(a => a))
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var src2 = @"
class C
{
    static unsafe void Main(string[] args)
    {
        foreach (var a in F(a => a + 1))
        {
            <AS:0>Console.WriteLine(2);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEach_Update_Lambda2()
        {
            var src1 = @"
class C
{
    static unsafe void Main(string[] args)
    {
        foreach (var a in F(a => a))
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var src2 = @"
class C
{
    static unsafe void Main(string[] args)
    {
        foreach (var a in G(a => a))
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "foreach (var a in G(a => a))", CSharpFeaturesResources.foreach_statement));
        }

        [Fact]
        public void ForEach_Update_Nullable()
        {
            var src1 = @"
class C
{
    static void F()
    {
        var arr = new int?[] { 0 };
        foreach (var s in arr)
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
        var arr = new int[] { 0 };
        foreach (var s in arr)
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEach_DeleteBody()
        {
            var src1 = @"
class C
{
    static void F()
    {
        foreach (var s in new[] { 1 }) <AS:0>G();</AS:0>
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
        foreach (var s in new[] { 1 }) <AS:0>;</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForEachVariable_DeleteBody()
        {
            var src1 = @"
class C
{
    static void F()
    {
        foreach ((var a1, var a2) in new[] { (1,1) }) <AS:0>G();</AS:0>
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
        foreach ((var a1, var a2) in new[] { (1,1) }) <AS:0>;</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region For Statement

        [Fact]
        public void ForStatement_Initializer1()
        {
            var src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        int i;
        for (<AS:1>i = F(1)</AS:1>; i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        int i;
        for (<AS:1>i = F(2)</AS:1>; i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "i = F(2)"));
        }

        [Fact]
        public void ForStatement_Initializer2()
        {
            var src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        int i;
        for (<AS:1>i = F(1)</AS:1>, F(1); i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        int i;
        for (<AS:1>i = F(1)</AS:1>, F(2); i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForStatement_Initializer_Delete()
        {
            var src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        int i;
        for (<AS:1>i = F(1)</AS:1>; i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        int i;
        for (; <AS:1>i < 10</AS:1>; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "for (;       i < 10       ; i++)", FeaturesResources.code));
        }

        [Fact]
        public void ForStatement_Declarator1()
        {
            var src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (<AS:1>var i = F(1)</AS:1>; i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (<AS:1>var i = F(2)</AS:1>; i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "var i = F(2)"));
        }

        [Fact]
        public void ForStatement_Declarator2()
        {
            var src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (<AS:1>int i = F(1)</AS:1>, j = F(1); i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (<AS:1>int i = F(1)</AS:1>, j = F(2); i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForStatement_Declarator3()
        {
            var src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (<AS:1>int i = F(1)</AS:1>; i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (<AS:1>int i = F(1)</AS:1>, j = F(2); i < 10; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForStatement_Condition1()
        {
            var src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; <AS:1>i < F(10)</AS:1>; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; <AS:1>i < F(20)</AS:1>; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "i < F(20)"));
        }

        [Fact]
        public void ForStatement_Condition_Delete()
        {
            var src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; <AS:1>i < F(10)</AS:1>; i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; ; <AS:1>i++</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "for (int i = 1; ;       i++       )", FeaturesResources.code));
        }

        [Fact]
        public void ForStatement_Incrementors1()
        {
            var src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); <AS:1>F(1)</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(20); <AS:1>F(1)</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForStatement_Incrementors2()
        {
            var src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); <AS:1>F(1)</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); <AS:1>F(2)</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "F(2)"));
        }

        [Fact]
        public void ForStatement_Incrementors3()
        {
            var src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); <AS:1>F(1)</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); <AS:1>F(1)</AS:1>, i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ForStatement_Incrementors4()
        {
            var src1 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); <AS:1>F(1)</AS:1>, i++)
        {
            System.Console.Write(0);
        }
    }
}";
            var src2 = @"
class Test
{
    private static int F(int a) { <AS:0>return a;</AS:0> }

    static void Main(string[] args)
    {
        for (int i = 1; i < F(10); i++, <AS:1>F(1)</AS:1>)
        {
            System.Console.Write(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region Using Statement and Local Declaration

        [Fact]
        public void UsingStatement_Expression_Update_Leaf()
        {
            var src1 = @"
class Test
{
    public static System.IDisposable a = null;
    public static System.IDisposable b = null;
    public static System.IDisposable c = null;
    
    static void Main(string[] args)
    {
        using (a)
        {
            using (b)
            {
                <AS:0>System.Console.Write();</AS:0>
            }
        }
    }
}";
            var src2 = @"
class Test
{
    public static System.IDisposable a = null;
    public static System.IDisposable b = null;
    public static System.IDisposable c = null;
    
    static void Main(string[] args)
    {
        using (a)
        {
            using (c)
            {
                using (b)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // Using with an expression generates code that stores the value of the expression in a compiler-generated temp.
            // This temp is not initialized when using is added around an active statement so the disposal is a no-op.
            // The user might expect that the object the field points to is disposed at the end of the using block, but it isn't.
            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "using (c)", CSharpFeaturesResources.using_statement));
        }

        [Fact]
        public void UsingStatement_Declaration_Update_Leaf()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        using (var a = new Disposable())
        {
            using (var b = new Disposable())
            {
                <AS:0>System.Console.Write();</AS:0>
            }
        }
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        using (var a = new Disposable())
        {
            using (var c = new Disposable())
            {
                using (var b = new Disposable())
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            }
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // Unlike using with an expression, using with a declaration does not introduce compiler-generated temps.
            // As with other local declarations that are added but not executed, the variable is not initialized and thus 
            // there should be no expectation (or need) for its disposal. Hence we do not report a rude edit.
            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UsingLocalDeclaration_Update_Leaf1()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        using var a = new Disposable(), b = new Disposable();
        <AS:0>System.Console.Write();</AS:0>
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        using var a = new Disposable(), c = new Disposable(), b = new Disposable();
        <AS:0>System.Console.Write();</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // Unlike using with an expression, using local declaration does not introduce compiler-generated temps.
            // As with other local declarations that are added but not executed, the variable is not initialized and thus 
            // there should be no expectation (or need) for its disposal. Hence we do not report a rude edit.
            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UsingLocalDeclaration_Update_Leaf2()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        using var a = new Disposable();
        using var b = new Disposable();
        <AS:0>System.Console.Write();</AS:0>
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        using var a = new Disposable();
        using var c = new Disposable();
        using var b = new Disposable();
        <AS:0>System.Console.Write();</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // Unlike using with an expression, using local declaration does not introduce compiler-generated temps.
            // As with other local declarations that are added but not executed, the variable is not initialized and thus 
            // there should be no expectation (or need) for its disposal. Hence we do not report a rude edit.
            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UsingStatement_Update_NonLeaf1()
        {
            var src1 = @"
class Disposable : IDisposable
{
    public void Dispose() <AS:0>{</AS:0>}
}

class Test
{
    static void Main(string[] args)
    {
        using (var a = new Disposable(1)) { System.Console.Write(); <AS:1>}</AS:1>
    }
}";
            var src2 = @"
class Disposable : IDisposable
{
    public void Dispose() <AS:0>{</AS:0>}
}

class Test
{
    static void Main(string[] args)
    {
        using (var a = new Disposable(2)) { System.Console.Write(); <AS:1>}</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "}"));
        }

        [Fact]
        public void UsingStatement_Update_NonLeaf2()
        {
            var src1 = @"
class Disposable : IDisposable
{
    public void Dispose() <AS:0>{</AS:0>}
}

class Test
{
    static void Main(string[] args)
    {
        using (Disposable a = new Disposable(1), b = Disposable(2)) { System.Console.Write(); <AS:1>}</AS:1>
    }
}";
            var src2 = @"
class Disposable : IDisposable
{
    public void Dispose() <AS:0>{</AS:0>}
}

class Test
{
    static void Main(string[] args)
    {
        using (Disposable a = new Disposable(1)) { System.Console.Write(); <AS:1>}</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "}"));
        }

        [Fact]
        public void UsingStatement_Update_NonLeaf_Lambda()
        {
            var src1 = @"
class Disposable : IDisposable
{
    public void Dispose() <AS:0>{</AS:0>}
}

class Test
{
    static void Main(string[] args)
    {
        using (var a = new Disposable(() => 1)) { System.Console.Write(); <AS:1>}</AS:1>
    }
}";
            var src2 = @"
class Disposable : IDisposable
{
    public void Dispose() <AS:0>{</AS:0>}
}

class Test
{
    static void Main(string[] args)
    {
        using (var a = new Disposable(() => 2)) { System.Console.Write(); <AS:1>}</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UsingLocalDeclaration_Update_NonLeaf1()
        {
            var src1 = @"
class Disposable : IDisposable
{
    public void Dispose() <AS:0>{</AS:0>}
}

class Test
{
    static void Main(string[] args)
    {
        if (F())
        {        
            using Disposable a = new Disposable(1);

            using Disposable b = new Disposable(2), c = new Disposable(3);

  <AS:1>}</AS:1>
    }
}";
            var src2 = @"
class Disposable : IDisposable
{
    public void Dispose() <AS:0>{</AS:0>}
}

class Test
{
    static void Main(string[] args)
    {
        if (F())
        {        
            using Disposable a = new Disposable(1);

            using Disposable b = new Disposable(20), c = new Disposable(3);

  <AS:1>}</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "}"));
        }

        [Fact]
        public void UsingLocalDeclaration_Update_NonLeaf_Lambda()
        {
            var src1 = @"
class Disposable : IDisposable
{
    public void Dispose() <AS:0>{</AS:0>}
}

class Test
{
    static void Main(string[] args)
    {
        if (F())
        {        
            using Disposable a = new Disposable(() => 1);

            {
                using var x = new Disposable(1);
            }

            using Disposable b = new Disposable(() => 2), c = new Disposable(() => 3);

  <AS:1>}</AS:1>
    }
}";
            var src2 = @"
class Disposable : IDisposable
{
    public void Dispose() <AS:0>{</AS:0>}
}

class Test
{
    static void Main(string[] args)
    {
        if (F())
        {        
            using Disposable a = new Disposable(() => 10);

            {
                using var x = new Disposable(2);
            }

            Console.WriteLine(1);

            using Disposable b = new Disposable(() => 20), c = new Disposable(() => 30);

  <AS:1>}</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UsingStatement_Expression_InLambdaBody1()
        {
            var src1 = @"
class Test
{
    public static System.IDisposable a = null;
    public static System.IDisposable b = null;
    public static System.IDisposable c = null;
    public static System.IDisposable d = null;
    
    static void Main(string[] args)
    {
        using (a)
        {
            Action a = () =>
            {
                using (b)
                {
                    <AS:0>System.Console.Write();</AS:0>
                }
            };
        }

        <AS:1>a();</AS:1>
    }
}";
            var src2 = @"
class Test
{
    public static System.IDisposable a = null;
    public static System.IDisposable b = null;
    public static System.IDisposable c = null;
    public static System.IDisposable d = null;
    
    static void Main(string[] args)
    {
        using (d)
        {
            Action a = () =>
            {
                using (c)
                {
                    using (b)
                    {
                        <AS:0>System.Console.Write();</AS:0>
                    }
                }
            };
        }

        <AS:1>a();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "using (c)", CSharpFeaturesResources.using_statement));
        }

        [Fact]
        public void UsingStatement_Expression_Update_Lambda1()
        {
            var src1 = @"
class C
{
    static unsafe void Main(string[] args)
    {
        using (F(a => a))
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var src2 = @"
class C
{
    static unsafe void Main(string[] args)
    {
        using (F(a => a + 1))
        {
            <AS:0>Console.WriteLine(2);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UsingStatement_Expression_Update_Lambda2()
        {
            var src1 = @"
class C
{
    static unsafe void Main(string[] args)
    {
        using (F(a => a))
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var src2 = @"
class C
{
    static unsafe void Main(string[] args)
    {
        using (G(a => a))
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "using (G(a => a))", CSharpFeaturesResources.using_statement));
        }

        #endregion

        #region Conditional Block Statements (If, Switch, While, Do)

        [Fact]
        public void IfBody_Update1()
        {
            var src1 = @"
class C
{
    public static bool B() <AS:0>{</AS:0> return false; }
    
    public static void Main()
    {
        <AS:1>if (B())</AS:1>
        {
            System.Console.WriteLine(0);
        }
    }
}";
            var src2 = @"
class C
{
    public static bool B() <AS:0>{</AS:0> return false; }
    
    public static void Main()
    {
        <AS:1>if (B())</AS:1>
        {
            System.Console.WriteLine(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void IfBody_Update2()
        {
            var src1 = @"
class C
{
    public static bool B() <AS:0>{</AS:0> return false; }
    
    public static void Main()
    {
        <AS:1>if (B())</AS:1>
        {
            System.Console.WriteLine(0);
        }
    }
}";
            var src2 = @"
class C
{
    public static bool B() <AS:0>{</AS:0> return false; }
    
    public static void Main()
    {
        <AS:1>if (!B())</AS:1>
        {
            System.Console.WriteLine(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "if (!B())"));
        }

        [Fact]
        public void IfBody_Update_Lambda()
        {
            var src1 = @"
class C
{
    public static bool B(Func<int> a) => <AS:0>false</AS:0>;
    
    public static void Main()
    {
        <AS:1>if (B(() => 1))</AS:1>
        {
            System.Console.WriteLine(0);
        }
    }
}";
            var src2 = @"
class C
{
    public static bool B(Func<int> a) => <AS:0>false</AS:0>;
    
    public static void Main()
    {
        <AS:1>if (B(() => 2))</AS:1>
        {
            System.Console.WriteLine(0);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void WhileBody_Update1()
        {
            var src1 = @"
class C
{
    public static bool B() <AS:0>{</AS:0> return false; }
    
    public static void Main()
    {
        <AS:1>while (B())</AS:1>
        {
            System.Console.WriteLine(0);
        }
    }
}";
            var src2 = @"
class C
{
    public static bool B() <AS:0>{</AS:0> return false; }
    
    public static void Main()
    {
        <AS:1>while (B())</AS:1>
        {
            System.Console.WriteLine(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void WhileBody_Update2()
        {
            var src1 = @"
class C
{
    public static bool B() <AS:0>{</AS:0> return false; }
    
    public static void Main()
    {
        <AS:1>while (B())</AS:1>
        {
            System.Console.WriteLine(0);
        }
    }
}";
            var src2 = @"
class C
{
    public static bool B() <AS:0>{</AS:0> return false; }
    
    public static void Main()
    {
        <AS:1>while (!B())</AS:1>
        {
            System.Console.WriteLine(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "while (!B())"));
        }

        [Fact]
        public void WhileBody_Update_Lambda()
        {
            var src1 = @"
class C
{
    public static bool B(Func<int> a) => <AS:0>false</AS:0>;
    
    public static void Main()
    {
        <AS:1>while (B(() => 1))</AS:1>
        {
            System.Console.WriteLine(0);
        }
    }
}";
            var src2 = @"
class C
{
    public static bool B(Func<int> a) => <AS:0>false</AS:0>;
    
    public static void Main()
    {
        <AS:1>while (B(() => 2))</AS:1>
        {
            System.Console.WriteLine(1);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void DoWhileBody_Update1()
        {
            var src1 = @"
class C
{
    public static bool B() <AS:0>{</AS:0> return false; }
    
    public static void Main()
    {
        do
        {
            System.Console.WriteLine(0);
        }
        <AS:1>while (B());</AS:1>
    }
}";
            var src2 = @"
class C
{
    public static bool B() <AS:0>{</AS:0> return false; }
    
    public static void Main()
    {
        do
        {
            System.Console.WriteLine(1);
        }
        <AS:1>while (B());</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void DoWhileBody_Update2()
        {
            var src1 = @"
class C
{
    public static bool B() <AS:0>{</AS:0> return false; }
    
    public static void Main()
    {
        do
        {
            System.Console.WriteLine(0);
        }
        <AS:1>while (B());</AS:1>
    }
}";
            var src2 = @"
class C
{
    public static bool B() <AS:0>{</AS:0> return false; }
    
    public static void Main()
    {
        do
        {
            System.Console.WriteLine(1);
        }
        <AS:1>while (!B());</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "while (!B());"));
        }

        [Fact]
        public void DoWhileBody_Update_Lambda()
        {
            var src1 = @"
class C
{
    public static bool B(Func<int> a) => <AS:0>false</AS:0>;
    
    public static void Main()
    {
        do
        {
            System.Console.WriteLine(0);
        }
        <AS:1>while (B(() => 1));</AS:1>
    }
}";
            var src2 = @"
class C
{
    public static bool B(Func<int> a) => <AS:0>false</AS:0>;
    
    public static void Main()
    {
        do
        {
            System.Console.WriteLine(1);
        }
        <AS:1>while (B(() => 2));</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void DoWhileBody_Delete()
        {
            var src1 = @"
class C
{
    static void F()
    {
        do <AS:0>G();</AS:0> while (true);
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
        do <AS:0>;</AS:0> while (true);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void SwitchCase_Update1()
        {
            var src1 = @"
class C
{
    public static string F() <AS:0>{</AS:0> return null; }
    
    public static void Main()
    {
        <AS:1>switch (F())</AS:1>
        {
            case ""a"": System.Console.WriteLine(0); break;
            case ""b"": System.Console.WriteLine(1); break;
        }
    }
}";
            var src2 = @"
class C
{
    public static string F() <AS:0>{</AS:0> return null; }
    
    public static void Main()
    {
        <AS:1>switch (F())</AS:1>
        {
            case ""a"": System.Console.WriteLine(0); break;
            case ""b"": System.Console.WriteLine(2); break;
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void SwitchCase_Update_Lambda()
        {
            var src1 = @"
class C
{
    public static bool B(Func<int> a) => <AS:0>false</AS:0>;
    
    public static void Main()
    {
        <AS:1>switch (B(() => 1))</AS:1>
        {
            case ""a"": System.Console.WriteLine(0); break;
            case ""b"": System.Console.WriteLine(1); break;
        }
    }
}";
            var src2 = @"
class C
{
    public static bool B(Func<int> a) => <AS:0>false</AS:0>;
    
    public static void Main()
    {
        <AS:1>switch (B(() => 2))</AS:1>
        {
            case ""a"": System.Console.WriteLine(0); break;
            case ""b"": System.Console.WriteLine(2); break;
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region Switch Statement When Clauses, Patterns

        [Fact]
        public void SwitchWhenClause_PatternUpdate1()
        {
            var src1 = @"
class C
{
    public static int Main()
    {
        switch (F())
        {
            case int a1 when G1(a1):
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
                
            case byte a when G5(a):
                return 10;
                
            case double b when G2(b):
                return 20;
                
            case C { X: 2 } when G4(9):
                return 30;
                
            case C { X: 2, Y: C { X: 1 } } c1 when G3(c1):
                return 40;
        }

        return 0;
    }
}";
            var src2 = @"
class C
{
    public static int Main()
    {
        switch (F())
        {
            case int a1 when G1(a1):
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
                
            case byte a when G5(a):
                return 10;
                
            case double b when G2(b):
                return 20;
                
            case C { X: 2 } when G4(9):
                return 30;
                
            case C { X: 2, Y: C { X: 2 } } c1 when G3(c1):
                return 40;
        }

        return 0;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "switch (F())", CSharpFeaturesResources.switch_statement_case_clause));
        }

        [Fact]
        public void SwitchWhenClause_PatternInsert()
        {
            var src1 = @"
class C
{
    public static int Main()
    {
        switch (F())
        {
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
        }

        return 0;
    }
}";
            var src2 = @"
class C
{
    public static int Main()
    {
        switch (F())
        {
            case int a1 when G1(a1):
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
        }

        return 0;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "switch (F())", CSharpFeaturesResources.switch_statement_case_clause));
        }

        [Fact]
        public void SwitchWhenClause_PatternDelete()
        {
            var src1 = @"
class C
{
    public static int Main()
    {
        switch (F())
        {
            case int a1 when G1(a1):
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
        }

        return 0;
    }
}";
            var src2 = @"
class C
{
    public static int Main()
    {
        switch (F())
        {
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
        }

        return 0;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "switch (F())", CSharpFeaturesResources.switch_statement_case_clause));
        }

        [Fact]
        public void SwitchWhenClause_WhenDelete()
        {
            var src1 = @"
class C
{
    public static int Main()
    {
        switch (F())
        {
            case byte a1 when G1(a1):
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
        }

        return 0;
    }
}";
            var src2 = @"
class C
{
    public static int Main()
    {
        switch (F())
        {
            case byte a1:
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
        }

        return 0;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "switch (F())", CSharpFeaturesResources.switch_statement_case_clause));
        }

        [Fact]
        public void SwitchWhenClause_WhenAdd()
        {
            var src1 = @"
class C
{
    public static int Main()
    {
        switch (F())
        {
            case byte a1:
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
        }

        return 0;
    }
}";
            var src2 = @"
class C
{
    public static int Main()
    {
        switch (F())
        {
            case byte a1 when G1(a1):
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
        }

        return 0;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "switch (F())", CSharpFeaturesResources.switch_statement_case_clause));
        }

        [Fact]
        public void SwitchWhenClause_WhenUpdate()
        {
            var src1 = @"
class C
{
    public static int Main()
    {
        switch (F())
        {
            case byte a1 when G1(a1):
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
        }

        return 0;
    }
}";
            var src2 = @"
class C
{
    public static int Main()
    {
        switch (F())
        {
            case byte a1 when G1(a1 * 2):
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
        }

        return 0;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void SwitchWhenClause_UpdateGoverningExpression()
        {
            var src1 = @"
class C
{
    public static int Main()
    {
        switch (F(1))
        {
            case int a1 when G1(a1):
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
        }

        return 0;
    }
}";
            var src2 = @"
class C
{
    public static int Main()
    {
        switch (F(2))
        {
            case int a1 when G1(a1):
            case int a2 <AS:0>when G1(a2)</AS:0>:
                return 10;
        }

        return 0;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "switch (F(2))", CSharpFeaturesResources.switch_statement));
        }

        [Fact]
        public void Switch_PropertyPattern_Update_NonLeaf()
        {
            var src1 = @"
class C
{
    public int X { get => <AS:0>1</AS:0>; }

    public static int F(object obj)
    {
        <AS:1>switch (obj)</AS:1>
        {
            case C { X: 1 }:
                return 1;
        }

        return 0;
    }
}";
            var src2 = @"
class C
{
    public int X { get => <AS:0>1</AS:0>; }

    public static int F(object obj)
    {
        <AS:1>switch (obj)</AS:1>
        {
            case C { X: 2 }:
                return 1;
        }

        return 0;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "switch (obj)"));
        }

        [Fact]
        public void Switch_PositionalPattern_Update_NonLeaf()
        {
            var src1 = @"
class C
{
    public void Deconstruct(out int x) => <AS:0>x = X</AS:0>;

    public static int F(object obj)
    {
        <AS:1>switch (obj)</AS:1>
        {
            case C ( x: 1 ):
                return 1;
        }

        return 0;
    }
}";
            var src2 = @"
class C
{
    public void Deconstruct(out int x) => <AS:0>x = X</AS:0>;

    public static int F(object obj)
    {
        <AS:1>switch (obj)</AS:1>
        {
            case C ( x: 2 ):
                return 1;
        }

        return 0;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "switch (obj)"));
        }

        [Fact]
        public void Switch_VarPattern_Update_NonLeaf()
        {
            var src1 = @"
class C
{
    public static object G() => <AS:0>null</AS:0>;
    
    public static int F(object obj)
    {
        <AS:1>switch (G())</AS:1>
        {
            case var (x, y):
                return 1;

            case 2:
                return 2;
        }

        return 0;
    }
}";
            var src2 = @"
class C
{
    public static object G() => <AS:0>null</AS:0>;

    public static int F(object obj)
    {
        <AS:1>switch (G())</AS:1>
        {
            case var (x, y):
                return 1;

            case 3:
                return 2;
        }

        return 0;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "switch (G())"));
        }

        [Fact]
        public void Switch_DiscardPattern_Update_NonLeaf()
        {
            var src1 = @"
class C
{
    public static object G() => <AS:0>null</AS:0>;
    
    public static int F(object obj)
    {
        <AS:1>switch (G())</AS:1>
        {
            case bool _:
                return 1;
        }

        return 0;
    }
}";
            var src2 = @"
class C
{
    public static object G() => <AS:0>null</AS:0>;

    public static int F(object obj)
    {
        <AS:1>switch (G())</AS:1>
        {
            case int _:
                return 1;
        }

        return 0;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "switch (G())"));
        }

        [Fact]
        public void Switch_NoPatterns_Update_NonLeaf()
        {
            var src1 = @"
class C
{
    public static object G() => <AS:0>null</AS:0>;

    public static int F(object obj)
    {
        <AS:1>switch (G())</AS:1>
        {
            case 1:
                return 1;
        }

        return 0;
    }
}";
            var src2 = @"
class C
{
    public static object G() => <AS:0>null</AS:0>;

    public static int F(object obj)
    {
        <AS:1>switch (G())</AS:1>
        {
            case 2:
                return 1;
        }

        return 0;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region Switch Expression

        [Fact]
        public void SwitchExpression()
        {
            var src1 = @"
class C
{
    public static int Main()
    {
        Console.WriteLine(1);

        <AS:4>return F() switch
        {
            int a <AS:0>when F1()</AS:0> => <AS:1>F2()</AS:1>,
            bool b => <AS:2>F3()</AS:2>,
            _ => <AS:3>F4()</AS:3>
        };</AS:4>
    }
}";
            var src2 = @"
class C
{
    public static int Main()
    {
        Console.WriteLine(2);

        <AS:4>return F() switch
        {
            int a <AS:0>when F1()</AS:0> => <AS:1>F2()</AS:1>,
            bool b => <AS:2>F3()</AS:2>,
            _ => <AS:3>F4()</AS:3>
        };</AS:4>
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void SwitchExpression_Lambda1()
        {
            var src1 = @"
class C
{
	public static int Main() => <AS:1>F() switch { 0 => new Func<int>(() => <AS:0>1</AS:0>)(), _ => 2}</AS:1>;
}";
            var src2 = @"
class C
{
	public static int Main() => <AS:1>F() switch { 0 => new Func<int>(() => <AS:0>3</AS:0>)(), _ => 2}</AS:1>;
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void SwitchExpression_Lambda2()
        {
            var src1 = @"
class C
{
	public static int Main() => <AS:1>F() switch { i => new Func<int>(() => <AS:0>i + 1</AS:0>)(), _ => 2}</AS:1>;
}";
            var src2 = @"
class C
{
	public static int Main() => <AS:1>F() switch { i => new Func<int>(() => <AS:0>i + 3</AS:0>)(), _ => 2}</AS:1>;
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        [WorkItem(43099, "https://github.com/dotnet/roslyn/issues/43099")]
        public void SwitchExpression_MemberExpressionBody()
        {
            var src1 = @"
class C
{
    public static int Main() => <AS:0>F() switch { 0 => 1, _ => 2}</AS:0>;
}";
            var src2 = @"
class C
{
    public static int Main() => <AS:0>G() switch { 0 => 10, _ => 20}</AS:0>;
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        [WorkItem(43099, "https://github.com/dotnet/roslyn/issues/43099")]
        public void SwitchExpression_LambdaBody()
        {
            var src1 = @"
class C
{
    public static Func<int> M() => () => <AS:0>F() switch { 0 => 1, _ => 2}</AS:0>;
}";
            var src2 = @"
class C
{
    public static Func<int> M() => () => <AS:0>G() switch { 0 => 10, _ => 20}</AS:0>;
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        [WorkItem(43099, "https://github.com/dotnet/roslyn/issues/43099")]
        public void SwitchExpression_QueryLambdaBody()
        {
            var src1 = @"
class C
{
    public static IEnumerable<int> M()
    {
        return 
           from a in new[] { 1 }
           where <AS:0>F() <AS:1>switch { 0 => true, _ => false}</AS:0>/**/</AS:1>
           select a;
    }
}";
            var src2 = @"
class C
{
    public static IEnumerable<int> M()
    {
        return 
           from a in new[] { 2 }
           where <AS:0>F() <AS:1>switch { 0 => true, _ => false}</AS:0>/**/</AS:1>
           select a;
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        [WorkItem(43099, "https://github.com/dotnet/roslyn/issues/43099")]
        public void SwitchExpression_NestedInGoverningExpression()
        {
            var src1 = @"
class C
{
    public static int Main() => <AS:1>(F() switch { 0 => 1, _ => 2 }) switch { 1 => <AS:0>10</AS:0>, _ => 20 }</AS:1>;
}";
            var src2 = @"
class C
{
    public static int Main() => <AS:1>(G() switch { 0 => 10, _ => 20 }) switch { 10 => <AS:0>100</AS:0>, _ => 200 }</AS:1>;
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "(G() switch { 0 => 10, _ => 20 }) switch { 10 =>       100       , _ => 200 }"));
        }

        [Fact]
        [WorkItem(43099, "https://github.com/dotnet/roslyn/issues/43099")]
        public void SwitchExpression_NestedInArm()
        {
            var src1 = @"
class C
{
    public static int Main() => F1() switch
    {
        1 when F2() <AS:0>switch { 0 => true, _ => false }</AS:0> => F3() <AS:1>switch { 0 => 1, _ => 2 }</AS:1>, 
        _ => 20
    };
}";
            var src2 = @"
class C
{
    public static int Main() => F1() switch
    {
        1 when F2() <AS:0>switch { 0 => true, _ => false }</AS:0> => F3() <AS:1>switch { 0 => 1, _ => 2 }</AS:1>,
        _ => 20
    };
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        [WorkItem(43099, "https://github.com/dotnet/roslyn/issues/43099")]
        public void SwitchExpression_Delete1()
        {
            var src1 = @"
class C
{
    public static int Main()
    {
        return Method() switch { true => G(), _ => F2() switch { 1 => <AS:0>0</AS:0>, _ => 2 } };
    }
}";
            var src2 = @"
class C
{
    public static int Main()
    {
        return Method() switch { true => G(), _ => <AS:0>1</AS:0> };
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        [WorkItem(43099, "https://github.com/dotnet/roslyn/issues/43099")]
        public void SwitchExpression_Delete2()
        {
            var src1 = @"
class C
{
    public static int Main()
    {
        return F1() switch { 1 => 0, _ => F2() switch { 1 => <AS:0>0</AS:0>, _ => 2 } };
    }
}";
            var src2 = @"
class C
{
    public static int Main()
    {
        return F1() switch { 1 => <AS:0>0</AS:0>, _ => 1 };
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        [WorkItem(43099, "https://github.com/dotnet/roslyn/issues/43099")]
        public void SwitchExpression_Delete3()
        {
            var src1 = @"
class C
{
    public static int Main()
    {
        return F1() switch { 1 when F2() switch { 1 => <AS:0>true</AS:0>, _ => false } => 0, _ => 2 };
    }
}";
            var src2 = @"
class C
{
    public static int Main()
    {
        return F1() switch { 1 <AS:0>when F3()</AS:0> => 0, _ => 1 };
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region Try

        [Fact]
        public void Try_Add_Inner()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Goo();</AS:1>
        }
        catch 
        {
        }
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "try", CSharpFeaturesResources.try_block));
        }

        [Fact]
        public void Try_Add_Leaf()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        try
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
        catch
        {
        }
    } 
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Try_Delete_Inner()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Goo();</AS:1>
        }
        <ER:1.0>catch 
        {
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Goo();", CSharpFeaturesResources.try_block));
        }

        [Fact]
        public void Try_Delete_Leaf()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>        
    }

    static void Goo()
    {
        try
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
        catch
        {
        }
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Try_Update_Inner()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Goo();</AS:1>
        }
        <ER:1.0>catch
        {
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Goo();</AS:1>
        }
        <ER:1.0>catch (IOException)
        {
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "try", CSharpFeaturesResources.try_block));
        }

        [Fact]
        public void Try_Update_Inner2()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Goo();</AS:1>
        }
        <ER:1.0>catch
        {
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Goo();</AS:1>
        }
        <ER:1.0>catch
        {
        }</ER:1.0>
        Console.WriteLine(2);
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void TryFinally_Update_Inner()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Goo();</AS:1>
        }
        <ER:1.0>finally
        {
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            <AS:1>Goo();</AS:1>
        }
        <ER:1.0>finally
        {
        }</ER:1.0>
        Console.WriteLine(2);
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Try_Update_Leaf()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        try
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
        catch
        {
        }
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        try
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
        catch (IOException)
        {
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void TryFinally_DeleteStatement_Inner()
        {
            var src1 = @"
class C
{
    static void Main()
    {
        <AS:0>Console.WriteLine(0);</AS:0>

        try
        {
            <AS:1>Console.WriteLine(1);</AS:1>
        }
        <ER:1.0>finally
        {
            Console.WriteLine(2);
        }</ER:1.0>
    }
}";
            var src2 = @"
class C
{
    static void Main()
    {
        <AS:0>Console.WriteLine(0);</AS:0>
     
        try
        {
        <AS:1>}</AS:1>
        finally
        {
            Console.WriteLine(2);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{", FeaturesResources.code));
        }

        [Fact]
        public void TryFinally_DeleteStatement_Leaf()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <ER:0.0>try
        {
            Console.WriteLine(0);
        }
        finally
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }</ER:0.0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine(0);
        }
        finally
        {
        <AS:0>}</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "finally", CSharpFeaturesResources.finally_clause));
        }

        [Fact]
        public void Try_DeleteStatement_Inner()
        {
            var src1 = @"
class C
{
    static void Main()
    {
        <AS:0>Console.WriteLine(0);</AS:0>
        
        try
        {
            <AS:1>Console.WriteLine(1);</AS:1>
        }
        <ER:1.0>finally
        {
            Console.WriteLine(2);
        }</ER:1.0>
    }
}";
            var src2 = @"
class C
{
    static void Main()
    {
        <AS:0>Console.WriteLine(0);</AS:0>
        
        try
        {
        <AS:1>}</AS:1>
        <ER:1.0>finally
        {
            Console.WriteLine(2);
        }</ER:1.0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{", FeaturesResources.code));
        }

        [Fact]
        public void Try_DeleteStatement_Leaf()
        {
            var src1 = @"
class C
{
    static void Main()
    {
        try
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
        finally
        {
            Console.WriteLine(2);
        }
    }
}";
            var src2 = @"
class C
{
    static void Main()
    {
        try
        {
        <AS:0>}</AS:0>
        finally
        {
            Console.WriteLine(2);
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region Catch

        [Fact]
        public void Catch_Add_Inner()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        catch 
        {
            <AS:1>Goo();</AS:1>
        }
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "catch", CSharpFeaturesResources.catch_clause));
        }

        [Fact]
        public void Catch_Add_Leaf()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        try
        {
        }
        catch 
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "catch", CSharpFeaturesResources.catch_clause));
        }

        [Fact]
        public void Catch_Delete_Inner()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        <ER:1.0>catch 
        {
            <AS:1>Goo();</AS:1>
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Goo();", CSharpFeaturesResources.catch_clause));
        }

        [Fact]
        public void Catch_Delete_Leaf()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>        
    }

    static void Goo()
    {
        try
        {
        }
        <ER:0.0>catch
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }</ER:0.0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Console.WriteLine(1);", CSharpFeaturesResources.catch_clause));
        }

        [Fact]
        public void Catch_Update_Inner()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        <ER:1.0>catch
        {
            <AS:1>Goo();</AS:1>
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        <ER:1.0>catch (IOException)
        {
            <AS:1>Goo();</AS:1>
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "catch", CSharpFeaturesResources.catch_clause));
        }

        [Fact]
        public void Catch_Update_InFilter_Inner()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException) <AS:1>when (Goo(1))</AS:1>
        {
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (Exception) <AS:1>when (Goo(1))</AS:1>
        {
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "catch", CSharpFeaturesResources.catch_clause));
        }

        [Fact]
        public void Catch_Update_Leaf()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        try
        {
        }
        <ER:0.0>catch
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }</ER:0.0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        try
        {
        }
        <ER:0.0>catch (IOException)
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }</ER:0.0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "catch", CSharpFeaturesResources.catch_clause));
        }

        [Fact]
        public void CatchFilter_Update_Inner()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException) <AS:1>when (Goo(1))</AS:1>
        {
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException) <AS:1>when (Goo(2))</AS:1>
        {
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "when (Goo(2))"),
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "catch", CSharpFeaturesResources.catch_clause));
        }

        [Fact]
        public void CatchFilter_Update_Leaf1()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:0.0>catch (IOException) <AS:0>when (Goo(1))</AS:0>
        {
        }</ER:0.0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:0.0>catch (IOException) <AS:0>when (Goo(2))</AS:0>
        {
        }</ER:0.0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "catch", CSharpFeaturesResources.catch_clause));
        }

        [Fact]
        public void CatchFilter_Update_Leaf2()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:0.0>catch (IOException) <AS:0>when (Goo(1))</AS:0>
        {
        }</ER:0.0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:0.0>catch (Exception) <AS:0>when (Goo(1))</AS:0>
        {
        }<ER:0.0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "catch", CSharpFeaturesResources.catch_clause));
        }

        #endregion

        #region Finally

        [Fact]
        public void Finally_Add_Inner()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        finally 
        {
            <AS:1>Goo();</AS:1>
        }
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "finally", CSharpFeaturesResources.finally_clause));
        }

        [Fact]
        public void Finally_Add_Leaf()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        try
        {
        }
        finally 
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "finally", CSharpFeaturesResources.finally_clause));
        }

        [Fact]
        public void Finally_Delete_Inner()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <ER:1.0>try
        {
        }
        finally 
        {
            <AS:1>Goo();</AS:1>
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Goo();", CSharpFeaturesResources.finally_clause));
        }

        [Fact]
        public void Finally_Delete_Leaf()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>        
    }

    static void Goo()
    {
        <ER:0.0>try
        {
        }
        finally
        {
            <AS:0>Console.WriteLine(1);</AS:0>
        }</ER:0.0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>Goo();</AS:1>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Console.WriteLine(1);", CSharpFeaturesResources.finally_clause));
        }

        #endregion

        #region Try-Catch-Finally

        [Fact]
        public void TryCatchFinally()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException)
        {
            try
            {
                try
                {
                    try
                    {
                        <AS:1>Goo();</AS:1>
                    }
                    catch 
                    {
                    }
                }
                catch (Exception)
                {
                }
            }
            finally
            {
            }
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {
        }
        <ER:1.0>catch (Exception)
        {
            try
            {
                try
                {
                }
                finally
                {
                    try
                    {
                        <AS:1>Goo();</AS:1>
                    }
                    catch 
                    {
                    }
                }   
            }
            catch (Exception)
            {
            }
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "catch", CSharpFeaturesResources.catch_clause),
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "try", CSharpFeaturesResources.try_block),
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "Goo();", CSharpFeaturesResources.try_block),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "finally", CSharpFeaturesResources.finally_clause));
        }

        [Fact, WorkItem(23865, "https://github.com/dotnet/roslyn/issues/23865")]
        public void TryCatchFinally_Regions()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException)
        {
            try
            {
                try
                {
                    try
                    {
                        <AS:1>Goo();</AS:1>
                    }
                    catch 
                    {
                    }
                }
                catch (Exception)
                {
                }
            }
            finally
            {
            }
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException)
        {
            try { try { try { <AS:1>Goo();</AS:1> } catch { } } catch (Exception) { } } finally { }
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // TODO: this is incorrect, we need to report a rude edit:
            edits.VerifySemanticDiagnostics(active);
        }

        [Fact, WorkItem(23865, "https://github.com/dotnet/roslyn/issues/23865")]
        public void TryCatchFinally2_Regions()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {    
            try
            {
                try
                {
                    try
                    {
                        <AS:1>Goo();</AS:1>
                    }
                    <ER:1.3>catch 
                    {
                    }</ER:1.3>
                }
                <ER:1.2>catch (Exception)
                {
                }</ER:1.2>
            }
            <ER:1.1>finally
            {
            }</ER:1.1>
        }
        <ER:1.0>catch (IOException)
        {
            
        }
        finally
        {
            
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {    
            try
            {
                try
                {
                    try
                    {
                        <AS:1>Goo();</AS:1>
                    }
                    <ER:1.3>catch 
                    {

                    }</ER:1.3>
                }
                <ER:1.2>catch (Exception)
                {
                }</ER:1.2>
            }
            <ER:1.1>finally
            {
            }</ER:1.1>
        }
        <ER:1.0>catch (IOException)
        {
            
        }
        finally
        {
            
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // TODO: this is incorrect, we need to report a rude edit since an ER span has been changed (empty line added):
            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void TryFilter_Regions1()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException e) when (e == null)
        {
            <AS:1>Goo();</AS:1>
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException e) when (e == null) { <AS:1>Goo();</AS:1> }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void TryFilter_Regions2()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException e) <AS:1>when (e == null)</AS:1>
        {
            Goo();
        }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        try
        {            
        }
        <ER:1.0>catch (IOException e) <AS:1>when (e == null)</AS:1> { Goo(); }</ER:1.0>
    }

    static void Goo()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Try_Lambda1()
        {
            var src1 = @"
using System;
using System.Linq;
class C
{
    static int Goo(int x)
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main()
    {
        Func<int, int> f = null;
        try
        {
            f = x => <AS:1>1 + Goo(x)</AS:1>;
        }
        catch
        {
        }

        <AS:2>Console.Write(f(2));</AS:2>
    }
}";
            var src2 = @"
using System;
using System.Linq;
class C
{
    static int Goo(int x)
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main()
    {
        Func<int, int> f = null;

        f = x => <AS:1>1 + Goo(x)</AS:1>;

        <AS:2>Console.Write(f(2));</AS:2>
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Try_Lambda2()
        {
            var src1 = @"
using System;
using System.Linq;
class C
{
    static int Goo(int x)
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main()
    {
        Func<int, int> f = x => 
        {
            try
            {
                <AS:1>return 1 + Goo(x);</AS:1>
            }
            <ER:1.0>catch
            {
            }</ER:1.0>
        };

        <AS:2>Console.Write(f(2));</AS:2>
    }
}";
            var src2 = @"
using System;
using System.Linq;
class C
{
    static int Goo(int x)
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main()
    {
        Func<int, int> f = x => 
        {
            <AS:1>return 1 + Goo(x);</AS:1>
        };

        <AS:2>Console.Write(f(2));</AS:2>
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "return 1 + Goo(x);", CSharpFeaturesResources.try_block));
        }

        [Fact]
        public void Try_Query_Join1()
        {
            var src1 = @"
class C
{
    static int Goo(int x)
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main()
    {
        try
        {
            q = from x in xs
                join y in ys on <AS:1>F()</AS:1> equals G()
                select 1;
        }
        catch
        {
        }

        <AS:2>q.ToArray();</AS:2>
    }
}";
            var src2 = @"
class C
{
    static int Goo(int x)
    {
        <AS:0>return 1;</AS:0>
    }

    static void Main()
    {
        q = from x in xs
            join y in ys on <AS:1>F()</AS:1> equals G()
            select 1;

        <AS:2>q.ToArray();</AS:2>
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region Checked/Unchecked

        [Fact]
        public void CheckedUnchecked_Insert_Leaf()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        int a = 1, b = 2;
        <AS:0>Console.WriteLine(a*b);</AS:0>
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        int a = 1, b = 2;
        checked
        {
            <AS:0>Console.WriteLine(a*b);</AS:0>
        }
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void CheckedUnchecked_Insert_Internal()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        <AS:1>System.Console.WriteLine(5 * M(1, 2));</AS:1>
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        checked
        {
            <AS:1>System.Console.WriteLine(5 * M(1, 2));</AS:1>
        }
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "checked", CSharpFeaturesResources.checked_statement));
        }

        [Fact]
        public void CheckedUnchecked_Delete_Internal()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        checked
        {
            <AS:1>System.Console.WriteLine(5 * M(1, 2));</AS:1>
        }
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        <AS:1>System.Console.WriteLine(5 * M(1, 2));</AS:1>
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteAroundActiveStatement, "System.Console.WriteLine(5 * M(1, 2));", CSharpFeaturesResources.checked_statement));
        }

        [Fact]
        public void CheckedUnchecked_Update_Internal()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        unchecked
        {   
            <AS:1>System.Console.WriteLine(5 * M(1, 2));</AS:1>
        }
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        checked
        {
            <AS:1>System.Console.WriteLine(5 * M(1, 2));</AS:1>
        }
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "checked", CSharpFeaturesResources.checked_statement));
        }

        [Fact]
        public void CheckedUnchecked_Lambda1()
        {
            var src1 = @"
class Test
{
    static void Main(string[] args)
    {
        unchecked
        {   
            Action f = () => <AS:1>5 * M(1, 2)</AS:1>;
        }

        <AS:2>f();</AS:2>
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var src2 = @"
class Test
{
    static void Main(string[] args)
    {
        checked
        {   
            Action f = () => <AS:1>5 * M(1, 2)</AS:1>;
        }

        <AS:2>f();</AS:2>
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "checked", CSharpFeaturesResources.checked_statement));
        }

        [Fact]
        public void CheckedUnchecked_Query1()
        {
            var src1 = @"
using System.Collections.Generic;
using System.Linq;

class Test
{
    static void Main()
    {
        IEnumerable<int> f;
        unchecked
        {
            f = from a in new[] { 5 } select <AS:1>M(a, int.MaxValue)</AS:1>;
        }

        <AS:2>f.ToArray();</AS:2>
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var src2 = @"
using System.Collections.Generic;
using System.Linq;

class Test
{
    static void Main()
    {
        IEnumerable<int> f;
        checked
        {
            f = from a in new[] { 5 } select <AS:1>M(a, int.MaxValue)</AS:1>;
        }

        <AS:2>f.ToArray();</AS:2>
    }

    private static int M(int a, int b) 
    {
        <AS:0>return a * b;</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "checked", CSharpFeaturesResources.checked_statement));
        }

        #endregion

        #region Lambdas

        [Fact, WorkItem(1359, "https://github.com/dotnet/roslyn/issues/1359")]
        public void Lambdas_LeafEdits_GeneralStatement()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>F(a => <AS:0>1</AS:0>);</AS:1>
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>F(a => <AS:0>2</AS:0>);</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact, WorkItem(1359, "https://github.com/dotnet/roslyn/issues/1359")]
        public void Lambdas_LeafEdits_Nested1()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:2>F(b => <AS:1>F(a => <AS:0>1</AS:0>)</AS:1>);</AS:2>
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:2>F(b => <AS:1>F(a => <AS:0>2</AS:0>)</AS:1>);</AS:2>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact, WorkItem(1359, "https://github.com/dotnet/roslyn/issues/1359")]
        public void Lambdas_LeafEdits_Nested2()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:2>F(b => <AS:1>F(a => <AS:0>1</AS:0>)</AS:1>);</AS:2>
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:2>F(b => <AS:1>G(a => <AS:0>2</AS:0>)</AS:1>);</AS:2>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "G(a =>       2       )"));
        }

        [Fact, WorkItem(1359, "https://github.com/dotnet/roslyn/issues/1359")]
        public void Lambdas_LeafEdits_IfStatement()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>if (F(a => <AS:0>1</AS:0>))</AS:1> { }
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>if (F(a => <AS:0>2</AS:0>))</AS:1> { }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact, WorkItem(1359, "https://github.com/dotnet/roslyn/issues/1359")]
        public void Lambdas_LeafEdits_WhileStatement()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>while (F(a => <AS:0>1</AS:0>))</AS:1> { }
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>while (F(a => <AS:0>2</AS:0>))</AS:1> { }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact, WorkItem(1359, "https://github.com/dotnet/roslyn/issues/1359")]
        public void Lambdas_LeafEdits_DoStatement()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        do {} <AS:1>while (F(a => <AS:0>1</AS:0>));</AS:1>
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        do {} <AS:1>while (F(a => <AS:0>2</AS:0>));</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact, WorkItem(1359, "https://github.com/dotnet/roslyn/issues/1359")]
        public void Lambdas_LeafEdits_SwitchStatement()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>switch (F(a => <AS:0>1</AS:0>))</AS:1>
        {
            case 0: break;
            case 1: break;
        }
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>switch (F(a => <AS:0>2</AS:0>))</AS:1>
        {
            case 0: break;
            case 1: break;
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact, WorkItem(1359, "https://github.com/dotnet/roslyn/issues/1359")]
        public void Lambdas_LeafEdits_LockStatement()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>lock (F(a => <AS:0>1</AS:0>))</AS:1> {}
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>lock (F(a => <AS:0>2</AS:0>))</AS:1> {}
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact, WorkItem(1359, "https://github.com/dotnet/roslyn/issues/1359")]
        public void Lambdas_LeafEdits_UsingStatement1()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>using (F(a => <AS:0>1</AS:0>))</AS:1> {}
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:1>using (F(a => <AS:0>2</AS:0>))</AS:1> {}
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Lambdas_ExpressionToStatements()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = a => <AS:0>1</AS:0>;
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Func<int, int> f = a => { return 1; };</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Lambdas_ExpressionToDelegate()
        {
            var src1 = @"
using System;
class C
{
    static void Main()
    {
        Func<int, int> f = a => <AS:0>1</AS:0>;
    }
}
";
            var src2 = @"
using System;
class C
{
    static void Main()
    {
        <AS:0>Func<int, int> f = delegate(int a) { return 1; };</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Lambdas_StatementsToExpression()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = a => { <AS:0>return 1;</AS:0> };
    }
}
";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        <AS:0>Func<int, int> f = a => 1;</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Lambdas_DelegateToExpression()
        {
            var src1 = @"
using System;
class C
{
    static void Main()
    {
        Func<int, int> f = delegate(int a) { <AS:0>return 1;</AS:0> };
    }
}
";
            var src2 = @"
using System;
class C
{
    static void Main()
    {
        <AS:0>Func<int, int> f = a => 1;</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Lambdas_StatementsToDelegate()
        {
            var src1 = @"
using System;
class C
{
    static void Main()
    {
        Func<int, int> f = a => { <AS:0>return 1;</AS:0> };
    }
}
";
            var src2 = @"
using System;
class C
{
    static void Main()
    {
        Func<int, int> f = delegate(int a) { <AS:0>return 2;</AS:0> };
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Lambdas_ActiveStatementUpdate()
        {
            var src1 = @"
using System;
class C
{
    static void Main(string[] args)
    {
        Func<int, int, int> f = (int a, int b) => <AS:0>a + b + 1</AS:0>;
        <AS:1>f(2);</AS:1>
    }
}";
            var src2 = @"
using System;
class C
{
    static void Main(string[] args)
    {
        Func<int, int, int> f = (_, _) => <AS:0>10</AS:0>;
        <AS:1>f(2);</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active, capabilities: EditAndContinueTestHelpers.Net6RuntimeCapabilities);
        }

        [Fact]
        public void Lambdas_ActiveStatementRemoved1()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = a =>
        {
            return b =>
            {
                <AS:0>return b;</AS:0>
            };
        };

        var z = f(1);
        <AS:1>z(2);</AS:1>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = b =>
        {
            <AS:0>return b;</AS:0>
        };

        var z = f;
        <AS:1>z(2);</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "return b;", CSharpFeaturesResources.lambda));
        }

        [Fact]
        public void Lambdas_ActiveStatementRemoved2()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = a => (b) => <AS:0>b</AS:0>;

        var z = f(1);
        <AS:1>z(2);</AS:1>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = <AS:0>(b)</AS:0> => b;

        var z = f;
        <AS:1>z(2);</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "(b)", CSharpFeaturesResources.lambda));
        }

        [Fact]
        public void Lambdas_ActiveStatementRemoved3()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = a =>
        {
            Func<int, int> z;

            F(b =>
            {
                <AS:0>return b;</AS:0>
            }, out z);

            return z;
        };

        var z = f(1);
        <AS:1>z(2);</AS:1>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, int> f = b =>
        {
            <AS:0>F(b);</AS:0>

            return 1;
        };

        var z = f;
        <AS:1>z(2);</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "F(b);", CSharpFeaturesResources.lambda));
        }

        [Fact]
        public void Lambdas_ActiveStatementRemoved4()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        Func<int, Func<int, int>> f = a =>
        {
            <AS:1>z(2);</AS:1>

            return b =>
            {
                <AS:0>return b;</AS:0>
            };
        };
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    <AS:0,1>{</AS:0,1>
    
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "{", CSharpFeaturesResources.lambda),
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "{", CSharpFeaturesResources.lambda));
        }

        [Fact]
        public void Queries_ActiveStatementRemoved_WhereClause()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        var s = from a in b where <AS:0>b.goo</AS:0> select b.bar;
        <AS:1>s.ToArray();</AS:1>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        var s = <AS:0>from</AS:0> a in b select b.bar;
        <AS:1>s.ToArray();</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "from", CSharpFeaturesResources.where_clause));
        }

        [Fact]
        public void Queries_ActiveStatementRemoved_LetClause()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        var s = from a in b let x = <AS:0>b.goo</AS:0> select x;
        <AS:1>s.ToArray();</AS:1>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        var s = <AS:0>from</AS:0> a in b select a.bar;
        <AS:1>s.ToArray();</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "from", CSharpFeaturesResources.let_clause));
        }

        [Fact]
        public void Queries_ActiveStatementRemoved_JoinClauseLeft()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        var s = from a in b
                join c in d on <AS:0>a.goo</AS:0> equals c.bar
                select a.bar;

        <AS:1>s.ToArray();</AS:1>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        var s = <AS:0>from</AS:0> a in b select a.bar;
        <AS:1>s.ToArray();</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "from", CSharpFeaturesResources.join_clause));
        }

        [Fact]
        public void Queries_ActiveStatementRemoved_OrderBy1()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        var s = from a in b
                orderby <AS:0>a.x</AS:0>, a.y descending, a.z ascending
                select a;

        <AS:1>s.ToArray();</AS:1>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        var s = <AS:0>from</AS:0> a in b select a.bar;
        <AS:1>s.ToArray();</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "from", CSharpFeaturesResources.orderby_clause));
        }

        [Fact]
        public void Queries_ActiveStatementRemoved_OrderBy2()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        var s = from a in b
                orderby a.x, <AS:0>a.y</AS:0> descending, a.z ascending
                select a;

        <AS:1>s.ToArray();</AS:1>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        var s = <AS:0>from</AS:0> a in b select a.bar;
        <AS:1>s.ToArray();</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "from", CSharpFeaturesResources.orderby_clause));
        }

        [Fact]
        public void Queries_ActiveStatementRemoved_OrderBy3()
        {
            var src1 = @"
class C
{
    static void Main(string[] args)
    {
        var s = from a in b
                orderby a.x, a.y descending, <AS:0>a.z</AS:0> ascending
                select a;

        <AS:1>s.ToArray();</AS:1>
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    {
        var s = <AS:0>from</AS:0> a in b select a.bar;
        <AS:1>s.ToArray();</AS:1>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "from", CSharpFeaturesResources.orderby_clause));
        }

        [Fact]
        public void Queries_Remove_JoinInto1()
        {
            var src1 = @"
class C
{
    static void Main()
    {
        var q = from x in xs
                join y in ys on F() equals G() into g
                select <AS:0>1</AS:0>;
    }
}";
            var src2 = @"
class C
{
    static void Main()
    {
        var q = from x in xs
                join y in ys on F() equals G()
                select <AS:0>1</AS:0>;
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Queries_Remove_QueryContinuation1()
        {
            var src1 = @"
class C
{
    static void Main()
    {
        var q = from x in xs
                group x by x.F() into g
                where <AS:0>g.F()</AS:0>
                select 1;
    }
}";
            var src2 = @"
class C
{
    static void Main()
    {
        var q = from x in xs
                group x by x.F() <AS:0>into</AS:0> g
                select 1;
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "into", CSharpFeaturesResources.where_clause));
        }

        [Fact]
        public void Queries_Remove_QueryContinuation2()
        {
            var src1 = @"
class C
{
    static void Main()
    {
        var q = from x in xs
                group x by x.F() into g
                select <AS:0>1</AS:0>;
    }
}";
            var src2 = @"
class C
{
    static void Main()
    {
        var q = from x in xs
                <AS:0>join</AS:0> y in ys on F() equals G() into g
                select 1;
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "join", CSharpFeaturesResources.select_clause));
        }

        [Fact]
        public void Queries_Select_Reduced1()
        {
            var src1 = @"
class C
{
    static void Main()
    {
        var q = from a in array
                where a > 0
                select <AS:0>a + 1</AS:0>;
    }
}";
            var src2 = @"
class C
{
    static void Main()
    {
        var q = from a in array
                where a > 0
                <AS:0>select</AS:0> a;
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "select", CSharpFeaturesResources.select_clause));
        }

        [Fact]
        public void Queries_Select_Reduced2()
        {
            var src1 = @"
class C
{
    static int F(IEnumerable<int> e) => <AS:0>1</AS:0>;

    static void Main()
    {
        <AS:1>F(from a in array where a > 0 select a + 1);</AS:1>
    }
}";
            var src2 = @"
class C
{
    static int F(IEnumerable<int> e) => <AS:0>1</AS:0>;
   
    static void Main()
    {
        <AS:1>F(from a in array where a > 0 select a);</AS:1>
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "F(from a in array where a > 0 select a);"));
        }

        [Fact]
        public void Queries_GroupBy_Reduced1()
        {
            var src1 = @"
class C
{
    static void Main()
    {
        var q = from a in array
                group <AS:0>a + 1</AS:0> by a;
    }
}";
            var src2 = @"
class C
{
    static void Main()
    {
        var q = from a in array
                <AS:0>group</AS:0> a by a;
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementLambdaRemoved, "group", CSharpFeaturesResources.groupby_clause));
        }

        [Fact]
        public void Queries_GroupBy_Reduced2()
        {
            var src1 = @"
class C
{
    static int F(IEnumerable<IGrouping<int, int>> e) => <AS:0>1</AS:0>;

    static void Main()
    {
        <AS:1>F(from a in array group a by a);</AS:1>
    }
}";
            var src2 = @"
class C
{
    static int F(IEnumerable<IGrouping<int, int>> e) => <AS:0>1</AS:0>;
   
    static void Main()
    {
        <AS:1>F(from a in array group a + 1 by a);</AS:1>
    }
}";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "F(from a in array group a + 1 by a);"));
        }

        #endregion

        #region State Machines

        [Fact]
        public void MethodToIteratorMethod_WithActiveStatement()
        {
            var src1 = @"
class C
{
    static IEnumerable<int> F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        return new[] { 1, 2, 3 };
    }
}
";
            var src2 = @"
class C
{
    static IEnumerable<int> F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        yield return 1;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "yield return 1;", CSharpFeaturesResources.yield_return_statement));
        }

        [Fact]
        public void MethodToIteratorMethod_WithActiveStatementInLambda()
        {
            var src1 = @"
class C
{
    static IEnumerable<int> F()
    {
        var f = new Action(() => { <AS:0>Console.WriteLine(1);</AS:0> });
        return new[] { 1, 2, 3 };
    }
}
";
            var src2 = @"
class C
{
    static IEnumerable<int> F()
    {
        var f = new Action(() => { <AS:0>Console.WriteLine(1);</AS:0> });
        yield return 1;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // should not contain RUDE_EDIT_INSERT_AROUND
            edits.VerifySemanticDiagnostics(
                active,
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void MethodToIteratorMethod_WithoutActiveStatement()
        {
            var src1 = @"
class C
{
    static IEnumerable<int> F()
    {
        Console.WriteLine(1);
        return new[] { 1, 2, 3 };
    }
}
";
            var src2 = @"
class C
{
    static IEnumerable<int> F()
    {
        Console.WriteLine(1);
        yield return 1;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(
                active,
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void MethodToAsyncMethod_WithActiveStatement_AwaitExpression()
        {
            var src1 = @"
class C
{
    static Task<int> F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>

        return Task.FromResult(1);
    }
}
";
            var src2 = @"
class C
{
    static async Task<int> F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        return await Task.FromResult(1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "await", CSharpFeaturesResources.await_expression));
        }

        [Fact]
        public void MethodToAsyncMethod_WithActiveStatement_AwaitForEach()
        {
            var src1 = @"
class C
{
    static Task<int> F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>

        return Task.FromResult(1);
    }
}
";
            var src2 = @"
class C
{
    static async Task<int> F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        await foreach (var x in AsyncIter()) { }
        return 1;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "await foreach (var x in AsyncIter())", CSharpFeaturesResources.asynchronous_foreach_statement));
        }

        [Fact]
        public void MethodToAsyncMethod_WithActiveStatement_AwaitUsing()
        {
            var src1 = @"
class C
{
    static Task<int> F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>

        return Task.FromResult(1);
    }
}
";
            var src2 = @"
class C
{
    static async Task<int> F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        await using IAsyncDisposable x = new AsyncDisposable(), y = new AsyncDisposable();
        return 1;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "x = new AsyncDisposable()", CSharpFeaturesResources.asynchronous_using_declaration),
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "y = new AsyncDisposable()", CSharpFeaturesResources.asynchronous_using_declaration));
        }

        [Fact]
        public void MethodToAsyncMethod_WithActiveStatement_NoAwait1()
        {
            var src1 = @"
class C
{
    static void F()
    <AS:0>{</AS:0>
        Console.WriteLine(1);
    }
}
";
            var src2 = @"
class C
{
    static async void F()
    <AS:0>{</AS:0>
        Console.WriteLine(1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "static async void F()"));
        }

        [Fact]
        public void MethodToAsyncMethod_WithActiveStatement_NoAwait2()
        {
            var src1 = @"
class C
{
    static void F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var src2 = @"
class C
{
    static async void F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "static async void F()"));
        }

        [Fact]
        public void MethodToAsyncMethod_WithActiveStatementInLambda1()
        {
            var src1 = @"
class C
{
    static Task<int> F()
    {
        var f = new Action(() => { <AS:0>Console.WriteLine(1);</AS:0> });
        return Task.FromResult(1);
    }
}
";
            var src2 = @"
class C
{
    static async Task<int> F()
    {
        var f = new Action(() => { <AS:0>Console.WriteLine(1);</AS:0> });
        return await Task.FromResult(1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // should not contain RUDE_EDIT_INSERT_AROUND
            edits.VerifySemanticDiagnostics(
                active,
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void MethodToAsyncMethod_WithActiveStatementInLambda_2()
        {
            var src1 = @"
class C
{
    static void F()
    {
        var f = new Action(() => { <AS:1>Console.WriteLine(1);</AS:1> });
        <AS:0>f();</AS:0>
    }
}
";
            var src2 = @"
class C
{
    static async void F()
    {
        var f = new Action(() => { <AS:1>Console.WriteLine(1);</AS:1> });
        <AS:0>f();</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "static async void F()"));
        }

        [Fact]
        public void MethodToAsyncMethod_WithLambda()
        {
            var src1 = @"
class C
{
    static void F()
    <AS:0>{</AS:0>
        var f = new Action(() => { Console.WriteLine(1); });
        f();
    }
}
";
            var src2 = @"
class C
{
    static async void F()
    <AS:0>{</AS:0>
        var f = new Action(() => { Console.WriteLine(1); });
        f();
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "static async void F()"));
        }

        [Fact]
        public void MethodToAsyncMethod_WithoutActiveStatement_1()
        {
            var src1 = @"
class C
{
    static Task<int> F()
    {
        Console.WriteLine(1);
        return Task.FromResult(1);
    }
}
";
            var src2 = @"
class C
{
    static async Task<int> F()
    {
        Console.WriteLine(1);
        return await Task.FromResult(1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(
                active,
                capabilities: EditAndContinueCapabilities.NewTypeDefinition);
        }

        [Fact]
        public void MethodToAsyncMethod_WithoutActiveStatement_2()
        {
            var src1 = @"
class C
{
    static void F()
    {
        Console.WriteLine(1);
    }
}
";
            var src2 = @"
class C
{
    static async void F()
    {
        Console.WriteLine(1);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void LambdaToAsyncLambda_WithActiveStatement()
        {
            var src1 = @"
using System;
using System.Threading.Tasks;

class C
{
    static void F()
    {
        var f = new Func<Task<int>>(() => 
        { 
            <AS:0>Console.WriteLine(1);</AS:0>
            return Task.FromResult(1);
        });
    }
}
";
            var src2 = @"
using System;
using System.Threading.Tasks;

class C
{
    static void F()
    {
        var f = new Func<Task<int>>(async () => 
        { 
            <AS:0>Console.WriteLine(1);</AS:0>
            return await Task.FromResult(1);
        });
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "await", CSharpFeaturesResources.await_expression));
        }

        [Fact]
        public void LambdaToAsyncLambda_WithActiveStatement_NoAwait()
        {
            var src1 = @"
using System;

class C
{
    static void F()
    {
        var f = new Action(() => { <AS:0>Console.WriteLine(1);</AS:0> });
    }
}
";
            var src2 = @"
using System;

class C
{
    static void F()
    {
        var f = new Action(async () => { <AS:0>Console.WriteLine(1);</AS:0> });
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "()"));
        }

        [Fact]
        public void LambdaToAsyncLambda_WithActiveStatement_NoAwait_Nested()
        {
            var src1 = @"
class C
{
    static void F()
    {
        var f = new Func<int, Func<int, int>>(a => <AS:0>b => 1</AS:0>);
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
        var f = new Func<int, Func<int, int>>(async a => <AS:0>b => 1</AS:0>);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, "a"));
        }

        [Fact]
        [WorkItem(37054, "https://github.com/dotnet/roslyn/issues/37054")]
        public void LocalFunctionToAsyncLocalFunction_BlockBody_WithActiveStatement()
        {
            var src1 = @"
class C
{
    static void F()
    {
        Task<int> f()
        { 
            <AS:0>Console.WriteLine(1);</AS:0>
            return Task.FromResult(1);
        }
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
        async Task<int> f()
        {
            <AS:0>Console.WriteLine(1);</AS:0>
            return await Task.FromResult(1);
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "await", CSharpFeaturesResources.await_expression));
        }

        [Fact]
        [WorkItem(37054, "https://github.com/dotnet/roslyn/issues/37054")]
        public void LocalFunctionToAsyncLocalFunction_ExpressionBody_WithActiveStatement()
        {
            var src1 = @"
class C
{
    static void F()
    {
        Task<int> f() => <AS:0>Task.FromResult(1)</AS:0>;
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
        async Task<int> f() => <AS:0>await Task.FromResult(1)</AS:0>;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.InsertAroundActiveStatement, "await", CSharpFeaturesResources.await_expression));
        }

        [Fact]
        public void AnonymousFunctionToAsyncAnonymousFunction_WithActiveStatement_NoAwait()
        {
            var src1 = @"
using System.Threading.Tasks;

class C
{
    static void F()
    {
        var f = new Func<Task>(delegate() { <AS:0>Console.WriteLine(1);</AS:0> return Task.CompletedTask; });
    }
}
";
            var src2 = @"
using System.Threading.Tasks;

class C
{
    static async void F()
    {
        var f = new Func<Task>(async delegate() { <AS:0>Console.WriteLine(1);</AS:0> });
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdatingStateMachineMethodAroundActiveStatement, FeaturesResources.delegate_));
        }

        [Fact]
        public void AsyncMethodEdit_Semantics()
        {
            var src1 = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task<int> F()
    {
        await using var x = new AsyncDisposable();

        await foreach (var x in AsyncIter()) 
        {
            Console.WriteLine(x);
        }

        return await Task.FromResult(1);
    }
}
";
            var src2 = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task<int> F()
    {
        await using var x = new AsyncDisposable();

        await foreach (var x in AsyncIter()) 
        {
            Console.WriteLine(x + 1);
        }

        return await Task.FromResult(2);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            _ = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void IteratorMethodEdit_Semantics()
        {
            var src1 = @"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F()
    {
        Console.WriteLine(1);
        yield return 1;
    }
}
";
            var src2 = @"
using System;
using System.Collections.Generic;

class C
{
    static IEnumerable<int> F()
    {
        Console.WriteLine(2);
        yield return 2;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            _ = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics();
        }

        [Fact]
        public void AsyncIteratorMethodEdit_Semantics()
        {
            var src1 = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    static async IAsyncEnumerable<int> F()
    {
        Console.WriteLine(1);
        await Task.Delay(1);
        yield return 1;
    }
}
";
            var src2 = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    static async IAsyncEnumerable<int> F()
    {
        Console.WriteLine(2);
        await Task.Delay(2);
        yield return 2;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            _ = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(targetFrameworks: new[] { TargetFramework.NetCoreApp });
        }

        [Fact]
        public void AsyncMethodToMethod()
        {
            var src1 = @"
class C
{
    static async void F()
    {
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ChangingFromAsynchronousToSynchronous, "static void F()", FeaturesResources.method));
        }

        #endregion

        #region Misplaced AS 

        [Fact]
        public void MisplacedActiveStatement1()
        {
            var src1 = @"
<AS:1>class C</AS:1>
{
    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
        <AS:2>return a;</AS:2> 
    }
}";
            var src2 = @"
class C
{
    public static int F(int a)
    {
        <AS:0>return a;</AS:0> 
        <AS:2>return a;</AS:2> 
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void MisplacedActiveStatement2()
        {
            var src1 = @"
class C
{
    static <AS:0>void</AS:0> Main(string[] args)
    {
    }
}";
            var src2 = @"
class C
{
    static void Main(string[] args)
    <AS:0>{</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void MisplacedTrackingSpan1()
        {
            var src1 = @"
class C
{
    static <AS:0>void</AS:0> Main(string[] args)
    {
    }
}";
            var src2 = @"
class C
{
    static <TS:0>void</TS:0> Main(string[] args)
    <AS:0>{</AS:0>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region C# 7.0

        [Fact]
        public void UpdateAroundActiveStatement_IsPattern()
        {
            var src1 = @"
class C
{
    static void F(object x)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        if (x is int i) { Console.WriteLine(""match""); }
    }
}
";
            var src2 = @"
class C
{
    static void F(object x)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        if (x is string s) { Console.WriteLine(""match""); }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UpdateAroundActiveStatement_DeconstructionDeclarationStatement()
        {
            var src1 = @"
class C
{
    static void F(object x)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        var (x, y) = (1, 2);
    }
}
";
            var src2 = @"
class C
{
    static void F(object x)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        var (x, y) = x;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UpdateAroundActiveStatement_DeconstructionForEach()
        {
            var src1 = @"
class C
{
    static void F(object o)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        foreach (var (x, y) in new[] { (1, 2) }) { Console.WriteLine(2); }
    }
}
";
            var src2 = @"
class C
{
    static void F(object o)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        foreach (var (x, y) in new[] { o }) { Console.WriteLine(2); }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UpdateAroundActiveStatement_VarDeconstruction()
        {
            var src1 = @"
class C
{
    static void F(object o1, object o2)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        for (var (x, y) = o1; ; ) { }
    }
}
";
            var src2 = @"
class C
{
    static void F(object o1, object o2)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        for (var (x, y) = o2; ; ) { }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UpdateAroundActiveStatement_TypedDeconstruction()
        {
            var src1 = @"
class C
{
    static void F(object o1, object o2)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        for ((int x, int y) = o1; ; ) { }
    }
}
";
            var src2 = @"
class C
{
    static void F(object o1, object o2)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        for ((int x, int y) = o2; ; ) { }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UpdateAroundActiveStatement_Tuple()
        {
            var src1 = @"
class C
{
    static void F(object o)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        (int, int) t;
    }
}
";
            var src2 = @"
class C
{
    static void F(object o)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        (int, int) t = (1, 2);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UpdateAroundActiveStatement_LocalFunction()
        {
            var src1 = @"
class C
{
    static void F(object o)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        void M() { Console.WriteLine(2); }
    }
}
";
            var src2 = @"
class C
{
    static void F(object o)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        void M() { Console.WriteLine(3); }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UpdateAroundActiveStatement_OutVar()
        {
            var src1 = @"
class C
{
    static void F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        M();
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        M(out var x);
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UpdateAroundActiveStatement_OutVarRemoved()
        {
            var src1 = @"
class C
{
    static void F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        M(out var x);
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UpdateAroundActiveStatement_Ref()
        {
            var src1 = @"
class C
{
    static void F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        ref int i = ref 1;
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        ref int i = ref 2;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UpdateAroundActiveStatement_DeconstructionDeclaration()
        {
            var src1 = @"
class C
{
    static void F(object o1, object o2)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        var (x, y) = o1;
    }
}
";
            var src2 = @"
class C
{
    static void F(object o1, object o2)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        var (x, y) = o2;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UpdateAroundActiveStatement_DeconstructionAssignment()
        {
            var src1 = @"
class C
{
    static void F(object o1, object o2)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        int x, y;
        (x, y) = o1;
    }
}
";
            var src2 = @"
class C
{
    static void F(object o1, object o2)
    {
        <AS:0>Console.WriteLine(1);</AS:0>
        int x, y;
        (x, y) = o2;
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void UpdateAroundActiveStatement_SwitchWithPattern()
        {
            var src1 = @"
class C
{
    static void F(object o1, object o2)
    {
        <AS:0>System.Console.WriteLine(1);</AS:0>
        switch (o1)
        {
            case int i:
                break;
        }
    }
}
";
            var src2 = @"
class C
{
    static void F(object o1, object o2)
    {
        <AS:0>System.Console.WriteLine(1);</AS:0>
        switch (o2)
        {
            case int i:
                break;
        }
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
            edits.VerifySemanticDiagnostics();
        }

        #endregion

        #region Nullable

        [Fact]
        public void ChangeLocalNullableToNonNullable()
        {
            var src1 = @"
class C
{
    static void F()
    {
        <AS:0>string? s = ""a"";</AS:0>
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
        <AS:0>string s = ""a"";</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void ChangeLocalNonNullableToNullable()
        {
            var src1 = @"
class C
{
    static void F()
    {
        <AS:0>string s = ""a"";</AS:0>
    }
}
";
            var src2 = @"
class C
{
    static void F()
    {
        <AS:0>string? s = ""a"";</AS:0>
    }
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region Partial Types

        [Fact]
        public void InsertDeleteMethod_Inactive()
        {
            // Moving inactive method declaration in a file with active statements.

            var srcA1 = "partial class C { void F1() { <AS:0>System.Console.WriteLine(1);</AS:0> } }";
            var srcB1 = "partial class C { void F2() { } }";
            var srcA2 = "partial class C { void F1() { <AS:0>System.Console.WriteLine(1);</AS:0> } void F2() { } }";
            var srcB2 = "partial class C { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        activeStatements: GetActiveStatements(srcA1, srcA2, path: "0"),
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F2")),
                        }),
                    DocumentResults(
                        activeStatements: GetActiveStatements(srcB1, srcB2, path: "1"))
                });
        }

        [Fact]
        [WorkItem(51177, "https://github.com/dotnet/roslyn/issues/51177")]
        [WorkItem(54758, "https://github.com/dotnet/roslyn/issues/54758")]
        public void InsertDeleteMethod_Active()
        {
            // Moving active method declaration in a file with active statements.
            // TODO: this is currently a rude edit

            var srcA1 = "partial class C { }";
            var srcB1 = "partial class C { void F() { <AS:0>System.Console.WriteLine(1);</AS:0> } }";
            var srcA2 = "partial class C { void F() { System.Console.WriteLine(1); } }";
            var srcB2 = "<AS:0>partial class C</AS:0> { }";

            EditAndContinueValidation.VerifySemantics(
                new[] { GetTopEdits(srcA1, srcA2), GetTopEdits(srcB1, srcB2) },
                new[]
                {
                    DocumentResults(
                        activeStatements: GetActiveStatements(srcA1, srcA2, path: "0"),
                        semanticEdits: new[]
                        {
                            SemanticEdit(SemanticEditKind.Update, c => c.GetMember<INamedTypeSymbol>("C").GetMember("F")),
                        }),
                    DocumentResults(
                        activeStatements: GetActiveStatements(srcB1, srcB2, path: "1"),
                        // TODO: this is odd AS location https://github.com/dotnet/roslyn/issues/54758
                        diagnostics: new[] { Diagnostic(RudeEditKind.DeleteActiveStatement, "      partial c", DeletedSymbolDisplay(FeaturesResources.method, "F()")) })
                });
        }

        #endregion

        #region Records

        [Fact]
        public void Record()
        {
            var src1 = @"
record C(int X)
{
    public int X { get; init; } = <AS:0>1</AS:0>;
}";
            var src2 = @"
record C(int X)
{
    public int X { get; init; } = <AS:0>2</AS:0>;
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Record_Constructor()
        {
            var src1 = @"
record C(int X)
{
    public int X { get; init; } = <AS:0>1</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>var x = new C(1);</AS:1>
    }
}";
            var src2 = @"
record C(int X)
{
    public int X { get; init; } = <AS:0>2</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>var x = new C(1);</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void Record_FieldInitializer_Lambda2()
        {
            var src1 = @"
record C(int X)
{
    Func<int, Func<int>> a = z => () => <AS:0>z + 1</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C(1).a(1)();</AS:1>
    }
}";
            var src2 = @"
record C(int X)
{
    Func<int, Func<int>> a = z => () => <AS:0>z + 2</AS:0>;

    static void Main(string[] args)
    {
        <AS:1>new C(1).a(1)();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region Line Mapping

        /// <summary>
        /// Validates that changes in #line directives produce semantic updates of the containing method.
        /// </summary>
        [Fact]
        public void LineMapping_ChangeLineNumber_WithinMethod()
        {
            var src1 = @"
class C
{
#line 1 ""a""
    static void F()
    {
        <AS:0>A();</AS:0>
#line 5 ""b""
        B(1);
        <AS:1>B();</AS:1>
#line 2 ""c""
        <AS:2>C();</AS:2>
        <AS:3>C();</AS:3>
#line hidden
        D();
#line default
        <AS:4>E();</AS:4>
    }
}";
            var src2 = @"
class C
{
#line 1 ""a""
    static void F()
    {
        <AS:0>A();</AS:0>
#line 5 ""b""
        B(2);
        <AS:1>B();</AS:1>
#line 9 ""c""
        <AS:2>C();</AS:2>
        <AS:3>C();</AS:3>
#line hidden
        D();
#line default
        <AS:4>E();</AS:4>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void LineMapping_ChangeFilePath()
        {
            var src1 = @"
class C
{
    static void F()
    {
        <AS:0>A();</AS:0>
#line 1 ""a""
        <AS:1>B();</AS:1>
    }
}";
            var src2 = @"
class C
{
    static void F()
    {
        <AS:0>A();</AS:0>
#line 1 ""b""
        <AS:1>B();</AS:1>
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.UpdateAroundActiveStatement, "B();", string.Format(FeaturesResources._0_directive, "line")));
        }

        [Fact]
        public void LineMapping_ExceptionRegions_ChangeLineNumber()
        {
            var src1 = @"
class C
{
    static void Main()
    <AS:0>{</AS:0>
        try
        {
            try
            {
                <AS:1>Goo();</AS:1>
            }
#line 20 ""a""
            <ER:1.1>catch (E1 e) { }</ER:1.1>
#line default
        }
#line 20 ""b""
        <ER:1.0>catch (E2 e) { }</ER:1.0>
#line default
    }
}";
            var src2 = @"
class C
{
    static void Main()
    <AS:0>{</AS:0>
        try
        {
            try
            {
                <AS:1>Goo();</AS:1>
            }
#line 20 ""a""
            <ER:1.1>catch (E1 e) { }</ER:1.1>
#line default
        }
#line 30 ""b""
        <ER:1.0>catch (E2 e) { }</ER:1.0>
#line default
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact, WorkItem(52971, "https://github.com/dotnet/roslyn/issues/52971")]
        public void LineMapping_ExceptionRegions_ChangeFilePath()
        {
            var src1 = @"
class C
{
    static void Main()
    <AS:0>{</AS:0>
        try
        {
            try
            {
                <AS:1>Goo();</AS:1>
            }
#line 20 ""a""
            <ER:1.1>catch (E1 e) { }</ER:1.1>
#line default
        }
#line 20 ""b""
        <ER:1.0>catch (E2 e) { }</ER:1.0>
#line default
    }
}";
            var src2 = @"
class C
{
    static void Main()
    <AS:0>{</AS:0>
        try
        {
            try
            {
                <AS:1>Goo();</AS:1>
            }
#line 20 ""a""
            <ER:1.1>catch (E1 e) { }</ER:1.1>
#line default
        }
#line 20 ""c""
        <ER:1.0>catch (E2 e) { }</ER:1.0>
#line default
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // TODO: rude edit should be reported
            edits.VerifySemanticDiagnostics(active);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/52971"), WorkItem(52971, "https://github.com/dotnet/roslyn/issues/52971")]
        public void LineMapping_ExceptionRegions_LineChange_MultipleMappedFiles()
        {
            var src1 = @"
class C
{
    static void Main()
    <AS:0>{</AS:0>
        try
        {
            <AS:1>Goo();</AS:1>
        }
#line 20 ""a""
        <ER:1.1>catch (E1 e) { }</ER:1.1>
#line 20 ""b""
        <ER:1.0>catch (E2 e) { }</ER:1.0>
#line default
    }
}";
            var src2 = @"
class C
{
    static void Main()
    <AS:0>{</AS:0>
        try
        {
            <AS:1>Goo();</AS:1>
        }
#line 20 ""a""
        <ER:1.1>catch (E1 e) { }</ER:1.1>
#line 30 ""b""
        <ER:1.0>catch (E2 e) { }</ER:1.0>
#line default
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            // TODO: rude edit?
            edits.VerifySemanticDiagnostics(active);
        }

        #endregion

        #region Misc

        [Fact]
        public void Delete_All_SourceText()
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
            var src2 = @"";
            var edits = GetTopEdits(src1, src2);

            edits.VerifySemanticDiagnostics(
                Diagnostic(RudeEditKind.Delete, null, DeletedSymbolDisplay(FeaturesResources.class_, "C")));
        }

        [Fact]
        public void PartiallyExecutedActiveStatement()
        {
            var src1 = @"
class C
{
    public static void F()
    {
        <AS:0>Console.WriteLine(1);</AS:0> 
        <AS:1>Console.WriteLine(2);</AS:1> 
        <AS:2>Console.WriteLine(3);</AS:2> 
        <AS:3>Console.WriteLine(4);</AS:3> 
        <AS:4>Console.WriteLine(5);</AS:4> 
    }
}";
            var src2 = @"
class C
{
    public static void F()
    {
        <AS:0>Console.WriteLine(10);</AS:0> 
        <AS:1>Console.WriteLine(20);</AS:1> 
        <AS:2>Console.WriteLine(30);</AS:2> 
        <AS:3>Console.WriteLine(40);</AS:3> 
        <AS:4>Console.WriteLine(50);</AS:4> 
    }
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2, flags: new[]
            {
                ActiveStatementFlags.PartiallyExecuted | ActiveStatementFlags.LeafFrame,
                ActiveStatementFlags.PartiallyExecuted | ActiveStatementFlags.NonLeafFrame,
                ActiveStatementFlags.LeafFrame,
                ActiveStatementFlags.NonLeafFrame,
                ActiveStatementFlags.NonLeafFrame | ActiveStatementFlags.LeafFrame
            });

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.PartiallyExecutedActiveStatementUpdate, "Console.WriteLine(10);"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Console.WriteLine(20);"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Console.WriteLine(40);"),
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Console.WriteLine(50);"));
        }

        [Fact]
        public void PartiallyExecutedActiveStatement_Deleted1()
        {
            var src1 = @"
class C
{
    public static void F()
    {
        <AS:0>Console.WriteLine(1);</AS:0> 
    }
}";
            var src2 = @"
class C
{
    public static void F()
    { 
    <AS:0>}</AS:0>
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2, flags: new[]
            {
                ActiveStatementFlags.PartiallyExecuted | ActiveStatementFlags.LeafFrame
            });

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.PartiallyExecutedActiveStatementDelete, "{", FeaturesResources.code));
        }

        [Fact]
        public void PartiallyExecutedActiveStatement_Deleted2()
        {
            var src1 = @"
class C
{
    public static void F()
    {
        <AS:0>Console.WriteLine(1);</AS:0> 
    }
}";
            var src2 = @"
class C
{
    public static void F()
    { 
    <AS:0>}</AS:0>
}";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2, flags: new[]
            {
                ActiveStatementFlags.NonLeafFrame | ActiveStatementFlags.LeafFrame
            });

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.DeleteActiveStatement, "{", FeaturesResources.code));
        }

        [Fact]
        public void Block_Delete()
        {
            var src1 = @"
class C
{
    public static void F()
    {
        G(1);
        <AS:0>{</AS:0> G(2); }
        G(3);
    }
}
";
            var src2 = @"
class C
{
    public static void F()
    {
        G(1);
        <AS:0>G(3);</AS:0>
    }
}
";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Theory, CombinatorialData]
        public void MemberBodyInternalError(bool outOfMemory)
        {
            var src1 = @"
class C
{
    public static void F()
    {
        <AS:1>G();</AS:1>
    }

    public static void G()
    {
        <AS:0>H(1);</AS:0>
    }

    public static void H(int x)
    {
    }
}
";
            var src2 = @"
class C
{
    public static void F()
    {
        <AS:1>G();</AS:1>
    }

    public static void G()
    {
        <AS:0>H(2);</AS:0>
    }

    public static void H(int x)
    {
    }
}
";

            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);
            var validator = new CSharpEditAndContinueTestHelpers(faultInjector: node =>
            {
                if (node.Parent is MethodDeclarationSyntax methodDecl && methodDecl.Identifier.Text == "G")
                {
                    throw outOfMemory ? new OutOfMemoryException() : new SimpleToStringException();
                }
            });

            var expectedDiagnostic = outOfMemory ?
                Diagnostic(RudeEditKind.MemberBodyTooBig, "public static void G()", FeaturesResources.method) :
                Diagnostic(RudeEditKind.MemberBodyInternalError, "public static void G()", FeaturesResources.method, SimpleToStringException.ToStringOutput);

            validator.VerifySemantics(
                new[] { edits },
                TargetFramework.NetCoreApp,
                new[] { DocumentResults(diagnostics: new[] { expectedDiagnostic }) });
        }

        /// <summary>
        /// Custom exception class that has a fixed ToString so that tests aren't relying
        /// on stack traces, which could make them flaky
        /// </summary>
        private class SimpleToStringException : Exception
        {
            public const string ToStringOutput = "<Exception>";

            public override string ToString()
            {
                return ToStringOutput;
            }
        }

        #endregion

        #region Top Level Statements

        [Fact]
        public void TopLevelStatements_UpdateAroundActiveStatement_LocalFunction()
        {
            var src1 = @"
using System;

<AS:0>Console.WriteLine(1);</AS:0>
void M() { Console.WriteLine(2); }
";
            var src2 = @"
using System;

<AS:0>Console.WriteLine(1);</AS:0>
void M() { Console.WriteLine(3); }
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void TopLevelStatements_UpdateAroundActiveStatement_OutVar()
        {
            var src1 = @"
using System;

<AS:0>Console.WriteLine(1);</AS:0>
M();
";
            var src2 = @"
using System;

<AS:0>Console.WriteLine(1);</AS:0>
M(out var x);
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active);
        }

        [Fact]
        public void TopLevelStatements_Inner()
        {
            var src1 = @"
using System;

<AS:1>Goo(1);</AS:1>

static void Goo(int a)
{
    <AS:0>Console.WriteLine(a);</AS:0>
}
";
            var src2 = @"
using System;

while (true)
{
    <AS:1>Goo(2);</AS:1>
}

static void Goo(int a)
{
    <AS:0>Console.WriteLine(a);</AS:0>
}
";
            var edits = GetTopEdits(src1, src2);
            var active = GetActiveStatements(src1, src2);

            edits.VerifySemanticDiagnostics(active,
                Diagnostic(RudeEditKind.ActiveStatementUpdate, "Goo(2);"));
        }

        #endregion
    }
}
