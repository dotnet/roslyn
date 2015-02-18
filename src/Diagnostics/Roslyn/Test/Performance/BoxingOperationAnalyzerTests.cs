// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Performance;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Performance
{
    public class BoxingOperationAnalyzerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() { return new BoxingOperationAnalyzer(); }
        protected override CodeFixProvider GetCSharpCodeFixProvider() { return null; }
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer() { return new BoxingOperationAnalyzer(); }
        protected override CodeFixProvider GetBasicCodeFixProvider() { return null; }

        [Fact]
        public void BoxingCSharp()
        {
            const string Source = @"
class C
{
    public object M1(object p1, object p2, object p3)
    {
         S v1 = new S();
         S v2 = v1;
         S v3 = v1.M1(v2);
         object v4 = M1(3, this, v1);
         object v5 = v3;
         if (p1 == null)
         {
             return 3;
         }
         if (p2 == null)
         {
             return v3;
         }
         if (p3 == null)
         {
             return v4;
         }
         return v5;
    }
}

struct S
{
    public int X;
    public int Y;
    public object Z;

    public S M1(S p1)
    {
        p1.GetType();
        Z = this;
        return p1;
    }
}";

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
        public void BoxingVisualBasic()
        {
            const string Source = @"
Class C
    Public Function M1(p1 As Object, p2 As Object, p3 As Object) As Object
         Dim v1 As New S
         Dim v2 As S = v1
         Dim v3 As S = v1.M1(v2)
         Dim v4 As Object = M1(3, Me, v1)
         Dim v5 As Object = v3
         If p1 Is Nothing
             return 3
         End If
         If p2 Is Nothing
             return v3
         End If
         If p3 Is Nothing
             Return v4
         End If
         Return v5
    End Function
End Class

Structure S
    Public X As Integer
    Public Y As Integer
    Public Z As Object

    Public Function M1(p1 As S) As S
        p1.GetType()
        Z = Me
        Return p1
    End Function
End Structure";

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
