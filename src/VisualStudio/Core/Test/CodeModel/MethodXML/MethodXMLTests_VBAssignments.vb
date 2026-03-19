' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    Partial Public Class MethodXMLTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_FieldWithoutMe()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        c = New Class1
    End Sub

    Private c As Class1
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="3">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>c</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewClass>
                        <Type>ClassLibrary1.Class1</Type>
                    </NewClass>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_WithEventsFieldWithoutMe()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        c = New Class1
    End Sub

    Private WithEvents c As Class1
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="3">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>c</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewClass>
                        <Type>ClassLibrary1.Class1</Type>
                    </NewClass>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_FieldWithMe()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Me.c = New Class1
    End Sub

    Private c As Class1
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="3">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>c</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewClass>
                        <Type>ClassLibrary1.Class1</Type>
                    </NewClass>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_PropertyThroughFieldWithoutMe()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        p.c = New Class1
    End Sub

    Private c As Class1

    Property p As Class1
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="3">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <NameRef variablekind="property">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>p</Name>
                            </NameRef>
                        </Expression>
                        <Name>c</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewClass>
                        <Type>ClassLibrary1.Class1</Type>
                    </NewClass>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_PropertyThroughFieldWithMe()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Me.p.c = New Class1
    End Sub

    Private c As Class1

    Property p As Class1
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="3">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <NameRef variablekind="property">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>p</Name>
                            </NameRef>
                        </Expression>
                        <Name>c</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NewClass>
                        <Type>ClassLibrary1.Class1</Type>
                    </NewClass>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_InferredWithBinaryPlusOperation()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class C
    $$Sub M()
        Dim i = 1 + 1
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <Type>System.Int32</Type>
        <Name>i</Name>
        <Expression>
            <BinaryOperation binaryoperator="plus">
                <Expression>
                    <Literal>
                        <Number type="System.Int32">1</Number>
                    </Literal>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">1</Number>
                    </Literal>
                </Expression>
            </BinaryOperation>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_WithBinaryPlusOperation()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class C
    $$Sub M()
        Dim i As Integer = 1 + 1
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <Type>System.Int32</Type>
        <Name>i</Name>
        <Expression>
            <BinaryOperation binaryoperator="plus">
                <Expression>
                    <Literal>
                        <Number type="System.Int32">1</Number>
                    </Literal>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">1</Number>
                    </Literal>
                </Expression>
            </BinaryOperation>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_HexNumber()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class C
    $$Sub M()
        Dim i As Integer = &amp;H42
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <Type>System.Int32</Type>
        <Name>i</Name>
        <Expression>
            <Literal>
                <Number type="System.Int32">66</Number>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/462922")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_Bug462922()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Test 
    Private _vt As System.ValueType = 2.0#
    Public Property VT() As System.ValueType
        Get
            Return _vt
        End Get
        Set(ByVal value As System.ValueType)
            _vt = value
        End Set
    End Property
End Class

Public Class C
    Private Test1 As New Test

    $$Sub M()
        Me.Test1.VT = 2.0R
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="17">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <NameRef variablekind="field">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>Test1</Name>
                            </NameRef>
                        </Expression>
                        <Name>VT</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Double">2</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_EnumsAndCasts()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class C
    $$Sub M()
        Me.Goo = (CType(((AnchorStyles.Top Or AnchorStyles.Left) Or AnchorStyles.Right), AnchorStyles))
    End Sub

    Public Property Goo As AnchorStyles
End Class

Enum AnchorStyles
    Top
    Left
    Right
    Bottom
End Enum
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="3">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>Goo</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Parentheses>
                        <Expression>
                            <Cast>
                                <Type>ClassLibrary1.AnchorStyles</Type>
                                <Expression>
                                    <Parentheses>
                                        <Expression>
                                            <BinaryOperation binaryoperator="bitor">
                                                <Expression>
                                                    <Parentheses>
                                                        <Expression>
                                                            <BinaryOperation binaryoperator="bitor">
                                                                <Expression>
                                                                    <NameRef variablekind="field">
                                                                        <Expression>
                                                                            <Literal>
                                                                                <Type>ClassLibrary1.AnchorStyles</Type>
                                                                            </Literal>
                                                                        </Expression>
                                                                        <Name>Top</Name>
                                                                    </NameRef>
                                                                </Expression>
                                                                <Expression>
                                                                    <NameRef variablekind="field">
                                                                        <Expression>
                                                                            <Literal>
                                                                                <Type>ClassLibrary1.AnchorStyles</Type>
                                                                            </Literal>
                                                                        </Expression>
                                                                        <Name>Left</Name>
                                                                    </NameRef>
                                                                </Expression>
                                                            </BinaryOperation>
                                                        </Expression>
                                                    </Parentheses>
                                                </Expression>
                                                <Expression>
                                                    <NameRef variablekind="field">
                                                        <Expression>
                                                            <Literal>
                                                                <Type>ClassLibrary1.AnchorStyles</Type>
                                                            </Literal>
                                                        </Expression>
                                                        <Name>Right</Name>
                                                    </NameRef>
                                                </Expression>
                                            </BinaryOperation>
                                        </Expression>
                                    </Parentheses>
                                </Expression>
                            </Cast>
                        </Expression>
                    </Parentheses>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/743120")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_PropertyOffParameter()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
Public Class C
    Sub $$M(builder As System.Text.StringBuilder)
        builder.Capacity = 10
    End Sub
End Class
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="3">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="property">
                        <Expression>
                            <NameRef variablekind="local">
                                <Name>builder</Name>
                            </NameRef>
                        </Expression>
                        <Name>Capacity</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Int32">10</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/831374")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_NullableValue()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
Public Class C
    Sub $$M()
        Dim i As Integer? = 0
        Dim j As Integer? = Nothing
    End Sub
End Class
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <Type>System.Nullable`1[System.Int32]</Type>
        <Name>i</Name>
        <Expression>
            <Literal>
                <Number type="System.Int32">0</Number>
            </Literal>
        </Expression>
    </Local>
    <Local line="4">
        <Type>System.Nullable`1[System.Int32]</Type>
        <Name>j</Name>
        <Expression>
            <Literal>
                <Null/>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/831374")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_ClosedGeneric1()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
Imports System.Collections.Generic
Public Class C
    Sub $$M()
        Dim l = New List(Of Integer)
    End Sub
End Class
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="4">
        <Type>System.Collections.Generic.List`1[System.Int32]</Type>
        <Name>l</Name>
        <Expression>
            <NewClass>
                <Type>System.Collections.Generic.List`1[System.Int32]</Type>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/831374")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_ClosedGeneric2()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
Imports System.Collections.Generic
Public Class C
    Sub $$M()
        Dim l = New Dictionary(Of String, List(Of Integer))
    End Sub
End Class
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="4">
        <Type>System.Collections.Generic.Dictionary`2[System.String,System.Collections.Generic.List`1[System.Int32]]</Type>
        <Name>l</Name>
        <Expression>
            <NewClass>
                <Type>System.Collections.Generic.Dictionary`2[System.String,System.Collections.Generic.List`1[System.Int32]]</Type>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/831374")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_ClosedGeneric3()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
Imports System.Collections.Generic
Public Class C
    Sub $$M()
        Dim l = New List(Of String())
    End Sub
End Class
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="4">
        <Type>System.Collections.Generic.List`1[System.String[]]</Type>
        <Name>l</Name>
        <Expression>
            <NewClass>
                <Type>System.Collections.Generic.List`1[System.String[]]</Type>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/831374")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_ClosedGeneric4()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
Imports System.Collections.Generic
Public Class C
    Sub $$M()
        Dim l = New List(Of String(,,))
    End Sub
End Class
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="4">
        <Type>System.Collections.Generic.List`1[System.String[,,]]</Type>
        <Name>l</Name>
        <Expression>
            <NewClass>
                <Type>System.Collections.Generic.List`1[System.String[,,]]</Type>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/831374")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_TypeConfluence()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
Imports L = System.Collections.Generic.List(Of Byte())
Public Class C
    Sub $$M()
        Dim l = New L()
    End Sub
End Class
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="4">
        <Type>System.Collections.Generic.List`1[System.Byte[]]</Type>
        <Name>l</Name>
        <Expression>
            <NewClass>
                <Type>System.Collections.Generic.List`1[System.Byte[]]</Type>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/887584")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_EscapedNames()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
Enum E
    [True]
    [False]
End Enum
Public Class C
    Dim e1 As E

    Sub $$M()
        e1 = E.[True]
    End Sub
End Class
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="9">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>e1</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <Literal>
                                <Type>E</Type>
                            </Literal>
                        </Expression>
                        <Name>True</Name>
                    </NameRef>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_FloatingPointLiteralInGermanUICulture()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class C
    $$Sub M()
        x = 0.42!
    End Sub

    Private x As Single
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="3">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="field">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>x</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <Number type="System.Single">0.42</Number>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Dim currentThread = Thread.CurrentThread
            Dim oldCulture = currentThread.CurrentCulture
            Try
                currentThread.CurrentCulture = CultureInfo.GetCultureInfo("de-DE")
                Test(definition, expected)
            Finally
                currentThread.CurrentCulture = oldCulture
            End Try
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_DoNotThrowWhenLeftHandSideDoesntBind()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class A
    Public Property Prop As B
End Class

Public Class C
    Dim x As New A

    $$Sub M()
        x.Prop.NestedProp = "Text"
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="9">
        <Expression>
            <Assignment>
                <Expression>
                    <NameRef variablekind="unknown">
                        <Expression>
                            <NameRef variablekind="property">
                                <Expression>
                                    <NameRef variablekind="field">
                                        <Expression>
                                            <ThisReference/>
                                        </Expression>
                                        <Name>x</Name>
                                    </NameRef>
                                </Expression>
                                <Name>Prop</Name>
                            </NameRef>
                        </Expression>
                        <Name>NestedProp</Name>
                    </NameRef>
                </Expression>
                <Expression>
                    <Literal>
                        <String>Text</String>
                    </Literal>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Dim currentThread = Thread.CurrentThread
            Dim oldCulture = currentThread.CurrentCulture
            Try
                currentThread.CurrentCulture = CultureInfo.GetCultureInfo("de-DE")
                Test(definition, expected)
            Finally
                currentThread.CurrentCulture = oldCulture
            End Try
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/4312")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_PropertyAssignedWithEmptyArray()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
Class C
    Private Property Series As Object()

    $$Sub M()
        Me.Series = New Object(-1) {}
    End Sub
End Class
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="5"><Expression>
        <Assignment>
            <Expression>
                <NameRef variablekind="property">
                    <Expression>
                        <ThisReference/>
                    </Expression>
                    <Name>Series</Name>
                </NameRef>
            </Expression>
            <Expression>
                <NewArray>
                    <ArrayType rank="1">
                        <Type>System.Object</Type>
                    </ArrayType>
                    <Bound>
                        <Expression>
                            <Literal>
                                <Number type="System.Int32">0</Number>
                            </Literal>
                        </Expression>
                    </Bound>
                </NewArray>
            </Expression>
        </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/4149")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_RoundTrippedDoubles()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
Class C
    Sub $$M()
        Dim d As Double = 9.2233720368547758E+18R
    End Sub
End Class
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <Type>System.Double</Type>
        <Name>d</Name>
        <Expression>
            <Literal>
                <Number type="System.Double">9.2233720368547758E+18</Number>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem("https://github.com/dotnet/roslyn/issues/4149")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_RoundTrippedSingles()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <Document>
Class C
    Sub $$M()
        Dim s As Single = 0.333333343F
    End Sub
End Class
            </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <Type>System.Single</Type>
        <Name>s</Name>
        <Expression>
            <Literal>
                <Number type="System.Single">0.333333343</Number>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

    End Class
End Namespace
