// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void NoInitializers()
        {
            var source = @"
class C
{
    static int s1;
    int i1;
    int P1 { get; }
}";

            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);

            var tree = compilation.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().Where(n => n is VariableDeclarationSyntax || n is PropertyDeclarationSyntax).ToArray();
            Assert.Equal(3, nodes.Length);

            var semanticModel = compilation.GetSemanticModel(tree);
            foreach (var node in nodes)
            {
                Assert.Null(semanticModel.GetOperationInternal(node));
            }
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ConstantInitializers_StaticField()
        {
            string source = @"
class C
{
    static int s1 /*<bind>*/= 1/*</bind>*/;
}
";
            string expectedOperationTree = @"
IFieldInitializer (Field: System.Int32 C.s1) (OperationKind.FieldInitializerAtDeclaration) (Syntax: '= 1')
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0414: The field 'C.s1' is assigned but its value is never used
                //     static int s1 /*<bind>*/= 1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s1").WithArguments("C.s1").WithLocation(4, 16)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ConstantInitializers_InstanceField()
        {
            string source = @"
class C
{
    int i1 = 1, i2 /*<bind>*/= 2/*</bind>*/;
}
";
            string expectedOperationTree = @"
IFieldInitializer (Field: System.Int32 C.i2) (OperationKind.FieldInitializerAtDeclaration) (Syntax: '= 2')
  ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0414: The field 'C.i2' is assigned but its value is never used
                //     int i1 = 1, i2 /*<bind>*/= 2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i2").WithArguments("C.i2").WithLocation(4, 17),
                // CS0414: The field 'C.i1' is assigned but its value is never used
                //     int i1 = 1, i2 /*<bind>*/= 2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i1").WithArguments("C.i1").WithLocation(4, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ConstantInitializers_Property()
        {
            string source = @"
class C
{
    int P1 { get; } /*<bind>*/= 1/*</bind>*/;
}
";
            string expectedOperationTree = @"
IPropertyInitializer (Property: System.Int32 C.P1 { get; }) (OperationKind.PropertyInitializerAtDeclaration) (Syntax: '= 1')
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ConstantInitializers_DefaultValueParameter()
        {
            string source = @"
class C
{
    void M(int p1 /*<bind>*/= 0/*</bind>*/, params int[] p2 = null) { }
}
";
            string expectedOperationTree = @"
IParameterInitializer (Parameter: [System.Int32 p1 = 0]) (OperationKind.ParameterInitializerAtDeclaration) (Syntax: '= 0')
  ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1751: Cannot specify a default value for a parameter array
                //     void M(int p1 /*<bind>*/= 0/*</bind>*/, params int[] p2 = null) { }
                Diagnostic(ErrorCode.ERR_DefaultValueForParamsParameter, "params").WithLocation(4, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ConstantInitializers_DefaultValueParamsArray()
        {
            string source = @"
class C
{
    void M(int p1 = 0, params int[] p2 /*<bind>*/= null/*</bind>*/) { }
}
";
            string expectedOperationTree = @"
IParameterInitializer (Parameter: params System.Int32[] p2) (OperationKind.ParameterInitializerAtDeclaration) (Syntax: '= null')
  IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Int32[], Constant: null) (Syntax: 'null')
    ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1751: Cannot specify a default value for a parameter array
                //     void M(int p1 = 0, params int[] p2 /*<bind>*/= null/*</bind>*/) { }
                Diagnostic(ErrorCode.ERR_DefaultValueForParamsParameter, "params").WithLocation(4, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ExpressionInitializers_StaticField()
        {
            string source = @"
class C
{
    static int s1 /*<bind>*/= 1 + F()/*</bind>*/;

    static int F() { return 1; }
}
";
            string expectedOperationTree = @"
IFieldInitializer (Field: System.Int32 C.s1) (OperationKind.FieldInitializerAtDeclaration) (Syntax: '= 1 + F()')
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '1 + F()')
    Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: IInvocationExpression (static System.Int32 C.F()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'F()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ExpressionInitializers_InstanceField()
        {
            string source = @"
class C
{
    static int s1 /*<bind>*/= 1 + F()/*</bind>*/;
    int i1 = 1 + F();
    int P1 { get; } = 1 + F();

    static int F() { return 1; }
}
";
            string expectedOperationTree = @"
IFieldInitializer (Field: System.Int32 C.s1) (OperationKind.FieldInitializerAtDeclaration) (Syntax: '= 1 + F()')
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '1 + F()')
    Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: IInvocationExpression (static System.Int32 C.F()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'F()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void ExpressionInitializers_Property()
        {
            string source = @"
class C
{
    int i1 /*<bind>*/= 1 + F()/*</bind>*/;

    static int F() { return 1; }
}
";
            string expectedOperationTree = @"
IFieldInitializer (Field: System.Int32 C.i1) (OperationKind.FieldInitializerAtDeclaration) (Syntax: '= 1 + F()')
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '1 + F()')
    Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: IInvocationExpression (static System.Int32 C.F()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'F()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void PartialClasses_StaticField()
        {
            string source = @"
partial class C
{
    static int s1 /*<bind>*/= 1/*</bind>*/;
    int i1 = 1;
}

partial class C
{
    static int s2 = 2;
    int i2 = 2;
}
";
            string expectedOperationTree = @"
IFieldInitializer (Field: System.Int32 C.s1) (OperationKind.FieldInitializerAtDeclaration) (Syntax: '= 1')
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0414: The field 'C.i1' is assigned but its value is never used
                //     int i1 = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i1").WithArguments("C.i1").WithLocation(5, 9),
                // CS0414: The field 'C.s2' is assigned but its value is never used
                //     static int s2 = 2;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s2").WithArguments("C.s2").WithLocation(10, 16),
                // CS0414: The field 'C.s1' is assigned but its value is never used
                //     static int s1 /*<bind>*/= 1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s1").WithArguments("C.s1").WithLocation(4, 16),
                // CS0414: The field 'C.i2' is assigned but its value is never used
                //     int i2 = 2;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i2").WithArguments("C.i2").WithLocation(11, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void PartialClasses_InstanceField()
        {
            string source = @"
partial class C
{
    static int s1 = 1;
    int i1 = 1;
}

partial class C
{
    static int s2 = 2;
    int i2 /*<bind>*/= 2/*</bind>*/;
}
";
            string expectedOperationTree = @"
IFieldInitializer (Field: System.Int32 C.i2) (OperationKind.FieldInitializerAtDeclaration) (Syntax: '= 2')
  ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0414: The field 'C.s2' is assigned but its value is never used
                //     static int s2 = 2;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s2").WithArguments("C.s2").WithLocation(10, 16),
                // CS0414: The field 'C.i2' is assigned but its value is never used
                //     int i2 /*<bind>*/= 2/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i2").WithArguments("C.i2").WithLocation(11, 9),
                // CS0414: The field 'C.s1' is assigned but its value is never used
                //     static int s1 = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s1").WithArguments("C.s1").WithLocation(4, 16),
                // CS0414: The field 'C.i1' is assigned but its value is never used
                //     int i1 = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "i1").WithArguments("C.i1").WithLocation(5, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void Events_StaticField()
        {
            string source = @"
class C
{
    static event System.Action e /*<bind>*/= MakeAction(1)/*</bind>*/;

    static System.Action MakeAction(int x) { return null; }
}
";
            string expectedOperationTree = @"
IFieldInitializer (Field: System.Action C.e) (OperationKind.FieldInitializerAtDeclaration) (Syntax: '= MakeAction(1)')
  IInvocationExpression (static System.Action C.MakeAction(System.Int32 x)) (OperationKind.InvocationExpression, Type: System.Action) (Syntax: 'MakeAction(1)')
    Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void Events_InstanceField()
        {
            string source = @"
class C
{
    event System.Action f /*<bind>*/= MakeAction(2)/*</bind>*/;

    static System.Action MakeAction(int x) { return null; }
}
";
            string expectedOperationTree = @"
IFieldInitializer (Field: System.Action C.f) (OperationKind.FieldInitializerAtDeclaration) (Syntax: '= MakeAction(2)')
  IInvocationExpression (static System.Action C.MakeAction(System.Int32 x)) (OperationKind.InvocationExpression, Type: System.Action) (Syntax: 'MakeAction(2)')
    Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '2')
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17595, "https://github.com/dotnet/roslyn/issues/17595")]
        public void MemberInitializerCSharp()
        {
            string source = @"
struct B
{
    public bool Field;
}

class F
{
    public int Field;
    public string Property1 { set; get; }
    public B Property2 { set; get; }
}

class C
{
    public void M1()
    /*<bind>*/{
        var x1 = new F();
        var x2 = new F() { Field = 2 };
        var x3 = new F() { Property1 = """""""" };
        var x4 = new F() { Property1 = """""""", Field = 2 };
        var x5 = new F() { Property2 = new B { Field = true } };

        var e1 = new F() { Property2 = 1 };
        var e2 = new F() { """""""" };
    }/*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (7 statements, 7 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: '{ ... }')
  Locals: Local_1: F x1
    Local_2: F x2
    Local_3: F x3
    Local_4: F x4
    Local_5: F x5
    Local_6: F e1
    Local_7: F e2
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var x1 = new F();')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x1 = new F();')
      Variables: Local_1: F x1
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F()')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var x2 = ne ... ield = 2 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x2 = ne ... ield = 2 };')
      Variables: Local_1: F x2
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F() { Field = 2 }')
          Member Initializers(1): IFieldInitializer (Field: System.Int32 F.Field) (OperationKind.FieldInitializerInCreation) (Syntax: 'Field = 2')
              ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var x3 = ne ... 1 = """""""" };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'var x3 = ne ... 1 = """""""" };')
      Variables: Local_1: F x3
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F, IsInvalid) (Syntax: 'new F() { P ... y1 = """""""" }')
          Member Initializers(1): IPropertyInitializer (Property: System.String F.Property1 { get; set; }) (OperationKind.PropertyInitializerInCreation) (Syntax: 'Property1 = """"')
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: """") (Syntax: '""""')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var x4 = ne ... ield = 2 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'var x4 = ne ... ield = 2 };')
      Variables: Local_1: F x4
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F, IsInvalid) (Syntax: 'new F() { P ... Field = 2 }')
          Member Initializers(2): IPropertyInitializer (Property: System.String F.Property1 { get; set; }) (OperationKind.PropertyInitializerInCreation) (Syntax: 'Property1 = """"')
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: """") (Syntax: '""""')
            IFieldInitializer (Field: System.Int32 F.Field) (OperationKind.FieldInitializerInCreation) (Syntax: 'Field = 2')
              ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var x5 = ne ... = true } };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var x5 = ne ... = true } };')
      Variables: Local_1: F x5
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F() { P ...  = true } }')
          Member Initializers(1): IPropertyInitializer (Property: B F.Property2 { get; set; }) (OperationKind.PropertyInitializerInCreation) (Syntax: 'Property2 = ... ld = true }')
              IObjectCreationExpression (Constructor: B..ctor()) (OperationKind.ObjectCreationExpression, Type: B) (Syntax: 'new B { Field = true }')
                Member Initializers(1): IFieldInitializer (Field: System.Boolean B.Field) (OperationKind.FieldInitializerInCreation) (Syntax: 'Field = true')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var e1 = ne ... rty2 = 1 };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'var e1 = ne ... rty2 = 1 };')
      Variables: Local_1: F e1
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F, IsInvalid) (Syntax: 'new F() { P ... erty2 = 1 }')
          Member Initializers(1): IPropertyInitializer (Property: B F.Property2 { get; set; }) (OperationKind.PropertyInitializerInCreation, IsInvalid) (Syntax: 'Property2 = 1')
              IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: B, IsInvalid) (Syntax: '1')
                ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var e2 = ne ... ) { """""""" };')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'var e2 = ne ... ) { """""""" };')
      Variables: Local_1: F e2
      Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F, IsInvalid) (Syntax: 'new F() { """""""" }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1003: Syntax error, ',' expected
                //         var x3 = new F() { Property1 = """" };
                Diagnostic(ErrorCode.ERR_SyntaxError, @"""""").WithArguments(",", "").WithLocation(20, 42),
                // CS1003: Syntax error, ',' expected
                //         var x4 = new F() { Property1 = """", Field = 2 };
                Diagnostic(ErrorCode.ERR_SyntaxError, @"""""").WithArguments(",", "").WithLocation(21, 42),
                // CS1003: Syntax error, ',' expected
                //         var e2 = new F() { """" };
                Diagnostic(ErrorCode.ERR_SyntaxError, @"""""").WithArguments(",", "").WithLocation(25, 30),
                // CS0747: Invalid initializer member declarator
                //         var x3 = new F() { Property1 = """" };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, @"""""").WithLocation(20, 42),
                // CS0747: Invalid initializer member declarator
                //         var x4 = new F() { Property1 = """", Field = 2 };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, @"""""").WithLocation(21, 42),
                // CS0029: Cannot implicitly convert type 'int' to 'B'
                //         var e1 = new F() { Property2 = 1 };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "B").WithLocation(24, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}