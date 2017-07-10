// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NoConversions()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        (int, int) t = /*<bind>*/(1, 2)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
  Elements(2): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NoConversions_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        /*<bind>*/(int, int) t = (1, 2)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: '(int, int)  ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(int, int)  ... *</bind>*/;')
    Variables: Local_1: (System.Int32, System.Int32) t
    Initializer: ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
        Elements(2): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_ImplicitConversions()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        (uint, uint) t = /*<bind>*/(1, 2)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleExpression (OperationKind.TupleExpression, Type: (System.UInt32, System.UInt32)) (Syntax: '(1, 2)')
  Elements(2): IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.UInt32, Constant: 1) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.UInt32, Constant: 2) (Syntax: '2')
      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_ImplicitConversions_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        /*<bind>*/(uint, uint) t = (1, 2)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: '(uint, uint ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(uint, uint ... *</bind>*/;')
    Variables: Local_1: (System.UInt32, System.UInt32) t
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: (System.UInt32, System.UInt32)) (Syntax: '(1, 2)')
        ITupleExpression (OperationKind.TupleExpression, Type: (System.UInt32, System.UInt32)) (Syntax: '(1, 2)')
          Elements(2): IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.UInt32, Constant: 1) (Syntax: '1')
              ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.UInt32, Constant: 2) (Syntax: '2')
              ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_ImplicitConversionFromNull()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        (uint, string) t = /*<bind>*/(1, null)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleExpression (OperationKind.TupleExpression, Type: (System.UInt32, System.String)) (Syntax: '(1, null)')
  Elements(2): IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.UInt32, Constant: 1) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
      ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_ImplicitConversionFromNull_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        /*<bind>*/(uint, string) t = (1, null)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: '(uint, stri ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(uint, stri ... *</bind>*/;')
    Variables: Local_1: (System.UInt32, System.String) t
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: (System.UInt32, System.String)) (Syntax: '(1, null)')
        ITupleExpression (OperationKind.TupleExpression, Type: (System.UInt32, System.String)) (Syntax: '(1, null)')
          Elements(2): IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.UInt32, Constant: 1) (Syntax: '1')
              ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
              ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NamedElements()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        var t = /*<bind>*/(A: 1, B: 2)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32 A, System.Int32 B)) (Syntax: '(A: 1, B: 2)')
  Elements(2): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NamedElements_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        /*<bind>*/var t = (A: 1, B: 2)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var t = (A: ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var t = (A: ... *</bind>*/;')
    Variables: Local_1: (System.Int32 A, System.Int32 B) t
    Initializer: ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32 A, System.Int32 B)) (Syntax: '(A: 1, B: 2)')
        Elements(2): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NamedElementsInTupleType()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        (int A, int B) t = /*<bind>*/(1, 2)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
  Elements(2): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NamedElementsInTupleType_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        /*<bind>*/(int A, int B) t = (1, 2)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: '(int A, int ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(int A, int ... *</bind>*/;')
    Variables: Local_1: (System.Int32 A, System.Int32 B) t
    Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: (System.Int32 A, System.Int32 B)) (Syntax: '(1, 2)')
        ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32, System.Int32)) (Syntax: '(1, 2)')
          Elements(2): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NamedElementsAndImplicitConversions()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        (short, string) t = /*<bind>*/(A: 1, B: null)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleExpression (OperationKind.TupleExpression, Type: (System.Int16 A, System.String B)) (Syntax: '(A: 1, B: null)')
  Elements(2): IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int16, Constant: 1) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
      ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8123: The tuple element name 'A' is ignored because a different name or no name is specified by the target type '(short, string)'.
                //         (short, string) t = /*<bind>*/(A: 1, B: null)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "A: 1").WithArguments("A", "(short, string)").WithLocation(8, 40),
                // CS8123: The tuple element name 'B' is ignored because a different name or no name is specified by the target type '(short, string)'.
                //         (short, string) t = /*<bind>*/(A: 1, B: null)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "B: null").WithArguments("B", "(short, string)").WithLocation(8, 46)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_NamedElementsAndImplicitConversions_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    static void Main()
    {
        /*<bind>*/(short, string) t = (A: 1, B: null)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: '(short, str ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(short, str ... *</bind>*/;')
    Variables: Local_1: (System.Int16, System.String) t
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: (System.Int16, System.String)) (Syntax: '(A: 1, B: null)')
        ITupleExpression (OperationKind.TupleExpression, Type: (System.Int16 A, System.String B)) (Syntax: '(A: 1, B: null)')
          Elements(2): IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int16, Constant: 1) (Syntax: '1')
              ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
              ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS8123: The tuple element name 'A' is ignored because a different name or no name is specified by the target type '(short, string)'.
                //         /*<bind>*/(short, string) t = (A: 1, B: null)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "A: 1").WithArguments("A", "(short, string)").WithLocation(8, 40),
                // CS8123: The tuple element name 'B' is ignored because a different name or no name is specified by the target type '(short, string)'.
                //         /*<bind>*/(short, string) t = (A: 1, B: null)/*</bind>*/;
                Diagnostic(ErrorCode.WRN_TupleLiteralNameMismatch, "B: null").WithArguments("B", "(short, string)").WithLocation(8, 46)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_UserDefinedConversionsForArguments()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C(int value)
    {
        return new C(value);
    }

    public static implicit operator short(C c)
    {
        return (short)c._x;
    }

    public static implicit operator string(C c)
    {
        return c._x.ToString();
    }

    public void M(C c1)
    {
        (short, string) t = /*<bind>*/(new C(0), c1)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleExpression (OperationKind.TupleExpression, Type: (System.Int16, System.String c1)) (Syntax: '(new C(0), c1)')
  Elements(2): IConversionExpression (ConversionKind.OperatorMethod, Implicit) (OperatorMethod: System.Int16 C.op_Implicit(C c)) (OperationKind.ConversionExpression, Type: System.Int16) (Syntax: 'new C(0)')
      IObjectCreationExpression (Constructor: C..ctor(System.Int32 x)) (OperationKind.ObjectCreationExpression, Type: C) (Syntax: 'new C(0)')
        Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '0')
            ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IConversionExpression (ConversionKind.OperatorMethod, Implicit) (OperatorMethod: System.String C.op_Implicit(C c)) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'c1')
      IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_UserDefinedConversionsForArguments_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C(int value)
    {
        return new C(value);
    }

    public static implicit operator short(C c)
    {
        return (short)c._x;
    }

    public static implicit operator string(C c)
    {
        return c._x.ToString();
    }

    public void M(C c1)
    {
        /*<bind>*/(short, string) t = (new C(0), c1)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: '(short, str ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(short, str ... *</bind>*/;')
    Variables: Local_1: (System.Int16, System.String) t
    Initializer: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: (System.Int16, System.String)) (Syntax: '(new C(0), c1)')
        ITupleExpression (OperationKind.TupleExpression, Type: (System.Int16, System.String c1)) (Syntax: '(new C(0), c1)')
          Elements(2): IConversionExpression (ConversionKind.OperatorMethod, Implicit) (OperatorMethod: System.Int16 C.op_Implicit(C c)) (OperationKind.ConversionExpression, Type: System.Int16) (Syntax: 'new C(0)')
              IObjectCreationExpression (Constructor: C..ctor(System.Int32 x)) (OperationKind.ObjectCreationExpression, Type: C) (Syntax: 'new C(0)')
                Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '0')
                    ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
            IConversionExpression (ConversionKind.OperatorMethod, Implicit) (OperatorMethod: System.String C.op_Implicit(C c)) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 'c1')
              IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_UserDefinedConversionFromTupleExpression()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C((int, string) x)
    {
        return new C(x.Item1);
    }

    public static implicit operator (int, string) (C c)
    {
        return (c._x, c._x.ToString());
    }

    public void M(C c1)
    {
        C t = /*<bind>*/(0, null)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32, System.String)) (Syntax: '(0, null)')
  Elements(2): ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
      ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_UserDefinedConversionFromTupleExpression_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C((int, string) x)
    {
        return new C(x.Item1);
    }

    public static implicit operator (int, string) (C c)
    {
        return (c._x, c._x.ToString());
    }

    public void M(C c1)
    {
        /*<bind>*/C t = (0, null)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'C t = (0, n ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'C t = (0, n ... *</bind>*/;')
    Variables: Local_1: C t
    Initializer: IConversionExpression (ConversionKind.OperatorMethod, Implicit) (OperatorMethod: C C.op_Implicit((System.Int32, System.String) x)) (OperationKind.ConversionExpression, Type: C) (Syntax: '(0, null)')
        IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: (System.Int32, System.String)) (Syntax: '(0, null)')
          ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32, System.String)) (Syntax: '(0, null)')
            Elements(2): ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
              IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
                ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_UserDefinedConversionToTupleType()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C((int, string) x)
    {
        return new C(x.Item1);
    }

    public static implicit operator (int, string) (C c)
    {
        return (c._x, c._x.ToString());
    }

    public void M(C c1)
    {
        (int, string) t = /*<bind>*/c1/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_UserDefinedConversionToTupleType_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C((int, string) x)
    {
        return new C(x.Item1);
    }

    public static implicit operator (int, string) (C c)
    {
        return (c._x, c._x.ToString());
    }

    public void M(C c1)
    {
        /*<bind>*/(int, string) t = c1/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: '(int, strin ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: '(int, strin ... *</bind>*/;')
    Variables: Local_1: (System.Int32, System.String) t
    Initializer: IConversionExpression (ConversionKind.OperatorMethod, Implicit) (OperatorMethod: (System.Int32, System.String) C.op_Implicit(C c)) (OperationKind.ConversionExpression, Type: (System.Int32, System.String)) (Syntax: 'c1')
        IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_InvalidConversion()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C(int value)
    {
        return new C(value);
    }

    public static implicit operator int(C c)
    {
        return (short)c._x;
    }

    public static implicit operator string(C c)
    {
        return c._x.ToString();
    }

    public void M(C c1)
    {
        (short, string) t = /*<bind>*/(new C(0), c1)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
ITupleExpression (OperationKind.TupleExpression, Type: (C, C c1)) (Syntax: '(new C(0), c1)')
  Elements(2): IObjectCreationExpression (Constructor: C..ctor(System.Int32 x)) (OperationKind.ObjectCreationExpression, Type: C) (Syntax: 'new C(0)')
      Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '0')
          ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C' to 'short'
                //         (short, string) t = /*<bind>*/(new C(0), c1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C(0)").WithArguments("C", "short").WithLocation(29, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<TupleExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_InvalidConversion_ParentVariableDeclaration()
        {
            string source = @"
using System;

class C
{
    private readonly int _x;
    public C(int x)
    {
        _x = x;
    }

    public static implicit operator C(int value)
    {
        return new C(value);
    }

    public static implicit operator int(C c)
    {
        return (short)c._x;
    }

    public static implicit operator string(C c)
    {
        return c._x.ToString();
    }

    public void M(C c1)
    {
        /*<bind>*/(short, string) t = (new C(0), c1)/*</bind>*/;
        Console.WriteLine(t);
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: '(short, str ... *</bind>*/;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: '(short, str ... *</bind>*/;')
    Variables: Local_1: (System.Int16, System.String) t
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: (System.Int16, System.String), IsInvalid) (Syntax: '(new C(0), c1)')
        ITupleExpression (OperationKind.TupleExpression, Type: (C, C c1)) (Syntax: '(new C(0), c1)')
          Elements(2): IObjectCreationExpression (Constructor: C..ctor(System.Int32 x)) (OperationKind.ObjectCreationExpression, Type: C) (Syntax: 'new C(0)')
              Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '0')
                  ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
            IParameterReferenceExpression: c1 (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'C' to 'short'
                //         /*<bind>*/(short, string) t = (new C(0), c1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new C(0)").WithArguments("C", "short").WithLocation(29, 40)
            };

            VerifyOperationTreeAndDiagnosticsForTest<VariableDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_Deconstruction()
        {
            string source = @"
class Point
{
    public int X { get; }
    public int Y { get; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }
}

class Class1
{
    public void M()
    {
        /*<bind>*/var (x, y) = new Point(0, 1)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: 'var (x, y)  ... Point(0, 1)')
  Children(2): ITupleExpression (OperationKind.TupleExpression, Type: (System.Int32 x, System.Int32 y)) (Syntax: 'var (x, y)')
      Elements(2): ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
    IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: (System.Int32 x, System.Int32 y)) (Syntax: 'new Point(0, 1)')
      IObjectCreationExpression (Constructor: Point..ctor(System.Int32 x, System.Int32 y)) (OperationKind.ObjectCreationExpression, Type: Point) (Syntax: 'new Point(0, 1)')
        Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '0')
            ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '1')
            ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(10856, "https://github.com/dotnet/roslyn/issues/10856")]
        public void TupleExpression_DeconstructionWithConversion()
        {
            string source = @"
class Point
{
    public int X { get; }
    public int Y { get; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void Deconstruct(out uint x, out uint y)
    {
        x = X;
        y = Y;
    }
}

class Class1
{
    public void M()
    {
        /*<bind>*/(uint x, uint y) = new Point(0, 1)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IOperation:  (OperationKind.None) (Syntax: '(uint x, ui ... Point(0, 1)')
  Children(2): ITupleExpression (OperationKind.TupleExpression, Type: (System.UInt32 x, System.UInt32 y)) (Syntax: '(uint x, uint y)')
      Elements(2): ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.UInt32) (Syntax: 'uint x')
        ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.UInt32) (Syntax: 'uint y')
    IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: (System.UInt32 x, System.UInt32 y)) (Syntax: 'new Point(0, 1)')
      IObjectCreationExpression (Constructor: Point..ctor(System.Int32 x, System.Int32 y)) (OperationKind.ObjectCreationExpression, Type: Point) (Syntax: 'new Point(0, 1)')
        Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '0')
            ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '1')
            ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0266: Cannot implicitly convert type 'int' to 'uint'. An explicit conversion exists (are you missing a cast?)
                //         x = X;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "X").WithArguments("int", "uint").WithLocation(15, 13),
                // CS0266: Cannot implicitly convert type 'int' to 'uint'. An explicit conversion exists (are you missing a cast?)
                //         y = Y;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "Y").WithArguments("int", "uint").WithLocation(16, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
