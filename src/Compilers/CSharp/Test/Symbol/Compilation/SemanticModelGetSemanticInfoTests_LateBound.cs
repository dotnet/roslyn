// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SemanticModelGetSemanticInfoTests_LateBound : SemanticModelTestBase
    {
        [Fact]
        public void ObjectCreation()
        {
            string sourceCode = @"
class C
{
    public C(string x) {}
    public C(int x) {}
    public C(double x) {}
    public void M(dynamic d)
    {
        /*<bind>*/new C(d);/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.Equal("C", semanticInfo.Type.Name);
            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(3, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(3, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectCreation_ByRefDynamicArgument1()
        {
            string sourceCode = @"
class C
{
    public C(out dynamic x, ref dynamic y) { }
    
    public void M(dynamic d)
    {
        /*<bind>*/new C(out d, ref d);/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.Equal("C", semanticInfo.Type.Name);
            Assert.Equal("C..ctor(out dynamic x, ref dynamic y)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
        }

        [Fact]
        public void ObjectCreation_ByRefDynamicArgument2()
        {
            string sourceCode = @"
class C
{
    public C(out dynamic x, dynamic y) {}
    
    public void M(dynamic d)
    {
        /*<bind>*/new C(out d, d);/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.Equal("C", semanticInfo.Type.Name);
            Assert.Equal("C..ctor(out dynamic x, dynamic y)", semanticInfo.Symbol.ToTestDisplayString());

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ObjectCreation_ByRefDynamicArgument3()
        {
            string sourceCode = @"
class C
{
    public C(out dynamic x, int y) {}
    public C(out dynamic x, long y) {}
    
    public void M(dynamic d)
    {
        /*<bind>*/new C(out d, d);/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.Equal("C", semanticInfo.Type.Name);
            Assert.Null(semanticInfo.Symbol);

            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            Assert.Equal("C..ctor(out dynamic x, System.Int32 y)", semanticInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("C..ctor(out dynamic x, System.Int64 y)", semanticInfo.CandidateSymbols[1].ToTestDisplayString());

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            Assert.Equal("C..ctor(out dynamic x, System.Int32 y)", semanticInfo.MethodGroup[0].ToTestDisplayString());
            Assert.Equal("C..ctor(out dynamic x, System.Int64 y)", semanticInfo.MethodGroup[1].ToTestDisplayString());
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void DelegateInvocation()
        {
            string sourceCode = @"
class C
{
    public void M()
    {
        dynamic d = null;
        /*<bind>*/d()/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.True(semanticInfo.ConvertedType.IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void MethodInvocation_DynamicReceiver()
        {
            string sourceCode = @"
class C
{
    public void M()
    {
        dynamic d = null;
        /*<bind>*/ d.bar() /*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.True(semanticInfo.ConvertedType.IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void MethodInvocation_StaticReceiver()
        {
            string sourceCode = @"
class C
{
    public void M()
    {
        dynamic d = null;
        C s = null;
        /*<bind>*/ s.bar(d) /*</bind>*/;
    }

    public void bar(int a) 
    {
    }
    public void bar(long a) 
    {
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);
            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.True(semanticInfo.ConvertedType.IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);

            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void MethodInvocation_TypeReceiver_01()
        {
            string sourceCode1 = @"
class C
{
    public static C Create(int arg) { return null; }

    public void M(dynamic d)
    {
        /*<bind>*/C.Create(d);/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode1);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.Equal("C C.Create(System.Int32 arg)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);

            string sourceCode2 = @"
class C
{
    public static C Create(int arg) { return null; }

    public void M(dynamic d)
    {
        /*<bind>*/C.Create/*</bind>*/(d);
    }
}
";
            semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode2);

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
        }

        [Fact]
        public void MethodInvocation_TypeReceiver_02()
        {
            string sourceCode = @"
class C
{
    public static C Create(int arg) { return null; }
    public static C Create(long arg) { return null; }

    public void M(dynamic d)
    {
        /*<bind>*/C.Create(d);/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void MethodGroup_EarlyBound()
        {
            string source = @"
using System.Collections.Generic;

class List : List<int>
{
    public void Add(int x, string y)
    {
        /*<bind>*/Add/*</bind>*/(x);
    }
}";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(source);

            Assert.Null(semanticInfo.Type);
            Assert.Equal("void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void MethodGroup_DynamicArg()
        {
            string source = @"
using System.Collections.Generic;

class List : List<int>
{
    public void Add(dynamic x, string y)
    {
        /*<bind>*/Add/*</bind>*/(x);
    }

    public void Add(long y)
    {
    }
}";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(source);

            Assert.Null(semanticInfo.Type);

            Assert.Null(semanticInfo.Symbol);

            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(3, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void MethodInvocation_DynamicArg()
        {
            string source = @"
using System.Collections.Generic;

class List : List<int>
{
    public void Add(dynamic x, string y)
    {
        /*<bind>*/Add(x)/*</bind>*/;
    }

    public void Add(long y)
    {
    }
}";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(source);

            Assert.True(semanticInfo.Type.IsDynamic());

            Assert.Null(semanticInfo.Symbol);

            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void MethodInvocation_StaticReceiver_ByRefDynamicArgument()
        {
            string sourceCode = @"
class C
{
    public void M()
    {
        dynamic d = null;
        C s = null;
        /*<bind>*/ s.bar(ref d) /*</bind>*/;
    }

    public int bar(ref dynamic a) 
    {
        return 1;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);

            Assert.Equal(SpecialType.System_Int32, semanticInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, semanticInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("System.Int32 C.bar(ref dynamic a)", semanticInfo.Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(531141, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531141")]
        public void MethodInvocation_StaticReceiver_IdentifierNameSyntax()
        {
            string sourceCode = @"
using System;
namespace Dynamic
{
    class FunctionTestingWithOverloading
    {
        public dynamic OverloadedFunction(int d)
        {
            return d;
        }

        public dynamic OverloadedFunction(long d)
        {
            return d;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            FunctionTestingWithOverloading obj = new FunctionTestingWithOverloading();
            dynamic valueToBePassed = ""Hello"";
            dynamic result = obj./*<bind>*/OverloadedFunction/*</bind>*/(valueToBePassed);
            Console.WriteLine(""Value from overloaded function is {0}"", result);
        }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<IdentifierNameSyntax>(sourceCode);

            Assert.Null(semanticInfo.Type);
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);

            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            Assert.Equal("dynamic Dynamic.FunctionTestingWithOverloading.OverloadedFunction(System.Int32 d)", semanticInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("dynamic Dynamic.FunctionTestingWithOverloading.OverloadedFunction(System.Int64 d)", semanticInfo.CandidateSymbols[1].ToTestDisplayString());

            Assert.Null(semanticInfo.Symbol);

            Assert.Equal(2, semanticInfo.MethodGroup.Length);
            Assert.Equal("dynamic Dynamic.FunctionTestingWithOverloading.OverloadedFunction(System.Int32 d)", semanticInfo.MethodGroup[0].ToTestDisplayString());
            Assert.Equal("dynamic Dynamic.FunctionTestingWithOverloading.OverloadedFunction(System.Int64 d)", semanticInfo.MethodGroup[1].ToTestDisplayString());

            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CollectionInitializer()
        {
            string sourceCode = @"
class C
{
    public void M()
    {
        dynamic d = 0;

        var l = new List<int>
        { 
            /*<bind>*/{ d }/*</bind>*/
        };
    }

    public void bar(int a) 
    {
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);
        }

        [Fact]
        public void ObjectInitializer()
        {
            string sourceCode = @"
class C
{
    public dynamic Z;

    public void M()
    {
        dynamic d = 0;

        var c = new C
        { 
            /*<bind>*/Z = d/*</bind>*/
        };
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            // TODO: what about object initializers?
        }

        [Fact]
        public void Indexer_StaticReceiver_01()
        {
            string sourceCode = @"
class C
{
    public void TestMeth()
    {
        dynamic d = null;
        C c = null;

        var x = /*<bind>*/c[d]/*</bind>*/;
    }

    public int this[int a]
    {
        get { return 0; }
        set { }
    }

    public int this[long a]
    {
        get { return 0; }
        set { }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.True(semanticInfo.ConvertedType.IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length);
            Assert.Equal("System.Int32 C.this[System.Int32 a] { get; set; }", semanticInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("System.Int32 C.this[System.Int64 a] { get; set; }", semanticInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Null(semanticInfo.Symbol);

            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void Indexer_StaticReceiver_02()
        {
            string sourceCode = @"
class C
{
    public void TestMeth()
    {
        dynamic d = null;
        C c = null;

        var x = /*<bind>*/c[d]/*</bind>*/;
    }

    public int this[int a]
    {
        get { return 0; }
        set { }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.True(semanticInfo.ConvertedType.IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal("System.Int32 C.this[System.Int32 a] { get; set; }", semanticInfo.Symbol.ToTestDisplayString());

            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void Indexer_DynamicReceiver()
        {
            string sourceCode = @"
class C
{
    public void TestMeth()
    {
        dynamic d = null;
        C c = null;

        var x = /*<bind>*/d[c]/*</bind>*/;
    }

    public int this[int a]
    {
        get { return 0; }
        set { }
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.True(semanticInfo.ConvertedType.IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void MemberAccess()
        {
            string sourceCode = @"
class C
{
    public void TestMeth()
    {
        dynamic d = null;
        var x = /*<bind>*/d.F/*</bind>*/;
    }

    public int F { get; set; }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.True(semanticInfo.ConvertedType.IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void UnaryOperators()
        {
            var operators = new[] { "~", "!", "-", "+", "++", "--" };
            var operatorNames = new[] { WellKnownMemberNames.OnesComplementOperatorName,
                                        WellKnownMemberNames.LogicalNotOperatorName,
                                        WellKnownMemberNames.UnaryNegationOperatorName,
                                        WellKnownMemberNames.UnaryPlusOperatorName,
                                        WellKnownMemberNames.IncrementOperatorName,
                                        WellKnownMemberNames.DecrementOperatorName };

            for (int i = 0; i < operators.Length; i++)
            {
                var op = operators[i];
                string sourceCode = @"
class C
{
    public void TestMeth()
    {
        dynamic d = null;
        var x1 = /*<bind>*/" + op + @"d/*</bind>*/;
    }
}
";
                var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

                Assert.True(semanticInfo.Type.IsDynamic());
                Assert.True(semanticInfo.ConvertedType.IsDynamic());
                Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

                Assert.Equal("dynamic dynamic." + operatorNames[i] + "(dynamic value)", semanticInfo.Symbol.ToTestDisplayString());
                Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
                Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
                Assert.Equal(0, semanticInfo.MethodGroup.Length);
                Assert.False(semanticInfo.IsCompileTimeConstant);
            }
        }

        [Fact]
        public void Await()
        {
            string sourceCode = @"
class C
{
    public async Task<dynamic> M()
    {
        dynamic d = null;
        var x1 = /*<bind>*/await d/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.True(semanticInfo.ConvertedType.IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void BinaryOperators()
        {
            foreach (var op in new[] { "*", "/", "%", "+", "-", "<<", ">>", "<", ">", "<=", ">=", "!=", "==", "^", "&", "|", "&&", "||" })
            {
                string sourceCode = @"
class C
{
    public void TestMeth()
    {
        dynamic d = null;
        var x1 = /*<bind>*/d" + op + @"d/*</bind>*/;
    }
}
";
                var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

                Assert.True(semanticInfo.Type.IsDynamic());
                Assert.True(semanticInfo.ConvertedType.IsDynamic());
                Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

                if (op == "&&" || op == "||")
                {
                    Assert.Null(semanticInfo.Symbol);
                }
                else
                {
                    Assert.Equal("dynamic.operator " + op + "(dynamic, dynamic)", semanticInfo.Symbol.ToString());
                }

                Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
                Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
                Assert.Equal(0, semanticInfo.MethodGroup.Length);
                Assert.False(semanticInfo.IsCompileTimeConstant);
            }
        }

        [Fact]
        public void NullCoalescing()
        {
            string sourceCode = @"
class C
{
    public void TestMeth()
    {
        dynamic d = null;
        var x1 = /*<bind>*/d ?? d/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.True(semanticInfo.ConvertedType.IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);

            // not a late bound operation
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);

            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ConditionalExpression_DynamicCondition()
        {
            string sourceCode = @"
class C
{
    public void TestMeth()
    {
        dynamic d = null;
        var x1 = /*<bind>*/d ? d : d/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.True(semanticInfo.ConvertedType.IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void ConditionalExpression_StaticCondition()
        {
            string sourceCode = @"
class C
{
    public void TestMeth()
    {
        bool s = true;
        dynamic d = null;
        var x1 = /*<bind>*/s ? d : d/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.True(semanticInfo.Type.IsDynamic());
            Assert.True(semanticInfo.ConvertedType.IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Null(semanticInfo.Symbol);
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(0, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void CompoundAssignment()
        {
            foreach (var op in new[] { "+=", "%=", "+=", "-=", "<<=", ">>=", "^=", "&=", "|=" })
            {
                string sourceCode = @"
class C
{
    public void M()
    {
        dynamic d = null;
        /*<bind>*/d" + op + @"d/*</bind>*/;
    }
}
";
                var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

                Assert.True(semanticInfo.Type.IsDynamic());
                Assert.True(semanticInfo.ConvertedType.IsDynamic());
                Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

                Assert.Equal("dynamic.operator " + op.Substring(0, op.Length - 1) + "(dynamic, dynamic)", semanticInfo.Symbol.ToString());
                Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
                Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
                Assert.Equal(0, semanticInfo.MethodGroup.Length);
                Assert.False(semanticInfo.IsCompileTimeConstant);
            }
        }

        [Fact]
        public void EventOperators()
        {
            foreach (var op in new[] { "+=", "-=" })
            {
                string sourceCode = @"
class C
{
    public event System.Action E;

    public void M()
    {
        dynamic d = null;
        /*<bind>*/E" + op + @"d/*</bind>*/;
    }
}
";
                var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

                Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString());
                Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString());
                Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

                Assert.Equal(op == "+=" ? "void C.E.add" : "void C.E.remove", semanticInfo.Symbol.ToTestDisplayString());
                Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
                Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
                Assert.Equal(0, semanticInfo.MethodGroup.Length);
                Assert.False(semanticInfo.IsCompileTimeConstant);
            }
        }
    }
}
