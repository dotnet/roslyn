// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) null-checked variables and lambdas.
    /// </summary>
    public class NullCheckedVariableTests : CompilingTestBase
    {
        [Fact]
        public void NullCheckedMethodDeclaration()
        {
            var source = @"
delegate void Del(int x!, int y);
class C
{
    Del d = delegate(int k!, int j) { /* ... */ };
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (2, 19): error CS8713: Parameter 'int x!' can only have exclamation - point null checking in implementation methods.
                    // delegate void Del(int x!, int y);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "int x!").WithArguments("int x!").WithLocation(2, 19));
        }

        [Fact]
        public void NullCheckedAbstractMethod()
        {
            var source = @"
abstract class C
{
    abstract public int M(int x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4, 27): error CS8713: Parameter 'int x!' can only have exclamation - point null checking in implementation methods.
                    // delegate void Del(int x!, int y);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "int x!").WithArguments("int x!").WithLocation(4, 27));
        }

        [Fact]
        public void NullCheckedInterfaceMethod()
        {
            var source = @"
interface C
{
    public int M(int x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4, 18): error CS8713: Parameter 'int x!' can only have exclamation - point null checking in implementation methods.
                    // delegate void Del(int x!, int y);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "int x!").WithArguments("int x!").WithLocation(4, 18));
        }

        [Fact]
        public void NullCheckedExternMethod()
        {
            var source = @"
using System.Runtime.InteropServices;
class C
{
    [DllImport(""User32.dll"")]
    public static extern int M(int x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (6, 32): error CS8713: Parameter 'int x!' can only have exclamation - point null checking in implementation methods.
                    // delegate void Del(int x!, int y);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "int x!").WithArguments("int x!").WithLocation(6, 32));
        }
    }
}
