// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : CompilingTestBase
    {
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void NoInitializers()
        {
            var source = @"
class C
{
    static int s1;
    int i1;
}";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);

            var tree = compilation.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<VariableDeclarationSyntax>().ToArray();
            Assert.Equal(2, nodes.Length);

            var semanticModel = compilation.GetSemanticModel(tree);
            foreach (var node in nodes)
            {
                Assert.Null(semanticModel.GetOperationInternal(node));
            }
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ConstantInitializers()
        {
            var source = @"
class C
{
    static int s1 = 1;
    int i1 = 1;
    void M(int p1 = 0, params int[] p2 = null) { }
}";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);

            var tree = compilation.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().ToArray();
            Assert.Equal(4, nodes.Length);

            compilation.VerifyOperationTree(nodes[0], expectedOperationTree:
@"IFieldInitializer (Field: System.Int32 C.s1) (OperationKind.FieldInitializerAtDeclaration)
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)");

            compilation.VerifyOperationTree(nodes[1], expectedOperationTree:
@"IFieldInitializer (Field: System.Int32 C.i1) (OperationKind.FieldInitializerAtDeclaration)
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)");

            compilation.VerifyOperationTree(nodes[2], expectedOperationTree:
@"IParameterInitializer (Parameter: [System.Int32 p1 = 0]) (OperationKind.ParameterInitializerAtDeclaration)
  ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)");

            compilation.VerifyOperationTree(nodes[3], expectedOperationTree:
@"IParameterInitializer (Parameter: params System.Int32[] p2) (OperationKind.ParameterInitializerAtDeclaration)
  IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Int32[], Constant: null)
    ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null)");
        }


        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ExpressionInitializers()
        {
            var source = @"
class C
{
    static int s1 = 1 + Foo();
    int i1 = 1 + Foo();

    static int Foo() { return 1; }
}";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);

            var tree = compilation.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().ToArray();
            Assert.Equal(2, nodes.Length);

            compilation.VerifyOperationTree(nodes[0], expectedOperationTree:
@"IFieldInitializer (Field: System.Int32 C.s1) (OperationKind.FieldInitializerAtDeclaration)
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
    Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    Right: IInvocationExpression (static System.Int32 C.Foo()) (OperationKind.InvocationExpression, Type: System.Int32)");

            compilation.VerifyOperationTree(nodes[1], expectedOperationTree:
@"IFieldInitializer (Field: System.Int32 C.i1) (OperationKind.FieldInitializerAtDeclaration)
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
    Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    Right: IInvocationExpression (static System.Int32 C.Foo()) (OperationKind.InvocationExpression, Type: System.Int32)");
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void PartialClasses()
        {
            var source = @"
partial class C
{
    static int s1 = 1;
    int i1 = 1;
}
partial class C
{
    static int s2 = 2;
    int i2 = 2;
}";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);

            var tree = compilation.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().ToArray();
            Assert.Equal(4, nodes.Length);

            compilation.VerifyOperationTree(nodes[0], expectedOperationTree:
@"IFieldInitializer (Field: System.Int32 C.s1) (OperationKind.FieldInitializerAtDeclaration)
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)");

            compilation.VerifyOperationTree(nodes[1], expectedOperationTree:
@"IFieldInitializer (Field: System.Int32 C.i1) (OperationKind.FieldInitializerAtDeclaration)
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)");
            compilation.VerifyOperationTree(nodes[2], expectedOperationTree:
 @"IFieldInitializer (Field: System.Int32 C.s2) (OperationKind.FieldInitializerAtDeclaration)
  ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)");

            compilation.VerifyOperationTree(nodes[3], expectedOperationTree:
@"IFieldInitializer (Field: System.Int32 C.i2) (OperationKind.FieldInitializerAtDeclaration)
  ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)");
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void Events()
        {
            var source = @"
class C
{
    static event System.Action e = MakeAction(1);
    event System.Action f = MakeAction(2);

    static System.Action MakeAction(int x) { return null; }
}";

            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);


            var tree = compilation.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().ToArray();
            Assert.Equal(2, nodes.Length);

            compilation.VerifyOperationTree(nodes[0], expectedOperationTree:
@"IFieldInitializer (Field: System.Action C.e) (OperationKind.FieldInitializerAtDeclaration)
  IInvocationExpression (static System.Action C.MakeAction(System.Int32 x)) (OperationKind.InvocationExpression, Type: System.Action)
    IArgument (Matching Parameter: x) (OperationKind.Argument)
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)");

            compilation.VerifyOperationTree(nodes[1], expectedOperationTree:
@"IFieldInitializer (Field: System.Action C.f) (OperationKind.FieldInitializerAtDeclaration)
  IInvocationExpression (static System.Action C.MakeAction(System.Int32 x)) (OperationKind.InvocationExpression, Type: System.Action)
    IArgument (Matching Parameter: x) (OperationKind.Argument)
      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)");
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void MemberInitializerCSharp()
        {
            const string source = @"
struct Bar
{
    public bool Field;
}

class Foo
{
    public int Field;
    public string Property1 { set; get; }
    public Bar Property2 { set; get; }
}

class C
{
    public void M1()
    {   
        var x1 = new Foo();
        var x2 = new Foo() { Field = 2 };
        var x3 = new Foo() { Property1 = """" };
        var x4 = new Foo() { Property1 = """", Field = 2 };
        var x5 = new Foo() { Property2 = new Bar { Field = true } };

        var e1 = new Foo() { Property2 = 1 };
        var e2 = new Foo() { """" };      
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularWithIOperationFeature);

            compilation.VerifyDiagnostics(
                      // (24,42): error CS0029: Cannot implicitly convert type 'int' to 'Bar'
                      //         var e1 = new Foo() { Property2 = 1 };
                      Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "Bar"),
                      // (25,28): error CS1922: Cannot initialize type 'Foo' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                      //         var e2 = new Foo() { "" };      
                      Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, @"{ """" }").WithArguments("Foo").WithLocation(25, 28));

            var tree = compilation.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(v => v.Initializer).Where(n => n != null).ToArray();
            Assert.Equal(7, nodes.Length);

            compilation.VerifyOperationTree(nodes[0], expectedOperationTree:
@"IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: Foo x1 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo)
");

            compilation.VerifyOperationTree(nodes[1], expectedOperationTree:
@"IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: Foo x2 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo)
        Member Initializers: IFieldInitializer (Field: System.Int32 Foo.Field) (OperationKind.FieldInitializerInCreation)
            ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)");

            compilation.VerifyOperationTree(nodes[2], expectedOperationTree:
@"IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: Foo x3 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo)
        Member Initializers: IPropertyInitializer (Property: System.String Foo.Property1 { get; set; }) (OperationKind.PropertyInitializerInCreation)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )");

            compilation.VerifyOperationTree(nodes[3], expectedOperationTree:
@"IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: Foo x4 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo)
        Member Initializers: IPropertyInitializer (Property: System.String Foo.Property1 { get; set; }) (OperationKind.PropertyInitializerInCreation)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
          IFieldInitializer (Field: System.Int32 Foo.Field) (OperationKind.FieldInitializerInCreation)
            ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)");

            compilation.VerifyOperationTree(nodes[4], expectedOperationTree:
@"IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: Foo x5 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo)
        Member Initializers: IPropertyInitializer (Property: Bar Foo.Property2 { get; set; }) (OperationKind.PropertyInitializerInCreation)
            IObjectCreationExpression (Constructor: Bar..ctor()) (OperationKind.ObjectCreationExpression, Type: Bar)
              Member Initializers: IFieldInitializer (Field: System.Boolean Bar.Field) (OperationKind.FieldInitializerInCreation)
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)");

            compilation.VerifyOperationTree(nodes[5], expectedOperationTree:
@"IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: Foo e1 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IObjectCreationExpression (Constructor: Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo, IsInvalid)
        Member Initializers: IPropertyInitializer (Property: Bar Foo.Property2 { get; set; }) (OperationKind.PropertyInitializerInCreation, IsInvalid)
            IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Bar, IsInvalid)
              ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)");

            compilation.VerifyOperationTree(nodes[6], expectedOperationTree:
@"IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: Foo e2 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IObjectCreationExpression (Constructor: Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo, IsInvalid)");
        }
    }
}