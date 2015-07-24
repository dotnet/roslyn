// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Performance;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Performance
{
    public class BigForOperationAnalyzerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() { return new BigForTestAnalyzer(); }
        protected override CodeFixProvider GetCSharpCodeFixProvider() { return null; }
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer() { return new BigForTestAnalyzer(); }
        protected override CodeFixProvider GetBasicCodeFixProvider() { return null; }

        [Fact]
        public void BigForCSharp()
        {
            const string Source = @"
class C
{
    public void M1()
    {
        int x;
        for (x = 0; x < 200000; x++)
        {
        }

        for (x = 0; x < 2000000; x++)
        {
        }

        for (x = 1500000; x > 0; x -= 2)
        {
        }

        for (x = 3000000; x > 0; x -= 2)
        {
        }

        for (x = 0; x < 200000; x = x + 1)
        {
        }

        for (x = 0; x < 2000000; x = x + 1)
        {
        }
    }
}
";

            VerifyCSharp(Source, new[]
            {
                GetCSharpResultAt(11, 9, BigForTestAnalyzer.BigForDescriptor),
                GetCSharpResultAt(19, 9, BigForTestAnalyzer.BigForDescriptor),
                GetCSharpResultAt(27, 9, BigForTestAnalyzer.BigForDescriptor)
            });
        }

        [Fact]
        public void BigForVisualBasic()
        {
            const string Source = @"
Class C
    Public Sub M1()
        Dim x as Integer
        For x = 1 To 200000
        Next
        For x = 1 To 2000000
        Next
        For x = 1500000 To 0 Step -2
        Next
        For x = 3000000 To 0 Step -2
        Next
    End Sub
End Class
";

            VerifyBasic(Source, new[]
            {
                GetBasicResultAt(7, 9, BigForTestAnalyzer.BigForDescriptor),
                GetBasicResultAt(11, 9, BigForTestAnalyzer.BigForDescriptor)
            });
        }
    }

    public class SparseSwitchOperationAnalyzerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() { return new SparseSwitchTestAnalyzer(); }
        protected override CodeFixProvider GetCSharpCodeFixProvider() { return null; }
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer() { return new SparseSwitchTestAnalyzer(); }
        protected override CodeFixProvider GetBasicCodeFixProvider() { return null; }

        [Fact]
        public void SparseSwitchCSharp()
        {
            const string Source = @"
class C
{
    public void M1(int x)
    {
        switch (x)
        {
            case 1:
                break;
            case 10:
                break;
            default:
                break;
        }

        switch (x)
        {
            case 1:
                break;
            case 1000:
                break;
            default:
                break;
        }
    }
}
";

            VerifyCSharp(Source, new[]
            {
                GetCSharpResultAt(16, 9, SparseSwitchTestAnalyzer.SparseSwitchDescriptor)
            });
        }

        [Fact]
        public void SparseSwitchVisualBasic()
        {
            const string Source = @"
Class C
    Public Sub M1(x As Integer)
        Select Case x
            Case 1, 2
                Exit Select
            Case = 10
                Exit Select
            Case Else
                Exit Select
        End Select Case

        Select Case x
            Case 1
                Exit Select
            Case = 1000
                Exit Select
            Case Else
                Exit Select
        End Select Case

        Select Case x
            Case 10 To 500
                Exit Select
            Case = 1000
                Exit Select
            Case Else
                Exit Select
        End Select Case

        Select Case x
            Case 1, 980 To 985
                Exit Select
            Case Else
                Exit Select
        End Select Case

        Select Case x
            Case 1 to 3, 980 To 985
                Exit Select
        End Select Case

         Select Case x
            Case 1
                Exit Select
            Case > 100000
                Exit Select
        End Select Case
    End Sub
End Class
";

            VerifyBasic(Source, new[]
            {
                GetBasicResultAt(13, 9, SparseSwitchTestAnalyzer.SparseSwitchDescriptor),
                GetBasicResultAt(31, 9, SparseSwitchTestAnalyzer.SparseSwitchDescriptor),
                GetBasicResultAt(38, 9, SparseSwitchTestAnalyzer.SparseSwitchDescriptor)
            });
        }
    }
}
