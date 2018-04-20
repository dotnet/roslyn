' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    Partial Public Class MethodXMLTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_NoInitializer()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim s As String
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <Type>System.String</Type>
        <Name>s</Name>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_WithLiteralInitializer()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim s As String = "Hello"
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
    <Block>
        <Local line="3">
            <Type>System.String</Type>
            <Name>s</Name>
            <Expression>
                <Literal>
                    <String>Hello</String>
                </Literal>
            </Expression>
        </Local>
    </Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_WithInvocationInitializer1()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim s As String = Goo()
    End Sub

    Function Goo() As String
        Return "Hello"
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <Type>System.String</Type>
        <Name>s</Name>
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
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_WithInvocationInitializer2()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim s As String = Goo(1)
    End Sub

    Function Goo(i As Integer) As String
        Return "Hello"
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <Type>System.String</Type>
        <Name>s</Name>
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
                <Argument>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">1</Number>
                        </Literal>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_WithEscapedNameAndAsNewClause()
            ' Note: The behavior here is different than Dev10 where escaped keywords
            ' would not be escaped in the generated XML.

            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim [class] as New Class1
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <Type>ClassLibrary1.Class1</Type>
        <Name>[class]</Name>
        <Expression>
            <NewClass>
                <Type>ClassLibrary1.Class1</Type>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_TwoInferredDeclarators()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim i = 0, j = 1
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
                <Number type="System.Int32">0</Number>
            </Literal>
        </Expression>
    </Local>
    <Local line="3">
        <Type>System.Int32</Type>
        <Name>j</Name>
        <Expression>
            <Literal>
                <Number type="System.Int32">1</Number>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_StaticLocal()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Static i As Integer = 1
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
                <Number type="System.Int32">1</Number>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_ConstLocal()
            ' NOTE: Dev10 didn't generate *any* XML for Const locals because it walked the
            ' lowered IL tree. We're now generating the same thing that C# does (which has
            ' generates a local without the "Const" modifier -- i.e. a bug).

            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Const i As Integer = 1
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
                <Number type="System.Int32">1</Number>
            </Literal>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_TwoNamesWithAsNewClause()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim o, n As New Object()
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
                <Type>System.Object</Type>
            </NewClass>
        </Expression>
        <Name>n</Name>
        <Expression>
            <NewClass>
                <Type>System.Object</Type>
            </NewClass>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_ArrayWithNoBoundOrInitializer1()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim i() As Integer
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <ArrayType rank="1">
            <Type>System.Int32</Type>
        </ArrayType>
        <Name>i</Name>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_ArrayWithNoBoundOrInitializer2()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim i As Integer()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <ArrayType rank="1">
            <Type>System.Int32</Type>
        </ArrayType>
        <Name>i</Name>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_ArrayWithSimpleBound()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim i(4) As Integer
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <ArrayType rank="1">
            <Type>System.Int32</Type>
        </ArrayType>
        <Name>i</Name>
        <Expression>
            <NewArray>
                <ArrayType rank="1">
                    <Type>System.Int32</Type>
                </ArrayType>
                <Bound>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">5</Number>
                        </Literal>
                    </Expression>
                </Bound>
            </NewArray>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_ArrayWithRangeBound()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim i(0 To 4) As Integer
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <ArrayType rank="1">
            <Type>System.Int32</Type>
        </ArrayType>
        <Name>i</Name>
        <Expression>
            <NewArray>
                <ArrayType rank="1">
                    <Type>System.Int32</Type>
                </ArrayType>
                <Bound>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">5</Number>
                        </Literal>
                    </Expression>
                </Bound>
            </NewArray>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_ArrayWithSimpleAndRangeBounds()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim i(3, 0 To 6) As Integer
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Local line="3">
        <ArrayType rank="2">
            <Type>System.Int32</Type>
        </ArrayType>
        <Name>i</Name>
        <Expression>
            <NewArray>
                <ArrayType rank="2">
                    <Type>System.Int32</Type>
                </ArrayType>
                <Bound>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">4</Number>
                        </Literal>
                    </Expression>
                </Bound>
                <Bound>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">7</Number>
                        </Literal>
                    </Expression>
                </Bound>
            </NewArray>
        </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_ArrayWithStringBound()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim i("Goo") As Integer
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Quote line="3">Dim i("Goo") As Integer</Quote>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_ArrayWithStringAndCastBound()
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim i(CInt("Goo")) As Integer
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Quote line="3">Dim i(CInt("Goo")) As Integer</Quote>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_ArrayWithPropertyAccessBound()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <CompilationOptions RootNamespace="ClassLibrary1"/>
            <Document>
Public Class Class1
    $$Sub M()
        Dim i("Goo".Length) As Integer
    End Sub
End Class
        </Document>
        </Project>
    </Workspace>

            Dim expected =
    <Block>
        <Quote line="3">Dim i("Goo".Length) As Integer</Quote>
    </Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_ArrayWithNoBoundAndCollectionInitializer1()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <CompilationOptions RootNamespace="ClassLibrary1"/>
            <Document>
Public Class Class1
    $$Sub M()
        Dim i() As Integer = {1, 2, 3}
    End Sub
End Class
        </Document>
        </Project>
    </Workspace>

            Dim expected =
    <Block>
        <Local line="3">
            <ArrayType rank="1">
                <Type>System.Int32</Type>
            </ArrayType>
            <Name>i</Name>
            <Expression>
                <NewArray>
                    <ArrayType rank="1">
                        <Type>System.Int32</Type>
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
                            <Number type="System.Int32">1</Number>
                        </Literal>
                    </Expression>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">2</Number>
                        </Literal>
                    </Expression>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">3</Number>
                        </Literal>
                    </Expression>
                </NewArray>
            </Expression>
        </Local>
    </Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_ArrayWithNoBoundAndCollectionInitializer2()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <CompilationOptions RootNamespace="ClassLibrary1"/>
            <Document>
Public Class Class1
    $$Sub M()
        Dim i As Integer() = {1, 2, 3}
    End Sub
End Class
        </Document>
        </Project>
    </Workspace>

            Dim expected =
    <Block>
        <Local line="3">
            <ArrayType rank="1">
                <Type>System.Int32</Type>
            </ArrayType>
            <Name>i</Name>
            <Expression>
                <NewArray>
                    <ArrayType rank="1">
                        <Type>System.Int32</Type>
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
                            <Number type="System.Int32">1</Number>
                        </Literal>
                    </Expression>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">2</Number>
                        </Literal>
                    </Expression>
                    <Expression>
                        <Literal>
                            <Number type="System.Int32">3</Number>
                        </Literal>
                    </Expression>
                </NewArray>
            </Expression>
        </Local>
    </Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_InitializeWithStringConcatenation()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <CompilationOptions RootNamespace="ClassLibrary1"/>
            <Document>
Public Class C
    $$Sub M()
        Dim s = "Text" &amp; "Text"
    End Sub
End Class
        </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block><Local line="3">
    <Type>System.String</Type>
    <Name>s</Name>
    <Expression>
        <BinaryOperation binaryoperator="concatenate">
            <Expression>
                <Literal>
                    <String>Text</String>
                </Literal>
            </Expression>
            <Expression>
                <Literal>
                    <String>Text</String>
                </Literal>
            </Expression>
        </BinaryOperation>
    </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_DirectCast()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <CompilationOptions RootNamespace="ClassLibrary1"/>
            <Document>
Public Class C
    $$Sub M()
        Dim s = DirectCast("Text", String)
    End Sub
End Class
        </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block><Local line="3">
    <Type>System.String</Type>
    <Name>s</Name>
    <Expression>
        <Cast directcast="yes">
            <Type>System.String</Type>
            <Expression>
                <Literal>
                    <String>Text</String>
                </Literal>
            </Expression>
        </Cast>
    </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Sub TestVBLocalDeclarations_TryCast()
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <CompilationOptions RootNamespace="ClassLibrary1"/>
            <Document>
Public Class C
    $$Sub M()
        Dim s = TryCast("Text", String)
    End Sub
End Class
        </Document>
        </Project>
    </Workspace>

            Dim expected =
<Block><Local line="3">
    <Type>System.String</Type>
    <Name>s</Name>
    <Expression>
        <Cast trycast="yes">
            <Type>System.String</Type>
            <Expression>
                <Literal>
                    <String>Text</String>
                </Literal>
            </Expression>
        </Cast>
    </Expression>
    </Local>
</Block>

            Test(definition, expected)
        End Sub

    End Class
End Namespace
