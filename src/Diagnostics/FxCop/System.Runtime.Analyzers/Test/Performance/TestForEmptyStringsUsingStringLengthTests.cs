// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    public class TestForEmptyStringsUsingStringLengthTests : DiagnosticAnalyzerTestBase
    {
        #region Helper methods

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpTestForEmptyStringsUsingStringLengthAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicTestForEmptyStringsUsingStringLengthAnalyzer();
        }

        private static DiagnosticResult CSharpResult(int line, int column)
        {
            return GetCSharpResultAt(line, column, CSharpTestForEmptyStringsUsingStringLengthAnalyzer.RuleId, SystemRuntimeAnalyzersResources.TestForEmptyStringsUsingStringLength);
        }

        private static DiagnosticResult BasicResult(int line, int column)
        {
            return GetBasicResultAt(line, column, BasicTestForEmptyStringsUsingStringLengthAnalyzer.RuleId, SystemRuntimeAnalyzersResources.TestForEmptyStringsUsingStringLength);
        }

        #endregion

        #region Diagnostic tests 

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1820StaticEqualsTestCSharp()
        {
            VerifyCSharp(@"
using System;

class C
{
    void Method()
    {
        string a;

        // equality with empty string
        string.Equals(a, """");
        string.Equals(a, """", StringComparison.CurrentCulture);
        string.Equals("""", a, StringComparison.Ordinal);

        // equality with string.Empty
        string.Equals(a, string.Empty);
        string.Equals(a, string.Empty, StringComparison.CurrentCulture);
        string.Equals(string.Empty, a, StringComparison.Ordinal);
    }
}
",
                CSharpResult(11, 9),
                CSharpResult(12, 9),
                CSharpResult(13, 9),
                CSharpResult(16, 9),
                CSharpResult(17, 9),
                CSharpResult(18, 9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1820StaticEqualsTestBasic()
        {
            VerifyBasic(@"
Imports System

Class C
    Sub Method()
        Dim a As String

        ' equality with empty string
        String.Equals(a, """")
        String.Equals(a, """", StringComparison.CurrentCulture)
        String.Equals("""", a, StringComparison.Ordinal)

        ' equality with string.Empty
        String.Equals(a, String.Empty)
        String.Equals(a, String.Empty, StringComparison.CurrentCulture)
        String.Equals(String.Empty, a, StringComparison.Ordinal)
    End Sub
End Class
",
                BasicResult(9, 9),
                BasicResult(10, 9),
                BasicResult(11, 9),
                BasicResult(14, 9),
                BasicResult(15, 9),
                BasicResult(16, 9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1820InstanceEqualsTestCSharp()
        {
            VerifyCSharp(@"
using System;

class C
{
    void Method()
    {
        string a;

        // equality with empty string
        a.Equals("""");
        a.Equals("""", StringComparison.CurrentCulture);

        // equality with string.Empty
        a.Equals(string.Empty);
        a.Equals(string.Empty, StringComparison.CurrentCulture);
    }
}
",
                CSharpResult(11, 9),
                CSharpResult(12, 9),
                CSharpResult(15, 9),
                CSharpResult(16, 9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1820InstanceEqualsTestBasic()
        {
            VerifyBasic(@"
Imports System

Class C
    Sub Method()
        Dim a As String

        ' equality with empty string
        a.Equals("""")
        a.Equals("""", StringComparison.CurrentCulture)

        ' equality with string.Empty
        a.Equals(String.Empty)
        a.Equals(String.Empty, StringComparison.CurrentCulture)
    End Sub
End Class
",
                BasicResult(9, 9),
                BasicResult(10, 9),
                BasicResult(13, 9),
                BasicResult(14, 9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1820OperatorOverloadTestCSharp()
        {
            VerifyCSharp(@"
using System;

class C
{
    void Method()
    {
        string a;
        if (a == """") { }
        if ("""" != a) { }
        if (a == string.Empty) { }
        if (string.Empty != a) { }
    }
}
",
                CSharpResult(9, 13),
                CSharpResult(10, 13),
                CSharpResult(11, 13),
                CSharpResult(12, 13));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1820OperatorOverloadTestBasic()
        {
            VerifyBasic(@"
Imports System

Class C
    Sub Method()
        Dim a As String
        If a = """" Then
        End If
        If """" <> a Then
        End If
        If a = String.Empty Then
        End If
        If String.Empty <> a Then
        End If
    End Sub
End Class
",
                BasicResult(7, 12),
                BasicResult(9, 12),
                BasicResult(11, 12),
                BasicResult(13, 12));
        }

        #endregion
    }
}
