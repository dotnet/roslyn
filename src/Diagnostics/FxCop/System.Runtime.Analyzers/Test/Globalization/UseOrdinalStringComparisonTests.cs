// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    [WorkItem(858659, "DevDiv")]
    public class UseOrdinalStringComparisonTests : DiagnosticAnalyzerTestBase
    {
        #region Helper methods

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpUseOrdinalStringComparisonAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicUseOrdinalStringComparisonAnalyzer();
        }

        private static DiagnosticResult CSharpResult(int line, int column)
        {
            return GetCSharpResultAt(line, column, UseOrdinalStringComparisonAnalyzer.RuleId, SystemRuntimeAnalyzersResources.StringComparisonShouldBeOrdinalOrOrdinalIgnoreCase);
        }

        private static DiagnosticResult BasicResult(int line, int column)
        {
            return GetBasicResultAt(line, column, UseOrdinalStringComparisonAnalyzer.RuleId, SystemRuntimeAnalyzersResources.StringComparisonShouldBeOrdinalOrOrdinalIgnoreCase);
        }

        #endregion

        #region Diagnostic tests 

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309CompareOverloadTestCSharp()
        {
            VerifyCSharp(@"
using System;
using System.Globalization;

class C
{
    void Method()
    {
        string a, b;
        // wrong overload
        string.Compare(a, b);
        string.Compare(a, b, true);
        string.Compare(a, b, true, default(CultureInfo));
        string.Compare(a, b, default(CultureInfo), default(CompareOptions));
        string.Compare(a, 0, b, 0, 0);
        string.Compare(a, 0, b, 0, 0, true);
        string.Compare(a, 0, b, 0, 0, true, default(CultureInfo));
        string.Compare(a, 0, b, 0, 0, default(CultureInfo), default(CompareOptions));
        System.String.Compare(a, b);
        // right overload, wrong value
        string.Compare(a, b, StringComparison.CurrentCulture);
        string.Compare(a, 0, b, 0, 0, StringComparison.CurrentCulture);
        // right overload, right value
        string.Compare(a, b, StringComparison.Ordinal);
        string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        string.Compare(a, 0, b, 0, 0, StringComparison.Ordinal);
        string.Compare(a, 0, b, 0, 0, StringComparison.OrdinalIgnoreCase);
    }
}
",
                CSharpResult(11, 16),
                CSharpResult(12, 16),
                CSharpResult(13, 16),
                CSharpResult(14, 16),
                CSharpResult(15, 16),
                CSharpResult(16, 16),
                CSharpResult(17, 16),
                CSharpResult(18, 16),
                CSharpResult(19, 23),
                CSharpResult(21, 30),
                CSharpResult(22, 39));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309CompareOverloadTestBasic()
        {
            VerifyBasic(@"
Imports System
Imports System.Globalization

Class C
    Sub Method()
        Dim a As String
        Dim b As String
        Dim ci As CultureInfo
        Dim co As CompareOptions
        ' wrong overload
        String.Compare(a, b)
        String.Compare(a, b, True)
        String.Compare(a, b, True, ci)
        String.Compare(a, b, ci, co)
        String.Compare(a, 0, b, 0, 0)
        String.Compare(a, 0, b, 0, 0, True)
        String.Compare(a, 0, b, 0, 0, True, ci)
        String.Compare(a, 0, b, 0, 0, ci, co)
        System.String.Compare(a, b)
        ' right overload, wrong value
        String.Compare(a, b, StringComparison.CurrentCulture)
        String.Compare(a, 0, b, 0, 0, StringComparison.CurrentCulture)
        ' right overload, right value
        String.Compare(a, b, StringComparison.Ordinal)
        String.Compare(a, b, StringComparison.OrdinalIgnoreCase)
        String.Compare(a, 0, b, 0, 0, StringComparison.Ordinal)
        String.Compare(a, 0, b, 0, 0, StringComparison.OrdinalIgnoreCase)
    End Sub
End Class
",
                BasicResult(12, 16),
                BasicResult(13, 16),
                BasicResult(14, 16),
                BasicResult(15, 16),
                BasicResult(16, 16),
                BasicResult(17, 16),
                BasicResult(18, 16),
                BasicResult(19, 16),
                BasicResult(20, 23),
                BasicResult(22, 30),
                BasicResult(23, 39));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309EqualsOverloadTestCSharp()
        {
            VerifyCSharp(@"
using System;

class C
{
    void Method()
    {
        string a, b;
        // wrong overload
        string.Equals(a, b); // (string, string) is bad
        // right overload, wrong value
        string.Equals(a, b, StringComparison.CurrentCulture);
        // right overload, right value
        string.Equals(a, b, StringComparison.Ordinal);
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        string.Equals(a, 15); // this is the (object, object) overload
    }
}
",
                CSharpResult(10, 16),
                CSharpResult(12, 29));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309EqualsOverloadTestBasic()
        {
            VerifyBasic(@"
Imports System

Class C
    Sub Method()
        Dim a As String, b As String
        ' wrong overload
        String.Equals(a, b) ' (String, String) is bad
        ' right overload, wrong value
        String.Equals(a, b, StringComparison.CurrentCulture)
        ' right overload, right value
        String.Equals(a, b, StringComparison.Ordinal)
        String.Equals(a, b, StringComparison.OrdinalIgnoreCase)
        String.Equals(a, 15) ' this is the (Object, Object) overload
    End Sub
End Class
",
                BasicResult(8, 16),
                BasicResult(10, 29));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309InstanceEqualsTestCSharp()
        {
            VerifyCSharp(@"
using System;

class C
{
    void Method()
    {
        string a, b;
        // wrong overload
        a.Equals(b);
        // right overload, wrong value
        a.Equals(b, StringComparison.CurrentCulture);
        // right overload, right value
        a.Equals(b, StringComparison.Ordinal);
        a.Equals(b, StringComparison.OrdinalIgnoreCase);
        a.Equals(15); // this is the (object) overload
    }
}
",
                CSharpResult(10, 11),
                CSharpResult(12, 21));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309InstanceEqualsTestBasic()
        {
            VerifyBasic(@"
Imports System

Class C
    Sub Method()
        Dim a As String, b As String
        ' wrong overload
        a.Equals(b)
        ' right overload, wrong value
        a.Equals(b, StringComparison.CurrentCulture)
        ' right overload, right value
        a.Equals(b, StringComparison.Ordinal)
        a.Equals(b, StringComparison.OrdinalIgnoreCase)
        a.Equals(15) ' this is the (Object) overload
    End Sub
End Class
",
                BasicResult(8, 11),
                BasicResult(10, 21));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309OperatorOverloadTestCSharp()
        {
            VerifyCSharp(@"
using System;

class C
{
    void Method()
    {
        string a, b;
        // not allowed
        if (a == b) { }
        if (a != b) { }
        // this is allowed
        if (a == null) { }
        if (null == a) { }
    }
}
",
                CSharpResult(10, 15),
                CSharpResult(11, 15));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309OperatorOverloadTestBasic()
        {
            VerifyBasic(@"
Imports System

Class C
    Sub Method()
        Dim a As String, b As String
        ' not allowed
        If a = b Then
        End If
        If a <> b Then
        End If
        ' this is allowed
        If a = Nothing Then
        End If
        If a Is Nothing Then
        End If
        If Nothing = a Then
        End If
    End Sub
End Class
",
                BasicResult(8, 14),
                BasicResult(10, 14));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309NotReallyCompareOrEqualsTestCSharp()
        {
            VerifyCSharp(@"
class C
{
    void Method()
    {
        string s = null;

        // verify extension methods don't trigger
        if (s.Equals(1, 2, 3)) { }
        if (s.Compare(1, 2, 3)) { }

        // verify other static string methods don't trigger
        string.Format(s);

        // verify other instance string methods don't trigger
        s.EndsWith(s);
    }
}

static class Extensions
{
    public static bool Equals(this string s, int a, int b, int c) { return false; }
    public static bool Compare(this string s, int a, int b, int c) { return false; }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309NotReallyCompareOrEqualsTestBasic()
        {
            VerifyBasic(@"
Imports System.Runtime.CompilerServices

Class C
    Sub Method()
        Dim s As String

        ' verify extension methods don't trigger
        If s.Equals(1, 2, 3) Then
        End If
        If s.Compare(1, 2) Then
        End If

        ' verify other static string methods don't trigger
        String.Format(s)

        ' verify other instance string methods don't trigger
        s.EndsWith(s)
    End Sub
End Class

Module Extensions
    <Extension>
    Public Function Equals(s As String, a As Integer, b As Integer, c As Integer) As Boolean
        Return False
    End Function
    <Extension()>
    Public Function Compare(s As String, a As Integer, b As Integer) As Boolean
        Return False
    End Function
End Module
");
        }

        #endregion
    }
}
