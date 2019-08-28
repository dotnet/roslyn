' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    Partial Public Class MethodXMLTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBInvocations_InvocationWithoutMe()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Goo()
    End Sub

    Sub Goo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="3">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>Goo</Name>
                    </NameRef>
                </Expression>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBInvocations_InvocationWithMe()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Me.Goo()
    End Sub

    Sub Goo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="3">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                        <Name>Goo</Name>
                    </NameRef>
                </Expression>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBInvocations_WithArrayInitializer1()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class C
    $$Sub M()
        Me.list.AddRange(New String() { "goo", "bar", "baz" })
    End Sub

    Dim list As System.Collections.ArrayList
End Class
            </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="3">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method">
                        <Expression>
                            <NameRef variablekind="field">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                                <Name>list</Name>
                            </NameRef>
                        </Expression>
                        <Name>AddRange</Name>
                    </NameRef>
                </Expression>
                <Argument>
                    <Expression>
                        <NewArray>
                            <ArrayType rank="1">
                                <Type>System.String</Type>
                            </ArrayType>
                            <Bound>
                                <Expression>
                                    <Literal>
                                        <Number>3</Number>
                                    </Literal>
                                </Expression>
                            </Bound>
                            <Expression>
                                <Literal>
                                    <String>goo</String>
                                </Literal>
                            </Expression>
                            <Expression>
                                <Literal>
                                    <String>bar</String>
                                </Literal>
                            </Expression>
                            <Expression>
                                <Literal>
                                    <String>baz</String>
                                </Literal>
                            </Expression>
                        </NewArray>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBInvocations_InvokeOnCast()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class C
    $$Sub M()
        Dim o As Object = New String("."c, 10)
        Dim s = CType(o, System.String).ToString()
    End Sub
End Class
            </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <Type>System.Object</Type>
        <Name>o</Name>
        <Expression>
            <NewClass>
                <Type>System.String</Type>
                <Argument>
                    <Expression>
                        <Literal>
                            <Char>.</Char>
                        </Literal>
                    </Expression>
                </Argument>
                <Argument>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">10</Number>
                        </Literal>
                    </Expression>
                </Argument>
            </NewClass>
        </Expression>
    </Local>
    <Local line="4">
        <Type>System.String</Type>
        <Name>s</Name>
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method">
                        <Expression>
                            <Cast>
                                <Type>System.String</Type>
                                <Expression>
                                    <NameRef variablekind="local">
                                        <Name>o</Name>
                                    </NameRef>
                                </Expression>
                            </Cast>
                        </Expression>
                        <Name>ToString</Name>
                    </NameRef>
                </Expression>
            </MethodCall>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBInvocations_InvokeFixInCast()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1" EmbedVbCoreRuntime="true"/>
        <Document>
Imports Microsoft.VisualBasic
Class C
    $$Sub M()
        Dim b = CByte(Fix(10))
    End Sub
End Class
            </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="4">
        <Type>System.Byte</Type>
        <Name>b</Name>
        <Expression>
            <Cast>
                <Type>System.Byte</Type>
                <Expression>
                    <MethodCall>
                        <Expression>
                            <NameRef variablekind="method">
                                <Expression>
                                    <Literal>
                                        <Type>Microsoft.VisualBasic.Conversion</Type>
                                    </Literal>
                                </Expression>
                                <Name>Fix</Name>
                            </NameRef>
                        </Expression>
                        <Argument>
                            <Expression>
                                <Literal>
                                    <Number type="System.Int32">10</Number>
                                </Literal>
                            </Expression>
                        </Argument>
                    </MethodCall>
                </Expression>
            </Cast>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <WorkItem(870422, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/870422")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBAssignments_MethodCallWithoutTypeQualification()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        c = Global.Microsoft.VisualBasic.ChrW(13)
    End Sub

    Private c As String
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
                    <MethodCall>
                        <Expression>
                            <NameRef variablekind="method">
                                <Expression>
                                    <Literal>
                                        <Type>Microsoft.VisualBasic.Strings</Type>
                                    </Literal>
                                </Expression>
                                <Name>ChrW</Name>
                            </NameRef>
                        </Expression>
                        <Argument>
                            <Expression>
                                <Literal>
                                    <Number type="System.Int32">13</Number>
                                </Literal>
                            </Expression>
                        </Argument>
                    </MethodCall>
                </Expression>
            </Assignment>
        </Expression>
    </ExpressionStatement>
</Block>

            Test(definition, expected)
        End Sub

    End Class
End Namespace
