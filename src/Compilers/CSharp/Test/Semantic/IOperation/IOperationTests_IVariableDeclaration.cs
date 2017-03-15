// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void SimpleVariableDeclaration()
        {
            string source = @"
class Program
{
    int P1 { get; set; }
    static void Main(string[] args)
    {
        /*<bind>*/int i1;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void MultipleDeclarations()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1, i2;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementDeclaration()
        {
            string source = @"
class Program
{
    int i1;
    static void Main(string[] args)
    {
        var reference = new Program();
        unsafe
        {
            fixed (/*<bind>*/int* p = &reference.i1/*</bind>*/)
            {

            }
        }
    }
}
";
            string expectedOperationTree = @"
IFixedStatement (OperationKind.FixedStatement)
  IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
    IVariableDeclaration: System.Int32* p (OperationKind.VariableDeclaration)
      Initializer: IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*)
          IFieldReferenceExpression: System.Int32 Program.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32)
            Instance Receiver: ILocalReferenceExpression: reference (OperationKind.LocalReferenceExpression, Type: Program)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementMultipleDeclaration()
        {
            string source = @"
class Program
{
    int i1, i2;
    static void Main(string[] args)
    {
        var reference = new Program();
        unsafe
        {
            fixed (/*<bind>*/int* p1 = &reference.i1, p2 = &reference.i2/*</bind>*/)
            {

            }
        }
    }
}
";
            string expectedOperationTree = @"
IFixedStatement (OperationKind.FixedStatement)
  IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
    IVariableDeclaration: System.Int32* p1 (OperationKind.VariableDeclaration)
      Initializer: IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*)
          IFieldReferenceExpression: System.Int32 Program.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32)
            Instance Receiver: ILocalReferenceExpression: reference (OperationKind.LocalReferenceExpression, Type: Program)
    IVariableDeclaration: System.Int32* p2 (OperationKind.VariableDeclaration)
      Initializer: IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*)
          IFieldReferenceExpression: System.Int32 Program.i2 (OperationKind.FieldReferenceExpression, Type: System.Int32)
            Instance Receiver: ILocalReferenceExpression: reference (OperationKind.LocalReferenceExpression, Type: Program)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementDeclaration()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 = new Program()/*</bind>*/)
        {
        }
    }

    public void Dispose() {}
}
";
            string expectedOperationTree = @"
IUsingStatement (OperationKind.UsingStatement)
  IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
    IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration)
      Initializer: IObjectCreationExpression (Constructor: Program..ctor()) (OperationKind.ObjectCreationExpression, Type: Program)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementMultipleDeclarations()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 = new Program(), p2 = new Program()/*</bind>*/)
        {
        }
    }

    public void Dispose() {}
}
";
            string expectedOperationTree = @"
IUsingStatement (OperationKind.UsingStatement)
  IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
    IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration)
      Initializer: IObjectCreationExpression (Constructor: Program..ctor()) (OperationKind.ObjectCreationExpression, Type: Program)
    IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration)
      Initializer: IObjectCreationExpression (Constructor: Program..ctor()) (OperationKind.ObjectCreationExpression, Type: Program)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopDeclaration()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i = 0/*</bind>*/; i < 0; i++)
        {

        }
    }
}
";
            string expectedOperationTree = @"
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopMultipleDeclarations()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i = 0, j = 0/*</bind>*/; i < 0; i++)
        {
        }
    }
}
";
            string expectedOperationTree = @"
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  Local_1: System.Int32 i
  Local_2: System.Int32 j
  Before: IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclaration()
        {
            string source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/const int i1 = 1;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMultipleDeclarations()
        {
            string source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/const int i1 = 1, i2 = 2;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
";
            VerifyOperationTreeForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree);
        }
    }
}
