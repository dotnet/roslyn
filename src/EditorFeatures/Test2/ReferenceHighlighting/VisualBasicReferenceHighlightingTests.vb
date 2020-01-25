﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
    Public Class VisualBasicReferenceHighlightingTests
        Inherits AbstractReferenceHighlightingTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Async Function TestVerifyNoHighlightsWhenOptionDisabled() As Task
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
                optionIsEnabled:=False)
        End Function

        <WorkItem(539121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539121")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Async Function TestVerifyHighlightsForVisualBasicClassWithConstructor() As Task
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
                </Workspace>)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(539121, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539121")>
        Public Async Function TestVerifyHighlightsForVisualBasicClassWithSynthesizedConstructor() As Task
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
                </Workspace>)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(540670, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540670")>
        Public Async Function TestVerifyHighlightsForVisualBasicClassWithMethodNameChange1() As Task
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
                </Workspace>)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(540670, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540670")>
        Public Async Function TestVerifyHighlightsForVisualBasicClassWithMethodNameChange2() As Task
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
                </Workspace>)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(540670, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540670")>
        Public Async Function TestVerifyHighlightsForVisualBasicClassWithMethodNameChange3() As Task
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
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(543816, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543816")>
        Public Async Function TestVerifyNoHighlightsForLiteral() As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Dim x as Integer = $$23
End Class
                        </Document>
                    </Project>
                </Workspace>)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(545531, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545531")>
        Public Async Function TestVerifyHighlightsForGlobal() As Task
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
                </Workspace>)
        End Function

        <WorkItem(567959, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/567959")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Async Function TestAccessor1() As Task
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

            Await VerifyHighlightsAsync(input)
        End Function

        <WorkItem(567959, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/567959")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Async Function TestAccessor2() As Task
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

            Await VerifyHighlightsAsync(input)
        End Function

        <WorkItem(531624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531624")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Async Function TestHighlightParameterizedPropertyParameter() As Task
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

            Await VerifyHighlightsAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Async Function TestWrittenReference() As Task
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

            Await VerifyHighlightsAsync(input)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Async Function TestWrittenReference2() As Task
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

            Await VerifyHighlightsAsync(input)
        End Function

        <WorkItem(1904, "https://github.com/dotnet/roslyn/issues/1904")>
        <WorkItem(2079, "https://github.com/dotnet/roslyn/issues/2079")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Async Function TestVerifyHighlightsForVisualBasicGlobalImportAliasedNamespace() As Task
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
                </Workspace>)
        End Function
    End Class
End Namespace
