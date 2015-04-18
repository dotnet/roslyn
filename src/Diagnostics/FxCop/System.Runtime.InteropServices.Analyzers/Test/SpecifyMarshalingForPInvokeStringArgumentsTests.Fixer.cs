// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace System.Runtime.InteropServices.Analyzers.UnitTests
{
    public class SpecifyMarshalingForPInvokeStringArgumentsFixerTests : CodeFixTestBase
    {
        #region Verifiers 

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new PInvokeDiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new PInvokeDiagnosticAnalyzer();
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new BasicSpecifyMarshalingForPInvokeStringArgumentsFixer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CSharpSpecifyMarshalingForPInvokeStringArgumentsFixer();
        }

        #endregion

        #region CA2101 Fixer tests 

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2101FixMarshalAsCSharpTest()
        {
            VerifyCSharpFix(@"
using System.Runtime.InteropServices;
using System.Text;

class C
{
    [DllImport(""user32.dll"")]
    private static extern void Foo1([MarshalAs(UnmanagedType.LPStr)] string s, [MarshalAs(UnmanagedType.LPStr)] StringBuilder t);

    [DllImport(""user32.dll"")]
    private static extern void Foo2([MarshalAs((short)0)] string s);
}
", @"
using System.Runtime.InteropServices;
using System.Text;

class C
{
    [DllImport(""user32.dll"")]
    private static extern void Foo1([MarshalAs(UnmanagedType.LPWStr)] string s, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder t);

    [DllImport(""user32.dll"")]
    private static extern void Foo2([MarshalAs(UnmanagedType.LPWStr)] string s);
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2101FixMarshalAsBasicTest()
        {
            VerifyBasicFix(@"
Imports System.Runtime.InteropServices
Imports System.Text

Class C
    <DllImport(""user32.dll"")>
    Private Shared Sub Foo1(<MarshalAs(UnmanagedType.LPStr)> s As String, <MarshalAs(UnmanagedType.LPStr)> t As StringBuilder)
    End Sub

    <DllImport(""user32.dll"")>
    Private Shared Sub Foo2(<MarshalAs(CShort(0)) s As String)
    End Sub

    Private Declare Sub Foo3 Lib ""user32.dll"" (<MarshalAs(UnmanagedType.LPStr)> s As String)
End Class
", @"
Imports System.Runtime.InteropServices
Imports System.Text

Class C
    <DllImport(""user32.dll"")>
    Private Shared Sub Foo1(<MarshalAs(UnmanagedType.LPWStr)> s As String, <MarshalAs(UnmanagedType.LPWStr)> t As StringBuilder)
    End Sub

    <DllImport(""user32.dll"")>
    Private Shared Sub Foo2(<MarshalAs(UnmanagedType.LPWStr) s As String)
    End Sub

    Private Declare Sub Foo3 Lib ""user32.dll"" (<MarshalAs(UnmanagedType.LPWStr)> s As String)
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2101FixCharSetCSharpTest()
        {
            VerifyCSharpFix(@"
using System.Runtime.InteropServices;
using System.Text;

class C
{
    [DllImport(""user32.dll"")]
    private static extern void Foo1(string s);

    [DllImport(""user32.dll"", CharSet = CharSet.Ansi)]
    private static extern void Foo2(string s);
}
", @"
using System.Runtime.InteropServices;
using System.Text;

class C
{
    [DllImport(""user32.dll"", CharSet = CharSet.Unicode)]
    private static extern void Foo1(string s);

    [DllImport(""user32.dll"", CharSet = CharSet.Unicode)]
    private static extern void Foo2(string s);
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2101FixCharSetBasicTest()
        {
            VerifyBasicFix(@"
Imports System.Runtime.InteropServices
Imports System.Text

Class C
    <DllImport(""user32.dll"")>
    Private Shared Sub Foo1(s As String)
    End Sub

    <DllImport(""user32.dll"", CharSet:=CharSet.Ansi)>
    Private Shared Sub Foo2(s As String)
    End Sub
EndClass
", @"
Imports System.Runtime.InteropServices
Imports System.Text

Class C
    <DllImport(""user32.dll"", CharSet:=CharSet.Unicode)>
    Private Shared Sub Foo1(s As String)
    End Sub

    <DllImport(""user32.dll"", CharSet:=CharSet.Unicode)>
    Private Shared Sub Foo2(s As String)
    End Sub
EndClass
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2101FixDeclareBasicTest()
        {
            VerifyBasicFix(@"
Imports System.Text

Class C
    Private Declare Sub Foo1 Lib ""user32.dll"" (s As String)
    Private Declare Ansi Sub Foo2 Lib ""user32.dll"" (s As StringBuilder)
    Private Declare Function Foo3 Lib ""user32.dll"" () As String
EndClass
", @"
Imports System.Text

Class C
    Private Declare Unicode Sub Foo1 Lib ""user32.dll"" (s As String)
    Private Declare Unicode Sub Foo2 Lib ""user32.dll"" (s As StringBuilder)
    Private Declare Unicode Function Foo3 Lib ""user32.dll"" () As String
EndClass
");
        }

        #endregion
    }
}
