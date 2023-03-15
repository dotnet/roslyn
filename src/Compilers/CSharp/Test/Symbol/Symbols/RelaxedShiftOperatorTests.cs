// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class RelaxedShiftOperatorTests : CSharpTestBase
    {
        [Theory]
        [InlineData("<<")]
        [InlineData(">>")]
        [InlineData(">>>")]
        public void Relaxed_01(string op)
        {
            var source0 = @"
public class C1
{
    public static C1 operator " + op + @"(C1 x, C1 y)
    {
        System.Console.WriteLine(""" + op + @""");
        return x;
    }
}
";

            var source1 =
@"
class C
{
    static void Main()
    {
        Test1(new C1(), new C1());
    }

    static C1 Test1(C1 x, C1 y) => x " + op + @" y; 
}
";
            var compilation1 = CreateCompilation(source0 + source1, options: TestOptions.DebugExe,
                                                 parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation1, expectedOutput: op).VerifyDiagnostics();

            var compilation0 = CreateCompilation(source0, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);

            var compilation2 = CreateCompilation(source1, options: TestOptions.DebugExe, references: new[] { compilation0.ToMetadataReference() },
                                                 parseOptions: TestOptions.RegularPreview);

            CompileAndVerify(compilation2, expectedOutput: op).VerifyDiagnostics();

            var compilation3 = CreateCompilation(source1, options: TestOptions.DebugExe, references: new[] { compilation0.EmitToImageReference() },
                                                 parseOptions: TestOptions.RegularPreview);
            CompileAndVerify(compilation3, expectedOutput: op).VerifyDiagnostics();
        }

        [Theory]
        [InlineData("<<")]
        [InlineData(">>")]
        [InlineData(">>>")]
        public void OverloadResolution_01(string op)
        {
            var source1 = @"
public class C1
{
    public static C1 operator " + op + @"(C1 x, C2 y)
    {
        return x;
    }
}

public class C2
{
    public static C1 operator " + op + @"(C1 x, C2 y)
    {
        return x;
    }
}

class C
{
    static C1 Test1(C1 x, C2 y) => x " + op + @" y; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (12,31): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type
                //     public static C1 operator >>>(C1 x, C2 y)
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, op).WithLocation(12, 31)
                );

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var shift = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            Assert.Equal("x " + op + " y", shift.ToString());
            Assert.Equal("C1.operator " + op + "(C1, C2)", model.GetSymbolInfo(shift).Symbol.ToDisplayString());
        }

        [Theory]
        [InlineData("<<")]
        [InlineData(">>")]
        [InlineData(">>>")]
        public void OverloadResolution_02(string op)
        {
            var source1 = @"
public interface C1
{
    public static C1 operator " + op + @"(C1 x, C2 y)
    {
        return x;
    }
}

public interface C2
{
    public static C1 operator " + op + @"(C1 x, C2 y)
    {
        return x;
    }
}

class C
{
    static C1 Test1(C1 x, C2 y) => x " + op + @" y; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetCoreApp,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (12,31): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type
                //     public static C1 operator >>>(C1 x, C2 y)
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, op).WithLocation(12, 31)
                );

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var shift = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            Assert.Equal("x " + op + " y", shift.ToString());
            Assert.Equal("C1.operator " + op + "(C1, C2)", model.GetSymbolInfo(shift).Symbol.ToDisplayString());
        }

        [Theory]
        [InlineData("<<")]
        [InlineData(">>")]
        [InlineData(">>>")]
        public void OverloadResolution_03(string op)
        {
            var source1 = @"
public interface C1
{
    public static C1 operator " + op + @"(C1 x, C2 y)
    {
        return x;
    }
}

public interface C2
{
    public static C1 operator " + op + @"(C1 x, C2 y)
    {
        return x;
    }
}

class C
{
    static C1 Test1<T>(T x, C2 y) where T : C1 => x " + op + @" y; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetCoreApp,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (12,31): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type
                //     public static C1 operator >>>(C1 x, C2 y)
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, op).WithLocation(12, 31)
                );

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var shift = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            Assert.Equal("x " + op + " y", shift.ToString());
            Assert.Equal("C1.operator " + op + "(C1, C2)", model.GetSymbolInfo(shift).Symbol.ToDisplayString());
        }

        [Theory]
        [InlineData("<<")]
        [InlineData(">>")]
        [InlineData(">>>")]
        public void OverloadResolution_04(string op)
        {
            var source1 = @"
public interface C1
{
    public static C1 operator " + op + @"(C1 x, C2 y)
    {
        return x;
    }
}

public interface C2
{
    public static C1 operator " + op + @"(C1 x, C2 y)
    {
        return x;
    }
}

class C
{
    static C1 Test1<T>(C1 x, T y) where T : C2 => x " + op + @" y; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetCoreApp,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (12,31): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type
                //     public static C1 operator >>>(C1 x, C2 y)
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, op).WithLocation(12, 31)
                );

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var shift = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            Assert.Equal("x " + op + " y", shift.ToString());
            Assert.Equal("C1.operator " + op + "(C1, C2)", model.GetSymbolInfo(shift).Symbol.ToDisplayString());
        }

        [Theory]
        [InlineData("<<")]
        [InlineData(">>")]
        [InlineData(">>>")]
        public void OverloadResolution_05(string op)
        {
            var source1 = @"
public interface C1
{
    public static C1 operator " + op + @"(C1 x, C2 y)
    {
        return x;
    }
}

public class C2
{
    public static C1 operator " + op + @"(C1 x, C2 y)
    {
        return x;
    }
}

class C
{
    static C1 Test1(C1 x, C2 y) => x " + op + @" y; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetCoreApp,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (12,31): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type
                //     public static C1 operator >>>(C1 x, C2 y)
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, op).WithLocation(12, 31)
                );

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var shift = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            Assert.Equal("x " + op + " y", shift.ToString());
            Assert.Equal("C1.operator " + op + "(C1, C2)", model.GetSymbolInfo(shift).Symbol.ToDisplayString());
        }

        [Theory]
        [InlineData("<<")]
        [InlineData(">>")]
        [InlineData(">>>")]
        public void OverloadResolution_06(string op)
        {
            var source1 = @"
public class C1
{
    public static C1 operator " + op + @"(C1 x, C2 y)
    {
        return x;
    }
}

public interface C2
{
    public static C1 operator " + op + @"(C1 x, C2 y)
    {
        return x;
    }
}

class C
{
    static C1 Test1(C1 x, C2 y) => x " + op + @" y; 
}
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetCoreApp,
                                                 parseOptions: TestOptions.RegularPreview);
            compilation1.VerifyDiagnostics(
                // (12,31): error CS0564: The first operand of an overloaded shift operator must have the same type as the containing type
                //     public static C1 operator >>>(C1 x, C2 y)
                Diagnostic(ErrorCode.ERR_BadShiftOperatorSignature, op).WithLocation(12, 31)
                );

            var tree = compilation1.SyntaxTrees.Single();
            var model = compilation1.GetSemanticModel(tree);
            var shift = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            Assert.Equal("x " + op + " y", shift.ToString());
            Assert.Equal("C1.operator " + op + "(C1, C2)", model.GetSymbolInfo(shift).Symbol.ToDisplayString());
        }
    }
}
