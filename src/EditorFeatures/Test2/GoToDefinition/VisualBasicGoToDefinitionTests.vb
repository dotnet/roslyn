' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToDefinition
    <[UseExportProvider]>
    Public Class VisualBasicGoToDefinitionTests
        Inherits GoToDefinitionTestsBase
#Region "Normal Visual Basic Tests"

        <WorkItem(3589, "https://github.com/dotnet/roslyn/issues/3589")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToDefinitionOnAnonymousMember()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
public class MyClass1
    public property [|Prop1|] as integer
end class
class Program
    sub Main()
        dim instance = new MyClass1()

        dim x as new With { instance.$$Prop1 }
    end sub
end class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToDefinition()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
            End Class
            Class OtherClass
                Dim obj As Some$$Class
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        <WorkItem(23030, "https://github.com/dotnet/roslyn/issues/23030")>
        Public Sub TestVisualBasicLiteralGoToDefinition()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Dim x as Integer = 12$$3
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        <WorkItem(23030, "https://github.com/dotnet/roslyn/issues/23030")>
        Public Sub TestVisualBasicStringLiteralGoToDefinition()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Dim x as String = "wo$$ow"
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(541105, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541105")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicPropertyBackingField()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Property [|P|] As Integer
    Sub M()
          Me.$$_P = 10
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToDefinitionSameClass()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim obj As Some$$Class
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToDefinitionNestedClass()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Outer
                Class [|Inner|]
                End Class
                Dim obj as In$$ner
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGotoDefinitionDifferentFiles()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class OtherClass
                Dim obj As SomeClass
            End Class
        </Document>
        <Document>
            Class OtherClass2
                Dim obj As Some$$Class
            End Class
        </Document>
        <Document>
            Class [|SomeClass|]
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGotoDefinitionPartialClasses()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            DummyClass
            End Class
        </Document>
        <Document>
            Partial Class [|OtherClass|]
                Dim a As Integer
            End Class
        </Document>
        <Document>
            Partial Class [|OtherClass|]
                Dim b As Integer
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Dim obj As Other$$Class
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGotoDefinitionMethod()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim x As Integer
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Sub goo()
                    Dim obj As Some$$Class
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(900438, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/900438")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGotoDefinitionPartialMethod()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Partial Class Customer
                Private Sub [|OnNameChanged|]()

                End Sub
            End Class
        </Document>
        <Document>
            Partial Class Customer
                Sub New()
                    Dim x As New Customer()
                    x.OnNameChanged$$()
                End Sub
                Partial Private Sub OnNameChanged()

                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicTouchLeft()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim x As Integer
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Sub goo()
                    Dim obj As $$SomeClass
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicTouchRight()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim x As Integer
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Sub goo()
                    Dim obj As SomeClass$$
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(542872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542872")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicMe()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class B
    Sub New()
    End Sub
End Class

Class [|C|]
    Inherits B

    Sub New()
        MyBase.New()
        MyClass.Goo()
        $$Me.Bar()
    End Sub

    Private Sub Bar()
    End Sub

    Private Sub Goo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(542872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542872")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicMyClass()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class B
    Sub New()
    End Sub
End Class

Class [|C|]
    Inherits B

    Sub New()
        MyBase.New()
        $$MyClass.Goo()
        Me.Bar()
    End Sub

    Private Sub Bar()
    End Sub

    Private Sub Goo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(542872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542872")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicMyBase()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class [|B|]
    Sub New()
    End Sub
End Class

Class C
    Inherits B

    Sub New()
        $$MyBase.New()
        MyClass.Goo()
        Me.Bar()
    End Sub

    Private Sub Bar()
    End Sub

    Private Sub Goo()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOverridenSubDefinition()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Base
                Overridable Sub [|Method|]()
                End Sub
            End Class
            Class Derived
                Inherits Base

                Overr$$ides Sub Method()
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOverridenFunctionDefinition()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Base
                Overridable Function [|Method|]() As Integer
                    Return 1
                End Function
            End Class
            Class Derived
                Inherits Base

                Overr$$ides Function Method() As Integer
                    Return 1
                End Function
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOverridenPropertyDefinition()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class Base
                Overridable Property [|Number|] As Integer
            End Class
            Class Derived
                Inherits Base

                Overr$$ides Property Number As Integer
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub
#End Region

#Region "Venus Visual Basic Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicVenusGotoDefinition()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            #ExternalSource ("Default.aspx", 1)
            Class [|Program|]
                Sub Main(args As String())
                    Dim f As New Pro$$gram()
                End Sub
            End Class
            #End ExternalSource
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(545324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545324")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicFilterGotoDefResultsFromHiddenCodeForUIPresenters()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|Program|]
                Sub Main(args As String())
            #ExternalSource ("Default.aspx", 1)
                    Dim f As New Pro$$gram()
                End Sub
            End Class
            #End ExternalSource
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(545324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545324")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicDoNotFilterGotoDefResultsFromHiddenCodeForApis()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|Program|]
                Sub Main(args As String())
            #ExternalSource ("Default.aspx", 1)
                    Dim f As New Pro$$gram()
                End Sub
            End Class
            #End ExternalSource
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub
#End Region

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicTestThroughExecuteCommand()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class [|SomeClass|]
                Dim x As Integer
            End Class
        </Document>
        <Document>
            Class ConsumingClass
                Sub goo()
                    Dim obj As SomeClass$$
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToDefinitionOnExtensionMethod()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim i As String = "1"
        i.Test$$Ext()
    End Sub
End Class

Module Ex
    <System.Runtime.CompilerServices.Extension()>
    Public Sub TestExt(Of T)(ex As T)
    End Sub
    <System.Runtime.CompilerServices.Extension()>
    Public Sub [|TestExt|](ex As string)
    End Sub
End Module]]>]
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(543218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543218")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicQueryRangeVariable()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim arr = New Integer() {4, 5}
        Dim q3 = From [|num|] In arr Select $$num
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(529060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529060")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGotoConstant()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module M
    Sub Main()
label1: GoTo $$200
[|200|]:    GoTo label1
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(10132, "https://github.com/dotnet/roslyn/issues/10132")>
        <WorkItem(545661, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545661")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCrossLanguageParameterizedPropertyOverride()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj">
        <Document>
Public Class A
    Public Overridable ReadOnly Property X(y As Integer) As Integer
        [|Get|]
        End Get
    End Property
End Class
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBProj</ProjectReference>
        <Document>
class B : A
{
    public override int get_X(int y)
    {
        return base.$$get_X(y);
    }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(866094, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/866094")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestCrossLanguageNavigationToVBModuleMember()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj">
        <Document>
Public Module A
    Public Sub [|M|]()
    End Sub
End Module
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>VBProj</ProjectReference>
        <Document>
class C
{
    static void N()
    {
        A.$$M();
    }
}
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

#Region "Show notification tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestShowNotificationVB()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
            Class SomeClass
            End Class
            C$$lass OtherClass
                Dim obj As SomeClass
            End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace, expectedResult:=False)
        End Sub

        <WorkItem(902119, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/902119")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestGoToDefinitionOnInferredFieldInitializer()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Public Class Class2
    Sub Test()
        Dim var1 = New With {Key .var2 = "Bob", Class2.va$$r3}
    End Sub

    Shared Property [|var3|]() As Integer
        Get
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
End Class

        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WorkItem(885151, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/885151")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestGoToDefinitionGlobalImportAlias()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <ProjectReference>VBAssembly</ProjectReference>
        <CompilationOptions>
            <GlobalImport>Goo = Importable.ImportMe</GlobalImport>
        </CompilationOptions>
        <Document>
Public Class Class2
    Sub Test()
        Dim x as Go$$o
    End Sub
End Class

        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBAssembly">
        <Document>
Namespace Importable
    Public Class [|ImportMe|]
    End Class
End Namespace
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub
#End Region

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitSelect_Exit()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M(parameter As String)
        Select Case parameter
            Case "a"
                Exit$$ Select
        End Select[||]
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitSelect_Select()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M(parameter As String)
        Select Case parameter
            Case "a"
                Exit Select$$
        End Select[||]
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitSub()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Exit Sub$$
    End Sub[||]
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitFunction()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Function M() As Integer
        Exit Sub$$
    End Function[||]
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnContinueWhile_Continue()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        [||]While True
             Continue$$ While
        End While
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnContinueWhile_While()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        [||]While True
             Continue While$$
        End While
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitWhile_While()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        While True
             Exit While$$
        End While[||]
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnContinueFor_Continue()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        [||]For index As Integer = 1 To 5
             Continue$$ For
        Next
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnContinueFor_For()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        [||]For index As Integer = 1 To 5
             Continue For$$
        Next
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitFor_For()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        For index As Integer = 1 To 5
             Exit For$$
        Next[||]
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnContinueForEach_For()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        [||]For Each element In Nothing
             Continue For$$
        Next
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitForEach_For()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        For Each element In Nothing
             Exit For$$
        Next[||]
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnContinueDoWhileLoop_Do()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        [||]Do While True
             Continue Do$$
        Loop
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitDoWhileLoop_Do()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Do While True
             Exit Do$$
        Loop[||]
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnContinueDoUntilLoop_Do()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        [||]Do Until True
             Continue Do$$
        Loop
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitDoUntilLoop_Do()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Do Until True
             Exit Do$$
        Loop[||]
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnContinueDoLoopWhile_Do()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        [||]Do
             Continue Do$$
        Loop While True
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnContinueDoLoopUntil_Do()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        [||]Do
             Continue Do$$
        Loop Until True
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitTry()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Try
             Exit Try$$
        End Try[||]
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitTryInCatch()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Try
        Catch Exception
             Exit Try$$
        End Try[||]
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInSub()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    [||]Sub M()
        Return$$
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInSub_Partial()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Partial Sub M()
    End Sub

    [||]Partial Private Sub M()
        Return$$
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInSub_Partial_ReverseOrder()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    [||]Partial Private Sub M()
        Return$$
    End Sub

    Partial Sub M()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInSubLambda()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Dim lambda = [||]Sub()
            Return$$
        End Sub
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInFunction()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    [||]Function M() As Int
        Return$$ 1
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInFunction_OnValue()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Function M([|x|] As Integer) As Integer
        Return x$$
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInIterator()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    [||]Public Iterator Function M() As IEnumerable(Of Integer)
        Yield$$ 1
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInIterator_OnValue()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Public Iterator Function M([|x|] As Integer) As IEnumerable(Of Integer)
        Yield x$$
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInFunctionLambda()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Dim lambda = [||]Function() As Int
            Return$$ 1
        End Function
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInConstructor()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    [||]Sub New()
        Return$$
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInOperator()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    [||]Public Shared Operator +(ByVal i As Integer) As Integer
        Return$$ 1
    End Operator
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInGetAccessor()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    ReadOnly Property P() As Integer
        [||]Get
            Return$$ 1
        End Get
    End Property
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInSetAccessor()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    ReadOnly Property P() As Integer
        [||]Set
            Return$$
        End Set
    End Property
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitPropertyInGetAccessor()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    ReadOnly Property P() As Integer
        [||]Get
            Exit Property$$
        End Get
    End Property
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnExitPropertyInSetAccessor()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Property P() As Integer
        [||]Set
            Exit Property$$
        End Set
    End Property
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInAddHandler()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Public Custom Event Click As EventHandler
        [||]AddHandler(ByVal value As EventHandler)
            Return$$
        End AddHandler
    End Event
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInRemoveHandler()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Public Custom Event Click As EventHandler
        [||]RemoveHandler(ByVal value As EventHandler)
            Return$$
        End RemoveHandler
    End Event
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition)>
        Public Sub TestVisualBasicGoToOnReturnInRaiseEvent()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Public Custom Event Click As EventHandler
        [||]RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
            Return$$
        End RaiseEvent
    End Event
End Class
        </Document>
    </Project>
</Workspace>

            Test(workspace)
        End Sub

    End Class
End Namespace
