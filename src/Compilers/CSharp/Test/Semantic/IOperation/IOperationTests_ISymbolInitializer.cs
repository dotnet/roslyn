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
        [CompilerTrait(CompilerFeature.IOperation)]
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IFieldInitializer (Field: System.Int32 C.s1) (OperationKind.FieldInitializer) (Syntax: '= 1')
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0414: The field 'C.s1' is assigned but its value is never used
                //     static int s1 /*<bind>*/= 1/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "s1").WithArguments("C.s1").WithLocation(4, 16)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IFieldInitializer (Field: System.Int32 C.i2) (OperationKind.FieldInitializer) (Syntax: '= 2')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IPropertyInitializer (Property: System.Int32 C.P1 { get; }) (OperationKind.PropertyInitializer) (Syntax: '= 1')
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IParameterInitializer (Parameter: [System.Int32 p1 = 0]) (OperationKind.ParameterInitializer) (Syntax: '= 0')
  ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1751: Cannot specify a default value for a parameter array
                //     void M(int p1 /*<bind>*/= 0/*</bind>*/, params int[] p2 = null) { }
                Diagnostic(ErrorCode.ERR_DefaultValueForParamsParameter, "params").WithLocation(4, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IParameterInitializer (Parameter: params System.Int32[] p2) (OperationKind.ParameterInitializer) (Syntax: '= null')
  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32[], Constant: null) (Syntax: 'null')
    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
    Operand: ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1751: Cannot specify a default value for a parameter array
                //     void M(int p1 = 0, params int[] p2 /*<bind>*/= null/*</bind>*/) { }
                Diagnostic(ErrorCode.ERR_DefaultValueForParamsParameter, "params").WithLocation(4, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IFieldInitializer (Field: System.Int32 C.s1) (OperationKind.FieldInitializer) (Syntax: '= 1 + F()')
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '1 + F()')
    Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: IInvocationExpression (System.Int32 C.F()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'F()')
        Instance Receiver: null
        Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IFieldInitializer (Field: System.Int32 C.s1) (OperationKind.FieldInitializer) (Syntax: '= 1 + F()')
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '1 + F()')
    Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: IInvocationExpression (System.Int32 C.F()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'F()')
        Instance Receiver: null
        Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IFieldInitializer (Field: System.Int32 C.i1) (OperationKind.FieldInitializer) (Syntax: '= 1 + F()')
  IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '1 + F()')
    Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    Right: IInvocationExpression (System.Int32 C.F()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'F()')
        Instance Receiver: null
        Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IFieldInitializer (Field: System.Int32 C.s1) (OperationKind.FieldInitializer) (Syntax: '= 1')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IFieldInitializer (Field: System.Int32 C.i2) (OperationKind.FieldInitializer) (Syntax: '= 2')
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

        [CompilerTrait(CompilerFeature.IOperation)]
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
IFieldInitializer (Field: System.Action C.e) (OperationKind.FieldInitializer) (Syntax: '= MakeAction(1)')
  IInvocationExpression (System.Action C.MakeAction(System.Int32 x)) (OperationKind.InvocationExpression, Type: System.Action) (Syntax: 'MakeAction(1)')
    Instance Receiver: null
    Arguments(1):
        IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IFieldInitializer (Field: System.Action C.f) (OperationKind.FieldInitializer) (Syntax: '= MakeAction(2)')
  IInvocationExpression (System.Action C.MakeAction(System.Int32 x)) (OperationKind.InvocationExpression, Type: System.Action) (Syntax: 'MakeAction(2)')
    Instance Receiver: null
    Arguments(1):
        IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '2')
          ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<EqualsValueClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
