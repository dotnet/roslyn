' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.MethodXML
    Partial Public Class MethodXMLTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBStatements_AddHandler1() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Test">
        <CompilationOptions RootNamespace="N"/>
        <Document>
Imports System

Class C
    Event E As EventHandler

    Sub Handler(sender As Object, e As EventArgs)
    End Sub

    $$Sub M()
        AddHandler E, AddressOf Handler
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="10">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method" name="add_E">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                    </NameRef>
                </Expression>
                <Type implicit="yes">N.C, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                <Argument>
                    <Expression>
                        <NewDelegate name="Handler">
                            <Type implicit="yes">System.EventHandler, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</Type>
                            <Expression>
                                <ThisReference/>
                            </Expression>
                            <Type implicit="yes">N.C, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                        </NewDelegate>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBStatements_AddHandler2() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Test">
        <CompilationOptions RootNamespace="N"/>
        <Document>
Imports System

Class B
    Event E As EventHandler
End Class

Class C
    Dim b As New B

    Sub Handler(sender As Object, e As EventArgs)
    End Sub

    $$Sub M()
        AddHandler b.E, AddressOf Handler
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="14">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method" name="add_E">
                        <Expression>
                            <NameRef variablekind="field" name="b" fullname="N.C.b">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                            </NameRef>
                        </Expression>
                    </NameRef>
                </Expression>
                <Type implicit="yes">N.B, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                <Argument>
                    <Expression>
                        <NewDelegate name="Handler">
                            <Type implicit="yes">System.EventHandler, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</Type>
                            <Expression>
                                <ThisReference/>
                            </Expression>
                            <Type implicit="yes">N.C, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                        </NewDelegate>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBStatements_AddHandler3() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Test">
        <CompilationOptions RootNamespace="N"/>
        <Document>
Imports System

Class B
    Event E As EventHandler

    Sub Handler(sender As Object, e As EventArgs)
    End Sub
End Class

Class C
    Dim b As New B

    $$Sub M()
        AddHandler b.E, AddressOf b.Handler
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="14">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method" name="add_E">
                        <Expression>
                            <NameRef variablekind="field" name="b" fullname="N.C.b">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                            </NameRef>
                        </Expression>
                    </NameRef>
                </Expression>
                <Type implicit="yes">N.B, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                <Argument>
                    <Expression>
                        <NewDelegate name="Handler">
                            <Type implicit="yes">System.EventHandler, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</Type>
                            <Expression>
                                <NameRef variablekind="field" name="b" fullname="N.C.b">
                                    <Expression>
                                        <ThisReference/>
                                    </Expression>
                                </NameRef>
                            </Expression>
                            <Type implicit="yes">N.B, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                        </NewDelegate>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBStatements_AddHandler4() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Test">
        <CompilationOptions RootNamespace="N"/>
        <Document>
Imports System

Class A
    Event E As EventHandler
End Class

Class B
    Property A As New A

    Sub Handler(sender As Object, e As EventArgs)
    End Sub
End Class

Class C
    Dim b As New B

    $$Sub M()
        AddHandler b.A.E, AddressOf b.Handler
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="18">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method" name="add_E">
                        <Expression>
                            <NameRef variablekind="property" name="A" fullname="N.B.A">
                                <Expression>
                                    <NameRef variablekind="field" name="b" fullname="N.C.b">
                                        <Expression>
                                            <ThisReference/>
                                        </Expression>
                                    </NameRef>
                                </Expression>
                            </NameRef>
                        </Expression>
                    </NameRef>
                </Expression>
                <Type implicit="yes">N.A, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                <Argument>
                    <Expression>
                        <NewDelegate name="Handler">
                            <Type implicit="yes">System.EventHandler, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</Type>
                            <Expression>
                                <NameRef variablekind="field" name="b" fullname="N.C.b">
                                    <Expression>
                                        <ThisReference/>
                                    </Expression>
                                </NameRef>
                            </Expression>
                            <Type implicit="yes">N.B, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                        </NewDelegate>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBStatements_AddHandler5() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Test">
        <CompilationOptions RootNamespace="N"/>
        <Document>
Imports System

Class A
    Sub Handler(sender As Object, e As EventArgs)
    End Sub
End Class

Class B
    Property A As New A

    Event E As EventHandler
End Class

Class C
    Dim b As New B

    $$Sub M()
        AddHandler b.E, AddressOf b.A.Handler
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="18">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method" name="add_E">
                        <Expression>
                            <NameRef variablekind="field" name="b" fullname="N.C.b">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                            </NameRef>
                        </Expression>
                    </NameRef>
                </Expression>
                <Type implicit="yes">N.B, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                <Argument>
                    <Expression>
                        <NewDelegate name="Handler">
                            <Type implicit="yes">System.EventHandler, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</Type>
                            <Expression>
                                <NameRef variablekind="property" name="A" fullname="N.B.A">
                                    <Expression>
                                        <NameRef variablekind="field" name="b" fullname="N.C.b">
                                            <Expression>
                                                <ThisReference/>
                                            </Expression>
                                        </NameRef>
                                    </Expression>
                                </NameRef>
                            </Expression>
                            <Type implicit="yes">N.A, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                        </NewDelegate>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBStatements_RemoveHandler1() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Test">
        <CompilationOptions RootNamespace="N"/>
        <Document>
Imports System

Class C
    Event E As EventHandler

    Sub Handler(sender As Object, e As EventArgs)
    End Sub

    $$Sub M()
        RemoveHandler E, AddressOf Handler
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="10">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method" name="remove_E">
                        <Expression>
                            <ThisReference/>
                        </Expression>
                    </NameRef>
                </Expression>
                <Type implicit="yes">N.C, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                <Argument>
                    <Expression>
                        <NewDelegate name="Handler">
                            <Type implicit="yes">System.EventHandler, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</Type>
                            <Expression>
                                <ThisReference/>
                            </Expression>
                            <Type implicit="yes">N.C, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                        </NewDelegate>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBStatements_RemoveHandler2() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Test">
        <CompilationOptions RootNamespace="N"/>
        <Document>
Imports System

Class B
    Event E As EventHandler
End Class

Class C
    Dim b As New B

    Sub Handler(sender As Object, e As EventArgs)
    End Sub

    $$Sub M()
        RemoveHandler b.E, AddressOf Handler
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="14">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method" name="remove_E">
                        <Expression>
                            <NameRef variablekind="field" name="b" fullname="N.C.b">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                            </NameRef>
                        </Expression>
                    </NameRef>
                </Expression>
                <Type implicit="yes">N.B, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                <Argument>
                    <Expression>
                        <NewDelegate name="Handler">
                            <Type implicit="yes">System.EventHandler, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</Type>
                            <Expression>
                                <ThisReference/>
                            </Expression>
                            <Type implicit="yes">N.C, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                        </NewDelegate>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBStatements_RemoveHandler3() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Test">
        <CompilationOptions RootNamespace="N"/>
        <Document>
Imports System

Class B
    Event E As EventHandler

    Sub Handler(sender As Object, e As EventArgs)
    End Sub
End Class

Class C
    Dim b As New B

    $$Sub M()
        RemoveHandler b.E, AddressOf b.Handler
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="14">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method" name="remove_E">
                        <Expression>
                            <NameRef variablekind="field" name="b" fullname="N.C.b">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                            </NameRef>
                        </Expression>
                    </NameRef>
                </Expression>
                <Type implicit="yes">N.B, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                <Argument>
                    <Expression>
                        <NewDelegate name="Handler">
                            <Type implicit="yes">System.EventHandler, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</Type>
                            <Expression>
                                <NameRef variablekind="field" name="b" fullname="N.C.b">
                                    <Expression>
                                        <ThisReference/>
                                    </Expression>
                                </NameRef>
                            </Expression>
                            <Type implicit="yes">N.B, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                        </NewDelegate>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBStatements_RemoveHandler4() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Test">
        <CompilationOptions RootNamespace="N"/>
        <Document>
Imports System

Class A
    Event E As EventHandler
End Class

Class B
    Property A As New A

    Sub Handler(sender As Object, e As EventArgs)
    End Sub
End Class

Class C
    Dim b As New B

    $$Sub M()
        RemoveHandler b.A.E, AddressOf b.Handler
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="18">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method" name="remove_E">
                        <Expression>
                            <NameRef variablekind="property" name="A" fullname="N.B.A">
                                <Expression>
                                    <NameRef variablekind="field" name="b" fullname="N.C.b">
                                        <Expression>
                                            <ThisReference/>
                                        </Expression>
                                    </NameRef>
                                </Expression>
                            </NameRef>
                        </Expression>
                    </NameRef>
                </Expression>
                <Type implicit="yes">N.A, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                <Argument>
                    <Expression>
                        <NewDelegate name="Handler">
                            <Type implicit="yes">System.EventHandler, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</Type>
                            <Expression>
                                <NameRef variablekind="field" name="b" fullname="N.C.b">
                                    <Expression>
                                        <ThisReference/>
                                    </Expression>
                                </NameRef>
                            </Expression>
                            <Type implicit="yes">N.B, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                        </NewDelegate>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Await TestAsync(definition, expected)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelMethodXml)>
        Public Async Function TestVBStatements_RemoveHandler5() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Test">
        <CompilationOptions RootNamespace="N"/>
        <Document>
Imports System

Class A
    Sub Handler(sender As Object, e As EventArgs)
    End Sub
End Class

Class B
    Property A As New A

    Event E As EventHandler
End Class

Class C
    Dim b As New B

    $$Sub M()
        RemoveHandler b.E, AddressOf b.A.Handler
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<Block>
    <ExpressionStatement line="18">
        <Expression>
            <MethodCall>
                <Expression>
                    <NameRef variablekind="method" name="remove_E">
                        <Expression>
                            <NameRef variablekind="field" name="b" fullname="N.C.b">
                                <Expression>
                                    <ThisReference/>
                                </Expression>
                            </NameRef>
                        </Expression>
                    </NameRef>
                </Expression>
                <Type implicit="yes">N.B, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                <Argument>
                    <Expression>
                        <NewDelegate name="Handler">
                            <Type implicit="yes">System.EventHandler, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</Type>
                            <Expression>
                                <NameRef variablekind="property" name="A" fullname="N.B.A">
                                    <Expression>
                                        <NameRef variablekind="field" name="b" fullname="N.C.b">
                                            <Expression>
                                                <ThisReference/>
                                            </Expression>
                                        </NameRef>
                                    </Expression>
                                </NameRef>
                            </Expression>
                            <Type implicit="yes">N.A, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null</Type>
                        </NewDelegate>
                    </Expression>
                </Argument>
            </MethodCall>
        </Expression>
    </ExpressionStatement>
</Block>

            Await TestAsync(definition, expected)
        End Function

    End Class
End Namespace