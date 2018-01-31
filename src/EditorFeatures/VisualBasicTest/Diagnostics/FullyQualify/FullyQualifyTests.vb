' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.FullyQualify
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.FullyQualify
    Public Class FullyQualifyTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicFullyQualifyCodeFixProvider())
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestParameterType() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String(), f As [|FileMode|])
    End Sub
End Module",
"Module Program
    Sub Main(args As String(), f As System.IO.FileMode)
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestSimpleQualifyFromSameFile() As Task
            Await TestInRegularAndScriptAsync(
"Class Class1
    Dim v As [|SomeClass1|]
End Class
Namespace SomeNamespace
    Public Class SomeClass1
    End Class
End Namespace",
"Class Class1
    Dim v As SomeNamespace.SomeClass1
End Class
Namespace SomeNamespace
    Public Class SomeClass1
    End Class
End Namespace")
        End Function

        Public Async Function TestOrdering() As Task
            Dim code = "
namespace System.Windows.Controls
    public class TextBox
    end class
end namespace

namespace System.Windows.Forms
    public class TextBox
    end class
end namespace

namespace System.Windows.Forms.VisualStyles.VisualStyleElement
    public class TextBox
    end class
end namespace

Public Class TextBoxEx
    Inherits TextBox

End Class"

            Await TestExactActionSetOfferedAsync(
                code,
                {"System.Windows.Controls.TextBox",
                 "System.Windows.Forms.TextBox",
                 "System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox"})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestSimpleQualifyFromReference() As Task
            Await TestInRegularAndScriptAsync(
"Class Class1
    Dim v As [|Thread|]
End Class",
"Class Class1
    Dim v As System.Threading.Thread
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericClassDefinitionAsClause() As Task
            Await TestInRegularAndScriptAsync(
"Namespace SomeNamespace
    Class Base
    End Class
End Namespace
Class SomeClass(Of x As [|Base|])
End Class",
"Namespace SomeNamespace
    Class Base
    End Class
End Namespace
Class SomeClass(Of x As SomeNamespace.Base)
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericClassInstantiationOfClause() As Task
            Await TestInRegularAndScriptAsync(
"Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class GenericClass(Of T)
End Class
Class Goo
    Sub Method1()
        Dim q As GenericClass(Of [|SomeClass|])
    End Sub
End Class",
"Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class GenericClass(Of T)
End Class
Class Goo
    Sub Method1()
        Dim q As GenericClass(Of SomeNamespace.SomeClass)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericMethodDefinitionAsClause() As Task
            Await TestInRegularAndScriptAsync(
"Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class Goo
    Sub Method1(Of T As [|SomeClass|])
    End Sub
End Class",
"Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class Goo
    Sub Method1(Of T As SomeNamespace.SomeClass)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericMethodInvocationOfClause() As Task
            Await TestInRegularAndScriptAsync(
"Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class Goo
    Sub Method1(Of T)
    End Sub
    Sub Method2()
        Method1(Of [|SomeClass|])
    End Sub
End Class",
"Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class Goo
    Sub Method1(Of T)
    End Sub
    Sub Method2()
        Method1(Of SomeNamespace.SomeClass)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestAttributeApplication() As Task
            Await TestInRegularAndScriptAsync(
"<[|Something|]()>
Class Goo
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits System.Attribute
    End Class
End Namespace",
"<SomeNamespace.Something()>
Class Goo
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits System.Attribute
    End Class
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestMultipleAttributeApplicationBelow() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
<Existing()>
<[|Something|]()>
Class Goo
End Class
Class ExistingAttribute
    Inherits System.Attribute
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits Attribute
    End Class
End Namespace",
"Imports System
<Existing()>
<SomeNamespace.Something()>
Class Goo
End Class
Class ExistingAttribute
    Inherits System.Attribute
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits Attribute
    End Class
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestMultipleAttributeApplicationAbove() As Task
            Await TestInRegularAndScriptAsync(
"<[|Something|]()>
<Existing()>
Class Goo
End Class
Class ExistingAttribute
    Inherits System.Attribute
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits System.Attribute
    End Class
End Namespace",
"<SomeNamespace.Something()>
<Existing()>
Class Goo
End Class
Class ExistingAttribute
    Inherits System.Attribute
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits System.Attribute
    End Class
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestQualifierIsEscapedWhenNamespaceMatchesKeyword() As Task
            Await TestInRegularAndScriptAsync(
"Class SomeClass
    Dim x As [|Something|]
End Class
Namespace [Namespace]
    Class Something
    End Class
End Namespace",
"Class SomeClass
    Dim x As [Namespace].Something
End Class
Namespace [Namespace]
    Class Something
    End Class
End Namespace")
        End Function

        <WorkItem(540559, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540559")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestQualifierIsNOTEscapedWhenNamespaceMatchesKeywordButIsNested() As Task
            Await TestInRegularAndScriptAsync(
"Class SomeClass
    Dim x As [|Something|]
End Class
Namespace Outer
    Namespace [Namespace]
        Class Something
        End Class
    End Namespace
End Namespace",
"Class SomeClass
    Dim x As Outer.Namespace.Something
End Class
Namespace Outer
    Namespace [Namespace]
        Class Something
        End Class
    End Namespace
End Namespace")
        End Function

        <WorkItem(540560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540560")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestFullyQualifyInImportsStatement() As Task
            Await TestInRegularAndScriptAsync(
"Imports [|InnerNamespace|]
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace",
"Imports SomeNamespace.InnerNamespace
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestFullyQualifyNotSuggestedForGenericTypeParametersOfClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class SomeClass
    Sub Goo(Of [|SomeClass|])(x As SomeClass)
    End Sub
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestFullyQualifyNotSuggestedForGenericTypeParametersAsClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class SomeClass
    Sub Goo(Of SomeClass)(x As [|SomeClass|])
    End Sub
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace")
        End Function

        <WorkItem(540673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540673")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestCaseSensitivityForNestedNamespace() As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Sub bar()
        Dim q As [|innernamespace|].someClass
    End Sub
End Class
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace",
"Class Goo
    Sub bar()
        Dim q As SomeNamespace.InnerNamespace.someClass
    End Sub
End Class
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace")
        End Function

        <WorkItem(540543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540543")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestCaseSensitivity1() As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Dim x As [|someclass|]
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace",
"Class Goo
    Dim x As SomeNamespace.SomeClass
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestTypeFromMultipleNamespaces1() As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Function F() As [|IDictionary|]
    End Function
End Class",
"Class Goo
    Function F() As System.Collections.IDictionary
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestTypeFromMultipleNamespaces2() As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Function F() As [|IDictionary|]
    End Function
End Class",
"Class Goo
    Function F() As System.Collections.Generic.IDictionary
    End Function
End Class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericWithNoArgs() As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Function F() As [|List|]
    End Function
End Class",
"Class Goo
    Function F() As System.Collections.Generic.List
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericWithCorrectArgs() As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Function F() As [|List(Of Integer)|]
    End Function
End Class",
"Class Goo
    Function F() As System.Collections.Generic.List(Of Integer)
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericWithWrongArgs() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class Goo
    Function F() As [|List(Of Integer, String)|]
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericInLocalDeclaration() As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Sub Test()
        Dim x As New [|List(Of Integer)|]
    End Sub
End Class",
"Class Goo
    Sub Test()
        Dim x As New System.Collections.Generic.List(Of Integer)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenericItemType() As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Sub Test()
        Dim x As New List(Of [|Int32|])
    End Sub
End Class",
"Class Goo
    Sub Test()
        Dim x As New List(Of System.Int32)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestGenerateInNamespace() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Namespace NS
    Class Goo
        Sub Test()
            Dim x As New [|List(Of Integer)|]
        End Sub
    End Class
End Namespace",
"Imports System
Namespace NS
    Class Goo
        Sub Test()
            Dim x As New Collections.Generic.List(Of Integer)
        End Sub
    End Class
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestMinimalQualify() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Module Program
    Dim q As [|List(Of Integer)|]
End Module",
"Imports System
Module Program
    Dim q As Collections.Generic.List(Of Integer)
End Module")
        End Function

        <WorkItem(540559, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540559")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestEscaping1() As Task
            Await TestInRegularAndScriptAsync(
"Class SomeClass
    Dim x As [|Something|]
End Class
Namespace Outer
    Namespace [Namespace]
        Class Something
        End Class
    End Namespace
End Namespace",
"Class SomeClass
    Dim x As Outer.Namespace.Something
End Class
Namespace Outer
    Namespace [Namespace]
        Class Something
        End Class
    End Namespace
End Namespace")
        End Function

        <WorkItem(540559, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540559")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestEscaping2() As Task
            Await TestInRegularAndScriptAsync(
"Class SomeClass
    Dim x As [|Something|]
End Class
Namespace [Namespace]
    Namespace Inner
        Class Something
        End Class
    End Namespace
End Namespace",
"Class SomeClass
    Dim x As [Namespace].Inner.Something
End Class
Namespace [Namespace]
    Namespace Inner
        Class Something
        End Class
    End Namespace
End Namespace")
        End Function

        <WorkItem(540559, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540559")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestEscaping3() As Task
            Await TestInRegularAndScriptAsync(
"Class SomeClass
    Dim x As [|[Namespace]|]
End Class
Namespace Outer
    Namespace Inner
        Class [Namespace]
        End Class
    End Namespace
End Namespace",
"Class SomeClass
    Dim x As Outer.Inner.[Namespace]
End Class
Namespace Outer
    Namespace Inner
        Class [Namespace]
        End Class
    End Namespace
End Namespace")
        End Function

        <WorkItem(540560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540560")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestInImport() As Task
            Await TestInRegularAndScriptAsync(
"Imports [|InnerNamespace|]
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace",
"Imports SomeNamespace.InnerNamespace
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace")
        End Function

        <WorkItem(540673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540673")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestCaseInsensitivity() As Task
            Await TestInRegularAndScriptAsync(
"Class GOo
    Sub bar()
        Dim q As [|innernamespace|].someClass
    End Sub
End Class
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace",
"Class GOo
    Sub bar()
        Dim q As SomeNamespace.InnerNamespace.someClass
    End Sub
End Class
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace")
        End Function

        <WorkItem(540706, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540706")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestStandaloneMethod() As Task
            Await TestInRegularAndScriptAsync(
"'Class [Class] 
Private Sub Method(i As Integer)
    [|[Enum]|] = 5
End Sub
End Class",
"'Class [Class] 
Private Sub Method(i As Integer)
    System.[Enum] = 5
End Sub
End Class")
        End Function

        <WorkItem(540736, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540736")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestMissingOnBoundFieldType() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System.Collections.Generic
Class A
    Private field As [|List(Of C)|]
    Sub Main()
        Dim local As List(Of C)
    End Sub
End Class")
        End Function

        <WorkItem(540736, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540736")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestMissingOnBoundLocalType() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System.Collections.Generic
Class A
    Private field As [|List(Of C)|]
    Sub Main()
        Dim local As List(Of C)
    End Sub
End Class")
        End Function

        <WorkItem(540745, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540745")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestCaseSensitivity2() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As [|goo|]
    End Sub
End Module
Namespace OUTER
    Namespace INNER
        Friend Class GOO
        End Class
    End Namespace
End Namespace",
"Module Program
    Sub Main(args As String())
        Dim x As OUTER.INNER.GOO
    End Sub
End Module
Namespace OUTER
    Namespace INNER
        Friend Class GOO
        End Class
    End Namespace
End Namespace")
        End Function

        <WorkItem(821292, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/821292")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestCaseSensitivity3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Module Program
    Sub Main(args As String())
        Dim x As [|stream|]
    End Sub
End Module",
"Imports System
Module Program
    Sub Main(args As String())
        Dim x As IO.Stream
    End Sub
End Module")
        End Function

        <WorkItem(545993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545993")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestNotOnNamedArgument() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    <MethodImpl([|methodImplOptions|]:=MethodImplOptions.ForwardRef) 
 Sub Main(args As String())
    End Sub
End Module")
        End Function

        <WorkItem(546107, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546107")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestDoNotQualifyNestedTypeOfGenericType() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic

Class Program
    Shared Sub Main()
        CType(GetEnumerator(), IDisposable).Dispose()
    End Sub

    Shared Function GetEnumerator() As [|Enumerator|]
        Return Nothing
    End Function
End Class")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestFormattingInFullyQualify() As Task
            Await TestInRegularAndScriptAsync(
<Text>Module Program
    &lt;[|Obsolete|]&gt;
    Sub Main(args As String())
    End Sub
End Module</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Module Program
    &lt;System.Obsolete&gt;
    Sub Main(args As String())
    End Sub
End Module</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(775448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775448")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestShouldTriggerOnBC32045() As Task
            ' BC32045: 'A' has no type parameters and so cannot have type arguments.
            Await TestInRegularAndScriptAsync(
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
End Module</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(947579, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/947579")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
        Public Async Function TestAmbiguousTypeFix() As Task
            Await TestInRegularAndScriptAsync(
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
End Namespace</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        Public Class AddImportTestsWithAddImportDiagnosticProvider
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
                Return (New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(),
                        New VisualBasicFullyQualifyCodeFixProvider())
            End Function

            <WorkItem(829970, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829970")>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestUnknownIdentifierInAttributeSyntaxWithoutTarget() As Task
                Await TestInRegularAndScriptAsync(
"Module Program
    <[|Extension|]>
End Module",
"Module Program
    <System.Runtime.CompilerServices.Extension>
End Module")
            End Function
        End Class
    End Class
End Namespace
