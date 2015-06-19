// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    [WorkItem(858659)]
    public class UseOrdinalStringComparisonFixerTests : CodeFixTestBase
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

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CSharpUseOrdinalStringComparisonFixer();
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new BasicUseOrdinalStringComparisonFixer();
        }

        #endregion

        #region Code fix tests

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309FixOperatorCSharp()
        {
            VerifyCSharpFix(@"
class C
{
    void M(string a, string b)
    {
        if (a == b) { }
        if (a != b) { }
        var c = a == b/*comment*/;
    }
}
", @"
class C
{
    void M(string a, string b)
    {
        if (string.Equals(a, b, System.StringComparison.Ordinal)) { }
        if (!string.Equals(a, b, System.StringComparison.Ordinal)) { }
        var c = string.Equals(a, b, System.StringComparison.Ordinal)/*comment*/;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309FixOperatorBasic()
        {
            VerifyBasicFix(@"
Class C
    Sub M(a As String, b As String)
        If a = b Then
        End If
        If a <> b Then
        End If
        Dim c = a = b'comment
    End Sub
End Class
", @"
Class C
    Sub M(a As String, b As String)
        If String.Equals(a, b, System.StringComparison.Ordinal) Then
        End If
        If Not String.Equals(a, b, System.StringComparison.Ordinal) Then
        End If
        Dim c = String.Equals(a, b, System.StringComparison.Ordinal) 'comment
    End Sub
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309FixStaticEqualsOverloadCSharp()
        {
            VerifyCSharpFix(@"
class C
{
    void M(string a, string b)
    {
        if (string.Equals(a, b)) { }
        if (string.Equals(a, b, System.StringComparison.CurrentCulture)) { }
        if (string.Equals(a, b, System.StringComparison.CurrentCultureIgnoreCase)) { }
    }
}
", @"
class C
{
    void M(string a, string b)
    {
        if (string.Equals(a, b, System.StringComparison.Ordinal)) { }
        if (string.Equals(a, b, System.StringComparison.Ordinal)) { }
        if (string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase)) { }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309FixStaticEqualsOverloadBasic()
        {
            VerifyBasicFix(@"
Class C
    Sub M(a As String, b As String)
        If String.Equals(a, b) Then
        End If
        If String.Equals(a, b, System.StringComparison.CurrentCulture) Then
        End If
        If String.Equals(a, b, System.StringComparison.CurrentCultureIgnoreCase) Then
        End If
    End Sub
End Class
", @"
Class C
    Sub M(a As String, b As String)
        If String.Equals(a, b, System.StringComparison.Ordinal) Then
        End If
        If String.Equals(a, b, System.StringComparison.Ordinal) Then
        End If
        If String.Equals(a, b, System.StringComparison.OrdinalIgnoreCase) Then
        End If
    End Sub
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309FixInstanceEqualsOverloadCSharp()
        {
            VerifyCSharpFix(@"
class C
{
    void M(string a, string b)
    {
        if (a.Equals(15)) { }
        if (a.Equals(b)) { }
        if (a.Equals(b, System.StringComparison.Ordinal)) { }
        if (a.Equals(b, System.StringComparison.CurrentCulture)) { }
    }
}
", @"
class C
{
    void M(string a, string b)
    {
        if (a.Equals(15)) { }
        if (a.Equals(b, System.StringComparison.Ordinal)) { }
        if (a.Equals(b, System.StringComparison.Ordinal)) { }
        if (a.Equals(b, System.StringComparison.Ordinal)) { }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309FixInstanceEqualsOverloadBasic()
        {
            VerifyBasicFix(@"
Class C
    Sub M(a As String, b As String)
        If a.Equals(15) Then
        End If
        If a.Equals(b) Then
        End If
        If a.Equals(b, System.StringComparison.Ordinal) Then
        End If
        If a.Equals(b, System.StringComparison.CurrentCulture) Then
        End If
    End Sub
End Class
", @"
Class C
    Sub M(a As String, b As String)
        If a.Equals(15) Then
        End If
        If a.Equals(b, System.StringComparison.Ordinal) Then
        End If
        If a.Equals(b, System.StringComparison.Ordinal) Then
        End If
        If a.Equals(b, System.StringComparison.Ordinal) Then
        End If
    End Sub
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309FixStaticCompareOverloadCSharp()
        {
            VerifyCSharpFix(@"
class C
{
    void M(string a, string b)
    {
        System.Globalization.CultureInfo ci;
        System.Globalization.CompareOptions co;

        // add or correct StringComparison
        if (string.Compare(a, b) == 0) { }
        if (string.Compare(a, 0, b, 0, 0) == 0) { }
        if (string.Compare(a, b, System.StringComparison.CurrentCulture) == 0) { }
        if (string.Compare(a, b, System.StringComparison.CurrentCultureIgnoreCase) == 0) { }
        if (string.Compare(a, 0, b, 0, 0, System.StringComparison.CurrentCulture) == 0) { }

        // these can't be auto-fixed
        if (string.Compare(a, b, true) == 0) { }
        if (string.Compare(a, b, true, ci) == 0) { }
        if (string.Compare(a, b, ci, co) == 0) { }
        if (string.Compare(a, 0, b, 0, 0, true) == 0) { }
        if (string.Compare(a, 0, b, 0, 0, true, ci) == 0) { }
        if (string.Compare(a, 0, b, 0, 0, ci, co) == 0) { }
    }
}
", @"
class C
{
    void M(string a, string b)
    {
        System.Globalization.CultureInfo ci;
        System.Globalization.CompareOptions co;

        // add or correct StringComparison
        if (string.Compare(a, b, System.StringComparison.Ordinal) == 0) { }
        if (string.Compare(a, 0, b, 0, 0, System.StringComparison.Ordinal) == 0) { }
        if (string.Compare(a, b, System.StringComparison.Ordinal) == 0) { }
        if (string.Compare(a, b, System.StringComparison.OrdinalIgnoreCase) == 0) { }
        if (string.Compare(a, 0, b, 0, 0, System.StringComparison.Ordinal) == 0) { }

        // these can't be auto-fixed
        if (string.Compare(a, b, true) == 0) { }
        if (string.Compare(a, b, true, ci) == 0) { }
        if (string.Compare(a, b, ci, co) == 0) { }
        if (string.Compare(a, 0, b, 0, 0, true) == 0) { }
        if (string.Compare(a, 0, b, 0, 0, true, ci) == 0) { }
        if (string.Compare(a, 0, b, 0, 0, ci, co) == 0) { }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1309FixStaticCompareOverloadBasic()
        {
            VerifyBasicFix(@"
Class C
    Sub M(a As String, b As String)
        Dim ci As System.Globalization.CultureInfo
        Dim co As System.Globalization.CompareOptions

        ' add or correct StringComparison
        If String.Compare(a, b) = 0 Then
        End If
        If String.Compare(a, 0, b, 0, 0) = 0 Then
        End If
        If String.Compare(a, b, System.StringComparison.CurrentCulture) = 0 Then
        End If
        If String.Compare(a, b, System.StringComparison.CurrentCultureIgnoreCase) = 0 Then
        End If
        If String.Compare(a, 0, b, 0, 0, System.StringComparison.CurrentCulture) = 0 Then
        End If

        ' these can't be auto-fixed
        If String.Compare(a, b, True) = 0 Then
        End If
        If String.Compare(a, b, True, ci) = 0 Then
        End If
        If String.Compare(a, b, ci, co) = 0 Then
        End If
        If String.Compare(a, 0, b, 0, 0, True) = 0 Then
        End If
        If String.Compare(a, 0, b, 0, 0, True, ci) = 0 Then
        End If
        If String.Compare(a, 0, b, 0, 0, ci, co) = 0 Then
        End If
    End Sub
End Class
", @"
Class C
    Sub M(a As String, b As String)
        Dim ci As System.Globalization.CultureInfo
        Dim co As System.Globalization.CompareOptions

        ' add or correct StringComparison
        If String.Compare(a, b, System.StringComparison.Ordinal) = 0 Then
        End If
        If String.Compare(a, 0, b, 0, 0, System.StringComparison.Ordinal) = 0 Then
        End If
        If String.Compare(a, b, System.StringComparison.Ordinal) = 0 Then
        End If
        If String.Compare(a, b, System.StringComparison.OrdinalIgnoreCase) = 0 Then
        End If
        If String.Compare(a, 0, b, 0, 0, System.StringComparison.Ordinal) = 0 Then
        End If

        ' these can't be auto-fixed
        If String.Compare(a, b, True) = 0 Then
        End If
        If String.Compare(a, b, True, ci) = 0 Then
        End If
        If String.Compare(a, b, ci, co) = 0 Then
        End If
        If String.Compare(a, 0, b, 0, 0, True) = 0 Then
        End If
        If String.Compare(a, 0, b, 0, 0, True, ci) = 0 Then
        End If
        If String.Compare(a, 0, b, 0, 0, ci, co) = 0 Then
        End If
    End Sub
End Class
");
        }

        #endregion
    }
}
