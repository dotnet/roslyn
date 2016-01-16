' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CaseCorrection
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text.Editor
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CaseCorrecting
    Public Class CaseCorrectionServiceTests
        Private Async Function TestAsync(input As XElement, expected As XElement, Optional interProject As Boolean = False) As Tasks.Task
            If (interProject) Then
                Await TestAsync(input, expected.NormalizedValue)
                Await TestAsync(input, expected.NormalizedValue)
            Else
                Await TestAsync(input.NormalizedValue, expected.NormalizedValue)
                Await TestAsync(input.NormalizedValue, expected.NormalizedValue)
            End If
        End Function

        Private Async Function TestAsync(input As String, expected As String) As Tasks.Task
            Using workspace = Await TestWorkspace.CreateVisualBasicAsync(input)
                Await TestAsync(expected, workspace)
            End Using
        End Function

        Private Shared Async Function TestAsync(expected As String, workspace As TestWorkspace) As Task
            Dim hostDocument = workspace.Documents.First()
            Dim buffer = hostDocument.GetTextBuffer()
            Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
            Dim span = (Await document.GetSyntaxRootAsync()).FullSpan

            Dim newDocument = Await CaseCorrector.CaseCorrectAsync(document, span, CancellationToken.None)
            newDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, CancellationToken.None)

            Dim actual = buffer.CurrentSnapshot.GetText()
            Assert.Equal(expected, actual)
        End Function

        Private Async Function TestAsync(input As XElement, expected As String) As Tasks.Task
            Using workspace = Await TestWorkspace.CreateWorkspaceAsync(input)
                Await TestAsync(expected, workspace)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestInterProject() As Tasks.Task
            Dim input =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <ProjectReference>CSAssembly1</ProjectReference>
                        <CompilationOptions><GlobalImport>CSAlias = CSNamespace.CSClass</GlobalImport></CompilationOptions>
                        <Document>
                            Module M1
                                CSAlias.Foo()
                            End Module
                        </Document>
                    </Project>
                    <Project Language="C#" AssemblyName="CSAssembly1" CommonReferences="true">
                        <Document>
                            namespace CSNamespace
                            {
                                public class CSClass
                                {
                                    public static void Foo() { }
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim expected =
                        <Code>
                            Module M1
                                CSAlias.Foo()
                            End Module
                        </Code>

            Await TestAsync(input, expected, interProject:=True)
        End Function

#Region "Identifiers"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestConstructorIdentifier() As Task
            Dim input = <Code>
Class Foo
    Sub Method()
        Dim i = New foo()
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class Foo
    Sub Method()
        Dim i = New Foo()
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(542058)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestConstructorNew1() As Task
            Dim input = <Code>
Class C
    Sub New
    End Sub

    Sub New(x As Integer)
        Me.new
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub New
    End Sub

    Sub New(x As Integer)
        Me.New
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(542058)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestConstructorNew2() As Task
            Dim input = <Code>
Class B
    Sub New()
    End Sub
End Class

Class C
    Inherits B

    Sub New(x As Integer)
        MyBase.new
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class B
    Sub New()
    End Sub
End Class

Class C
    Inherits B

    Sub New(x As Integer)
        MyBase.New
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(542058)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestConstructorNew3() As Task
            Dim input = <Code>
Class C
    Sub New
    End Sub

    Sub New(x As Integer)
        MyClass.new
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub New
    End Sub

    Sub New(x As Integer)
        MyClass.New
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(542058)>
        <WorkItem(543999)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestConstructorNew4() As Task
            Dim input = <Code>
Class C
    Sub New
    End Sub

    Sub New(x As Integer)
        With Me
            .new
        End With
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub New
    End Sub

    Sub New(x As Integer)
        With Me
            .New
        End With
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(541352)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestAlias1() As Task
            Dim input = <Code>
Imports S = System.String
Class T
    Dim x As s = "hello"
End Class
</Code>

            Dim expected = <Code>
Imports S = System.String
Class T
    Dim x As S = "hello"
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestClassIdentifier() As Task
            Dim input = <Code>
Class Foo
End Class

Class Consumer
    Dim i As foo
End Class
</Code>

            Dim expected = <Code>
Class Foo
End Class

Class Consumer
    Dim i As Foo
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestStructureIdentifier() As Task
            Dim input = <Code>
Structure Foo
End Structure

Class Consumer
    Dim i As foo
End Class
</Code>

            Dim expected = <Code>
Structure Foo
End Structure

Class Consumer
    Dim i As Foo
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestEnumIdentifier() As Task
            Dim input = <Code>
Enum Foo
    A
    B
    C
End Enum

Class Consumer
    Dim i As foo
End Class
</Code>

            Dim expected = <Code>
Enum Foo
    A
    B
    C
End Enum

Class Consumer
    Dim i As Foo
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestMethodIdentifier() As Task
            Dim input = <Code>
Class C
    Sub Foo()
        foo()
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub Foo()
        Foo()
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestMethodParameterLocalIdentifier() As Task
            Dim input = <Code>
Class C
    Sub Foo(Parameter As Integer)
        Console.WriteLine(parameter)
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub Foo(Parameter As Integer)
        Console.WriteLine(Parameter)
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(4680, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestNamedParameterIdentifier() As Task
            Dim input = <Code>
Class C
    Sub Foo(Parameter As Integer)
        Foo(parameter:=23)
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub Foo(Parameter As Integer)
        Foo(Parameter:=23)
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestLocalIdentifier() As Task
            Dim input = <Code>
Class C
    Sub Method()
        Dim Foo As Integer
        foo = 23
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub Method()
        Dim Foo As Integer
        Foo = 23
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestPropertyIdentifier() As Task
            Dim input = <Code>
Class C
    Property Foo As Integer

    Sub Method()
        Dim value = foo
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Property Foo As Integer

    Sub Method()
        Dim value = Foo
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestFieldIdentifier() As Task
            Dim input = <Code>
Class C
    Dim Foo As Integer

    Sub Method()
        Dim value = foo
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Dim Foo As Integer

    Sub Method()
        Dim value = Foo
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestEnumMemberIdentifier() As Task
            Dim input = <Code>
Class C
    Enum SomeEnum
        Member1
        Member2
    End Enum

    Sub Method()
        Dim value = SomeEnum.member1
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Enum SomeEnum
        Member1
        Member2
    End Enum

    Sub Method()
        Dim value = SomeEnum.Member1
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestDelegateInvocation() As Task
            Dim input = <Code>
Class C
    Delegate Sub D1()

    Sub Foo()
        Dim d As D1
        D()
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Delegate Sub D1()

    Sub Foo()
        Dim d As D1
        d()
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestDefaultProperty1() As Task
            Dim input = <Code>
Class X
    Public ReadOnly Property Foo As Y
        Get
            Return Nothing
        End Get
    End Property
 
End Class
 
Class Y
    Public Default ReadOnly Property Item(ByVal a As Integer) As String
        Get
            Return "hi"
        End Get
    End Property
End Class
 
Module M1
    Sub Main()
        Dim a As String
        Dim b As X
        b = New X()
        a = b.Foo(4)
    End Sub
End Module
</Code>

            Dim expected = <Code>
Class X
    Public ReadOnly Property Foo As Y
        Get
            Return Nothing
        End Get
    End Property
 
End Class
 
Class Y
    Public Default ReadOnly Property Item(ByVal a As Integer) As String
        Get
            Return "hi"
        End Get
    End Property
End Class
 
Module M1
    Sub Main()
        Dim a As String
        Dim b As X
        b = New X()
        a = b.Foo(4)
    End Sub
End Module
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestDefaultProperty2() As Task
            Dim input = <Code>
Class X
    Public ReadOnly Property Foo As Y
        Get
            Return Nothing
        End Get
    End Property
 
End Class
 
Class Y
    Public Default ReadOnly Property Item(ByVal a As Integer) As String
        Get
            Return "hi"
        End Get
    End Property
End Class
 
Module M1
    Sub Main()
        Dim a As String
        Dim b As X
        b = New X()
        a = b.Foo.Item(4)
    End Sub
End Module
</Code>

            Dim expected = <Code>
Class X
    Public ReadOnly Property Foo As Y
        Get
            Return Nothing
        End Get
    End Property
 
End Class
 
Class Y
    Public Default ReadOnly Property Item(ByVal a As Integer) As String
        Get
            Return "hi"
        End Get
    End Property
End Class
 
Module M1
    Sub Main()
        Dim a As String
        Dim b As X
        b = New X()
        a = b.Foo.Item(4)
    End Sub
End Module
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        <WorkItem(599333)>
        Public Async Function TestPartialMethodName1() As Task
            Dim input = <Code>
Partial Class foo
    Private Sub ABC()
    End Sub
End Class

Partial Class FOO
    Partial Private Sub abc()
    End Sub
End Class
</Code>

            Dim expected = <Code>
Partial Class foo
    Private Sub abc()
    End Sub
End Class

Partial Class FOO
    Partial Private Sub abc()
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        <WorkItem(599333)>
        Public Async Function TestPartialMethodName2() As Task
            ' Partial methods must be SUBs
            Dim input = <Code>
Partial Class foo
    Partial Private Function ABC() as Boolean
    End Function
End Class

Partial Class FOO
    Private Function abc() as Boolean
        Return False
    End Function
End Class
</Code>

            Dim expected = <Code>
Partial Class foo
    Partial Private Function ABC() As Boolean
    End Function
End Class

Partial Class FOO
    Private Function abc() As Boolean
        Return False
    End Function
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        <WorkItem(599333)>
        Public Async Function TestPartialMethodParameterName1() As Task
            ' Partial method with parameters
            Dim input = <Code>
Partial Class foo
    Partial Private Sub ABC(XYZ as Integer)
    End Sub
End Class

Partial Class FOO
    Private Sub abc(xyz as Integer)
    End Sub
End Class
</Code>

            Dim expected = <Code>
Partial Class foo
    Partial Private Sub ABC(XYZ As Integer)
    End Sub
End Class

Partial Class FOO
    Private Sub ABC(XYZ As Integer)
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        <WorkItem(599333)>
        Public Async Function TestPartialMethodParameterName2() As Task
            ' Multiple overloaded partial methods
            Dim input = <Code>
Partial Class foo
    Partial Private Sub ABC()
    End Sub

    Private Sub ABC(XYZ As Integer)
    End Sub
End Class

Partial Class FOO
    Private Sub abc()
    End Sub

    Partial Private Sub abc(xyz As Integer)
    End Sub
End Class
</Code>

            Dim expected = <Code>
Partial Class foo
    Partial Private Sub ABC()
    End Sub

    Private Sub abc(xyz As Integer)
    End Sub
End Class

Partial Class FOO
    Private Sub ABC()
    End Sub

    Partial Private Sub abc(xyz As Integer)
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        <WorkItem(599333)>
        Public Async Function TestPartialMethodParameterName3() As Task
            ' Partial method with different parameter names.

            ' We should not rename the parameter if names are not equal ignoring case.
            ' Compiler will anyways generate an error for this case.
            Dim input = <Code>
Partial Class foo
    Private Sub ABC(XYZ As Integer)
    End Sub
End Class

Partial Class FOO
    Partial Private Sub abc(x As Integer)
    End Sub
End Class
</Code>

            Dim expected = <Code>
Partial Class foo
    Private Sub abc(XYZ As Integer)
    End Sub
End Class

Partial Class FOO
    Partial Private Sub abc(x As Integer)
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(608626)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestOverloadResolutionFailure() As Task
            Dim input = <Code>
Option Strict On
Class C
    Sub M(i As Integer)
        m()
    End Sub
End Class
</Code>

            Dim expected = <Code>
Option Strict On
Class C
    Sub M(i As Integer)
        M()
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(1949, "https://github.com/dotnet/roslyn/issues/1949")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestUnResolvedTypeDoesNotBindToAnyAccessibleSymbol() As Task
            Dim unchangeCode = <Code>
Option Strict On
Class C
    Property prop As Integer
    Sub GetIt(arg As Integer)
        Dim var1 As Var1
        Dim var2 As Arg
        Dim var3 As Prop
    End Sub
End Class
</Code>

            Await TestAsync(unchangeCode, unchangeCode)
        End Function

#End Region

#Region "Keywords and type suffixes"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestIfElseThenKeywords() As Task
            Dim input = <Code>
Class C
    Sub Foo()
        if True then : else : end if
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub Foo()
        If True Then : Else : End If
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        <WorkItem(17313, "DevDiv_Projects/Roslyn")>
        Public Async Function TestElseIfKeyword() As Task
            Dim input =
<Code><![CDATA[
        If True Then
        Else If False Then
        End If
]]></Code>

            Dim expected =
<Code><![CDATA[
        If True Then
        ElseIf False Then
        End If
]]></Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestTrueFalseKeywords() As Task
            Dim input = <Code>
Class C
    Sub Foo()
        Dim q As Boolean = false
        Dim f As Boolean = true
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub Foo()
        Dim q As Boolean = False
        Dim f As Boolean = True
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(538930)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestCharacterTypeSuffix() As Task
            Dim input = <Code>
Class C
    Sub Foo()
        Dim ch = "x"C
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub Foo()
        Dim ch = "x"c
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(538930)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestULTypeSuffix() As Task
            Dim input = <Code>
Class C
    Sub Foo()
        Dim x = 2ul
        Dim y = &amp;h2ul
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub Foo()
        Dim x = 2UL
        Dim y = &amp;H2UL
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(538930)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestFTypeSuffix() As Task
            Dim input = <Code>
Class C
    Sub Foo()
        Dim x = 2.1f
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub Foo()
        Dim x = 2.1F
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(538930)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestRTypeSuffix() As Task
            Dim input = <Code>
Class C
    Sub Foo()
        Dim x = 2.1r
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub Foo()
        Dim x = 2.1R
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(538930)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestDTypeSuffix() As Task
            Dim input = <Code>
Class C
    Sub Foo()
        Dim x = 2.1d
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub Foo()
        Dim x = 2.1D
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(538930)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestDateLiteral() As Task
            Dim input = <Code>
Class C
    Sub Foo()
        Dim t1 = # 1:00 am #
        Dim t2 = # 1:00 pm #
    End Sub
End Class
</Code>

            Dim expected = <Code>
Class C
    Sub Foo()
        Dim t1 = # 1:00 AM #
        Dim t2 = # 1:00 PM #
    End Sub
End Class
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(539020)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestEscaping1() As Task
            Dim input = <Code>
Imports [GLobal].ns1
Namespace [GLOBAL].ns1
    Class C1
    End Class
End Namespace
</Code>

            Dim expected = <Code>
Imports [GLOBAL].ns1
Namespace [GLOBAL].ns1
    Class C1
    End Class
End Namespace
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(539020)>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestEscaping2() As Task
            Dim input = <Code>
Class [class]
    Shared Sub [shared]([boolean] As Boolean)
        If [BoOlEaN] Then
            Console.WriteLine("true")
        Else
            Console.WriteLine("false")
        End If
    End Sub
End Class

Module [module]
    Sub Main()
        [ClASs].ShArEd(True)
    End Sub
End Module
</Code>

            Dim expected = <Code>
Class [class]
    Shared Sub [shared]([boolean] As Boolean)
        If [boolean] Then
            Console.WriteLine("true")
        Else
            Console.WriteLine("false")
        End If
    End Sub
End Class

Module [module]
    Sub Main()
        [class].shared(True)
    End Sub
End Module
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(539356)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestREMInComment() As Task
            Dim input = <Code>
rem this is a comment
</Code>

            Dim expected = <Code>
REM this is a comment
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(529938), WorkItem(529935)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestFullwidthREMInComment() As Task
            Dim input = <Code>
ＲＥＭ this is a comment
</Code>

            Dim expected = <Code>
REM this is a comment
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestNameOf() As Task
            Dim input = <Code>
Module M
    Dim s = nameof(m)
End Module
</Code>

            Dim expected = <Code>
Module M
    Dim s = NameOf(M)
End Module
</Code>

            Await TestAsync(input, expected)
        End Function

#End Region

#Region "Preprocessor"

        <WorkItem(539308)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestPreprocessor() As Task
            Dim input = <Code>
#if true then
#end if
</Code>

            Dim expected = <Code>
#If True Then
#End If
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(539352)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestPreprocessorLiterals() As Task
            Dim input = <Code>
#const foo = 2.0d
</Code>

            Dim expected = <Code>
#Const foo = 2.0D
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(539352)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestPreprocessorInMethodBodies() As Task
            Dim input = <Code>
Module Program
    Sub Main(args As String())
#const foo = 2.0d
 
    End Sub
End Module
</Code>

            Dim expected = <Code>
Module Program
    Sub Main(args As String())
#Const foo = 2.0D
 
    End Sub
End Module
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(539352)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestPreprocessorAroundClass() As Task
            Dim input = <Code>
#if true then
Class C
End Class
#end if
</Code>

            Dim expected = <Code>
#If True Then
Class C
End Class
#End If
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(539472)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestRemCommentAfterPreprocessor() As Task
            Dim input = <Code>
#const foo = 42 rem foo
</Code>

            Dim expected = <Code>
#Const foo = 42 REM foo
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(5568, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestPreprocessorIdentifierBasic() As Task
            Dim input = <Code>
#Const ccConst = 0
#if CCCONST then
#end if
</Code>

            Dim expected = <Code>
#Const ccConst = 0
#If ccConst Then
#End If
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(5568, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestPreprocessorIdentifierBracketed() As Task
            Dim input = <Code>
#Const [Const] = 0
#if [CONST] then
#end if

#Const Const = 0
#if [CONST] then
#end if

#Const [ccConst] = 0
#if [CCCONST] then
#end if

#Const ccConst2 = 0
#if [CCCONST2] then
#end if
</Code>

            Dim expected = <Code>
#Const [Const] = 0
#If [Const] Then
#End If

#Const Const = 0
#If [Const] Then
#End If

#Const [ccConst] = 0
#If [ccConst] Then
#End If

#Const ccConst2 = 0
#If [ccConst2] Then
#End If
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(5568, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestPreprocessorIdentifierInCCExpression() As Task
            Dim input = <Code>
#Const ccConst = "SomeValue"
#Const ccConst2 = CCCONST + "Suffix"

' Binary expression
#if CCconST = CCCONST2 then
#elseif CccONsT2 = ccCONSt + "Suffix" then
Module Module1
    Sub Main()
        Dim CCConst As Integer = 0
#if CCCONST2 = ccCONSt + "Suffix" then
        Console.WriteLine(CCCONST) ' Case correction
#elseif CCCONST2 = "Suffix" then
        Console.WriteLine(CCCONST) ' No Case correction
#end if
    End Sub
End Module
#end if
</Code>

            Dim expected = <Code>
#Const ccConst = "SomeValue"
#Const ccConst2 = ccConst + "Suffix"

' Binary expression
#If ccConst = ccConst2 Then
#ElseIf ccConst2 = ccConst + "Suffix" Then
Module Module1
    Sub Main()
        Dim CCConst As Integer = 0
#If ccConst2 = ccConst + "Suffix" Then
        Console.WriteLine(CCConst) ' Case correction
#ElseIf ccConst2 = "Suffix" Then
        Console.WriteLine(CCCONST) ' No Case correction
#End If
    End Sub
End Module
#End If
</Code>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(5568, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestPreprocessorIdentifierErrorCases() As Task
            Dim input = <Code>
#Const ccConst = "SomeValue"

' No conversion, but case correction should work.
#if CCCONst = 0 then
#end if

' Constant ccConst2 is defined later, no case correction for CCCONST2.
#if CCCONST2 = ccCONSt + "Suffix" then
#end if

#Const ccConst2 = CCCONST + "Suffix"

' Case correction works here.
#if CCCONST2 = ccCONSt + "Suffix" then
#end if
</Code>

            Dim expected = <Code>
#Const ccConst = "SomeValue"

' No conversion, but case correction should work.
#If ccConst = 0 Then
#End If

' Constant ccConst2 is defined later, no case correction for CCCONST2.
#If CCCONST2 = ccConst + "Suffix" Then
#End If

#Const ccConst2 = ccConst + "Suffix"

' Case correction works here.
#If ccConst2 = ccConst + "Suffix" Then
#End If
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestWarningDirectives() As Task
            Dim input = <Code>
#disable warning bc123, BC456, SomeOtherId 'comment
#enable warning
#disable warning
#enable warning bc123, BC456, _
[someId]
</Code>

            Dim expected = <Code>
#Disable Warning bc123, BC456, SomeOtherId 'comment
#Enable Warning
#Disable Warning
#Enable Warning bc123, BC456, _
[someId]
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestWarningDirectives_FullWidth() As Task
            Dim input = <Code>
Module Module1
    Sub Main
＃ＤＩＳＡＢＬＥ ＷＡＲＮＩＮＧ
＃ _ 
 ｅｎａｂｌｅ     ｗａｒｎｉｎｇ _
ｅｎａｂｌｅ
    End Sub
End Module
</Code>
            Dim expected = <Code>
Module Module1
    Sub Main
＃Disable Warning
＃ _ 
 Enable     Warning _
ｅｎａｂｌｅ
    End Sub
End Module
</Code>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestWarningDirectives_ErrorCases() As Task
            Dim input = <Code>
#disable warning bc123, 'comment
#enable BC123
#disable warning ,AP123,
#enable warning AP123, BC456, _
</Code>

            Dim expected = <Code>
#Disable Warning bc123, 'comment
#Enable BC123
#Disable Warning ,AP123,
#Enable Warning AP123, BC456, _
</Code>

            Await TestAsync(input, expected)
        End Function
#End Region

#Region "Other tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function Test1() As Task
            Dim input =
<Text>
imports system
class C
    public sub Test(args As string)
        Test("foo")
        test(4)
        console.WRITELINE(arGS)
    end sub

    public sub TEST(i As integer)
    end sub
end class</Text>

            Dim expected =
<Text>
Imports System
Class C
    Public Sub Test(args As String)
        Test("foo")
        TEST(4)
        Console.WriteLine(args)
    End Sub

    Public Sub TEST(i As Integer)
    End Sub
End Class</Text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestAll() As Task

            Dim input =
<Text><![CDATA[
option compare binary
option explicit off
option infer on
option strict off

imports System
imports <xmlns="http://DefaultNamespace">

#Const MyLocation = "USA"

#If DEBUG Then
#ElseIf TRACE Then
#Else
#End If

public module thisModule
    dim withevents EClass as new EventClass

    sub Main()
    end sub
end module

namespace NS
    public mustinherit class CL
        inherits object
        implements ICloneable

        interface ITest
        end interface

        private sub TestEvents(byval a as long, optional byref op as byte = 1)

            dim threeDimArray(9, 9, 9), twoDimArray(9, 9) as integer
            erase threeDimArray, twoDimArray
            redim threeDimArray(4, 4, 9)

            const con as decimal = 10

            dim Obj as new string
            addhandler Obj.Ev_Event, addressof EventHandler
            removehandler Obj.Ev_Event, addressof EventHandler

            dim firstCheck, secondCheck as boolean
            firstCheck = a > a and a > a or a < a xor a
            secondCheck = a > a andalso a > a orelse a < a

            try
                select case a
                    case 1 to 5
                    case Obj is nothing
                    case else
                        call printToDebugWindow()
                end select
            catch ex as Exception when a = 1
            finally
            end try

            for c as date = Now to a step 1
                continue for
            next

            do until Obj isnot nothing
                if a < 10 then
                    exit do
                elseif not false then
                end if
            loop

            for each p as integer in new long() {}
            next

            REM goto
Line1:
            goto Line1

            dim s = if(false, 1, 2)

            dim discountedProducts = from prod in products
                                     let Discount = prod.UnitPrice * 0.1
                                     where Discount >= 50
                                     select prod.ProductName, prod.UnitPrice, Discount

            dim result = "F" like trycast("F", string)

            me.Clone()

            dim modResult = 10 mod 5

            dim toString = mybase.ToString() = myclass.ToString()

            throw new ArgumentNullException()

            using nf as new Font()
            end using

            with s
            end with
        end sub

        shadows function Test() as object handles Obj.Ev_Event
            do while true
                stop
                on error resume next
            loop
        end function

        enum filePermissions
            A
        end enum

        public overloads shared narrowing operator ctype(byval x as CL) as integer
            return 1
        end operator

        public overloads shared widening operator ctype(byval x as sbyte) as CL
            return nothing
        end operator

        default readonly property quoteForTheDay(byval b as ulong) as global.System.UInt64
            get
                quoteForTheDay = b
                exit property
            end get
        end property

        writeonly property quoteForTheDay1(byval a as ulong) as global.System.UInt64
            set(byval value as ulong)
            end set
        end property

        public overridable property property1(byval a as uinteger) as uinteger
            get
                dim b = getxmlnamespace(a)
            end get
            set(byval value as uinteger)
                dim t = gettype(ValueType)
            end set
        end property

        public mustoverride sub Overriable()

        protected friend function Conversion(byval paramarray a as List(of char)) as object
            dim check = cbool(a)
            check = cbyte(a)
            check = cchar(a)
            check = cdate(a)
            check = cdec(a)
            check = cdbl(a)
            check = cint(a)
            check = clng(a)
            check = cobj(a)
            check = csbyte(a)
            check = cshort(a)
            check = csng(a)
            check = cstr(a)
            check = directcast(ctype(a, double), double)
            check = cuint(a)
            check = culng(a)
            check = cushort(a)
            return check
        end function

        declare function getUserName lib "advapi32.dll" alias "GetUserNameA" (byval lpBuffer as short, byref nSize as single) as integer

        delegate function MathOperator(byval x as double, byval y as double) as double

        public event LogonCompleted(byval UserName as string)

        public function Clone() as object implements System.ICloneable.Clone
            static a as ushort = 1
            dim bool = typeof a is integer
            raiseevent LogonCompleted(nothing)
            return nothing
        end function

        public notoverridable overrides function ToString() as string
            synclock me
            end synclock

            return mybase.ToString()
        end function

        partial public notinheritable class A
        end class

        private structure S
        end structure
    end class

end namespace
]]></Text>

            Dim expected =
<Text><![CDATA[
Option Compare Binary
Option Explicit Off
Option Infer On
Option Strict Off

Imports System
Imports <xmlns="http://DefaultNamespace">

#Const MyLocation = "USA"

#If DEBUG Then
#ElseIf TRACE Then
#Else
#End If

Public Module thisModule
    Dim WithEvents EClass As New EventClass

    Sub Main()
    End Sub
End Module

Namespace NS
    Public MustInherit Class CL
        Inherits Object
        Implements ICloneable

        Interface ITest
        End Interface

        Private Sub TestEvents(ByVal a As Long, Optional ByRef op As Byte = 1)

            Dim threeDimArray(9, 9, 9), twoDimArray(9, 9) As Integer
            Erase threeDimArray, twoDimArray
            ReDim threeDimArray(4, 4, 9)

            Const con As Decimal = 10

            Dim Obj As New String
            AddHandler Obj.Ev_Event, AddressOf EventHandler
            RemoveHandler Obj.Ev_Event, AddressOf EventHandler

            Dim firstCheck, secondCheck As Boolean
            firstCheck = a > a And a > a Or a < a Xor a
            secondCheck = a > a AndAlso a > a OrElse a < a

            Try
                Select Case a
                    Case 1 To 5
                    Case Obj Is Nothing
                    Case Else
                        Call printToDebugWindow()
                End Select
            Catch ex As Exception When a = 1
            Finally
            End Try

            For c As Date = Now To a Step 1
                Continue For
            Next

            Do Until Obj IsNot Nothing
                If a < 10 Then
                    Exit Do
                ElseIf Not False Then
                End If
            Loop

            For Each p As Integer In New Long() {}
            Next

            REM goto
Line1:
            GoTo Line1

            Dim s = If(False, 1, 2)

            Dim discountedProducts = From prod In products
                                     Let Discount = prod.UnitPrice * 0.1
                                     Where Discount >= 50
                                     Select prod.ProductName, prod.UnitPrice, Discount

            Dim result = "F" Like TryCast("F", String)

            Me.Clone()

            Dim modResult = 10 Mod 5

            Dim toString = MyBase.ToString() = MyClass.ToString()

            Throw New ArgumentNullException()

            Using nf As New Font()
            End Using

            With s
            End With
        End Sub

        Shadows Function Test() As Object Handles Obj.Ev_Event
            Do While True
                Stop
                On Error Resume Next
            Loop
        End Function

        Enum filePermissions
            A
        End Enum

        Public Overloads Shared Narrowing Operator CType(ByVal x As CL) As Integer
            Return 1
        End Operator

        Public Overloads Shared Widening Operator CType(ByVal x As SByte) As CL
            Return Nothing
        End Operator

        Default ReadOnly Property quoteForTheDay(ByVal b As ULong) As Global.System.UInt64
            Get
                quoteForTheDay = b
                Exit Property
            End Get
        End Property

        WriteOnly Property quoteForTheDay1(ByVal a As ULong) As Global.System.UInt64
            Set(ByVal value As ULong)
            End Set
        End Property

        Public Overridable Property property1(ByVal a As UInteger) As UInteger
            Get
                Dim b = GetXmlNamespace(a)
            End Get
            Set(ByVal value As UInteger)
                Dim t = GetType(ValueType)
            End Set
        End Property

        Public MustOverride Sub Overriable()

        Protected Friend Function Conversion(ByVal ParamArray a As List(Of Char)) As Object
            Dim check = CBool(a)
            check = CByte(a)
            check = CChar(a)
            check = CDate(a)
            check = CDec(a)
            check = CDbl(a)
            check = CInt(a)
            check = CLng(a)
            check = CObj(a)
            check = CSByte(a)
            check = CShort(a)
            check = CSng(a)
            check = CStr(a)
            check = DirectCast(CType(a, Double), Double)
            check = CUInt(a)
            check = CULng(a)
            check = CUShort(a)
            Return check
        End Function

        Declare Function getUserName Lib "advapi32.dll" Alias "GetUserNameA" (ByVal lpBuffer As Short, ByRef nSize As Single) As Integer

        Delegate Function MathOperator(ByVal x As Double, ByVal y As Double) As Double

        Public Event LogonCompleted(ByVal UserName As String)

        Public Function Clone() As Object Implements System.ICloneable.Clone
            Static a As UShort = 1
            Dim bool = TypeOf a Is Integer
            RaiseEvent LogonCompleted(Nothing)
            Return Nothing
        End Function

        Public NotOverridable Overrides Function ToString() As String
            SyncLock Me
            End SyncLock

            Return MyBase.ToString()
        End Function

        Partial Public NotInheritable Class A
        End Class

        Private Structure S
        End Structure
    End Class

End Namespace
]]></Text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(542110)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function SkippedTokens() As Task
            Dim input =
<Text>
#If False
#endif
</Text>

            Dim expected =
<Text>
#If False
#EndIf
</Text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(544395)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestAttribute() As Task
            Dim input =
<Text><![CDATA[
Class FlagsAttribute : Inherits System.Attribute
End Class

<flags>
Enum EN
  EN
End Enum
]]></Text>

            Dim expected =
<Text><![CDATA[
Class FlagsAttribute : Inherits System.Attribute
End Class

<Flags>
Enum EN
  EN
End Enum
]]></Text>

            Await TestAsync(input, expected)
        End Function

        <WorkItem(530927)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestNewOnRightSideOfDot() As Task
            Dim input =
<Text><![CDATA[
Class C
    Sub New()
        Object.new()
    End Sub
End Class

Class D
    Sub New()
        Object.new(Object.new())
        Dim x = New C.new()
        Dim y = New C()
    End Sub
End Class

Class E
    Sub New(c As C)
        With c : .new()
        End With
    End Sub
End Class
]]></Text>

            Dim expected =
<Text><![CDATA[
Class C
    Sub New()
        Object.New()
    End Sub
End Class

Class D
    Sub New()
        Object.New(Object.New())
        Dim x = New C.New()
        Dim y = New C()
    End Sub
End Class

Class E
    Sub New(c As C)
        With c : .New()
        End With
    End Sub
End Class
]]></Text>

            Await TestAsync(input, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CaseCorrection)>
        Public Async Function TestAlias() As Task
            Dim input =
<Text>
Imports [Namespace] = System.Console
Class C
    Public Sub Test(args As String)
        Dim a As [namespace]
    End Sub
End Class</Text>

            Dim expected =
<Text>
Imports [Namespace] = System.Console
Class C
    Public Sub Test(args As String)
        Dim a As [Namespace]
    End Sub
End Class</Text>

            Await TestAsync(input, expected)
        End Function
#End Region

    End Class
End Namespace
