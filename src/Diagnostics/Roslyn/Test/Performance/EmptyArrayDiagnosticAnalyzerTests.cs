// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Performance;
using System.Reflection;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Performance
{
    public class EmptyArrayDiagnosticAnalyzerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() { return new CSharpEmptyArrayDiagnosticAnalyzer(); }
        protected override CodeFixProvider GetCSharpCodeFixProvider() { return new CSharpEmptyArrayCodeFixProvider(); }
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer() { return new BasicEmptyArrayDiagnosticAnalyzer(); }
        protected override CodeFixProvider GetBasicCodeFixProvider() { return new BasicEmptyArrayCodeFixProvider(); }

        /// <summary>
        /// This type isn't defined in all locations where this test runs.  Need to alter the 
        /// test code slightly to account for this.
        /// </summary>
        private static bool IsArrayEmptyDefined()
        {
            var assembly = typeof(object).Assembly;
            var type = assembly.GetType("System.Array");
            return type.GetMethod("Empty", BindingFlags.Public | BindingFlags.Static) != null;
        }

        [Fact]
        public void EmptyArrayCSharp()
        {
            const string arrayEmptySourceRaw =
                @"namespace System { public class Array { public static T[] Empty<T>() { return null; } } }";

            const string badSource = @"
[System.Runtime.CompilerServices.Dynamic(new bool[0])] // no
class C
{
    unsafe void M1()
    {
        int[] arr1 = new int[0];                       // yes
        byte[] arr2 = { };                             // yes
        C[] arr3 = new C[] { };                        // yes
        string[] arr4 = new string[] { null };         // no
        double[] arr5 = new double[1];                 // no
        int[] arr6 = new[] { 1 };                      // no
        int[][] arr7 = new int[0][];                   // yes
        int[][][][] arr8 = new int[0][][][];           // yes
        int[,] arr9 = new int[0,0];                    // no
        int[][,] arr10 = new int[0][,];                // yes
        int[][,] arr11 = new int[1][,];                // no
        int[,][] arr12 = new int[0,0][];               // no
        int*[] arr13 = new int*[0];                    // no
        List<int> list1 = new List<int>() { }          // no
    }
}";

            const string fixedSource = @"
[System.Runtime.CompilerServices.Dynamic(new bool[0])] // no
class C
{
    unsafe void M1()
    {
        int[] arr1 = System.Array.Empty<int>();                       // yes
        byte[] arr2 = System.Array.Empty<byte>();                             // yes
        C[] arr3 = System.Array.Empty<C>();                        // yes
        string[] arr4 = new string[] { null };         // no
        double[] arr5 = new double[1];                 // no
        int[] arr6 = new[] { 1 };                      // no
        int[][] arr7 = System.Array.Empty<int[]>();                   // yes
        int[][][][] arr8 = System.Array.Empty<int[][][]>();           // yes
        int[,] arr9 = new int[0,0];                    // no
        int[][,] arr10 = System.Array.Empty<int[,]>();                // yes
        int[][,] arr11 = new int[1][,];                // no
        int[,][] arr12 = new int[0,0][];               // no
        int*[] arr13 = new int*[0];                    // no
        List<int> list1 = new List<int>() { }          // no
    }
}";
            var arrayEmptySource = IsArrayEmptyDefined() ? string.Empty : arrayEmptySourceRaw;

            VerifyCSharp(badSource + arrayEmptySource, new[]
            {
                GetCSharpResultAt(7, 22, EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor),
                GetCSharpResultAt(8, 23, EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor),
                GetCSharpResultAt(9, 20, EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor),
                GetCSharpResultAt(13, 24, EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor),
                GetCSharpResultAt(14, 28, EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor),
                GetCSharpResultAt(16, 26, EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor)
            });
            VerifyCSharpFix(
                arrayEmptySource + badSource,
                arrayEmptySource + fixedSource,
                allowNewCompilerDiagnostics: true);
            VerifyCSharpFix(
                "using System;\r\n" + arrayEmptySource + badSource,
                "using System;\r\n" + arrayEmptySource + fixedSource.Replace("System.Array.Empty", "Array.Empty"),
                allowNewCompilerDiagnostics: true);
        }

        [Fact]
        public void EmptyArrayVisualBasic()
        {
            const string arrayEmptySourceRaw = @"
Namespace System
    Public Class Array
       Public Shared Function Empty(Of T)() As T()
           Return Nothing
       End Function
    End Class
End Namespace
";
            const string badSource = @"
<System.Runtime.CompilerServices.Dynamic(new Boolean(-1) {})> _
Class C
    Sub M1()
        Dim arr1 As Integer() = New Integer(-1) { }               ' yes
        Dim arr2 As Byte() = { }                                  ' yes
        Dim arr3 As C() = New C(-1) { }                           ' yes
        Dim arr4 As String() = New String() { Nothing }           ' no
        Dim arr5 As Double() = New Double(1) { }                  ' no
        Dim arr6 As Integer() = { -1 }                            ' no
        Dim arr7 as Integer()() = New Integer(-1)() { }           ' yes
        Dim arr8 as Integer()()()() = New Integer(  -1)()()() { } ' yes
        Dim arr9 as Integer(,) = New Integer(-1,-1) { }           ' no
        Dim arr10 as Integer()(,) = New Integer(-1)(,) { }        ' yes
        Dim arr11 as Integer()(,) = New Integer(1)(,) { }         ' no
        Dim arr12 as Integer(,)() = New Integer(-1,-1)() { }      ' no
        Dim arr13 as Integer() = New Integer(0) { }               ' no
        Dim list1 as List(Of Integer) = New List(Of Integer) From { }  ' no
    End Sub
End Class";

            const string fixedSource = @"
<System.Runtime.CompilerServices.Dynamic(new Boolean(-1) {})> _
Class C
    Sub M1()
        Dim arr1 As Integer() = System.Array.Empty(Of Integer)               ' yes
        Dim arr2 As Byte() = System.Array.Empty(Of Byte)                                  ' yes
        Dim arr3 As C() = System.Array.Empty(Of C)                           ' yes
        Dim arr4 As String() = New String() { Nothing }           ' no
        Dim arr5 As Double() = New Double(1) { }                  ' no
        Dim arr6 As Integer() = { -1 }                            ' no
        Dim arr7 as Integer()() = System.Array.Empty(Of Integer())           ' yes
        Dim arr8 as Integer()()()() = System.Array.Empty(Of Integer()()()) ' yes
        Dim arr9 as Integer(,) = New Integer(-1,-1) { }           ' no
        Dim arr10 as Integer()(,) = System.Array.Empty(Of Integer(,))        ' yes
        Dim arr11 as Integer()(,) = New Integer(1)(,) { }         ' no
        Dim arr12 as Integer(,)() = New Integer(-1,-1)() { }      ' no
        Dim arr13 as Integer() = New Integer(0) { }               ' no
        Dim list1 as List(Of Integer) = New List(Of Integer) From { }  ' no
    End Sub
End Class";

            var arrayEmptySource = IsArrayEmptyDefined() ? string.Empty : arrayEmptySourceRaw;

            VerifyBasic(badSource + arrayEmptySource, new[]
            {
                GetBasicResultAt(5, 33, EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor),
                GetBasicResultAt(6, 30, EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor),
                GetBasicResultAt(7, 27, EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor),
                GetBasicResultAt(11, 35, EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor),
                GetBasicResultAt(12, 39, EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor),
                GetBasicResultAt(14, 37, EmptyArrayDiagnosticAnalyzer.UseArrayEmptyDescriptor)
            });
            VerifyBasicFix(
                arrayEmptySource + badSource,
                arrayEmptySource + fixedSource,
                allowNewCompilerDiagnostics: true);
            VerifyBasicFix(
                "Imports System\r\n" + arrayEmptySource + badSource,
                "Imports System\r\n" + arrayEmptySource + fixedSource.Replace("System.Array.Empty", "Array.Empty"),
                allowNewCompilerDiagnostics: true);
        }
    }
}
