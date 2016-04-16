// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(1, semanticInfo.MethodGroup.Length);
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

            Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
            Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
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

            Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
            Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
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
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);
            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);


            Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
            Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal("void C.bar(System.Int32 a)", semanticInfo.Symbol.ToTestDisplayString());

            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            Assert.False(semanticInfo.IsCompileTimeConstant);
        }

        [Fact]
        public void MethodInvocation_TypeReceiver()
        {
            string sourceCode = @"
class C
{
    public static C Create(int arg) { return null; }

    public void M(dynamic d)
    {
        /*<bind>*/C.Create(d);/*</bind>*/;
    }
}
";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(sourceCode);

            Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
            Assert.Equal("C C.Create(System.Int32 arg)", semanticInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(1, semanticInfo.MethodGroup.Length);
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
}";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(source);

            Assert.Null(semanticInfo.Type);

            // there is only one applicable candidate:
            Assert.Equal("void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)", semanticInfo.Symbol.ToTestDisplayString());

            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(2, semanticInfo.MethodGroup.Length);
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
}";
            var semanticInfo = GetSemanticInfoForTest<ExpressionSyntax>(source);

            Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());

            // there is only one applicable candidate:
            Assert.Equal("void System.Collections.Generic.List<System.Int32>.Add(System.Int32 item)", semanticInfo.Symbol.ToTestDisplayString());

            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal(1, semanticInfo.MethodGroup.Length);
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
        public dynamic OverloadedFunction(dynamic d)
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

            Assert.Equal(0, semanticInfo.CandidateSymbols.Length);
            Assert.Equal("dynamic Dynamic.FunctionTestingWithOverloading.OverloadedFunction(dynamic d)", semanticInfo.Symbol.ToTestDisplayString());

            Assert.Equal(1, semanticInfo.MethodGroup.Length);
            Assert.Equal("dynamic Dynamic.FunctionTestingWithOverloading.OverloadedFunction(dynamic d)", semanticInfo.MethodGroup.First().ToTestDisplayString());

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
        public void Indexer_StaticReceiver()
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

            Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
            Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind);

            Assert.Equal(CandidateReason.LateBound, semanticInfo.CandidateReason);
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

            Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
            Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
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

            Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
            Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
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

                Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
                Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
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

            Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
            Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
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

                Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
                Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
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

            Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
            Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
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

            Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
            Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
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

            Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
            Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
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

                Assert.True(((TypeSymbol)semanticInfo.Type).IsDynamic());
                Assert.True(((TypeSymbol)semanticInfo.ConvertedType).IsDynamic());
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
