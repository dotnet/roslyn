// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class GetSemanticInfoTests : SemanticModelTestBase
    {
        [WorkItem(544320, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544320")]
        [Fact]
        public void TestBug12592()
        {
            var text = @"
class B
{    
  public B(int x = 42) {}
}
class D : B
{
    public D(int y) : base(/*<bind>*/x/*</bind>*/: y) { }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var sym = model.GetSymbolInfo(expr);

            Assert.Equal(SymbolKind.Parameter, sym.Symbol.Kind);
            Assert.Equal("x", sym.Symbol.Name);
        }

        [WorkItem(541948, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541948")]
        [Fact]
        public void DelegateArgumentType()
        {
            var text = @"using System;
delegate void MyEvent();

class Test
{
    event MyEvent Clicked;
    void Handler() { }

    public void Run()
    {
        Test t = new Test();
        t.Clicked += new MyEvent(/*<bind>*/Handler/*</bind>*/);
    }
}
";

            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var sym = model.GetSymbolInfo(expr);
            Assert.Equal(SymbolKind.Method, sym.Symbol.Kind);

            var info = model.GetTypeInfo(expr);
            Assert.Null(info.Type);
            Assert.NotNull(info.ConvertedType);
        }

        [WorkItem(541949, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541949")]
        [Fact]
        public void LambdaWithParenthesis_BindOutsideOfParenthesis()
        {
            var text = @"using System;

public class Test
{
    delegate int D();
    static void Main(int xx)
    {
        // int x = (xx);
        D d = /*<bind>*/( delegate() { return 0; } )/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var sym = model.GetSymbolInfo(expr);
            Assert.NotNull(sym.Symbol);
            Assert.Equal(SymbolKind.Method, sym.Symbol.Kind);

            var info = model.GetTypeInfo(expr);
            var conv = model.GetConversion(expr);
            Assert.Equal(ConversionKind.AnonymousFunction, conv.Kind);
            Assert.Null(info.Type);
            Assert.NotNull(info.ConvertedType);
            Assert.Equal("Test.D", info.ConvertedType.ToTestDisplayString());
        }

        [WorkItem(529056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529056")]
        [WorkItem(529056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529056")]
        [Fact]
        public void LambdaWithParenthesis_BindInsideOfParenthesis()
        {
            var text = @"using System;

public class Test
{
    delegate int D();
    static void Main(int xx)
    {
        // int x = (xx);
        D d = (/*<bind>*/ delegate() { return 0; }/*</bind>*/) ;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var sym = model.GetSymbolInfo(expr);
            Assert.NotNull(sym.Symbol);
            Assert.Equal(SymbolKind.Method, sym.Symbol.Kind);

            var info = model.GetTypeInfo(expr);
            var conv = model.GetConversion(expr);
            Assert.Equal(ConversionKind.AnonymousFunction, conv.Kind);
            Assert.Null(info.Type);
            Assert.NotNull(info.ConvertedType);
            Assert.Equal("Test.D", info.ConvertedType.ToTestDisplayString());
        }

        [Fact, WorkItem(528656, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528656")]
        public void SemanticInfoForInvalidExpression()
        {
            var text = @"
public class A 
{
    static void Main() 
    {	
         Console.WriteLine(/*<bind>*/ delegate  * delegate /*</bind>*/);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var sym = model.GetSymbolInfo(expr);
            Assert.Null(sym.Symbol);
        }

        [WorkItem(541973, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541973")]
        [Fact]
        public void LambdaAsAttributeArgumentErr()
        {
            var text = @"using System;
delegate void D();

class MyAttr: Attribute
{
    public MyAttr(D d) { }
}

[MyAttr((D)/*<bind>*/delegate { }/*</bind>*/)] // CS0182
public class A
{ 
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var info = model.GetTypeInfo(expr);
            Assert.Null(info.Type);
            Assert.NotNull(info.ConvertedType);
        }

        [Fact]
        public void ClassifyConversionImplicit()
        {
            var text = @"using System;
enum E { One, Two, Three }
public class Test
{
    static byte[] ary;
    static int Main()
    {
        // Identity
        ary = new byte[3];
        // ImplicitConstant
        ary[0] = 0x0F;

        // ImplicitNumeric
        ushort ret = ary[0];

        // ImplicitReference
        Test obj = null;
        // Identity
        obj = new Test();

        // ImplicitNumeric
        obj.M(ary[0]);

        // boxing
        object box = -1;

        // ImplicitEnumeration
        E e = 0;

        // Identity
        E e2 = E.Two; // bind 'Two'

        return (int)ret;
    }
    void M(ulong p) { }
}
";

            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var testClass = tree.GetCompilationUnitRoot().Members[1] as TypeDeclarationSyntax;
            Assert.Equal(3, testClass.Members.Count);
            var mainMethod = testClass.Members[1] as MethodDeclarationSyntax;
            var mainStats = mainMethod.Body.Statements;
            Assert.Equal(10, mainStats.Count);
            // ary = new byte[3];
            var v1 = (mainStats[0] as ExpressionStatementSyntax).Expression;
            Assert.Equal(SyntaxKind.SimpleAssignmentExpression, v1.Kind());
            ConversionTestHelper(model, (v1 as AssignmentExpressionSyntax).Right, ConversionKind.Identity, ConversionKind.Identity);
            // ary[0] = 0x0F;
            var v2 = (mainStats[1] as ExpressionStatementSyntax).Expression;
            ConversionTestHelper(model, (v2 as AssignmentExpressionSyntax).Right, ConversionKind.ImplicitConstant, ConversionKind.ExplicitNumeric);
            // ushort ret = ary[0];
            var v3 = (mainStats[2] as LocalDeclarationStatementSyntax).Declaration.Variables;
            ConversionTestHelper(model, v3[0].Initializer.Value, ConversionKind.ImplicitNumeric, ConversionKind.ImplicitNumeric);
            // object obj01 = null;
            var v4 = (mainStats[3] as LocalDeclarationStatementSyntax).Declaration.Variables;
            ConversionTestHelper(model, v4[0].Initializer.Value, ConversionKind.ImplicitReference, ConversionKind.NoConversion);
            // obj.M(ary[0]);
            var v6 = (mainStats[5] as ExpressionStatementSyntax).Expression;
            Assert.Equal(SyntaxKind.InvocationExpression, v6.Kind());
            var v61 = (v6 as InvocationExpressionSyntax).ArgumentList.Arguments;
            ConversionTestHelper(model, v61[0].Expression, ConversionKind.ImplicitNumeric, ConversionKind.ImplicitNumeric);
            // object box = -1;
            var v7 = (mainStats[6] as LocalDeclarationStatementSyntax).Declaration.Variables;
            ConversionTestHelper(model, v7[0].Initializer.Value, ConversionKind.Boxing, ConversionKind.Boxing);
            // E e = 0;
            var v8 = (mainStats[7] as LocalDeclarationStatementSyntax).Declaration.Variables;
            ConversionTestHelper(model, v8[0].Initializer.Value, ConversionKind.ImplicitEnumeration, ConversionKind.ExplicitEnumeration);
            // E e2 = E.Two;
            var v9 = (mainStats[8] as LocalDeclarationStatementSyntax).Declaration.Variables;
            var v9val = (MemberAccessExpressionSyntax)(v9[0].Initializer.Value);
            var v9right = v9val.Name;
            ConversionTestHelper(model, v9right, ConversionKind.Identity, ConversionKind.Identity);
        }

        private void TestClassifyConversionBuiltInNumeric(string from, string to, ConversionKind ck)
        {
            const string template = @"
class C
{{
    static void Goo({1} v) {{ }}

    static void Main() {{ {0} v = default({0}); Goo({2}v); }}
}}
";

            var isExplicitConversion = ck == ConversionKind.ExplicitNumeric;
            var source = string.Format(template, from, to, isExplicitConversion ? "(" + to + ")" : "");
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            var model = comp.GetSemanticModel(tree);
            var c = (TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0];
            var main = (MethodDeclarationSyntax)c.Members[1];
            var call = (InvocationExpressionSyntax)((ExpressionStatementSyntax)main.Body.Statements[1]).Expression;
            var arg = call.ArgumentList.Arguments[0].Expression;

            if (isExplicitConversion)
            {
                ConversionTestHelper(model, ((CastExpressionSyntax)arg).Expression, model.GetTypeInfo(arg).ConvertedType, ck);
            }
            else
            {
                ConversionTestHelper(model, arg, ck, ck);
            }
        }

        [Fact]
        public void ClassifyConversionBuiltInNumeric()
        {
            const ConversionKind ID = ConversionKind.Identity;
            const ConversionKind IN = ConversionKind.ImplicitNumeric;
            const ConversionKind XN = ConversionKind.ExplicitNumeric;

            var types = new[] { "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong", "char", "float", "double", "decimal" };
            var conversions = new ConversionKind[,] {
                // to     sb  b  s  us i ui  l ul  c  f  d  m
                // from
                /* sb */ { ID, XN, IN, XN, IN, XN, IN, XN, XN, IN, IN, IN },
                /*  b */ { XN, ID, IN, IN, IN, IN, IN, IN, XN, IN, IN, IN },
                /*  s */ { XN, XN, ID, XN, IN, XN, IN, XN, XN, IN, IN, IN },
                /* us */ { XN, XN, XN, ID, IN, IN, IN, IN, XN, IN, IN, IN },
                /*  i */ { XN, XN, XN, XN, ID, XN, IN, XN, XN, IN, IN, IN },
                /* ui */ { XN, XN, XN, XN, XN, ID, IN, IN, XN, IN, IN, IN },
                /*  l */ { XN, XN, XN, XN, XN, XN, ID, XN, XN, IN, IN, IN },
                /* ul */ { XN, XN, XN, XN, XN, XN, XN, ID, XN, IN, IN, IN },
                /*  c */ { XN, XN, XN, IN, IN, IN, IN, IN, ID, IN, IN, IN },
                /*  f */ { XN, XN, XN, XN, XN, XN, XN, XN, XN, ID, IN, XN },
                /*  d */ { XN, XN, XN, XN, XN, XN, XN, XN, XN, XN, ID, XN },
                /*  m */ { XN, XN, XN, XN, XN, XN, XN, XN, XN, XN, XN, ID }
            };

            for (var from = 0; from < types.Length; from++)
            {
                for (var to = 0; to < types.Length; to++)
                {
                    TestClassifyConversionBuiltInNumeric(types[from], types[to], conversions[from, to]);
                }
            }
        }

        [WorkItem(527486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527486")]
        [Fact]
        public void ClassifyConversionExplicit()
        {
            var text = @"using System;
public class Test
{
    object obj01;
    public void Testing(int x, Test obj02)
    {
        uint y = 5678;
        // Cast
        y = (uint) x;

        // Boxing
        obj01 = x;
        // Cast
        x = (int)obj01;

        // NoConversion
        obj02 = (Test)obj01;
    }
}
";

            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var testClass = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            Assert.Equal(2, testClass.Members.Count);
            var mainMethod = testClass.Members[1] as MethodDeclarationSyntax;
            var mainStats = mainMethod.Body.Statements;
            Assert.Equal(5, mainStats.Count);
            // y = (uint) x;
            var v1 = ((mainStats[1] as ExpressionStatementSyntax).Expression as AssignmentExpressionSyntax).Right;
            ConversionTestHelper(model, (v1 as CastExpressionSyntax).Expression, comp.GetSpecialType(SpecialType.System_UInt32), ConversionKind.ExplicitNumeric);
            // obj01 = x;
            var v2 = (mainStats[2] as ExpressionStatementSyntax).Expression;
            ConversionTestHelper(model, (v2 as AssignmentExpressionSyntax).Right, comp.GetSpecialType(SpecialType.System_Object), ConversionKind.Boxing);
            // x = (int)obj01;
            var v3 = ((mainStats[3] as ExpressionStatementSyntax).Expression as AssignmentExpressionSyntax).Right;
            ConversionTestHelper(model, (v3 as CastExpressionSyntax).Expression, comp.GetSpecialType(SpecialType.System_Int32), ConversionKind.Unboxing);
            // obj02 = (Test)obj01;
            var tsym = comp.SourceModule.GlobalNamespace.GetTypeMembers("Test").FirstOrDefault();
            var v4 = ((mainStats[4] as ExpressionStatementSyntax).Expression as AssignmentExpressionSyntax).Right;
            ConversionTestHelper(model, (v4 as CastExpressionSyntax).Expression, tsym, ConversionKind.ExplicitReference);
        }

        [Fact]
        public void DiagnosticsInStages()
        {
            var text = @"
public class Test
{
    object obj01;
    public void Testing(int x, Test obj02)
    {
        // ExplicitReference -> CS0266
        obj02 = obj01;
    }

    binding error;
    parse err
}
";

            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var errs = model.GetDiagnostics();
            Assert.Equal(4, errs.Count());
            errs = model.GetSyntaxDiagnostics();
            Assert.Equal(1, errs.Count());
            errs = model.GetDeclarationDiagnostics();
            Assert.Equal(2, errs.Count());
            errs = model.GetMethodBodyDiagnostics();
            Assert.Equal(1, errs.Count());
        }

        [Fact]
        public void DiagnosticsFilteredWithPragmas()
        {
            var text = @"
public class Test
{

#pragma warning disable 1633
#pragma xyzzy whatever
#pragma warning restore 1633

}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var errs = model.GetDiagnostics();
            Assert.Equal(0, errs.Count());
            errs = model.GetSyntaxDiagnostics();
            Assert.Equal(0, errs.Count());
        }

        [Fact]
        public void ClassifyConversionExplicitNeg()
        {
            var text = @"
public class Test
{
    object obj01;
    public void Testing(int x, Test obj02)
    {
        uint y = 5678;
        // ExplicitNumeric - CS0266
        y = x;

        // Boxing
        obj01 = x;
        // unboxing - CS0266
        x = obj01;

        // ExplicitReference -> CS0266
        obj02 = obj01;
    }
}
";

            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var testClass = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            Assert.Equal(2, testClass.Members.Count);
            var mainMethod = testClass.Members[1] as MethodDeclarationSyntax;
            var mainStats = mainMethod.Body.Statements;
            Assert.Equal(5, mainStats.Count);
            // y = x;
            var v1 = ((mainStats[1] as ExpressionStatementSyntax).Expression as AssignmentExpressionSyntax).Right;
            ConversionTestHelper(model, v1, ConversionKind.ExplicitNumeric, ConversionKind.ExplicitNumeric);
            // x = obj01;
            var v2 = ((mainStats[3] as ExpressionStatementSyntax).Expression as AssignmentExpressionSyntax).Right;
            ConversionTestHelper(model, v2, ConversionKind.Unboxing, ConversionKind.Unboxing);
            // obj02 = obj01;
            var v3 = ((mainStats[4] as ExpressionStatementSyntax).Expression as AssignmentExpressionSyntax).Right;
            ConversionTestHelper(model, v3, ConversionKind.ExplicitReference, ConversionKind.ExplicitReference);
            // CC
            var errs = model.GetDiagnostics();
            Assert.Equal(3, errs.Count());
            errs = model.GetDeclarationDiagnostics();
            Assert.Equal(0, errs.Count());
        }

        [WorkItem(527767, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527767")]
        [Fact]
        public void ClassifyConversionNullable()
        {
            var text = @"using System;

public class Test
{
    static void Main()
    {
        // NullLiteral
        sbyte? nullable = null;

        // ImplicitNullable
        uint? nullable01 = 100;

        ushort localVal = 123;
        // ImplicitNullable
        nullable01 = localVal;

        E e = 0;
        E? en = 0; // Oddly enough, C# classifies this as an implicit enumeration conversion.
    }
}

enum E { zero, one }
";

            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var testClass = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            Assert.Equal(1, testClass.Members.Count);
            var mainMethod = testClass.Members[0] as MethodDeclarationSyntax;
            var mainStats = mainMethod.Body.Statements;
            Assert.Equal(6, mainStats.Count);

            // sbyte? nullable = null;
            var v1 = (mainStats[0] as LocalDeclarationStatementSyntax).Declaration.Variables;
            ConversionTestHelper(model, v1[0].Initializer.Value, ConversionKind.NullLiteral, ConversionKind.NoConversion);
            // uint? nullable01 = 100;
            var v2 = (mainStats[1] as LocalDeclarationStatementSyntax).Declaration.Variables;
            ConversionTestHelper(model, v2[0].Initializer.Value, ConversionKind.ImplicitNullable, ConversionKind.ExplicitNullable);
            // nullable01 = localVal;
            var v3 = (mainStats[3] as ExpressionStatementSyntax).Expression;
            Assert.Equal(SyntaxKind.SimpleAssignmentExpression, v3.Kind());
            ConversionTestHelper(model, (v3 as AssignmentExpressionSyntax).Right, ConversionKind.ImplicitNullable, ConversionKind.ImplicitNullable);

            // E e = 0;
            var v4 = (mainStats[4] as LocalDeclarationStatementSyntax).Declaration.Variables;
            ConversionTestHelper(model, v4[0].Initializer.Value, ConversionKind.ImplicitEnumeration, ConversionKind.ExplicitEnumeration);
            // E? en = 0;
            var v5 = (mainStats[5] as LocalDeclarationStatementSyntax).Declaration.Variables;
            // Bug#5035 (ByDesign): Conversion from literal 0 to nullable enum is Implicit Enumeration (not ImplicitNullable). Conversion from int to nullable enum is Explicit Nullable.
            ConversionTestHelper(model, v5[0].Initializer.Value, ConversionKind.ImplicitEnumeration, ConversionKind.ExplicitNullable);
        }

        [Fact, WorkItem(543994, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543994")]
        public void ClassifyConversionImplicitUserDef()
        {
            var text = @"using System;

class MyClass
{
    public static bool operator true(MyClass p)
    {
        return true;
    }

    public static bool operator false(MyClass p)
    {
        return false;
    }

    public static MyClass operator &(MyClass mc1, MyClass mc2)
    {
        return new MyClass();
    }

    public static int Main()
    {
        var cls1 = new MyClass();
        var cls2 = new MyClass();
        if (/*<bind0>*/cls1/*</bind0>*/)
            return 0;

        if (/*<bind1>*/cls1 && cls2/*</bind1>*/)
            return 1;

        return 2;
    }
}
";

            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprs = GetBindingNodes<ExpressionSyntax>(comp);
            var expr1 = exprs.First();
            var expr2 = exprs.Last();

            var info = model.GetTypeInfo(expr1);
            Assert.NotEqual(default, info);
            Assert.NotNull(info.ConvertedType);
            // It was ImplicitUserDef -> Design Meeting resolution: not expose op_True|False as conversion through API
            var impconv = model.GetConversion(expr1);
            Assert.Equal(Conversion.Identity, impconv);
            Conversion conv = model.ClassifyConversion(expr1, info.ConvertedType);
            CheckIsAssignableTo(model, expr1);
            Assert.Equal(impconv, conv);
            Assert.Equal("Identity", conv.ToString());

            conv = model.ClassifyConversion(expr2, info.ConvertedType);
            CheckIsAssignableTo(model, expr2);
            Assert.Equal(impconv, conv);
        }

        [Fact, WorkItem(1019372, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1019372")]
        public void ClassifyConversionImplicitUserDef02()
        {
            var text = @"
class C {
    public static implicit operator int(C c) { return 0; }
    public C() {
        int? i = /*<bind0>*/this/*</bind0>*/;
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprs = GetBindingNodes<ExpressionSyntax>(comp);
            var expr1 = exprs.First();
            var info = model.GetTypeInfo(expr1);
            Assert.NotEqual(default, info);
            Assert.NotNull(info.ConvertedType);
            var impconv = model.GetConversion(expr1);
            Assert.True(impconv.IsImplicit);
            Assert.True(impconv.IsUserDefined);

            Conversion conv = model.ClassifyConversion(expr1, info.ConvertedType);
            CheckIsAssignableTo(model, expr1);
            Assert.Equal(impconv, conv);
            Assert.True(conv.IsImplicit);
            Assert.True(conv.IsUserDefined);
        }

        private void CheckIsAssignableTo(SemanticModel model, ExpressionSyntax syntax)
        {
            var info = model.GetTypeInfo(syntax);
            var conversion = info.Type != null && info.ConvertedType != null ? model.Compilation.ClassifyConversion(info.Type, info.ConvertedType) : Conversion.NoConversion;
            Assert.Equal(conversion.IsImplicit, model.Compilation.HasImplicitConversion(info.Type, info.ConvertedType));
        }

        [Fact, WorkItem(544151, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544151")]
        public void PublicViewOfPointerConversions()
        {
            ValidateConversion(Conversion.PointerToVoid, ConversionKind.ImplicitPointerToVoid);
            ValidateConversion(Conversion.NullToPointer, ConversionKind.ImplicitNullToPointer);
            ValidateConversion(Conversion.PointerToPointer, ConversionKind.ExplicitPointerToPointer);
            ValidateConversion(Conversion.IntegerToPointer, ConversionKind.ExplicitIntegerToPointer);
            ValidateConversion(Conversion.PointerToInteger, ConversionKind.ExplicitPointerToInteger);
            ValidateConversion(Conversion.IntPtr, ConversionKind.IntPtr);
        }

        #region "Conversion helper"
        private void ValidateConversion(Conversion conv, ConversionKind kind)
        {
            Assert.Equal(conv.Kind, kind);

            switch (kind)
            {
                case ConversionKind.NoConversion:
                    Assert.False(conv.Exists);
                    Assert.False(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    break;
                case ConversionKind.Identity:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.True(conv.IsIdentity);
                    break;
                case ConversionKind.ImplicitNumeric:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.True(conv.IsNumeric);
                    break;
                case ConversionKind.ImplicitEnumeration:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.True(conv.IsEnumeration);
                    break;
                case ConversionKind.ImplicitNullable:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.True(conv.IsNullable);
                    break;
                case ConversionKind.NullLiteral:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.True(conv.IsNullLiteral);
                    break;
                case ConversionKind.ImplicitReference:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.True(conv.IsReference);
                    break;
                case ConversionKind.Boxing:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.True(conv.IsBoxing);
                    break;
                case ConversionKind.ImplicitDynamic:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.True(conv.IsDynamic);
                    break;
                case ConversionKind.ExplicitDynamic:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsExplicit);
                    Assert.False(conv.IsImplicit);
                    Assert.True(conv.IsDynamic);
                    break;
                case ConversionKind.ImplicitConstant:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.True(conv.IsConstantExpression);
                    break;
                case ConversionKind.ImplicitUserDefined:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.True(conv.IsUserDefined);
                    break;
                case ConversionKind.AnonymousFunction:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.True(conv.IsAnonymousFunction);
                    break;
                case ConversionKind.MethodGroup:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.True(conv.IsMethodGroup);
                    break;
                case ConversionKind.ExplicitNumeric:
                    Assert.True(conv.Exists);
                    Assert.False(conv.IsImplicit);
                    Assert.True(conv.IsExplicit);
                    Assert.True(conv.IsNumeric);
                    break;
                case ConversionKind.ExplicitEnumeration:
                    Assert.True(conv.Exists);
                    Assert.False(conv.IsImplicit);
                    Assert.True(conv.IsExplicit);
                    Assert.True(conv.IsEnumeration);
                    break;
                case ConversionKind.ExplicitNullable:
                    Assert.True(conv.Exists);
                    Assert.False(conv.IsImplicit);
                    Assert.True(conv.IsExplicit);
                    Assert.True(conv.IsNullable);
                    break;
                case ConversionKind.ExplicitReference:
                    Assert.True(conv.Exists);
                    Assert.False(conv.IsImplicit);
                    Assert.True(conv.IsExplicit);
                    Assert.True(conv.IsReference);
                    break;
                case ConversionKind.Unboxing:
                    Assert.True(conv.Exists);
                    Assert.False(conv.IsImplicit);
                    Assert.True(conv.IsExplicit);
                    Assert.True(conv.IsUnboxing);
                    break;
                case ConversionKind.ExplicitUserDefined:
                    Assert.True(conv.Exists);
                    Assert.False(conv.IsImplicit);
                    Assert.True(conv.IsExplicit);
                    Assert.True(conv.IsUserDefined);
                    break;
                case ConversionKind.ImplicitNullToPointer:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.False(conv.IsUserDefined);
                    Assert.True(conv.IsPointer);
                    break;
                case ConversionKind.ImplicitPointerToVoid:
                    Assert.True(conv.Exists);
                    Assert.True(conv.IsImplicit);
                    Assert.False(conv.IsExplicit);
                    Assert.False(conv.IsUserDefined);
                    Assert.True(conv.IsPointer);
                    break;
                case ConversionKind.ExplicitPointerToPointer:
                    Assert.True(conv.Exists);
                    Assert.False(conv.IsImplicit);
                    Assert.True(conv.IsExplicit);
                    Assert.False(conv.IsUserDefined);
                    Assert.True(conv.IsPointer);
                    break;
                case ConversionKind.ExplicitIntegerToPointer:
                    Assert.True(conv.Exists);
                    Assert.False(conv.IsImplicit);
                    Assert.True(conv.IsExplicit);
                    Assert.False(conv.IsUserDefined);
                    Assert.True(conv.IsPointer);
                    break;
                case ConversionKind.ExplicitPointerToInteger:
                    Assert.True(conv.Exists);
                    Assert.False(conv.IsImplicit);
                    Assert.True(conv.IsExplicit);
                    Assert.False(conv.IsUserDefined);
                    Assert.True(conv.IsPointer);
                    break;
                case ConversionKind.IntPtr:
                    Assert.True(conv.Exists);
                    Assert.False(conv.IsImplicit);
                    Assert.True(conv.IsExplicit);
                    Assert.False(conv.IsUserDefined);
                    Assert.False(conv.IsPointer);
                    Assert.True(conv.IsIntPtr);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="expr"></param>
        /// <param name="ept1">expr -> TypeInParent</param>
        /// <param name="ept2">Type(expr) -> TypeInParent</param>
        private void ConversionTestHelper(SemanticModel semanticModel, ExpressionSyntax expr, ConversionKind ept1, ConversionKind ept2)
        {
            var info = semanticModel.GetTypeInfo(expr);
            Assert.NotEqual(default, info);
            Assert.NotNull(info.ConvertedType);
            var conv = semanticModel.GetConversion(expr);

            // NOT expect NoConversion
            Conversion act1 = semanticModel.ClassifyConversion(expr, info.ConvertedType);
            CheckIsAssignableTo(semanticModel, expr);
            Assert.Equal(ept1, act1.Kind);
            ValidateConversion(act1, ept1);
            ValidateConversion(act1, conv.Kind);

            if (ept2 == ConversionKind.NoConversion)
            {
                Assert.Null(info.Type);
            }
            else
            {
                Assert.NotNull(info.Type);
                var act2 = semanticModel.Compilation.ClassifyConversion(info.Type, info.ConvertedType);
                Assert.Equal(ept2, act2.Kind);
                ValidateConversion(act2, ept2);
            }
        }

        private void ConversionTestHelper(SemanticModel semanticModel, ExpressionSyntax expr, ITypeSymbol expsym, ConversionKind expkind)
        {
            var info = semanticModel.GetTypeInfo(expr);
            Assert.NotEqual(default, info);
            Assert.NotNull(info.ConvertedType);

            // NOT expect NoConversion
            Conversion act1 = semanticModel.ClassifyConversion(expr, expsym);
            CheckIsAssignableTo(semanticModel, expr);
            Assert.Equal(expkind, act1.Kind);
            ValidateConversion(act1, expkind);
        }

        #endregion

        [Fact]
        public void EnumOffsets()
        {
            // sbyte
            EnumOffset(ConstantValue.Create((sbyte)sbyte.MinValue), 1, EnumOverflowKind.NoOverflow, ConstantValue.Create((sbyte)(sbyte.MinValue + 1)));
            EnumOffset(ConstantValue.Create((sbyte)sbyte.MinValue), 2, EnumOverflowKind.NoOverflow, ConstantValue.Create((sbyte)(sbyte.MinValue + 2)));
            EnumOffset(ConstantValue.Create((sbyte)-2), 1, EnumOverflowKind.NoOverflow, ConstantValue.Create((sbyte)(-1)));
            EnumOffset(ConstantValue.Create((sbyte)-2), 3, EnumOverflowKind.NoOverflow, ConstantValue.Create((sbyte)(1)));
            EnumOffset(ConstantValue.Create((sbyte)(sbyte.MaxValue - 3)), 3, EnumOverflowKind.NoOverflow, ConstantValue.Create((sbyte)sbyte.MaxValue));
            EnumOffset(ConstantValue.Create((sbyte)(sbyte.MaxValue - 3)), 4, EnumOverflowKind.OverflowReport, ConstantValue.Bad);
            EnumOffset(ConstantValue.Create((sbyte)(sbyte.MaxValue - 3)), 5, EnumOverflowKind.OverflowIgnore, ConstantValue.Bad);

            // byte
            EnumOffset(ConstantValue.Create((byte)0), 1, EnumOverflowKind.NoOverflow, ConstantValue.Create((byte)1));
            EnumOffset(ConstantValue.Create((byte)0), 2, EnumOverflowKind.NoOverflow, ConstantValue.Create((byte)2));
            EnumOffset(ConstantValue.Create((byte)(byte.MaxValue - 3)), 3, EnumOverflowKind.NoOverflow, ConstantValue.Create((byte)byte.MaxValue));
            EnumOffset(ConstantValue.Create((byte)(byte.MaxValue - 3)), 4, EnumOverflowKind.OverflowReport, ConstantValue.Bad);
            EnumOffset(ConstantValue.Create((byte)(byte.MaxValue - 3)), 5, EnumOverflowKind.OverflowIgnore, ConstantValue.Bad);

            // short
            EnumOffset(ConstantValue.Create((short)short.MinValue), 1, EnumOverflowKind.NoOverflow, ConstantValue.Create((short)(short.MinValue + 1)));
            EnumOffset(ConstantValue.Create((short)short.MinValue), 2, EnumOverflowKind.NoOverflow, ConstantValue.Create((short)(short.MinValue + 2)));
            EnumOffset(ConstantValue.Create((short)-2), 1, EnumOverflowKind.NoOverflow, ConstantValue.Create((short)(-1)));
            EnumOffset(ConstantValue.Create((short)-2), 3, EnumOverflowKind.NoOverflow, ConstantValue.Create((short)(1)));
            EnumOffset(ConstantValue.Create((short)(short.MaxValue - 3)), 3, EnumOverflowKind.NoOverflow, ConstantValue.Create((short)short.MaxValue));
            EnumOffset(ConstantValue.Create((short)(short.MaxValue - 3)), 4, EnumOverflowKind.OverflowReport, ConstantValue.Bad);
            EnumOffset(ConstantValue.Create((short)(short.MaxValue - 3)), 5, EnumOverflowKind.OverflowIgnore, ConstantValue.Bad);

            // ushort
            EnumOffset(ConstantValue.Create((ushort)0), 1, EnumOverflowKind.NoOverflow, ConstantValue.Create((ushort)1));
            EnumOffset(ConstantValue.Create((ushort)0), 2, EnumOverflowKind.NoOverflow, ConstantValue.Create((ushort)2));
            EnumOffset(ConstantValue.Create((ushort)(ushort.MaxValue - 3)), 3, EnumOverflowKind.NoOverflow, ConstantValue.Create((ushort)ushort.MaxValue));
            EnumOffset(ConstantValue.Create((ushort)(ushort.MaxValue - 3)), 4, EnumOverflowKind.OverflowReport, ConstantValue.Bad);
            EnumOffset(ConstantValue.Create((ushort)(ushort.MaxValue - 3)), 5, EnumOverflowKind.OverflowIgnore, ConstantValue.Bad);

            // int
            EnumOffset(ConstantValue.Create((int)int.MinValue), 1, EnumOverflowKind.NoOverflow, ConstantValue.Create((int)(int.MinValue + 1)));
            EnumOffset(ConstantValue.Create((int)int.MinValue), 2, EnumOverflowKind.NoOverflow, ConstantValue.Create((int)(int.MinValue + 2)));
            EnumOffset(ConstantValue.Create((int)-2), 1, EnumOverflowKind.NoOverflow, ConstantValue.Create((int)(-1)));
            EnumOffset(ConstantValue.Create((int)-2), 3, EnumOverflowKind.NoOverflow, ConstantValue.Create((int)(1)));
            EnumOffset(ConstantValue.Create((int)(int.MaxValue - 3)), 3, EnumOverflowKind.NoOverflow, ConstantValue.Create((int)int.MaxValue));
            EnumOffset(ConstantValue.Create((int)(int.MaxValue - 3)), 4, EnumOverflowKind.OverflowReport, ConstantValue.Bad);
            EnumOffset(ConstantValue.Create((int)(int.MaxValue - 3)), 5, EnumOverflowKind.OverflowIgnore, ConstantValue.Bad);

            // uint
            EnumOffset(ConstantValue.Create((uint)0), 1, EnumOverflowKind.NoOverflow, ConstantValue.Create((uint)1));
            EnumOffset(ConstantValue.Create((uint)0), 2, EnumOverflowKind.NoOverflow, ConstantValue.Create((uint)2));
            EnumOffset(ConstantValue.Create((uint)(uint.MaxValue - 3)), 3, EnumOverflowKind.NoOverflow, ConstantValue.Create((uint)uint.MaxValue));
            EnumOffset(ConstantValue.Create((uint)(uint.MaxValue - 3)), 4, EnumOverflowKind.OverflowReport, ConstantValue.Bad);
            EnumOffset(ConstantValue.Create((uint)(uint.MaxValue - 3)), 5, EnumOverflowKind.OverflowIgnore, ConstantValue.Bad);

            // long
            EnumOffset(ConstantValue.Create((long)long.MinValue), 1, EnumOverflowKind.NoOverflow, ConstantValue.Create((long)(long.MinValue + 1)));
            EnumOffset(ConstantValue.Create((long)long.MinValue), 2, EnumOverflowKind.NoOverflow, ConstantValue.Create((long)(long.MinValue + 2)));
            EnumOffset(ConstantValue.Create((long)-2), 1, EnumOverflowKind.NoOverflow, ConstantValue.Create((long)(-1)));
            EnumOffset(ConstantValue.Create((long)-2), 3, EnumOverflowKind.NoOverflow, ConstantValue.Create((long)(1)));
            EnumOffset(ConstantValue.Create((long)(long.MaxValue - 3)), 3, EnumOverflowKind.NoOverflow, ConstantValue.Create((long)long.MaxValue));
            EnumOffset(ConstantValue.Create((long)(long.MaxValue - 3)), 4, EnumOverflowKind.OverflowReport, ConstantValue.Bad);
            EnumOffset(ConstantValue.Create((long)(long.MaxValue - 3)), 5, EnumOverflowKind.OverflowIgnore, ConstantValue.Bad);

            // ulong
            EnumOffset(ConstantValue.Create((ulong)0), 1, EnumOverflowKind.NoOverflow, ConstantValue.Create((ulong)1));
            EnumOffset(ConstantValue.Create((ulong)0), 2, EnumOverflowKind.NoOverflow, ConstantValue.Create((ulong)2));
            EnumOffset(ConstantValue.Create((ulong)(ulong.MaxValue - 3)), 3, EnumOverflowKind.NoOverflow, ConstantValue.Create((ulong)ulong.MaxValue));
            EnumOffset(ConstantValue.Create((ulong)(ulong.MaxValue - 3)), 4, EnumOverflowKind.OverflowReport, ConstantValue.Bad);
            EnumOffset(ConstantValue.Create((ulong)(ulong.MaxValue - 3)), 5, EnumOverflowKind.OverflowIgnore, ConstantValue.Bad);
        }

        private void EnumOffset(ConstantValue constantValue, uint offset, EnumOverflowKind expectedOverflowKind, ConstantValue expectedValue)
        {
            ConstantValue actualValue;
            var actualOverflowKind = EnumConstantHelper.OffsetValue(constantValue, offset, out actualValue);
            Assert.Equal(expectedOverflowKind, actualOverflowKind);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void TestGetSemanticInfoInParentInIf()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M(int x)
  {
    if (x == 10) {}
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var ifStatement = (IfStatementSyntax)methodDecl.Body.Statements[0];
            var condition = ifStatement.Condition;
            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(condition);
            Assert.NotNull(info.ConvertedType);
            Assert.Equal("Boolean", info.Type.Name);
            Assert.Equal("System.Boolean System.Int32.op_Equality(System.Int32 left, System.Int32 right)", info.Symbol.ToTestDisplayString());
            Assert.Equal(0, info.CandidateSymbols.Length);
        }

        [Fact]
        public void TestGetSemanticInfoInParentInFor()
        {
            var compilation = CreateCompilation(@"
class C 
{
  void M(int x)
  {
    for (int i = 0; i < 10; i = i + 1) { }
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var methodDecl = (MethodDeclarationSyntax)((TypeDeclarationSyntax)tree.GetCompilationUnitRoot().Members[0]).Members[0];
            var forStatement = (ForStatementSyntax)methodDecl.Body.Statements[0];
            var condition = forStatement.Condition;
            var model = compilation.GetSemanticModel(tree);
            var info = model.GetSemanticInfoSummary(condition);
            Assert.NotNull(info.ConvertedType);
            Assert.Equal("Boolean", info.ConvertedType.Name);
            Assert.Equal("System.Boolean System.Int32.op_LessThan(System.Int32 left, System.Int32 right)", info.Symbol.ToTestDisplayString());
            Assert.Equal(0, info.CandidateSymbols.Length);
        }

        [WorkItem(540279, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540279")]
        [Fact]
        public void NoMembersForVoidReturnType()
        {
            var text = @"
class C
{
    void M()
    {
        /*<bind>*/System.Console.WriteLine()/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);
            IMethodSymbol methodSymbol = (IMethodSymbol)bindInfo.Symbol;
            ITypeSymbol returnType = methodSymbol.ReturnType;
            var symbols = model.LookupSymbols(0, returnType);
            Assert.Equal(0, symbols.Length);
        }

        [WorkItem(540767, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540767")]
        [Fact]
        public void BindIncompleteVarDeclWithDoKeyword()
        {
            var code = @"
class Test
{
    static int Main(string[] args)
    {
        do";

            var compilation = CreateCompilation(code);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var exprSyntaxList = GetExprSyntaxList(tree);

            Assert.Equal(6, exprSyntaxList.Count); // Note the omitted array size expression in "string[]"
            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxList[4].Kind());
            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxList[5].Kind());

            Assert.Equal("", exprSyntaxList[4].ToFullString());
            Assert.Equal("", exprSyntaxList[5].ToFullString());

            var exprSyntaxToBind = exprSyntaxList[exprSyntaxList.Count - 2];

            model.GetSemanticInfoSummary(exprSyntaxToBind);
        }

        [Fact]
        public void TestBindBaseConstructorInitializer()
        {
            var text = @"
class C
{
    C() : base() { }
}
";
            var bindInfo = BindFirstConstructorInitializer(text);
            Assert.NotEqual(default, bindInfo);

            var baseConstructor = bindInfo.Symbol;
            Assert.Equal(SymbolKind.Method, baseConstructor.Kind);
            Assert.Equal(MethodKind.Constructor, ((IMethodSymbol)baseConstructor).MethodKind);
            Assert.Equal("System.Object..ctor()", baseConstructor.ToTestDisplayString());
        }

        [Fact]
        public void TestBindThisConstructorInitializer()
        {
            var text = @"
class C
{
    C() : this(1) { }
    C(int x) { }
}
";
            var bindInfo = BindFirstConstructorInitializer(text);
            Assert.NotEqual(default, bindInfo);

            var baseConstructor = bindInfo.Symbol;
            Assert.Equal(SymbolKind.Method, baseConstructor.Kind);
            Assert.Equal(MethodKind.Constructor, ((IMethodSymbol)baseConstructor).MethodKind);
            Assert.Equal("C..ctor(System.Int32 x)", baseConstructor.ToTestDisplayString());
        }

        [WorkItem(540862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540862")]
        [Fact]
        public void BindThisStaticConstructorInitializer()
        {
            var text = @"
class MyClass
{
    static MyClass()
        : this()
    {
        intI = 2;
    }
    public MyClass() { }
    static int intI = 1;
}
";
            var bindInfo = BindFirstConstructorInitializer(text);
            Assert.NotEqual(default, bindInfo);
            var invokedConstructor = (IMethodSymbol)bindInfo.Symbol;
            Assert.Equal(MethodKind.Constructor, invokedConstructor.MethodKind);
            Assert.Equal("MyClass..ctor()", invokedConstructor.ToTestDisplayString());
        }

        [WorkItem(541053, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541053")]
        [Fact]
        public void CheckAndAdjustPositionOutOfRange()
        {
            var text = @"
using System;

> 1
";
            var tree = Parse(text, options: TestOptions.Script);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var root = tree.GetCompilationUnitRoot();

            Assert.NotEqual(0, root.SpanStart);

            var stmt = (GlobalStatementSyntax)root.Members.Single();
            var expr = ((ExpressionStatementSyntax)stmt.Statement).Expression;

            Assert.Equal(SyntaxKind.GreaterThanExpression, expr.Kind());

            var info = model.GetSemanticInfoSummary(expr);
            Assert.Equal(SpecialType.System_Boolean, info.Type.SpecialType);
        }

        [Fact]
        public void AddAccessorValueParameter()
        {
            var text = @"
class C
{
    private System.Action e;
    event System.Action E
    {
        add { e += /*<bind>*/value/*</bind>*/; }
        remove { e -= value; }
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var systemActionType = GetSystemActionType(comp);
            Assert.Equal(systemActionType, bindInfo.Type);

            var parameterSymbol = (IParameterSymbol)bindInfo.Symbol;
            Assert.Equal(systemActionType, parameterSymbol.Type);
            Assert.Equal("value", parameterSymbol.Name);
            Assert.Equal(MethodKind.EventAdd, ((IMethodSymbol)parameterSymbol.ContainingSymbol).MethodKind);
        }

        [Fact]
        public void RemoveAccessorValueParameter()
        {
            var text = @"
class C
{
    private System.Action e;
    event System.Action E
    {
        add { e += value; }
        remove { e -= /*<bind>*/value/*</bind>*/; }
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var systemActionType = GetSystemActionType(comp);
            Assert.Equal(systemActionType, bindInfo.Type);

            var parameterSymbol = (IParameterSymbol)bindInfo.Symbol;
            Assert.Equal(systemActionType, parameterSymbol.Type);
            Assert.Equal("value", parameterSymbol.Name);
            Assert.Equal(MethodKind.EventRemove, ((IMethodSymbol)parameterSymbol.ContainingSymbol).MethodKind);
        }

        [Fact]
        public void FieldLikeEventInitializer()
        {
            var text = @"
class C
{
    event System.Action E = /*<bind>*/new System.Action(() => { })/*</bind>*/;
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var systemActionType = GetSystemActionType(comp);
            Assert.Equal(systemActionType, bindInfo.Type);
            Assert.Null(bindInfo.Symbol);
            Assert.Equal(0, bindInfo.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, bindInfo.CandidateReason);
        }

        [Fact]
        public void FieldLikeEventInitializer2()
        {
            var text = @"
class C
{
    event System.Action E = new /*<bind>*/System.Action/*</bind>*/(() => { });
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var systemActionType = GetSystemActionType(comp);
            Assert.Null(bindInfo.Type);
            Assert.Equal(systemActionType, bindInfo.Symbol);
        }

        [Fact]
        public void CustomEventAccess()
        {
            var text = @"
class C
{
    event System.Action E { add { } remove { } }

    void Method()
    {
        /*<bind>*/E/*</bind>*/ += null;
    }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var systemActionType = GetSystemActionType(comp);
            Assert.Equal(systemActionType, bindInfo.Type);

            var eventSymbol = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IEventSymbol>("E");
            Assert.Equal(eventSymbol, bindInfo.Symbol);
        }

        [Fact]
        public void FieldLikeEventAccess()
        {
            var text = @"
class C
{
    event System.Action E;

    void Method()
    {
        /*<bind>*/E/*</bind>*/ += null;
    }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var systemActionType = GetSystemActionType(comp);
            Assert.Equal(systemActionType, bindInfo.Type);

            var eventSymbol = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IEventSymbol>("E");
            Assert.Equal(eventSymbol, bindInfo.Symbol);
        }

        [Fact]
        public void CustomEventAssignmentOperator()
        {
            var text = @"
class C
{
    event System.Action E { add { } remove { } }

    void Method()
    {
        /*<bind>*/E += null/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            Assert.Equal(SpecialType.System_Void, bindInfo.Type.SpecialType);

            var eventSymbol = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IEventSymbol>("E");
            Assert.Equal(eventSymbol.AddMethod, bindInfo.Symbol);
        }

        [Fact]
        public void FieldLikeEventAssignmentOperator()
        {
            var text = @"
class C
{
    event System.Action E;

    void Method()
    {
        /*<bind>*/E += null/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            Assert.Equal(SpecialType.System_Void, bindInfo.Type.SpecialType);

            var eventSymbol = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IEventSymbol>("E");
            Assert.Equal(eventSymbol.AddMethod, bindInfo.Symbol);
        }

        [Fact]
        public void CustomEventMissingAssignmentOperator()
        {
            var text = @"
class C
{
    event System.Action E { /*add { }*/ remove { } } //missing add

    void Method()
    {
        /*<bind>*/E += null/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);
            Assert.Null(bindInfo.Symbol);
            Assert.Equal(0, bindInfo.CandidateSymbols.Length);
            Assert.Equal(SpecialType.System_Void, bindInfo.Type.SpecialType);
        }

        private static INamedTypeSymbol GetSystemActionType(CSharpCompilation comp)
        {
            return GetSystemActionType((Compilation)comp);
        }

        private static INamedTypeSymbol GetSystemActionType(Compilation comp)
        {
            return (INamedTypeSymbol)comp.GlobalNamespace.GetMember<INamespaceSymbol>("System").GetMembers("Action").Where(s => !((INamedTypeSymbol)s).IsGenericType).Single();
        }

        [Fact]
        public void IndexerAccess()
        {
            var text = @"
class C
{
    int this[int x] { get { return x; } }
    int this[int x, int y] { get { return x + y; } }

    void Method()
    {
        int x = /*<bind>*/this[1]/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.ElementAccessExpression, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var indexerSymbol = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Indexers.Where(i => i.ParameterCount == 1).Single().GetPublicSymbol();
            Assert.Equal(indexerSymbol, bindInfo.Symbol);
            Assert.Equal(0, bindInfo.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, bindInfo.CandidateReason);

            Assert.Equal(SpecialType.System_Int32, bindInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, bindInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, bindInfo.ImplicitConversion.Kind);
            Assert.Equal(CandidateReason.None, bindInfo.CandidateReason);
            Assert.Equal(0, bindInfo.MethodGroup.Length);
            Assert.False(bindInfo.IsCompileTimeConstant);
            Assert.Null(bindInfo.ConstantValue.Value);
        }

        [Fact]
        public void IndexerAccessOverloadResolutionFailure()
        {
            var text = @"
class C
{
    int this[int x] { get { return x; } }
    int this[int x, int y] { get { return x + y; } }

    void Method()
    {
        int x = /*<bind>*/this[1, 2, 3]/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.ElementAccessExpression, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var indexerSymbol1 = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Indexers.Where(i => i.ParameterCount == 1).Single().GetPublicSymbol();
            var indexerSymbol2 = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").Indexers.Where(i => i.ParameterCount == 2).Single().GetPublicSymbol();
            var candidateIndexers = ImmutableArray.Create<ISymbol>(indexerSymbol1, indexerSymbol2);

            Assert.Null(bindInfo.Symbol);
            Assert.True(bindInfo.CandidateSymbols.SetEquals(candidateIndexers, EqualityComparer<ISymbol>.Default));
            Assert.Equal(CandidateReason.OverloadResolutionFailure, bindInfo.CandidateReason);

            Assert.Equal(SpecialType.System_Int32, bindInfo.Type.SpecialType); //still have the type since all candidates agree
            Assert.Equal(SpecialType.System_Int32, bindInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, bindInfo.ImplicitConversion.Kind);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, bindInfo.CandidateReason);
            Assert.Equal(0, bindInfo.MethodGroup.Length);
            Assert.False(bindInfo.IsCompileTimeConstant);
            Assert.Null(bindInfo.ConstantValue.Value);
        }

        [Fact]
        public void IndexerAccessNoIndexers()
        {
            var text = @"
class C
{
    void Method()
    {
        int x = /*<bind>*/this[1, 2, 3]/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.ElementAccessExpression, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            Assert.Null(bindInfo.Symbol);
            Assert.Equal(0, bindInfo.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, bindInfo.CandidateReason);

            Assert.Equal(TypeKind.Error, bindInfo.Type.TypeKind);
            Assert.Equal(TypeKind.Struct, bindInfo.ConvertedType.TypeKind);
            Assert.Equal(ConversionKind.NoConversion, bindInfo.ImplicitConversion.Kind);
            Assert.Equal(CandidateReason.None, bindInfo.CandidateReason);
            Assert.Equal(0, bindInfo.MethodGroup.Length);
            Assert.False(bindInfo.IsCompileTimeConstant);
            Assert.Null(bindInfo.ConstantValue.Value);
        }

        [WorkItem(542296, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542296")]
        [Fact]
        public void TypeArgumentsOnFieldAccess1()
        {
            var text = @"
public class Test
{
    public int Fld;
    public int Func()
    {
        return (int)(/*<bind>*/Fld<int>/*</bind>*/);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.GenericName, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            Assert.Null(bindInfo.Symbol);
            Assert.Equal(CandidateReason.WrongArity, bindInfo.CandidateReason);
            Assert.Equal(1, bindInfo.CandidateSymbols.Length);
            var candidate = bindInfo.CandidateSymbols.Single();
            Assert.Equal(SymbolKind.Field, candidate.Kind);
            Assert.Equal("Fld", candidate.Name);
        }

        [WorkItem(542296, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542296")]
        [Fact]
        public void TypeArgumentsOnFieldAccess2()
        {
            var text = @"
public class Test
{
    public int Fld;
    public int Func()
    {
        return (int)(Fld</*<bind>*/Test/*</bind>*/>);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var symbol = bindInfo.Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal("Test", symbol.Name);
        }

        [WorkItem(528785, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528785")]
        [Fact]
        public void TopLevelIndexer()
        {
            var text = @"
this[double E] { get { return /*<bind>*/E/*</bind>*/; } }
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var symbol = bindInfo.Symbol;
            Assert.Null(symbol);
        }

        [WorkItem(542360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542360")]
        [Fact]
        public void TypeAndMethodHaveSameTypeParameterName()
        {
            var text = @"
interface I<T>
{
    void Goo<T>();
}
 
class A<T> : I<T>
{
    void I</*<bind>*/T/*</bind>*/>.Goo<T>() { }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var symbol = bindInfo.Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.TypeParameter, symbol.Kind);
            Assert.Equal("T", symbol.Name);
            Assert.Equal(comp.GlobalNamespace.GetMember<INamedTypeSymbol>("A"), symbol.ContainingSymbol); //from the type, not the method
        }

        [WorkItem(542436, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542436")]
        [Fact]
        public void RecoveryFromBadNamespaceDeclaration()
        {
            var text =
@"namespace alias::
using alias = /*<bind>*/N/*</bind>*/;
namespace N { }
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxToBind.Kind());
            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);
        }

        /// Test that binding a local declared with var binds the same way when localSymbol.Type is called before BindVariableDeclaration.
        /// Assert occurs if the two do not compute the same type.
        [Fact]
        [WorkItem(542634, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542634")]
        public void VarInitializedWithStaticType()
        {
            var text =
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
class Program
{
    static string xunit = " + "@" + @"..\..\Closed\Tools\xUnit\xunit.console.x86.exe" + @";
    static string test = " + @"Roslyn.VisualStudio.Services.UnitTests.dll" + @";
    static string commandLine = test" + @" /html log.html" + @";
    static void Main(string[] args)
    {
        var options = CreateOptions();
        /*<bind>*/Parallel/*</bind>*/.For(0, 100, RunTest, options);
    }
    private static Parallel CreateOptions()
    {
        var result = new ParallelOptions();
    }
    private static void RunTest(int i)
    {
        
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxToBind.Kind());
            // Bind Parallel from line Parallel.For(0, 100, RunTest, options);
            // This will implicitly bind "var" to determine type of options.
            // This calls LocalSymbol.GetType
            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);
            var varIdentifier = (IdentifierNameSyntax)tree.GetCompilationUnitRoot().DescendantNodes().First(n => n.ToString() == "var");
            // var from line var options = CreateOptions;
            // Explicitly bind "var".
            // This path calls BindvariableDeclaration.
            bindInfo = model.GetSemanticInfoSummary(varIdentifier);
        }

        [WorkItem(542186, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542186")]
        [Fact]
        public void IndexerParameter()
        {
            var text = @"
class C
{
    int this[int x]
    {
        get
        {
            return /*<bind>*/x/*</bind>*/;
        }
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var symbol = bindInfo.Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Equal("x", symbol.Name);
            Assert.Equal(SymbolKind.Method, symbol.ContainingSymbol.Kind);

            var lookupSymbols = model.LookupSymbols(exprSyntaxToBind.SpanStart, name: "x");
            Assert.Equal(symbol, lookupSymbols.Single());
        }

        [WorkItem(542186, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542186")]
        [Fact]
        public void IndexerValueParameter()
        {
            var text = @"
class C
{
    int this[int x]
    {
        set
        {
            x = /*<bind>*/value/*</bind>*/;
        }
    }
}
";
            verify(Parse(text, options: TestOptions.Regular12), SyntaxKind.IdentifierName);
            verify(Parse(text), SyntaxKind.ValueExpression);

            void verify(SyntaxTree tree, SyntaxKind expectedKind)
            {
                var comp = CreateCompilation(tree);
                var model = comp.GetSemanticModel(tree);
                var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

                Assert.Equal(expectedKind, exprSyntaxToBind.Kind());

                var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

                var symbol = bindInfo.Symbol;
                Assert.NotNull(symbol);
                Assert.Equal(SymbolKind.Parameter, symbol.Kind);
                Assert.Equal("value", symbol.Name);
                Assert.Equal(SymbolKind.Method, symbol.ContainingSymbol.Kind);

                var lookupSymbols = model.LookupSymbols(exprSyntaxToBind.SpanStart, name: "value");
                Assert.Equal(symbol, lookupSymbols.Single());
            }
        }

        [WorkItem(542777, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542777")]
        [Fact]
        public void IndexerThisParameter()
        {
            var text = @"
class C
{
    int this[int x]
    {
        set
        {
            System.Console.Write(/*<bind>*/this/*</bind>*/);
        }
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.ThisExpression, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var symbol = bindInfo.Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Equal("this", symbol.Name);
            Assert.Equal(SymbolKind.Method, symbol.ContainingSymbol.Kind);
        }

        [WorkItem(542592, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542592")]
        [Fact]
        public void TypeParameterParamsParameter()
        {
            var text = @"
class Test<T>
{
    public void Method(params T arr)
    {
    }
}
class Program
{
    static void Main(string[] args)
    {
        new Test<int[]>()./*<bind>*/Method/*</bind>*/(new int[][] { });
    }
}

";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSemanticInfoSummary(exprSyntaxToBind);

            var symbol = bindInfo.Symbol;
            Assert.Null(symbol);

            Assert.Equal(CandidateReason.OverloadResolutionFailure, bindInfo.CandidateReason);
            var candidate = (IMethodSymbol)bindInfo.CandidateSymbols.Single();
            Assert.Equal("void Test<System.Int32[]>.Method(params System.Int32[] arr)", candidate.ToTestDisplayString());
            Assert.Equal(TypeKind.Array, candidate.Parameters.Last().Type.TypeKind);
            Assert.Equal(TypeKind.TypeParameter, ((IMethodSymbol)candidate.OriginalDefinition).Parameters.Last().Type.TypeKind);
        }

        [WorkItem(542458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542458")]
        [Fact]
        public void ParameterDefaultValues()
        {
            var text = @"
struct S
{
    void M(
        int i = 1,
        string str = ""hello"",
        object o = null,
        S s = default(S))
    {
        /*<bind>*/M/*</bind>*/();
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSymbolInfo(exprSyntaxToBind);

            var method = (IMethodSymbol)bindInfo.Symbol;
            Assert.NotNull(method);

            var parameters = method.Parameters;
            Assert.Equal(4, parameters.Length);

            Assert.True(parameters[0].HasExplicitDefaultValue);
            Assert.Equal(1, parameters[0].ExplicitDefaultValue);

            Assert.True(parameters[1].HasExplicitDefaultValue);
            Assert.Equal("hello", parameters[1].ExplicitDefaultValue);

            Assert.True(parameters[2].HasExplicitDefaultValue);
            Assert.Null(parameters[2].ExplicitDefaultValue);

            Assert.True(parameters[3].HasExplicitDefaultValue);
            Assert.Null(parameters[3].ExplicitDefaultValue);
        }

        [WorkItem(542764, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542764")]
        [Fact]
        public void UnboundGenericTypeArity()
        {
            var text = @"
class C<T, U, V>
{
    void M()
    {
        System.Console.Write(typeof(/*<bind>*/C<,,>/*</bind>*/));
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var nameSyntaxToBind = (SimpleNameSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.GenericName, nameSyntaxToBind.Kind());
            Assert.Equal(3, nameSyntaxToBind.Arity);

            var bindInfo = model.GetSymbolInfo(nameSyntaxToBind);

            var type = (INamedTypeSymbol)bindInfo.Symbol;
            Assert.NotNull(type);
            Assert.True(type.IsUnboundGenericType);
            Assert.Equal(3, type.Arity);
            Assert.Equal("C<,,>", type.ToTestDisplayString());
        }

        [Fact]
        public void GetType_VoidArray()
        {
            var text = @"
class C
{
    void M()
    {
        var x = typeof(/*<bind>*/System.Void[]/*</bind>*/);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.ArrayType, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSymbolInfo(exprSyntaxToBind);

            var arrayType = (IArrayTypeSymbol)bindInfo.Symbol;
            Assert.NotNull(arrayType);
            Assert.Equal("System.Void[]", arrayType.ToTestDisplayString());
        }

        [WorkItem(543295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543295")]
        [Fact]
        public void TypeParameterDiamondInheritance1()
        {
            var text = @"
using System.Linq;
 
public interface IA
{
    object P { get; }
}
 
public interface IB : IA { }

public class C<T> where T : IA, IB // can find IA.P in two different ways
{
    void M()
    {
        new T[1]./*<bind>*/Select/*</bind>*/(i => i.P);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { tree });
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSymbolInfo(exprSyntaxToBind);
            Assert.Equal("System.Collections.Generic.IEnumerable<System.Object> System.Collections.Generic.IEnumerable<T>.Select<T, System.Object>(System.Func<T, System.Object> selector)", bindInfo.Symbol.ToTestDisplayString());
        }

        [WorkItem(543295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543295")]
        [Fact]
        public void TypeParameterDiamondInheritance2() //add hiding member in derived interface
        {
            var text = @"
using System.Linq;
 
public interface IA
{
    object P { get; }
}
 
public interface IB : IA
{
    new int P { get; }
}

public class C<T> where T : IA, IB
{
    void M()
    {
        new T[1]./*<bind>*/Select/*</bind>*/(i => i.P);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { tree });
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSymbolInfo(exprSyntaxToBind);
            Assert.Equal("System.Collections.Generic.IEnumerable<System.Int32> System.Collections.Generic.IEnumerable<T>.Select<T, System.Int32>(System.Func<T, System.Int32> selector)", bindInfo.Symbol.ToTestDisplayString());
        }

        [WorkItem(543295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543295")]
        [Fact]
        public void TypeParameterDiamondInheritance3() //reverse order of interface list (shouldn't matter)
        {
            var text = @"
using System.Linq;
 
public interface IA
{
    object P { get; }
}
 
public interface IB : IA
{
    new int P { get; }
}

public class C<T> where T : IB, IA
{
    void M()
    {
        new T[1]./*<bind>*/Select/*</bind>*/(i => i.P);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { tree });
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSymbolInfo(exprSyntaxToBind);
            Assert.Equal("System.Collections.Generic.IEnumerable<System.Int32> System.Collections.Generic.IEnumerable<T>.Select<T, System.Int32>(System.Func<T, System.Int32> selector)", bindInfo.Symbol.ToTestDisplayString());
        }

        [WorkItem(543295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543295")]
        [Fact]
        public void TypeParameterDiamondInheritance4() //Two interfaces with a common base
        {
            var text = @"
using System.Linq;
 
public interface IA
{
    object P { get; }
}
 
public interface IB : IA { }

public interface IC : IA { }

public class C<T> where T : IB, IC
{
    void M()
    {
        new T[1]./*<bind>*/Select/*</bind>*/(i => i.P);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { tree });
            var model = comp.GetSemanticModel(tree);
            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            Assert.Equal(SyntaxKind.IdentifierName, exprSyntaxToBind.Kind());

            var bindInfo = model.GetSymbolInfo(exprSyntaxToBind);
            Assert.Equal("System.Collections.Generic.IEnumerable<System.Object> System.Collections.Generic.IEnumerable<T>.Select<T, System.Object>(System.Func<T, System.Object> selector)", bindInfo.Symbol.ToTestDisplayString());
        }

        [WorkItem(543295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543295")]
        [Fact]
        public void TypeParameterMemberLookup1()
        {
            var types = @"
public interface IA
{
    object P { get; }
}
";
            var members = LookupTypeParameterMembers(types, "IA", "P", out _);
            Assert.Equal("System.Object IA.P { get; }", members.Single().ToTestDisplayString());
        }

        [WorkItem(543295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543295")]
        [Fact]
        public void TypeParameterMemberLookup2()
        {
            var types = @"
public interface IA
{
    object P { get; }
}

public interface IB
{
    object P { get; }
}
";

            ITypeParameterSymbol typeParameter;
            var members = LookupTypeParameterMembers(types, "IA, IB", "P", out typeParameter);
            Assert.True(members.SetEquals(typeParameter.AllEffectiveInterfacesNoUseSiteDiagnostics().Select(i => i.GetMember<IPropertySymbol>("P"))));
        }

        [WorkItem(543295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543295")]
        [Fact]
        public void TypeParameterMemberLookup3()
        {
            var types = @"
public interface IA
{
    object P { get; }
}

public interface IB : IA
{
    new object P { get; }
}
";

            var members = LookupTypeParameterMembers(types, "IA, IB", "P", out _);
            Assert.Equal("System.Object IB.P { get; }", members.Single().ToTestDisplayString());
        }

        [WorkItem(543295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543295")]
        [Fact]
        public void TypeParameterMemberLookup4()
        {
            var types = @"
public interface IA
{
    object P { get; }
}

public class D
{
    public object P { get; set; }
}
";
            var members = LookupTypeParameterMembers(types, "D, IA", "P", out _);
            Assert.Equal("System.Object D.P { get; set; }", members.Single().ToTestDisplayString());
        }

        [WorkItem(543295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543295")]
        [Fact]
        public void TypeParameterMemberLookup5()
        {
            var types = @"
public interface IA
{
    void M();
}

public class D
{
    public void M() { }
}
";
            var members = LookupTypeParameterMembers(types, "IA", "M", out _);
            Assert.Equal("void IA.M()", members.Single().ToTestDisplayString());

            members = LookupTypeParameterMembers(types, "D, IA", "M", out _);
            Assert.True(members.Select(m => m.ToTestDisplayString()).SetEquals(new[] { "void IA.M()", "void D.M()" }));
        }

        [WorkItem(543295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543295")]
        [Fact]
        public void TypeParameterMemberLookup6()
        {
            var types = @"
public interface IA
{
    void M();
    void M(int x);
}

public class D
{
    public void M() { }
}
";
            var members = LookupTypeParameterMembers(types, "IA", "M", out _);
            Assert.True(members.Select(m => m.ToTestDisplayString()).SetEquals(new[] { "void IA.M()", "void IA.M(System.Int32 x)" }));

            members = LookupTypeParameterMembers(types, "D, IA", "M", out _);
            Assert.True(members.Select(m => m.ToTestDisplayString()).SetEquals(new[] { "void D.M()", "void IA.M()", "void IA.M(System.Int32 x)" }));
        }

        [WorkItem(543295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543295")]
        [Fact]
        public void TypeParameterMemberLookup7()
        {
            var types = @"
public interface IA
{
    string ToString();
}

public class D
{
    public new string ToString() { return null; }
}
";
            var members = LookupTypeParameterMembers(types, "IA", "ToString", out _);
            Assert.True(members.Select(m => m.ToTestDisplayString()).SetEquals(new[] { "System.String System.Object.ToString()", "System.String IA.ToString()" }));

            members = LookupTypeParameterMembers(types, "D, IA", "ToString", out _);
            Assert.True(members.Select(m => m.ToTestDisplayString()).SetEquals(new[] { "System.String System.Object.ToString()", "System.String D.ToString()", "System.String IA.ToString()" }));
        }

        private IEnumerable<ISymbol> LookupTypeParameterMembers(string types, string constraints, string memberName, out ITypeParameterSymbol typeParameter)
        {
            var template = @"
{0}
 
public class C<T> where T : {1}
{{
    void M()
    {{
        System.Console.WriteLine(/*<bind>*/default(T)/*</bind>*/);
    }}
}}
";

            var tree = Parse(string.Format(template, types, constraints));
            var comp = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(new[] { tree });
            comp.VerifyDiagnostics();
            var model = comp.GetSemanticModel(tree);

            var classC = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("C");
            typeParameter = classC.TypeParameters.Single();

            var exprSyntaxToBind = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.DefaultExpression, exprSyntaxToBind.Kind());

            return model.LookupSymbols(exprSyntaxToBind.SpanStart, typeParameter, memberName);
        }

        [WorkItem(542966, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542966")]
        [Fact]
        public async Task IndexerMemberRaceAsync()
        {
            var text = @"
using System;

interface IA
{
    [System.Runtime.CompilerServices.IndexerName(""Goo"")]
    string this[int index] { get; }
}
 
class A : IA
{
    public virtual string this[int index]
    {
        get { return """"; }
    }
 
    string IA.this[int index]
    {
        get { return """"; }
    }
}
 
class B : A, IA
{
    public override string this[int index]
    {
        get { return """"; }
    }
}
class Program
{
    public static void Main(string[] args)
    {
        IA x = new B();
        Console.WriteLine(x[0]);
    }
}
";

            TimeSpan timeout = TimeSpan.FromSeconds(2);

            for (int i = 0; i < 20; i++)
            {
                var comp = CreateCompilation(text);

                var task1 = new Task(() => comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMembers());
                var task2 = new Task(() => comp.GlobalNamespace.GetMember<NamedTypeSymbol>("IA").GetMembers());

                if (i % 2 == 0)
                {
                    task1.Start();
                    task2.Start();
                }
                else
                {
                    task2.Start();
                    task1.Start();
                }

                comp.VerifyDiagnostics();

                await Task.WhenAll(task1, task2);
            }
        }

        [Fact]
        public void ImplicitDeclarationMultipleDeclarators()
        {
            var text = @"
using System.IO;

class C
{
    static void Main()
    {
        /*<bind>*/var a = new StreamWriter(""""), b = new StreamReader("""")/*</bind>*/;
    }
}
";

            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics(
                // (8,19): error CS0819: Implicitly-typed variables cannot have multiple declarators
                //         /*<bind>*/var a = new StreamWriter(""), b = new StreamReader("")/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, @"var a = new StreamWriter(""""), b = new StreamReader("""")").WithLocation(8, 19)
                );

            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var typeInfo = model.GetSymbolInfo(expr);
            // the type info uses the type inferred for the first declared local
            Assert.Equal("System.IO.StreamWriter", typeInfo.Symbol.ToTestDisplayString());
        }

        [WorkItem(543169, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543169")]
        [Fact]
        public void ParameterOfLambdaPassedToOutParameter()
        {
            var text = @"
using System.Linq;
class D
{
    static void Main(string[] args)
    {
        string[] str = new string[] { };
    label1:
        var s = str.Where(out /*<bind>*/x/*</bind>*/ =>
        {
            return x == ""1"";
        });
    }
}
";

            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var lambdaSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().Single();
            var parameterSymbol = model.GetDeclaredSymbol(lambdaSyntax.Parameter);
            Assert.NotNull(parameterSymbol);
            Assert.Equal("x", parameterSymbol.Name);
            Assert.Equal(MethodKind.AnonymousFunction, ((IMethodSymbol)parameterSymbol.ContainingSymbol).MethodKind);
        }

        [WorkItem(529096, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529096")]
        [Fact]
        public void MemberAccessExpressionResults()
        {
            var text = @"
class C
{
    public static int A;

    public static byte B() { return 3; }

    public static string D { get; set; }

    static void Main(string[] args)
    {
        /*<bind0>*/C.A/*</bind0>*/;
        /*<bind1>*/C.B/*</bind1>*/();
        /*<bind2>*/C.D/*</bind2>*/;
        /*<bind3>*/C.B()/*</bind3>*/;
        int goo = /*<bind4>*/C.B()/*</bind4>*/;
        goo = /*<bind5>*/C.B/*</bind5>*/();
    }
}
";

            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var exprs = GetBindingNodes<ExpressionSyntax>(comp);
            for (int i = 0; i < exprs.Count; i++)
            {
                var expr = exprs[i];
                var symbolInfo = model.GetSymbolInfo(expr);
                Assert.NotNull(symbolInfo.Symbol);
                var typeInfo = model.GetTypeInfo(expr);
                switch (i)
                {
                    case 0:
                        Assert.Equal("A", symbolInfo.Symbol.Name);
                        Assert.NotNull(typeInfo.Type);
                        Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString());
                        Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());
                        break;
                    case 1:
                    case 5:
                        Assert.Equal("B", symbolInfo.Symbol.Name);
                        Assert.Null(typeInfo.Type);
                        break;
                    case 2:
                        Assert.Equal("D", symbolInfo.Symbol.Name);
                        Assert.NotNull(typeInfo.Type);
                        Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString());
                        Assert.Equal("System.String", typeInfo.ConvertedType.ToTestDisplayString());
                        break;
                    case 3:
                        Assert.Equal("B", symbolInfo.Symbol.Name);
                        Assert.NotNull(typeInfo.Type);
                        Assert.Equal("System.Byte", typeInfo.Type.ToTestDisplayString());
                        Assert.Equal("System.Byte", typeInfo.ConvertedType.ToTestDisplayString());
                        break;
                    case 4:
                        Assert.Equal("B", symbolInfo.Symbol.Name);
                        Assert.NotNull(typeInfo.Type);
                        Assert.Equal("System.Byte", typeInfo.Type.ToTestDisplayString());
                        Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString());
                        break;
                } // switch
            }
        }

        [WorkItem(543554, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543554")]
        [Fact]
        public void SemanticInfoForUncheckedExpression()
        {
            var text = @"
public class A 
{
    static void Main() 
    {	
         Console.WriteLine(/*<bind>*/unchecked(42 + 42.1)/*</bind>*/);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var sym = model.GetSymbolInfo(expr);
            Assert.Equal("System.Double System.Double.op_Addition(System.Double left, System.Double right)", sym.Symbol.ToTestDisplayString());

            var info = model.GetTypeInfo(expr);
            var conv = model.GetConversion(expr);
            Assert.Equal(ConversionKind.Identity, conv.Kind);
            Assert.Equal(SpecialType.System_Double, info.Type.SpecialType);
            Assert.Equal(SpecialType.System_Double, info.ConvertedType.SpecialType);
        }

        [WorkItem(543554, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543554")]
        [Fact]
        public void SemanticInfoForCheckedExpression()
        {
            var text = @"
class Program
{
    public static int Add(int a, int b) 
    {
        return /*<bind>*/checked(a+b)/*</bind>*/;
    } 
}
";
            var comp = CreateCompilation(text);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            var sym = model.GetSymbolInfo(expr);
            Assert.Equal("System.Int32 System.Int32.op_CheckedAddition(System.Int32 left, System.Int32 right)", sym.Symbol.ToTestDisplayString());

            var info = model.GetTypeInfo(expr);
            var conv = model.GetConversion(expr);
            Assert.Equal(ConversionKind.Identity, conv.Kind);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, info.ConvertedType.SpecialType);
        }

        [WorkItem(543554, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543554")]
        [Fact]
        public void CheckedUncheckedExpression()
        {
            var text = @"
class Test
{
    public void F()
    {
        int y = /*<bind>*/(checked(unchecked((1))))/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            var info = model.GetTypeInfo(expr);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, info.ConvertedType.SpecialType);
        }

        [WorkItem(543543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543543")]
        [Fact]
        public void SymbolInfoForImplicitOperatorParameter()
        {
            var text = @"
class Program
{
    public Program(string s)
    {
    }
 
    public static implicit operator Program(string str)
    {
        return new Program(/*<bind>*/str/*</bind>*/);
    }
} 

";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            var info = model.GetSymbolInfo(expr);
            var symbol = info.Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Equal(MethodKind.Conversion, ((IMethodSymbol)symbol.ContainingSymbol).MethodKind);
        }

        [WorkItem(543494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543494")]
        [WorkItem(543560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543560")]
        [Fact]
        public void BrokenPropertyDeclaration()
        {
            var source = @"
using System;

Class Program // this will get a Property declaration ... *sigh*
{
    static void Main(string[] args)
    {
        Func<int, int> f = /*<bind0>*/x/*</bind0>*/ => x + 1;
    }
}
";
            var compilation = CreateCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var expr = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            var declaredSymbol = model.GetDeclaredSymbol(expr);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SemanticInfoForUserDefinedBinaryOperator()
        {
            var text = @"
class C
{
    public static C operator+(C c1, C c2)
    {
        return c1 ?? c2;
    }

    static void Main()
    {
        C c1 = new C();
        C c2 = new C();
        C c3 = /*<bind>*/c1 + c2/*</bind>*/;
    }
}
";
            CheckOperatorSemanticInfo(text, WellKnownMemberNames.AdditionOperatorName);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SemanticInfoForUserDefinedLogicalOperator()
        {
            var text = @"
class C
{
    public static C operator &(C c1, C c2)
    {
        return c1 ?? c2;
    }

    public static bool operator true(C c)
    {
        return true;
    }

    public static bool operator false(C c)
    {
        return false;
    }

    static void Main()
    {
        C c1 = new C();
        C c2 = new C();
        C c3 = /*<bind>*/c1 && c2/*</bind>*/;
    }
}
";
            CheckOperatorSemanticInfo(text, WellKnownMemberNames.BitwiseAndOperatorName);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SemanticInfoForUserDefinedUnaryOperator()
        {
            var text = @"
class C
{
    public static C operator+(C c1)
    {
        return c1;
    }

    static void Main()
    {
        C c1 = new C();
        C c2 = /*<bind>*/+c1/*</bind>*/;
    }
}
";
            CheckOperatorSemanticInfo(text, WellKnownMemberNames.UnaryPlusOperatorName);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SemanticInfoForUserDefinedExplicitConversion()
        {
            var text = @"
class C
{
    public static explicit operator C(int i)
    {
        return null;
    }

    static void Main()
    {
        C c1 = /*<bind>*/(C)1/*</bind>*/;
    }
}
";
            CheckOperatorSemanticInfo(text, WellKnownMemberNames.ExplicitConversionName);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SemanticInfoForUserDefinedImplicitConversion()
        {
            var text = @"
class C
{
    public static implicit operator C(int i)
    {
        return null;
    }

    static void Main()
    {
        C c1 = /*<bind>*/(C)1/*</bind>*/;
    }
}
";
            CheckOperatorSemanticInfo(text, WellKnownMemberNames.ImplicitConversionName);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SemanticInfoForUserDefinedTrueOperator()
        {
            var text = @"
class C
{
    public static bool operator true(C c)
    {
        return true;
    }
    public static bool operator false(C c)
    {
        return false;
    }

    static void Main()
    {
        C c = new C();  
        if (/*<bind>*/c/*</bind>*/)
        {

        }
    }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var type = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("C");

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            var symbol = symbolInfo.Symbol;
            Assert.Equal(SymbolKind.Local, symbol.Kind);
            Assert.Equal("c", symbol.Name);
            Assert.Equal(type, ((ILocalSymbol)symbol).Type);

            var typeInfo = model.GetTypeInfo(expr);
            Assert.Equal(type, typeInfo.Type);
            Assert.Equal(type, typeInfo.ConvertedType);
            var conv = model.GetConversion(expr);
            Assert.Equal(Conversion.Identity, conv);

            Assert.False(model.GetConstantValue(expr).HasValue);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SemanticInfoForUserDefinedIncrement()
        {
            var text = @"
class C
{
    public static C operator ++(C c)
    {
        return c;
    }

    static void Main()
    {
        C c1 = new C();  
        C c2 = /*<bind>*/c1++/*</bind>*/;  
    }
}
";
            CheckOperatorSemanticInfo(text, WellKnownMemberNames.IncrementOperatorName);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SemanticInfoForUserDefinedCompoundAssignment()
        {
            var text = @"
class C
{
    public static C operator +(C c1, C c2)
    {
        return c1 ?? c2;
    }

    static void Main()
    {
        C c1 = new C();  
        C c2 = new C();  
        /*<bind>*/c1 += c2/*</bind>*/;
    }
}
";
            CheckOperatorSemanticInfo(text, WellKnownMemberNames.AdditionOperatorName);
        }

        private void CheckOperatorSemanticInfo(string text, string operatorName)
        {
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var operatorSymbol = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>(operatorName);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Equal(operatorSymbol, symbolInfo.Symbol);

            var method = (IMethodSymbol)symbolInfo.Symbol;
            var returnType = method.ReturnType;

            var typeInfo = model.GetTypeInfo(expr);
            Assert.Equal(returnType, typeInfo.Type);
            Assert.Equal(returnType, typeInfo.ConvertedType);
            var conv = model.GetConversion(expr);
            Assert.Equal(ConversionKind.Identity, conv.Kind);

            Assert.False(model.GetConstantValue(expr).HasValue);
        }

        [Fact, WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550"), WorkItem(543439, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543439")]
        public void SymbolInfoForUserDefinedConversionOverloadResolutionFailure()
        {
            var text = @"
struct S
{
    public static explicit operator S(string s)
    {
        return default(S);
    }

    public static explicit operator S(System.Text.StringBuilder s)
    {
        return default(S);
    }

    static void Main()
    {
        S s = /*<bind>*/(S)null/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var conversions = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("S").GetMembers(WellKnownMemberNames.ExplicitConversionName);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);

            var candidates = symbolInfo.CandidateSymbols;
            Assert.Equal(2, candidates.Length);
            Assert.True(candidates.SetEquals(conversions, EqualityComparer<ISymbol>.Default));
        }

        [Fact, WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550"), WorkItem(543439, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543439")]
        public void SymbolInfoForUserDefinedConversionOverloadResolutionFailureEmpty()
        {
            var text = @"
struct S
{
    static void Main()
    {
        S s = /*<bind>*/(S)null/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var conversions = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("S").GetMembers(WellKnownMemberNames.ExplicitConversionName);
            Assert.Equal(0, conversions.Length);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

            var candidates = symbolInfo.CandidateSymbols;
            Assert.Equal(0, candidates.Length);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SymbolInfoForUserDefinedUnaryOperatorOverloadResolutionFailure()
        {
            var il = @"
.class public auto ansi beforefieldinit UnaryOperator
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class UnaryOperator  op_UnaryPlus(class UnaryOperator unaryOperator) cil managed
  {
    ldnull
    throw
  }

  // Differs only by return type
  .method public hidebysig specialname static 
          string  op_UnaryPlus(class UnaryOperator unaryOperator) cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            var text = @"
class Program
{
    static void Main()
    {
        UnaryOperator u1 = new UnaryOperator();
        UnaryOperator u2 = /*<bind>*/+u1/*</bind>*/;
    }
}
";
            var comp = (Compilation)CreateCompilationWithILAndMscorlib40(text, il);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var operators = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("UnaryOperator").GetMembers(WellKnownMemberNames.UnaryPlusOperatorName);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);

            var candidates = symbolInfo.CandidateSymbols;
            Assert.Equal(2, candidates.Length);
            Assert.True(candidates.SetEquals(operators, EqualityComparer<ISymbol>.Default));
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SymbolInfoForUserDefinedUnaryOperatorOverloadResolutionFailureEmpty()
        {
            var text = @"
class C
{
    static void Main()
    {
        C c1 = new C();
        C c2 = /*<bind>*/+c1/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var operators = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.UnaryPlusOperatorName);
            Assert.Equal(0, operators.Length);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

            var candidates = symbolInfo.CandidateSymbols;
            Assert.Equal(0, candidates.Length);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SymbolInfoForUserDefinedIncrementOperatorOverloadResolutionFailure()
        {
            var il = @"
.class public auto ansi beforefieldinit IncrementOperator
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname static 
          class IncrementOperator  op_Increment(class IncrementOperator incrementOperator) cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname static 
          string  op_Increment(class IncrementOperator incrementOperator) cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

            var text = @"
class Program
{
    static void Main()
    {
        IncrementOperator i1 = new IncrementOperator();
        IncrementOperator i2 = /*<bind>*/i1++/*</bind>*/;
    }
}
";
            var comp = (Compilation)CreateCompilationWithILAndMscorlib40(text, il);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var operators = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("IncrementOperator").GetMembers(WellKnownMemberNames.IncrementOperatorName);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);

            var candidates = symbolInfo.CandidateSymbols;
            Assert.Equal(2, candidates.Length);
            Assert.True(candidates.SetEquals(operators, EqualityComparer<ISymbol>.Default));
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SymbolInfoForUserDefinedIncrementOperatorOverloadResolutionFailureEmpty()
        {
            var text = @"
class C
{
    static void Main()
    {
        C c1 = new C();
        C c2 = /*<bind>*/c1++/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var operators = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.IncrementOperatorName);
            Assert.Equal(0, operators.Length);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

            var candidates = symbolInfo.CandidateSymbols;
            Assert.Equal(0, candidates.Length);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SymbolInfoForUserDefinedBinaryOperatorOverloadResolutionFailure()
        {
            var text = @"
class C
{
    public static C operator+(C c1, C c2)
    {
        return c1 ?? c2;
    }

    public static C operator+(C c1, string s)
    {
        return c1;
    }

    static void Main()
    {
        C c1 = new C();
        C c2 = /*<bind>*/c1 + null/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var operators = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.AdditionOperatorName);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);

            var candidates = symbolInfo.CandidateSymbols;
            Assert.Equal(2, candidates.Length);
            Assert.True(candidates.SetEquals(operators, EqualityComparer<ISymbol>.Default));
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SymbolInfoForUserDefinedBinaryOperatorOverloadResolutionFailureEmpty()
        {
            var text = @"
class C
{
    static void Main()
    {
        C c1 = new C();
        C c2 = /*<bind>*/c1 + null/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var operators = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.AdditionOperatorName);
            Assert.Equal(0, operators.Length);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Equal("System.String System.String.op_Addition(System.Object left, System.String right)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

            var candidates = symbolInfo.CandidateSymbols;
            Assert.Equal(0, candidates.Length);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SymbolInfoForUserDefinedCompoundAssignmentOperatorOverloadResolutionFailure()
        {
            var text = @"
class C
{
    public static C operator+(C c1, C c2)
    {
        return c1 ?? c2;
    }

    public static C operator+(C c1, string s)
    {
        return c1;
    }

    static void Main()
    {
        C c = new C();
        /*<bind>*/c += null/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var operators = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.AdditionOperatorName);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.Ambiguous, symbolInfo.CandidateReason);

            var candidates = symbolInfo.CandidateSymbols;
            Assert.Equal(2, candidates.Length);
            Assert.True(candidates.SetEquals(operators, EqualityComparer<ISymbol>.Default));
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550")]
        [Fact]
        public void SymbolInfoForUserDefinedCompoundAssignmentOperatorOverloadResolutionFailureEmpty()
        {
            var text = @"
class C
{
    static void Main()
    {
        C c = new C();
        /*<bind>*/c += null/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var operators = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.AdditionOperatorName);
            Assert.Equal(0, operators.Length);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Equal("System.String System.String.op_Addition(System.Object left, System.String right)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);

            var candidates = symbolInfo.CandidateSymbols;
            Assert.Equal(0, candidates.Length);
        }

        [WorkItem(543550, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543550"), WorkItem(529158, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529158")]
        [Fact]
        public void MethodGroupForUserDefinedBinaryOperator()
        {
            var text = @"
class C
{
    public static C operator+(C c1, C c2)
    {
        return c1 ?? c2;
    }

    public static C operator+(C c1, int i)
    {
        return c1;
    }

    static void Main()
    {
        C c1 = new C();
        C c2 = new C();
        C c3 = /*<bind>*/c1 + c2/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var operators = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMembers(WellKnownMemberNames.AdditionOperatorName).Cast<IMethodSymbol>();
            var operatorSymbol = operators.Where(method => method.Parameters[0].Type.Equals(method.Parameters[1].Type, SymbolEqualityComparer.ConsiderEverything)).Single();

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Equal(operatorSymbol, symbolInfo.Symbol);

            // NOTE: This check captures, rather than enforces, the current behavior (i.e. feel free to change it).
            var memberGroup = model.GetMemberGroup(expr);
            Assert.Equal(0, memberGroup.Length);
        }

        [Fact]
        public void CacheDuplicates()
        {
            var text = @"
class C
{
    static void Main()
    {
        long l = /*<bind>*/(long)1/*</bind>*/;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            bool sawWrongConversionKind = false;
            ThreadStart ts = () => sawWrongConversionKind |= ConversionKind.Identity != model.GetConversion(expr).Kind;

            Thread[] threads = new Thread[4];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(ts);
            }

            foreach (Thread t in threads)
            {
                t.Start();
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }

            Assert.False(sawWrongConversionKind);
        }

        [WorkItem(543674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543674")]
        [Fact()]
        public void SemanticInfo_NormalVsLiftedUserDefinedImplicitConversion()
        {
            string text = @"
using System;

struct G { }
struct L
{
    public static implicit operator G(L l) { return default(G); }
}

class Z
{
    public static void Main()
    {
        MNG(/*<bind>*/default(L)/*</bind>*/);
    }

    static void MNG(G? g)
    {
    }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var gType = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("G");
            var mngMethod = (IMethodSymbol)comp.GlobalNamespace.GetMember<INamedTypeSymbol>("Z").GetMembers("MNG").First();
            var gNullableType = mngMethod.GetParameterType(0);
            Assert.True(gNullableType.IsNullableType(), "MNG parameter is not a nullable type?");
            Assert.Equal(gType, gNullableType.StrippedType());

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var conversion = model.ClassifyConversion(expr, gNullableType);
            CheckIsAssignableTo(model, expr);

            // Here we have a situation where Roslyn deliberately violates the specification in order
            // to be compatible with the native compiler. 
            //
            // The specification states that there are two applicable candidates: We can use
            // the "lifted" operator from L? to G?, or we can use the unlifted operator
            // from L to G, and then convert the G that comes out the back end to G?.
            // The specification says that the second conversion is the better conversion.
            // Therefore, the conversion on the "front end" should be an identity conversion,
            // and the conversion on the "back end" of the user-defined conversion should
            // be an implicit nullable conversion.
            //
            // This is not at all what the native compiler does, and we match the native
            // compiler behavior. The native compiler says that there is a "half lifted"
            // conversion from L-->G?, and that this is the winner. Therefore the conversion
            // "on the back end" of the user-defined conversion is in fact an *identity*
            // conversion, even though obviously we are going to have to
            // do code generation as though it was an implicit nullable conversion.

            Assert.Equal(ConversionKind.Identity, conversion.UserDefinedFromConversion.Kind);
            Assert.Equal(ConversionKind.Identity, conversion.UserDefinedToConversion.Kind);
            Assert.Equal("ImplicitUserDefined", conversion.ToString());
        }

        [WorkItem(543715, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543715")]
        [Fact]
        public void SemanticInfo_NormalVsLiftedUserDefinedConversion_ImplicitConversion()
        {
            string text = @"
using System;

struct G {}
struct M
{
  public static implicit operator G(M m) { System.Console.WriteLine(1); return default(G); }
  public static implicit operator G(M? m) {System.Console.WriteLine(2); return default(G); }
}

class Z
{
    public static void Main()
    {
        M? m = new M();
        MNG(/*<bind>*/m/*</bind>*/);
    }

    static void MNG(G? g)
    {
    }
}
";
            var tree = Parse(text);
            var comp = (Compilation)CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var gType = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("G");
            var mngMethod = (IMethodSymbol)comp.GlobalNamespace.GetMember<INamedTypeSymbol>("Z").GetMembers("MNG").First();
            var gNullableType = mngMethod.GetParameterType(0);
            Assert.True(gNullableType.IsNullableType(), "MNG parameter is not a nullable type?");
            Assert.Equal(gType, gNullableType.StrippedType());

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var conversion = model.ClassifyConversion(expr, gNullableType);
            CheckIsAssignableTo(model, expr);

            Assert.Equal(ConversionKind.ImplicitUserDefined, conversion.Kind);

            // Dev10 violates the spec for finding the most specific operator for an implicit user-defined conversion.

            // SPEC:    •	Find the most specific conversion operator:
            // SPEC:        (a)	If U contains exactly one user-defined conversion operator that converts from SX to TX, then this is the most specific conversion operator.
            // SPEC:        (b)	Otherwise, if U contains exactly one lifted conversion operator that converts from SX to TX, then this is the most specific conversion operator.
            // SPEC:        (c)	Otherwise, the conversion is ambiguous and a compile-time error occurs.

            // In this test we try to classify conversion from M? to G?.
            // 1) Classify conversion establishes that SX: M? and TX: G?.
            // 2) Most specific conversion operator from M? to G?:
            //      (a) does not hold here as neither of the implicit operators convert from M? to G?
            //      (b) does hold here as the lifted form of "implicit operator G(M m)" converts from M? to G?

            // Hence "operator G(M m)" must be chosen in lifted form, but Dev10 chooses "G M.op_Implicit(System.Nullable<M> m)" in normal form.

            // We may want to maintain compatibility with Dev10.
            Assert.Equal("G M.op_Implicit(M? m)", conversion.MethodSymbol.ToTestDisplayString());
            Assert.Equal("ImplicitUserDefined", conversion.ToString());
        }

        [Fact]
        public void AmbiguousImplicitConversionOverloadResolution1()
        {
            var source = @"
public class A
{
    static public implicit operator A(B b)
    {
        return default(A);
    }
}

public class B
{
    static public implicit operator A(B b)
    {
        return default(A);
    }
}

class Test
{
    static void M(A a) { }
    static void M(object o) { }

    static void Main()
    {
        B b = new B();
        /*<bind>*/M(b)/*</bind>*/;
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (26,21): error CS0457: Ambiguous user defined conversions 'B.implicit operator A(B)' and 'A.implicit operator A(B)' when converting from 'B' to 'A'
                Diagnostic(ErrorCode.ERR_AmbigUDConv, "b").WithArguments("B.implicit operator A(B)", "A.implicit operator A(B)", "B", "A"));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var expr = (InvocationExpressionSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Equal("void Test.M(A a)", symbolInfo.Symbol.ToTestDisplayString());

            var argexpr = expr.ArgumentList.Arguments.Single().Expression;
            var argTypeInfo = model.GetTypeInfo(argexpr);
            Assert.Equal("B", argTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("A", argTypeInfo.ConvertedType.ToTestDisplayString());

            var argConversion = model.GetConversion(argexpr);
            Assert.Equal(ConversionKind.ImplicitUserDefined, argConversion.Kind);
            Assert.False(argConversion.IsValid);
            Assert.Null(argConversion.Method);
        }

        [Fact]
        public void AmbiguousImplicitConversionOverloadResolution2()
        {
            var source = @"
public class A
{
    static public implicit operator A(B<A> b)
    {
        return default(A);
    }
}

public class B<T>
{
    static public implicit operator T(B<T> b)
    {
        return default(T);
    }
}

class C
{
    static void M(A a) { }
    static void M<T>(T t) { }

    static void Main()
    {
        B<A> b = new B<A>();
        /*<bind>*/M(b)/*</bind>*/;
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(); // since no conversion is performed, the ambiguity doesn't matter

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var expr = (InvocationExpressionSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.NotNull(expr);

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Equal("void C.M<B<A>>(B<A> t)", symbolInfo.Symbol.ToTestDisplayString());

            var argexpr = expr.ArgumentList.Arguments.Single().Expression;
            var argTypeInfo = model.GetTypeInfo(argexpr);
            Assert.Equal("B<A>", argTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("B<A>", argTypeInfo.ConvertedType.ToTestDisplayString());

            var argConversion = model.GetConversion(argexpr);
            Assert.Equal(ConversionKind.Identity, argConversion.Kind);
            Assert.True(argConversion.IsValid);
            Assert.Null(argConversion.Method);
        }

        [Fact]
        public void DefaultParameterLocalScope()
        {
            var source = @"
public class A
{
    static void Main(string[] args, int a = /*<bind>*/System/*</bind>*/.)
    {
    }
}";
            var compilation = CreateCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal("System", expr.ToString());

            var info = model.GetSemanticInfoSummary(expr); //Shouldn't throw/assert
            Assert.Equal(SymbolKind.Namespace, info.Symbol.Kind);
        }

        [Fact]
        public void PinvokeSemanticModel()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;
class C
{
    [DllImport(""user32.dll"", CharSet = CharSet.Unicode, ExactSpelling = false, EntryPoint = ""MessageBox"")]
    public static extern int MessageBox(IntPtr hwnd, string t, string c, UInt32 t2);

    static void Main()
    {
         /*<bind>*/MessageBox(IntPtr.Zero, """", """", 1)/*</bind>*/;
    }
}";
            var compilation = CreateCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var expr = (InvocationExpressionSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal("MessageBox(IntPtr.Zero, \"\", \"\", 1)", expr.ToString());

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Equal("C.MessageBox(System.IntPtr, string, string, uint)", symbolInfo.Symbol.ToDisplayString());

            var argTypeInfo = model.GetTypeInfo(expr.ArgumentList.Arguments.First().Expression);
            Assert.Equal("System.IntPtr", argTypeInfo.Type.ToTestDisplayString());
            Assert.Equal("System.IntPtr", argTypeInfo.ConvertedType.ToTestDisplayString());
        }

        [Fact]
        public void ImplicitBoxingConversion1()
        {
            var source = @"
class C
{
    static void Main()
    {
        object o = 1;
    }
}";
            var compilation = CreateCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var literal = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single();

            var literalTypeInfo = model.GetTypeInfo(literal);
            var conv = model.GetConversion(literal);
            Assert.Equal(SpecialType.System_Int32, literalTypeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Object, literalTypeInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Boxing, conv.Kind);
        }

        [Fact]
        public void ImplicitBoxingConversion2()
        {
            var source = @"
class C
{
    static void Main()
    {
        object o = (long)1;
    }
}";
            var compilation = CreateCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var literal = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single();

            var literalTypeInfo = model.GetTypeInfo(literal);
            var literalConversion = model.GetConversion(literal);
            Assert.Equal(SpecialType.System_Int32, literalTypeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, literalTypeInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, literalConversion.Kind);

            var cast = (CastExpressionSyntax)literal.Parent;

            var castTypeInfo = model.GetTypeInfo(cast);
            var castConversion = model.GetConversion(cast);
            Assert.Equal(SpecialType.System_Int64, castTypeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Object, castTypeInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Boxing, castConversion.Kind);
        }

        [Fact]
        public void ExplicitBoxingConversion1()
        {
            var source = @"
class C
{
    static void Main()
    {
        object o = (object)1;
    }
}";
            var compilation = CreateCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var literal = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single();

            var literalTypeInfo = model.GetTypeInfo(literal);
            var literalConversion = model.GetConversion(literal);
            Assert.Equal(SpecialType.System_Int32, literalTypeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, literalTypeInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, literalConversion.Kind);

            var cast = (CastExpressionSyntax)literal.Parent;

            var castTypeInfo = model.GetTypeInfo(cast);
            var castConversion = model.GetConversion(cast);
            Assert.Equal(SpecialType.System_Object, castTypeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Object, castTypeInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, castConversion.Kind);

            Assert.Equal(ConversionKind.Boxing, model.ClassifyConversion(literal, castTypeInfo.Type).Kind);
            CheckIsAssignableTo(model, literal);
        }

        [Fact]
        public void ExplicitBoxingConversion2()
        {
            var source = @"
class C
{
    static void Main()
    {
        object o = (object)(long)1;
    }
}";
            var compilation = CreateCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var literal = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single();

            var literalTypeInfo = model.GetTypeInfo(literal);
            var literalConversion = model.GetConversion(literal);
            Assert.Equal(SpecialType.System_Int32, literalTypeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int32, literalTypeInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, literalConversion.Kind);

            var cast1 = (CastExpressionSyntax)literal.Parent;

            var cast1TypeInfo = model.GetTypeInfo(cast1);
            var cast1Conversion = model.GetConversion(cast1);
            Assert.Equal(SpecialType.System_Int64, cast1TypeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int64, cast1TypeInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, cast1Conversion.Kind);

            // Note that this reflects the hypothetical conversion, not the cast in the code.
            Assert.Equal(ConversionKind.ImplicitNumeric, model.ClassifyConversion(literal, cast1TypeInfo.Type).Kind);
            CheckIsAssignableTo(model, literal);

            var cast2 = (CastExpressionSyntax)cast1.Parent;

            var cast2TypeInfo = model.GetTypeInfo(cast2);
            var cast2Conversion = model.GetConversion(cast2);
            Assert.Equal(SpecialType.System_Object, cast2TypeInfo.Type.SpecialType);
            Assert.Equal(SpecialType.System_Object, cast2TypeInfo.ConvertedType.SpecialType);
            Assert.Equal(ConversionKind.Identity, cast2Conversion.Kind);

            Assert.Equal(ConversionKind.Boxing, model.ClassifyConversion(cast1, cast2TypeInfo.Type).Kind);
            CheckIsAssignableTo(model, cast1);
        }

        [WorkItem(545136, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545136")]
        [WorkItem(538320, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538320")]
        [Fact()] // TODO: Dev10 does not report ERR_SameFullNameAggAgg here - source wins.
        public void SpecialTypeInSourceAndMetadata()
        {
            var text = @"
using System;
 
namespace System
{
    public struct Void
    {
        static void Main()
        {
            System./*<bind>*/Void/*</bind>*/.Equals(1, 1);
        }
    }
}
";

            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var expr = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            var symbolInfo = model.GetSymbolInfo(expr);
            Assert.Equal("System.Void", symbolInfo.Symbol.ToString());
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
        }

        [WorkItem(544651, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544651")]
        [Fact]
        public void SpeculativelyBindMethodGroup1()
        {
            var text = @"
using System;

class C
{
    static void M()
    {
        int here;
    }
}
";

            var compilation = (Compilation)CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var position = text.IndexOf("here", StringComparison.Ordinal);
            var syntax = SyntaxFactory.ParseExpression("       C.M"); //Leading trivia was significant for triggering an assert before the fix.
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, syntax.Kind());

            var info = model.GetSpeculativeSymbolInfo(position, syntax, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("M"), info.CandidateSymbols.Single());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
        }

        [WorkItem(544651, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544651")]
        [Fact]
        public void SpeculativelyBindMethodGroup2()
        {
            var text = @"
using System;

class C
{
    static void M()
    {
        int here;
    }

    static void M(int x)
    {
    }
}
";

            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var position = text.IndexOf("here", StringComparison.Ordinal);
            var syntax = SyntaxFactory.ParseExpression("       C.M"); //Leading trivia was significant for triggering an assert before the fix.
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, syntax.Kind());

            var info = model.GetSpeculativeSymbolInfo(position, syntax, SpeculativeBindingOption.BindAsExpression);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
            Assert.Equal(2, info.CandidateSymbols.Length);
        }

        [WorkItem(546046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546046")]
        [Fact]
        public void UnambiguousMethodGroupWithoutBoundParent1()
        {
            var text = @"
using System;

class C
{
    static void M()
    {
        /*<bind>*/M/*</bind>*/
";

            var compilation = (Compilation)CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.IdentifierName, syntax.Kind());

            var info = model.GetSymbolInfo(syntax);
            Assert.Equal(compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("C").GetMember<IMethodSymbol>("M"), info.CandidateSymbols.Single());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
        }

        [WorkItem(546046, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546046")]
        [Fact]
        public void UnambiguousMethodGroupWithoutBoundParent2()
        {
            var text = @"
using System;

class C
{
    static void M()
    {
        /*<bind>*/M/*</bind>*/[]
";

            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.IdentifierName, syntax.Kind());

            var info = model.GetSymbolInfo(syntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.NotATypeOrNamespace, info.CandidateReason);
            Assert.Equal(1, info.CandidateSymbols.Length);
        }

        [WorkItem(544651, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544651")]
        [ClrOnlyFact]
        public void SpeculativelyBindPropertyGroup1()
        {
            var source1 =
@"Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P(o As Object) As Object
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);

            var source2 = @"
using System;

class C
{
    static void M(IA a)
    {
        int here;
    }
}
";

            var compilation = (Compilation)CreateCompilation(source2, new[] { reference1 }, assemblyName: "SpeculativelyBindPropertyGroup");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var position = source2.IndexOf("here", StringComparison.Ordinal);
            var syntax = SyntaxFactory.ParseExpression("       a.P"); //Leading trivia was significant for triggering an assert before the fix.
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, syntax.Kind());

            var info = model.GetSpeculativeSymbolInfo(position, syntax, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(compilation.GlobalNamespace.GetMember<INamedTypeSymbol>("IA").GetMember<IPropertySymbol>("P"), info.Symbol);
        }

        [WorkItem(544651, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544651")]
        [ClrOnlyFact]
        public void SpeculativelyBindPropertyGroup2()
        {
            var source1 =
@"Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P(o As Object) As Object
    Property P(x As Object, y As Object) As Object
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);

            var source2 = @"
using System;

class C
{
    static void M(IA a)
    {
        int here;
    }
}
";

            var compilation = CreateCompilation(source2, new[] { reference1 }, assemblyName: "SpeculativelyBindPropertyGroup");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var position = source2.IndexOf("here", StringComparison.Ordinal);
            var syntax = SyntaxFactory.ParseExpression("       a.P"); //Leading trivia was significant for triggering an assert before the fix.
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, syntax.Kind());

            var info = model.GetSpeculativeSymbolInfo(position, syntax, SpeculativeBindingOption.BindAsExpression);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
            Assert.Equal(2, info.CandidateSymbols.Length);
        }

        // There is no test analogous to UnambiguousMethodGroupWithoutBoundParent1 because
        // a.P does not yield a bound property group - it yields a bound indexer access
        // (i.e. it doesn't actually hit the code path that formerly contained the assert).
        //[WorkItem(15177)]
        //[Fact]
        //public void UnambiguousPropertyGroupWithoutBoundParent1()

        [WorkItem(546117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546117")]
        [ClrOnlyFact]
        public void UnambiguousPropertyGroupWithoutBoundParent2()
        {
            var source1 =
@"Imports System.Runtime.InteropServices
<Assembly: PrimaryInteropAssembly(0, 0)>
<Assembly: Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E210"")>
<ComImport()>
<Guid(""165F752D-E9C4-4F7E-B0D0-CDFD7A36E211"")>
Public Interface IA
    Property P(o As Object) As Object
End Interface";
            var reference1 = BasicCompilationUtils.CompileToMetadata(source1);

            var source2 = @"
using System;

class C
{
    static void M(IA a)
    {
        /*<bind>*/a.P/*</bind>*/[]
";

            var compilation = CreateCompilation(source2, new[] { reference1 }, assemblyName: "SpeculativelyBindPropertyGroup");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.QualifiedName, syntax.Kind());

            var info = model.GetSymbolInfo(syntax);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.NotATypeOrNamespace, info.CandidateReason);
            Assert.Equal(1, info.CandidateSymbols.Length);
        }

        [WorkItem(544648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544648")]
        [Fact]
        public void SpeculativelyBindExtensionMethod()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Reflection;
static class Program
{
    static void Main()
    {
        FieldInfo[] fields = typeof(Exception).GetFields();
        Console.WriteLine(/*<bind>*/fields.Any((Func<FieldInfo, bool>)(field => field.IsStatic))/*</bind>*/);
    }
    static bool Any<T>(this IEnumerable<T> s, Func<T, bool> predicate)
    { 
        return false; 
    }
    static bool Any<T>(this ICollection<T> s, Func<T, bool> predicate)
    {
        return true;
    }
}
";
            var comp = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var originalSyntax = GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal(SyntaxKind.InvocationExpression, originalSyntax.Kind());

            var info1 = model.GetSymbolInfo(originalSyntax);
            var method1 = info1.Symbol as IMethodSymbol;
            Assert.NotNull(method1);

            Assert.Equal("System.Boolean System.Collections.Generic.ICollection<System.Reflection.FieldInfo>.Any<System.Reflection.FieldInfo>(System.Func<System.Reflection.FieldInfo, System.Boolean> predicate)", method1.ToTestDisplayString());
            Assert.Same(method1.ReducedFrom.TypeParameters[0], method1.TypeParameters[0].ReducedFrom);
            Assert.Null(method1.ReducedFrom.TypeParameters[0].ReducedFrom);

            Assert.Equal("System.Boolean Program.Any<T>(this System.Collections.Generic.ICollection<T> s, System.Func<T, System.Boolean> predicate)", method1.ReducedFrom.ToTestDisplayString());
            Assert.Equal("System.Collections.Generic.ICollection<System.Reflection.FieldInfo>", method1.ReceiverType.ToTestDisplayString());
            Assert.Equal("System.Reflection.FieldInfo", method1.GetTypeInferredDuringReduction(method1.ReducedFrom.TypeParameters[0]).ToTestDisplayString());

            Assert.Throws<InvalidOperationException>(() => method1.ReducedFrom.GetTypeInferredDuringReduction(null));
            Assert.Throws<ArgumentNullException>(() => method1.GetTypeInferredDuringReduction(null));
            Assert.Throws<ArgumentException>(() => method1.GetTypeInferredDuringReduction(
                                                    comp.Assembly.GlobalNamespace.GetMember<INamedTypeSymbol>("Program").GetMembers("Any").
                                                        Where((m) => (object)m != (object)method1.ReducedFrom).Cast<IMethodSymbol>().Single().TypeParameters[0]));

            Assert.Equal("Any", method1.Name);
            var reducedFrom1 = method1.GetSymbol().CallsiteReducedFromMethod;
            Assert.NotNull(reducedFrom1);
            Assert.Equal("System.Boolean Program.Any<System.Reflection.FieldInfo>(this System.Collections.Generic.ICollection<System.Reflection.FieldInfo> s, System.Func<System.Reflection.FieldInfo, System.Boolean> predicate)", reducedFrom1.ToTestDisplayString());
            Assert.Equal("Program", reducedFrom1.ReceiverType.ToTestDisplayString());
            Assert.Equal(SpecialType.System_Collections_Generic_ICollection_T, ((TypeSymbol)reducedFrom1.Parameters[0].Type.OriginalDefinition).SpecialType);

            var speculativeSyntax = SyntaxFactory.ParseExpression("fields.Any((field => field.IsStatic))"); //cast removed
            Assert.Equal(SyntaxKind.InvocationExpression, speculativeSyntax.Kind());

            var info2 = model.GetSpeculativeSymbolInfo(originalSyntax.SpanStart, speculativeSyntax, SpeculativeBindingOption.BindAsExpression);
            var method2 = info2.Symbol as IMethodSymbol;
            Assert.NotNull(method2);
            Assert.Equal("Any", method2.Name);
            var reducedFrom2 = method2.GetSymbol().CallsiteReducedFromMethod;
            Assert.NotNull(reducedFrom2);
            Assert.Equal(SpecialType.System_Collections_Generic_ICollection_T, ((TypeSymbol)reducedFrom2.Parameters[0].Type.OriginalDefinition).SpecialType);

            Assert.Equal(reducedFrom1, reducedFrom2);
            Assert.Equal(method1, method2);
        }

        /// <summary>
        /// This test reproduces the issue we were seeing in DevDiv #13366: LocalSymbol.SetType was asserting
        /// because it was set to IEnumerable&lt;int&gt; before binding the declaration of x but to an error
        /// type after binding the declaration of x.
        /// </summary>
        [WorkItem(545097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545097")]
        [Fact]
        public void NameConflictDuringLambdaBinding1()
        {
            var source = @"
using System.Linq;
 
class C
{
    static void Main()
    {
        var x = 0;
        var q = from e in """" let x = 2 select x;
    }
}
";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);

            var tree = comp.SyntaxTrees.Single();

            var localDecls = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclarationSyntax>();

            var localDecl1 = localDecls.First();
            Assert.Equal("x", localDecl1.Variables.Single().Identifier.ValueText);
            var localDecl2 = localDecls.Last();
            Assert.Equal("q", localDecl2.Variables.Single().Identifier.ValueText);

            var model = comp.GetSemanticModel(tree);

            var info0 = model.GetSymbolInfo(localDecl2.Type);
            Assert.Equal(SpecialType.System_Collections_Generic_IEnumerable_T, ((ITypeSymbol)info0.Symbol.OriginalDefinition).SpecialType);
            Assert.Equal(SpecialType.System_Int32, ((INamedTypeSymbol)info0.Symbol).TypeArguments.Single().SpecialType);

            var info1 = model.GetSymbolInfo(localDecl1.Type);
            Assert.Equal(SpecialType.System_Int32, ((ITypeSymbol)info1.Symbol).SpecialType);

            // This used to assert because the second binding would see the declaration of x and report CS7040, disrupting the delegate conversion.
            var info2 = model.GetSymbolInfo(localDecl2.Type);
            Assert.Equal(SpecialType.System_Collections_Generic_IEnumerable_T, ((ITypeSymbol)info2.Symbol.OriginalDefinition).SpecialType);
            Assert.Equal(SpecialType.System_Int32, ((INamedTypeSymbol)info2.Symbol).TypeArguments.Single().SpecialType);

            comp.VerifyDiagnostics(
    // (9,34): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
    //         var q = from e in "" let x = 2 select x;
    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x").WithLocation(9, 34),
    // (8,13): warning CS0219: The variable 'x' is assigned but its value is never used
    //         var x = 0;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(8, 13)
);
        }

        /// <summary>
        /// This test reverses the order of statement binding from NameConflictDuringLambdaBinding2 to confirm that
        /// the results are the same.
        /// </summary>
        [WorkItem(545097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545097")]
        [Fact]
        public void NameConflictDuringLambdaBinding2()
        {
            var source = @"
using System.Linq;
 
class C
{
    static void Main()
    {
        var x = 0;
        var q = from e in """" let x = 2 select x;
    }
}
";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);

            var tree = comp.SyntaxTrees.Single();

            var localDecls = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclarationSyntax>();

            var localDecl1 = localDecls.First();
            Assert.Equal("x", localDecl1.Variables.Single().Identifier.ValueText);
            var localDecl2 = localDecls.Last();
            Assert.Equal("q", localDecl2.Variables.Single().Identifier.ValueText);

            var model = comp.GetSemanticModel(tree);

            var info1 = model.GetSymbolInfo(localDecl1.Type);
            Assert.Equal(SpecialType.System_Int32, ((ITypeSymbol)info1.Symbol).SpecialType);

            // This used to assert because the second binding would see the declaration of x and report CS7040, disrupting the delegate conversion.
            var info2 = model.GetSymbolInfo(localDecl2.Type);
            Assert.Equal(SpecialType.System_Collections_Generic_IEnumerable_T, ((ITypeSymbol)info2.Symbol.OriginalDefinition).SpecialType);
            Assert.Equal(SpecialType.System_Int32, ((INamedTypeSymbol)info2.Symbol).TypeArguments.Single().SpecialType);

            comp.VerifyDiagnostics(
    // (9,34): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
    //         var q = from e in "" let x = 2 select x;
    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x").WithLocation(9, 34),
    // (8,13): warning CS0219: The variable 'x' is assigned but its value is never used
    //         var x = 0;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(8, 13)
    );
        }

        [WorkItem(546263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546263")]
        [Fact]
        public void SpeculativeSymbolInfoForOmittedTypeArgumentSyntaxNode()
        {
            var text =
@"namespace N2
{
	using N1;
	class Test
	{
		class N1<G1> {}
		static void Main() 
		{
			int res = 0;
			N1<int> n1 = new N1<int>();
			global::N1 < > .C1 c1 = null;
		}
	}
}";
            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var position = text.IndexOf("< >", StringComparison.Ordinal);
            var syntax = tree.GetCompilationUnitRoot().FindToken(position).Parent.DescendantNodesAndSelf().OfType<OmittedTypeArgumentSyntax>().Single();

            var info = model.GetSpeculativeSymbolInfo(syntax.SpanStart, syntax, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Null(info.Symbol);
        }

        [WorkItem(530313, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530313")]
        [Fact]
        public void SpeculativeTypeInfoForOmittedTypeArgumentSyntaxNode()
        {
            var text =
@"namespace N2
{
	using N1;
	class Test
	{
		class N1<G1> {}
		static void Main() 
		{
			int res = 0;
			N1<int> n1 = new N1<int>();
			global::N1 < > .C1 c1 = null;
		}
	}
}";
            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var position = text.IndexOf("< >", StringComparison.Ordinal);
            var syntax = tree.GetCompilationUnitRoot().FindToken(position).Parent.DescendantNodesAndSelf().OfType<OmittedTypeArgumentSyntax>().Single();

            var info = model.GetSpeculativeTypeInfo(syntax.SpanStart, syntax, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal(TypeInfo.None, info);
        }

        [WorkItem(546266, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546266")]
        [Fact]
        public void SpeculativeTypeInfoForGenericNameSyntaxWithinTypeOfInsideAnonMethod()
        {
            var text = @"
delegate void Del();

class C
{
    public void M1()
    {
        Del d = delegate ()
        {
            var v1 = typeof(S<,,,>);
        };
    }
}
";

            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var position = text.IndexOf("S<,,,>", StringComparison.Ordinal);
            var syntax = tree.GetCompilationUnitRoot().FindToken(position).Parent.DescendantNodesAndSelf().OfType<GenericNameSyntax>().Single();

            var info = model.GetSpeculativeTypeInfo(syntax.SpanStart, syntax, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.NotNull(info.Type);
        }

        [WorkItem(547160, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547160")]
        [Fact, WorkItem(531496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531496")]
        public void SemanticInfoForOmittedTypeArgumentInIncompleteMember()
        {
            var text = @"
class Test
{
    C<>
";

            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<OmittedTypeArgumentSyntax>().Single();

            var info = model.GetSemanticInfoSummary(syntax);
            Assert.Null(info.Alias);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
            Assert.True(info.CandidateSymbols.IsEmpty);
            Assert.False(info.ConstantValue.HasValue);
            Assert.Null(info.ConvertedType);
            Assert.Equal(Conversion.Identity, info.ImplicitConversion);
            Assert.False(info.IsCompileTimeConstant);
            Assert.True(info.MemberGroup.IsEmpty);
            Assert.True(info.MethodGroup.IsEmpty);
            Assert.Null(info.Symbol);
            Assert.Null(info.Type);
        }

        [WorkItem(547160, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547160")]
        [Fact, WorkItem(531496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531496")]
        public void CollectionInitializerSpeculativeInfo()
        {
            var text = @"
class Test
{
}
";

            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var speculativeSyntax = SyntaxFactory.ParseExpression("new List { 1, 2 }");
            var initializerSyntax = speculativeSyntax.DescendantNodesAndSelf().OfType<InitializerExpressionSyntax>().Single();

            var symbolInfo = model.GetSpeculativeSymbolInfo(0, initializerSyntax, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(SymbolInfo.None, symbolInfo);

            var typeInfo = model.GetSpeculativeTypeInfo(0, initializerSyntax, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(TypeInfo.None, typeInfo);
        }

        [WorkItem(531362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531362")]
        [Fact]
        public void DelegateElementAccess()
        {
            var text = @"
class C
{
    void M(bool b)
    {
        System.Action o = delegate { if (b) { } } [1];
    }
}
";

            var compilation = CreateCompilation(text);
            compilation.VerifyDiagnostics(
                // (6,27): error CS0021: Cannot apply indexing with [] to an expression of type 'anonymous method'
                //         System.Action o = delegate { if (b) { } } [1];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "delegate { if (b) { } } [1]").WithArguments("anonymous method"));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Last(id => id.Identifier.ValueText == "b");
            var info = model.GetSymbolInfo(syntax);
        }

        [Fact]
        public void EnumBitwiseComplement()
        {
            var text = @"
using System;
enum Color { Red, Green, Blue }
class C
{
    static void Main()
    {
        Func<Color, Color> f2 = x => /*<bind>*/~x/*</bind>*/;
    }
}";
            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = GetExprSyntaxForBinding(GetExprSyntaxList(tree));

            var info = model.GetTypeInfo(syntax);
            var conv = model.GetConversion(syntax);

            Assert.Equal(TypeKind.Enum, info.Type.TypeKind);
            Assert.Equal(ConversionKind.Identity, conv.Kind);
        }

        [WorkItem(531534, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531534")]
        [Fact]
        public void LambdaOutsideMemberModel()
        {
            var text = @"
int P
{
    badAccessorName
    {
        M(env => env);
";
            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().Last();

            var symbol = model.GetDeclaredSymbol(syntax); // Doesn't assert.
            Assert.Null(symbol);
        }

        [WorkItem(633340, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633340")]
        [Fact]
        public void MemberOfInaccessibleType()
        {
            var text = @"
class A
{
    private class Nested
    {
        public class Another
        {
        }
    }
}
 
public class B : A
{
    public Nested.Another a;
}
";
            var compilation = (Compilation)CreateCompilation(text);

            var global = compilation.GlobalNamespace;
            var classA = global.GetMember<INamedTypeSymbol>("A");
            var classNested = classA.GetMember<INamedTypeSymbol>("Nested");
            var classAnother = classNested.GetMember<INamedTypeSymbol>("Another");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var fieldSyntax = tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Single();

            var qualifiedSyntax = (QualifiedNameSyntax)fieldSyntax.Declaration.Type;
            var leftSyntax = qualifiedSyntax.Left;
            var rightSyntax = qualifiedSyntax.Right;

            var leftInfo = model.GetSymbolInfo(leftSyntax);
            Assert.Equal(CandidateReason.Inaccessible, leftInfo.CandidateReason);
            Assert.Equal(classNested, leftInfo.CandidateSymbols.Single());

            var rightInfo = model.GetSymbolInfo(rightSyntax);
            Assert.Equal(CandidateReason.Inaccessible, rightInfo.CandidateReason);
            Assert.Equal(classAnother, rightInfo.CandidateSymbols.Single());

            compilation.VerifyDiagnostics(
                // (12,14): error CS0060: Inconsistent accessibility: base type 'A' is less accessible than class 'B'
                // public class B : A
                Diagnostic(ErrorCode.ERR_BadVisBaseClass, "B").WithArguments("B", "A"),
                // (14,12): error CS0122: 'A.Nested' is inaccessible due to its protection level
                //     public Nested.Another a;
                Diagnostic(ErrorCode.ERR_BadAccess, "Nested").WithArguments("A.Nested"));
        }

        [WorkItem(633340, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633340")]
        [Fact]
        public void NotReferencableMemberOfInaccessibleType()
        {
            var text = @"
class A
{
    private class Nested
    {
        public int P { get; set; }
    }
}
 
class B : A
{
    int Test(Nested nested)
    {
        return nested.get_P();
    }
}
";
            var compilation = (Compilation)CreateCompilation(text);

            var global = compilation.GlobalNamespace;
            var classA = global.GetMember<INamedTypeSymbol>("A");
            var classNested = classA.GetMember<INamedTypeSymbol>("Nested");
            var propertyP = classNested.GetMember<IPropertySymbol>("P");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var memberAccessSyntax = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();

            var info = model.GetSymbolInfo(memberAccessSyntax);
            Assert.Equal(CandidateReason.NotReferencable, info.CandidateReason);
            Assert.Equal(propertyP.GetMethod, info.CandidateSymbols.Single());

            compilation.VerifyDiagnostics(
    // (12,14): error CS0122: 'A.Nested' is inaccessible due to its protection level
    //     int Test(Nested nested)
    Diagnostic(ErrorCode.ERR_BadAccess, "Nested").WithArguments("A.Nested"),
    // (14,23): error CS0571: 'A.Nested.P.get': cannot explicitly call operator or accessor
    //         return nested.get_P();
    Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "get_P").WithArguments("A.Nested.P.get")
);
        }

        [WorkItem(633340, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633340")]
        [Fact]
        public void AccessibleMemberOfInaccessibleType()
        {
            var text = @"
public class A
{
    private class Nested
    {
    }
}
 
public class B : A
{
    void Test()
    {
        Nested.ReferenceEquals(null, null); // Actually object.ReferenceEquals.
    }
}
";
            var compilation = (Compilation)CreateCompilation(text);

            var global = compilation.GlobalNamespace;
            var classA = global.GetMember<INamedTypeSymbol>("A");
            var classNested = classA.GetMember<INamedTypeSymbol>("Nested");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var callSyntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            var methodAccessSyntax = (MemberAccessExpressionSyntax)callSyntax.Expression;
            var nestedTypeAccessSyntax = methodAccessSyntax.Expression;

            var typeInfo = model.GetSymbolInfo(nestedTypeAccessSyntax);
            Assert.Equal(CandidateReason.Inaccessible, typeInfo.CandidateReason);
            Assert.Equal(classNested, typeInfo.CandidateSymbols.Single());

            var methodInfo = model.GetSymbolInfo(callSyntax);
            Assert.Equal(compilation.GetSpecialTypeMember(SpecialMember.System_Object__ReferenceEquals), methodInfo.Symbol);

            compilation.VerifyDiagnostics(
                // (13,9): error CS0122: 'A.Nested' is inaccessible due to its protection level
                //         Nested.ReferenceEquals(null, null); // Actually object.ReferenceEquals.
                Diagnostic(ErrorCode.ERR_BadAccess, "Nested").WithArguments("A.Nested"));
        }

        [WorkItem(530252, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530252")]
        [Fact]
        public void MethodGroupHiddenSymbols1()
        {
            var text = @"
class C
{
    public override int GetHashCode() { return 0; }
}

struct S
{
    public override int GetHashCode() { return 0; }
}

class Test
{
    int M(C c)
    {
        return c.GetHashCode
    }

    int M(S s)
    {
        return s.GetHashCode
    }
}
";
            var compilation = CreateCompilation(text);

            var global = compilation.GlobalNamespace;
            var classType = global.GetMember<NamedTypeSymbol>("C");
            var structType = global.GetMember<NamedTypeSymbol>("S");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var memberAccesses = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToArray();
            Assert.Equal(2, memberAccesses.Length);

            var classMemberAccess = memberAccesses[0];
            var structMemberAccess = memberAccesses[1];

            var classInfo = model.GetSymbolInfo(classMemberAccess);
            var structInfo = model.GetSymbolInfo(structMemberAccess);

            // Only one candidate.
            Assert.Equal(CandidateReason.OverloadResolutionFailure, classInfo.CandidateReason);
            Assert.Equal("System.Int32 C.GetHashCode()", classInfo.CandidateSymbols.Single().ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, structInfo.CandidateReason);
            Assert.Equal("System.Int32 S.GetHashCode()", structInfo.CandidateSymbols.Single().ToTestDisplayString());
        }

        [WorkItem(530252, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530252")]
        [Fact]
        public void MethodGroupHiddenSymbols2()
        {
            var text = @"
class A
{
    public virtual void M() { }
}
 
class B : A
{
    public override void M() { }
}
 
class C : B
{
    public override void M() { }
}
 
class Program
{
    static void Main(string[] args)
    {
        C c = new C();
        c.M
    }
}
";
            var compilation = CreateCompilation(text);

            var classC = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var memberAccess = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();

            var info = model.GetSymbolInfo(memberAccess);

            // Only one candidate.
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
            Assert.Equal("void C.M()", info.CandidateSymbols.Single().ToTestDisplayString());
        }

        [Fact]
        [WorkItem(645512, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645512")]
        public void LookupProtectedMemberOnConstrainedTypeParameter()
        {
            var source = @"
class A
{
    protected void Goo() { }
}

class C : A
{
    public void Bar<T>(T t, C c) where T : C
    {
        t.Goo();
        c.Goo();
    }
}
";

            var comp = (Compilation)CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var classA = global.GetMember<INamedTypeSymbol>("A");
            var methodGoo = classA.GetMember<IMethodSymbol>("Goo");
            var classC = global.GetMember<INamedTypeSymbol>("C");
            var methodBar = classC.GetMember<IMethodSymbol>("Bar");

            var paramType0 = methodBar.GetParameterType(0);
            Assert.Equal(TypeKind.TypeParameter, paramType0.TypeKind);
            var paramType1 = methodBar.GetParameterType(1);
            Assert.Equal(TypeKind.Class, paramType1.TypeKind);

            int position = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First().SpanStart;

            Assert.Contains("Goo", model.LookupNames(position, paramType0));
            Assert.Contains("Goo", model.LookupNames(position, paramType1));
        }

        [Fact]
        [WorkItem(645512, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/645512")]
        public void LookupProtectedMemberOnConstrainedTypeParameter2()
        {
            var source = @"
class A
{
    protected void Goo() { }
}

class C : A
{
    public void Bar<T, U>(T t, C c) 
        where T : U
        where U : C
    {
        t.Goo();
        c.Goo();
    }
}
";

            var comp = (Compilation)CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;
            var classA = global.GetMember<INamedTypeSymbol>("A");
            var methodGoo = classA.GetMember<IMethodSymbol>("Goo");
            var classC = global.GetMember<INamedTypeSymbol>("C");
            var methodBar = classC.GetMember<IMethodSymbol>("Bar");

            var paramType0 = methodBar.GetParameterType(0);
            Assert.Equal(TypeKind.TypeParameter, paramType0.TypeKind);
            var paramType1 = methodBar.GetParameterType(1);
            Assert.Equal(TypeKind.Class, paramType1.TypeKind);

            int position = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First().SpanStart;

            Assert.Contains("Goo", model.LookupNames(position, paramType0));
            Assert.Contains("Goo", model.LookupNames(position, paramType1));
        }

        [Fact]
        [WorkItem(652583, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/652583")]
        public void ParameterDefaultValueWithoutParameter()
        {
            var source = @"
class A
{
    protected void Goo(bool b, = true
";

            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var trueLiteral = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single();
            Assert.Equal(SyntaxKind.TrueLiteralExpression, trueLiteral.Kind());

            model.GetSymbolInfo(trueLiteral);

            var parameterSyntax = trueLiteral.FirstAncestorOrSelf<ParameterSyntax>();
            Assert.Equal(SyntaxKind.Parameter, parameterSyntax.Kind());

            model.GetDeclaredSymbol(parameterSyntax);
        }

        [Fact]
        [WorkItem(530791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530791")]
        public void Repro530791()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        Test test = new Test(() => { return null; });
    }
}

class Test
{
}
";

            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var lambdaSyntax = tree.GetRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

            var symbolInfo = model.GetSymbolInfo(lambdaSyntax);
            var lambda = (IMethodSymbol)symbolInfo.Symbol;
            Assert.False(lambda.ReturnsVoid);
            Assert.Equal(SymbolKind.ErrorType, lambda.ReturnType.Kind);
        }

        [Fact]
        public void InvocationInLocalDeclarationInLambdaInConstructorInitializer()
        {
            var source = @"
using System;

public class C 
{ 
    public int M() 
    { 
        return null; 
    } 
}

public class Test
{
    public Test()
        : this(c =>
        {
            int i = c.M();
            return i;
        })
    { 
    }

    public Test(Func<C, int> f)
    {
    }
}
";

            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var syntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            var symbolInfo = model.GetSymbolInfo(syntax);
            var methodSymbol = (IMethodSymbol)symbolInfo.Symbol;
            Assert.False(methodSymbol.ReturnsVoid);
        }

        [Fact]
        [WorkItem(654753, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/654753")]
        public void Repro654753()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;

public class C
{
    private readonly C Instance = new C();

    bool M(IDisposable d)
    {
        using(d)
        {
            bool any = this.Instance.GetList().OfType<D>().Any();
            return any;
        }
    }

    IEnumerable<C> GetList()
    {
        return null;
    }
}

public class D : C
{
}
";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var position = source.IndexOf("this", StringComparison.Ordinal);
            var statement = tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
            var newSyntax = SyntaxFactory.ParseExpression("Instance.GetList().OfType<D>().Any()");
            var newStatement = statement.ReplaceNode(statement.Declaration.Variables[0].Initializer.Value, newSyntax);
            newSyntax = newStatement.Declaration.Variables[0].Initializer.Value;
            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(position, newStatement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var newSyntaxMemberAccess = newSyntax.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().
                Single(e => e.ToString() == "Instance.GetList().OfType<D>");
            speculativeModel.GetTypeInfo(newSyntaxMemberAccess);
        }

        [Fact]
        [WorkItem(750557, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/750557")]
        public void MethodGroupFromMetadata()
        {
            var source = @"
class Goo
{
delegate int D(int i);
void M()
{
  var v = ((D)(x => x)).Equals is bool;
}
}

";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var position = source.IndexOf("Equals", StringComparison.Ordinal);
            var equalsToken = tree.GetRoot().FindToken(position);
            var equalsNode = equalsToken.Parent;
            var symbolInfo = model.GetSymbolInfo(equalsNode);
            //note that we don't guarantee what symbol will come back on a method group in an is expression.
            Assert.Null(symbolInfo.Symbol);
            Assert.True(symbolInfo.CandidateSymbols.Length > 0);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(531304, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531304")]
        public void GetPreprocessingSymbolInfoForDefinedSymbol()
        {
            string sourceCode = @"
#define X

#if X //bind
#define Z
#endif

#if Z //bind
#endif

// broken code cases
#define A
#if A + 1 //bind
#endif

#define B = 0
#if B //bind
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "X //bind");
            Assert.Equal("X", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");

            symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");

            symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "A + 1 //bind");
            Assert.Equal("A", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");

            symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "B //bind");
            Assert.Equal("B", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");

            Assert.True(symbolInfo.Symbol.Equals(symbolInfo.Symbol));
            Assert.False(symbolInfo.Symbol.Equals(null));
            PreprocessingSymbolInfo symbolInfo2 = GetPreprocessingSymbolInfoForTest(sourceCode, "B //bind");
            Assert.NotSame(symbolInfo.Symbol, symbolInfo2.Symbol);
            Assert.Equal(symbolInfo.Symbol, symbolInfo2.Symbol);
            Assert.Equal(symbolInfo.Symbol.GetHashCode(), symbolInfo2.Symbol.GetHashCode());
        }

        [Fact, WorkItem(531304, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531304")]
        public void GetPreprocessingSymbolInfoForUndefinedSymbol()
        {
            string sourceCode = @"
#define X
#undef X

#if X //bind
#endif

#if x //bind
#endif

#if Y //bind
#define Z
#endif

#if Z //bind
#endif

// Not in preprocessor trivia
#define A

public class T
{
    public int Goo(int A)
    {
        return A; //bind
    }
}
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "X //bind");
            Assert.Equal("X", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");

            symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "x //bind");
            Assert.Equal("x", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");

            symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Y //bind");
            Assert.Equal("Y", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");

            symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");

            symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "A; //bind");
            Assert.Null(symbolInfo.Symbol);
        }

        [Fact, WorkItem(531304, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531304"), WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void GetPreprocessingSymbolInfoForSymbolDefinedLaterInSource()
        {
            string sourceCode = @"
#if Z //bind
#endif

#define Z
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_01()
        {
            string sourceCode = @"
#define Z

#if Z //bind
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_02()
        {
            string sourceCode = @"
#if true
    #define Z

    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_03()
        {
            string sourceCode = @"
#if false
    #define Z

    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_04()
        {
            string sourceCode = @"
#if true
    #define Z
#endif

#if Z //bind
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_05()
        {
            string sourceCode = @"
#if false
    #define Z
#endif

#if Z //bind
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_06()
        {
            string sourceCode = @"
#if true
#else
    #define Z

    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_07()
        {
            string sourceCode = @"
#if false
#else
    #define Z

    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_08()
        {
            string sourceCode = @"
#if true
    #define Z
#else
    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_09()
        {
            string sourceCode = @"
#if false
    #define Z
#else
    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_10()
        {
            string sourceCode = @"
#if true
#elif true 
    #define Z

    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_11()
        {
            string sourceCode = @"
#if false
#elif true
    #define Z

    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_12()
        {
            string sourceCode = @"
#if false
#elif false
    #define Z

    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_13()
        {
            string sourceCode = @"
#if true
    #define Z
#elif false
    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_14()
        {
            string sourceCode = @"
#if true
    #define Z
#elif true
    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_15()
        {
            string sourceCode = @"
#if false
    #define Z
#elif true
    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_16()
        {
            string sourceCode = @"
#if false
    #define Z
#elif false
    #if Z //bind
    #endif
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_17()
        {
            string sourceCode = @"
#if false
#else
    #define Z
#endif

#if Z //bind
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_18()
        {
            string sourceCode = @"
#if false
#elif true
    #define Z
#endif

#if Z //bind
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.True(symbolInfo.IsDefined, "must be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_19()
        {
            string sourceCode = @"
#if true
#else
    #define Z
#endif

#if Z //bind
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_20()
        {
            string sourceCode = @"
#if true
#elif true
    #define Z
#endif

#if Z //bind
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_21()
        {
            string sourceCode = @"
#if true
#elif false
    #define Z
#endif

#if Z //bind
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");
        }

        [Fact, WorkItem(720566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720566")]
        public void Bug720566_22()
        {
            string sourceCode = @"
#if false
#elif false
    #define Z
#endif

#if Z //bind
#endif
";
            PreprocessingSymbolInfo symbolInfo = GetPreprocessingSymbolInfoForTest(sourceCode, "Z //bind");
            Assert.Equal("Z", symbolInfo.Symbol.Name);
            Assert.False(symbolInfo.IsDefined, "must not be defined");
        }

        [WorkItem(835391, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/835391")]
        [Fact]
        public void ConstructedErrorTypeValidation()
        {
            var text =
@"class C1 : E1 { }
class C2<T> : E2<T> { }";
            var compilation = (Compilation)CreateCompilation(text);
            var objectType = compilation.GetSpecialType(SpecialType.System_Object);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();

            // Non-generic type.
            var type = (INamedTypeSymbol)model.GetDeclaredSymbol(root.Members[0]);
            Assert.False(type.IsGenericType);
            Assert.False(type.IsErrorType());
            Assert.Throws<InvalidOperationException>(() => type.Construct(objectType)); // non-generic type

            // Non-generic error type.
            type = type.BaseType;
            Assert.False(type.IsGenericType);
            Assert.True(type.IsErrorType());
            Assert.Throws<InvalidOperationException>(() => type.Construct(objectType)); // non-generic type

            // Generic type.
            type = (INamedTypeSymbol)model.GetDeclaredSymbol(root.Members[1]);
            Assert.True(type.IsGenericType);
            Assert.False(type.IsErrorType());
            Assert.Throws<ArgumentException>(() => type.Construct(new ITypeSymbol[] { null })); // null type arg
            Assert.Throws<ArgumentException>(() => type.Construct()); // typeArgs.Length != Arity
            Assert.Throws<InvalidOperationException>(() => type.Construct(objectType).Construct(objectType)); // constructed type

            // Generic error type.
            type = type.BaseType.ConstructedFrom;
            Assert.True(type.IsGenericType);
            Assert.True(type.IsErrorType());
            Assert.Throws<ArgumentException>(() => type.Construct(new ITypeSymbol[] { null })); // null type arg
            Assert.Throws<ArgumentException>(() => type.Construct()); // typeArgs.Length != Arity
            Assert.Throws<InvalidOperationException>(() => type.Construct(objectType).Construct(objectType)); // constructed type
        }

        [Fact]
        [WorkItem(849371, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849371")]
        public void NestedLambdaErrorRecovery()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        Task<IEnumerable<Task<A>>> teta = null;
        teta.ContinueWith(tasks =>
        {
            var list = tasks.Result.Select(t => X(t.Result)); // Wrong argument type for X.
            list.ToString();
        });
    }

    static B X(int x)
    {
        return null;
    }

    class A { }
    class B { }
}
";

            for (int i = 0; i < 10; i++) // Ten runs to ensure consistency.
            {
                var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
                comp.VerifyDiagnostics(
                    // (13,51): error CS1503: Argument 1: cannot convert from 'Program.A' to 'int'
                    //             var list = tasks.Result.Select(t => X(t.Result)); // Wrong argument type for X.
                    Diagnostic(ErrorCode.ERR_BadArgType, "t.Result").WithArguments("1", "Program.A", "int").WithLocation(13, 51),
                    // (13,30): error CS1061: 'System.Threading.Tasks.Task' does not contain a definition for 'Result' and no extension method 'Result' accepting a first argument of type 'System.Threading.Tasks.Task' could be found (are you missing a using directive or an assembly reference?)
                    //             var list = tasks.Result.Select(t => X(t.Result)); // Wrong argument type for X.
                    Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Result").WithArguments("System.Threading.Tasks.Task", "Result").WithLocation(13, 30));

                var tree = comp.SyntaxTrees.Single();
                var model = comp.GetSemanticModel(tree);

                var invocationSyntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
                var invocationInfo = model.GetSymbolInfo(invocationSyntax);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, invocationInfo.CandidateReason);
                Assert.Null(invocationInfo.Symbol);
                Assert.NotEqual(0, invocationInfo.CandidateSymbols.Length);

                var parameterSyntax = invocationSyntax.DescendantNodes().OfType<ParameterSyntax>().First();
                var parameterSymbol = model.GetDeclaredSymbol(parameterSyntax);
                Assert.Equal("System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task<Program.A>>>", parameterSymbol.Type.ToTestDisplayString());
            }
        }

        [WorkItem(849371, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849371")]
        [WorkItem(854543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854543")]
        [WorkItem(854548, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854548")]
        [Fact]
        public void SemanticModelLambdaErrorRecovery()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        M(() => 1); // Neither overload wins.
    }

    static void M(Func<string> a)
    {
    }

    static void M(Func<char> a)
    {
    }
}
";

            {
                var comp = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(source);
                var tree = comp.SyntaxTrees.Single();
                var model = comp.GetSemanticModel(tree);

                var lambdaSyntax = tree.GetRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

                var otherFuncType = comp.GetWellKnownType(WellKnownType.System_Func_T).Construct(comp.GetSpecialType(SpecialType.System_Int32));

                var typeInfo = model.GetTypeInfo(lambdaSyntax);
                Assert.Null(typeInfo.Type);
                Assert.NotEqual(otherFuncType, typeInfo.ConvertedType);
            }

            {
                var comp = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(source);
                var tree = comp.SyntaxTrees.Single();
                var model = comp.GetSemanticModel(tree);

                var lambdaSyntax = tree.GetRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

                var otherFuncType = comp.GetWellKnownType(WellKnownType.System_Func_T).Construct(comp.GetSpecialType(SpecialType.System_Int32));
                var conversion = model.ClassifyConversion(lambdaSyntax, otherFuncType);
                CheckIsAssignableTo(model, lambdaSyntax);

                var typeInfo = model.GetTypeInfo(lambdaSyntax);
                Assert.Null(typeInfo.Type);
                Assert.NotEqual(otherFuncType, typeInfo.ConvertedType); // Not affected by call to ClassifyConversion.
            }
        }

        [Fact]
        [WorkItem(854543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854543")]
        public void ClassifyConversionOnNull()
        {
            var source = @"
class Program
{
    static void Main()
    {
        M(null); // Ambiguous.
    }

    static void M(A a)
    {
    }

    static void M(B b)
    {
    }
}

class A { }
class B { }
class C { }
";

            var comp = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(A)' and 'Program.M(B)'
                //         M(null); // Ambiguous.
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(A)", "Program.M(B)").WithLocation(6, 9));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var nullSyntax = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().Single();

            var typeC = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("C");

            var conversion = model.ClassifyConversion(nullSyntax, typeC);
            CheckIsAssignableTo(model, nullSyntax);
            Assert.Equal(ConversionKind.ImplicitReference, conversion.Kind);
        }

        [WorkItem(854543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854543")]
        [Fact]
        public void ClassifyConversionOnLambda()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        M(() => null);
    }

    static void M(Func<A> a)
    {
    }
}

class A { }
class B { }
";

            var comp = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var lambdaSyntax = tree.GetRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

            var typeB = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("B");
            var typeFuncB = comp.GetWellKnownType(WellKnownType.System_Func_T).Construct(typeB);

            var conversion = model.ClassifyConversion(lambdaSyntax, typeFuncB);
            CheckIsAssignableTo(model, lambdaSyntax);
            Assert.Equal(ConversionKind.AnonymousFunction, conversion.Kind);
        }

        [WorkItem(854543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854543")]
        [Fact]
        public void ClassifyConversionOnAmbiguousLambda()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        M(() => null); // Ambiguous.
    }

    static void M(Func<A> a)
    {
    }

    static void M(Func<B> b)
    {
    }
}

class A { }
class B { }
class C { }
";

            var comp = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (8,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.M(System.Func<A>)' and 'Program.M(System.Func<B>)'
                //         M(() => null); // Ambiguous.
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Program.M(System.Func<A>)", "Program.M(System.Func<B>)").WithLocation(8, 9));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var lambdaSyntax = tree.GetRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

            var typeC = comp.GlobalNamespace.GetMember<INamedTypeSymbol>("C");
            var typeFuncC = comp.GetWellKnownType(WellKnownType.System_Func_T).Construct(typeC);

            var conversion = model.ClassifyConversion(lambdaSyntax, typeFuncC);
            CheckIsAssignableTo(model, lambdaSyntax);
            Assert.Equal(ConversionKind.AnonymousFunction, conversion.Kind);
        }

        [WorkItem(854543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/854543")]
        [Fact]
        public void ClassifyConversionOnAmbiguousMethodGroup()
        {
            var source = @"
using System;

class Base<T>
{
    public A N(T t) { throw null; }
    public B N(int t) { throw null; }
}

class Derived : Base<int>
{
    void Test()
    {
        M(N); // Ambiguous.
    }

    static void M(Func<int, A> a)
    {
    }

    static void M(Func<int, B> b)
    {
    }
}

class A { }
class B { }
class C { }
";

            var comp = (Compilation)CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (14,9): error CS0121: The call is ambiguous between the following methods or properties: 'Derived.M(System.Func<int, A>)' and 'Derived.M(System.Func<int, B>)'
                //         M(N); // Ambiguous.
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Derived.M(System.Func<int, A>)", "Derived.M(System.Func<int, B>)").WithLocation(14, 9));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var methodGroupSyntax = tree.GetRoot().DescendantNodes().OfType<ArgumentSyntax>().Single().Expression;

            var global = comp.GlobalNamespace;
            var typeA = global.GetMember<INamedTypeSymbol>("A");
            var typeB = global.GetMember<INamedTypeSymbol>("B");
            var typeC = global.GetMember<INamedTypeSymbol>("C");

            var typeInt = comp.GetSpecialType(SpecialType.System_Int32);
            var typeFunc = comp.GetWellKnownType(WellKnownType.System_Func_T2);
            var typeFuncA = typeFunc.Construct(typeInt, typeA);
            var typeFuncB = typeFunc.Construct(typeInt, typeB);
            var typeFuncC = typeFunc.Construct(typeInt, typeC);

            var conversionA = model.ClassifyConversion(methodGroupSyntax, typeFuncA);
            CheckIsAssignableTo(model, methodGroupSyntax);
            Assert.Equal(ConversionKind.MethodGroup, conversionA.Kind);

            var conversionB = model.ClassifyConversion(methodGroupSyntax, typeFuncB);
            Assert.Equal(ConversionKind.MethodGroup, conversionB.Kind);

            var conversionC = model.ClassifyConversion(methodGroupSyntax, typeFuncC);
            Assert.Equal(ConversionKind.NoConversion, conversionC.Kind);
        }

        [WorkItem(872064, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/872064")]
        [Fact]
        public void PartialMethodImplementationDiagnostics()
        {
            var file1 = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    partial class MyPartialClass
    {
        partial void MyPartialMethod(MyUndefinedMethod m);
    }
}
";

            var file2 = @"
namespace ConsoleApplication1
{
    partial class MyPartialClass
    {
        partial void MyPartialMethod(MyUndefinedMethod m)
        {
            c = new MyUndefinedMethod(23, true);
        }
    }
}
";

            var tree1 = Parse(file1);
            var tree2 = Parse(file2);
            var comp = CreateCompilation(new[] { tree1, tree2 });
            var model = comp.GetSemanticModel(tree2);

            var errs = model.GetDiagnostics();
            Assert.Equal(3, errs.Count());
            errs = model.GetSyntaxDiagnostics();
            Assert.Equal(0, errs.Count());
            errs = model.GetDeclarationDiagnostics();
            Assert.Equal(1, errs.Count());
            errs = model.GetMethodBodyDiagnostics();
            Assert.Equal(2, errs.Count());
        }

        [Fact]
        public void PartialTypeDiagnostics_StaticConstructors()
        {
            var file1 = @"
partial class C
{
    static C() {}
}
";

            var file2 = @"
partial class C
{
    static C() {}
}
";
            var file3 = @"
partial class C
{
    static C() {}
}
";

            var tree1 = Parse(file1);
            var tree2 = Parse(file2);
            var tree3 = Parse(file3);
            var comp = CreateCompilation(new[] { tree1, tree2, tree3 });
            var model1 = comp.GetSemanticModel(tree1);
            var model2 = comp.GetSemanticModel(tree2);
            var model3 = comp.GetSemanticModel(tree3);

            model1.GetDeclarationDiagnostics().Verify();

            model2.GetDeclarationDiagnostics().Verify(
                // (4,12): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(4, 12));

            model3.GetDeclarationDiagnostics().Verify(
                // (4,12): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(4, 12));

            Assert.Equal(3, comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").StaticConstructors.Length);
        }

        [Fact]
        public void PartialTypeDiagnostics_Constructors()
        {
            var file1 = @"
partial class C
{
    C() {}
}
";

            var file2 = @"
partial class C
{
    C() {}
}
";
            var file3 = @"
partial class C
{
    C() {}
}
";

            var tree1 = Parse(file1);
            var tree2 = Parse(file2);
            var tree3 = Parse(file3);
            var comp = CreateCompilation(new[] { tree1, tree2, tree3 });
            var model1 = comp.GetSemanticModel(tree1);
            var model2 = comp.GetSemanticModel(tree2);
            var model3 = comp.GetSemanticModel(tree3);

            model1.GetDeclarationDiagnostics().Verify();

            model2.GetDeclarationDiagnostics().Verify(
                // (4,5): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(4, 5));

            model3.GetDeclarationDiagnostics().Verify(
                // (4,5): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(4, 5));

            Assert.Equal(3, comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").InstanceConstructors.Length);
        }

        [WorkItem(1076661, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1076661")]
        [Fact]
        public void Bug1076661()
        {
            const string source = @"
using X = System.Collections.Generic.List<dynamic>;
class Test
{
    void Goo(ref X. x) { }
}";

            var comp = CreateCompilation(source);
            var diag = comp.GetDiagnostics();
        }

        [Fact]
        public void QueryClauseInBadStatement_Catch()
        {
            var source =
@"using System;
class C
{
    static void F(object[] c)
    {
        catch (Exception) when (from o in c where true)
        {
        }
    }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var tokens = tree.GetCompilationUnitRoot().DescendantTokens();
            var expr = tokens.Single(t => t.Kind() == SyntaxKind.TrueKeyword).Parent;
            Assert.Null(model.GetSymbolInfo(expr).Symbol);
            Assert.Equal(SpecialType.System_Boolean, model.GetTypeInfo(expr).Type.SpecialType);
        }

        [Fact]
        public void GetSpecialType_ThrowsOnLessThanZero()
        {
            var source = "class C1 { }";
            var comp = CreateCompilation(source);

            var specialType = (SpecialType)(-1);

            var exceptionThrown = false;

            try
            {
                comp.GetSpecialType(specialType);
            }
            catch (ArgumentOutOfRangeException e)
            {
                exceptionThrown = true;
                Assert.StartsWith(expectedStartString: $"Unexpected SpecialType: '{(int)specialType}'.", actualString: e.Message);
            }

            Assert.True(exceptionThrown, $"{nameof(comp.GetSpecialType)} did not throw when it should have.");
        }

        [Fact]
        public void GetSpecialType_ThrowsOnGreaterThanCount()
        {
            var source = "class C1 { }";
            var comp = (Compilation)CreateCompilation(source);

            var specialType = SpecialType.Count + 1;

            var exceptionThrown = false;

            try
            {
                comp.GetSpecialType(specialType);
            }
            catch (ArgumentOutOfRangeException e)
            {
                exceptionThrown = true;
                Assert.StartsWith(expectedStartString: $"Unexpected SpecialType: '{(int)specialType}'.", actualString: e.Message);
            }

            Assert.True(exceptionThrown, $"{nameof(comp.GetSpecialType)} did not throw when it should have.");
        }

        [Fact]
        [WorkItem(34984, "https://github.com/dotnet/roslyn/issues/34984")]
        public void ConversionIsExplicit_UnsetConversionKind()
        {
            var source =
@"class C1
{
}

class C2
{
    public void M() 
    {
        var c = new C1();
        foreach (string item in c.Items)
        {
        }
}";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var root = tree.GetRoot();
            var foreachSyntaxNode = root.DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var foreachSymbolInfo = model.GetForEachStatementInfo(foreachSyntaxNode);

            Assert.Equal(Conversion.UnsetConversion, foreachSymbolInfo.CurrentConversion);
            Assert.True(foreachSymbolInfo.CurrentConversion.Exists);
            Assert.False(foreachSymbolInfo.CurrentConversion.IsImplicit);
        }

        [Fact, WorkItem(29933, "https://github.com/dotnet/roslyn/issues/29933")]
        public void SpeculativelyBindBaseInXmlDoc()
        {
            var text = @"
class C
{
    /// <summary> </summary>
    static void M() { }
}
";

            var compilation = CreateCompilation(text);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var position = text.IndexOf(">", StringComparison.Ordinal);
            var syntax = SyntaxFactory.ParseExpression("base");

            var info = model.GetSpeculativeSymbolInfo(position, syntax, SpeculativeBindingOption.BindAsExpression);
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.NotReferencable, info.CandidateReason);
        }

        [Fact]
        [WorkItem(42840, "https://github.com/dotnet/roslyn/issues/42840")]
        public void DuplicateTypeArgument()
        {
            var source =
@"class A<T>
{
}
class B<T, U, U>
    where T : A<U>
    where U : class
{
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,15): error CS0692: Duplicate type parameter 'U'
                // class B<T, U, U>
                Diagnostic(ErrorCode.ERR_DuplicateTypeParameter, "U").WithArguments("U").WithLocation(4, 15),
                // (5,17): error CS0229: Ambiguity between 'U' and 'U'
                //     where T : A<U>
                Diagnostic(ErrorCode.ERR_AmbigMember, "U").WithArguments("U", "U").WithLocation(5, 17));

            comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var typeParameters = tree.GetRoot().DescendantNodes().OfType<TypeParameterSyntax>().ToArray();
            var symbol = model.GetDeclaredSymbol(typeParameters[typeParameters.Length - 1]);
            Assert.False(symbol.IsReferenceType);
            symbol = model.GetDeclaredSymbol(typeParameters[typeParameters.Length - 2]);
            Assert.True(symbol.IsReferenceType);
        }
    }
}
