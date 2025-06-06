' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
    Public Class VisualBasicReferenceHighlightingTests
        Inherits AbstractReferenceHighlightingTests

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestVerifyNoHighlightsWhenOptionDisabled(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class $$Goo
                                Dim f As Goo
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                testHost, optionIsEnabled:=False)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539121")>
        Public Async Function TestVerifyHighlightsForVisualBasicClassWithConstructor(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class {|Definition:$$Goo|}
                                Public Sub {|Definition:New|}()
                                    Dim x = New {|Reference:Goo|}()
                                    Dim y As New {|Reference:Goo|}()
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539121")>
        Public Async Function TestVerifyHighlightsForVisualBasicClassWithSynthesizedConstructor(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class {|Definition:Goo|}
                                Public Sub Blah()
                                    Dim x = New {|Reference:$$Goo|}()
                                    Dim y As New {|Reference:Goo|}()
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540670")>
        Public Async Function TestVerifyHighlightsForVisualBasicClassWithMethodNameChange1(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Interface I
    Sub {|Definition:$$Goo|}()
End Interface

Class C
    Implements I

    Public Sub Bar() Implements I.{|Reference:Goo|}
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540670")>
        Public Async Function TestVerifyHighlightsForVisualBasicClassWithMethodNameChange2(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Interface I
    Sub {|Definition:Goo|}()
End Interface

Class C
    Implements I

    Public Sub Bar() Implements I.{|Reference:$$Goo|}
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540670")>
        Public Async Function TestVerifyHighlightsForVisualBasicClassWithMethodNameChange3(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Interface I
    Sub {|Definition:Goo|}()
End Interface

Class C
    Implements I

    ' This method itself is not found as nothing references it.
    Public Sub Goo() Implements I.{|Reference:$$Goo|}
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540670")>
        Public Async Function TestVerifyHighlightsForVisualBasicClassWithMethodNameChange4(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Interface I
    Sub {|Definition:Goo|}()
End Interface

Class C
    Implements I

    Public Sub {|Definition:Goo|}() Implements I.{|Reference:$$Goo|}
        ' Presence of this reference means we find the containing definition.
        {|Reference:Goo|}()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543816")>
        Public Async Function TestVerifyNoHighlightsForLiteral(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Dim x as Integer = $$23
End Class
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545531")>
        Public Async Function TestVerifyHighlightsForGlobal(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module M
    Sub Main
        {|Reference:$$Global|}.M.Main()
        {|Reference:Global|}.M.Main()
    End Sub
End Module
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/567959")>
        <CombinatorialData>
        Public Async Function TestAccessor1(testHost As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Public Property P As String
        $$Get
            Return P
        End Get
        Set(value As String)
            P = ""
        End Set
    End Property
End Class
        </Document>
    </Project>
</Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/567959")>
        <CombinatorialData>
        Public Async Function TestAccessor2(testHost As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Public Property P As String
        Get
            Return P
        End Get
        $$Set(value As String)
            P = ""
        End Set
    End Property
End Class
        </Document>
    </Project>
</Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531624")>
        <CombinatorialData>
        Public Async Function TestHighlightParameterizedPropertyParameter(testHost As TestHost) As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
Class C
    Default Public Property Goo($${|Definition:x|} As Integer) As Integer
        Get
            Return {|Reference:x|}
        End Get
        Set(value As Integer)

        End Set
    End Property
End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestWrittenReference(testHost As TestHost) As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
Class Goo
    Public Sub New()
        Dim {|Definition:$$x|} As Integer
        {|WrittenReference:x|} = 0
    End Sub
End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function TestWrittenReference2(testHost As TestHost) As Task
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
Class Goo
    Public Sub New()
        Dim {|Definition:$$x|} As Integer
        Goo({|WrittenReference:x|})
    End Sub

    Public Sub Goo(ByRef a as Integer)
    End Sub
End Class
                    </Document>
                </Project>
            </Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/1904")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/2079")>
        Public Async Function TestVerifyHighlightsForVisualBasicGlobalImportAliasedNamespace(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <CompilationOptions><GlobalImport>VB = Microsoft.VisualBasic</GlobalImport></CompilationOptions>
                        <Document>
                            Class Test
                                Public Sub TestMethod()
                                    ' Add reference tags to verify after #2079 is fixed
                                    Console.Write(NameOf($$VB))
                                    Console.Write(NameOf(VB))
                                    Console.Write(NameOf(Microsoft.VisualBasic))
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WpfTheory, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1820930")>
        <CombinatorialData>
        Public Async Function TestIncompleteProperty1(testHost As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    $$Public ReadOnly Property As String
        Get
            Return ""
        End Get
    End Property
End Class
        </Document>
    </Project>
</Workspace>

            Await VerifyHighlightsAsync(input, testHost)
        End Function
    End Class
End Namespace
