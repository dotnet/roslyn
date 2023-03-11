' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.GenerateType

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateType
    <Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
    Partial Public Class GenerateTypeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest
#Region "Same Project"
#Region "SameProject SameFile"
        <Fact>
        Public Async Function GenerateTypeDefaultValues() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class Program
    Sub Main()
        Dim f As [|$$Goo|]
    End Sub
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Class Program
    Sub Main()
        Dim f As Goo
    End Sub
End Class

Class Goo
End Class
</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact>
        Public Async Function GenerateTypeInsideNamespace() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.Goo$$|]
    End Sub
End Class

Namespace A
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Class Program
    Sub Main()
        Dim f As A.Goo
    End Sub
End Class

Namespace A
    Class Goo
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact>
        Public Async Function GenerateTypeInsideQualifiedNamespace() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Class Program
    Sub Main()
        Dim f As A.B.Goo
    End Sub
End Class

Namespace A.B
    Class Goo
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact>
        Public Async Function GenerateTypeWithinQualifiedNestedNamespace() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.B.C.Goo$$|]
    End Sub
End Class

Namespace A.B
    Namespace C
    End Namespace
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Class Program
    Sub Main()
        Dim f As A.B.C.Goo
    End Sub
End Class

Namespace A.B
    Namespace C
        Class Goo
        End Class
    End Namespace
End Namespace</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact>
        Public Async Function GenerateTypeWithinNestedQualifiedNamespace() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.B.C.Goo$$|]
    End Sub
End Class

Namespace A
    Namespace B.C
    End Namespace
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Class Program
    Sub Main()
        Dim f As A.B.C.Goo
    End Sub
End Class

Namespace A
    Namespace B.C
        Class Goo
        End Class
    End Namespace
End Namespace</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact>
        Public Async Function GenerateTypeWithConstructorMembers() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class Program
    Sub Main()
        Dim f = New [|$$Goo|](bar:=1, baz:=2)
    End Sub
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Class Program
    Sub Main()
        Dim f = New Goo(bar:=1, baz:=2)
    End Sub
End Class

Class Goo
    Private bar As Integer
    Private baz As Integer

    Public Sub New(bar As Integer, baz As Integer)
        Me.bar = bar
        Me.baz = baz
    End Sub
End Class
</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact>
        Public Async Function GenerateTypeWithBaseTypes() As Task
            Await TestWithMockedGenerateTypeDialog(
            initial:=<Text>Imports System.Collections.Generic
Class Program
    Sub Main()
        Dim f As List(Of Integer) = New [|$$Goo|]()
    End Sub
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
            expected:=<Text>Imports System.Collections.Generic
Class Program
    Sub Main()
        Dim f As List(Of Integer) = New Goo()
    End Sub
End Class

Class Goo
    Inherits List(Of Integer)
End Class
</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact>
        Public Async Function GenerateTypeWithPublicInterface() As Task
            Await TestWithMockedGenerateTypeDialog(
            initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.B.C.Goo$$|]
    End Sub
End Class
Namespace A
    Namespace B.C
    End Namespace
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
            expected:=<Text>Class Program
    Sub Main()
        Dim f As A.B.C.Goo
    End Sub
End Class
Namespace A
    Namespace B.C
        Public Interface Goo
        End Interface
    End Namespace
End Namespace</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False)
        End Function

        <Fact>
        Public Async Function GenerateTypeWithInternalStruct() As Task
            Await TestWithMockedGenerateTypeDialog(
            initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.B.C.Goo$$|]
    End Sub
End Class
Namespace A
    Namespace B.C
    End Namespace
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
            expected:=<Text>Class Program
    Sub Main()
        Dim f As A.B.C.Goo
    End Sub
End Class
Namespace A
    Namespace B.C
        Friend Structure Goo
        End Structure
    End Namespace
End Namespace</Text>.NormalizedValue,
accessibility:=Accessibility.Friend,
typeKind:=TypeKind.Structure,
isNewFile:=False)
        End Function

        <Fact>
        Public Async Function GenerateTypeWithDefaultEnum() As Task
            Await TestWithMockedGenerateTypeDialog(
            initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class
Namespace A
    Namespace B
    End Namespace
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
            expected:=<Text>Class Program
    Sub Main()
        Dim f As A.B.Goo
    End Sub
End Class
Namespace A
    Namespace B
        Enum Goo
        End Enum
    End Namespace
End Namespace</Text>.NormalizedValue,
accessibility:=Accessibility.NotApplicable,
typeKind:=TypeKind.Enum,
isNewFile:=False)
        End Function
#End Region

#Region "SameProject ExistingFile"
        <Fact>
        Public Async Function GenerateTypeInExistingEmptyFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                       <Document FilePath="Test2.vb">
                                       </Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace A.B
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False,
existingFilename:="Test2.vb")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        Public Async Function GenerateTypeInExistingEmptyFile_Usings_Folders() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Goo$$|]
    End Sub
End Class</Document>
                                       <Document Folders="outer\inner" FilePath="Test2.vb">
                                       </Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace outer.inner
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports outer.inner

Class Program
    Sub Main()
        Dim f As Goo
    End Sub
End Class</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False,
existingFilename:="Test2.vb")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        Public Async Function GenerateTypeInExistingEmptyFile_NoUsings_Folders_NotSimpleName() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                       <Document FilePath="Test2.vb" Folders="outer\inner">
                                       </Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace A.B
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False,
existingFilename:="Test2.vb")
        End Function
#End Region

#Region "SameProject NewFile"
        <WpfFact>
        Public Async Function GenerateTypeInNewFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace A.B
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileFolderContainers:=ImmutableArray(Of String).Empty,
newFileName:="Test2.vb")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact>
        Public Async Function GenerateType_UsingsNotNeeded_InNewFile_InFolder() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Namespace outer
    Namespace inner
        Class Program
            Sub Main()
                Dim f As [|Goo$$|]
            End Sub
        End Class
    End Namespace
End Namespace</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace outer.inner
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileFolderContainers:=ImmutableArray.Create("outer", "inner"),
newFileName:="Test2.vb")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898452")>
        <WpfFact>
        Public Async Function GenerateType_InValidFolderNameNotMadeNamespace() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Namespace outer
    Namespace inner
        Class Program
            Sub Main()
                Dim f As [|Goo$$|]
            End Sub
        End Class
    End Namespace
End Namespace</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Public Interface Goo
End Interface
</Text>.NormalizedValue,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileFolderContainers:=ImmutableArray.Create("@@@@@", "#####"),
areFoldersValidIdentifiers:=False,
newFileName:="Test2.vb")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907454")>
        <WpfFact>
        Public Async Function GenerateType_UsingsNeeded_InNewFile_InFolder() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|$$Goo|]
    End Sub
End Class</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace outer.inner
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports BarBaz.outer.inner

Class Program
    Sub Main()
        Dim f As Goo
    End Sub
End Class</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileFolderContainers:=ImmutableArray.Create("outer", "inner"),
newFileName:="Test2.vb")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907454")>
        <WpfFact>
        Public Async Function GenerateType_UsingsPresentAlready_InNewFile_InFolder() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb" Folders="outer">
Imports BarBaz.outer

Class Program
    Sub Main()
        Dim f As [|$$Goo|]
    End Sub
End Class</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace outer
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports BarBaz.outer

Class Program
    Sub Main()
        Dim f As Goo
    End Sub
End Class</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileFolderContainers:=ImmutableArray.Create("outer"),
newFileName:="Test2.vb")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact>
        Public Async Function GenerateType_UsingsNotNeeded_InNewFile_InFolder_NotSimpleName() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace A.B
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileFolderContainers:=ImmutableArray.Create("outer", "inner"),
newFileName:="Test2.vb")
        End Function
#End Region
#End Region
#Region "SameLanguage DifferentProject"
#Region "SameLanguage DifferentProject ExistingFile"
        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectEmptyFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                       <Document FilePath="Test2.vb">
                                       </Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace Global.A.B
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False,
existingFilename:="Test2.vb",
projectName:="Assembly2")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectExistingFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                       <CompilationOptions RootNamespace="Zoozoo"/>
                                       <Document FilePath="Test2.vb" Folders="outer\inner">Namespace Global.BarBaz.A
    Namespace B
    End Namespace
End Namespace</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace Global.BarBaz.A
    Namespace B
        Public Interface Goo
        End Interface
    End Namespace
End Namespace</Text>.NormalizedValue,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False,
existingFilename:="Test2.vb",
projectName:="Assembly2")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectExistingFile_Usings_Folders() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                       <CompilationOptions RootNamespace="Zoozoo"/>
                                       <Document FilePath="Test2.vb" Folders="outer\inner">Namespace A
    Namespace B
    End Namespace
End Namespace</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace A
    Namespace B
    End Namespace
End Namespace

Namespace outer.inner
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports Zoozoo.outer.inner

Class Program
    Sub Main()
        Dim f As Goo
    End Sub
End Class

Namespace A.B
End Namespace</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False,
existingFilename:="Test2.vb",
projectName:="Assembly2")
        End Function
#End Region
#Region "SameLanguage DifferentProject NewFile"
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectNewFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace Global.A.B
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileName:="Test2.vb",
newFileFolderContainers:=ImmutableArray(Of String).Empty,
projectName:="Assembly2")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_Usings() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Goo$$|]
    End Sub
End Class</Document>
                                   </Project>
                                   <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                       <CompilationOptions RootNamespace="Zoozoo"/>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace outer.inner
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports Zoozoo.outer.inner

Class Program
    Sub Main()
        Dim f As Goo
    End Sub
End Class</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileName:="Test2.vb",
newFileFolderContainers:=ImmutableArray.Create("outer", "inner"),
projectName:="Assembly2")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_NoUsings_NotSimpleName() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                       <CompilationOptions RootNamespace="Zoozoo"/>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace Global.BarBaz.A.B
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileName:="Test2.vb",
newFileFolderContainers:=ImmutableArray(Of String).Empty,
projectName:="Assembly2")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_NoUsings_NotSimpleName_ProjectReference() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                       <CompilationOptions RootNamespace="Zoozoo"/>
                                       <Document FilePath="Test2.vb">
Namespace A.B
    Public Class Bar
    End Class
End Namespace</Document>
                                   </Project>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <ProjectReference>Assembly2</ProjectReference>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Zoozoo.A.B.Goo$$|]
    End Sub
End Class</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Namespace A.B
    Public Interface Goo
    End Interface
End Namespace
</Text>.NormalizedValue,
checkIfUsingsNotIncluded:=False,
expectedTextWithUsings:=<Text></Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileName:="Test3.vb",
newFileFolderContainers:=ImmutableArray(Of String).Empty,
projectName:="Assembly2")
        End Function
#End Region
#End Region
#Region "Different Language"
        <WpfFact>
        Public Async Function GenerateTypeIntoDifferentLanguageNewFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>namespace A.B
{
    public class Goo
    {
    }
}</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=ImmutableArray(Of String).Empty,
projectName:="Assembly2")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact>
        Public Async Function GenerateTypeIntoDifferentLanguageNewFile_Folders_Imports() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                       <CompilationOptions RootNamespace="Zoozoo"/>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>namespace outer.inner
{
    public class Goo
    {
    }
}</Text>.NormalizedValue,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports outer.inner

Class Program
    Sub Main()
        Dim f As Goo
    End Sub
End Class

Namespace A.B
End Namespace</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=ImmutableArray.Create("outer", "inner"),
projectName:="Assembly2")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact>
        Public Async Function GenerateTypeIntoDifferentLanguageNewFile_Folders_NoImports_NotSimpleName() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>namespace A.B
{
    public class Goo
    {
    }
}</Text>.NormalizedValue,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=ImmutableArray.Create("outer", "inner"),
projectName:="Assembly2")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact>
        Public Async Function GenerateTypeIntoDifferentLanguageNewFile_Folders_Imports_DefaultNamespace() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>namespace ConsoleApplication.outer.inner
{
    public class Goo
    {
    }
}</Text>.NormalizedValue,
defaultNamespace:="ConsoleApplication",
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports ConsoleApplication.outer.inner

Class Program
    Sub Main()
        Dim f As Goo
    End Sub
End Class

Namespace A.B
End Namespace</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=ImmutableArray.Create("outer", "inner"),
projectName:="Assembly2")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact>
        Public Async Function GenerateTypeIntoDifferentLanguageNewFile_Folders_NoImports_NotSimpleName_DefaultNamespace() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>namespace BarBaz.A.B
{
    public class Goo
    {
    }
}</Text>.NormalizedValue,
defaultNamespace:="ConsoleApplication",
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=ImmutableArray.Create("outer", "inner"),
projectName:="Assembly2")
        End Function

        <Fact>
        Public Async Function GenerateTypeIntoDifferentLanguageExistingEmptyFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                       <Document Folders="outer\inner" FilePath="Test2.cs">
                                       </Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>namespace A.B
{
    public class Goo
    {
    }
}</Text>.NormalizedValue,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=False,
existingFilename:="Test2.cs",
projectName:="Assembly2")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        Public Async Function GenerateTypeIntoDifferentLanguageExistingEmptyFile_Imports_Folder() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Goo$$|]
    End Sub
End Class</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                       <Document Folders="outer\inner" FilePath="Test2.cs">
                                       </Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>namespace outer.inner
{
    public class Goo
    {
    }
}</Text>.NormalizedValue,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports outer.inner

Class Program
    Sub Main()
        Dim f As Goo
    End Sub
End Class</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=False,
existingFilename:="Test2.cs",
projectName:="Assembly2")
        End Function

        <Fact>
        Public Async Function GenerateTypeIntoDifferentLanguageExistingNonEmptyFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                       <Document FilePath="Test2.cs">
namespace A
{
}</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>
namespace A
{
}

namespace A.B
{
    public class Goo
    {
    }
}</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=False,
existingFilename:="Test2.cs",
projectName:="Assembly2")
        End Function

        <Fact>
        Public Async Function GenerateTypeIntoDifferentLanguageExistingTargetFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                       <Document FilePath="Test2.cs">namespace A
{
    namespace B
    {
    }
}</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>namespace A
{
    namespace B
    {
        public class Goo
        {
        }
    }
}</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=False,
existingFilename:="Test2.cs",
projectName:="Assembly2")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858826")>
        <WpfFact>
        Public Async Function GenerateTypeIntoDifferentLanguageNewFileAdjustTheFileExtension() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Goo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>namespace A.B
{
    public class Goo
    {
    }
}</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=ImmutableArray(Of String).Empty,
projectName:="Assembly2")
        End Function
#End Region
#Region "Bugfix"
        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861462")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/873066")>
        Public Async Function GenerateTypeWithProperAccessibilityAndTypeKind_1() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Public Class C
    Implements [|$$D|]
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="D",
expected:=<Text>Public Class C
    Implements D
End Class

Public Interface D
End Interface
</Text>.NormalizedValue,
isNewFile:=False,
typeKind:=TypeKind.Interface,
accessibility:=Accessibility.Public,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(True, TypeKindOptions.Interface))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861462")>
        Public Async Function GenerateTypeWithProperAccessibilityAndTypeKind_2() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Public Class CC
    Inherits [|$$DD|]
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="DD",
expected:=<Text>Public Class CC
    Inherits DD
End Class

Class DD
End Class
</Text>.NormalizedValue,
isNewFile:=False,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(True, TypeKindOptions.Class))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861462")>
        Public Async Function GenerateTypeWithProperAccessibilityAndTypeKind_3() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Public Interface CCC
    Inherits [|$$DDD|]
End Interface</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="DDD",
expected:=<Text>Public Interface CCC
    Inherits DDD
End Interface

Interface DDD
End Interface
</Text>.NormalizedValue,
isNewFile:=False,
typeKind:=TypeKind.Interface,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(True, TypeKindOptions.Interface))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861462")>
        Public Async Function GenerateTypeWithProperAccessibilityAndTypeKind_4() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Public Structure CCC
    Implements [|$$DDD|]
End Structure</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="DDD",
expected:=<Text>Public Structure CCC
    Implements DDD
End Structure

Public Interface DDD
End Interface
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(True, TypeKindOptions.Interface))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861362")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/869593")>
        Public Async Function GenerateTypeWithModuleOption() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s as [|$$A.B.C|]
    End Sub
End Module

Namespace A
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="B",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s as A.B.C
    End Sub
End Module

Namespace A
    Module B
    End Module
End Namespace</Text>.NormalizedValue,
isNewFile:=False,
typeKind:=TypeKind.Module,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Module))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861362")>
        Public Async Function GenerateTypeInMemberAccessExpression() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s = [|$$A.B|]
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="A",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s = A.B
    End Sub
End Module

Public Module A
End Module
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Module,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.MemberAccessWithNamespace))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861362")>
        Public Async Function GenerateTypeInMemberAccessExpressionWithNamespace() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Namespace A
    Module Program
        Sub Main(args As String())
            Dim s = [|$$A.B.C|]
        End Sub
    End Module
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="B",
expected:=<Text>Namespace A
    Module Program
        Sub Main(args As String())
            Dim s = A.B.C
        End Sub
    End Module

    Public Module B
    End Module
End Namespace</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Module,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.MemberAccessWithNamespace))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/876202")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883531")>
        Public Async Function GenerateType_NoParameterLessConstructor() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s = new [|$$Goo|]()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="B",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s = new Goo()
    End Sub
End Module

Public Structure B
End Structure
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Structure,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")>
        Public Async Function GenerateTypeWithoutEnumForGenericsInMemberAccessExpression() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s = [|$$Goo(Of Bar).D|]
    End Sub
End Module

Class Bar
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s = Goo(Of Bar).D
    End Sub
End Module

Class Bar
End Class

Public Class Goo(Of T)
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")>
        Public Async Function GenerateTypeWithoutEnumForGenericsInNameContext() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s As [|$$Goo(Of Bar)|]
    End Sub
End Module

Class Bar
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s As Goo(Of Bar)
    End Sub
End Module

Class Bar
End Class

Public Class Goo(Of T)
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Interface Or TypeKindOptions.Delegate))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")>
        Public Async Function GenerateTypeInMemberAccessWithNSForModule() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s = [|$$Goo.Bar|].Baz
    End Sub
End Module

Namespace Goo
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s = Goo.Bar.Baz
    End Sub
End Module

Namespace Goo
    Public Class Bar
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.MemberAccessWithNamespace))
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")>
        Public Async Function GenerateTypeInMemberAccessWithGlobalNSForModule() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s = [|$$Bar|].Baz
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s = Bar.Baz
    End Sub
End Module

Public Class Bar
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.MemberAccessWithNamespace))
        End Function

        <Fact>
        Public Async Function GenerateTypeInMemberAccessWithoutNS() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s = [|$$Bar|].Baz
    End Sub
End Module

Namespace Bar
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
isMissing:=True)
        End Function

#End Region
#Region "Delegates"
        <Fact>
        Public Async Function GenerateTypeDelegateFromObjectCreationExpression() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s = New [|$$MyD|](AddressOf goo)
    End Sub

    Sub goo()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s = New MyD(AddressOf goo)
    End Sub

    Sub goo()
    End Sub
End Module

Public Delegate Sub MyD()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateFromObjectCreationExpressionIntoNamespace() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim goo = New NS.[|$$MyD|](Sub()
                             End Sub)
    End Sub
End Module

Namespace NS
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim goo = New NS.MyD(Sub()
                             End Sub)
    End Sub
End Module

Namespace NS
    Public Delegate Sub MyD()
End Namespace</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateFromObjectCreationExpression_1() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim goo = New [|$$NS.MyD|](Function(n) n)
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim goo = New NS.MyD(Function(n) n)
    End Sub
End Module

Namespace NS
    Public Delegate Function MyD(n As Object) As Object
End Namespace
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateFromObjectCreationExpression_2() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim goo = New [|$$MyD|](Sub() System.Console.WriteLine(1))
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim goo = New MyD(Sub() System.Console.WriteLine(1))
    End Sub
End Module

Public Delegate Sub MyD()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateFromObjectCreationExpression_3() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim goo = New [|$$MyD|](Function(n As Integer)
                              Return n + n
                          End Function)
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim goo = New MyD(Function(n As Integer)
                              Return n + n
                          End Function)
    End Sub
End Module

Public Delegate Function MyD(n As Integer) As Integer
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateAddressOfExpression() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As [|$$MyD(Of Integer)|] = AddressOf goo(Of Integer)
    End Sub
    Public Sub goo(Of T)()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As MyD(Of Integer) = AddressOf goo(Of Integer)
    End Sub
    Public Sub goo(Of T)()
    End Sub
End Module

Public Delegate Sub MyD(Of T)()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Interface Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateAddressOfExpressionWrongTypeArgument_1() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As [|$$MyD|] = AddressOf goo(Of Integer)
    End Sub
    Public Sub goo(Of T)()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As MyD = AddressOf goo(Of Integer)
    End Sub
    Public Sub goo(Of T)()
    End Sub
End Module

Public Delegate Sub MyD(Of T)()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateAddressOfExpressionWrongTypeArgument_2() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As [|$$MyD|] = AddressOf goo
    End Sub
    Public Sub goo(Of T)()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As MyD = AddressOf goo
    End Sub
    Public Sub goo(Of T)()
    End Sub
End Module

Public Delegate Sub MyD(Of T)()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateAddressOfExpressionWrongTypeArgument_3() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As [|$$MyD|] = AddressOf goo
    End Sub
    Public Sub goo(Of T)()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As MyD = AddressOf goo
    End Sub
    Public Sub goo(Of T)()
    End Sub
End Module

Public Delegate Sub MyD(Of T)()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateWithNoInitializer() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As [|$$MyD|]
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As MyD
    End Sub
End Module

Public Delegate Sub MyD()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateWithLambda_MultiLineFunction() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As [|$$MyD|] = Function()
                             Return 0
                         End Function
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As MyD = Function()
                             Return 0
                         End Function
    End Sub
End Module

Public Delegate Function MyD() As Integer
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateWithLambda_SingleLineFunction() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim a As [|$$MyD|] = Function(n As Integer) ""
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim a As MyD = Function(n As Integer) ""
    End Sub
End Module

Public Delegate Function MyD(n As Integer) As String
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateWithLambda_MultiLineSub() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As [|$$MyD|] = Sub()
                         End Sub
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As MyD = Sub()
                         End Sub
    End Sub
End Module

Public Delegate Sub MyD()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateWithLambda_SingleLineSub() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim a As [|$$MyD|] = Sub(n As Double) Console.WriteLine(0)
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim a As MyD = Sub(n As Double) Console.WriteLine(0)
    End Sub
End Module

Public Delegate Sub MyD(n As Double)
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateWithCast() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar = DirectCast(AddressOf goo, [|$$MyD|])
    End Sub
    Public Sub goo()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar = DirectCast(AddressOf goo, MyD)
    End Sub
    Public Sub goo()
    End Sub
End Module

Public Delegate Sub MyD()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateTypeDelegateWithCastAndError() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar = DirectCast(AddressOf goo, [|$$MyD|])
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar = DirectCast(AddressOf goo, MyD)
    End Sub
End Module

Public Delegate Sub MyD()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <WpfFact>
        Public Async Function GenerateDelegateTypeIntoDifferentLanguageNewFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Module Program
    Sub Main(args As String())
        Dim gooGoo = DirectCast(AddressOf Main, [|$$Bar|])
    End Sub
End Module</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>public delegate void Bar(string[] args);
</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=ImmutableArray(Of String).Empty,
projectName:="Assembly2")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860210")>
        Public Async Function GenerateTypeDelegate_NoInfo() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s as [|$$MyD(Of Integer)|]
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s as MyD(Of Integer)
    End Sub
End Module

Public Delegate Sub MyD(Of T)()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate)
        End Function
#End Region
#Region "Dev12Filtering"
        <Fact>
        Public Async Function GenerateType_Invocation_NoEnum_0() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim a = [|$$Baz.Goo|].Bar()
    End Sub
End Module

Namespace Baz
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim a = Baz.Goo.Bar()
    End Sub
End Module

Namespace Baz
    Public Class Goo
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertTypeKindAbsent:=New TypeKindOptions() {TypeKindOptions.Enum})
        End Function

        <Fact>
        Public Async Function GenerateType_Invocation_NoEnum_1() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim a = [|$$Goo.Bar|]()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim a = Goo.Bar()
    End Sub
End Module

Public Class Bar
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertTypeKindAbsent:=New TypeKindOptions() {TypeKindOptions.Enum})
        End Function

        <Fact>
        Public Async Function GenerateType_Invocation_NoEnum_2() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class C
    Custom Event E As Action
        AddHandler(value As [|$$Goo|])
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Class C
    Custom Event E As Action
        AddHandler(value As Goo)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Public Delegate Sub Goo()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertTypeKindPresent:=New TypeKindOptions() {TypeKindOptions.Delegate},
assertTypeKindAbsent:=New TypeKindOptions() {TypeKindOptions.Enum})
        End Function

        <Fact>
        Public Async Function GenerateType_Invocation_NoEnum_3() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class C
    Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As [|$$Goo|])
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>Class C
    Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Goo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Public Delegate Sub Goo()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertTypeKindPresent:=New TypeKindOptions() {TypeKindOptions.Delegate},
assertTypeKindAbsent:=New TypeKindOptions() {TypeKindOptions.Enum})
        End Function

        <Fact>
        Public Async Function GenerateType_Invocation_NoEnum_4() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Imports System
Module Program
    Sub Main(args As String())
        Dim s As Action = AddressOf [|NS.Bar$$|].Method
    End Sub
End Module

Namespace NS
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>Imports System
Module Program
    Sub Main(args As String())
        Dim s As Action = AddressOf NS.Bar.Method
    End Sub
End Module

Namespace NS
    Public Class Bar
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertTypeKindAbsent:=New TypeKindOptions() {TypeKindOptions.Enum})
        End Function

        <Fact>
        Public Async Function GenerateType_TypeConstraint_1() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Public Class Goo(Of T As [|$$Bar|])
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Public Class Goo(Of T As Bar)
End Class

Public Class Bar
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(True, TypeKindOptions.BaseList))
        End Function

        <Fact>
        Public Async Function GenerateType_TypeConstraint_2() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Outer
    Public Class Goo(Of T As [|$$Bar|])
    End Class
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Class Outer
    Public Class Goo(Of T As Bar)
    End Class
End Class

Public Class Bar
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.BaseList))
        End Function

        <Fact>
        Public Async Function GenerateType_TypeConstraint_3() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Public Class OuterOuter
    Public Class Outer
        Public Class Goo(Of T As [|$$Bar|])
        End Class
    End Class
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Public Class OuterOuter
    Public Class Outer
        Public Class Goo(Of T As Bar)
        End Class
    End Class
End Class

Public Class Bar
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(True, TypeKindOptions.BaseList))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_1() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class C1
    Custom Event E As [|$$Goo|]
        AddHandler(value As Goo)
        End AddHandler
        RemoveHandler(value As Goo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>
Class C1
    Custom Event E As Goo
        AddHandler(value As Goo)
        End AddHandler
        RemoveHandler(value As Goo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Public Delegate Sub Goo()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_2() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class C1
    Custom Event E As [|$$NS.Goo|]
        AddHandler(value As Goo)
        End AddHandler
        RemoveHandler(value As Goo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>
Class C1
    Custom Event E As NS.Goo
        AddHandler(value As Goo)
        End AddHandler
        RemoveHandler(value As Goo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Namespace NS
    Public Delegate Sub Goo()
End Namespace
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_3() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class C1
    Custom Event E As [|$$NS.Goo.MyDel|]
        AddHandler(value As Goo)
        End AddHandler
        RemoveHandler(value As Goo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Namespace NS
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Goo",
expected:=<Text>
Class C1
    Custom Event E As NS.Goo.MyDel
        AddHandler(value As Goo)
        End AddHandler
        RemoveHandler(value As Goo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Namespace NS
    Public Class Goo
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Module))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_4() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Goo
    Public Event F As [|$$Bar|]
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Class Goo
    Public Event F As Bar
End Class

Public Delegate Sub Bar()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_5() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Goo
    Public Event F As [|$$NS.Bar|]
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Class Goo
    Public Event F As NS.Bar
End Class

Namespace NS
    Public Delegate Sub Bar()
End Namespace
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_6() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Goo
    Public Event F As [|$$NS.Bar.MyDel|]
End Class

Namespace NS
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Class Goo
    Public Event F As NS.Bar.MyDel
End Class

Namespace NS
    Public Class Bar
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Module))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_7() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Bar
    Public WithEvents G As [|$$Delegate1|]
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Delegate1",
expected:=<Text>
Class Bar
    Public WithEvents G As Delegate1
End Class

Public Class Delegate1
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.BaseList))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_8() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Bar
    Public WithEvents G As [|$$NS.Delegate1|]
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Delegate1",
expected:=<Text>
Class Bar
    Public WithEvents G As NS.Delegate1
End Class

Namespace NS
    Public Class Delegate1
    End Class
End Namespace
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.BaseList))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_9() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Bar
    Public WithEvents G As [|$$NS.Delegate1.MyDel|]
End Class

Namespace NS
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Delegate1",
expected:=<Text>
Class Bar
    Public WithEvents G As NS.Delegate1.MyDel
End Class

Namespace NS
    Public Class Delegate1
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.BaseList))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_10() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Baz
    Public Class Goo
        Public Event F As [|$$Bar|]
    End Class
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Class Baz
    Public Class Goo
        Public Event F As Bar
    End Class
End Class

Public Delegate Sub Bar()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_11() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Public Class Baz
    Public Class Goo
        Public Event F As [|$$Bar|]
    End Class
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Public Class Baz
    Public Class Goo
        Public Event F As Bar
    End Class
End Class

Public Delegate Sub Bar()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(True, TypeKindOptions.Delegate))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_12() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Baz
    Public Class Bar
        Public WithEvents G As [|$$Delegate1|]
    End Class
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Delegate1",
expected:=<Text>
Class Baz
    Public Class Bar
        Public WithEvents G As Delegate1
    End Class
End Class

Public Class Delegate1
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.BaseList))
        End Function

        <Fact>
        Public Async Function GenerateType_Event_13() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Public Class Baz
    Public Class Bar
        Public WithEvents G As [|$$Delegate1|]
    End Class
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Delegate1",
expected:=<Text>
Public Class Baz
    Public Class Bar
        Public WithEvents G As Delegate1
    End Class
End Class

Public Class Delegate1
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(True, TypeKindOptions.BaseList))
        End Function

#End Region
    End Class
End Namespace
