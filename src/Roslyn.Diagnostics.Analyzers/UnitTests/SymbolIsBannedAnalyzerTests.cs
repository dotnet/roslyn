// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Test.Utilities;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class SymbolIsBannedAnalyzerTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SymbolIsBannedAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new SymbolIsBannedAnalyzer();
        }

        private void VerifyBasic(string source, string bannedApiText, params DiagnosticResult[] expected)
        {
            var additionalFiles = GetAdditionalTextFiles(SymbolIsBannedAnalyzer.BannedSymbolsFileName, bannedApiText);
            Verify(source, LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), additionalFiles, compilationOptions: null, parseOptions: null, expected: expected);
        }

        private void VerifyCSharp(string source, string bannedApiText, params DiagnosticResult[] expected)
        {
            var additionalFiles = GetAdditionalTextFiles(SymbolIsBannedAnalyzer.BannedSymbolsFileName, bannedApiText);
            Verify(source, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), additionalFiles, compilationOptions: null, parseOptions: null, expected: expected);
        }

        #region Diagnostic tests

        [Fact]
        public void NoDiagnosticForNoBannedText()
        {
            Verify("class C { }", LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), additionalFiles: null, compilationOptions: null, parseOptions: null, expected: Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public void NoDiagnosticReportedForEmptyBannedText()
        {
            var source = @"";

            var bannedText = @"";

            VerifyCSharp(source, bannedText);
        }

        [Fact]
        public void DiagnosticReportedForDuplicateBannedApiLines()
        {
            var source = @"";
            var bannedText = @"
System.Console
System.Console";

            VerifyCSharp(source, bannedText,
                GetResultAt(
                    SymbolIsBannedAnalyzer.BannedSymbolsFileName,
                    SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule.Id,
                    string.Format(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule.MessageFormat.ToString(), "System.Console"),
                    "(3,1)", "(2,1)"));
        }

        [Fact]
        public void CSharp_DiagnosticReportedForTypeInSource()
        {
            var source = @"
namespace N
{
    class Banned { }
    class C
    {
        void M()
        {
            var c = new Banned();
        }
    }
}";

            var bannedText = @"
N.Banned";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(9, 21, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned"));
        }

        [Fact]
        public void CSharp_DiagnosticReportedForGenericTypeFromMetadata()
        {
            var source = @"
class C
{
    void M()
    {
        var c = new System.Collections.Generic.List<string>();
    }
}";

            var bannedText = @"
System.Collections.Generic.List`1";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(6, 17, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "List<T>"));
        }

        [Fact]
        public void CSharp_DiagnosticReportedForNestedType()
        {
            var source = @"
class C
{
    class Nested { }
    void M()
    {
        var n = new Nested();
    }
}";

            var bannedText = @"
C+Nested";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(7, 17, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Nested"));
        }

        [Fact]
        public void CSharp_DiagnosticReportedForNestedType2()
        {
            var source = @"
class C
{
    public static class Nested
    {
        public static void M() { }
    }
}

class D
{
    void M2()
    {
        C.Nested.M();
    }
}";
            var bannedText = @"
C";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(14, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"));
        }

        [Fact]
        public void CSharp_InvocationOfMethodOnInterface()
        {
            var source = @"
interface I
{
    void M();
}

class C
{
    void M()
    {
        I i = null;
        i.M();
    }
}";
            var bannedText = @"I";

            VerifyCSharp(source, bannedText, GetCSharpResultAt(12, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "I"));
        }

        [Fact]
        public void CSharp_PropertyAccess()
        {
            var source = @"
class C
{
    public int P { get; set; }
    void M()
    {
        P = P;
    }
}";
            var bannedText = @"C";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(7, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"),
                GetCSharpResultAt(7, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"));
        }

        [Fact]
        public void CSharp_FieldAccess()
        {
            var source = @"
class C
{
    public int F;
    void M()
    {
        F = F;
    }
}";
            var bannedText = @"C";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(7, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"),
                GetCSharpResultAt(7, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"));
        }

        [Fact]
        public void CSharp_EventAccess()
        {
            var source = @"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        E += null;
        E -= null;
        E(null, EventArgs.Empty);
    }
}";
            var bannedText = @"C";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(9, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"),
                GetCSharpResultAt(10, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"),
                GetCSharpResultAt(11, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"));
        }

        [Fact]
        public void CSharp_MethodReference()
        {
            var source = @"
delegate void D();
class C
{
    void M()
    {
        D d = M;
    }
}
";
            var bannedText = @"C";

            VerifyCSharp(source, bannedText,
                GetCSharpResultAt(7, 15, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"));
        }

        [Fact]
        public void VisualBasic_DiagnosticReportedForTypeInSource()
        {
            var source = @"
Namespace N
    Class Banned : End Class
    Class C
        Sub M()
            Dim c As New Banned()
        End Sub
    End Class
End Namespace";

            var bannedText = @"
N.Banned";

            VerifyBasic(source, bannedText, GetBasicResultAt(6, 22, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "Banned"));
        }

        [Fact]
        public void VisualBasic_DiagnosticReportedForGenericTypeFromMetadata()
        {
            var source = @"
Class C
    Sub M()
        Dim c = New System.Collections.Generic.List(Of String)()
    End Sub
End Class";

            var bannedText = @"
System.Collections.Generic.List`1";

            VerifyBasic(source, bannedText, GetBasicResultAt(4, 17, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "List(Of T)"));
        }

        [Fact]
        public void VisualBasic_DiagnosticReportedForNestedType()
        {
            var source = @"
Class C
    Class Nested : End Class
    Sub M()
        Dim n As New Nested()
    End Sub
End Class";

            var bannedText = @"
C+Nested";

            VerifyBasic(source, bannedText, GetBasicResultAt(5, 18, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C.Nested"));
        }

        [Fact]
        public void VisualBasic_DiagnosticReportedForNestedType2()
        {
            var source = @"
Class C
    Public Class Nested
        Public Shared Sub M() : End Sub
    End Class
End Class

Class D
    Sub M2()
        C.Nested.M()
    End Sub
End Class
";
            var bannedText = @"
C";

            VerifyBasic(source, bannedText, GetBasicResultAt(10, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"));
        }

        [Fact]
        public void VisualBasic_InvocationOfMethodOnInterface()
        {
            var source = @"
Interface I
    Sub M()
End Interface

Class C
    Sub M()
        Dim i As I = Nothing
        i.M()
    End Sub
End Class";
            var bannedText = @"I";

            VerifyBasic(source, bannedText, GetBasicResultAt(9, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "I"));
        }

        [Fact]
        public void VisualBasic_PropertyAccess()
        {
            var source = @"
Class C
    Public Property P As Integer
    Sub M()
        P = P
    End Sub
End Class";
            var bannedText = @"C";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(5, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"),
                GetBasicResultAt(5, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"));
        }

        [Fact]
        public void VisualBasic_FieldAccess()
        {
            var source = @"
Class C
    Public F As Integer
    Sub M()
        F = F
    End Sub
End Class";
            var bannedText = @"C";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(5, 9, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"),
                GetBasicResultAt(5, 13, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"));
        }

        [Fact]
        public void VisualBasic_EventAccess()
        {
            var source = @"
Imports System

Class C
    public Event E As EventHandler
    Sub M()
        AddHandler E, Nothing
        RemoveHandler E, Nothing
        RaiseEvent E(Me, EventArgs.Empty)
    End Sub
End Class";
            var bannedText = @"C";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(7, 20, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"),
                GetBasicResultAt(8, 23, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"),
                GetBasicResultAt(9, 20, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"));
        }

        [Fact]
        public void VisualBasic_MethodReference()
        {
            var source = @"
Delegate Sub D()
Class C
    Sub M()
        Dim d as D = AddressOf M
    End Sub
End Class";
            var bannedText = @"C";

            VerifyBasic(source, bannedText,
                GetBasicResultAt(5, 22, SymbolIsBannedAnalyzer.SymbolIsBannedRule, "C"));
        }
        #endregion
    }
}
