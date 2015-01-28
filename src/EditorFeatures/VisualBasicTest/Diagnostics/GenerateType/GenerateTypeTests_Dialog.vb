' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
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
        Public Sub GenerateTypeDefaultValues()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeInsideNamespace()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeInsideQualifiedNamespace()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithinQualifiedNestedNamespace()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithinNestedQualifiedNamespace()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithConstructorMembers()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithBaseTypes()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithPublicInterface()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithInternalStruct()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithDefaultEnum()
            TestWithMockedGenerateTypeDialog(
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
        End Sub
#End Region

#Region "SameProject ExistingFile"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeInExistingEmptyFile()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeInExistingEmptyFile_Usings_Folders()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeInExistingEmptyFile_NoUsings_Folders_NotSimpleName()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub
#End Region

#Region "SameProject NewFile"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeInNewFile()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_UsingsNotNeeded_InNewFile_InFolder()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(898452)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_InValidFolderNameNotMadeNamespace()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <WorkItem(907454)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_UsingsNeeded_InNewFile_InFolder()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(907454)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_UsingsPresentAlready_InNewFile_InFolder()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_UsingsNotNeeded_InNewFile_InFolder_NotSimpleName()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub
#End Region
#End Region
#Region "SameLanguage DifferentProject"
#Region "SameLanguage DifferentProject ExistingFile"
        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoSameLanguageDifferentProjectEmptyFile()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoSameLanguageDifferentProjectExistingFile()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoSameLanguageDifferentProjectExistingFile_Usings_Folders()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub
#End Region
#Region "SameLanguage DifferentProject NewFile"
        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoSameLanguageDifferentProjectNewFile()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_Usings()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_NoUsings_NotSimpleName()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoSameLanguageDifferentProjectNewFile_Folders_NoUsings_NotSimpleName_ProjectReference()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub
#End Region
#End Region
#Region "Different Language"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoDifferentLanguageNewFile()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoDifferentLanguageNewFile_Folders_Imports()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoDifferentLanguageNewFile_Folders_NoImports_NotSimpleName()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoDifferentLanguageNewFile_Folders_Imports_DefaultNamespace()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoDifferentLanguageNewFile_Folders_NoImports_NotSimpleName_DefaultNamespace()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoDifferentLanguageExistingEmptyFile()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(850101)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoDifferentLanguageExistingEmptyFile_Imports_Folder()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoDifferentLanguageExistingNonEmptyFile()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoDifferentLanguageExistingTargetFile()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(858826)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeIntoDifferentLanguageNewFileAdjustTheFileExtension()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub
#End Region
#Region "Bugfix"
        <WorkItem(861462)>
        <WorkItem(873066)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithProperAccessibilityAndTypeKind_1()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(861462)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithProperAccessibilityAndTypeKind_2()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(861462)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithProperAccessibilityAndTypeKind_3()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(861462)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithProperAccessibilityAndTypeKind_4()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(861362)>
        <WorkItem(869593)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithModuleOption()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(861362)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeInMemberAccessExpression()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(861362)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeInMemberAccessExpressionWithNamespace()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(876202)>
        <WorkItem(883531)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_NoParameterLessConstructor()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(861600)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithoutEnumForGenericsInMemberAccessExpression()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(861600)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithoutEnumForGenericsInNameContext()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(861600)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeInMemberAccessWithNSForModule()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(861600)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeInMemberAccessWithGlobalNSForModule()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeInMemberAccessWithoutNS()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

#End Region
#Region "Delegates"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateFromObjectCreationExpression()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateFromObjectCreationExpressionIntoNamespace()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateFromObjectCreationExpression_1()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateFromObjectCreationExpression_2()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateFromObjectCreationExpression_3()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateAddressOfExpression()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateAddressOfExpressionWrongTypeArgument_1()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateAddressOfExpressionWrongTypeArgument_2()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateAddressOfExpressionWrongTypeArgument_3()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateWithNoInitializer()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateWithLambda_MultiLineFunction()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateWithLambda_SingleLineFunction()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateWithLambda_MultiLineSub()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateWithLambda_SingleLineSub()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateWithCast()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegateWithCastAndError()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateDelegateTypeIntoDifferentLanguageNewFile()
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
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <WorkItem(860210)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeDelegate_NoInfo()
            TestWithMockedGenerateTypeDialog(
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
        End Sub
#End Region
#Region "Dev12Filtering"
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Invocation_NoEnum_0()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Invocation_NoEnum_1()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Invocation_NoEnum_2()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Invocation_NoEnum_3()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Invocation_NoEnum_4()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_TypeConstraint_1()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_TypeConstraint_2()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_TypeConstraint_3()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_1()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_2()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_3()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_4()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_5()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_6()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_7()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_8()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_9()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_10()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_11()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_12()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateType_Event_13()
            TestWithMockedGenerateTypeDialog(
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
        End Sub

#End Region
    End Class
End Namespace
