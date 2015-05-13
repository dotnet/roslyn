' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.FullyQualify
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.FullyQualify
    Public Class FullyQualifyTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New VisualBasicFullyQualifyCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestParameterType()
            Test(
NewLines("Module Program \n Sub Main(args As String(), f As [|FileMode|]) \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String(), f As System.IO.FileMode) \n End Sub \n End Module"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestSimpleQualifyFromSameFile()
            Test(
NewLines("Class Class1 \n Dim v As [|SomeClass1|] \n End Class \n Namespace SomeNamespace \n Public Class SomeClass1 \n End Class \n End Namespace"),
NewLines("Class Class1 \n Dim v As SomeNamespace.SomeClass1 \n End Class \n Namespace SomeNamespace \n Public Class SomeClass1 \n End Class \n End Namespace"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestSimpleQualifyFromReference()
            Test(
NewLines("Class Class1 \n Dim v As [|Thread|] \n End Class"),
NewLines("Class Class1 \n Dim v As System.Threading.Thread \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestGenericClassDefinitionAsClause()
            Test(
NewLines("Namespace SomeNamespace \n Class Base \n End Class \n End Namespace \n Class SomeClass(Of x As [|Base|]) \n End Class"),
NewLines("Namespace SomeNamespace \n Class Base \n End Class \n End Namespace \n Class SomeClass(Of x As SomeNamespace.Base) \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestGenericClassInstantiationOfClause()
            Test(
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class GenericClass(Of T) \n End Class \n Class Foo \n Sub Method1() \n Dim q As GenericClass(Of [|SomeClass|]) \n End Sub \n End Class"),
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class GenericClass(Of T) \n End Class \n Class Foo \n Sub Method1() \n Dim q As GenericClass(Of SomeNamespace.SomeClass) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestGenericMethodDefinitionAsClause()
            Test(
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T As [|SomeClass|]) \n End Sub \n End Class"),
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T As SomeNamespace.SomeClass) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestGenericMethodInvocationOfClause()
            Test(
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T) \n End Sub \n Sub Method2() \n Method1(Of [|SomeClass|]) \n End Sub \n End Class"),
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T) \n End Sub \n Sub Method2() \n Method1(Of SomeNamespace.SomeClass) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestAttributeApplication()
            Test(
NewLines("<[|Something|]()> \n Class Foo \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
NewLines("<SomeNamespace.Something()> \n Class Foo \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestMultipleAttributeApplicationBelow()
            Test(
NewLines("Imports System \n <Existing()> \n <[|Something|]()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits Attribute \n End Class \n End Namespace"),
NewLines("Imports System \n <Existing()> \n <SomeNamespace.Something()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits Attribute \n End Class \n End Namespace"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestMultipleAttributeApplicationAbove()
            Test(
NewLines("<[|Something|]()> \n <Existing()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
NewLines("<SomeNamespace.Something()> \n <Existing()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestQualifierIsEscapedWhenNamespaceMatchesKeyword()
            Test(
NewLines("Class SomeClass \n Dim x As [|Something|] \n End Class \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace"),
NewLines("Class SomeClass \n Dim x As [Namespace].Something \n End Class \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace"))
        End Sub

        <WorkItem(540559)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestQualifierIsNOTEscapedWhenNamespaceMatchesKeywordButIsNested()
            Test(
NewLines("Class SomeClass \n Dim x As [|Something|] \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"),
NewLines("Class SomeClass \n Dim x As Outer.Namespace.Something \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WorkItem(540560)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestFullyQualifyInImportsStatement()
            Test(
NewLines("Imports [|InnerNamespace|] \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"),
NewLines("Imports SomeNamespace.InnerNamespace \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestFullyQualifyNotSuggestedForGenericTypeParametersOfClause()
            TestMissing(
NewLines("Class SomeClass \n Sub Foo(Of [|SomeClass|])(x As SomeClass) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestFullyQualifyNotSuggestedForGenericTypeParametersAsClause()
            TestMissing(
NewLines("Class SomeClass \n Sub Foo(Of SomeClass)(x As [|SomeClass|]) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"))
        End Sub

        <WorkItem(540673)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestCaseSensitivityForNestedNamespace()
            Test(
NewLines("Class Foo \n Sub bar() \n Dim q As [|innernamespace|].someClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"),
NewLines("Class Foo \n Sub bar() \n Dim q As SomeNamespace.InnerNamespace.someClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WorkItem(540543)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestCaseSensitivity1()
            Test(
NewLines("Class Foo \n Dim x As [|someclass|] \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"),
NewLines("Class Foo \n Dim x As SomeNamespace.SomeClass \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestTypeFromMultipleNamespaces1()
            Test(
NewLines("Class Foo \n Function F() As [|IDictionary|] \n End Function \n End Class"),
NewLines("Class Foo \n Function F() As System.Collections.IDictionary \n End Function \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestTypeFromMultipleNamespaces2()
            Test(
NewLines("Class Foo \n Function F() As [|IDictionary|] \n End Function \n End Class"),
NewLines("Class Foo \n Function F() As System.Collections.Generic.IDictionary \n End Function \n End Class"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestGenericWithNoArgs()
            Test(
NewLines("Class Foo \n Function F() As [|List|] \n End Function \n End Class"),
NewLines("Class Foo \n Function F() As System.Collections.Generic.List \n End Function \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestGenericWithCorrectArgs()
            Test(
NewLines("Class Foo \n Function F() As [|List(Of Integer)|] \n End Function \n End Class"),
NewLines("Class Foo \n Function F() As System.Collections.Generic.List(Of Integer) \n End Function \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestGenericWithWrongArgs()
            TestMissing(
NewLines("Class Foo \n Function F() As [|List(Of Integer, String)|] \n End Function \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestGenericInLocalDeclaration()
            Test(
NewLines("Class Foo \n Sub Test() \n Dim x As New [|List(Of Integer)|] \n End Sub \n End Class"),
NewLines("Class Foo \n Sub Test() \n Dim x As New System.Collections.Generic.List(Of Integer) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestGenericItemType()
            Test(
NewLines("Class Foo \n Sub Test() \n Dim x As New List(Of [|Int32|]) \n End Sub \n End Class"),
NewLines("Class Foo \n Sub Test() \n Dim x As New List(Of System.Int32) \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestGenerateInNamespace()
            Test(
NewLines("Imports System \n Namespace NS \n Class Foo \n Sub Test() \n Dim x As New [|List(Of Integer)|] \n End Sub \n End Class \n End Namespace"),
NewLines("Imports System \n Namespace NS \n Class Foo \n Sub Test() \n Dim x As New Collections.Generic.List(Of Integer) \n End Sub \n End Class \n End Namespace"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestMinimalQualify()
            Test(
NewLines("Imports System \n Module Program \n Dim q As [|List(Of Integer)|] \n End Module"),
NewLines("Imports System \n Module Program \n Dim q As Collections.Generic.List(Of Integer) \n End Module"))
        End Sub

        <WorkItem(540559)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestEscaping1()
            Test(
NewLines("Class SomeClass \n Dim x As [|Something|] \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"),
NewLines("Class SomeClass \n Dim x As Outer.Namespace.Something \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WorkItem(540559)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestEscaping2()
            Test(
NewLines("Class SomeClass \n Dim x As [|Something|] \n End Class \n Namespace [Namespace] \n Namespace Inner \n Class Something \n End Class \n End Namespace \n End Namespace"),
NewLines("Class SomeClass \n Dim x As [Namespace].Inner.Something \n End Class \n Namespace [Namespace] \n Namespace Inner \n Class Something \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WorkItem(540559)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestEscaping3()
            Test(
NewLines("Class SomeClass \n Dim x As [|[Namespace]|] \n End Class \n Namespace Outer \n Namespace Inner \n Class [Namespace] \n End Class \n End Namespace \n End Namespace"),
NewLines("Class SomeClass \n Dim x As Outer.Inner.[Namespace] \n End Class \n Namespace Outer \n Namespace Inner \n Class [Namespace] \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WorkItem(540560)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestInImport()
            Test(
NewLines("Imports [|InnerNamespace|] \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"),
NewLines("Imports SomeNamespace.InnerNamespace \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WorkItem(540673)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestCaseInsensitivity()
            Test(
NewLines("Class FOo \n Sub bar() \n Dim q As [|innernamespace|].someClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"),
NewLines("Class FOo \n Sub bar() \n Dim q As SomeNamespace.InnerNamespace.someClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WorkItem(540706)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestStandaloneMethod()
            Test(
NewLines("'Class [Class] \n Private Sub Method(i As Integer) \n [|[Enum]|] = 5 \n End Sub \n End Class"),
NewLines("'Class [Class] \n Private Sub Method(i As Integer) \n System.[Enum] = 5 \n End Sub \n End Class"))
        End Sub

        <WorkItem(540736)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestMissingOnBoundFieldType()
            TestMissing(
NewLines("Imports System.Collections.Generic \n Class A \n Private field As [|List(Of C)|] \n Sub Main() \n Dim local As List(Of C) \n End Sub \n End Class"))
        End Sub

        <WorkItem(540736)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestMissingOnBoundLocalType()
            TestMissing(
NewLines("Imports System.Collections.Generic \n Class A \n Private field As [|List(Of C)|] \n Sub Main() \n Dim local As List(Of C) \n End Sub \n End Class"))
        End Sub

        <WorkItem(540745)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestCaseSensitivity2()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As [|foo|] \n End Sub \n End Module \n Namespace OUTER \n Namespace INNER \n Friend Class FOO \n End Class \n End Namespace \n End Namespace"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As OUTER.INNER.FOO \n End Sub \n End Module \n Namespace OUTER \n Namespace INNER \n Friend Class FOO \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WorkItem(821292)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestCaseSensitivity3()
            Test(
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Dim x As [|stream|] \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Dim x As IO.Stream \n End Sub \n End Module"))
        End Sub

        <WorkItem(545993)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestNotOnNamedArgument()
            TestMissing(
NewLines("Module Program \n <MethodImpl([|methodImplOptions|]:=MethodImplOptions.ForwardRef) \n Sub Main(args As String()) \n End Sub \n End Module"))
        End Sub

        <WorkItem(546107)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestDoNotQualifyNestedTypeOfGenericType()
            TestMissing(
NewLines("Imports System \n Imports System.Collections.Generic \n  \n Class Program \n Shared Sub Main() \n CType(GetEnumerator(), IDisposable).Dispose() \n End Sub \n  \n Shared Function GetEnumerator() As [|Enumerator|] \n Return Nothing \n End Function \n End Class"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub TestFormattingInFullyQualify()
            Test(
<Text>Module Program
    &lt;[|Obsolete|]&gt;
    Sub Main(args As String())
    End Sub
End Module</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Module Program
    &lt;System.Obsolete&gt;
    Sub Main(args As String())
    End Sub
End Module</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Sub

        <WorkItem(775448)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub ShouldTriggerOnBC32045()
            ' BC32045: 'A' has no type parameters and so cannot have type arguments.
            Test(
<Text>Imports System.Collections

Module Program
    Sub Main(args As String())
        Dim x As [|IEnumerable(Of Integer)|]
    End Sub
End Module</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System.Collections

Module Program
    Sub Main(args As String())
        Dim x As Generic.IEnumerable(Of Integer)
    End Sub
End Module</Text>.Value.Replace(vbLf, vbCrLf),
index:=0,
compareTokens:=False)
        End Sub

        <WorkItem(947579)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Sub AmbiguousTypeFix()
            Test(
<Text>Imports N1
Imports N2

Module Program
    Sub M1()
        [|Dim a As A|]
    End Sub
End Module

Namespace N1
    Class A
    End Class
End Namespace

Namespace N2
    Class A
    End Class
End Namespace</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports N1
Imports N2

Module Program
    Sub M1()
        Dim a As N1.A
    End Sub
End Module

Namespace N1
    Class A
    End Class
End Namespace

Namespace N2
    Class A
    End Class
End Namespace</Text>.Value.Replace(vbLf, vbCrLf),
index:=0,
compareTokens:=False)
        End Sub

        Public Class AddImportTestsWithAddImportDiagnosticProvider
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(),
                    New VisualBasicFullyQualifyCodeFixProvider())
            End Function

            <WorkItem(829970)>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Sub TestUnknownIdentifierInAttributeSyntaxWithoutTarget()
                Test(
    NewLines("Module Program \n <[|Extension|]> \n End Module"),
    NewLines("Module Program \n <System.Runtime.CompilerServices.Extension> \n End Module"))
            End Sub
        End Class
    End Class
End Namespace
