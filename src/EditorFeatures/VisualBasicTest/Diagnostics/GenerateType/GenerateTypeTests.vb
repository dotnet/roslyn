' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateType
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateType
    Public Class GenerateTypeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateTypeCodeFixProvider())
        End Function

        Protected Overrides Function MassageActions(actions As IList(Of CodeAction)) As IList(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateTypeParameterFromArgumentInferT()
            Test(
NewLines("Module Program \n Sub Main() \n Dim f As [|Foo(Of Integer)|] \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n Dim f As Foo(Of Integer) \n End Sub \n End Module \n Friend Class Foo(Of T) \n End Class"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateClassFromTypeParameter()
            Test(
NewLines("Class C \n Dim emp As List(Of [|Employee|]) \n End Class"),
NewLines("Class C \n Dim emp As List(Of Employee) \n Private Class Employee \n End Class \n End Class"),
index:=2)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateClassFromFieldDeclarationIntoSameType()
            Test(
NewLines("Class C \n dim f as [|Foo|] \n End Class"),
NewLines("Class C \n dim f as Foo \n Private Class Foo \n End Class \n End Class"),
index:=2)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateClassFromFieldDeclarationIntoSameNamespace()
            Test(
NewLines("Class C \n dim f as [|Foo|] \n End Class"),
NewLines("Class C \n dim f as Foo \n End Class \n Friend Class Foo \n End Class"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestMissingOnLowercaseName()
            TestMissing(
NewLines("Class C \n dim f as [|foo|] \n End Class"))
        End Sub

        <WorkItem(539716)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateClassFromFullyQualifiedFieldIntoSameNamespace()
            Test(
NewLines("Namespace NS \n Class Foo \n Private x As New NS.[|Bar|] \n End Class \n End Namespace"),
NewLines("Namespace NS \n Class Foo \n Private x As New NS.Bar \n End Class \n Friend Class Bar \n End Class \n End Namespace"),
index:=1,
parseOptions:=Nothing) ' Namespaces not supported in script
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateClassWithCtorFromObjectCreation()
            Test(
NewLines("Class C \n Dim f As Foo = New [|Foo|]() \n End Class"),
NewLines("Class C \n Dim f As Foo = New Foo() \n Private Class Foo \n Public Sub New() \n End Sub \n End Class \n End Class"),
index:=2)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestCreateException()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Throw New [|Foo|]() \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Imports System.Runtime.Serialization \n Module Program \n Sub Main(args As String()) \n Throw New Foo() \n End Sub \n End Module \n <Serializable> Friend Class Foo \n Inherits Exception \n Public Sub New() \n End Sub \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As SerializationInfo, context As StreamingContext) \n MyBase.New(info, context) \n End Sub \n End Class"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestCreateFieldDelegatingConstructor()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Call New [|Foo|](1, ""blah"") \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Call New Foo(1, ""blah"") \n End Sub \n End Module \n Friend Class Foo \n Private v1 As Integer \n Private v2 As String \n Public Sub New(v1 As Integer, v2 As String) \n Me.v1 = v1 \n Me.v2 = v2 \n End Sub \n End Class"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestCreateBaseDelegatingConstructor()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim d As B = New [|D|](4) \n End Sub \n End Module \n Class B \n Protected Sub New(value As Integer) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim d As B = New D(4) \n End Sub \n End Module \n Friend Class D \n Inherits B \n Public Sub New(value As Integer) \n MyBase.New(value) \n End Sub \n End Class \n Class B \n Protected Sub New(value As Integer) \n End Sub \n End Class"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateIntoNamespace()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Namespace Outer \n Module Program \n Sub Main(args As String()) \n Call New [|Blah|]() \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Namespace Outer \n Module Program \n Sub Main(args As String()) \n Call New Blah() \n End Sub \n End Module \n Friend Class Blah \n Public Sub New() \n End Sub \n End Class \n End Namespace"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateAssignmentToBaseField()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(i As Integer) \n Dim d As B = New [|D|](i) \n End Sub \n End Module \n Class B \n Protected i As Integer \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(i As Integer) \n Dim d As B = New D(i) \n End Sub \n End Module \n Friend Class D \n Inherits B \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class \n Class B \n Protected i As Integer \n End Class"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateGenericType()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Outer(Of M) \n Sub Main(i As Integer) \n Call New [|Foo(Of M)|] \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Outer(Of M) \n Sub Main(i As Integer) \n Call New Foo(Of M) \n End Sub \n End Class \n Friend Class Foo(Of M) \n End Class"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateIntoClass()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Outer(Of M) \n Sub Main(i As Integer) \n Call New [|Foo(Of M)|] \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Outer(Of M) \n Sub Main(i As Integer) \n Call New Foo(Of M) \n End Sub \n Private Class Foo(Of M) \n End Class \n End Class"),
index:=2)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateIntoClassFromFullyQualifiedInvocation()
            Test(
NewLines("Class Program \n Sub Test() \n Dim d = New [|Program.Foo|]() \n End Sub \n End Class"),
NewLines("Class Program \n Sub Test() \n Dim d = New Program.Foo() \n End Sub \n Private Class Foo \n Public Sub New() \n End Sub \n End Class \n End Class"))
        End Sub

        <WorkItem(5776, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateIntoNamespaceFromFullyQualifiedInvocation()
            Test(
NewLines("Namespace Foo \n Class Program \n Sub Test() \n Dim d = New [|Foo.Bar|]() \n End Sub \n End Class \n End Namespace"),
NewLines("Namespace Foo \n Class Program \n Sub Test() \n Dim d = New Foo.Bar() \n End Sub \n End Class \n Friend Class Bar \n Public Sub New() \n End Sub \n End Class \n End Namespace"),
index:=1,
parseOptions:=Nothing) ' Namespaces not supported in script
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestInSecondConstraintClause()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program(Of T As {Foo, [|IBar|]}) \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program(Of T As {Foo, IBar}) \n End Class \n Friend Interface IBar \n End Interface"),
index:=1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateIntoNewNamespace() As Task
            Await TestAddDocument(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Sub Main() \n Call New Foo.[|Bar|]() \n End Sub \n End Class"),
NewLines("Namespace Foo \n Friend Class Bar \n Public Sub New() \n End Sub \n End Class \n End Namespace"),
expectedContainers:={"Foo"},
expectedDocumentName:="Bar.vb").ConfigureAwait(True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateIntoGlobalNamespaceNewFile() As Task
            Await TestAddDocument(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim x As New [|Foo|] \n End Sub \n End Module"),
NewLines("Friend Class Foo \n End Class"),
expectedContainers:=Array.Empty(Of String)(),
expectedDocumentName:="Foo.vb").ConfigureAwait(True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateTypeThatImplementsInterface1()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim d As [|IFoo|] = New Foo() \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim d As IFoo = New Foo() \n End Sub \n End Module \n Friend Interface IFoo \n End Interface"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateTypeThatImplementsInterface2()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim d As IFoo = New [|Foo|]() \n End Sub \n End Module \n Friend Interface IFoo \n End Interface"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim d As IFoo = New Foo() \n End Sub \n End Module \n Friend Class Foo \n Implements IFoo \n End Class \n Friend Interface IFoo \n End Interface"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateTypeWithNamedArguments()
            Test(
NewLines("Class Program \n Sub Test() \n Dim x = New [|Bar|](value:=7) \n End Sub \n End Class"),
NewLines("Class Program \n Sub Test() \n Dim x = New Bar(value:=7) \n End Sub \n End Class \n Friend Class Bar \n Private value As Integer \n Public Sub New(value As Integer) \n Me.value = value \n End Sub \n End Class"),
index:=1)
        End Sub

        <WorkItem(539730)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestNotIntoType()
            TestActionCount(
NewLines("Class Program \n Inherits [|Temp|] \n Sub Test() \n End Sub \n End Class"),
count:=3)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateClassFromReturnType()
            Test(
NewLines("Class Foo \n Function F() As [|Bar|] \n End Function \n End Class"),
NewLines("Class Foo \n Function F() As Bar \n End Function \n End Class \n Public Class Bar \n End Class"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateClassWhereKeywordBecomesTypeName()
            Test(
NewLines("Class Foo \n Dim x As New [|[Class]|] \n End Class"),
NewLines("Class Foo \n Dim x As New [Class] \n End Class \n Friend Class [Class] \n End Class"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub NegativeTestGenerateClassFromEscapedType()
            Test(
NewLines("Class Foo \n Dim x as New [|[Bar]|] \n End Class"),
NewLines("Class Foo \n Dim x as New [Bar] \n End Class \n Friend Class Bar \n End Class"),
index:=1)
        End Sub

        <WorkItem(539716)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateTypeIntoContainingNamespace()
            Test(
NewLines("Namespace NS \n Class Foo \n Dim x As New NS.[|Bar|] \n End Class \n End Namespace"),
NewLines("Namespace NS \n Class Foo \n Dim x As New NS.Bar \n End Class \n Friend Class Bar \n End Class \n End Namespace"),
index:=1,
parseOptions:=Nothing) ' Namespaces not supported in script
        End Sub

        <WorkItem(539736)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateTypeIntoContainingModule()
            Test(
NewLines("Module M \n Dim x As [|C|] \n End Module"),
NewLines("Module M \n Dim x As C \n Private Class C \n End Class \n End Module"),
index:=2)
        End Sub

        <WorkItem(539737)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateInterfaceInImplementsStatement()
            Test(
NewLines("Class C \n Implements [|D|] \n End Class"),
NewLines("Class C \n Implements D \n End Class \n Friend Interface D \n End Interface"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAbsenceOfGenerateIntoInvokingTypeForConstraintList()
            TestActionCount(
NewLines("Class EmployeeList(Of T As [|Employee|]) \n End Class"),
count:=3,
parseOptions:=TestOptions.Regular)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestMissingOnImportsDirective()
            TestMissing(
NewLines("Imports [|System|]"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestNoContainersInNewType() As Task
            Await TestAddDocument(
NewLines("Class Base \n Sub Main \n Dim p = New [|Derived|]() \n End Sub \n End Class"),
NewLines("Friend Class Derived \n Public Sub New() \n End Sub \n End Class"),
expectedContainers:=Array.Empty(Of String)(),
expectedDocumentName:="Derived.vb").ConfigureAwait(True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestNotOfferedInsideBinaryExpressions()
            TestMissing(
NewLines("Class Base \n Sub Main \n Dim a = 1 + [|Foo|] \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestNotOfferedIfLeftSideOfDotIsNotAName()
            TestMissing(
NewLines("Module Program \n Sub Main(args As String()) \n Call 1.[|T|] \n End Sub \n End Module"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestNotOfferedIfLeftFromDotIsNotAName()
            TestMissing(
NewLines("Class C1 \n Sub Foo \n Me.[|Foo|] = 3 \n End Sub \n End Class"))
        End Sub

        <WorkItem(539786)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestMissingOnAssignedVariable()
            TestMissing(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n [|B|] = 10 \n End Sub \n End Module"))
        End Sub

        <WorkItem(539757)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestArrayInference1()
            Test(
NewLines("Class Base \n Sub Main \n Dim p() As Base = New [|Derived|](10) {} \n End Sub \n End Class"),
NewLines("Class Base \n Sub Main \n Dim p() As Base = New Derived(10) {} \n End Sub \n End Class \n Friend Class Derived \n Inherits Base \n End Class"),
index:=1)
        End Sub

        <WorkItem(539757)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestArrayInference2()
            Test(
NewLines("Class Base \n Sub Main \n Dim p As Base() = New [|Derived|](10) {} \n End Sub \n End Class"),
NewLines("Class Base \n Sub Main \n Dim p As Base() = New Derived(10) {} \n End Sub \n End Class \n Friend Class Derived \n Inherits Base \n End Class"),
index:=1)
        End Sub

        <WorkItem(539757)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestArrayInference3()
            Test(
NewLines("Class Base \n Sub Main \n Dim p As Base = New [|Derived|](10) {} \n End Sub \n End Class"),
NewLines("Class Base \n Sub Main \n Dim p As Base = New Derived(10) {} \n End Sub \n End Class \n Friend Class Derived \n End Class"),
index:=1)
        End Sub

        <WorkItem(539749)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestMatchWithDifferentArity()
            Test(
NewLines("Class Program \n Private Sub Main() \n Dim f As [|Foo(Of Integer)|] \n End Sub \n End Class \n Class Foo \n End Class"),
NewLines("Class Program \n Private Sub Main() \n Dim f As Foo(Of Integer) \n End Sub \n End Class \n Friend Class Foo(Of T) \n End Class \n Class Foo \n End Class"),
index:=1)
        End Sub

        <WorkItem(540504)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestNoUnavailableTypeParameters1()
            Test(
NewLines("Class C(Of T1, T2) \n Sub M(x As T1, y As T2) \n Dim a As Test = New [|Test|](x, y) \n End Sub \n End Class"),
NewLines("Class C(Of T1, T2) \n Sub M(x As T1, y As T2) \n Dim a As Test = New Test(x, y) \n End Sub \n End Class \n Friend Class Test \n Private x As Object \n Private y As Object \n Public Sub New(x As Object, y As Object) \n Me.x = x \n Me.y = y \n End Sub \n End Class"),
index:=1)
        End Sub

        <WorkItem(540534)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestMultipleTypeParamsInConstructor1()
            Test(
NewLines("Class C(Of T1, T2) \n Sub M(x As T1, y As T2) \n Dim a As Test(Of T1, T2) = New [|Test(Of T1, T2)|](x, y) \n End Sub \n End Class"),
NewLines("Class C(Of T1, T2) \n Sub M(x As T1, y As T2) \n Dim a As Test(Of T1, T2) = New Test(Of T1, T2)(x, y) \n End Sub \n End Class \n Friend Class Test(Of T1, T2) \n Private x As T1 \n Private y As T2 \n Public Sub New(x As T1, y As T2) \n Me.x = x \n Me.y = y \n End Sub \n End Class"),
index:=1)
        End Sub

        <WorkItem(540644)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateWithVoidArg()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As C = New [|C|](M()) \n End Sub \n Sub M() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As C = New C(M()) \n End Sub \n Sub M() \n End Sub \n End Module \n Friend Class C \n Private v As Object \n Public Sub New(v As Object) \n Me.v = v \n End Sub \n End Class"),
index:=1)
        End Sub

        <WorkItem(539735)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestInAsClause()
            Test(
NewLines("Class D \n Sub M() \n Dim x As New [|C|](4) \n End Sub \n End Class"),
NewLines("Class D \n Sub M() \n Dim x As New C(4) \n End Sub \n End Class \n Friend Class C \n Private v As Integer \n Public Sub New(v As Integer) \n Me.v = v \n End Sub \n End Class"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestNotOnConstructorToActualType()
            TestMissing(
NewLines("Class C \n Sub Test() \n Dim x As Integer = 1 \n Dim obj As New [|C|](x) \n End Sub \n End Class"))
        End Sub

        <WorkItem(540986)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateAttribute1()
            Test(
NewLines("<[|AttClass|]()> \n Class C \n End Class"),
NewLines("Imports System \n <AttClass()> \n Class C \n End Class \n Friend Class AttClassAttribute \n Inherits Attribute \n End Class"),
index:=1)
        End Sub

        <WorkItem(540986)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateAttribute2()
            Test(
NewLines("Imports System \n <[|AttClass|]()> \n Class C \n End Class"),
NewLines("Imports System \n <AttClass()> \n Class C \n End Class \n Friend Class AttClassAttribute \n Inherits Attribute \n End Class"),
index:=1)
        End Sub

        <WorkItem(541607)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestNotOnDictionaryAccess()
            TestMissing(
NewLines("Imports System \n Imports System.Collections \n Imports System.Collections.Generic \n Public Class A \n Public Sub Foo() \n Dim Table As Hashtable = New Hashtable() \n Table![|Orange|] = ""A fruit"" \n Table(""Broccoli"") = ""A vegetable"" \n Console.WriteLine(Table!Orange) \n End Sub \n End Class"))
        End Sub

        <WorkItem(542392)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAccessibilityConstraint1()
            Test(
NewLines("Imports System.Runtime.CompilerServices \n Module StringExtensions \n <Extension()> \n Public Sub Print(ByVal aString As String, x As [|C|]) \n Console.WriteLine(aString) \n End Sub \n End Module"),
NewLines("Imports System.Runtime.CompilerServices \n Module StringExtensions \n <Extension()> \n Public Sub Print(ByVal aString As String, x As C) \n Console.WriteLine(aString) \n End Sub \n Public Class C \n End Class \n End Module"),
index:=2)
        End Sub

        <WorkItem(542836)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestNewLineAfterNestedType()
            Test(
<Text>Class A
    Sub Main()
        Dim x As A()() = New [|HERE|]()
    End Sub
End Class</Text>.NormalizedValue,
<Text>Class A
    Sub Main()
        Dim x As A()() = New HERE()
    End Sub

    Private Class HERE
        Public Sub New()
        End Sub
    End Class
End Class</Text>.NormalizedValue,
index:=2,
compareTokens:=False)
        End Sub

        <WorkItem(543290)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestNestedType()
            Test(
NewLines("Option Explicit Off \n Module Program \n Sub Main(args As String()) \n Dim i = 2 \n Dim r As New i.[|Extension|] \n End Sub \n Public Class i \n End Class \n End Module"),
NewLines("Option Explicit Off \n Module Program \n Sub Main(args As String()) \n Dim i = 2 \n Dim r As New i.Extension \n End Sub \n Public Class i \n Friend Class Extension \n End Class \n End Class \n End Module"))
        End Sub

        <WorkItem(543397)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestNewModule()
            TestMissing(
NewLines("Module Program \n Sub Main \n Dim f As New [|Program|] \n End Sub \n End Module"))
        End Sub

        <WorkItem(545363)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestInHiddenNamespace1()
            TestExactActionSetOffered(
<text>
#ExternalSource ("Default.aspx", 1)
Class Program
    Sub Main(args As String())
        Dim f As New [|Foo|]()
    End Sub
End Class
#End ExternalSource
</text>.NormalizedValue,
{String.Format(FeaturesResources.Generate_0_1_in_new_file, "class", "Foo", FeaturesResources.GlobalNamespace), String.Format(FeaturesResources.Generate_nested_0_1, "class", "Foo", "Program"), FeaturesResources.GenerateNewType})
        End Sub

        <WorkItem(545363)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestInHiddenNamespace2()
            TestExactActionSetOffered(
<text>
#ExternalSource ("Default.aspx", 1)
Class Program
    Sub Main(args As String())
        Dim f As New [|Foo|]()
    End Sub
End Class

Class Bar
End Class
#End ExternalSource
</text>.NormalizedValue,
{String.Format(FeaturesResources.Generate_0_1_in_new_file, "class", "Foo", FeaturesResources.GlobalNamespace),
String.Format(FeaturesResources.Generate_0_1, "class", "Foo", FeaturesResources.GlobalNamespace),
String.Format(FeaturesResources.Generate_nested_0_1, "class", "Foo"), FeaturesResources.GenerateNewType})
        End Sub

        <WorkItem(545363)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestInHiddenNamespace3()
            Test(
<text>
#ExternalSource ("Default.aspx", 1)
Class Program
    Sub Main(args As String())
        Dim f As New [|Foo|]()
    End Sub
End Class

Class Bar
End Class
#End ExternalSource
</text>.NormalizedValue,
<text>
#ExternalSource ("Default.aspx", 1)
Class Program
    Sub Main(args As String())
        Dim f As New Foo()
    End Sub
End Class

Friend Class Foo
    Public Sub New()
    End Sub
End Class

Class Bar
End Class
#End ExternalSource
</text>.NormalizedValue,
index:=1)
        End Sub

        <WorkItem(546852)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAnonymousMethodArgument()
            Test(
NewLines("Module Program \n Sub Main() \n Dim c = New [|C|](Function() x) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n Dim c = New C(Function() x) \n End Sub \n End Module \n Friend Class C \n Private p As Func(Of Object) \n Public Sub New(p As Func(Of Object)) \n Me.p = p \n End Sub \n End Class"),
index:=1)
        End Sub

        <WorkItem(546851)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestOmittedArguments()
            Test(
NewLines("Imports System \n Module Program \n Sub Main() \n Dim x = New [|C|](,) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n Dim x = New C(,) \n End Sub \n End Module \n Friend Class C \n Private p1 As Object \n Private p2 As Object \n Public Sub New(p1 As Object, p2 As Object) \n Me.p1 = p1 \n Me.p2 = p2 \n End Sub \n End Class"),
index:=1)
        End Sub

        <WorkItem(1003618)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeThatBindsToNamespace()
            Test(
NewLines("Imports System \n [|<System>|] \n Module Program \n Sub Main() \n End Sub \n End Module"),
NewLines("Imports System \n <System> \n Module Program \n Sub Main() \n End Sub \n End Module \n Friend Class SystemAttribute \n Inherits Attribute \n End Class"),
index:=1)
        End Sub

        <WorkItem(821277)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestTooFewTypeArgument()
            Test(
<text>
Class Program
    Sub Main(args As String())
        Dim f As [|AA|]
    End Sub
End Class

Class AA(Of T)
End Class
</text>.NormalizedValue,
<text>
Class Program
    Sub Main(args As String())
        Dim f As AA
    End Sub
End Class

Friend Class AA
End Class

Class AA(Of T)
End Class
</text>.NormalizedValue,
index:=1,
compareTokens:=False)
        End Sub

        <WorkItem(821277)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestTooMoreTypeArgument()
            Test(
<text>
Class Program
    Sub Main(args As String())
        Dim f As [|AA(Of Integer, Integer)|]
    End Sub
End Class

Class AA(Of T)
End Class
</text>.NormalizedValue,
<text>
Class Program
    Sub Main(args As String())
        Dim f As AA(Of Integer, Integer)
    End Sub
End Class

Friend Class AA(Of T1, T2)
End Class

Class AA(Of T)
End Class
</text>.NormalizedValue,
index:=1,
compareTokens:=False)
        End Sub

        <WorkItem(942568)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub GenerateTypeWithPreferIntrinsicPredefinedKeywordFalse()
            Test(
<text>
Class Program
    Sub M(args As Integer)
        Dim f = new [|T(args)|]
    End Sub
End Class
</text>.NormalizedValue,
<text>
Class Program
    Sub M(args As Integer)
        Dim f = new T(args)
    End Sub
End Class

Friend Class T
    Private args As System.Int32

    Public Sub New(args As System.Int32)
        Me.args = args
    End Sub
End Class
</text>.NormalizedValue,
index:=1,
compareTokens:=False,
options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic), False}})
        End Sub

        <WorkItem(869506)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateTypeOutsideCurrentProject()
            Dim initial = <Workspace>
                              <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                  <ProjectReference>Assembly2</ProjectReference>
                                  <Document FilePath="Test1.vb">
Class Program
    Sub Main()
        Dim f As [|A.B.C$$|].D
    End Sub
End Class

Namespace A
End Namespace</Document>
                              </Project>
                              <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                  <Document FilePath="Test2.cs">
Namespace A
    Public Class B
    End Class
End Namespace</Document>
                              </Project>
                          </Workspace>.ToString()

            Dim expected = <Text>
Namespace A
    Public Class B
        Public Class C
        End Class
    End Class
End Namespace</Text>.NormalizedValue

            Test(initial, expected, compareTokens:=False)
        End Sub

        <WorkItem(940003)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestWithProperties1()
            Test(
NewLines("Imports System \n Module Program \n Sub Main() \n  Dim c As New [|Customer|](x:=1, y:=""Hello"") With {.Name = ""John"", .Age = Date.Today} \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n  Dim c As New [|Customer|](x:=1, y:=""Hello"") With {.Name = ""John"", .Age = Date.Today} \n End Sub \n End Module \n Friend Class Customer \n Private x As Integer \n Private y As String \n Public Sub New(x As Integer, y As String) \n Me.x = x \n Me.y = y \n End Sub \n Public Property Age As Date \n Public Property Name As String \n End Class"),
index:=1)
        End Sub

        <WorkItem(940003)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestWithProperties2()
            Test(
NewLines("Imports System \n Module Program \n Sub Main() \n  Dim c As New [|Customer|](x:=1, y:=""Hello"") With {.Name = Nothing, .Age = Date.Today} \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n  Dim c As New [|Customer|](x:=1, y:=""Hello"") With {.Name = Nothing, .Age = Date.Today} \n End Sub \n End Module \n Friend Class Customer \n Private x As Integer \n Private y As String \n Public Sub New(x As Integer, y As String) \n Me.x = x \n Me.y = y \n End Sub \n Public Property Age As Date \n Public Property Name As Object \n End Class"),
index:=1)
        End Sub

        <WorkItem(940003)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestWithProperties3()
            Test(
NewLines("Imports System \n Module Program \n Sub Main() \n  Dim c As New [|Customer|](x:=1, y:=""Hello"") With {.Name = Foo, .Age = Date.Today} \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n  Dim c As New [|Customer|](x:=1, y:=""Hello"") With {.Name = Foo, .Age = Date.Today} \n End Sub \n End Module \n Friend Class Customer \n Private x As Integer \n Private y As String \n Public Sub New(x As Integer, y As String) \n Me.x = x \n Me.y = y \n End Sub \n Public Property Age As Date \n Public Property Name As Object \n End Class"),
index:=1)
        End Sub

        <WorkItem(1082031)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestWithProperties4()
            Test(
NewLines("Imports System \n Module Program \n Sub Main() \n  Dim c As New [|Customer|] With {.Name = ""John"", .Age = Date.Today} \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n  Dim c As New [|Customer|] With {.Name = ""John"", .Age = Date.Today} \n End Sub \n End Module \n Friend Class Customer \n Public Property Age As Date \n Public Property Name As String \n End Class"),
index:=1)
        End Sub

        <WorkItem(1032176)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestWithNameOf()
            Test(
NewLines("Imports System \n Module Program \n Sub Main() \n  Dim x = nameof([|Z|]) \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main() \n  Dim x = nameof([|Z|]) \n End Sub \n End Module \n Friend Class Z \n End Class"),
index:=1)
        End Sub

        <WorkItem(1032176)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestWithNameOf2()
            Test(
NewLines("Imports System \n Class Program \n Sub Main() \n  Dim x = nameof([|Z|]) \n End Sub \n End Class"),
NewLines("Imports System \n Class Program \n Sub Main() \n  Dim x = nameof([|Z|]) \n End Sub \n Private Class Z \n End Class \n End Class "),
index:=2)
        End Sub

        <WorkItem(1032176)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestWithNameOf3()
            Test(
NewLines("Imports System \n Class Program \n Sub Main() \n  Dim x = nameof([|Program.Z|]) \n End Sub \n End Class"),
NewLines("Imports System \n Class Program \n Sub Main() \n  Dim x = nameof([|Program.Z|]) \n End Sub \n Private Class Z \n End Class \n End Class "),
index:=0)
        End Sub

        <WorkItem(1065647)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAccessibilityForNestedType()
            Test(
NewLines("Public Interface I \n  Sub Foo(a As [|X.Y.Z|]) \n End Interface \n Public Class X \n End Class"),
NewLines("Public Interface I \n  Sub Foo(a As X.Y.Z) \n End Interface \n Public Class X \n Public Class Y \n End Class \n End Class"),
index:=0)
        End Sub

        <WorkItem(1130905)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateTypeInImports()
            Test(
NewLines("Imports [|Fizz|]"),
NewLines("Friend Class Fizz\nEnd Class\n"))
        End Sub

        <WorkItem(1130905)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestGenerateTypeInImports2()
            Test(
NewLines("Imports [|Fizz|]"),
NewLines("Imports Fizz \n Friend Class Fizz \n End Class"),
index:=1)
        End Sub

        <WorkItem(1107929)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAccessibilityForPublicFields()
            Test(
NewLines("Public Class A \n Public B As New [|B|]() \n End Class"),
NewLines("Public Class B \n Public Sub New() \n End Sub \n End Class"),
index:=0)
        End Sub

        <WorkItem(1107929)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAccessibilityForPublicFields2()
            Test(
NewLines("Public Class A \n Public B As New [|B|]() \n End Class"),
NewLines("Public Class A \n Public B As New B() \n End Class \n\n Public Class B \n Public Sub New() \n End Sub \n End Class"),
index:=1)
        End Sub

        <WorkItem(1107929)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAccessibilityForPublicFields3()
            Test(
NewLines("Public Class A \n Public B As New [|B|]() \n End Class"),
NewLines("Public Class A \n Public B As New B() \n Public Class B \n Public Sub New() \n End Sub \n End Class \n End Class"),
index:=2)
        End Sub

        <WorkItem(1107929)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAccessibilityForPublicFields4()
            Test(
NewLines("Public Class A \n Public B As New [|B|] \n End Class"),
NewLines("Public Class B \n End Class"),
index:=0)
        End Sub

        <WorkItem(1107929)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAccessibilityForPublicFields5()
            Test(
NewLines("Public Class A \n Public B As New [|B|] \n End Class"),
NewLines("Public Class A \n Public B As New B \n End Class \n\n Public Class B \n End Class"),
index:=1)
        End Sub

        <WorkItem(1107929)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAccessibilityForPublicFields6()
            Test(
NewLines("Public Class A \n Public B As New [|B|] \n End Class"),
NewLines("Public Class A \n Public B As New B \n Public Class B \n End Class \n End Class"),
index:=2)
        End Sub

        <WorkItem(1107929)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAccessibilityForPublicFields7()
            Test(
NewLines("Public Class A \n Public B As New [|B(Of Integer)|] \n End Class"),
NewLines("Public Class B(Of T) \n End Class"),
index:=0)
        End Sub

        <WorkItem(1107929)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAccessibilityForPublicFields8()
            Test(
NewLines("Public Class A \n Public B As New [|B(Of Integer)|] \n End Class"),
NewLines("Public Class A \n Public B As New B(Of Integer) \n End Class \n\n Public Class B(Of T) \n End Class"),
index:=1)
        End Sub

        <WorkItem(1107929)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Sub TestAccessibilityForPublicFields9()
            Test(
NewLines("Public Class A \n Public B As New [|B(Of Integer)|] \n End Class"),
NewLines("Public Class A \n Public B As New B(Of Integer) \n Public Class B(Of T) \n End Class \n End Class"),
index:=2)
        End Sub

        Public Class AddImportTestsWithAddImportDiagnosticProvider
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(),
                    New GenerateTypeCodeFixProvider())
            End Function

            Protected Overrides Function MassageActions(actions As IList(Of CodeAction)) As IList(Of CodeAction)
                Return FlattenActions(actions)
            End Function

            <WorkItem(829970)>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Sub TestUnknownIdentifierInAttributeSyntaxWithoutTarget()
                Test(
NewLines("Module Program \n <[|Extension|]> \n End Module"),
NewLines("Imports System \n Module Program \n <Extension> \n End Module \n Friend Class ExtensionAttribute \n Inherits Attribute \n End Class"),
index:=1)
            End Sub
        End Class
    End Class
End Namespace
