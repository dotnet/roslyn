' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    <Trait(Traits.Feature, Traits.Features.Simplification)>
    Public Class ModuleNameSimplifierTest
        Inherits AbstractSimplificationTests

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624131")>
        Public Async Function TestSimplifyModuleNameInNewStatement() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Imports System
Namespace N
    Module M
        Class D
        End Class
    End Module
End Namespace
Module Program
    Sub Main(args As String())
        Dim d = New N.{|SimplifyParent:M.D|}()
    End Sub
End Module
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Imports System
Namespace N
    Module M
        Class D
        End Class
    End Module
End Namespace
Module Program
    Sub Main(args As String())
        Dim d = New N.D()
    End Sub
End Module
            </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestSimplifyModuleNameInNestedNamespaces() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Namespace N
    Namespace M
        Module K
            Class C
                Shared Sub Goo()
                End Sub
            End Class
        End Module
        Namespace L
            Module K
                Class C
                    Shared Sub Goo()

                    End Sub
                End Class
            End Module
            Class C
                Shared Sub Goo()
                    {|SimplifyExtension:N.M.K.C.Goo|}()
                End Sub
            End Class
        End Namespace
    End Namespace
End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Namespace N
    Namespace M
        Module K
            Class C
                Shared Sub Goo()
                End Sub
            End Class
        End Module
        Namespace L
            Module K
                Class C
                    Shared Sub Goo()

                    End Sub
                End Class
            End Module
            Class C
                Shared Sub Goo()
                    M.C.Goo()
                End Sub
            End Class
        End Namespace
    End Namespace
End Namespace
            </text>

            Await TestAsync(input, expected)
        End Function

        <Fact>
        Public Async Function TestSimplifyModuleNameInDelegateConstruct() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Imports System
Namespace N
    Module M
        Class C
            Shared Sub Goo()

            End Function
        End Class
    End Module
End Namespace
Module Program
    Delegate Sub myDel()
    Sub Main(args As String())
        Dim m As myDel = AddressOf {|SimplifyExtension:N.M.C.Goo|}
    End Sub
End Module
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Imports System
Namespace N
    Module M
        Class C
            Shared Sub Goo()

            End Function
        End Class
    End Module
End Namespace
Module Program
    Delegate Sub myDel()
    Sub Main(args As String())
        Dim m As myDel = AddressOf N.C.Goo
    End Sub
End Module
            </text>

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608198")>
        Public Async Function TestDoNotSimplifyModuleNameInFieldInitializerAndConflictOfModuleNameAndField() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Module X
    Dim x As Action = Sub() Console.WriteLine(Global.X.{|SimplifyParent:x|})
End Module
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Module X
    Dim x As Action = Sub() Console.WriteLine(x)
End Module
            </text>

            Await TestAsync(input, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608198")>
        Public Async Function TestDoNotSimplifyModuleNameInFieldInitializerAndConflictOfModuleNameAndField_2() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Module X
    Dim x As Action = Sub() Console.WriteLine(Global.{|SimplifyParent:X|}.x)
End Module
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
Module X
    Dim x As Action = Sub() Console.WriteLine(Global.X.x)
End Module
            </text>

            Await TestAsync(input, expected)
        End Function

    End Class
End Namespace
