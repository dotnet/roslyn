// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        [WpfFact]
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

        [WpfFact, WorkItem(672396)]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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
        [|((IComparable)c).CompareTo(null)|];
    }
}           ", "c.CompareTo(null)", true);
        }

        [WpfFact]
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
        [|((IComparable)c).CompareTo(null)|];
    }
}           ", "c.CompareTo(null)", false);
        }

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
        public void SpeculationAnalyzerCollectionInitializers()
        {
            Test(@"
using System.Collections;
class Collection : IEnumerable
{
    public IEnumerator GetEnumerator() { return null; }
    public void Add(string s) { }
    public void Add(int i) { }
    void Main()
    {
        var c = new Collection { [|""5""|] };
    }
}           ", "5", true);
        }

        [WpfFact, WorkItem(1088815)]
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

        protected override SyntaxTree Parse(string text)
        {
            return SyntaxFactory.ParseSyntaxTree(text);
        }

        protected override bool IsExpressionNode(SyntaxNode node)
        {
            return node is ExpressionSyntax;
        }

        protected override Compilation CreateCompilation(SyntaxTree tree)
        {
            return CSharpCompilation.Create(
                CompilationName,
                new[] { tree },
                References,
                TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(new[] { KeyValuePair.Create("CS0219", ReportDiagnostic.Suppress) }));
        }

        protected override bool CompilationSucceeded(Compilation compilation, Stream temporaryStream)
        {
            var langCompilation = compilation;
            Func<Diagnostic, bool> isProblem = d => d.Severity >= DiagnosticSeverity.Warning;
            return !langCompilation.GetDiagnostics().Any(isProblem) &&
                !langCompilation.Emit(temporaryStream).Diagnostics.Any(isProblem);
        }

        protected override bool ReplacementChangesSemantics(SyntaxNode initialNode, SyntaxNode replacementNode, SemanticModel initialModel)
        {
            return new SpeculationAnalyzer((ExpressionSyntax)initialNode, (ExpressionSyntax)replacementNode, initialModel, CancellationToken.None).ReplacementChangesSemantics();
        }
    }
}
