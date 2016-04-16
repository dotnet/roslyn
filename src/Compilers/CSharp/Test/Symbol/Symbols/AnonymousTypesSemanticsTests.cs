// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class AnonymousTypesSemanticsTests : CompilingTestBase
    {
        [Fact()]
        public void AnonymousTypeSymbols_Simple()
        {
            var source = @"
public class ClassA
{
    public struct SSS
    {
    }

    public static void Test1(int x)
    {
        object v1 = [# new
        {
            [# aa  #] = 1,
            [# BB  #] = """",
            [# CCC #] = new SSS()
        } #];

        object v2 = [# new
        {
            [# aa  #] = new SSS(),
            [# BB  #] = 123.456,
            [# CCC #] = [# new
            {
                (new ClassA()).[# aa  #],
                ClassA.[# BB  #],
                ClassA.[# CCC #]
            } #]
        } #];

        object v3 = [# new {} #];
        var v4 = [# new {} #];
    }

    public int aa
    {
        get { return 123; }
    }

    public const string BB = ""-=-=-"";

    public static SSS CCC = new SSS();
}";
            var data = Compile(source, 14);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1, 2, 3);

            var info1 = GetAnonymousTypeInfoSummary(data, 4,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 3).Span,
                            5, 6, 7);

            var info2 = GetAnonymousTypeInfoSummary(data, 8,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 5).Span,
                            9, 10, 11);

            Assert.Equal(info0.Type, info2.Type);
            Assert.NotEqual(info0.Type, info1.Type);

            var info3 = GetAnonymousTypeInfoSummary(data, 12,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 7).Span);
            var info4 = GetAnonymousTypeInfoSummary(data, 13,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 8).Span);
            Assert.Equal(info3.Type, info4.Type);
        }

        [Fact()]
        public void AnonymousTypeSymbols_ContextualKeywordsInFields()
        {
            var source = @"
class ClassA
{
    static void Test1(int x)
    {
        object v1 = [# new
        {
            [# var #] = ""var"",
            [# get #] = new {},
            [# partial #] = [# new
                            {
                                (new ClassA()).[# select #],
                                [# global  #]
                            } #]
        } #];
    }

    public int select
    {
        get { return 123; }
    }

    public const string global = ""-=-=-"";
}";
            var data = Compile(source, 7);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1, 2, 3);

            var info1 = GetAnonymousTypeInfoSummary(data, 4,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 3).Span,
                            5, 6);

            Assert.Equal(
                "<anonymous type: System.String var, <empty anonymous type> get, <anonymous type: System.Int32 select, System.String global> partial>",
                info0.Type.ToTestDisplayString());

            Assert.Equal(
                "<anonymous type: System.Int32 select, System.String global>..ctor(System.Int32 select, System.String global)",
                info1.Symbol.ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeSymbols_DelegateMembers()
        {
            var source = @"
delegate bool D1();
class ClassA
{
    void Main()
    {
        var at1 = [# new { [# module #] = (D1)(() => false)} #].module();
    }
}";
            var data = Compile(source, 2);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1);

            Assert.Equal("<anonymous type: D1 module>", info0.Type.ToTestDisplayString());
            Assert.Equal("<anonymous type: D1 module>..ctor(D1 module)", info0.Symbol.ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeSymbols_BaseAccessInMembers()
        {
            var source = @"
delegate bool D1();
class ClassB
{
    protected System.Func<int, int> F = x => x;
}
class ClassA: ClassB
{
    void Main()
    {
        var at1 = [# [# new { base.[# F #] } #].F(1) #];
    }
}";
            var data = Compile(source, 3);

            var info0 = GetAnonymousTypeInfoSummary(data, 1,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            2);

            Assert.Equal("<anonymous type: System.Func<System.Int32, System.Int32> F>", info0.Type.ToTestDisplayString());

            var info1 = data.Model.GetSemanticInfoSummary(data.Nodes[0]);

            Assert.Equal("System.Int32 System.Func<System.Int32, System.Int32>.Invoke(System.Int32 arg)", info1.Symbol.ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeSymbols_InFieldInitializer()
        {
            var source = @"
class ClassA
{
    private static object F = [# new { [# F123 #] = typeof(ClassA) } #];
}";
            var data = Compile(source, 2);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1);

            Assert.Equal("<anonymous type: System.Type F123>", info0.Type.ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeSymbols_Equals()
        {
            var source = @"
class ClassA
{
    static void Test1(int x)
    {
        bool result = [# new { f1 = 1, f2 = """" }.Equals(new { }) #];
    }
}";
            var data = Compile(source, 1);

            var info = data.Model.GetSemanticInfoSummary(data.Nodes[0]);

            var method = info.Symbol;
            Assert.NotNull(method);
            Assert.Equal(SymbolKind.Method, method.Kind);
            Assert.Equal("object.Equals(object)", method.ToDisplayString());
        }

        [Fact()]
        public void AnonymousTypeSymbols_ToString()
        {
            var source = @"
class ClassA
{
    static void Test1(int x)
    {
        string result = [# new { f1 = 1, f2 = """" }.ToString() #];
    }
}";
            var data = Compile(source, 1);

            var info = data.Model.GetSemanticInfoSummary(data.Nodes[0]);

            var method = info.Symbol;
            Assert.NotNull(method);
            Assert.Equal(SymbolKind.Method, method.Kind);
            Assert.Equal("object.ToString()", method.ToDisplayString());
        }

        [Fact()]
        public void AnonymousTypeSymbols_GetHashCode()
        {
            var source = @"
class ClassA
{
    static void Test1(int x)
    {
        int result = [# new { f1 = 1, f2 = """" }.GetHashCode() #];
    }
}";
            var data = Compile(source, 1);

            var info = data.Model.GetSemanticInfoSummary(data.Nodes[0]);

            var method = info.Symbol;
            Assert.NotNull(method);
            Assert.Equal(SymbolKind.Method, method.Kind);
            Assert.Equal("object.GetHashCode()", method.ToDisplayString());
        }

        [Fact()]
        public void AnonymousTypeSymbols_Ctor()
        {
            var source = @"
class ClassA
{
    static void Test1(int x)
    {
        var result = [# new { f1 = 1, f2 = """" } #];
    }
}";
            var data = Compile(source, 1);

            var info = data.Model.GetSemanticInfoSummary(data.Nodes[0]);

            var method = info.Symbol;
            Assert.NotNull(method);
            Assert.Equal(SymbolKind.Method, method.Kind);
            Assert.Equal("<anonymous type: int f1, string f2>..ctor(int, string)", method.ToDisplayString());
            Assert.Equal("<anonymous type: System.Int32 f1, System.String f2>..ctor(System.Int32 f1, System.String f2)", method.ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeTemplateCannotConstruct()
        {
            var source = @"
class ClassA
{
    object F = [# new { [# F123 #] = typeof(ClassA) } #];
}";
            var data = Compile(source, 2);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1);

            var type = info0.Type;
            Assert.Equal("<anonymous type: System.Type F123>", type.ToTestDisplayString());
            Assert.True(type.IsDefinition);
            AssertCannotConstruct(type);
        }

        [Fact()]
        public void AnonymousTypeTemplateCannotConstruct_Empty()
        {
            var source = @"
class ClassA
{
    object F = [# new { } #];
}";
            var data = Compile(source, 1);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span);

            var type = info0.Type;
            Assert.Equal("<empty anonymous type>", type.ToTestDisplayString());
            Assert.True(type.IsDefinition);
            AssertCannotConstruct(type);
        }

        [Fact()]
        public void AnonymousTypeFieldDeclarationIdentifier()
        {
            var source = @"
class ClassA
{
    object F = new { [# F123 #] = typeof(ClassA) };
}";
            var data = Compile(source, 1);
            var info = data.Model.GetSymbolInfo((ExpressionSyntax)data.Nodes[0]);
            Assert.NotNull(info.Symbol);
            Assert.Equal(SymbolKind.Property, info.Symbol.Kind);
            Assert.Equal("System.Type <anonymous type: System.Type F123>.F123 { get; }", info.Symbol.ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeFieldCreatedInQuery()
        {
            var source = LINQ + @"
class ClassA
{
    void m()
    {
        var o = from x in new List1<int>(1, 2, 3) select [# new { [# x #], [# y #] = x } #];
    }
}";
            var data = Compile(source, 3);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, NumberOfNewKeywords(LINQ) + 2).Span,
                1, 2);

            var info1 = data.Model.GetSymbolInfo(((AnonymousObjectMemberDeclaratorSyntax)data.Nodes[1]).Expression);
            Assert.NotNull(info1.Symbol);
            Assert.Equal(SymbolKind.RangeVariable, info1.Symbol.Kind);
            Assert.Equal("x", info1.Symbol.ToDisplayString());

            var info2 = data.Model.GetSymbolInfo((ExpressionSyntax)data.Nodes[2]);
            Assert.NotNull(info2.Symbol);
            Assert.Equal(SymbolKind.Property, info2.Symbol.Kind);
            Assert.Equal("System.Int32 <anonymous type: System.Int32 x, System.Int32 y>.y { get; }", info2.Symbol.ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeFieldCreatedInQuery2()
        {
            var source = LINQ + @"
class ClassA
{
    void m()
    {
        var o = from x in new List1<int>(1, 2, 3) let y = """" select [# new { [# x #], [# y #] } #];
    }
}";
            var data = Compile(source, 3);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, NumberOfNewKeywords(LINQ) + 2).Span,
                1, 2);

            Assert.Equal("<anonymous type: System.Int32 x, System.String y>", info0.Type.ToTestDisplayString());

            var info1 = data.Model.GetSymbolInfo(((AnonymousObjectMemberDeclaratorSyntax)data.Nodes[1]).Expression);
            Assert.NotNull(info1.Symbol);
            Assert.Equal(SymbolKind.RangeVariable, info1.Symbol.Kind);
            Assert.Equal("x", info1.Symbol.ToDisplayString());

            var info2 = data.Model.GetSymbolInfo(((AnonymousObjectMemberDeclaratorSyntax)data.Nodes[2]).Expression);
            Assert.NotNull(info2.Symbol);
            Assert.Equal(SymbolKind.RangeVariable, info2.Symbol.Kind);
            Assert.Equal("y", info2.Symbol.ToDisplayString());
        }

        [Fact()]
        public void AnonymousTypeFieldCreatedInLambda()
        {
            var source = @"
using System;
class ClassA
{
    void m()
    {
        var o = (Action)(() => ( [# new { [# x #] = 1, [# y #] = [# new { } #] } #]).ToString());;
    }
}";
            var data = Compile(source, 4);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                1, 2);

            var info1 = GetAnonymousTypeInfoSummary(data, 3,
                data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 2).Span);

            Assert.Equal("<anonymous type: System.Int32 x, <empty anonymous type> y>..ctor(System.Int32 x, <empty anonymous type> y)", info0.Symbol.ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeFieldCreatedInLambda2()
        {
            var source = @"
using System;
class ClassA
{
    void m()
    {
        var o = (Action)
                    (() =>
                        ((Func<string>) (() => ( [# new { [# x #] = 1, [# y #] = [# new { } #] } #]).ToString())
                    ).Invoke());
    }
}";
            var data = Compile(source, 4);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                1, 2);

            var info1 = GetAnonymousTypeInfoSummary(data, 3,
                data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 2).Span);

            Assert.Equal("<anonymous type: System.Int32 x, <empty anonymous type> y>..ctor(System.Int32 x, <empty anonymous type> y)", info0.Symbol.ToTestDisplayString());
        }

        [ClrOnlyFact]
        public void AnonymousTypeSymbols_DontCrashIfNameIsQueriedBeforeEmit()
        {
            var source = @"
public class ClassA
{
    public static void Test1(int x)
    {
        object v1 = [# new { [# aa  #] = 1, [# BB  #] = 2 } #];
        object v2 = [# new { } #];
    }
}";
            var data = Compile(source, 4);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1, 2);

            CheckAnonymousType(info0.Type, "", "");

            info0 = GetAnonymousTypeInfoSummary(data, 3,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 2).Span);

            CheckAnonymousType(info0.Type, "", "");

            //  perform emit
            CompileAndVerify(
                data.Compilation,
                symbolValidator: module => CheckAnonymousTypes(module)
            );
        }

        #region "AnonymousTypeSymbols_DontCrashIfNameIsQueriedBeforeEmit"

        private void CheckAnonymousType(ITypeSymbol type, string name, string metadataName)
        {
            Assert.NotNull(type);
            Assert.Equal(name, type.Name);
            Assert.Equal(metadataName, type.MetadataName);
        }

        private void CheckAnonymousTypes(ModuleSymbol module)
        {
            var ns = module.GlobalNamespace;
            Assert.NotNull(ns);

            CheckAnonymousType(ns.GetMember<NamedTypeSymbol>("<>f__AnonymousType0"), "<>f__AnonymousType0", "<>f__AnonymousType0`2");
            CheckAnonymousType(ns.GetMember<NamedTypeSymbol>("<>f__AnonymousType1"), "<>f__AnonymousType1", "<>f__AnonymousType1");
        }

        #endregion

        [Fact()]
        public void AnonymousTypeSymbols_Error_Simple()
        {
            var source = @"
public class ClassA
{
    public static void Test1(int x)
    {
        object v1 = [# new
        {
            [# aa  #] = xyz,
            [# BB  #] = """",
            [# CCC #] = new SSS()
        } #];

        object v2 = [# new
        {
            [# aa  #] = new SSS(),
            [# BB  #] = 123.456,
            [# CCC #] = [# new
            {
                (new ClassA()).[# aa  #],
                ClassA.[# BB  #],
                ClassA.[# CCC #]
            } #]
        } #];
    }
}";
            var data = Compile(source, 12,
                // (8,25): error CS0103: The name 'xyz' does not exist in the current context
                //                aa     = xyz,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "xyz").WithArguments("xyz"),
                // (10,29): error CS0246: The type or namespace name 'SSS' could not be found (are you missing a using directive or an assembly reference?)
                //                CCC    = new SSS()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "SSS").WithArguments("SSS"),
                // (15,29): error CS0246: The type or namespace name 'SSS' could not be found (are you missing a using directive or an assembly reference?)
                //                aa     = new SSS(),
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "SSS").WithArguments("SSS"),
                // (19,35): error CS1061: 'ClassA' does not contain a definition for 'aa' and no extension method 'aa' accepting a first argument of type 'ClassA' could be found (are you missing a using directive or an assembly reference?)
                //                 (new ClassA()).   aa    ,
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "aa").WithArguments("ClassA", "aa"),
                // (20,27): error CS0117: 'ClassA' does not contain a definition for 'BB'
                //                 ClassA.   BB    ,
                Diagnostic(ErrorCode.ERR_NoSuchMember, "BB").WithArguments("ClassA", "BB"),
                // (21,27): error CS0117: 'ClassA' does not contain a definition for 'CCC'
                //                 ClassA.   CCC   
                Diagnostic(ErrorCode.ERR_NoSuchMember, "CCC").WithArguments("ClassA", "CCC")
            );

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1, 2, 3);

            var info1 = GetAnonymousTypeInfoSummary(data, 4,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 3).Span,
                            5, 6, 7);

            var info2 = GetAnonymousTypeInfoSummary(data, 8,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 5).Span,
                            9, 10, 11);

            Assert.Equal("<anonymous type: ? aa, System.String BB, SSS CCC>", info0.Type.ToTestDisplayString());
            Assert.Equal("<anonymous type: SSS aa, System.Double BB, <anonymous type: ? aa, ? BB, ? CCC> CCC>", info1.Type.ToTestDisplayString());
            Assert.Equal("<anonymous type: ? aa, ? BB, ? CCC>", info2.Type.ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeSymbols_Error_InUsingStatement()
        {
            var source = @"
public class ClassA
{
    public static void Test1(int x)
    {
        using (var v1 = [# new { } #])
        {
        }
    }
}";
            var data = Compile(source, 1,
                // (6,16): error CS1674: '<empty anonymous type>': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (var v1 =    new { }   )
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var v1 =    new { }").WithArguments("<empty anonymous type>")
            );

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span);

            Assert.Equal("<empty anonymous type>", info0.Type.ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeSymbols_Error_DuplicateName()
        {
            var source = @"
public class ClassA
{
    public static void Test1(int x)
    {
        object v1 = [# new
        {
            [# aa  #] = 1,
            ClassA.[# aa #],
            [# bb #] = 1.2
        } #];
    }

    public static string aa = ""-field-aa-"";
}";
            var data = Compile(source, 4,
                // (9,13): error CS0833: An anonymous type cannot have multiple properties with the same name
                //             ClassA.   aa   ,
                Diagnostic(ErrorCode.ERR_AnonymousTypeDuplicatePropertyName, "ClassA.   aa")
            );

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1, /*2,*/ 3);

            Assert.Equal("<anonymous type: System.Int32 aa, System.String $1, System.Double bb>", info0.Type.ToTestDisplayString());

            var properties = (from m in info0.Type.GetMembers() where m.Kind == SymbolKind.Property select m).ToArray();
            Assert.Equal(3, properties.Length);

            Assert.Equal("System.Int32 <anonymous type: System.Int32 aa, System.String $1, System.Double bb>.aa { get; }", properties[0].ToTestDisplayString());
            Assert.Equal("System.String <anonymous type: System.Int32 aa, System.String $1, System.Double bb>.$1 { get; }", properties[1].ToTestDisplayString());
            Assert.Equal("System.Double <anonymous type: System.Int32 aa, System.String $1, System.Double bb>.bb { get; }", properties[2].ToTestDisplayString());
        }

        [Fact()]
        public void AnonymousTypeSymbols_LookupSymbols()
        {
            var source = @"
public class ClassA
{
    public static void Test1(int x)
    {
        object v1 = [# new
        {
            [# aa  #] = """",
            [# abc #] = 123.456
        } #];
        object v2 = [# new{ } #];
    }
}";
            var data = Compile(source, 4);

            var info0 = GetAnonymousTypeInfoSummary(data, 0,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 1).Span,
                            1, 2);

            Assert.Equal("<anonymous type: System.String aa, System.Double abc>", info0.Type.ToTestDisplayString());

            var pos = data.Nodes[0].Span.End;
            var syms = data.Model.LookupSymbols(pos, container: info0.Type).Select(x => x.ToTestDisplayString()).OrderBy(x => x).ToArray();
            Assert.Equal(8, syms.Length);

            int index = 0;
            Assert.Equal("System.Boolean System.Object.Equals(System.Object obj)", syms[index++]);
            Assert.Equal("System.Boolean System.Object.Equals(System.Object objA, System.Object objB)", syms[index++]);
            Assert.Equal("System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)", syms[index++]);
            Assert.Equal("System.Double <anonymous type: System.String aa, System.Double abc>.abc { get; }", syms[index++]);
            Assert.Equal("System.Int32 System.Object.GetHashCode()", syms[index++]);
            Assert.Equal("System.String <anonymous type: System.String aa, System.Double abc>.aa { get; }", syms[index++]);
            Assert.Equal("System.String System.Object.ToString()", syms[index++]);
            Assert.Equal("System.Type System.Object.GetType()", syms[index++]);

            info0 = GetAnonymousTypeInfoSummary(data, 3,
                            data.Tree.FindNodeOrTokenByKind(SyntaxKind.NewKeyword, 2).Span);

            Assert.Equal("<empty anonymous type>", info0.Type.ToTestDisplayString());

            pos = data.Nodes[3].Span.End;
            syms = data.Model.LookupSymbols(pos, container: info0.Type).Select(x => x.ToTestDisplayString()).OrderBy(x => x).ToArray();
            Assert.Equal(6, syms.Length);

            index = 0;
            Assert.Equal("System.Boolean System.Object.Equals(System.Object obj)", syms[index++]);
            Assert.Equal("System.Boolean System.Object.Equals(System.Object objA, System.Object objB)", syms[index++]);
            Assert.Equal("System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)", syms[index++]);
            Assert.Equal("System.Int32 System.Object.GetHashCode()", syms[index++]);
            Assert.Equal("System.String System.Object.ToString()", syms[index++]);
            Assert.Equal("System.Type System.Object.GetType()", syms[index++]);
        }

        [WorkItem(543189, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543189")]
        [Fact()]
        public void CheckAnonymousTypeAsConstValue()
        {
            var source = @"
public class A
{
    const int i = /*<bind>*/(new {a = 2}).a/*</bind>*/;
}";

            var comp = CreateCompilationWithMscorlib(source);
            var tuple = GetBindingNodeAndModel<ExpressionSyntax>(comp);
            var info = tuple.Item2.GetSymbolInfo(tuple.Item1);
            Assert.NotNull(info.Symbol);
            Assert.Equal("<anonymous type: int a>.a", info.Symbol.ToDisplayString());
        }

        [WorkItem(546416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546416")]
        [ClrOnlyFact]
        public void TestAnonymousTypeInsideGroupBy_Queryable()
        {
            CompileAndVerify(
 @"using System.Linq;

public class Product
{
    public int ProductID;
    public string ProductName;
    public int SupplierID;
}
public class DB
{
    public IQueryable<Product> Products;
}

public class Program
{
    public static void Main()
    {
        var db = new DB();
        var q0 = db.Products.GroupBy(p => new { Conditional = false ? new { p.ProductID, p.ProductName, p.SupplierID } : new { p.ProductID, p.ProductName, p.SupplierID } }).ToList();
    }
}", additionalRefs: new[] { SystemCoreRef }).VerifyDiagnostics();
        }

        [WorkItem(546416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546416")]
        [ClrOnlyFact]
        public void TestAnonymousTypeInsideGroupBy_Enumerable()
        {
            CompileAndVerify(
 @"using System.Linq;
using System.Collections.Generic;

public class Product
{
    public int ProductID;
    public string ProductName;
    public int SupplierID;
}
public class DB
{
    public IEnumerable<Product> Products;
}

public class Program
{
    public static void Main()
    {
        var db = new DB();
        var q0 = db.Products.GroupBy(p => new { Conditional = false ? new { p.ProductID, p.ProductName, p.SupplierID } : new { p.ProductID, p.ProductName, p.SupplierID } }).ToList();
    }
}", additionalRefs: new[] { SystemCoreRef }).VerifyDiagnostics();
        }

        [WorkItem(546416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546416")]
        [ClrOnlyFact]
        public void TestAnonymousTypeInsideGroupBy_Enumerable2()
        {
            CompileAndVerify(
 @"using System.Linq;
using System.Collections.Generic;

public class Product
{
    public int ProductID;
    public int SupplierID;
}
public class DB
{
    public IEnumerable<Product> Products;
}

public class Program
{
    public static void Main()
    {
        var db = new DB();
        var q0 = db.Products.GroupBy(p => new { Conditional = false ? new { p.ProductID, p.SupplierID } : new { p.ProductID, p.SupplierID } }).ToList();
        var q1 = db.Products.GroupBy(p => new { Conditional = false ? new { p.ProductID, p.SupplierID } : new { p.ProductID, p.SupplierID } }).ToList();
    }
}", additionalRefs: new[] { SystemCoreRef }).VerifyDiagnostics();
        }

        #region "Utility methods"

        private void AssertCannotConstruct(ISymbol type)
        {
            var namedType = type as NamedTypeSymbol;
            Assert.NotNull(namedType);

            var objType = namedType.BaseType;
            Assert.NotNull(objType);
            Assert.Equal("System.Object", objType.ToTestDisplayString());

            TypeSymbol[] args = new TypeSymbol[namedType.Arity];
            for (int i = 0; i < namedType.Arity; i++)
            {
                args[i] = objType;
            }

            Assert.Throws<InvalidOperationException>(() => namedType.Construct(args));
        }

        private CompilationUtils.SemanticInfoSummary GetAnonymousTypeInfoSummary(TestData data, int node, TextSpan typeSpan, params int[] fields)
        {
            var info = data.Model.GetSemanticInfoSummary(data.Nodes[node]);
            var type = info.Type;

            Assert.True(type.IsAnonymousType);
            Assert.False(type.CanBeReferencedByName);

            Assert.Equal("System.Object", type.BaseType.ToTestDisplayString());
            Assert.Equal(0, type.Interfaces.Length);

            Assert.Equal(1, type.Locations.Length);
            Assert.Equal(typeSpan, type.Locations[0].SourceSpan);

            foreach (int field in fields)
            {
                CheckFieldNameAndLocation(data, type, data.Nodes[field]);
            }

            return info;
        }

        private void CheckFieldNameAndLocation(TestData data, ITypeSymbol type, SyntaxNode identifier)
        {
            var anonymousType = (NamedTypeSymbol)type;

            var current = identifier;
            while (current.Span == identifier.Span && !current.IsKind(SyntaxKind.IdentifierName))
            {
                current = current.ChildNodes().Single();
            }
            var node = (IdentifierNameSyntax)current;
            Assert.NotNull(node);

            var span = node.Span;
            var fieldName = node.ToString();

            var property = anonymousType.GetMember<PropertySymbol>(fieldName);
            Assert.NotNull(property);
            Assert.Equal(fieldName, property.Name);
            Assert.Equal(1, property.Locations.Length);
            Assert.Equal(span, property.Locations[0].SourceSpan);

            MethodSymbol getter = property.GetMethod;
            Assert.NotNull(getter);
            Assert.Equal("get_" + fieldName, getter.Name);
        }

        private struct TestData
        {
            public CSharpCompilation Compilation;
            public SyntaxTree Tree;
            public List<SyntaxNode> Nodes;
            public SemanticModel Model;
        }

        private TestData Compile(string source, int expectedIntervals, params DiagnosticDescription[] diagnostics)
        {
            var intervals = ExtractTextIntervals(ref source);
            Assert.Equal(expectedIntervals, intervals.Count);

            var compilation = Compile(source);

            compilation.VerifyDiagnostics(diagnostics);

            var tree = compilation.SyntaxTrees[0];
            var nodes = new List<SyntaxNode>();

            foreach (var span in intervals)
            {
                var stack = new Stack<SyntaxNode>();
                stack.Push(tree.GetCompilationUnitRoot());

                while (stack.Count > 0)
                {
                    var node = stack.Pop();
                    if (span.Contains(node.Span))
                    {
                        nodes.Add(node);
                        break;
                    }

                    foreach (var child in node.ChildNodes())
                    {
                        stack.Push(child);
                    }
                }
            }
            Assert.Equal(expectedIntervals, nodes.Count);

            return new TestData()
            {
                Compilation = compilation,
                Tree = tree,
                Model = compilation.GetSemanticModel(tree),
                Nodes = nodes
            };
        }

        private CSharpCompilation Compile(string source)
        {
            return (CSharpCompilation)GetCompilationForEmit(
                new[] { source },
                new MetadataReference[] { },
                TestOptions.ReleaseDll,
                TestOptions.Regular
            );
        }

        private static List<TextSpan> ExtractTextIntervals(ref string source)
        {
            const string startTag = "[#";
            const string endTag = "#]";

            List<TextSpan> intervals = new List<TextSpan>();

            var all = (from s in FindAll(source, startTag)
                       select new { start = true, offset = s }).Union(
                                from s in FindAll(source, endTag)
                                select new { start = false, offset = s }
                      ).OrderBy(value => value.offset).ToList();

            while (all.Count > 0)
            {
                int i = 1;
                bool added = false;
                while (i < all.Count)
                {
                    if (all[i - 1].start && !all[i].start)
                    {
                        intervals.Add(TextSpan.FromBounds(all[i - 1].offset, all[i].offset));
                        all.RemoveAt(i);
                        all.RemoveAt(i - 1);
                        added = true;
                    }
                    else
                    {
                        i++;
                    }
                }
                Assert.True(added);
            }

            source = source.Replace(startTag, "  ").Replace(endTag, "  ");

            intervals.Sort((x, y) => x.Start.CompareTo(y.Start));
            return intervals;
        }

        private static IEnumerable<int> FindAll(string source, string what)
        {
            int index = source.IndexOf(what, StringComparison.Ordinal);
            while (index >= 0)
            {
                yield return index;
                index = source.IndexOf(what, index + 1, StringComparison.Ordinal);
            }
        }

        private int NumberOfNewKeywords(string source)
        {
            int cnt = 0;
            foreach (var line in source.Split(new String[] { Environment.NewLine }, StringSplitOptions.None))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (!line.Trim().StartsWith("//", StringComparison.Ordinal))
                    {
                        for (int index = line.IndexOf("new ", StringComparison.Ordinal); index >= 0;)
                        {
                            cnt++;
                            index = line.IndexOf("new ", index + 1, StringComparison.Ordinal);
                        }
                    }
                }
            }
            return cnt;
        }

        #endregion
    }
}
