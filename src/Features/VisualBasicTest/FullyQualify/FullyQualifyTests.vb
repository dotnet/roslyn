' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.FullyQualify
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.FullyQualify
    <Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)>
    Public Class FullyQualifyTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicFullyQualifyCodeFixProvider())
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestParameterType(testHost As TestHost) As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String(), f As [|FileMode|])
    End Sub
End Module",
"Module Program
    Sub Main(args As String(), f As System.IO.FileMode)
    End Sub
End Module", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSimpleQualifyFromSameFile(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1889385")>
        Public Async Function TestPreservesIncorrectIndentation(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestOrdering(testHost As TestHost) As Task
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
    Inherits [|TextBox|]

End Class"

            Await TestExactActionSetOfferedAsync(
                code,
                {"System.Windows.Controls.TextBox",
                 "System.Windows.Forms.TextBox",
                 "System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox"}, New TestParameters(testHost:=testHost))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSimpleQualifyFromReference(testHost As TestHost) As Task
            Await TestInRegularAndScriptAsync(
"Class Class1
    Dim v As [|Thread|]
End Class",
"Class Class1
    Dim v As System.Threading.Thread
End Class", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericClassDefinitionAsClause(testHost As TestHost) As Task
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
End Class", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericClassInstantiationOfClause(testHost As TestHost) As Task
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
End Class", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericMethodDefinitionAsClause(testHost As TestHost) As Task
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
End Class", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericMethodInvocationOfClause(testHost As TestHost) As Task
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
End Class", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestAttributeApplication(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestMultipleAttributeApplicationBelow(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestMultipleAttributeApplicationAbove(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestQualifierIsEscapedWhenNamespaceMatchesKeyword(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540559")>
        Public Async Function TestQualifierIsNOTEscapedWhenNamespaceMatchesKeywordButIsNested(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540560")>
        Public Async Function TestFullyQualifyInImportsStatement(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestFullyQualifyNotSuggestedForGenericTypeParametersOfClause(testHost As TestHost) As Task
            Await TestMissingInRegularAndScriptAsync(
"Class SomeClass
    Sub Goo(Of [|SomeClass|])(x As SomeClass)
    End Sub
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace", New TestParameters(testHost:=testHost))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestFullyQualifyNotSuggestedForGenericTypeParametersAsClause(testHost As TestHost) As Task
            Await TestMissingInRegularAndScriptAsync(
"Class SomeClass
    Sub Goo(Of SomeClass)(x As [|SomeClass|])
    End Sub
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace", New TestParameters(testHost:=testHost))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540673")>
        Public Async Function TestCaseSensitivityForNestedNamespace(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540543")>
        Public Async Function TestCaseSensitivity1(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestTypeFromMultipleNamespaces1(testHost As TestHost) As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Function F() As [|IDictionary|]
    End Function
End Class",
"Class Goo
    Function F() As System.Collections.IDictionary
    End Function
End Class", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestTypeFromMultipleNamespaces2(testHost As TestHost) As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Function F() As [|IDictionary|]
    End Function
End Class",
"Class Goo
    Function F() As System.Collections.Generic.IDictionary
    End Function
End Class",
index:=1, testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericWithNoArgs(testHost As TestHost) As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Function F() As [|List|]
    End Function
End Class",
"Class Goo
    Function F() As System.Collections.Generic.List
    End Function
End Class", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericWithCorrectArgs(testHost As TestHost) As Task
            Await TestInRegularAndScriptAsync(
"Class Goo
    Function F() As [|List(Of Integer)|]
    End Function
End Class",
"Class Goo
    Function F() As System.Collections.Generic.List(Of Integer)
    End Function
End Class", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericWithWrongArgs(testHost As TestHost) As Task
            Await TestMissingInRegularAndScriptAsync(
"Class Goo
    Function F() As [|List(Of Integer, String)|]
    End Function
End Class", New TestParameters(testHost:=testHost))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericInLocalDeclaration(testHost As TestHost) As Task
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
End Class", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericItemType(testHost As TestHost) As Task
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
End Class", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenerateInNamespace(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestMinimalQualify(testHost As TestHost) As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Module Program
    Dim q As [|List(Of Integer)|]
End Module",
"Imports System
Module Program
    Dim q As Collections.Generic.List(Of Integer)
End Module", testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540559")>
        Public Async Function TestEscaping1(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540559")>
        Public Async Function TestEscaping2(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540559")>
        Public Async Function TestEscaping3(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540560")>
        Public Async Function TestInImport(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540673")>
        Public Async Function TestCaseInsensitivity(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540706")>
        Public Async Function TestStandaloneMethod(testHost As TestHost) As Task
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

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540736")>
        Public Async Function TestMissingOnBoundFieldType(testHost As TestHost) As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System.Collections.Generic
Class A
    Private field As [|List(Of C)|]
    Sub Main()
        Dim local As List(Of C)
    End Sub
End Class", New TestParameters(testHost:=testHost))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540736")>
        Public Async Function TestMissingOnBoundLocalType(testHost As TestHost) As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System.Collections.Generic
Class A
    Private field As [|List(Of C)|]
    Sub Main()
        Dim local As List(Of C)
    End Sub
End Class", New TestParameters(testHost:=testHost))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540745")>
        Public Async Function TestCaseSensitivity2(testHost As TestHost) As Task
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
End Namespace", testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/821292")>
        Public Async Function TestCaseSensitivity3(testHost As TestHost) As Task
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
End Module", testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545993")>
        Public Async Function TestNotOnNamedArgument(testHost As TestHost) As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    <MethodImpl([|methodImplOptions|]:=MethodImplOptions.ForwardRef) 
 Sub Main(args As String())
    End Sub
End Module", New TestParameters(testHost:=testHost))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546107")>
        Public Async Function TestDoNotQualifyNestedTypeOfGenericType(testHost As TestHost) As Task
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
End Class", New TestParameters(testHost:=testHost))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestFormattingInFullyQualify(testHost As TestHost) As Task
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
End Module</Text>.Value.Replace(vbLf, vbCrLf), testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775448")>
        Public Async Function TestShouldTriggerOnBC32045(testHost As TestHost) As Task
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
End Module</Text>.Value.Replace(vbLf, vbCrLf), testHost:=testHost)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/947579")>
        Public Async Function TestAmbiguousTypeFix(testHost As TestHost) As Task
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
End Namespace</Text>.Value.Replace(vbLf, vbCrLf), testHost:=testHost)
        End Function

        Public Class AddImportTestsWithAddImportDiagnosticProvider
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
                Return (New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(),
                        New VisualBasicFullyQualifyCodeFixProvider())
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829970")>
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
