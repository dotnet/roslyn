﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Test our handling of the Color Color problem (spec section 7.6.4.1).
    /// </summary>
    public class ColorColorTests : SemanticModelTestBase
    {
        #region LHS kinds

        [Fact]
        public void TestPropertyOnLeft()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E E { get; set; }

    void M()
    {
        /*<bind>*/E/*</bind>*/.M(1);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Property, "E F.E { get; set; }",
                SymbolKind.Method, "void E.M(System.Int32 x)");
        }

        [Fact]
        public void TestFieldOnLeft()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E E;

    void M()
    {
        /*<bind>*/E/*</bind>*/.M(1);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Field, "E F.E",
                SymbolKind.Method, "void E.M(System.Int32 x)",
                // (10,14): warning CS0649: Field 'F.E' is never assigned to, and will always have its default value null
                //     public E E;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "E").WithArguments("F.E", "null")
            );
        }

        [Fact]
        public void TestFieldLikeEventOnLeft()
        {
            var text = @"
delegate void E(int x);

class F
{
    public event E E;

    void M()
    {
        /*<bind>*/E/*</bind>*/.Invoke(1);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Event, "event E F.E",
                SymbolKind.Method, "void E.Invoke(System.Int32 x)");
        }

        [Fact]
        public void TestParameterOnLeft()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    void M(E E)
    {
        /*<bind>*/E/*</bind>*/.M(1);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Parameter, "E E",
                SymbolKind.Method, "void E.M(System.Int32 x)");
        }

        [Fact]
        public void TestLocalOnLeft()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    void M()
    {
        E E = null;
        /*<bind>*/E/*</bind>*/.M(1);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Local, "E E",
                SymbolKind.Method, "void E.M(System.Int32 x)");
        }

        [WorkItem(542642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542642")]
        [Fact]
        public void TestNonInitializedLocalOnLeft()
        {
            var text = @"
class Color
{
    static void Main()
    {
        Color Color;
        Color.Equals(1, 1);
    }
}
";
            var comp = CreateStandardCompilation(text);

            comp.VerifyDiagnostics(
                    // Dev10 does not give a warning about unused variable. Should we?
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Color").WithArguments("Color"));
        }

        #endregion LHS kinds

        #region RHS kinds

        [Fact]
        public void TestPropertyOnRight()
        {
            var text = @"
class E
{
    public int P { get; set; }
}

class F
{
    public E E;

    void M()
    {
        int f = /*<bind>*/E/*</bind>*/.P;
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Field, "E F.E",
                SymbolKind.Property, "System.Int32 E.P { get; set; }",
                // (9,14): warning CS0649: Field 'F.E' is never assigned to, and will always have its default value null
                //     public E E;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "E").WithArguments("F.E", "null")
            );
        }

        [Fact]
        public void TestFieldOnRight()
        {
            var text = @"
class E
{
    public int F;
}

class F
{
    public E E;

    void M()
    {
        int f = /*<bind>*/E/*</bind>*/.F;
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Field, "E F.E",
                SymbolKind.Field, "System.Int32 E.F",
                // (4,16): warning CS0649: Field 'E.F' is never assigned to, and will always have its default value 0
                //     public int F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("E.F", "0"),
                // (9,14): warning CS0649: Field 'F.E' is never assigned to, and will always have its default value null
                //     public E E;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "E").WithArguments("F.E", "null")
            );
        }

        [Fact]
        public void TestEventOnRight()
        {
            var text = @"
class E
{
    public event System.Action Event;
}

class F
{
    public E E;

    void M()
    {
        /*<bind>*/E/*</bind>*/.Event += null;
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Field, "E F.E",
                SymbolKind.Event, "event System.Action E.Event",
                // (4,32): warning CS0067: The event 'E.Event' is never used
                //     public event System.Action Event;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Event").WithArguments("E.Event"),
                // (9,14): warning CS0649: Field 'F.E' is never assigned to, and will always have its default value null
                //     public E E;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "E").WithArguments("F.E", "null")
            );
        }

        [Fact]
        public void TestEnumFieldOnRight()
        {
            var text = @"
enum E
{
    Element,
}

class F
{
    public E E;

    void M(E e)
    {
        M(/*<bind>*/E/*</bind>*/.Element);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.NamedType, "E",
                SymbolKind.Field, "E.Element",
                // (9,14): warning CS0649: Field 'F.E' is never assigned to, and will always have its default value 
                //     public E E;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "E").WithArguments("F.E", "")
            );
        }

        [WorkItem(542407, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542407")]
        [Fact]
        public void TestClassOnRight()
        {
            var text = @"
class C
{
    public class Inner
    {
        public static C M() { return null; }
    }
}

class F
{
    public C C;

    void M(C c)
    {
        M(/*<bind>*/C/*</bind>*/.Inner.M());
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.NamedType, "C",
                SymbolKind.NamedType, "C.Inner",
                // (12,14): warning CS0649: Field 'F.C' is never assigned to, and will always have its default value null
                //     public C C;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "C").WithArguments("F.C", "null")
            );
        }

        [Fact]
        public void TestMethodGroupOnRight()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E E;

    void M()
    {
        System.Action<int> f = /*<bind>*/E/*</bind>*/.M;
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Field, "E F.E",
                SymbolKind.Method, "void E.M(System.Int32 x)",
                // (10,14): warning CS0649: Field 'F.E' is never assigned to, and will always have its default value null
                //     public E E;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "E").WithArguments("F.E", "null")
                );
        }

        [Fact]
        public void TestGenericMethodOnRight()
        {
            var text = @"
class E
{
    public void M<T>(T x) { }
    public static void M<T>(params T[] a) { }
}

class F
{
    public E E;

    void M()
    {
        /*<bind>*/E/*</bind>*/.M<int>(1);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Field, "E F.E",
                SymbolKind.Method, "void E.M<System.Int32>(System.Int32 x)",
                // (10,14): warning CS0649: Field 'F.E' is never assigned to, and will always have its default value null
                //     public E E;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "E").WithArguments("F.E", "null")
            );
        }

        [Fact]
        public void TestGenericMethodGroupOnRight()
        {
            var text = @"
class E
{
    public void M<T>(T x) { }
    public static void M<T>(params T[] a) { }
}

class F
{
    public E E;

    void M()
    {
        System.Action<bool> f = /*<bind>*/E/*</bind>*/.M;
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Field, "E F.E",
                SymbolKind.Method, "void E.M<System.Boolean>(System.Boolean x)",
                // (10,14): warning CS0649: Field 'F.E' is never assigned to, and will always have its default value null
                //     public E E;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "E").WithArguments("F.E", "null")
            );
        }

        [Fact]
        public void TestExtensionMethodOnRight()
        {
            var text = @"
class E
{
}

static class Extensions
{
    public static void M(this E e, int x) { }
    public static void M(this E e, params int[] a) { }
}

class F
{
    public E E;

    void M()
    {
        /*<bind>*/E/*</bind>*/.M(1);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Field, "E F.E",
                SymbolKind.Method, "void E.M(System.Int32 x)",
                // (14,14): warning CS0649: Field 'F.E' is never assigned to, and will always have its default value null
                //     public E E;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "E").WithArguments("F.E", "null")
            );
        }

        [Fact]
        public void TestExtensionMethodGroupOnRight()
        {
            var text = @"
class E
{
}

static class Extensions
{
    public static void M(this E e, int x) { }
    public static void M(this E e, params int[] a) { }
}

class F
{
    public E E;

    void M()
    {
        System.Action<int> f = /*<bind>*/E/*</bind>*/.M;
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Field, "E F.E",
                SymbolKind.Method, "void E.M(System.Int32 x)",
                // (14,14): warning CS0649: Field 'F.E' is never assigned to, and will always have its default value null
                //     public E E;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "E").WithArguments("F.E", "null")
            );
        }

        #endregion RHS kinds

        #region Aliases

        [Fact]
        public void TestAliasMemberNameMatchesDefinition1()
        {
            var text = @"
using Q = E;

class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public Q E { get; set; }

    void M()
    {
        /*<bind>*/E/*</bind>*/.M(1);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Property, "E F.E { get; set; }",
                SymbolKind.Method, "void E.M(System.Int32 x)");
        }

        [Fact]
        public void TestAliasMemberNameMatchesDefinition2()
        {
            var text = @"
using Q = E;

class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public Q E { get; set; }

    void M()
    {
        /*<bind>*/E/*</bind>*/.M(1, 2);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.NamedType, "E",
                SymbolKind.Method, "void E.M(params System.Int32[] a)");
        }

        [Fact]
        public void TestAliasMemberNameMatchesAlias1()
        {
            var text = @"
using Q = E;

class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E Q { get; set; }

    void M()
    {
        /*<bind>*/Q/*</bind>*/.M(1);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Property, "E F.Q { get; set; }",
                SymbolKind.Method, "void E.M(System.Int32 x)");
        }

        [Fact]
        public void TestAliasMemberNameMatchesAlias2()
        {
            var text = @"
using Q = E;

class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E Q { get; set; }

    void M()
    {
        /*<bind>*/Q/*</bind>*/.M(1, 2);
    }
}
";
            // Can't use CheckExpressionAndParent because we're using alias.

            var tree = Parse(text);
            var comp = CreateStandardCompilation(tree, new[] { TestReferences.NetFx.v4_0_30319.System_Core });
            var model = comp.GetSemanticModel(tree);

            var expr = (IdentifierNameSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var alias = model.GetAliasInfo(expr);
            Assert.Equal(SymbolKind.Alias, alias.Kind);
            Assert.Equal("Q=E", alias.ToTestDisplayString());

            var parentExpr = (ExpressionSyntax)expr.Parent;
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, parentExpr.Kind());
            var parentInfo = model.GetSymbolInfo(parentExpr);
            Assert.NotNull(parentInfo);
            Assert.Equal(SymbolKind.Method, parentInfo.Symbol.Kind);
            Assert.Equal("void E.M(params System.Int32[] a)", parentInfo.Symbol.ToTestDisplayString());
        }

        #endregion Aliases

        [WorkItem(864605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864605")]
        [Fact]
        public void TestTypeOrValueInMethodGroupIsExpression()
        {
            var text = @"
class Color
{
    public void M() { }
    public static void M(int x) { }
}

class C
{
    void M()
    {
        Color Color = null;
        System.Console.WriteLine(/*<bind>*/Color/*</bind>*/.M is object);
    }
}
";
            var tree = Parse(text);

            var comp = CreateStandardCompilation(tree);
            comp.VerifyDiagnostics(
                // (13,44): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         System.Console.WriteLine(/*<bind>*/Color/*</bind>*/.M is object);
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "Color/*</bind>*/.M is object").WithLocation(13, 44));

            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind());
            var info = model.GetSymbolInfo(expr);
            Assert.NotNull(info);
            Assert.Equal(SymbolKind.Local, info.Symbol.Kind);
            Assert.Equal("Color Color", info.Symbol.ToTestDisplayString());

            var parentExpr = (ExpressionSyntax)expr.Parent;
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, parentExpr.Kind());
            var parentInfo = model.GetSymbolInfo(parentExpr);
            Assert.NotNull(parentInfo);
            Assert.Null(parentInfo.Symbol); // the lexically first matching method
            Assert.Equal(2, parentInfo.CandidateSymbols.Length);
            Assert.Equal("void Color.M()", parentInfo.CandidateSymbols[0].ToTestDisplayString());
            Assert.Equal("void Color.M(System.Int32 x)", parentInfo.CandidateSymbols[1].ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, parentInfo.CandidateReason);
        }

        [Fact]
        public void TestInUnboundLambdaValue()
        {
            var text = @"
using System;

class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    void M()
    {
        var E = new E();
        Action<Action<int>> a = f =>
        {
            f = /*<bind>*/E/*</bind>*/.M;
        };
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Local, "E E",
                SymbolKind.Method, "void E.M(System.Int32 x)");
        }

        [Fact]
        public void TestInUnboundLambdaType()
        {
            var text = @"
using System;

class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    void M()
    {
        var E = new E();
        Action<Action<int[]>> a = f =>
        {
            f = /*<bind>*/E/*</bind>*/.M;
        };
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.NamedType, "E",
                SymbolKind.Method, "void E.M(params System.Int32[] a)");
        }

        [Fact]
        public void StackOverflow01()
        {
            var text = @"using System.Linq;
class Program
{
    public static void Main(string[] args)
    {
        var x = /*<bind>*/args.Select/*</bind>*/(a => x.
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlibAndSystemCore(new[] { tree });
            var model = comp.GetSemanticModel(tree);
            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var info = model.GetSymbolInfo(expr);
            Assert.NotNull(info);
        }

        [WorkItem(542642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542642")]
        [Fact]
        public void TestAliasNameCollisionMemberNameMatchesAlias01()
        {
            var text = @"
using Q = E;

class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E Q { get; set; }

    void M()
    {
        /*<bind>*/Q/*</bind>*/.M(1);
        {
            Q E;
            E Q;
        }
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Property, "E F.Q { get; set; }",
                SymbolKind.Method, "void E.M(System.Int32 x)",
    // (18,15): warning CS0168: The variable 'E' is declared but never used
    //             Q E;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "E").WithArguments("E").WithLocation(18, 15),
    // (19,15): warning CS0168: The variable 'Q' is declared but never used
    //             E Q;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Q").WithArguments("Q").WithLocation(19, 15)
);
        }

        [WorkItem(542642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542642")]
        [Fact]
        public void TestAliasNameCollisionMemberNameMatchesAlias02()
        {
            var text = @"
using Q = E;

class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E Q { get; set; }

    void M()
    {
        /*<bind>*/Q/*</bind>*/.M(1, 2);
        {
            Q E;
            E Q;
        }
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.NamedType, "E",
                SymbolKind.Method, "void E.M(params System.Int32[] a)",
    // (18,15): warning CS0168: The variable 'E' is declared but never used
    //             Q E;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "E").WithArguments("E").WithLocation(18, 15),
    // (19,15): warning CS0168: The variable 'Q' is declared but never used
    //             E Q;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Q").WithArguments("Q").WithLocation(19, 15)
);
        }

        [WorkItem(542642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542642")]
        [Fact]
        public void TestAliasNameCollisionMemberNameMatchesDefinition01()
        {
            var text = @"
using Q = E;

class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public Q E { get; set; }

    void M()
    {
        /*<bind>*/E/*</bind>*/.M(1);
        {
            Q E;
            E Q;
        }
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Property, "E F.E { get; set; }",
                SymbolKind.Method, "void E.M(System.Int32 x)",
    // (18,15): warning CS0168: The variable 'E' is declared but never used
    //             Q E;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "E").WithArguments("E").WithLocation(18, 15),
    // (19,15): warning CS0168: The variable 'Q' is declared but never used
    //             E Q;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Q").WithArguments("Q").WithLocation(19, 15)
);
        }

        [WorkItem(542642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542642")]
        [Fact]
        public void TestAliasNameCollisionMemberNameMatchesDefinition02()
        {
            var text = @"
using Q = E;

class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public Q E { get; set; }

    void M()
    {
        /*<bind>*/E/*</bind>*/.M(1, 2);
        {
            Q E;
            E Q;
        }
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.NamedType, "E",
                SymbolKind.Method, "void E.M(params System.Int32[] a)",
    // (18,15): warning CS0168: The variable 'E' is declared but never used
    //             Q E;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "E").WithArguments("E").WithLocation(18, 15),
    // (19,15): warning CS0168: The variable 'Q' is declared but never used
    //             E Q;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Q").WithArguments("Q").WithLocation(19, 15)
);
        }

        [Fact]
        public void TestAliasNameCollisionWithParameter1()
        {
            var text = @"
using U = C;

class C
{
    void Instance(U U)
    {
        /*<bind>*/U/*</bind>*/.Static(); //Formerly CS7040 (bug)
    }

    static void Static()
    {
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.NamedType, "C",
                SymbolKind.Method, "void C.Static()");
        }

        [Fact]
        public void TestAliasNameCollisionWithParameter2()
        {
            var text = @"
using U = C;

class C
{
    void Instance(U U)
    {
        U.Instance(null);
        /*<bind>*/U/*</bind>*/.Static(); //Formerly CS7039 (bug)
    }

    static void Static()
    {
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.NamedType, "C",
                SymbolKind.Method, "void C.Static()");
        }

        [WorkItem(542642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542642")]
        [Fact]
        public void LambdaConversion()
        {
            var text = @"
using System; 

enum Color { Red }
class Bravo { public static Color Red { get; set; } }
class C
{
    static void X(Action<Color> f) {}
    static void X(Action<Bravo> f) { }
    static void Main()
    {
        X(Color => Console.WriteLine(""{0}{1}"", /*<bind>*/Color/*</bind>*/.Red, Color.ToString()));
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.NamedType, "Color",
                SymbolKind.Field, "Color.Red");
        }

        [WorkItem(9715, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void GenericTypeOk()
        {
            var text =
@"struct var<T>
{
    public static T field;
}
class Program
{
    static void Main(string[] args)
    {
        var var = ""A"";
        var xs = var<int>.field;
    }
}";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (9,13): warning CS0219: The variable 'var' is assigned but its value is never used
                //         var var = "A";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "var").WithArguments("var"),
                // (3,21): warning CS0649: Field 'var<T>.field' is never assigned to, and will always have its default value 
                //     public static T field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("var<T>.field", "")
                );
        }

        [WorkItem(543551, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543551")]
        [Fact]
        public void FieldOfEnumType()
        {
            var text =
@"
enum DayOfWeek { Monday }
class C
{
    public static DayOfWeek DayOfWeek = DayOfWeek.Monday;
 
    public static void Main(string[] args)
    {
        switch (DayOfWeek)
        {
            case DayOfWeek.Monday:
                break;
        }
    }
}";
            CreateStandardCompilation(text).VerifyDiagnostics();
        }

        [WorkItem(531386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531386")]
        [Fact]
        public void AlternateTypeAndVariable()
        {
            var text =
@"class Color
{
    public static int X = 0;
}

public class Program
{
    static void Main(string[] args)
    {
        Color Color = null;       // variable
        {
            var tmp = Color.X;    // type, disambiguated using color-color rule
            {
                object x = Color; // variable
            }
        }
    }
}";
            CreateStandardCompilation(text).VerifyDiagnostics();
        }

        #region Error cases

        [Fact]
        public void TestErrorLookup()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E E { get; set; }

    void M()
    {
        /*<bind>*/E/*</bind>*/.Q(1, 2);
    }
}
";
            var tree = Parse(text);

            var comp = CreateStandardCompilation(tree);
            comp.VerifyDiagnostics(
            // (14,32): error CS1061: 'E' does not contain a definition for 'Q' and no extension method 'Q' accepting a first argument of type 'E' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Q").WithArguments("E", "Q"));

            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind());
            var info = model.GetSymbolInfo(expr);
            Assert.NotNull(info);
            Assert.Equal(SymbolKind.Property, info.Symbol.Kind);
            Assert.Equal("E F.E { get; set; }", info.Symbol.ToTestDisplayString());

            var parentExpr = (ExpressionSyntax)expr.Parent;
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, parentExpr.Kind());
            var parentInfo = model.GetSymbolInfo(parentExpr);
            Assert.NotNull(parentInfo);
            Assert.Null(parentInfo.Symbol);
            Assert.Equal(CandidateReason.None, parentInfo.CandidateReason);
            Assert.Equal(0, parentInfo.CandidateSymbols.Length);
        }

        [Fact]
        public void TestErrorLookupMethodGroup()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E E { get; set; }

    void M()
    {
        System.Action<int> f = /*<bind>*/E/*</bind>*/.Q;
    }
}
";
            var tree = Parse(text);

            var comp = CreateStandardCompilation(tree, new[] { TestReferences.NetFx.v4_0_30319.System_Core });
            comp.VerifyDiagnostics(
            // (14,32): error CS1061: 'E' does not contain a definition for 'Q' and no extension method 'Q' accepting a first argument of type 'E' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Q").WithArguments("E", "Q"));

            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind());
            var info = model.GetSymbolInfo(expr);
            Assert.NotNull(info);
            Assert.Equal(SymbolKind.Property, info.Symbol.Kind);
            Assert.Equal("E F.E { get; set; }", info.Symbol.ToTestDisplayString());

            var parentExpr = (ExpressionSyntax)expr.Parent;
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, parentExpr.Kind());
            var parentInfo = model.GetSymbolInfo(parentExpr);
            Assert.NotNull(parentInfo);
            Assert.Null(parentInfo.Symbol);
            Assert.Equal(CandidateReason.None, parentInfo.CandidateReason);
            Assert.Equal(0, parentInfo.CandidateSymbols.Length);
        }

        [Fact]
        public void TestErrorOverloadResolution()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E E { get; set; }

    void M()
    {
        /*<bind>*/E/*</bind>*/.M(""Hello"");
    }
}
";
            var tree = Parse(text);

            var comp = CreateStandardCompilation(tree);
            comp.VerifyDiagnostics(
            // (14,34): error CS1503: Argument 1: cannot convert from 'string' to 'int'
                Diagnostic(ErrorCode.ERR_BadArgType, @"""Hello""").WithArguments("1", "string", "int"));

            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind());
            var info = model.GetSymbolInfo(expr);
            Assert.NotNull(info);
            Assert.Equal(SymbolKind.Property, info.Symbol.Kind);
            Assert.Equal("E F.E { get; set; }", info.Symbol.ToTestDisplayString());

            var parentExpr = (ExpressionSyntax)expr.Parent;
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, parentExpr.Kind());
            var parentInfo = model.GetSymbolInfo(parentExpr);
            Assert.NotNull(parentInfo);
            Assert.Null(parentInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, parentInfo.CandidateReason);
            Assert.Equal(2, parentInfo.CandidateSymbols.Length);
        }

        [Fact]
        public void TestErrorOverloadResolutionMethodGroup()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E E { get; set; }

    void M()
    {
        System.Action<string> f = /*<bind>*/E/*</bind>*/.M;
    }
}
";
            var tree = Parse(text);

            var comp = CreateStandardCompilation(tree, new[] { TestReferences.NetFx.v4_0_30319.System_Core });
            comp.VerifyDiagnostics(
            // (14,58): error CS0123: No overload for 'M' matches delegate 'System.Action<string>'
                Diagnostic(ErrorCode.ERR_MethDelegateMismatch, "M").WithArguments("M", "System.Action<string>"));

            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind());
            var info = model.GetSymbolInfo(expr);
            Assert.NotNull(info);
            Assert.Equal(SymbolKind.Property, info.Symbol.Kind);
            Assert.Equal("E F.E { get; set; }", info.Symbol.ToTestDisplayString());

            var parentExpr = (ExpressionSyntax)expr.Parent;
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, parentExpr.Kind());
            var parentInfo = model.GetSymbolInfo(parentExpr);
            Assert.NotNull(parentInfo);
            Assert.Null(parentInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, parentInfo.CandidateReason);
            Assert.Equal(2, parentInfo.CandidateSymbols.Length);
        }

        [Fact]
        public void TestErrorNonFieldLikeEvent()
        {
            var text = @"
delegate void E();

class F
{
    public event E E { add { } remove { } }

    void M()
    {
        /*<bind>*/E/*</bind>*/.Invoke();
    }
}
";
            var tree = Parse(text);

            var comp = CreateStandardCompilation(tree);
            comp.VerifyDiagnostics(
            // (10,19): error CS0079: The event 'F.E' can only appear on the left hand side of += or -=
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "E").WithArguments("F.E"));

            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind());
            var info = model.GetSymbolInfo(expr);
            Assert.NotNull(info);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.NotAValue, info.CandidateReason);
            var candidate = info.CandidateSymbols.Single();
            Assert.Equal(SymbolKind.Event, candidate.Kind);
            Assert.Equal("event E F.E", candidate.ToTestDisplayString());

            var parentExpr = (ExpressionSyntax)expr.Parent;
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, parentExpr.Kind());
            var parentInfo = model.GetSymbolInfo(parentExpr);
            Assert.NotNull(parentInfo);
            Assert.Equal(WellKnownMemberNames.DelegateInvokeName, parentInfo.Symbol.Name); // Succeeded even though the receiver has an error.
        }

        [Fact]
        public void TestInEnumDecl()
        {
            var text = @"
enum Color
{
    Color, //inside the decl, this has type int so there is no Color Color Issue
    Blue,
    Navy = /*<bind>*/Color/*</bind>*/.Blue,
}
";
            var tree = Parse(text);

            var comp = CreateStandardCompilation(tree);
            comp.VerifyDiagnostics(
            // (6,18): error CS1061: 'int' does not contain a definition for 'Blue' and no extension method 'Blue' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Blue").WithArguments("int", "Blue"));

            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind());
            var info = model.GetSymbolInfo(expr);
            Assert.NotNull(info);
            Assert.Equal(SymbolKind.Field, info.Symbol.Kind);
            Assert.Equal("Color.Color", info.Symbol.ToTestDisplayString());

            var parentExpr = (ExpressionSyntax)expr.Parent;
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, parentExpr.Kind());
            var parentInfo = model.GetSymbolInfo(parentExpr);
            Assert.NotNull(parentInfo);
            Assert.Null(parentInfo.Symbol);
            Assert.Equal(CandidateReason.None, parentInfo.CandidateReason);
            Assert.Equal(0, parentInfo.CandidateSymbols.Length);
        }

        [WorkItem(542586, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542586")]
        [Fact]
        public void TestNestedNameCollisionType()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E E { get; set; }

    void M()
    {
        /*<bind>*/E/*</bind>*/.M(1, 2);
        {
            E E;
        }
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.NamedType, "E",
                SymbolKind.Method, "void E.M(params System.Int32[] a)",
    // (16,15): warning CS0168: The variable 'E' is declared but never used
    //             E E;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "E").WithArguments("E").WithLocation(16, 15)
);
        }

        [WorkItem(542586, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542586")]
        [Fact]
        public void TestNestedNameCollisionType02()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

delegate D D(E x);

class F
{
    void M()
    {
        /*<bind>*/E/*</bind>*/.M(1, 2);
        D d = (E E) => null;
    }
}";
            CheckExpressionAndParent(text,
                SymbolKind.NamedType, "E",
                SymbolKind.Method, "void E.M(params System.Int32[] a)");
        }

        [WorkItem(542586, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542586")]
        [Fact]
        public void TestNestedNameCollisionValue()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    public E E { get; set; }

    void M()
    {
        /*<bind>*/E/*</bind>*/.M(1);
        {
            E E;
        }
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Property, "E F.E { get; set; }",
                SymbolKind.Method, "void E.M(System.Int32 x)",
    // (16,15): warning CS0168: The variable 'E' is declared but never used
    //             E E;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "E").WithArguments("E").WithLocation(16, 15)
);
        }

        [WorkItem(542642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542642")]
        [Fact]
        public void TestNameCollisionType()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{
    void M()
    {
        E E = null;
        /*<bind>*/E/*</bind>*/.M(1, 2);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.NamedType, "E",
                SymbolKind.Method, "void E.M(params System.Int32[] a)",
                // (12,11): warning CS0219: The variable 'E' is assigned but its value is never used
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "E").WithArguments("E"));
        }

        [WorkItem(542642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542642")]
        [Fact]
        public void TestNameCollisionValue()
        {
            var text = @"
class E
{
    public void M(int x) { }
    public static void M(params int[] a) { }
}

class F
{

    void M()
    {
        E E = null;
        /*<bind>*/E/*</bind>*/.M(1);
    }
}
";
            CheckExpressionAndParent(text,
                SymbolKind.Local, "E E",
                SymbolKind.Method, "void E.M(System.Int32 x)");
        }

        [WorkItem(542039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542039")]
        [Fact]
        public void FieldAndMethodSameName()
        {
            var text =
@"class A
{
    delegate void D();
    static void Foo() { }
    class B
    {
        const int Foo = 123;
        static void Main()
        {
            Foo();
            Bar(Foo);
        }
        static void Bar(int x) { }
        static void Bar(D x) { }
    }
}";
            CreateStandardCompilation(text).VerifyDiagnostics();
        }

        [WorkItem(542039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542039")]
        [Fact]
        public void TypeAndMethodSameName()
        {
            var text =
@"class A
{
    static void Foo(object x) { }
    class B
    {
        class Foo
        {
            public const int N = 3;
        }
        static void Main()
        {
            Foo(Foo.N);
        }
        static void Bar(int x) { }
    }
}";
            CreateStandardCompilation(text).VerifyDiagnostics();
        }

        #endregion Error cases

        #region Regression cases

        [WorkItem(546427, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546427")]
        [Fact]
        public void ExtensionMethodWithColorColorReceiver()
        {
            var text = @"
using System;

static class Test
{
    public static void ExtensionMethod(this object arg)
    {
    }

    static void Main()
    {
        Int32 Int32 = 0;
        Int32.ExtensionMethod(); // should box
        ExtensionMethod(Int32); // should box

        Int32 OtherName = 0;
        OtherName.ExtensionMethod(); // should box
        ExtensionMethod(OtherName); // should box
    }
}
";

            var comp = CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.DebugExe);
            CompileAndVerify(comp).VerifyIL("Test.Main", @"
{
  // Code size       54 (0x36)
  .maxstack  1
  .locals init (int V_0, //Int32
                int V_1) //OtherName
  IL_0000:  nop
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  box        ""int""
  IL_0009:  call       ""void Test.ExtensionMethod(object)""
  IL_000e:  nop
  IL_000f:  ldloc.0
  IL_0010:  box        ""int""
  IL_0015:  call       ""void Test.ExtensionMethod(object)""
  IL_001a:  nop
  IL_001b:  ldc.i4.0
  IL_001c:  stloc.1
  IL_001d:  ldloc.1
  IL_001e:  box        ""int""
  IL_0023:  call       ""void Test.ExtensionMethod(object)""
  IL_0028:  nop
  IL_0029:  ldloc.1
  IL_002a:  box        ""int""
  IL_002f:  call       ""void Test.ExtensionMethod(object)""
  IL_0034:  nop
  IL_0035:  ret
}
");
        }

        [WorkItem(938389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/938389")]
        [Fact]
        public void ShadowedTypeReceiver_1()
        {
            const string source1 = @"
namespace Foo
{
    public class A { public static int I { get { return -42; } } }
}";

            const string source2 = @"
namespace Foo
{
    public class A { public static int I { get { return 42; } } }

    class C
    {
        static A A { get { return new A(); } }

        static void Main()
        {
            System.Console.WriteLine(A.I);
        }
    }
}";

            var comp1 = CreateStandardCompilation(source1, options: TestOptions.ReleaseDll, assemblyName: System.Guid.NewGuid().ToString());
            var ref1 = MetadataReference.CreateFromStream(comp1.EmitToStream());
            var refIdentity = ((AssemblyMetadata)ref1.GetMetadataNoCopy()).GetAssembly().Identity.ToString();
            CompileAndVerify(source2, new[] { ref1 }, expectedOutput: "42").VerifyDiagnostics(
                // (8,16): warning CS0436: The type 'A' in '' conflicts with the imported type 'A' in '04f2260a-2ee6-4e74-938a-c47b6dc61d9c, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         static A A { get { return null; } }
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "A").WithArguments("", "Foo.A", refIdentity, "Foo.A").WithLocation(8, 16),
                // (8,39): warning CS0436: The type 'A' in '' conflicts with the imported type 'A' in '59c700fa-e88d-45e4-acec-fd0bae894f9d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         static A A { get { return new A(); } }
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "A").WithArguments("", "Foo.A", refIdentity, "Foo.A").WithLocation(8, 39),
                // (12,38): warning CS0436: The type 'A' in '' conflicts with the imported type 'A' in '04f2260a-2ee6-4e74-938a-c47b6dc61d9c, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //             System.Console.WriteLine(A.I);
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "A").WithArguments("", "Foo.A", refIdentity, "Foo.A").WithLocation(12, 38));
        }

        [WorkItem(938389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/938389")]
        [Fact]
        public void ShadowedTypeReceiver_2()
        {
            const string source1 = @"
namespace Foo
{
    public class A { public int I { get { return -42; } } }
}";

            const string source2 = @"
namespace Foo
{
    public class A { public int I { get { return 42; } } }

    class C
    {
        static A A { get { return new A(); } }

        static void Main()
        {
            System.Console.WriteLine(A.I);
        }
    }
}";

            var comp1 = CreateStandardCompilation(source1, options: TestOptions.ReleaseDll, assemblyName: System.Guid.NewGuid().ToString());
            var ref1 = MetadataReference.CreateFromStream(comp1.EmitToStream());
            var refIdentity = ((AssemblyMetadata)ref1.GetMetadataNoCopy()).GetAssembly().Identity.ToString();
            CompileAndVerify(source2, new[] { ref1 }, expectedOutput: "42").VerifyDiagnostics(
                // (8,16): warning CS0436: The type 'A' in '' conflicts with the imported type 'A' in '59c700fa-e88d-45e4-acec-fd0bae894f9d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         static A A { get { return new A(); } }
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "A").WithArguments("", "Foo.A", refIdentity, "Foo.A").WithLocation(8, 16),
                // (8,39): warning CS0436: The type 'A' in '' conflicts with the imported type 'A' in '59c700fa-e88d-45e4-acec-fd0bae894f9d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         static A A { get { return new A(); } }
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "A").WithArguments("", "Foo.A", refIdentity, "Foo.A").WithLocation(8, 39));
        }

        [WorkItem(938389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/938389")]
        [Fact]
        public void ShadowedTypeReceiver_3()
        {
            const string source1 = @"
namespace Foo
{
    public class A { public int I { get { return -42; } } }
}";

            const string source2 = @"
namespace Foo
{
    public class A { public static int I { get { return 42; } } }

    class C
    {
        static A A { get { return new A(); } }

        static void Main()
        {
            System.Console.WriteLine(A.I);
        }
    }
}";

            var comp1 = CreateStandardCompilation(source1, options: TestOptions.ReleaseDll, assemblyName: System.Guid.NewGuid().ToString());
            var ref1 = MetadataReference.CreateFromStream(comp1.EmitToStream());
            var refIdentity = ((AssemblyMetadata)ref1.GetMetadataNoCopy()).GetAssembly().Identity.ToString();
            CompileAndVerify(source2, new[] { ref1 }, expectedOutput: "42").VerifyDiagnostics(
                // (8,16): warning CS0436: The type 'A' in '' conflicts with the imported type 'A' in '499975c2-0b0d-4d9b-8f1f-4d91133627db, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         static A A { get { return null; } }
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "A").WithArguments("", "Foo.A", refIdentity, "Foo.A").WithLocation(8, 16),
                // (8,39): warning CS0436: The type 'A' in '' conflicts with the imported type 'A' in '59c700fa-e88d-45e4-acec-fd0bae894f9d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         static A A { get { return new A(); } }
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "A").WithArguments("", "Foo.A", refIdentity, "Foo.A").WithLocation(8, 39),
                // (12,38): warning CS0436: The type 'A' in '' conflicts with the imported type 'A' in '499975c2-0b0d-4d9b-8f1f-4d91133627db, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //             System.Console.WriteLine(A.I);
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "A").WithArguments("", "Foo.A", refIdentity, "Foo.A").WithLocation(12, 38));
        }

        [WorkItem(938389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/938389")]
        [Fact]
        public void ShadowedTypeReceiver_4()
        {
            const string source1 = @"
namespace Foo
{
    public class A { public static int I { get { return -42; } } }
}";

            const string source2 = @"
namespace Foo
{
    public class A { public int I { get { return 42; } } }

    class C
    {
        static A A { get { return new A(); } }

        static void Main()
        {
            System.Console.WriteLine(A.I);
        }
    }
}";

            var comp1 = CreateStandardCompilation(source1, options: TestOptions.ReleaseDll, assemblyName: System.Guid.NewGuid().ToString());
            var ref1 = MetadataReference.CreateFromStream(comp1.EmitToStream());
            var refIdentity = ((AssemblyMetadata)ref1.GetMetadataNoCopy()).GetAssembly().Identity.ToString();
            CompileAndVerify(source2, new[] { ref1 }, expectedOutput: "42").VerifyDiagnostics(
                // (8,16): warning CS0436: The type 'A' in '' conflicts with the imported type 'A' in 'cb07e894-1bb8-4db2-93ba-747f45e89f22, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         static A A { get { return new A(); } }
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "A").WithArguments("", "Foo.A", refIdentity, "Foo.A").WithLocation(8, 16),
                // (8,39): warning CS0436: The type 'A' in '' conflicts with the imported type 'A' in 'cb07e894-1bb8-4db2-93ba-747f45e89f22, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'. Using the type defined in ''.
                //         static A A { get { return new A(); } }
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "A").WithArguments("", "Foo.A", refIdentity, "Foo.A").WithLocation(8, 39));
        }

        [WorkItem(1095020, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1095020")]
        [Fact]
        public void RangeVariableColorColor()
        {
            const string source = @"
using System.Linq;
using System.Collections.Generic;
 
class Program
{
    static void Main()
    {
        var q = from X X in new List<X> { new X() }
                where X.Static() // error CS0176: Member 'X.Static()' cannot be accessed with an instance reference; qualify it with a type name instead
                where X.Instance()
                select 42;
        System.Console.Write(q.Single());
    }
}
  
class X
{
    public static bool Static() { return true; }
    public bool Instance() { return true; }
}";

            var comp = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [WorkItem(5362, "https://github.com/dotnet/roslyn/issues/5362")]
        [Fact]
        public void TestColorColorSymbolInfoInArrowExpressionClauseSyntax()
        {
            const string source = @"public enum Lifetime
{
    Persistent,
    Transient,
    Scoped
}
public class Example
{
    public Lifetime Lifetime => Lifetime.Persistent;
    //                          ^^^^^^^^
}";
            var analyzer = new ColorColorSymbolInfoInArrowExpressionClauseSyntaxAnalyzer();
            CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseDll)
                .VerifyAnalyzerOccurrenceCount(new[] { analyzer }, 0);

            Assert.True(analyzer.ActionFired);
        }

        class ColorColorSymbolInfoInArrowExpressionClauseSyntaxAnalyzer : DiagnosticAnalyzer
        {
            public bool ActionFired { get; private set; }

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(HandleMemberAccessExpression, SyntaxKind.SimpleMemberAccessExpression);
            }

            private void HandleMemberAccessExpression(SyntaxNodeAnalysisContext context)
            {
                ActionFired = true;

                var memberAccessExpression = context.Node as MemberAccessExpressionSyntax;

                var actualSymbol = context.SemanticModel.GetSymbolInfo(memberAccessExpression.Expression);

                Assert.Equal("Lifetime", actualSymbol.Symbol.ToTestDisplayString());
                Assert.Equal(SymbolKind.NamedType, actualSymbol.Symbol.Kind);
            }
        }

        [WorkItem(5362, "https://github.com/dotnet/roslyn/issues/5362")]
        [Fact]
        public void TestColorColorSymbolInfoInArrowExpressionClauseSyntax_2()
        {
            const string source = @"public enum Lifetime
{
    Persistent,
    Transient,
    Scoped
}
public class Example
{
    public Lifetime Lifetime => Lifetime.Persistent;
    //                          ^^^^^^^^
}";
            var comp = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            var syntaxTree = comp.SyntaxTrees[0];
            var syntaxRoot = syntaxTree.GetRoot();

            var semanticModel = comp.GetSemanticModel(syntaxTree, false);

            var memberAccess = syntaxRoot.DescendantNodes().Single(node => node.IsKind(SyntaxKind.SimpleMemberAccessExpression)) as MemberAccessExpressionSyntax;
            Assert.Equal("Lifetime", memberAccess.Expression.ToString());
            Assert.Equal("Lifetime.Persistent", memberAccess.ToString());

            var actualSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression);

            Assert.Equal("Lifetime", actualSymbol.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, actualSymbol.Symbol.Kind);
        }

        #endregion Regression cases


        private void CheckExpressionAndParent(
            string text,
            SymbolKind exprSymbolKind,
            string exprDisplayString,
            SymbolKind parentSymbolKind,
            string parentDisplayString,
            params DiagnosticDescription[] expectedDiagnostics)
        {
            var tree = Parse(text);

            var comp = CreateStandardCompilation(tree, new[] { TestReferences.NetFx.v4_0_30319.System_Core });
            comp.VerifyDiagnostics(expectedDiagnostics);

            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind());
            var info = model.GetSymbolInfo(expr);
            Assert.NotNull(info);
            Assert.Equal(exprSymbolKind, info.Symbol.Kind);
            Assert.Equal(exprDisplayString, info.Symbol.ToTestDisplayString());

            var parentExpr = (ExpressionSyntax)expr.Parent;
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, parentExpr.Kind());
            var parentInfo = model.GetSymbolInfo(parentExpr);
            Assert.NotNull(parentInfo);
            Assert.Equal(parentSymbolKind, parentInfo.Symbol.Kind);
            Assert.Equal(parentDisplayString, parentInfo.Symbol.ToTestDisplayString());
        }

        [WorkItem(969006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969006")]
        [Fact]
        public void Bug969006_1()
        {
            const string source = @"
enum E
{
    A
}

class C
{
    void M()
    {
        const E E = E.A;
        var z = E;
    }
}
";
            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees[0];
            var model1 = compilation.GetSemanticModel(tree);
            var node1 = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            Assert.Equal("E.A", node1.ToString());
            Assert.Equal("E", node1.Expression.ToString());

            var symbolInfo = model1.GetSymbolInfo(node1.Expression);

            Assert.Equal("E", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, symbolInfo.Symbol.Kind);

            var model2 = compilation.GetSemanticModel(tree);
            var node2 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.Text == "E" && (n.Parent is EqualsValueClauseSyntax)).Single();

            Assert.Equal("= E", node2.Parent.ToString());

            symbolInfo = model2.GetSymbolInfo(node2);

            Assert.Equal("E E", symbolInfo.Symbol.ToTestDisplayString());

            symbolInfo = model2.GetSymbolInfo(node1.Expression);

            Assert.Equal("E", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, symbolInfo.Symbol.Kind);

            compilation.VerifyDiagnostics(
                // (12,13): warning CS0219: The variable 'z' is assigned but its value is never used
                //         var z = E;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "z").WithArguments("z").WithLocation(12, 13)
                );
        }

        [WorkItem(969006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969006"), WorkItem(1112493, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112493")]
        [Fact]
        public void Bug969006_2()
        {
            // E in "E.A" does not qualify for Color Color (and thus does not bind to the enum type) because
            // the type of the const var E is an error due to the circular reference (and thus the name of
            // the type cannot be the same as the name of the var).
            const string source = @"
enum E
{
    A
}

class C
{
    void M()
    {
        const var E = E.A;
        var z = E;
    }
}
";

            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees[0];
            var model1 = compilation.GetSemanticModel(tree);
            var node1 = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            Assert.Equal("E.A", node1.ToString());
            Assert.Equal("E", node1.Expression.ToString());

            var symbolInfo = model1.GetSymbolInfo(node1.Expression);

            Assert.Equal("? E", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, symbolInfo.Symbol.Kind);

            var model2 = compilation.GetSemanticModel(tree);
            var node2 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.Text == "E" && (n.Parent is EqualsValueClauseSyntax)).Single();

            Assert.Equal("= E", node2.Parent.ToString());

            symbolInfo = model2.GetSymbolInfo(node2);

            Assert.Equal("? E", symbolInfo.Symbol.ToTestDisplayString());

            symbolInfo = model2.GetSymbolInfo(node1.Expression);

            Assert.Equal("? E", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, symbolInfo.Symbol.Kind);

            compilation.VerifyDiagnostics(
                // (11,15): error CS0822: Implicitly-typed variables cannot be constant
                //         const var E = E.A;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableCannotBeConst, "var E = E.A").WithLocation(11, 15),
                // (11,23): error CS0841: Cannot use local variable 'E' before it is declared
                //         const var E = E.A;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "E").WithArguments("E").WithLocation(11, 23)
                );
        }

        [WorkItem(969006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969006")]
        [Fact]
        public void Bug969006_3()
        {
            const string source = @"
enum E
{
    A
}

class C
{
    void M()
    {
        E E = E.A;
        var z = E;
    }
}
";

            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees[0];
            var model1 = compilation.GetSemanticModel(tree);
            var node1 = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            Assert.Equal("E.A", node1.ToString());
            Assert.Equal("E", node1.Expression.ToString());

            var symbolInfo = model1.GetSymbolInfo(node1.Expression);

            Assert.Equal("E", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, symbolInfo.Symbol.Kind);

            var model2 = compilation.GetSemanticModel(tree);
            var node2 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.Text == "E" && (n.Parent is EqualsValueClauseSyntax)).Single();

            Assert.Equal("= E", node2.Parent.ToString());

            symbolInfo = model2.GetSymbolInfo(node2);

            Assert.Equal("E E", symbolInfo.Symbol.ToTestDisplayString());

            symbolInfo = model2.GetSymbolInfo(node1.Expression);

            Assert.Equal("E", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.NamedType, symbolInfo.Symbol.Kind);

            compilation.VerifyDiagnostics();
        }

        [WorkItem(969006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969006")]
        [Fact]
        public void Bug969006_4()
        {
            const string source = @"
enum E
{
    A
}

class C
{
    void M()
    {
        var E = E.A;
        var z = E;
    }
}
";

            var compilation = CreateStandardCompilation(source);

            var tree = compilation.SyntaxTrees[0];
            var model1 = compilation.GetSemanticModel(tree);
            var node1 = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            Assert.Equal("E.A", node1.ToString());
            Assert.Equal("E", node1.Expression.ToString());

            var symbolInfo = model1.GetSymbolInfo(node1.Expression);

            Assert.Equal("? E", symbolInfo.Symbol.ToTestDisplayString());

            var model2 = compilation.GetSemanticModel(tree);
            var node2 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.Text == "E" && (n.Parent is EqualsValueClauseSyntax)).Single();

            Assert.Equal("= E", node2.Parent.ToString());

            symbolInfo = model2.GetSymbolInfo(node2);

            Assert.Equal("? E", symbolInfo.Symbol.ToTestDisplayString());

            symbolInfo = model2.GetSymbolInfo(node1.Expression);

            Assert.Equal("? E", symbolInfo.Symbol.ToTestDisplayString());

            compilation.VerifyDiagnostics(
                // (11,17): error CS0841: Cannot use local variable 'E' before it is declared
                //         var E = E.A;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "E").WithArguments("E").WithLocation(11, 17)
                );
        }
    }
}
