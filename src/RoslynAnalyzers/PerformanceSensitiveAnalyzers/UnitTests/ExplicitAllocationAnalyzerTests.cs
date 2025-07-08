// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;
using Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests.CSharpPerformanceCodeFixVerifier<
    Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.ExplicitAllocationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests.VisualBasicPerformanceCodeFixVerifier<
    Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.ExplicitAllocationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers.UnitTests
{
    public class ExplicitAllocationAnalyzerTests
    {
        [Fact]
        public async Task ExplicitAllocation_ObjectInitializerAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var @class = new TestClass { Name = ""Bob"" };
    }
}

public class TestClass
{
    public string Name { get; set; }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 22));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectInitializer_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(
                @"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim instance = New TestClass With {.Name = ""Bob""}
    End Sub
End Class

Public Class TestClass
    Public Property Name As String
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 24));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectInitializerStruct_NoWarningAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var @struct = new TestStruct { Name = ""Bob"" };
    }
}

public struct TestStruct
{
    public string Name { get; set; }
}");
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectInitializerStruct_NoWarning_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim instance = New TestClass With {.Name = ""Bob""}
    End Sub
End Class

Public Structure TestClass
    Public Property Name As String
End Structure");
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitArrayCreationAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System.Collections.Generic;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        int[] intData = new[] { 123, 32, 4 };
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(9, 25));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitArrayCreation_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System.Collections.Generic
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim intData() As Integer = {123, 32, 4}
    End Sub
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(7, 36));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_AnonymousObjectCreationAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var temp = new { A = 123, Name = ""Test"", };
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.AnonymousObjectCreationRule).WithLocation(9, 20));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_AnonymousObjectCreation_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim temp = New With {Key .B = 123, .Name = ""Test""}
    End Sub
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.AnonymousObjectCreationRule).WithLocation(7, 20));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ArrayCreationAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System.Collections.Generic;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        int[] intData = new int[] { 123, 32, 4 };
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(9, 25));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ArrayCreation_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System.Collections.Generic
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim intData = New Integer() {123, 32, 4}
    End Sub
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(7, 23));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectCreationAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var allocation = new String('a', 10);
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 26));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectCreation_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim allocation = New String(""a""c, 10)
    End Sub
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 26));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_LetClauseAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        int[] intData = new[] { 123, 32, 4 };
        var result = (from a in intData
                      let b = a * 3
                      select b).ToList();
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(10, 25),
#pragma warning restore RS0030 // Do not use banned APIs
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.LetCauseRule).WithLocation(12, 23));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_LetClause_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System.Collections.Generic
Imports System.Linq
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim intData() As Integer = {123, 32, 4}
        Dim result = (From x In intData
                      Let b = x * 3
                      Select b).ToList()
    End Sub
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(8, 36),
#pragma warning restore RS0030 // Do not use banned APIs
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.LetCauseRule).WithLocation(10, 27));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_Object_typeAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using Roslyn.Utilities;

public struct S { }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        object box = new S();
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(11, 22));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_Object_type_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"
Imports Roslyn.Utilities

Public Structure S
End Structure

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub SomeMethod()
        Dim box As Object = new S()
    End Sub
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(10, 29));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_ValueType_typeAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using Roslyn.Utilities;

public struct S { }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        System.ValueType box = new S();
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(11, 32));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_ValueType_type_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"
Imports Roslyn.Utilities

Public Structure S
End Structure

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub SomeMethod()
        Dim box As System.ValueType = new S()
    End Sub
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(10, 39));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_into_interface_referenceAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using Roslyn.Utilities;

interface I { }

public struct S : I { }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        I box = new S();
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(13, 17));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_into_interface_reference_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"
Imports Roslyn.Utilities

Interface I
End Interface

Public Structure S
    Implements I
End Structure

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub SomeMethod()
        Dim box As I = new S()
    End Sub
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(14, 24));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_StructCreation_NoWarningAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public struct S { }

public class MyClass
{

    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var noBox1 = new DateTime();
        S noBox2 = new S();
    }
}");
        }

        [Fact]
        public async Task ExplicitAllocation_StructCreation_NoWarning_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System
Imports Roslyn.Utilities

Public Structure S
End Structure

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim noBox1 = new DateTime()
        Dim noBox2 As S = new S()
    End Sub
End Class");
        }

        [Fact]
        public async Task ExplicitAllocation_PrimitiveTypeConversion_NoWarningAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        double x = new int();
    }
}");
        }

        [Fact]
        public async Task ExplicitAllocation_PrimitiveTypeConversion_NoWarning_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim x As Double = New Integer()
    End Sub
End Class");
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitValueTypeConversion_NoWarningAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

struct A
{
    public static implicit operator A(B other)
    {
        return new A();
    }
}

struct B
{
}

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        A a = new B();
    }
}");
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitValueTypeConversion_NoWarning_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System
Imports Roslyn.Utilities

Structure A
    Public Shared Widening Operator CType(other As B) As A
        Return New A()
    End Operator
End Structure

Structure B
End Structure

Public Class C
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim a As A = New B()
    End Sub
End Class");
        }

        [Fact]
        public async Task ExplicitAllocation_NoParamsArrayCreationAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System.Collections.Generic;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing(params int[] values)
    {
        Testing();
    }
}");
        }

        [Fact]
        public async Task ExplicitAllocation_NoParamsArrayCreation_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System.Collections.Generic
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing(ParamArray values() As Integer)
        Testing()
    End Sub
End Class");
        }

        [Fact]
        public async Task ExplicitAllocation_ExplicitDelegateCreationAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing(object sender, EventArgs e)
    {
        var handler = new EventHandler(Testing);
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 23));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ExplicitDelegateCreation_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing(sender As Object, e As EventArgs)
        Dim handler = new EventHandler(AddressOf Testing)
    End Sub
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 23));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitDelegateCreationAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing(object sender, EventArgs e)
    {
        EventHandler handler = Testing;
    }
}");
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitDelegateCreation_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing(sender As Object, e As EventArgs)
        Dim handler As EventHandler = AddressOf Testing
    End Sub
End Class");
        }

        [Fact]
        public async Task ExplicitAllocation_ListInitializerCreationAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System.Collections.Generic;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var intData = new List<int> { 3, 4 };
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 23));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ListInitializerCreation_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System.Collections.Generic
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim intData = New List(Of Integer) From {3, 4}
    End Sub
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 23));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreationAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing<T>()
        where T : class, new()
    {
        var allocation = new T();
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(10, 26));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing(Of T As {Class, New})()
        Dim allocation = New T()
    End Sub
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 26));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation2Async()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing<T>()
        where T : struct
    {
        object allocation = new T();
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(10, 29));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation2_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing(Of T As Structure)()
        Dim allocation As Object = New T()
    End Sub
End Class",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 36));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation3Async()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing<T>()
        where T : struct
    {
        T value = new T();
    }
}");
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation3_VisualBasicAsync()
        {
            await VerifyVB.VerifyAnalyzerAsync(@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing(Of T As Structure)()
        Dim value As T = new T()
    End Sub
End Class");
        }
    }
}
