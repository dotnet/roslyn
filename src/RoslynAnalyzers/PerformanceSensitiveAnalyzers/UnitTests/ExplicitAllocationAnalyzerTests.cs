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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 22));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectInitializer_VisualBasicAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 24));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectInitializerStruct_NoWarningAsync()
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
        public async Task ExplicitAllocation_ObjectInitializerStruct_NoWarning_VisualBasicAsync()
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
        public async Task ExplicitAllocation_ImplicitArrayCreationAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(9, 25));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitArrayCreation_VisualBasicAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(7, 36));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_AnonymousObjectCreationAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.AnonymousObjectCreationRule).WithLocation(9, 20));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_AnonymousObjectCreation_VisualBasicAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.AnonymousObjectCreationRule).WithLocation(7, 20));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ArrayCreationAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(9, 25));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ArrayCreation_VisualBasicAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(7, 23));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectCreationAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 26));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectCreation_VisualBasicAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 26));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_LetClauseAsync()
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
            var source = @"
using Roslyn.Utilities;

public struct S { }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        object box = new S();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(11, 22));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_Object_type_VisualBasicAsync()
        {
            var source = @"
Imports Roslyn.Utilities

Public Structure S
End Structure

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub SomeMethod()
        Dim box As Object = new S()
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(source,
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(10, 29));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_ValueType_typeAsync()
        {
            var source = @"
using Roslyn.Utilities;

public struct S { }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        System.ValueType box = new S();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(11, 32));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_ValueType_type_VisualBasicAsync()
        {
            var source = @"
Imports Roslyn.Utilities

Public Structure S
End Structure

Public Class A
    <PerformanceSensitive(""uri"")>
    Public Sub SomeMethod()
        Dim box As System.ValueType = new S()
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(source,
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(10, 39));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_into_interface_referenceAsync()
        {
            var source = @"
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
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(13, 17));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_into_interface_reference_VisualBasicAsync()
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
    Public Sub SomeMethod()
        Dim box As I = new S()
    End Sub
End Class";
            await VerifyVB.VerifyAnalyzerAsync(source,
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(14, 24));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_StructCreation_NoWarningAsync()
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
        public async Task ExplicitAllocation_StructCreation_NoWarning_VisualBasicAsync()
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
        public async Task ExplicitAllocation_PrimitiveTypeConversion_NoWarningAsync()
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
        public async Task ExplicitAllocation_PrimitiveTypeConversion_NoWarning_VisualBasicAsync()
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
        public async Task ExplicitAllocation_ImplicitValueTypeConversion_NoWarningAsync()
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
        public async Task ExplicitAllocation_ImplicitValueTypeConversion_NoWarning_VisualBasicAsync()
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
        public async Task ExplicitAllocation_NoParamsArrayCreationAsync()
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
        public async Task ExplicitAllocation_NoParamsArrayCreation_VisualBasicAsync()
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
        public async Task ExplicitAllocation_ExplicitDelegateCreationAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 23));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ExplicitDelegateCreation_VisualBasicAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 23));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitDelegateCreationAsync()
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
        public async Task ExplicitAllocation_ImplicitDelegateCreation_VisualBasicAsync()
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
        public async Task ExplicitAllocation_ListInitializerCreationAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 23));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_ListInitializerCreation_VisualBasicAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 23));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreationAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(10, 26));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation_VisualBasicAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 26));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation2Async()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(10, 29));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation2_VisualBasicAsync()
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
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyVB.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(7, 36));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task ExplicitAllocation_GenericObjectCreation3Async()
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
        public async Task ExplicitAllocation_GenericObjectCreation3_VisualBasicAsync()
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
