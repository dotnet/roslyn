' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
    Public Class VisualBasicReferenceHighlightingTests
        Inherits AbstractReferenceHighlightingTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub VerifyNoHighlightsWhenOptionDisabled()
            VerifyHighlights(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class $$Foo
                                Dim f As Foo
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                optionIsEnabled:=False)
        End Sub

        <WorkItem(539121)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub VerifyHighlightsForVisualBasicClassWithConstructor()
            VerifyHighlights(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class {|Definition:$$Foo|}
                                Public Sub {|Definition:New|}()
                                    Dim x = New {|Reference:Foo|}()
                                    Dim y As New {|Reference:Foo|}()
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(539121)>
        Public Sub VerifyHighlightsForVisualBasicClassWithSynthesizedConstructor()
            VerifyHighlights(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class {|Definition:Foo|}
                                Public Sub Blah()
                                    Dim x = New {|Reference:$$Foo|}()
                                    Dim y As New {|Reference:Foo|}()
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(540670)>
        Public Sub VerifyHighlightsForVisualBasicClassWithMethodNameChange1()
            VerifyHighlights(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Interface I
    Sub {|Definition:$$Foo|}()
End Interface

Class C
    Implements I

    Public Sub Bar() Implements I.{|Reference:Foo|}
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(540670)>
        Public Sub VerifyHighlightsForVisualBasicClassWithMethodNameChange2()
            VerifyHighlights(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Interface I
    Sub {|Definition:Foo|}()
End Interface

Class C
    Implements I

    Public Sub Bar() Implements I.{|Reference:$$Foo|}
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(540670)>
        Public Sub VerifyHighlightsForVisualBasicClassWithMethodNameChange3()
            VerifyHighlights(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Interface I
    Sub {|Definition:Foo|}()
End Interface

Class C
    Implements I

    Public Sub {|Definition:Foo|}() Implements I.{|Reference:$$Foo|}
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(543816)>
        Public Sub VerifyNoHighlightsForLiteral()
            VerifyHighlights(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Dim x as Integer = $$23
End Class
                        </Document>
                    </Project>
                </Workspace>)
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        <WorkItem(545531)>
        Public Sub VerifyHighlightsForGlobal()
            VerifyHighlights(
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
        End Sub

        <WorkItem(567959)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestAccessor1()
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

            VerifyHighlights(input)
        End Sub

        <WorkItem(567959)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestAccessor2()
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

            VerifyHighlights(input)
        End Sub

        <WorkItem(531624)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestHighlightParameterizedPropertyParameter()
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
Class C
    Default Public Property Foo($${|Definition:x|} As Integer) As Integer
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

            VerifyHighlights(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestWrittenReference()
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
Class Foo
    Public Sub New()
        Dim {|Definition:$$x|} As Integer
        {|WrittenReference:x|} = 0
    End Sub
End Class
                    </Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub TestWrittenReference2()
            Dim input =
            <Workspace>
                <Project Language="Visual Basic" CommonReferences="true">
                    <Document>
Class Foo
    Public Sub New()
        Dim {|Definition:$$x|} As Integer
        Foo({|WrittenReference:x|})
    End Sub

    Public Sub Foo(ByRef a as Integer)
    End Sub
End Class
                    </Document>
                </Project>
            </Workspace>

            VerifyHighlights(input)
        End Sub

        <WorkItem(1904, "https://github.com/dotnet/roslyn/issues/1904")>
        <WorkItem(2079, "https://github.com/dotnet/roslyn/issues/2079")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Sub VerifyHighlightsForVisualBasicGlobalImportAliasedNamespace()
            VerifyHighlights(
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
        End Sub
    End Class
End Namespace
