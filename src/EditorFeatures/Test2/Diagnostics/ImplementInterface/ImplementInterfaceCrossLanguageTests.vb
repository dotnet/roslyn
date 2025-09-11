' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CSharp.ImplementInterface
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.ImplementInterface

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.ImplementInterface
    <Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)>
    Public Class ImplementInterfaceCrossLanguageUserDiagnosticTests
        Inherits AbstractCrossLanguageUserDiagnosticTests

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As (DiagnosticAnalyzer, CodeFixProvider)
            If language = LanguageNames.CSharp Then
                Return (Nothing, New CSharpImplementInterfaceCodeFixProvider())
            Else
                Return (Nothing, New VisualBasicImplementInterfaceCodeFixProvider())
            End If
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545692")>
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
    void Goo(E x = E._);
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
 
    Public Sub Goo(Optional x As E = 1) Implements I.Goo
        Throw New NotImplementedException()
    End Sub
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545743")>
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
    void Goo(E x = E.X);
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
 
    Public Sub Goo(Optional x As E = 1) Implements I.Goo
        Throw New NotImplementedException()
    End Sub
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545788"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/715013")>
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
    void Goo(E x = E.A | E.a | E.B);
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

    Public Sub Goo(Optional x As E = CType(1, E) Or CType(2, E) Or E.B) Implements I.Goo
        Throw New NotImplementedException()
    End Sub
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545699")>
        Public Async Function Test_OptionalWithNoDefaultValue() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
using System.Runtime.InteropServices;
 
public interface I
{
    void Goo([Optional] int x);
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

    Public Sub Goo(Optional x As Integer = Nothing) Implements I.Goo
        Throw New NotImplementedException()
    End Sub
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545820")>
        Public Async Function Test_IndexerWithNoRequiredParameters_01() As Task
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72227")>
        Public Async Function Test_IndexerWithNoRequiredParameters_02() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
using System.Collections.Generic;

public interface I
{
    int this[params IEnumerable&lt;int&gt; y] { get; }
}
                        </Document>
                    </Project>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <ProjectReference>CSharpAssembly1</ProjectReference>
                        <Document>
Imports System.Collections.Generic

Class C
    Implements $$I
End Class
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
Imports System.Collections.Generic

Class C
    Implements I

    Default Public ReadOnly Property Item(y As IEnumerable(Of Integer)) As Integer Implements I.Item
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545868")>
        Public Async Function Test_ConflictingParameterNames1() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
public interface IA
{
    void Goo(int a, int A);
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

    Public Sub Goo(a1 As Integer, A2 As Integer) Implements IA.Goo
        Throw New NotImplementedException()
    End Sub
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545868")>
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

    Default Public ReadOnly Property Item(a1 As Integer, A2 As Integer) As Integer Implements IA.Item
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Class
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39434")>
        Public Async Function Test_ParameterizedProperty() As Task
            Dim input =
                <Workspace>
                    <Project Language='Visual Basic' AssemblyName='VBAssembly1' CommonReferences='true'>
                        <Document>
Public Interface I
    Property Goo(x As Integer) As String
End Interface
                        </Document>
                    </Project>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <ProjectReference>VBAssembly1</ProjectReference>
                        <Document FilePath='Test1.cs'>
public class C : $$I
{
}
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                <text>
public class C : I
{
    public string get_Goo(int x)
    {
        throw new System.NotImplementedException();
    }

    public void set_Goo(int x, string Value)
    {
        throw new System.NotImplementedException();
    }
}
                </text>.Value.Trim()

            Await TestAsync(input, expected)
        End Function
    End Class
End Namespace
