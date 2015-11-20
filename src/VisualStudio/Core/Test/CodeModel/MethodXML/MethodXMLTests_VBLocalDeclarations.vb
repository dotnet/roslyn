' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    Partial Public Class MethodXMLTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_NoInitializer() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_WithLiteralInitializer() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_WithInvocationInitializer1() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim s As String = Foo()
    End Sub

    Function Foo() As String
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
                        <Name>Foo</Name>
                    </NameRef>
                </Expression>
            </MethodCall>
        </Expression>
    </Local>
</Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_WithInvocationInitializer2() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim s As String = Foo(1)
    End Sub

    Function Foo(i As Integer) As String
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
                        <Name>Foo</Name>
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_WithEscapedNameAndAsNewClause() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_TwoInferredDeclarators() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_StaticLocal() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_ConstLocal() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_TwoNamesWithAsNewClause() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_ArrayWithNoBoundOrInitializer1() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_ArrayWithNoBoundOrInitializer2() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_ArrayWithSimpleBound() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_ArrayWithRangeBound() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_ArrayWithSimpleAndRangeBounds() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_ArrayWithStringBound() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim i("Foo") As Integer
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Quote line="3">Dim i("Foo") As Integer</Quote>
</Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_ArrayWithStringAndCastBound() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <CompilationOptions RootNamespace="ClassLibrary1"/>
        <Document>
Public Class Class1
    $$Sub M()
        Dim i(CInt("Foo")) As Integer
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <Quote line="3">Dim i(CInt("Foo")) As Integer</Quote>
</Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_ArrayWithPropertyAccessBound() As Task
            Dim definition =
    <Workspace>
        <Project Language="Visual Basic" CommonReferences="true">
            <CompilationOptions RootNamespace="ClassLibrary1"/>
            <Document>
Public Class Class1
    $$Sub M()
        Dim i("Foo".Length) As Integer
    End Sub
End Class
        </Document>
        </Project>
    </Workspace>

            Dim expected =
    <Block>
        <Quote line="3">Dim i("Foo".Length) As Integer</Quote>
    </Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_ArrayWithNoBoundAndCollectionInitializer1() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_ArrayWithNoBoundAndCollectionInitializer2() As Task
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

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBLocalDeclarations_InitializeWithStringConcatenation() As Task
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

            Await TestAsync(definition, expected)
        End Function

    End Class
End Namespace
