' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.ImplementInterface

    Public Class ImplementInterfaceCrossLanguageTests
        Inherits AbstractCrossLanguageUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            If language = LanguageNames.CSharp Then
                Throw New NotSupportedException("Please add C# Implement interface tests to CSharpEditorTestTests.csproj. These tests require DiagnosticAnalyzer based test base and are NYI for AbstractCrossLanguageUserDiagnosticTest test base.")
            Else
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    Nothing,
                    New CodeAnalysis.VisualBasic.CodeFixes.ImplementInterface.ImplementInterfaceCodeFixProvider())
            End If
        End Function

        <WorkItem(545692)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function Test_EnumsWithConflictingNames1() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
public enum E
{
    _ = 1
}

public interface I
{
    void Foo(E x = E._);
}
                        </Document>
                    </Project>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <ProjectReference>CSharpAssembly1</ProjectReference>
                        <Document>
Class C
    Implements $$I
End Class
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
Class C
    Implements I
 
    Public Sub Foo(Optional x As E = 1) Implements I.Foo
        Throw New NotImplementedException()
    End Sub
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(545743)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function Test_EnumsWithConflictingNames2() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
public enum E
{
    x,
    X,
} 
public interface I
{
    void Foo(E x = E.X);
}
                        </Document>
                    </Project>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <ProjectReference>CSharpAssembly1</ProjectReference>
                        <Document>
Class C
    Implements $$I
End Class
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
Class C
    Implements I
 
    Public Sub Foo(Optional x As E = 1) Implements I.Foo
        Throw New NotImplementedException()
    End Sub
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(545788), WorkItem(715013)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function Test_EnumsWithConflictingNames3() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
using System;

[Flags]
public enum E
{
    A = 1,
    a = 2,
    B = 4,
}
 
public interface I
{
    void Foo(E x = E.A | E.a | E.B);
}
                        </Document>
                    </Project>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <ProjectReference>CSharpAssembly1</ProjectReference>
                        <Document>
Class C
    Implements $$I
End Class
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
Class C
    Implements I 

    Public Sub Foo(Optional x As E = CType(1, E) Or CType(2, E) Or E.B) Implements I.Foo
        Throw New NotImplementedException()
    End Sub
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(545699)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function Test_OptionalWithNoDefaultValue() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
using System.Runtime.InteropServices;
 
public interface I
{
    void Foo([Optional] int x);
}
                        </Document>
                    </Project>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <ProjectReference>CSharpAssembly1</ProjectReference>
                        <Document>
Class C
    Implements $$I
End Class
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
Class C
    Implements I 

    Public Sub Foo(Optional x As Integer = Nothing) Implements I.Foo
        Throw New NotImplementedException()
    End Sub
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(545820)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function Test_IndexerWithNoRequiredParameters() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
public interface I
{
    int this[params int[] y] { get; }
}
                        </Document>
                    </Project>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <ProjectReference>CSharpAssembly1</ProjectReference>
                        <Document>
Class C
    Implements $$I
End Class
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
Class C
    Implements I
 
    Public ReadOnly Property Item(ParamArray y() As Integer) As Integer Implements I.Item
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(545868)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function Test_ConflictingParameterNames1() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
public interface IA
{
    void Foo(int a, int A);
}
                        </Document>
                    </Project>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <ProjectReference>CSharpAssembly1</ProjectReference>
                        <Document>
Class C
    Implements $$IA
End Class
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
Class C
    Implements IA

    Public Sub Foo(a1 As Integer, a2 As Integer) Implements IA.Foo
        Throw New NotImplementedException()
    End Sub
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <WorkItem(545868)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
        Public Async Function Test_ConflictingParameterNames2() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
public interface IA
{
    int this[int a, int A] { get; }
}
                        </Document>
                    </Project>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <ProjectReference>CSharpAssembly1</ProjectReference>
                        <Document>
Class C
    Implements $$IA
End Class
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
Class C
    Implements IA

    Default Public ReadOnly Property Item(a1 As Integer, a2 As Integer) As Integer Implements IA.Item
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function
    End Class
End Namespace
