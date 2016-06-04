' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic
Imports Microsoft.CodeAnalysis.GenerateType
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateType
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateType
    Partial Public Class GenerateTypeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest
#Region "Same Project"
#Region "SameProject SameFile"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeDefaultValues() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class Program
    Sub Main()
        Dim f As [|$$Foo|]
    End Sub
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Class Program
    Sub Main()
        Dim f As Foo
    End Sub
End Class

Class Foo
End Class
</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeInsideNamespace() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.Foo$$|]
    End Sub
End Class

Namespace A
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Class Program
    Sub Main()
        Dim f As A.Foo
    End Sub
End Class

Namespace A
    Class Foo
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeInsideQualifiedNamespace() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Class Program
    Sub Main()
        Dim f As A.B.Foo
    End Sub
End Class

Namespace A.B
    Class Foo
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeWithinQualifiedNestedNamespace() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.B.C.Foo$$|]
    End Sub
End Class

Namespace A.B
    Namespace C
    End Namespace
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Class Program
    Sub Main()
        Dim f As A.B.C.Foo
    End Sub
End Class

Namespace A.B
    Namespace C
        Class Foo
        End Class
    End Namespace
End Namespace</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeWithinNestedQualifiedNamespace() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.B.C.Foo$$|]
    End Sub
End Class

Namespace A
    Namespace B.C
    End Namespace
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Class Program
    Sub Main()
        Dim f As A.B.C.Foo
    End Sub
End Class

Namespace A
    Namespace B.C
        Class Foo
        End Class
    End Namespace
End Namespace</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeWithConstructorMembers() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class Program
    Sub Main()
        Dim f = New [|$$Foo|](bar:=1, baz:=2)
    End Sub
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Class Program
    Sub Main()
        Dim f = New Foo(bar:=1, baz:=2)
    End Sub
End Class

Class Foo
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeWithBaseTypes() As Task
            Await TestWithMockedGenerateTypeDialog(
            initial:=<Text>Imports System.Collections.Generic
Class Program
    Sub Main()
        Dim f As List(Of Integer) = New [|$$Foo|]()
    End Sub
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
            expected:=<Text>Imports System.Collections.Generic
Class Program
    Sub Main()
        Dim f As List(Of Integer) = New Foo()
    End Sub
End Class

Class Foo
    Inherits List(Of Integer)
End Class
</Text>.NormalizedValue,
isNewFile:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeWithPublicInterface() As Task
            Await TestWithMockedGenerateTypeDialog(
            initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.B.C.Foo$$|]
    End Sub
End Class
Namespace A
    Namespace B.C
    End Namespace
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
            expected:=<Text>Class Program
    Sub Main()
        Dim f As A.B.C.Foo
    End Sub
End Class
Namespace A
    Namespace B.C
        Public Interface Foo
        End Interface
    End Namespace
End Namespace</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeWithInternalStruct() As Task
            Await TestWithMockedGenerateTypeDialog(
            initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.B.C.Foo$$|]
    End Sub
End Class
Namespace A
    Namespace B.C
    End Namespace
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
            expected:=<Text>Class Program
    Sub Main()
        Dim f As A.B.C.Foo
    End Sub
End Class
Namespace A
    Namespace B.C
        Friend Structure Foo
        End Structure
    End Namespace
End Namespace</Text>.NormalizedValue,
accessibility:=Accessibility.Friend,
typeKind:=TypeKind.Structure,
isNewFile:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeWithDefaultEnum() As Task
            Await TestWithMockedGenerateTypeDialog(
            initial:=<Text>Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
    End Sub
End Class
Namespace A
    Namespace B
    End Namespace
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
            expected:=<Text>Class Program
    Sub Main()
        Dim f As A.B.Foo
    End Sub
End Class
Namespace A
    Namespace B
        Enum Foo
        End Enum
    End Namespace
End Namespace</Text>.NormalizedValue,
accessibility:=Accessibility.NotApplicable,
typeKind:=TypeKind.Enum,
isNewFile:=False)
        End Function
#End Region

#Region "SameProject ExistingFile"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeInExistingEmptyFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>Namespace A.B
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False,
existingFilename:="Test2.vb")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeInExistingEmptyFile_Usings_Folders() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Foo$$|]
    End Sub
End Class</Document>
                                       <Document Folders="outer\inner" FilePath="Test2.vb">

                                       </Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Namespace outer.inner
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports outer.inner

Class Program
    Sub Main()
        Dim f As Foo
    End Sub
End Class</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False,
existingFilename:="Test2.vb")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeInExistingEmptyFile_NoUsings_Folders_NotSimpleName() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>Namespace A.B
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False,
existingFilename:="Test2.vb")
        End Function
#End Region

#Region "SameProject NewFile"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeInNewFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Namespace A.B
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileFolderContainers:=New String(0) {},
newFileName:="Test2.vb")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_UsingsNotNeeded_InNewFile_InFolder() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Namespace outer
    Namespace inner
        Class Program
            Sub Main()
                Dim f As [|Foo$$|]
            End Sub
        End Class
    End Namespace
End Namespace</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Namespace outer.inner
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileFolderContainers:=New String() {"outer", "inner"},
newFileName:="Test2.vb")
        End Function

        <WorkItem(898452, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/898452")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_InValidFolderNameNotMadeNamespace() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Namespace outer
    Namespace inner
        Class Program
            Sub Main()
                Dim f As [|Foo$$|]
            End Sub
        End Class
    End Namespace
End Namespace</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Public Interface Foo
End Interface
</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileFolderContainers:=New String() {"@@@@@", "#####"},
areFoldersValidIdentifiers:=False,
newFileName:="Test2.vb")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WorkItem(907454, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907454")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_UsingsNeeded_InNewFile_InFolder() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|$$Foo|]
    End Sub
End Class</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Namespace outer.inner
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports BarBaz.outer.inner

Class Program
    Sub Main()
        Dim f As Foo
    End Sub
End Class</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileFolderContainers:=New String() {"outer", "inner"},
newFileName:="Test2.vb")
        End Function

        <WorkItem(907454, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907454")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_UsingsPresentAlready_InNewFile_InFolder() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb" Folders="outer">
Imports BarBaz.outer

Class Program
    Sub Main()
        Dim f As [|$$Foo|]
    End Sub
End Class</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Namespace outer
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports BarBaz.outer

Class Program
    Sub Main()
        Dim f As Foo
    End Sub
End Class</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileFolderContainers:=New String() {"outer"},
newFileName:="Test2.vb")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_UsingsNotNeeded_InNewFile_InFolder_NotSimpleName() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
    End Sub
End Class

Namespace A.B
End Namespace</Document>
                                   </Project>
                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Namespace A.B
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileFolderContainers:=New String() {"outer", "inner"},
newFileName:="Test2.vb")
        End Function
#End Region
#End Region
#Region "SameLanguage DifferentProject"
#Region "SameLanguage DifferentProject ExistingFile"
        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectEmptyFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>Namespace Global.A.B
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False,
existingFilename:="Test2.vb",
projectName:="Assembly2")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectExistingFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>Namespace Global.BarBaz.A
    Namespace B
        Public Interface Foo
        End Interface
    End Namespace
End Namespace</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=False,
existingFilename:="Test2.vb",
projectName:="Assembly2")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectExistingFile_Usings_Folders() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Foo$$|]
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
typeName:="Foo",
expected:=<Text>Namespace A
    Namespace B
    End Namespace
End Namespace

Namespace outer.inner
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports Zoozoo.outer.inner

Class Program
    Sub Main()
        Dim f As Foo
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
        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectNewFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>Namespace Global.A.B
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileName:="Test2.vb",
newFileFolderContainers:=New String(0) {},
projectName:="Assembly2")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_Usings() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Foo$$|]
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
typeName:="Foo",
expected:=<Text>Namespace outer.inner
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports Zoozoo.outer.inner

Class Program
    Sub Main()
        Dim f As Foo
    End Sub
End Class</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileName:="Test2.vb",
newFileFolderContainers:=New String() {"outer", "inner"},
projectName:="Assembly2")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_NoUsings_NotSimpleName() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>Namespace Global.BarBaz.A.B
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileName:="Test2.vb",
newFileFolderContainers:=New String(0) {},
projectName:="Assembly2")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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
        Dim f As [|Zoozoo.A.B.Foo$$|]
    End Sub
End Class</Document>
                                   </Project>

                               </Workspace>.ToString()
            Await TestWithMockedGenerateTypeDialog(
initial:=markupString,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Namespace A.B
    Public Interface Foo
    End Interface
End Namespace
</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsNotIncluded:=False,
expectedTextWithUsings:=<Text></Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Interface,
isNewFile:=True,
newFileName:="Test3.vb",
newFileFolderContainers:=New String(0) {},
projectName:="Assembly2")
        End Function
#End Region
#End Region
#Region "Different Language"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoDifferentLanguageNewFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>namespace A.B
{
    public class Foo
    {
    }
}</Text>.NormalizedValue,
isLine:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=New String(0) {},
projectName:="Assembly2")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoDifferentLanguageNewFile_Folders_Imports() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Foo$$|]
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
typeName:="Foo",
expected:=<Text>namespace outer.inner
{
    public class Foo
    {
    }
}</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports outer.inner

Class Program
    Sub Main()
        Dim f As Foo
    End Sub
End Class

Namespace A.B
End Namespace</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=New String() {"outer", "inner"},
projectName:="Assembly2")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoDifferentLanguageNewFile_Folders_NoImports_NotSimpleName() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>namespace A.B
{
    public class Foo
    {
    }
}</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=New String() {"outer", "inner"},
projectName:="Assembly2")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoDifferentLanguageNewFile_Folders_Imports_DefaultNamespace() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Foo$$|]
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
typeName:="Foo",
expected:=<Text>namespace ConsoleApplication.outer.inner
{
    public class Foo
    {
    }
}</Text>.NormalizedValue,
isLine:=False,
defaultNamespace:="ConsoleApplication",
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports ConsoleApplication.outer.inner

Class Program
    Sub Main()
        Dim f As Foo
    End Sub
End Class

Namespace A.B
End Namespace</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=New String() {"outer", "inner"},
projectName:="Assembly2")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoDifferentLanguageNewFile_Folders_NoImports_NotSimpleName_DefaultNamespace() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <CompilationOptions RootNamespace="BarBaz"/>
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>namespace BarBaz.A.B
{
    public class Foo
    {
    }
}</Text>.NormalizedValue,
isLine:=False,
defaultNamespace:="ConsoleApplication",
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=New String() {"outer", "inner"},
projectName:="Assembly2")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoDifferentLanguageExistingEmptyFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>namespace A.B
{
    public class Foo
    {
    }
}</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsNotIncluded:=True,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=False,
existingFilename:="Test2.cs",
projectName:="Assembly2")
        End Function

        <WorkItem(850101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850101")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoDifferentLanguageExistingEmptyFile_Imports_Folder() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|Foo$$|]
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
typeName:="Foo",
expected:=<Text>namespace outer.inner
{
    public class Foo
    {
    }
}</Text>.NormalizedValue,
isLine:=False,
checkIfUsingsIncluded:=True,
expectedTextWithUsings:=<Text>
Imports outer.inner

Class Program
    Sub Main()
        Dim f As Foo
    End Sub
End Class</Text>.NormalizedValue,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=False,
existingFilename:="Test2.cs",
projectName:="Assembly2")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoDifferentLanguageExistingNonEmptyFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>
namespace A
{
}

namespace A.B
{
    public class Foo
    {
    }
}</Text>.NormalizedValue,
isLine:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=False,
existingFilename:="Test2.cs",
projectName:="Assembly2")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoDifferentLanguageExistingTargetFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>namespace A
{
    namespace B
    {
        public class Foo
        {
        }
    }
}</Text>.NormalizedValue,
isLine:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=False,
existingFilename:="Test2.cs",
projectName:="Assembly2")
        End Function

        <WorkItem(858826, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858826")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeIntoDifferentLanguageNewFileAdjustTheFileExtension() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.Foo$$|]
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
typeName:="Foo",
expected:=<Text>namespace A.B
{
    public class Foo
    {
    }
}</Text>.NormalizedValue,
isLine:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=New String(0) {},
projectName:="Assembly2")
        End Function
#End Region
#Region "Bugfix"
        <WorkItem(861462, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861462")>
        <WorkItem(873066, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/873066")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <WorkItem(861462, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861462")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <WorkItem(861462, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861462")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <WorkItem(861462, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861462")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <WorkItem(861362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861362")>
        <WorkItem(869593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/869593")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <WorkItem(861362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861362")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <WorkItem(861362, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861362")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <WorkItem(876202, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/876202")>
        <WorkItem(883531, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883531")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_NoParameterLessConstructor() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s = new [|$$Foo|]()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="B",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s = new Foo()
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

        <WorkItem(861600, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeWithoutEnumForGenericsInMemberAccessExpression() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s = [|$$Foo(Of Bar).D|]
    End Sub
End Module

Class Bar
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s = Foo(Of Bar).D
    End Sub
End Module

Class Bar
End Class

Public Class Foo(Of T)
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure))
        End Function

        <WorkItem(861600, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeWithoutEnumForGenericsInNameContext() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s As [|$$Foo(Of Bar)|]
    End Sub
End Module

Class Bar
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s As Foo(Of Bar)
    End Sub
End Module

Class Bar
End Class

Public Class Foo(Of T)
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Interface Or TypeKindOptions.Delegate))
        End Function

        <WorkItem(861600, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeInMemberAccessWithNSForModule() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s = [|$$Foo.Bar|].Baz
    End Sub
End Module

Namespace Foo
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s = Foo.Bar.Baz
    End Sub
End Module

Namespace Foo
    Public Class Bar
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.MemberAccessWithNamespace))
        End Function

        <WorkItem(861600, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/861600")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeDelegateFromObjectCreationExpression() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim s = New [|$$MyD|](AddressOf foo)
    End Sub

    Sub foo()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim s = New MyD(AddressOf foo)
    End Sub

    Sub foo()
    End Sub
End Module

Public Delegate Sub MyD()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Delegate))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeDelegateFromObjectCreationExpressionIntoNamespace() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim foo = New NS.[|$$MyD|](Sub()
                             End Sub)
    End Sub
End Module

Namespace NS
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim foo = New NS.MyD(Sub()
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeDelegateFromObjectCreationExpression_1() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim foo = New [|$$NS.MyD|](Function(n) n)
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim foo = New NS.MyD(Function(n) n)
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeDelegateFromObjectCreationExpression_2() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim foo = New [|$$MyD|](Sub() System.Console.WriteLine(1))
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim foo = New MyD(Sub() System.Console.WriteLine(1))
    End Sub
End Module

Public Delegate Sub MyD()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Delegate))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeDelegateFromObjectCreationExpression_3() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim foo = New [|$$MyD|](Function(n As Integer)
                              Return n + n
                          End Function)
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim foo = New MyD(Function(n As Integer)
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeDelegateAddressOfExpression() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As [|$$MyD(Of Integer)|] = AddressOf foo(Of Integer)
    End Sub
    Public Sub foo(Of T)()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As MyD(Of Integer) = AddressOf foo(Of Integer)
    End Sub
    Public Sub foo(Of T)()
    End Sub
End Module

Public Delegate Sub MyD(Of T)()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Interface Or TypeKindOptions.Delegate))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeDelegateAddressOfExpressionWrongTypeArgument_1() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As [|$$MyD|] = AddressOf foo(Of Integer)
    End Sub
    Public Sub foo(Of T)()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As MyD = AddressOf foo(Of Integer)
    End Sub
    Public Sub foo(Of T)()
    End Sub
End Module

Public Delegate Sub MyD(Of T)()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeDelegateAddressOfExpressionWrongTypeArgument_2() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As [|$$MyD|] = AddressOf foo
    End Sub
    Public Sub foo(Of T)()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As MyD = AddressOf foo
    End Sub
    Public Sub foo(Of T)()
    End Sub
End Module

Public Delegate Sub MyD(Of T)()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeDelegateAddressOfExpressionWrongTypeArgument_3() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As [|$$MyD|] = AddressOf foo
    End Sub
    Public Sub foo(Of T)()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar As MyD = AddressOf foo
    End Sub
    Public Sub foo(Of T)()
    End Sub
End Module

Public Delegate Sub MyD(Of T)()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeDelegateWithCast() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar = DirectCast(AddressOf foo, [|$$MyD|])
    End Sub
    Public Sub foo()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar = DirectCast(AddressOf foo, MyD)
    End Sub
    Public Sub foo()
    End Sub
End Module

Public Delegate Sub MyD()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateTypeDelegateWithCastAndError() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim bar = DirectCast(AddressOf foo, [|$$MyD|])
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="MyD",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim bar = DirectCast(AddressOf foo, MyD)
    End Sub
End Module

Public Delegate Sub MyD()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.AllOptions Or TypeKindOptions.Delegate))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateDelegateTypeIntoDifferentLanguageNewFile() As Task
            Dim markupString = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Module Program
    Sub Main(args As String())
        Dim fooFoo = DirectCast(AddressOf Main, [|$$Bar|])
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
isLine:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
isNewFile:=True,
newFileName:="Test2.cs",
newFileFolderContainers:=New String(0) {},
projectName:="Assembly2")
        End Function

        <WorkItem(860210, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860210")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_Invocation_NoEnum_0() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim a = [|$$Baz.Foo|].Bar()
    End Sub
End Module

Namespace Baz
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim a = Baz.Foo.Bar()
    End Sub
End Module

Namespace Baz
    Public Class Foo
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertTypeKindAbsent:=New TypeKindOptions() {TypeKindOptions.Enum})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_Invocation_NoEnum_1() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Module Program
    Sub Main(args As String())
        Dim a = [|$$Foo.Bar|]()
    End Sub
End Module</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>Module Program
    Sub Main(args As String())
        Dim a = Foo.Bar()
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_Invocation_NoEnum_2() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class C
    Custom Event E As Action
        AddHandler(value As [|$$Foo|])
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Class C
    Custom Event E As Action
        AddHandler(value As Foo)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Public Delegate Sub Foo()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertTypeKindPresent:=New TypeKindOptions() {TypeKindOptions.Delegate},
assertTypeKindAbsent:=New TypeKindOptions() {TypeKindOptions.Enum})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_Invocation_NoEnum_3() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>Class C
    Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As [|$$Foo|])
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>Class C
    Custom Event E As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Foo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Public Delegate Sub Foo()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertTypeKindPresent:=New TypeKindOptions() {TypeKindOptions.Delegate},
assertTypeKindAbsent:=New TypeKindOptions() {TypeKindOptions.Enum})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_TypeConstraint_1() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Public Class Foo(Of T As [|$$Bar|])
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Public Class Foo(Of T As Bar)
End Class

Public Class Bar
End Class
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(True, TypeKindOptions.BaseList))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_TypeConstraint_2() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Outer
    Public Class Foo(Of T As [|$$Bar|])
    End Class
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Class Outer
    Public Class Foo(Of T As Bar)
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_TypeConstraint_3() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Public Class OuterOuter
    Public Class Outer
        Public Class Foo(Of T As [|$$Bar|])
        End Class
    End Class
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Public Class OuterOuter
    Public Class Outer
        Public Class Foo(Of T As Bar)
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_Event_1() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class C1
    Custom Event E As [|$$Foo|]
        AddHandler(value As Foo)
        End AddHandler
        RemoveHandler(value As Foo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>
Class C1
    Custom Event E As Foo
        AddHandler(value As Foo)
        End AddHandler
        RemoveHandler(value As Foo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Public Delegate Sub Foo()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Delegate))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_Event_2() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class C1
    Custom Event E As [|$$NS.Foo|]
        AddHandler(value As Foo)
        End AddHandler
        RemoveHandler(value As Foo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>
Class C1
    Custom Event E As NS.Foo
        AddHandler(value As Foo)
        End AddHandler
        RemoveHandler(value As Foo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Namespace NS
    Public Delegate Sub Foo()
End Namespace
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Delegate))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_Event_3() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class C1
    Custom Event E As [|$$NS.Foo.MyDel|]
        AddHandler(value As Foo)
        End AddHandler
        RemoveHandler(value As Foo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Namespace NS
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Foo",
expected:=<Text>
Class C1
    Custom Event E As NS.Foo.MyDel
        AddHandler(value As Foo)
        End AddHandler
        RemoveHandler(value As Foo)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class

Namespace NS
    Public Class Foo
    End Class
End Namespace</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Class,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Module))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_Event_4() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Foo
    Public Event F As [|$$Bar|]
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Class Foo
    Public Event F As Bar
End Class

Public Delegate Sub Bar()
</Text>.NormalizedValue,
isNewFile:=False,
accessibility:=Accessibility.Public,
typeKind:=TypeKind.Delegate,
assertGenerateTypeDialogOptions:=New GenerateTypeDialogOptions(False, TypeKindOptions.Delegate))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_Event_5() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Foo
    Public Event F As [|$$NS.Bar|]
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Class Foo
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_Event_6() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Foo
    Public Event F As [|$$NS.Bar.MyDel|]
End Class

Namespace NS
End Namespace</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Class Foo
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_Event_10() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Class Baz
    Public Class Foo
        Public Event F As [|$$Bar|]
    End Class
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Class Baz
    Public Class Foo
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function GenerateType_Event_11() As Task
            Await TestWithMockedGenerateTypeDialog(
initial:=<Text>
Public Class Baz
    Public Class Foo
        Public Event F As [|$$Bar|]
    End Class
End Class</Text>.NormalizedValue,
languageName:=LanguageNames.VisualBasic,
typeName:="Bar",
expected:=<Text>
Public Class Baz
    Public Class Foo
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
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
