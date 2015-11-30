' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestParameterType() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String(), f As [|FileMode|]) \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String(), f As System.IO.FileMode) \n End Sub \n End Module"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestSimpleQualifyFromSameFile() As Task
            Await TestAsync(
NewLines("Class Class1 \n Dim v As [|SomeClass1|] \n End Class \n Namespace SomeNamespace \n Public Class SomeClass1 \n End Class \n End Namespace"),
NewLines("Class Class1 \n Dim v As SomeNamespace.SomeClass1 \n End Class \n Namespace SomeNamespace \n Public Class SomeClass1 \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestSimpleQualifyFromReference() As Task
            Await TestAsync(
NewLines("Class Class1 \n Dim v As [|Thread|] \n End Class"),
NewLines("Class Class1 \n Dim v As System.Threading.Thread \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericClassDefinitionAsClause() As Task
            Await TestAsync(
NewLines("Namespace SomeNamespace \n Class Base \n End Class \n End Namespace \n Class SomeClass(Of x As [|Base|]) \n End Class"),
NewLines("Namespace SomeNamespace \n Class Base \n End Class \n End Namespace \n Class SomeClass(Of x As SomeNamespace.Base) \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericClassInstantiationOfClause() As Task
            Await TestAsync(
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class GenericClass(Of T) \n End Class \n Class Foo \n Sub Method1() \n Dim q As GenericClass(Of [|SomeClass|]) \n End Sub \n End Class"),
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class GenericClass(Of T) \n End Class \n Class Foo \n Sub Method1() \n Dim q As GenericClass(Of SomeNamespace.SomeClass) \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericMethodDefinitionAsClause() As Task
            Await TestAsync(
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T As [|SomeClass|]) \n End Sub \n End Class"),
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T As SomeNamespace.SomeClass) \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericMethodInvocationOfClause() As Task
            Await TestAsync(
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T) \n End Sub \n Sub Method2() \n Method1(Of [|SomeClass|]) \n End Sub \n End Class"),
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T) \n End Sub \n Sub Method2() \n Method1(Of SomeNamespace.SomeClass) \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestAttributeApplication() As Task
            Await TestAsync(
NewLines("<[|Something|]()> \n Class Foo \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
NewLines("<SomeNamespace.Something()> \n Class Foo \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestMultipleAttributeApplicationBelow() As Task
            Await TestAsync(
NewLines("Imports System \n <Existing()> \n <[|Something|]()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits Attribute \n End Class \n End Namespace"),
NewLines("Imports System \n <Existing()> \n <SomeNamespace.Something()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits Attribute \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestMultipleAttributeApplicationAbove() As Task
            Await TestAsync(
NewLines("<[|Something|]()> \n <Existing()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
NewLines("<SomeNamespace.Something()> \n <Existing()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestQualifierIsEscapedWhenNamespaceMatchesKeyword() As Task
            Await TestAsync(
NewLines("Class SomeClass \n Dim x As [|Something|] \n End Class \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace"),
NewLines("Class SomeClass \n Dim x As [Namespace].Something \n End Class \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace"))
        End Function

        <WorkItem(540559)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestQualifierIsNOTEscapedWhenNamespaceMatchesKeywordButIsNested() As Task
            Await TestAsync(
NewLines("Class SomeClass \n Dim x As [|Something|] \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"),
NewLines("Class SomeClass \n Dim x As Outer.Namespace.Something \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WorkItem(540560)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestFullyQualifyInImportsStatement() As Task
            Await TestAsync(
NewLines("Imports [|InnerNamespace|] \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"),
NewLines("Imports SomeNamespace.InnerNamespace \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestFullyQualifyNotSuggestedForGenericTypeParametersOfClause() As Task
            Await TestMissingAsync(
NewLines("Class SomeClass \n Sub Foo(Of [|SomeClass|])(x As SomeClass) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestFullyQualifyNotSuggestedForGenericTypeParametersAsClause() As Task
            Await TestMissingAsync(
NewLines("Class SomeClass \n Sub Foo(Of SomeClass)(x As [|SomeClass|]) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"))
        End Function

        <WorkItem(540673)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestCaseSensitivityForNestedNamespace() As Task
            Await TestAsync(
NewLines("Class Foo \n Sub bar() \n Dim q As [|innernamespace|].someClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"),
NewLines("Class Foo \n Sub bar() \n Dim q As SomeNamespace.InnerNamespace.someClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WorkItem(540543)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestCaseSensitivity1() As Task
            Await TestAsync(
NewLines("Class Foo \n Dim x As [|someclass|] \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"),
NewLines("Class Foo \n Dim x As SomeNamespace.SomeClass \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestTypeFromMultipleNamespaces1() As Task
            Await TestAsync(
NewLines("Class Foo \n Function F() As [|IDictionary|] \n End Function \n End Class"),
NewLines("Class Foo \n Function F() As System.Collections.IDictionary \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestTypeFromMultipleNamespaces2() As Task
            Await TestAsync(
NewLines("Class Foo \n Function F() As [|IDictionary|] \n End Function \n End Class"),
NewLines("Class Foo \n Function F() As System.Collections.Generic.IDictionary \n End Function \n End Class"),
index:=1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericWithNoArgs() As Task
            Await TestAsync(
NewLines("Class Foo \n Function F() As [|List|] \n End Function \n End Class"),
NewLines("Class Foo \n Function F() As System.Collections.Generic.List \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericWithCorrectArgs() As Task
            Await TestAsync(
NewLines("Class Foo \n Function F() As [|List(Of Integer)|] \n End Function \n End Class"),
NewLines("Class Foo \n Function F() As System.Collections.Generic.List(Of Integer) \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericWithWrongArgs() As Task
            Await TestMissingAsync(
NewLines("Class Foo \n Function F() As [|List(Of Integer, String)|] \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericInLocalDeclaration() As Task
            Await TestAsync(
NewLines("Class Foo \n Sub Test() \n Dim x As New [|List(Of Integer)|] \n End Sub \n End Class"),
NewLines("Class Foo \n Sub Test() \n Dim x As New System.Collections.Generic.List(Of Integer) \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericItemType() As Task
            Await TestAsync(
NewLines("Class Foo \n Sub Test() \n Dim x As New List(Of [|Int32|]) \n End Sub \n End Class"),
NewLines("Class Foo \n Sub Test() \n Dim x As New List(Of System.Int32) \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenerateInNamespace() As Task
            Await TestAsync(
NewLines("Imports System \n Namespace NS \n Class Foo \n Sub Test() \n Dim x As New [|List(Of Integer)|] \n End Sub \n End Class \n End Namespace"),
NewLines("Imports System \n Namespace NS \n Class Foo \n Sub Test() \n Dim x As New Collections.Generic.List(Of Integer) \n End Sub \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestMinimalQualify() As Task
            Await TestAsync(
NewLines("Imports System \n Module Program \n Dim q As [|List(Of Integer)|] \n End Module"),
NewLines("Imports System \n Module Program \n Dim q As Collections.Generic.List(Of Integer) \n End Module"))
        End Function

        <WorkItem(540559)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestEscaping1() As Task
            Await TestAsync(
NewLines("Class SomeClass \n Dim x As [|Something|] \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"),
NewLines("Class SomeClass \n Dim x As Outer.Namespace.Something \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WorkItem(540559)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestEscaping2() As Task
            Await TestAsync(
NewLines("Class SomeClass \n Dim x As [|Something|] \n End Class \n Namespace [Namespace] \n Namespace Inner \n Class Something \n End Class \n End Namespace \n End Namespace"),
NewLines("Class SomeClass \n Dim x As [Namespace].Inner.Something \n End Class \n Namespace [Namespace] \n Namespace Inner \n Class Something \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WorkItem(540559)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestEscaping3() As Task
            Await TestAsync(
NewLines("Class SomeClass \n Dim x As [|[Namespace]|] \n End Class \n Namespace Outer \n Namespace Inner \n Class [Namespace] \n End Class \n End Namespace \n End Namespace"),
NewLines("Class SomeClass \n Dim x As Outer.Inner.[Namespace] \n End Class \n Namespace Outer \n Namespace Inner \n Class [Namespace] \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WorkItem(540560)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestInImport() As Task
            Await TestAsync(
NewLines("Imports [|InnerNamespace|] \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"),
NewLines("Imports SomeNamespace.InnerNamespace \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WorkItem(540673)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestCaseInsensitivity() As Task
            Await TestAsync(
NewLines("Class FOo \n Sub bar() \n Dim q As [|innernamespace|].someClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"),
NewLines("Class FOo \n Sub bar() \n Dim q As SomeNamespace.InnerNamespace.someClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WorkItem(540706)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestStandaloneMethod() As Task
            Await TestAsync(
NewLines("'Class [Class] \n Private Sub Method(i As Integer) \n [|[Enum]|] = 5 \n End Sub \n End Class"),
NewLines("'Class [Class] \n Private Sub Method(i As Integer) \n System.[Enum] = 5 \n End Sub \n End Class"))
        End Function

        <WorkItem(540736)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestMissingOnBoundFieldType() As Task
            Await TestMissingAsync(
NewLines("Imports System.Collections.Generic \n Class A \n Private field As [|List(Of C)|] \n Sub Main() \n Dim local As List(Of C) \n End Sub \n End Class"))
        End Function

        <WorkItem(540736)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestMissingOnBoundLocalType() As Task
            Await TestMissingAsync(
NewLines("Imports System.Collections.Generic \n Class A \n Private field As [|List(Of C)|] \n Sub Main() \n Dim local As List(Of C) \n End Sub \n End Class"))
        End Function

        <WorkItem(540745)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestCaseSensitivity2() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As [|foo|] \n End Sub \n End Module \n Namespace OUTER \n Namespace INNER \n Friend Class FOO \n End Class \n End Namespace \n End Namespace"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As OUTER.INNER.FOO \n End Sub \n End Module \n Namespace OUTER \n Namespace INNER \n Friend Class FOO \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WorkItem(821292)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestCaseSensitivity3() As Task
            Await TestAsync(
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Dim x As [|stream|] \n End Sub \n End Module"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Dim x As IO.Stream \n End Sub \n End Module"))
        End Function

        <WorkItem(545993)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestNotOnNamedArgument() As Task
            Await TestMissingAsync(
NewLines("Module Program \n <MethodImpl([|methodImplOptions|]:=MethodImplOptions.ForwardRef) \n Sub Main(args As String()) \n End Sub \n End Module"))
        End Function

        <WorkItem(546107)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestDoNotQualifyNestedTypeOfGenericType() As Task
            Await TestMissingAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n  \n Class Program \n Shared Sub Main() \n CType(GetEnumerator(), IDisposable).Dispose() \n End Sub \n  \n Shared Function GetEnumerator() As [|Enumerator|] \n Return Nothing \n End Function \n End Class"))
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestFormattingInFullyQualify() As Task
            Await TestAsync(
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
        End Function

        <WorkItem(775448)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestShouldTriggerOnBC32045() As Task
            ' BC32045: 'A' has no type parameters and so cannot have type arguments.
            Await TestAsync(
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
        End Function

        <WorkItem(947579)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestAmbiguousTypeFix() As Task
            Await TestAsync(
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
        End Function

        Public Class AddImportTestsWithAddImportDiagnosticProvider
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(),
                    New VisualBasicFullyQualifyCodeFixProvider())
            End Function

            <WorkItem(829970)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestUnknownIdentifierInAttributeSyntaxWithoutTarget() As Task
                Await TestAsync(
    NewLines("Module Program \n <[|Extension|]> \n End Module"),
    NewLines("Module Program \n <System.Runtime.CompilerServices.Extension> \n End Module"))
            End Function
        End Class
    End Class
End Namespace
