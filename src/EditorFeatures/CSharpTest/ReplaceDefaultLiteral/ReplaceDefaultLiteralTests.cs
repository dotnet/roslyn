// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ReplaceDefaultLiteral;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReplaceDefaultLiteral
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDefaultLiteral)]
    public sealed class ReplaceDefaultLiteralTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpReplaceDefaultLiteralCodeFixProvider());

        private static readonly TestParameters s_csharpLatest =
            new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

        private static readonly TestParameters s_csharp7_1 =
            new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_1));

        [Fact]
        public async Task TestCSharpLatest_InCaseSwitchLabel_Bool()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (true) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch (true) { case false: }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_InCaseSwitchLabel_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case default(System.DateTime): }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_InCasePatternSwitchLabel_Bool()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (true) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (true) { case false when true: }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_InCasePatternSwitchLabel_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case default(System.DateTime) when true: }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_InIsPattern_Int()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (1 is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (1 is 0) { }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharpLatest_InIsPattern_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (System.DateTime.Now is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (System.DateTime.Now is default(System.DateTime)) { }
    }
}", parameters: s_csharpLatest);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (1) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch (1) { case 0: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int_InParentheses()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (1) { case ([||]default): }
    }
}",
@"class C
{
    void M()
    {
        switch (1) { case (0): }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int_NotInsideCast()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (1) { case (int)[||]default: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int_NotOnDefaultExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (1) { case [||]default(int): }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_Int_NotOnNumericLiteral()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (1) { case [||]0: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case default(System.DateTime): }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_TupleType()
        {
            // Note that the default value of a tuple type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch ((0, true)) { case [||]default: }
    }
}",
@"class C
{
    void M()
    {
        switch ((0, true)) { case default((int, bool)): }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_NotForInvalidType1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (null) { case [||]default: }
    }
}", parameters: s_csharp7_1);
        }
    
        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_NotForInvalidType2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (default) { case [||]default: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_NotForInvalidType3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (() => { }) { case [||]default: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCaseSwitchLabel_NotForMissingExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch () { case [||]default: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (1) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (1) { case 0 when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int_InParentheses()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (1) { case ([||]default) when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (1) { case (0) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int_NotInsideCast()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (1) { case (int)[||]default when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int_NotOnDefaultExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (1) { case [||]default(int) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_Int_NotOnNumericLiteral()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (1) { case [||]0 when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch (System.DateTime.Now) { case default(System.DateTime) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_TupleType()
        {
            // Note that the default value of a tuple type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        switch ((0, true)) { case [||]default when true: }
    }
}",
@"class C
{
    void M()
    {
        switch ((0, true)) { case default((int, bool)) when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_NotForInvalidType1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (null) { case [||]default when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_NotForInvalidType2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (default) { case [||]default when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_NotForInvalidType3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch (() => { }) { case [||]default when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InCasePatternSwitchLabel_NotForMissingExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        switch () { case [||]default when true: }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (true is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if (true is false) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_InParentheses()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (true is ([||]default)) { }
    }
}",
@"class C
{
    void M()
    {
        if (true is (false)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_NotInsideCast()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (true is (bool)[||]default) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_NotOnDefaultExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (true is [||]default(bool)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_NotOnFalseLiteral()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (true is [||]false) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Int()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int value = 1;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        int value = 1;
        if (value is 0) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_UInt()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        uint value = 1;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        uint value = 1;
        if (value is 0U) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Byte()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        byte value = 1;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        byte value = 1;
        if (value is 0) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_SByte()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        sbyte value = 1;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        sbyte value = 1;
        if (value is 0) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Short()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        short value = 1;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        short value = 1;
        if (value is 0) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_UShort()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        ushort value = 1;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        ushort value = 1;
        if (value is 0) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Long()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        long value = 1;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        long value = 1;
        if (value is 0L) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_ULong()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        ulong value = 1;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        ulong value = 1;
        if (value is 0UL) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Float()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        float value = 1;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        float value = 1;
        if (value is 0F) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Double()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        double value = 1;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        double value = 1;
        if (value is 0D) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Decimal()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        decimal value = 1;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        decimal value = 1;
        if (value is 0M) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Char()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        char value = '1';
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        char value = '1';
        if (value is '\0') { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_String()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        string value = "";
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        string value = "";
        if (value is null) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Object()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        var value = new object();
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        var value = new object();
        if (value is null) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_AnonymousType()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        var value = new { a = 0 };
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        var value = new { a = 0 };
        if (value is null) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_DateTime()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        var value = System.DateTime.Now;
        if (value is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        var value = System.DateTime.Now;
        if (value is default(System.DateTime)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_CustomClass()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    class Class { }
    void M()
    {
        if (new Class() is [||]default) { }
    }
}",
@"class C
{
    class Class { }
    void M()
    {
        if (new Class() is null) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_CustomInterface()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    interface Interface { }
    void M()
    {
        if (new Interface() is [||]default) { }
    }
}",
@"class C
{
    interface Interface { }
    void M()
    {
        if (new Interface() is null) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_CustomDelegate()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    delegate void Delegate();
    void M()
    {
        if (new Delegate() is [||]default) { }
    }
}",
@"class C
{
    delegate void Delegate();
    void M()
    {
        if (new Delegate() is null) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_CustomEnum()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    enum Enum { }
    void M()
    {
        if (new Enum() is [||]default) { }
    }
}",
@"class C
{
    enum Enum { }
    void M()
    {
        if (new Enum() is 0) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_CustomStruct()
        {
            // Note that the default value of a struct type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    struct Struct { }
    void M()
    {
        if (new Struct() is [||]default) { }
    }
}",
@"class C
{
    struct Struct { }
    void M()
    {
        if (new Struct() is default(Struct)) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_TupleType()
        {
            // Note that the default value of a tuple type is not a constant, so this code is incorrect.
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if ((0, true) is [||]default) { }
    }
}",
@"class C
{
    void M()
    {
        if ((0, true) is default((int, bool))) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_NotForInvalidType1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    { 
        var value;
        if (value is [||]default) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_NotForInvalidType2()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (value is [||]default) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_NotForInvalidType3()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (null is [||]default) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_NotForInvalidType4()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (default is [||]default) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_NotForInvalidType5()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if (() => { } is [||]default) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_NotForMissingExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        if ( is [||]default) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_Bool_Trivia()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (true is
            /*a*/ [||]default /*b*/) { }
    }
}",
@"class C
{
    void M()
    {
        if (true is
            /*a*/ false /*b*/) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_InIsPattern_DateTime_Trivia()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        if (System.DateTime.Now is
            /*a*/ [||]default /*b*/) { }
    }
}",
@"class C
{
    void M()
    {
        if (System.DateTime.Now is
            /*a*/ default(System.DateTime) /*b*/) { }
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_NotInsideExpression()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int i = [||]default;
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7_1_NotInsideExpression_InvalidType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        var v = [||]default;
    }
}", parameters: s_csharp7_1);
        }

        [Fact]
        public async Task TestCSharp7Lower_NotInsideExpression()
        {
            foreach (var languageVersion in new[] {
                LanguageVersion.CSharp7,
                LanguageVersion.CSharp6,
                LanguageVersion.CSharp5,
                LanguageVersion.CSharp4,
                LanguageVersion.CSharp3,
                LanguageVersion.CSharp2,
                LanguageVersion.CSharp1,
                })
            {
                await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int i = [||]default;
    }
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion)));
            }
        }
    }
}
