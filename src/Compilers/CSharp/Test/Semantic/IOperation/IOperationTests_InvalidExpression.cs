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
        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidInvocationExpression_BadReceiver()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/Console.WriteLine2()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( ? ?.()) (OperationKind.InvocationExpression, Type: ?, IsInvalid)
  Instance Receiver: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
      IOperation:  (OperationKind.None)
";
            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidInvocationExpression_OverloadResolutionFailureBadArgument()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/F(string.Empty)/*</bind>*/;
    }

    void F(int x)
    {
    }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void Program.F(System.Int32 x)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid)
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program)
  IArgument (Matching Parameter: x) (OperationKind.Argument)
    IFieldReferenceExpression: System.String System.String.Empty (Static) (OperationKind.FieldReferenceExpression, Type: System.String)
";
            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8813"), WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidInvocationExpression_OverloadResolutionFailureExtraArgument()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/F(string.Empty)/*</bind>*/;
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void Program.F(System.Int32 x)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid)
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: Program)
  IArgument (Matching Parameter: x) (OperationKind.Argument)
    IFieldReferenceExpression: System.String System.String.Empty (Static) (OperationKind.FieldReferenceExpression, Type: System.String)
";
            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidFieldReferenceExpression()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new Program();
        var y /*<bind>*/= x.MissingField/*</bind>*/;
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: ? y (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
";
            VerifyOperationTreeForTest<EqualsValueClauseSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidConversionExpression_ImplicitCast()
        {
            string source = @"
using System;

class Program
{
    int i1;
    static void Main(string[] args)
    {
        var x = new Program();
        string y /*<bind>*/= x.i1/*</bind>*/;
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.String y (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.String, IsInvalid)
        IFieldReferenceExpression: System.Int32 Program.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32)
          Instance Receiver: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
";
            VerifyOperationTreeForTest<EqualsValueClauseSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidConversionExpression_ExplicitCast()
        {
            string source = @"
using System;

class Program
{
    int i1;
    static void Main(string[] args)
    {
        var x = new Program();
        Program y /*<bind>*/= (Program)x.i1/*</bind>*/;
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: Program y (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Explicit) (OperationKind.ConversionExpression, Type: Program, IsInvalid)
        IFieldReferenceExpression: System.Int32 Program.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32)
          Instance Receiver: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
";
            VerifyOperationTreeForTest<EqualsValueClauseSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18056"), WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidUnaryExpression()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new Program();
        Console.Write(/*<bind>*/++x/*</bind>*/);
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IIncrementExpression (UnaryOperandKind.Invalid) (OperationKind.IncrementExpression, Type: System.Object, IsInvalid)
  Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Object, Constant: 1)
";
            VerifyOperationTreeForTest<PrefixUnaryExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidBinaryExpression()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new Program();
        Console.Write(/*<bind>*/x + (y * args.Length)/*</bind>*/);
    }

    void F()
    {
    }
}
";
            string expectedOperationTree = @"
IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid)
  Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
  Right: IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid)
      Left: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
      Right: IPropertyReferenceExpression: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32)
          Instance Receiver: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[])
";
            VerifyOperationTreeForTest<BinaryExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18057"), WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidLambdaBinding_UnboundLambda()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x /*<bind>*/= () => F()/*</bind>*/;
    }

    static void F()
    {
    }
}
";

            string expectedLambdaOperationTree = @"
ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void Program.F()) (OperationKind.InvocationExpression, Type: System.Void)
";
            VerifyOperationTreeForTest<ParenthesizedLambdaExpressionSyntax>(source, expectedLambdaOperationTree);

            string expectedEqualsValueOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: var x (OperationKind.VariableDeclaration)
    Initializer: IUnboundLambdaExpression (OperationKind.UnboundLambdaExpression, Type: null)
        ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IExpressionStatement (OperationKind.ExpressionStatement)
              IInvocationExpression (static void Program.F()) (OperationKind.InvocationExpression, Type: System.Void)
";
            VerifyOperationTreeForTest<EqualsValueClauseSyntax>(source, expectedEqualsValueOperationTree);
        }

        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidFieldInitializer()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    int x /*<bind>*/= Program/*</bind>*/;
    static void Main(string[] args)
    {
        var x = new Program() { x = Program };
    }
}
";
            string expectedOperationTree = @"
IFieldInitializer (Field: System.Int32 Program.x) (OperationKind.FieldInitializerAtDeclaration, IsInvalid)
  IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
    IInvalidExpression (OperationKind.InvalidExpression, Type: Program, IsInvalid)
      IOperation:  (OperationKind.None)
";
            VerifyOperationTreeForTest<EqualsValueClauseSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidArrayInitializer()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var x = new int[2, 2] /*<bind>*/{ { { 1, 1 } }, { 2, 2 } }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayInitializer (2 elements) (OperationKind.ArrayInitializer, IsInvalid)
  IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid)
    IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
        IArrayInitializer (2 elements) (OperationKind.ArrayInitializer, IsInvalid)
          IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
            ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
          IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
            ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArrayInitializer (2 elements) (OperationKind.ArrayInitializer)
    ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
    ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
";
            VerifyOperationTreeForTest<InitializerExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidArrayCreation()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var x = /*<bind>*/new X[Program] { { 1 } }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IArrayCreationExpression (Dimension sizes: 1, Element Type: X) (OperationKind.ArrayCreationExpression, Type: X[], IsInvalid)
  IInvalidExpression (OperationKind.InvalidExpression, Type: Program, IsInvalid)
    IOperation:  (OperationKind.None)
  IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid)
    IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: X, IsInvalid)
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
        IArrayInitializer (1 elements) (OperationKind.ArrayInitializer, IsInvalid)
          IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
            ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<ArrayCreationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18059"), WorkItem(17598, "https://github.com/dotnet/roslyn/issues/17598")]
        public void InvalidParameterDefaultValueInitializer()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static int M() { return 0; }
    void F(int p /*<bind>*/= M()/*</bind>*/)
    {
    }
}
";
            string expectedOperationTree = @"
IParameterInitializer (Parameter: [System.Int32 p = default(System.Int32)]) (OperationKind.ParameterInitializerAtDeclaration, IsInvalid)
  IInvocationExpression (static System.Int32 Program.M()) (OperationKind.InvocationExpression, Type: System.Int32, IsInvalid)
";
            VerifyOperationTreeForTest<EqualsValueClauseSyntax>(source, expectedOperationTree);
        }
    }
}