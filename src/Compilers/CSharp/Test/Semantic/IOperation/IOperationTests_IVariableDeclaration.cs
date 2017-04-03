// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        #region Variable Declarations

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void SingleVariableDeclaration()
        {
            string source = @"
class Program
{
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
        public void SingleVariableDeclarationWithInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1 = 1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void SingleVariableDeclarationWithInvalidInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1 = /*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
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
        public void MultipleDeclarationsWithInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1 = 2, i2 = 2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }



        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void MultipleDeclarationsWithInvalidInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i1 = , i2 = 2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void InvalidMultipleVariableDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i,/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
  IVariableDeclaration: System.Int32  (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void SingleVariableDeclarationExpressionInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i = GetInt()/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void MutlipleVariableDeclarationsExpressionInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int i = GetInt(), j = GetInt()/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32)
  IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void SingleVariableDeclarationLocalReferenceInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 1;
        /*<bind>*/int i1 = i/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void MultipleDeclarationsLocalReferenceInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 1;
        /*<bind>*/int i1 = i, i2 = i1/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void InvalidArrayDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int[2, 3] a/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32[,] a (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void InvalidArrayMultipleDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int[2, 3] a, b/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32[,] a (OperationKind.VariableDeclaration)
  IVariableDeclaration: System.Int32[,] b (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        #endregion

        #region Fixed Statements

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32* p (OperationKind.VariableDeclaration)
    Initializer: IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*)
        IFieldReferenceExpression: System.Int32 Program.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32)
          Instance Receiver: ILocalReferenceExpression: reference (OperationKind.LocalReferenceExpression, Type: Program)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32* p1 (OperationKind.VariableDeclaration)
    Initializer: IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*)
        IFieldReferenceExpression: System.Int32 Program.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32)
          Instance Receiver: ILocalReferenceExpression: reference (OperationKind.LocalReferenceExpression, Type: Program)
  IVariableDeclaration: System.Int32* p2 (OperationKind.VariableDeclaration)
    Initializer: IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*)
        IFieldReferenceExpression: System.Int32 Program.i2 (OperationKind.FieldReferenceExpression, Type: System.Int32)
          Instance Receiver: ILocalReferenceExpression: reference (OperationKind.LocalReferenceExpression, Type: Program)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementInvalidAssignment()
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
            /*<bind>*/fixed (/*<bind>*/int* p = /*</bind>*/)
            {

            }/*</bind>*/
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32* p (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32*, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
";
            VerifyOperationTreeForTest<FixedStatementSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementMultipleDeclarationsInvalidInitializers()
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
            fixed (/*<bind>*/int* p1 = , p2 = /*</bind>*/)
            {

            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32* p1 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32*, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IVariableDeclaration: System.Int32* p2 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32*, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }



        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementNoInitializer()
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
            /*<bind>*/fixed (/*<bind>*/int* p/*</bind>*/)
            {

            }/*</bind>*/
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32* p (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<FixedStatementSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementMultipleDeclarationsNoInitializers()
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
            fixed (/*<bind>*/int* p1, p2/*</bind>*/)
            {

            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32* p1 (OperationKind.VariableDeclaration)
  IVariableDeclaration: System.Int32* p2 (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18061"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void FixedStatementInvalidMulipleDeclarations()
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
            fixed (/*<bind>*/int* p1 = &reference.i1,/*</bind>*/)
            {

            }
        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32* p1 (OperationKind.VariableDeclaration)
    Initializer: IAddressOfExpression (OperationKind.AddressOfExpression, Type: System.Int32*)
        IFieldReferenceExpression: System.Int32 Program.i1 (OperationKind.FieldReferenceExpression, Type: System.Int32)
          Instance Receiver: ILocalReferenceExpression: reference (OperationKind.LocalReferenceExpression, Type: Program)
  IVariableDeclaration: System.Int32*  (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        #endregion

        #region Using Statements

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Program..ctor()) (OperationKind.ObjectCreationExpression, Type: Program)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
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
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Program..ctor()) (OperationKind.ObjectCreationExpression, Type: Program)
  IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Program..ctor()) (OperationKind.ObjectCreationExpression, Type: Program)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementInvalidInitializer()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 =/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Program, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementMultipleDeclarationsInvalidInitializers()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 =, p2 =/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Program, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: Program, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }



        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementNoInitializer()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementMultipleDeclarationsNoInitializers()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1, p2/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration)
  IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementInvalidMultipleDeclarations()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 = new Program(),/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration)
    Initializer: IObjectCreationExpression (Constructor: Program..ctor()) (OperationKind.ObjectCreationExpression, Type: Program)
  IVariableDeclaration: Program  (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementExpressionInitializer()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 = GetProgram()/*</bind>*/)
        {
        }
    }

    static Program GetProgram() => new Program();

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static Program Program.GetProgram()) (OperationKind.InvocationExpression, Type: Program)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementMultipleDeclarationsExpressionInitializers()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        using (/*<bind>*/Program p1 = GetProgram(), p2 = GetProgram()/*</bind>*/)
        {
        }
    }

    static Program GetProgram() => new Program();

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: Program p1 (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static Program Program.GetProgram()) (OperationKind.InvocationExpression, Type: Program)
  IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static Program Program.GetProgram()) (OperationKind.InvocationExpression, Type: Program)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementLocalReferenceInitializer()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        Program p1 = new Program();
        using (/*<bind>*/Program p2 = p1/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: Program)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18062"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void UsingStatementMultipleDeclarationsLocalReferenceInitializers()
        {
            string source = @"
using System;

class Program : IDisposable
{
    static void Main(string[] args)
    {
        Program p1 = new Program();
        using (/*<bind>*/Program p2 = p1, p3 = p1/*</bind>*/)
        {
        }
    }

    public void Dispose() { }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: Program p2 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: Program)
  IVariableDeclaration: Program p3 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: p1 (OperationKind.LocalReferenceExpression, Type: Program)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        #endregion

        #region For Loops

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopDeclaration()
        {
            string source = @"
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
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopMultipleDeclarations()
        {
            string source = @"
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
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
    Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopInvalidInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i =/*</bind>*/; i < 0; i++)
        {

        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopMultipleDeclarationsInvalidInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i =, j =/*</bind>*/; i < 0; i++)
        {

        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopNoInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i/*</bind>*/; i < 0; i++)
        {

        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopMultipleDeclarationsNoInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i, j/*</bind>*/; i < 0; i++)
        {

        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
  IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }




        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopInvalidMultipleDeclarations()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i =,/*</bind>*/; i < 0; i++)
        {

        }
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IVariableDeclaration: System.Int32  (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopExpressionInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i = GetInt()/*</bind>*/; i < 0; i++)
        {

        }
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18063"), WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ForLoopMultipleDeclarationsExpressionInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        for (/*<bind>*/int i = GetInt(), j = GetInt()/*</bind>*/; i < 0; i++)
        {

        }
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32)
  IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }


        #endregion

        #region Const Local Declarations

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclaration()
        {
            string source = @"
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

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclarationInvalidInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1 = /*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMultipleDeclarationsInvalidInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1 = , i2 = /*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration, IsInvalid)
    Initializer: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclarationNoInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMutlipleDeclarationsNoInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1, i2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalInvalidMultipleDeclarations()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1,/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
  IVariableDeclaration: System.Int32  (OperationKind.VariableDeclaration)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclarationExpressionInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1 = GetInt()/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMultipleDeclarationsExpressionInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const /*<bind>*/int i1 = GetInt(), i2 = GetInt()/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32)
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration)
    Initializer: IInvocationExpression (static System.Int32 Program.GetInt()) (OperationKind.InvocationExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalDeclarationLocalReferenceInitializer()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const int i = 1;
        const /*<bind>*/int i1 = i/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17599, "https://github.com/dotnet/roslyn/issues/17599")]
        public void ConstLocalMultipleDeclarationsLocalReferenceInitializers()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        const int i = 1;
        const /*<bind>*/int i1 = i, i2 = i1/*</bind>*/;
    }

    static int GetInt() => 1;
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
  IVariableDeclaration: System.Int32 i1 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1)
  IVariableDeclaration: System.Int32 i2 (OperationKind.VariableDeclaration)
    Initializer: ILocalReferenceExpression: i1 (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<VariableDeclarationSyntax>(source, expectedOperationTree);
        }

        #endregion
    }
}
