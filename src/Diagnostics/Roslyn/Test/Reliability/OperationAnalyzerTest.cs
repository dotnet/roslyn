// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Performance;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Performance
{
    public class OperationAnalyzerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() { return new OperationTestAnalyzer(); }
        protected override CodeFixProvider GetCSharpCodeFixProvider() { return null; }
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer() { return new OperationTestAnalyzer(); }
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
                GetCSharpResultAt(9, 25, BoxingOperationAnalyzer.BoxingDescriptor),
                GetCSharpResultAt(9, 34, BoxingOperationAnalyzer.BoxingDescriptor),
                GetCSharpResultAt(10, 22, BoxingOperationAnalyzer.BoxingDescriptor),
                GetCSharpResultAt(13, 21, BoxingOperationAnalyzer.BoxingDescriptor),
                GetCSharpResultAt(17, 21, BoxingOperationAnalyzer.BoxingDescriptor),
                GetCSharpResultAt(35, 9, BoxingOperationAnalyzer.BoxingDescriptor),
                GetCSharpResultAt(36, 13, BoxingOperationAnalyzer.BoxingDescriptor)
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
    End Function
End Class
";

            VerifyBasic(Source, new[]
            {
                GetBasicResultAt(7, 32, BoxingOperationAnalyzer.BoxingDescriptor),
                GetBasicResultAt(7, 39, BoxingOperationAnalyzer.BoxingDescriptor),
                GetBasicResultAt(8, 29, BoxingOperationAnalyzer.BoxingDescriptor),
                GetBasicResultAt(10, 21, BoxingOperationAnalyzer.BoxingDescriptor),
                GetBasicResultAt(13, 21, BoxingOperationAnalyzer.BoxingDescriptor),
                GetBasicResultAt(28, 9, BoxingOperationAnalyzer.BoxingDescriptor),
                GetBasicResultAt(29, 13, BoxingOperationAnalyzer.BoxingDescriptor)
            });
        }
    }
}
