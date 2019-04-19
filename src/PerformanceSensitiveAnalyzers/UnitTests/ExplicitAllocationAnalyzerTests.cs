// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public async Task ExplicitAllocation_ObjectInitializer()
        {
            var sampleProgram =
    @"using System;
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
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 22));
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectInitializer_VisualBasic()
        {
            var code =
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
End Class";

            await VerifyVB.VerifyAnalyzerAsync(
                code,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 24));
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectInitializerStruct_NoWarning()
        {
            var sampleProgram =
    @"using System;
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
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectInitializerStruct_NoWarning_VisualBasic()
        {
            var code =
    @"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim instance = New TestClass With {.Name = ""Bob""}
    End Sub
End Class

Public Structure TestClass
    Public Property Name As String
End Structure";

            await VerifyVB.VerifyAnalyzerAsync(code);
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitArrayCreation()
        {
            var sampleProgram =
    @"using System.Collections.Generic;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        int[] intData = new[] { 123, 32, 4 };
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(9, 25));
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitArrayCreation_VisualBasic()
        {
            var sampleProgram =
    @"Imports System.Collections.Generic
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim intData() As Integer = {123, 32, 4}
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(7, 36));
        }

        [Fact]
        public async Task ExplicitAllocation_AnonymousObjectCreation()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var temp = new { A = 123, Name = ""Test"", };
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.AnonymousObjectCreationRule).WithLocation(9, 20));
        }

        [Fact]
        public async Task ExplicitAllocation_AnonymousObjectCreation_VisualBasic()
        {
            var sampleProgram =
@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim temp = New With {Key .B = 123, .Name = ""Test""}
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.AnonymousObjectCreationRule).WithLocation(7, 20));
        }

        [Fact]
        public async Task ExplicitAllocation_ArrayCreation()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        int[] intData = new int[] { 123, 32, 4 };
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(9, 25));
        }

        [Fact]
        public async Task ExplicitAllocation_ArrayCreation_VisualBasic()
        {
            var sampleProgram =
@"Imports System.Collections.Generic
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim intData = New Integer() {123, 32, 4}
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(7, 23));
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectCreation()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var allocation = new String('a', 10);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 26));
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectCreation_VisualBasic()
        {
            var sampleProgram =
@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim allocation = New String(""a""c, 10)
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 26));
        }

        [Fact]
        public async Task ExplicitAllocation_LetClause()
        {
            var sampleProgram =
@"using System.Collections.Generic;
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
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(10, 25),
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.LetCauseRule).WithLocation(12, 23));
        }

        [Fact]
        public async Task ExplicitAllocation_LetClause_VisualBasic()
        {
            var sampleProgram =
@"Imports System.Collections.Generic
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
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(8, 36),
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.LetCauseRule).WithLocation(10, 27));
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_Object_type()
        {
            var source = @"
using Roslyn.Utilities;

public struct S { }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Foo() 
    {
        object box = new S();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(11, 22));
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_Object_type_VisualBasic()
        {
            var source = @"
Imports Roslyn.Utilities

Public Structure S
End Structure

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Foo() 
        Dim box As Object = new S()
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(source,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(10, 29));
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_ValueType_type()
        {
            var source = @"
using Roslyn.Utilities;

public struct S { }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Foo() 
    {
        System.ValueType box = new S();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(11, 32));
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_ValueType_type_VisualBasic()
        {
            var source = @"
Imports Roslyn.Utilities

Public Structure S
End Structure

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Foo() 
        Dim box As System.ValueType = new S()
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(source,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(10, 39));
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_into_interface_reference()
        {
            var source = @"
using Roslyn.Utilities;

interface I { }

public struct S : I { }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Foo() 
    {
        I box = new S();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(13, 17));
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_into_interface_reference_VisualBasic()
        {
            var source = @"
Imports Roslyn.Utilities

Interface I
End Interface

Public Structure S
    Implements I
End Structure

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Foo() 
        Dim box As I = new S()
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(source,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(14, 24));
        }

        [Fact]
        public async Task ExplicitAllocation_StructCreation_NoWarning()
        {
            var sampleProgram =
@"using System;
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
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_StructCreation_NoWarning_VisualBasic()
        {
            var sampleProgram =
@"Imports System
Imports Roslyn.Utilities

Public Structure S
End Structure

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim noBox1 = new DateTime()
        Dim noBox2 As S = new S()
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_PrimitiveTypeConversion_NoWarning()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        double x = new int();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_PrimitiveTypeConversion_NoWarning_VisualBasic()
        {
            var sampleProgram =
@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim x As Double = New Integer()
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitValueTypeConversion_NoWarning()
        {
            var sampleProgram =
@"using System;
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
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitValueTypeConversion_NoWarning_VisualBasic()
        {
            var sampleProgram =
@"Imports System
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
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_NoParamsArrayCreation()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing(params int[] values)
    {
        Testing();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_NoParamsArrayCreation_VisualBasic()
        {
            var sampleProgram =
@"Imports System.Collections.Generic
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing(ParamArray values() As Integer)
        Testing()
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_ExplicitDelegateCreation()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing(object sender, EventArgs e)
    {
        var handler = new EventHandler(Testing);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 23));
        }

        [Fact]
        public async Task ExplicitAllocation_ExplicitDelegateCreation_VisualBasic()
        {
            var sampleProgram =
@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing(sender As Object, e As EventArgs)
        Dim handler = new EventHandler(AddressOf Testing)
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 23));
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitDelegateCreation()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing(object sender, EventArgs e)
    {
        EventHandler handler = Testing;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitDelegateCreation_VisualBasic()
        {
            var sampleProgram =
@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing(sender As Object, e As EventArgs)
        Dim handler As EventHandler = AddressOf Testing
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_ListInitializerCreation()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var intData = new List<int> { 3, 4 };
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 23));
        }

        [Fact]
        public async Task ExplicitAllocation_ListInitializerCreation_VisualBasic()
        {
            var sampleProgram =
@"Imports System.Collections.Generic
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing()
        Dim intData = New List(Of Integer) From {3, 4}
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 23));
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing<T>()
        where T : class, new()
    {
        var allocation = new T();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(10, 26));
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation_VisualBasic()
        {
            var sampleProgram =
@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing(Of T As {Class, New})()
        Dim allocation = New T()
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 26));
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation2()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing<T>()
        where T : struct
    {
        object allocation = new T();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(10, 29));
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation2_VisualBasic()
        {
            var sampleProgram =
@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing(Of T As Structure)()
        Dim allocation As Object = New T()
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram,
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 36));
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation3()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing<T>()
        where T : struct
    {
        T value = new T();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation3_VisualBasic()
        {
            var sampleProgram =
@"Imports System
Imports Roslyn.Utilities

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub Testing(Of T As Structure)()
        Dim value As T = new T()
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(sampleProgram);
        }
    }
}
