' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class ModuleNameSimplifierTest
        Inherits AbstractSimplificationTests

        <WorkItem(624131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/624131")>
        <Fact(), Trait(Traits.Feature, Traits.Features.Simplification)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyModuleNameInNestedNamespaces() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Namespace N
    Namespace M
        Module K
            Class C
                Shared Sub Foo()
                End Sub
            End Class
        End Module
        Namespace L
            Module K
                Class C
                    Shared Sub Foo()

                    End Sub
                End Class
            End Module
            Class C
                Shared Sub Foo()
                    {|SimplifyExtension:N.M.K.C.Foo|}()
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
                Shared Sub Foo()
                End Sub
            End Class
        End Module
        Namespace L
            Module K
                Class C
                    Shared Sub Foo()

                    End Sub
                End Class
            End Module
            Class C
                Shared Sub Foo()
                    M.C.Foo()
                End Sub
            End Class
        End Namespace
    End Namespace
End Namespace
            </text>

            Await TestAsync(input, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestSimplifyModuleNameInDelegateConstruct() As Task
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
Imports System
Namespace N
    Module M
        Class C
            Shared Sub Foo()

            End Function
        End Class
    End Module
End Namespace
Module Program
    Delegate Sub myDel()
    Sub Main(args As String())
        Dim m As myDel = AddressOf {|SimplifyExtension:N.M.C.Foo|}
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
            Shared Sub Foo()

            End Function
        End Class
    End Module
End Namespace
Module Program
    Delegate Sub myDel()
    Sub Main(args As String())
        Dim m As myDel = AddressOf N.C.Foo
    End Sub
End Module
            </text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(608198, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608198")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestDontSimplifyModuleNameInFieldInitializerAndConflictOfModuleNameAndField() As Task
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

        <WorkItem(608198, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608198")>
        <Fact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestDontSimplifyModuleNameInFieldInitializerAndConflictOfModuleNameAndField_2() As Task
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
