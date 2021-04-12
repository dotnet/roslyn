﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Semantics;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Semantics
{
    public class SpeculationAnalyzerTests : SpeculationAnalyzerTestsBase
    {
        [Fact]
        public void SpeculationAnalyzerDifferentOverloads()
        {
            Test(@"
class Program
{
    void Vain(int arg = 3) { }
    void Vain(string arg) { }
    void Main()
    {
        [|Vain(5)|];
    }
}           ", "Vain(string.Empty)", true);
        }

        [Fact, WorkItem(672396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/672396")]
        public void SpeculationAnalyzerExtensionMethodExplicitInvocation()
        {
            Test(@"
static class Program
{
    public static void Vain(this int arg) { }
    static void Main()
    {
        [|5.Vain()|];
    }
}           ", "Vain(5)", false);
        }

        [Fact]
        public void SpeculationAnalyzerImplicitBaseClassConversion()
        {
            Test(@"
using System;
class Program
{
    void Main()
    {
        Exception ex = [|(Exception)new InvalidOperationException()|];
    }
}           ", "new InvalidOperationException()", false);
        }

        [Fact]
        public void SpeculationAnalyzerImplicitNumericConversion()
        {
            Test(@"
class Program
{
    void Main()
    {
        long i = [|(long)5|];
    }
}           ", "5", false);
        }

        [Fact]
        public void SpeculationAnalyzerImplicitUserConversion()
        {
            Test(@"
class From
{
    public static implicit operator To(From from) { return new To(); }
}
class To { }
class Program
{
    void Main()
    {
        To to = [|(To)new From()|];
    }
}           ", "new From()", true);
        }

        [Fact]
        public void SpeculationAnalyzerExplicitConversion()
        {
            Test(@"
using System;
class Program
{
    void Main()
    {
        Exception ex1 = new InvalidOperationException();
        var ex2 = [|(InvalidOperationException)ex1|];
    }
}           ", "ex1", true);
        }

        [Fact]
        public void SpeculationAnalyzerArrayImplementingNonGenericInterface()
        {
            Test(@"
using System.Collections;
class Program
{
    void Main()
    {
        var a = new[] { 1, 2, 3 };
        [|((IEnumerable)a).GetEnumerator()|];
    }
}           ", "a.GetEnumerator()", false);
        }

        [Fact]
        public void SpeculationAnalyzerVirtualMethodWithBaseConversion()
        {
            Test(@"
using System;
using System.IO;
class Program
{
    void Main()
    {
        var s = new MemoryStream();
        [|((Stream)s).Flush()|];
    }
}            ", "s.Flush()", false);
        }

        [Fact]
        public void SpeculationAnalyzerNonVirtualMethodImplementingInterface()
        {
            Test(@"
using System;
class Class : IComparable
{
    public int CompareTo(object other) { return 1; }
}
class Program
{
    static void Main()
    {
        var c = new Class();
        var d = new Class();
        [|((IComparable)c).CompareTo(d)|];
    }
}           ", "c.CompareTo(d)", true);
        }

        [Fact]
        public void SpeculationAnalyzerSealedClassImplementingInterface()
        {
            Test(@"
using System;
sealed class Class : IComparable
{
    public int CompareTo(object other) { return 1; }
}
class Program
{
    static void Main()
    {
        var c = new Class();
        var d = new Class();
        [|((IComparable)c).CompareTo(d)|];
    }
}           ", "((IComparable)c).CompareTo(d)", semanticChanges: false);
        }

        [Fact]
        public void SpeculationAnalyzerValueTypeImplementingInterface()
        {
            Test(@"
using System;
class Program
{
    void Main()
    {
        decimal d = 5;
        [|((IComparable<decimal>)d).CompareTo(6)|];
    }
}           ", "d.CompareTo(6)", false);
        }

        [Fact]
        public void SpeculationAnalyzerBinaryExpressionIntVsLong()
        {
            Test(@"
class Program
{
    void Main()
    {
        var r = [|1+1L|];
    }
}           ", "1+1", true);
        }

        [Fact]
        public void SpeculationAnalyzerQueryExpressionSelectType()
        {
            Test(@"
using System.Linq;
class Program
{
    static void Main(string[] args)
    {
        var items = [|from i in Enumerable.Range(0, 3) select (long)i|];
    }
}           ", "from i in Enumerable.Range(0, 3) select i", true);
        }

        [Fact]
        public void SpeculationAnalyzerQueryExpressionFromType()
        {
            Test(@"
using System.Linq;
class Program
{
    static void Main(string[] args)
    {
        var items = [|from i in new long[0] select i|];
    }
}           ", "from i in new int[0] select i", true);
        }

        [Fact]
        public void SpeculationAnalyzerQueryExpressionGroupByType()
        {
            Test(@"
using System.Linq;
class Program
{
    static void Main(string[] args)
    {
        var items = [|from i in Enumerable.Range(0, 3) group (long)i by i|];
    }
}           ", "from i in Enumerable.Range(0, 3) group i by i", true);
        }

        [Fact]
        public void SpeculationAnalyzerQueryExpressionOrderByType()
        {
            Test(@"
using System.Linq;
class Program
{
    static void Main(string[] args)
    {
        var items = from i in Enumerable.Range(0, 3) orderby [|(long)i|] select i;
    }
}           ", "i", true);
        }

        [Fact]
        public void SpeculationAnalyzerDifferentAttributeConstructors()
        {
            Test(@"
using System;
class AnAttribute : Attribute
{
    public AnAttribute(string a, long b) { }
    public AnAttribute(int a, int b) { }
}
class Program
{
    [An([|""5""|], 6)]
    static void Main() { }
}           ", "5", false, "6");

            // Note: the answer should have been that the replacement does change semantics (true),
            // however to have enough context one must analyze AttributeSyntax instead of separate ExpressionSyntaxes it contains,
            // which is not supported in SpeculationAnalyzer, but possible with GetSpeculativeSemanticModel API
        }

        [Fact]
        public void SpeculationAnalyzerCollectionInitializers()
        {
            Test(@"
using System.Collections;
class Collection : IEnumerable
{
    public IEnumerator GetEnumerator() { throw new System.NotImplementedException(); }
    public void Add(string s) { }
    public void Add(int i) { }
    void Main()
    {
        var c = new Collection { [|""5""|] };
    }
}           ", "5", true);
        }

        [Fact, WorkItem(1088815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1088815")]
        public void SpeculationAnalyzerBrokenCode()
        {
            Test(@"
public interface IRogueAction
{
    public string Name { get; private set; }

    protected IRogueAction(string name)
    {
        [|this.Name|] = name;
    }
}           ", "Name", semanticChanges: false, isBrokenCode: true);
        }

        [Fact, WorkItem(8111, "https://github.com/dotnet/roslyn/issues/8111")]
        public void SpeculationAnalyzerAnonymousObjectMemberDeclaredWithNeededCast()
        {
            Test(@"
class Program
{
    static void Main(string[] args)
    {
        object thing = new { shouldBeAnInt = [|(int)Directions.South|] };
    }
    public enum Directions { North, East, South, West }
}           ", "Directions.South", semanticChanges: true);
        }

        [Fact, WorkItem(8111, "https://github.com/dotnet/roslyn/issues/8111")]
        public void SpeculationAnalyzerAnonymousObjectMemberDeclaredWithUnneededCast()
        {
            Test(@"
class Program
{
    static void Main(string[] args)
    {
        object thing = new { shouldBeAnInt = [|(Directions)Directions.South|] };
    }
    public enum Directions { North, East, South, West }
}           ", "Directions.South", semanticChanges: false);
        }

        [Fact, WorkItem(19987, "https://github.com/dotnet/roslyn/issues/19987")]
        public void SpeculationAnalyzerSwitchCaseWithRedundantCast()
        {
            Test(@"
class Program
{
    static void Main(string[] arts)
    {
        var x = 1f;
        switch (x)
        {
            case [|(float) 1|]:
                System.Console.WriteLine(""one"");
                break;

            default:
                System.Console.WriteLine(""not one"");
                break;
        }
    }
}
            ", "1", semanticChanges: false);
        }

        [Fact, WorkItem(19987, "https://github.com/dotnet/roslyn/issues/19987")]
        public void SpeculationAnalyzerSwitchCaseWithRequiredCast()
        {
            Test(@"
class Program
{
    static void Main(string[] arts)
    {
        object x = 1f;
        switch (x)
        {
            case [|(float) 1|]: // without the case, object x does not match int 1
                System.Console.WriteLine(""one"");
                break;

            default:
                System.Console.WriteLine(""not one"");
                break;
        }
    }
}
            ", "1", semanticChanges: true);
        }

        [Fact, WorkItem(28412, "https://github.com/dotnet/roslyn/issues/28412")]
        public void SpeculationAnalyzerIndexerPropertyWithRedundantCast()
        {
            Test(code: @"
class Indexer
{
    public int this[int x] { get { return x; } }
}
class A
{
    public Indexer Foo { get; } = new Indexer();
}
class B : A
{
}
class Program
{
    static void Main(string[] args)
    {
        var b = new B();
        var y = ([|(A)b|]).Foo[1];
    }
}
", replacementExpression: "b", semanticChanges: false);
        }

        [Fact, WorkItem(28412, "https://github.com/dotnet/roslyn/issues/28412")]
        public void SpeculationAnalyzerIndexerPropertyWithRequiredCast()
        {
            Test(code: @"
class Indexer
{
    public int this[int x] { get { return x; } }
}
class A
{
    public Indexer Foo { get; } = new Indexer();
}
class B : A
{
    public new Indexer Foo { get; } = new Indexer();
}
class Program
{
    static void Main(string[] args)
    {
        var b = new B();
        var y = ([|(A)b|]).Foo[1];
    }
}
", replacementExpression: "b", semanticChanges: true);
        }

        [Fact, WorkItem(28412, "https://github.com/dotnet/roslyn/issues/28412")]
        public void SpeculationAnalyzerDelegatePropertyWithRedundantCast()
        {
            Test(code: @"
public delegate void MyDelegate();
class A
{
    public MyDelegate Foo { get; }
}
class B : A
{
}
class Program
{
    static void Main(string[] args)
    {
        var b = new B();
        ([|(A)b|]).Foo();
    }
}
", replacementExpression: "b", semanticChanges: false);
        }

        [Fact, WorkItem(28412, "https://github.com/dotnet/roslyn/issues/28412")]
        public void SpeculationAnalyzerDelegatePropertyWithRequiredCast()
        {
            Test(code: @"
public delegate void MyDelegate();
class A
{
    public MyDelegate Foo { get; }
}
class B : A
{
    public new MyDelegate Foo { get; }
}
class Program
{
    static void Main(string[] args)
    {
        var b = new B();
        ([|(A)b|]).Foo();
    }
}
", replacementExpression: "b", semanticChanges: true);
        }

        protected override SyntaxTree Parse(string text)
            => SyntaxFactory.ParseSyntaxTree(text);

        protected override bool IsExpressionNode(SyntaxNode node)
            => node is ExpressionSyntax;

        protected override Compilation CreateCompilation(SyntaxTree tree)
        {
            return CSharpCompilation.Create(
                CompilationName,
                new[] { tree },
                References,
                TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(new[] { KeyValuePairUtil.Create("CS0219", ReportDiagnostic.Suppress) }));
        }

        protected override bool CompilationSucceeded(Compilation compilation, Stream temporaryStream)
        {
            var langCompilation = compilation;
            static bool isProblem(Diagnostic d) => d.Severity >= DiagnosticSeverity.Warning;
            return !langCompilation.GetDiagnostics().Any(isProblem) &&
                !langCompilation.Emit(temporaryStream).Diagnostics.Any(isProblem);
        }

        protected override bool ReplacementChangesSemantics(SyntaxNode initialNode, SyntaxNode replacementNode, SemanticModel initialModel)
            => new SpeculationAnalyzer((ExpressionSyntax)initialNode, (ExpressionSyntax)replacementNode, initialModel, CancellationToken.None).ReplacementChangesSemantics();
    }
}
