// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
delegate void Del(string x!!, int y);
class C
{
    Del d = delegate(string k!!, int j) { /* ... */ };
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (2,26): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    // delegate void Del(int x!!, int y);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(2, 26));
        }

        [Fact]
        public void NullCheckedAbstractMethod()
        {
            var source = @"
abstract class C
{
    abstract public int M(string x!!);
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (4,34): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     abstract public int M(int x!!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(4, 34));
        }

        [Fact]
        public void NullCheckedInterfaceMethod()
        {
            var source = @"
interface C
{
    public int M(string x!!);
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (4,25): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     public int M(int x!!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(4, 25));
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void NullCheckedInterfaceMethod2()
        {
            var source = @"
interface C
{
    public void M(string x!!) { }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll, targetFramework: TargetFramework.StandardLatest,
                parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedAutoProperty()
        {
            var source = @"
abstract class C
{
    string FirstName!! { get; set; }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (4,12): warning CS0169: The field 'C.FirstName' is never used
                //     string FirstName!! { get; set; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "FirstName").WithArguments("C.FirstName").WithLocation(4, 12),
                // (4,21): error CS1003: Syntax error, ',' expected
                //     string FirstName!! { get; set; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments(",", "!").WithLocation(4, 21),
                // (4,26): error CS1002: ; expected
                //     string FirstName!! { get; set; }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "get").WithLocation(4, 26),
                // (4,29): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                //     string FirstName!! { get; set; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 29),
                // (4,29): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                //     string FirstName!! { get; set; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 29),
                // (4,34): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                //     string FirstName!! { get; set; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 34),
                // (4,34): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                //     string FirstName!! { get; set; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 34),
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
    partial void M(string x!!);
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (4,27): error CS8717: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     partial void M(string x!!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(4, 27));
        }

        [Fact]
        public void NullCheckedBadSyntax()
        {
            var source = @"
partial class C
{
    void M0(string name !!=""a"") { }
    void M1(string name! !=""a"") { }
    void M2(string name!!= ""a"") { }
    void M3(string name ! !=""a"") { }
    void M4(string name ! ! =""a"") { }
    void M5(string name! ! =""a"") { }
    void M6(string name! != ""a"") { }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (5,24): error CS1003: Syntax error, '!!' expected
                //     void M1(string name! !="a") { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "! !").WithArguments("!!", "!").WithLocation(5, 24),
                // (7,25): error CS1003: Syntax error, '!!' expected
                //     void M3(string name ! !="a") { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "! !").WithArguments("!!", "!").WithLocation(7, 25),
                // (8,25): error CS1003: Syntax error, '!!' expected
                //     void M4(string name ! ! ="a") { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(8, 25),
                // (9,24): error CS1003: Syntax error, '!!' expected
                //     void M5(string name! ! ="a") { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(9, 24),
                // (10,24): error CS1003: Syntax error, '!!' expected
                //     void M6(string name! != "a") { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "! !").WithArguments("!!", "!").WithLocation(10, 24)
                );
        }

        [Fact]
        public void CommentTriviaBetweenExclamations()
        {
            var source = @"
partial class C
{
    void M0(string name !/*comment1*/
    /*comment2*/!) { }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(
                    // (4,25): error CS1003: Syntax error, '!!' expected
                    //     void M0(string name !/*comment1*/
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(4, 25)
                    );
        }

        [Fact]
        public void CommentTriviaSurroundingNotEquals()
        {
            var source = @"
partial class C
{
    void M0(string name
        /*comment1*/!=/*comment2*/
        ""a"") { }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview)
                .VerifyDiagnostics(
                    // (4,12): error CS1003: Syntax error, '!!' expected
                    //     void M0(string name
                    Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("!!", "!").WithLocation(4, 12)
                    );
        }

        [Fact]
        public void NullCheckedInterfaceProperty()
        {
            var source = @"
interface C
{
    public string this[string index!!] { get; set; }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (4,31): error CS8714: Parameter 'index' can only have exclamation-point null checking in implementation methods.
                    //     public string this[int index!!] { get; set; }
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "index").WithArguments("index").WithLocation(4, 31));
        }

        [Fact]
        public void NullCheckedAbstractProperty()
        {
            var source = @"
abstract class C
{
    public abstract string this[string index!!] { get; }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (4,40): error CS8714: Parameter 'index' can only have exclamation-point null checking in implementation methods.
                    //     public abstract string this[int index!!] { get; }
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "index").WithArguments("index").WithLocation(4, 40));
        }

        [Fact]
        public void NullCheckedIndexedProperty()
        {
            var source = @"
class C
{
    public string this[string index!!] => null;
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourcePropertySymbol>("this[]");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedExternMethod()
        {
            var source = @"
using System.Runtime.InteropServices;
class C
{
    [DllImport(""User32.dll"")]
    public static extern int M(string x!!);
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (6,39): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     public static extern int M(int x!!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(6, 39));
        }

        [Fact]
        public void NullCheckedMethodDeclaration()
        {
            var source = @"
class C
{
    void M(string name!!) { }
    void M2(string x) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);

            var m2 = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M2");
            Assert.False(((SourceParameterSymbol)m2.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void FailingNullCheckedArgList()
        {
            var source = @"
class C
{
    void M()
    {
        M2(__arglist(1, 'M'));
    }
    void M2(__arglist!!)
    {
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (8,22): error CS1003: Syntax error, ',' expected
                    //     void M2(__arglist!!)
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments(",", "!").WithLocation(8, 22));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/58335")]
        public void NullCheckedArgList()
        {
            var source = @"
class C
{
    void M()
    {
        M2(__arglist(1!!, 'M'));
    }
    void M2(__arglist)
    {
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedMethodValidationWithOptionalNullParameter()
        {
            var source = @"
class C
{
    void M(string name!! = null) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                    // (4,19): warning CS8719: Parameter 'name' is null-checked but is null by default.
                    //     void M(string name!! = null) { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "name").WithArguments("name").WithLocation(4, 19));
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedMethodValidationWithOptionalStringLiteralParameter()
        {
            var source = @"
class C
{
    void M(string name!! = ""rose"") { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedMethodValidationWithOptionalParameterSplitBySpace()
        {
            var source = @"
class C
{
    void M(string name!! =null) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                    // (4,19): warning CS8719: Parameter 'name' is null-checked but is null by default.
                    //     void M(string name!! =null) { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "name").WithArguments("name").WithLocation(4, 19));
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedConstructor()
        {
            var source = @"
class C
{
    public C(string name!!) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>(".ctor");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedOperator()
        {
            var source = @"
class Box 
{ 
    public static int operator+ (Box b!!, Box c)  
    { 
        return 2;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("Box").GetMember<SourceUserDefinedOperatorSymbol>("op_Addition");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
            Assert.False(((SourceParameterSymbol)m.Parameters[1]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedLambdaParameter()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = x!! => x + ""1"";
    }
}";
            var tree = Parse(source, options: TestOptions.RegularPreview);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedLambdaWithMultipleParameters()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string, bool> func1 = (x!!, y) => x == y;
    }
}";
            var tree = Parse(source, options: TestOptions.RegularPreview);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedLambdaSingleParameterInParentheses()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = (x!!) => x;
    }
}";
            var tree = Parse(source, options: TestOptions.RegularPreview);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedLambdaSingleParameterNoSpaces()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = x!!=> x;
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedLambdaBadSyntax()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func0 = x!=> x;
        Func<string, string> func1 = x !=> x;
        Func<string, string> func2 = x != > x;
        Func<string, string> func3 = x! => x;
        Func<string, string> func4 = x ! => x;
        Func<string, string> func5 = x !!=> x;
        Func<string, string> func6 = x !!= > x;
        Func<string, string> func7 = x !! => x;
        Func<string, string> func8 = x! !=> x;
        Func<string, string> func9 = x! ! => x;
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (7,39): error CS1003: Syntax error, '!!' expected
                    //         Func<string, string> func0 = x!=> x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(7, 39),
                    // (8,40): error CS1003: Syntax error, '!!' expected
                    //         Func<string, string> func1 = x !=> x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(8, 40),
                    // (9,40): error CS1003: Syntax error, '!!' expected
                    //         Func<string, string> func2 = x != > x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(9, 40),
                    // (9,41): error CS1003: Syntax error, '=>' expected
                    //         Func<string, string> func2 = x != > x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=").WithArguments("=>", "=").WithLocation(9, 41),
                    // (10,38): error CS0103: The name 'x' does not exist in the current context
                    //         Func<string, string> func3 = x! => x;
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(10, 38),
                    // (10,41): error CS1003: Syntax error, ',' expected
                    //         Func<string, string> func3 = x! => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(10, 41),
                    // (10,44): error CS1002: ; expected
                    //         Func<string, string> func3 = x! => x;
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x").WithLocation(10, 44),
                    // (10,44): error CS0103: The name 'x' does not exist in the current context
                    //         Func<string, string> func3 = x! => x;
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(10, 44),
                    // (10,44): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                    //         Func<string, string> func3 = x! => x;
                    Diagnostic(ErrorCode.ERR_IllegalStatement, "x").WithLocation(10, 44),
                    // (11,38): error CS0103: The name 'x' does not exist in the current context
                    //         Func<string, string> func4 = x ! => x;
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(11, 38),
                    // (11,42): error CS1003: Syntax error, ',' expected
                    //         Func<string, string> func4 = x ! => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(11, 42),
                    // (11,45): error CS1002: ; expected
                    //         Func<string, string> func4 = x ! => x;
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x").WithLocation(11, 45),
                    // (11,45): error CS0103: The name 'x' does not exist in the current context
                    //         Func<string, string> func4 = x ! => x;
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(11, 45),
                    // (11,45): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                    //         Func<string, string> func4 = x ! => x;
                    Diagnostic(ErrorCode.ERR_IllegalStatement, "x").WithLocation(11, 45),
                    // (13,42): error CS1003: Syntax error, '=>' expected
                    //         Func<string, string> func6 = x !!= > x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=").WithArguments("=>", "=").WithLocation(13, 42),
                    // (15,39): error CS1003: Syntax error, '!!' expected
                    //         Func<string, string> func8 = x! !=> x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments("!!", "!").WithLocation(15, 39),
                    // (16,39): error CS1003: Syntax error, '!!' expected
                    //         Func<string, string> func9 = x! ! => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "! ").WithArguments("!!", "!").WithLocation(16, 39));
        }

        [Fact]
        public void NullCheckedLambdaSingleTypedParameterInParentheses()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = (string x!!) => x;
    }
}";
            var tree = Parse(source, options: TestOptions.RegularPreview);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedLambdaManyTypedParametersInParentheses()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string, string> func1 = (string x!!, string y) => x;
    }
}";
            var tree = Parse(source, options: TestOptions.RegularPreview);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LambdaManyNullCheckedParametersInParentheses()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string, string> func1 = (string x!!, string y!!) => x;
    }
}";
            var tree = Parse(source, options: TestOptions.RegularPreview);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedDiscard_1()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, int> func1 = (_!!) => 42;
        Func<string, string, int> func2 = (_!!, x) => 42;
    }
}";
            var tree = Parse(source, options: TestOptions.RegularPreview);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedDiscard_2()
        {
            var source = @"
using System;
class C
{
    public Action<string, string> action0 = (_, _) => { }; // 1
    public Action<string, string> action1 = (_!!, _) => { }; // 1
    public Action<string, string> action2 = (_, _!!) => { }; // 2
    public Action<string, string> action3 = (_!!, _!!) => { }; // 3, 4
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics(
                // (6,46): error CS8990: Discard parameter cannot be null-checked.
                //     public Action<string, string> action1 = (_!!, _) => { }; // 1
                Diagnostic(ErrorCode.ERR_DiscardCannotBeNullChecked, "_").WithLocation(6, 46),
                // (7,49): error CS8990: Discard parameter cannot be null-checked.
                //     public Action<string, string> action2 = (_, _!!) => { }; // 2
                Diagnostic(ErrorCode.ERR_DiscardCannotBeNullChecked, "_").WithLocation(7, 49),
                // (8,46): error CS8990: Discard parameter cannot be null-checked.
                //     public Action<string, string> action3 = (_!!, _!!) => { }; // 3, 4
                Diagnostic(ErrorCode.ERR_DiscardCannotBeNullChecked, "_").WithLocation(8, 46),
                // (8,51): error CS8990: Discard parameter cannot be null-checked.
                //     public Action<string, string> action3 = (_!!, _!!) => { }; // 3, 4
                Diagnostic(ErrorCode.ERR_DiscardCannotBeNullChecked, "_").WithLocation(8, 51));
        }

        [Fact]
        public void TestNullCheckedOutString()
        {
            var source = @"
class C
{
    public static void Main() { }
    public void M(out string x!!)
    {
        x = ""hello world"";
    }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics(
                    // (5,31): error CS8994: 'out' parameter 'x' cannot be null-checked.
                    //     public void M(out string x!!)
                    Diagnostic(ErrorCode.ERR_NullCheckingOnOutParameter, "!!").WithArguments("x").WithLocation(5, 31));
        }

        [Fact]
        public void TestNullCheckedRefString()
        {
            var source = @"
class C
{
    public static void Main() { }
    public void M(ref string x!!)
    {
        x = ""hello world"";
    }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void TestNullCheckedInString()
        {
            var source = @"
class C
{
    public static void Main() { }
    public void M(in string x!!) { }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void TestNullCheckedGenericWithDefault()
        {
            var source = @"
class C
{
    static void M1<T>(T t!! = default) { }
    static void M2<T>(T? t!! = default) where T : struct { }
    static void M3<T>(T t!! = default) where T : class { }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics(
                    // (5,26): warning CS8721: Nullable value type 'T?' is null-checked and will throw if null.
                    //     static void M2<T>(T? t!! = default) where T : struct { }
                    Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "t").WithArguments("T?").WithLocation(5, 26),
                    // (6,25): warning CS8719: Parameter 't' is null-checked but is null by default.
                    //     static void M3<T>(T t!! = default) where T : class { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "t").WithArguments("t").WithLocation(6, 25));
        }

        [Fact]
        public void TestNullableInteraction()
        {
            var source = @"
class C
{
    static void M(int? i!!) { }
    public static void Main() { }
}";
            // Release
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics(
                    // (4,24): error CS8721: Nullable value type 'int?' is null-checked and will throw if null.
                    //     static void M(int? i!!) { }
                    Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "i").WithArguments("int?").WithLocation(4, 24));
        }

        [Fact]
        public void TestNullableGenericsImplementingGenericAbstractClass()
        {
            var source = @"
abstract class A<T>
{
    internal abstract void F<U>(U u) where U : T;
}
class B1 : A<object>
{
    internal override void F<U>(U u!! = default) { }
}
class B2 : A<string>
{
    internal override void F<U>(U u!! = default) { }
}
class B3 : A<int?>
{
    internal override void F<U>(U u!! = default) { } // note: 'U' is a nullable type here but we don't give a warning due to complexity of accurately searching the constraints.
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics(
                // (12,35): warning CS8993: Parameter 'u' is null-checked but is null by default.
                //     internal override void F<U>(U u!! = default) { }
                Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "u").WithArguments("u").WithLocation(12, 35));
        }

        [Fact]
        public void NoGeneratedNullCheckIfNonNullableTest()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<int, int> func1 = x!! => x;
    }
}";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            compilation.VerifyDiagnostics(
                    // (7,32): error CS8718: Parameter 'int' is a non-nullable value type and therefore cannot be null-checked.
                    //         Func<int, int> func1 = x!! => x;
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "x").WithArguments("int").WithLocation(7, 32));
        }

        [Fact]
        public void TestNullCheckedParamWithOptionalNullParameter()
        {
            var source = @"
class C
{
    public static void Main() { }
    void M(string name!! = null) { }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (5,19): warning CS8719: Parameter 'name' is null-checked but is null by default.
                    //     void M(string name!! = null) { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "name").WithArguments("name").WithLocation(5, 19));
        }

        [Fact]
        public void TestManyNullCheckedArgs()
        {
            var source = @"
class C
{
    public void M(int x!!, string y!!) { }
    public static void Main() { }
}";

            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (4,23): error CS8718: Parameter 'int' is a non-nullable value type and therefore cannot be null-checked.
                    //     public void M(int x!!, string y!!) { }
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "x").WithArguments("int").WithLocation(4, 23));
        }

        [Fact]
        public void TestNullCheckedSubstitutionWithDiagnostic1()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!!) where U : T { }
}
class B2<T> : A<T> where T : struct
{
    internal override void M<U>(U u!!) { }
}";

            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (8,35): error CS8718: Parameter 'U' is a non-nullable value type and therefore cannot be null-checked.
                    //     internal override void M<U>(U u!!) { }
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "u").WithArguments("U").WithLocation(8, 35));
        }

        [Fact]
        public void TestNullCheckedSubstitutionWithDiagnostic2()
        {
            var source = @"
class A<T>
{
    internal virtual void M<U>(U u!!) where U : T { }
}
class B4 : A<int>
{
    internal override void M<U>(U u!!) { }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (8,35): error CS8718: Parameter 'U' is a non-nullable value type and therefore cannot be null-checked.
                    //     internal override void M<U>(U u!!) { }
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "u").WithArguments("U").WithLocation(8, 35));
        }

        [Fact]
        public void TestNullCheckedSubstitutionWithDiagnostic3()
        {
            var source = @"
class C
{
    void M<T>(T value!!) where T : unmanaged { }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (4,17): error CS8718: Parameter 'T' is a non-nullable value type and cannot be null-checked.
                    //     void M<T>(T value!!) where T : unmanaged { }
                    Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "value").WithArguments("T").WithLocation(4, 17));
        }

        [Fact]
        public void TestNullCheckedNullableValueTypeInLocalFunction()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M((int?)5);
        void M(int? x!!) { }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (7,21): warning CS8721: Nullable value type 'int?' is null-checked and will throw if null.
                    //         void M(int? x!!) { }
                    Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "x").WithArguments("int?").WithLocation(7, 21));
        }

        [Fact]
        public void TestNullCheckedParameterWithDefaultNullValueInLocalFunction()
        {
            var source = @"
class C
{
    public static void Main()
    {
        M(""ok"");
        void M(string x!! = null) { }
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                    // (7,23): warning CS8719: Parameter 'x' is null-checked but is null by default.
                    //         void M(string x!! = null) { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "x").WithArguments("x").WithLocation(7, 23));
        }

        [Fact]
        public void TestNullCheckedWithMissingType()
        {
            var source =
@"
class Program
{
    static void Main(string[] args!!) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.MakeMemberMissing(WellKnownMember.System_ArgumentNullException__ctorString);
            comp.MakeTypeMissing(WellKnownType.System_ArgumentNullException);
            comp.VerifyDiagnostics(
                    // (4,31): error CS0656: Missing compiler required member 'System.ArgumentNullException..ctor'
                    //     static void Main(string[] args!!) { }
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "args").WithArguments("System.ArgumentNullException", ".ctor").WithLocation(4, 31));
        }

        [Fact]
        public void TestNullCheckedMethodParameterWithWrongLanguageVersion()
        {
            var source =
@"
class Program
{
    void M(string x!!) { }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                    // (4,20): error CS8652: The feature 'parameter null-checking' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //     void M(string x!!) { }
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "!!").WithArguments("parameter null-checking").WithLocation(4, 20));
        }

        [Fact]
        public void TestNullCheckedLambdaParameterWithWrongLanguageVersion()
        {
            var source =
@"
using System;
class Program
{
    void M()
    {
        Func<string, string> func = x!! => x;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                    // (7,38): error CS8652: The feature 'parameter null-checking' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         Func<string, string> func = x!! => x;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "!!").WithArguments("parameter null-checking").WithLocation(7, 38));
        }

        [Fact]
        public void TestNullCheckedLambdaParametersWithWrongLanguageVersion()
        {
            var source =
@"
using System;
class Program
{
    void M()
    {
        Func<string, string, string> func = (x!!, y) => x;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                    // (7,47): error CS8652: The feature 'parameter null-checking' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         Func<string, string, string> func = (x!!, y) => x;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "!!").WithArguments("parameter null-checking").WithLocation(7, 47));
        }

        [Fact]
        public void TestNullCheckedParameterUpdatesFlowState1()
        {
            var source =
@"
#nullable enable

class Program
{
    string M(string? s!!) // 1
    {
        return s;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,22): warning CS8995: Nullable type 'string?' is null-checked and will throw if null.
                //     string M(string? s!!) // 1
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "s").WithArguments("string?").WithLocation(6, 22));
        }

        [Fact]
        public void TestNullCheckedParameterUpdatesFlowState2()
        {
            var source =
@"
#nullable enable

class Program
{
    int M(int? x!!) // 1
    {
        return x.Value;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,16): warning CS8995: Nullable type 'int?' is null-checked and will throw if null.
                //     int M(int? x!!) // 1
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "x").WithArguments("int?").WithLocation(6, 16));
        }

        [Fact]
        public void TestNullCheckedParameterUpdatesFlowState3()
        {
            var source =
@"
#nullable enable

using System.Diagnostics.CodeAnalysis;

class Program
{
    void M1(string? s1, string? s2!!) // 1
    {
        s1.ToString(); // 2
        s2.ToString();
    }

    static void M2<T>(T x1, T y1!!)
    {
        x1.ToString(); // 3
        y1.ToString();
    }
    static void M3<T>([AllowNull] T x2, [AllowNull] T y2!!)
    {
        x2.ToString(); // 4
        y2.ToString();
    }
    static void M4<T>([DisallowNull] T x3, [DisallowNull] T y3!!)
    {
        x3.ToString();
        y3.ToString();
    }
}";
            var comp = CreateCompilation(new[] { source, AllowNullAttributeDefinition, DisallowNullAttributeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,33): warning CS8995: Nullable type 'string?' is null-checked and will throw if null.
                //     void M1(string? s1, string? s2!!) // 1
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "s2").WithArguments("string?").WithLocation(8, 33),
                // (10,9): warning CS8602: Dereference of a possibly null reference.
                //         s1.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "s1").WithLocation(10, 9),
                // (16,9): warning CS8602: Dereference of a possibly null reference.
                //         x1.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x1").WithLocation(16, 9),
                // (19,55): warning CS8995: Nullable type 'T' is null-checked and will throw if null.
                //     static void M3<T>([AllowNull] T x2, [AllowNull] T y2!!)
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "y2").WithArguments("T").WithLocation(19, 55),
                // (21,9): warning CS8602: Dereference of a possibly null reference.
                //         x2.ToString(); // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x2").WithLocation(21, 9));
        }

        [Fact]
        public void TestNullCheckedParameterDoesNotAffectNullableVarianceChecks()
        {
            var source =
@"
#nullable enable

class Base
{
    public virtual void M1(string? s) { }
    public virtual void M2(string? s!!) { } // 1
}

class Derived : Base
{
    public override void M1(string s) { } // 2
    public override void M2(string s) { } // 3
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,36): warning CS8995: Nullable type 'string?' is null-checked and will throw if null.
                //     public virtual void M2(string? s!!) { } // 1
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "s").WithArguments("string?").WithLocation(7, 36),
                // (12,26): warning CS8765: Nullability of type of parameter 's' doesn't match overridden member (possibly because of nullability attributes).
                //     public override void M1(string s) { } // 2
                Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride, "M1").WithArguments("s").WithLocation(12, 26),
                // (13,26): warning CS8765: Nullability of type of parameter 's' doesn't match overridden member (possibly because of nullability attributes).
                //     public override void M2(string s) { } // 3
                Diagnostic(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride, "M2").WithArguments("s").WithLocation(13, 26)
            );
        }

        [Fact]
        public void TestNullabilityAttributes()
        {
            var source =
@"
#nullable enable
using System.Diagnostics.CodeAnalysis;

class C
{
    void M<T>(
        string s1!!,
        [NotNull] string s2!!,
        [DisallowNull] string s3!!,
        [AllowNull] string s4!!, // 1
        [AllowNull, DisallowNull] string s5!!, // 2
        [AllowNull, NotNull] string s6!!,

        string? s7!!, // 3
        [NotNull] string? s8!!, // ok: this is a typical signature for an 'AssertNotNull' style method.
        [DisallowNull] string? s9!!,
        [AllowNull] string? s10!!, // 4
        [AllowNull, DisallowNull] string? s11!!, // 5
        [AllowNull, NotNull] string? s12!!,

        int i1!!, // 6
        [NotNull] int i2!!, // 7
        [DisallowNull] int i3!!, // 8
        [AllowNull] int i4!!, // 9
        [AllowNull, DisallowNull] int i5!!, // 10
        [AllowNull, NotNull] int i6!!, // 11

        int? i7!!, // 12
        [NotNull] int? i8!!,
        [DisallowNull] int? i9!!,
        [AllowNull] int? i10!!, // 13
        [AllowNull, DisallowNull] int? i11!!, // 14
        [AllowNull, NotNull] int? i12!!
    ) { }
}";
            var comp = CreateCompilation(new[] { source, AllowNullAttributeDefinition, DisallowNullAttributeDefinition, NotNullAttributeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (11,28): warning CS8995: Nullable type 'string' is null-checked and will throw if null.
                //         [AllowNull] string s4!!, // 1
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "s4").WithArguments("string").WithLocation(11, 28),
                // (12,42): warning CS8995: Nullable type 'string' is null-checked and will throw if null.
                //         [AllowNull, DisallowNull] string s5!!, // 2
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "s5").WithArguments("string").WithLocation(12, 42),
                // (15,17): warning CS8995: Nullable type 'string?' is null-checked and will throw if null.
                //         string? s7!!, // 3
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "s7").WithArguments("string?").WithLocation(15, 17),
                // (18,29): warning CS8995: Nullable type 'string?' is null-checked and will throw if null.
                //         [AllowNull] string? s10!!, // 4
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "s10").WithArguments("string?").WithLocation(18, 29),
                // (19,43): warning CS8995: Nullable type 'string?' is null-checked and will throw if null.
                //         [AllowNull, DisallowNull] string? s11!!, // 5
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "s11").WithArguments("string?").WithLocation(19, 43),
                // (22,13): error CS8992: Parameter 'int' is a non-nullable value type and cannot be null-checked.
                //         int i1!!, // 6
                Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "i1").WithArguments("int").WithLocation(22, 13),
                // (23,23): error CS8992: Parameter 'int' is a non-nullable value type and cannot be null-checked.
                //         [NotNull] int i2!!, // 7
                Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "i2").WithArguments("int").WithLocation(23, 23),
                // (24,28): error CS8992: Parameter 'int' is a non-nullable value type and cannot be null-checked.
                //         [DisallowNull] int i3!!, // 8
                Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "i3").WithArguments("int").WithLocation(24, 28),
                // (25,25): error CS8992: Parameter 'int' is a non-nullable value type and cannot be null-checked.
                //         [AllowNull] int i4!!, // 9
                Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "i4").WithArguments("int").WithLocation(25, 25),
                // (26,39): error CS8992: Parameter 'int' is a non-nullable value type and cannot be null-checked.
                //         [AllowNull, DisallowNull] int i5!!, // 10
                Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "i5").WithArguments("int").WithLocation(26, 39),
                // (27,34): error CS8992: Parameter 'int' is a non-nullable value type and cannot be null-checked.
                //         [AllowNull, NotNull] int i6!!, // 11
                Diagnostic(ErrorCode.ERR_NonNullableValueTypeIsNullChecked, "i6").WithArguments("int").WithLocation(27, 34),
                // (29,14): warning CS8995: Nullable type 'int?' is null-checked and will throw if null.
                //         int? i7!!, // 12
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "i7").WithArguments("int?").WithLocation(29, 14),
                // (32,26): warning CS8995: Nullable type 'int?' is null-checked and will throw if null.
                //         [AllowNull] int? i10!!, // 13
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "i10").WithArguments("int?").WithLocation(32, 26),
                // (33,40): warning CS8995: Nullable type 'int?' is null-checked and will throw if null.
                //         [AllowNull, DisallowNull] int? i11!!, // 14
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "i11").WithArguments("int?").WithLocation(33, 40)
            );
        }

        [Theory]
        [InlineData("")]
        [InlineData("where T : class")]
        [InlineData("where T : class?")]
        [InlineData("where T : notnull")]
        public void TestNullabilityAttributes_Generic(string constraints)
        {
            var source =
@"
#nullable enable
using System.Diagnostics.CodeAnalysis;

class C
{
    void M<T>(
        T t1!!,
        [NotNull] T t2!!,
        [DisallowNull] T t3!!,
        [AllowNull] T t4!!, // 1
        [AllowNull, DisallowNull] T t5!!, // 2
        [AllowNull, NotNull] T t6!!,

        T? t7!!, // 3
        [NotNull] T? t8!!,
        [DisallowNull] T? t9!!,
        [AllowNull] T? t10!!, // 4
        [AllowNull, DisallowNull] T? t11!!, // 5
        [AllowNull, NotNull] T? t12!!
    ) " + constraints + @" { }
}";
            var comp = CreateCompilation(new[] { source, AllowNullAttributeDefinition, DisallowNullAttributeDefinition, NotNullAttributeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (11,23): warning CS8995: Nullable type 'T' is null-checked and will throw if null.
                //         [AllowNull] T t4!!, // 1
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "t4").WithArguments("T").WithLocation(11, 23),
                // (12,37): warning CS8995: Nullable type 'T' is null-checked and will throw if null.
                //         [AllowNull, DisallowNull] T t5!!, // 2
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "t5").WithArguments("T").WithLocation(12, 37),
                // (15,12): warning CS8995: Nullable type 'T?' is null-checked and will throw if null.
                //         T? t7!!, // 3
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "t7").WithArguments("T?").WithLocation(15, 12),
                // (18,24): warning CS8995: Nullable type 'T?' is null-checked and will throw if null.
                //         [AllowNull] T? t10!!, // 4
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "t10").WithArguments("T?").WithLocation(18, 24),
                // (19,38): warning CS8995: Nullable type 'T?' is null-checked and will throw if null.
                //         [AllowNull, DisallowNull] T? t11!! // 5
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "t11").WithArguments("T?").WithLocation(19, 38)
            );
        }

        [Fact]
        public void AnnotatedTypeParameter_Indirect()
        {
            var source = @"
#nullable enable

class C
{
    void M<T, U>(
        T? t!!, // 1
        U u!!) where U : T?
    {
    }
}";
            // note: U is always nullable when a reference type,
            // but we don't warn on '!!' for it due to complexity of accurately searching the constraints.
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,12): warning CS8995: Nullable type 'T?' is null-checked and will throw if null.
                //         T? t!!, // 1
                Diagnostic(ErrorCode.WRN_NullCheckingOnNullableType, "t").WithArguments("T?").WithLocation(7, 12));
        }
    }
}
