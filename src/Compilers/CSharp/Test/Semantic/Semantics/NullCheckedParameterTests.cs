// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) null-checked variables and lambdas.
    /// </summary>
    public class NullCheckedParameterTests : CompilingTestBase
    {
        [Fact]
        public void NullCheckedDelegateDeclaration()
        {
            var source = @"
delegate void Del(int x!, int y);
class C
{
    Del d = delegate(int k!, int j) { /* ... */ };
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (2,23): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    // delegate void Del(int x!, int y);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(2, 23));
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
                    // (4,31): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     abstract public int M(int x!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(4, 31));
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
                    // (4,22): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     public int M(int x!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(4, 22));
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void NullCheckedInterfaceMethod2()
        {
            var source = @"
interface C
{
    public void M(int x!) { }
}

class C
{
    static void Main() { }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.NetStandardLatest,
                                                         parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedAutoProperty()
        {
            var source = @"
abstract class C
{
    string FirstName! { get; set; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,12): warning CS0169: The field 'C.FirstName' is never used
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "FirstName").WithArguments("C.FirstName").WithLocation(4, 12),
                    // (4,21): error CS1003: Syntax error, ',' expected
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments(",", "!").WithLocation(4, 21),
                    // (4,25): error CS1002: ; expected
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "get").WithLocation(4, 25),
                    // (4,28): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 28),
                    // (4,28): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 28),
                    // (4,33): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 33),
                    // (4,33): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 33),
                    // (5,1): error CS1022: Type or namespace definition, or end-of-file expected
                    // }
                    Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(5, 1));
        }

        [Fact]
        public void NullCheckedPartialMethod()
        {
            var source = @"
partial class C
{
    partial void M(int x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,24): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     partial void M(int x!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(4, 24));
        }

        [Fact]
        public void NullCheckedInterfaceProperty()
        {
            var source = @"
interface C
{
    public string this[int index!] { get; set; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,28): error CS8714: Parameter 'index' can only have exclamation-point null checking in implementation methods.
                    //     public string this[int index!] { get; set; }
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "index").WithArguments("index").WithLocation(4, 28));
        }

        [Fact]
        public void NullCheckedAbstractProperty()
        {
            var source = @"
abstract class C
{
    public abstract string this[int index!] { get; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,37): error CS8714: Parameter 'index' can only have exclamation-point null checking in implementation methods.
                    //     public abstract string this[int index!] { get; }
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "index").WithArguments("index").WithLocation(4, 37));
        }

        [Fact]
        public void NullCheckedIndexedProperty()
        {
            var source = @"
class C
{
    public string this[int index!] => null;
}";
            CreateCompilation(source).VerifyDiagnostics();
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
                    // (6,36): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     public static extern int M(int x!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(6, 36));
        }
    }
}
