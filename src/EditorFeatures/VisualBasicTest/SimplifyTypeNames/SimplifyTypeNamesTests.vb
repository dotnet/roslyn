' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames
Imports Microsoft.CodeAnalysis.VisualBasic.SimplifyTypeNames

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SimplifyTypeNames
    Partial Public Class SimplifyTypeNamesTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicSimplifyTypeNamesDiagnosticAnalyzer(),
                    New SimplifyTypeNamesCodeFixProvider())
        End Function

        Private Function PreferIntrinsicPredefinedTypeEverywhere() As IDictionary(Of OptionKey, Object)
            Dim language = GetLanguage()

            Return OptionsSet(
                SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, True, NotificationOption.Error),
                SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, Me.onWithError, language))
        End Function

        Private Function PreferIntrinsicPredefinedTypeInDeclaration() As IDictionary(Of OptionKey, Object)
            Dim language = GetLanguage()

            Return OptionsSet(
                SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, True, NotificationOption.Error),
                SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, Me.offWithSilent, language))
        End Function

        Private Function PreferIntrinsicTypeInMemberAccess() As IDictionary(Of OptionKey, Object)
            Dim language = GetLanguage()

            Return OptionsSet(
                SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, True, NotificationOption.Error),
                SingleOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, Me.offWithSilent, language))
        End Function

        Private ReadOnly onWithError As New CodeStyleOption(Of Boolean)(True, NotificationOption.Error)
        Private ReadOnly offWithSilent As New CodeStyleOption(Of Boolean)(False, NotificationOption.Silent)

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestGenericNames() As Task
            Dim source =
        <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Shared Sub F(Of T)(x As Func(Of Integer, T))

    End Sub

    Shared Sub main()
        [|F(Of Integer)|](Function(a) a)
    End Sub
End Class
</Code>
            Dim expected =
        <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Shared Sub F(Of T)(x As Func(Of Integer, T))

    End Sub

    Shared Sub main()
        F(Function(a) a)
    End Sub
End Class
</Code>
            Await TestInRegularAndScriptAsync(source.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestArgument() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As [|System.String|]())
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
    End Sub
End Module",
options:=PreferIntrinsicPredefinedTypeEverywhere())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestAliasWithMemberAccess() As Task
            Await TestInRegularAndScriptAsync(
"Imports Goo = System.Int32
Module Program
    Sub Main(args As String())
        Dim x = [|System.Int32|].MaxValue
    End Sub
End Module",
"Imports Goo = System.Int32
Module Program
    Sub Main(args As String())
        Dim x = Goo.MaxValue
    End Sub
End Module")
        End Function

        <WorkItem(21449, "https://github.com/dotnet/roslyn/issues/21449")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function DoNotChangeToAliasInNameOfIfItChangesNameOfName() As Task
            Await TestInRegularAndScript1Async(
"Imports System
Imports Foo = SimplifyInsideNameof.Program

namespace SimplifyInsideNameof
  class Program
    shared sub Main()
      Console.WriteLine(nameof([|SimplifyInsideNameof.Program|]))
    end sub
  end class
end namespace",
"Imports System
Imports Foo = SimplifyInsideNameof.Program

namespace SimplifyInsideNameof
  class Program
    shared sub Main()
      Console.WriteLine(nameof(Program))
    end sub
  end class
end namespace")
        End Function

        <WorkItem(21449, "https://github.com/dotnet/roslyn/issues/21449")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function DoChangeToAliasInNameOfIfItDoesNotAffectName1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports Goo = SimplifyInsideNameof.Program

namespace SimplifyInsideNameof
  class Program
    shared sub Main()
      Console.WriteLine(nameof([|SimplifyInsideNameof.Program|].Main))
    end sub
  end class
end namespace",
"Imports System
Imports Goo = SimplifyInsideNameof.Program

namespace SimplifyInsideNameof
  class Program
    shared sub Main()
      Console.WriteLine(nameof(Goo.Main))
    end sub
  end class
end namespace")
        End Function

        <WorkItem(21449, "https://github.com/dotnet/roslyn/issues/21449")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function DoChangeToAliasInNameOfIfItDoesNotAffectName2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports Goo = N.Goo

namespace N
    class Goo
    end class
end namespace

namespace SimplifyInsideNameof
  class Program
    shared sub Main()
      Console.WriteLine(nameof([|N.Goo|]))
    end sub
  end class
end namespace",
"Imports System
Imports Goo = N.Goo

namespace N
    class Goo
    end class
end namespace

namespace SimplifyInsideNameof
  class Program
    shared sub Main()
      Console.WriteLine(nameof(Goo))
    end sub
  end class
end namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestWithCursorAtBeginning() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.IO
Module Program
    Sub Main(args As String())
        Dim x As [|System.IO.File|]
    End Sub
End Module",
"Imports System.IO
Module Program
    Sub Main(args As String())
        Dim x As File
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMinimalSimplifyOnNestedNamespaces() As Task
            Dim source =
"Imports Outer
Namespace Outer
    Namespace Inner
        Class Goo
        End Class
    End Namespace
End Namespace
Module Program
    Sub Main(args As String())
        Dim x As [|Outer.Inner.Goo|]
    End Sub
End Module"

            Await TestInRegularAndScriptAsync(source,
"Imports Outer
Namespace Outer
    Namespace Inner
        Class Goo
        End Class
    End Namespace
End Namespace
Module Program
    Sub Main(args As String())
        Dim x As Inner.Goo
    End Sub
End Module")
            Await TestActionCountAsync(source, 1)
        End Function

        <WorkItem(540567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540567")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMinimalSimplifyOnNestedNamespacesFromMetadataAlias() As Task
            Await TestInRegularAndScriptAsync(
"Imports A1 = System.IO.File
Class Goo
    Dim x As [|System.IO.File|]
End Class",
"Imports A1 = System.IO.File
Class Goo
    Dim x As A1
End Class")
        End Function

        <WorkItem(540567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540567")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMinimalSimplifyOnNestedNamespacesFromMetadata() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class Goo
    Dim x As [|System.IO.File|]
End Class",
"Imports System
Class Goo
    Dim x As IO.File
End Class")
        End Function

        <WorkItem(540569, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540569")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestFixAllOccurrences() As Task
            Await TestInRegularAndScriptAsync(
"Imports NS1
Namespace NS1
    Class SomeClass
    End Class
End Namespace
Class Goo
    Dim x As {|FixAllInDocument:NS1.SomeClass|}
    Dim y As NS1.SomeClass
End Class",
"Imports NS1
Namespace NS1
    Class SomeClass
    End Class
End Namespace
Class Goo
    Dim x As SomeClass
    Dim y As SomeClass
End Class")
        End Function

        <WorkItem(578686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578686")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/9877"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllOccurrencesForAliases() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports goo = C.D
Imports bar = A.B
Namespace C
    Class D
    End Class
End Namespace
Module Program Sub Main(args As String()) 
 Dim Local1 = [|New A.B().prop|]
    End Sub
End Module
Namespace A
    Class B
        Public Property prop As C.D
    End Class
End Namespace",
"Imports System
Imports goo = C.D
Imports bar = A.B
Namespace C
    Class D
    End Class
End Namespace
Module Program Sub Main(args As String()) 
 Dim Local1 = New bar().prop
    End Sub
End Module
Namespace A
    Class B
        Public Property prop As goo
    End Class
End Namespace",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyFromReference() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading
Class Class1
    Dim v As [|System.Threading.Thread|]
End Class",
"Imports System.Threading
Class Class1
    Dim v As Thread
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestGenericClassDefinitionAsClause() As Task
            Await TestInRegularAndScriptAsync(
"Imports SomeNamespace
Namespace SomeNamespace
    Class Base
    End Class
End Namespace
Class SomeClass(Of x As [|SomeNamespace.Base|])
End Class",
"Imports SomeNamespace
Namespace SomeNamespace
    Class Base
    End Class
End Namespace
Class SomeClass(Of x As Base)
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestGenericClassInstantiationOfClause() As Task
            Await TestInRegularAndScriptAsync(
"Imports SomeNamespace
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class GenericClass(Of T)
End Class
Class Goo
    Sub Method1()
        Dim q As GenericClass(Of [|SomeNamespace.SomeClass|])
    End Sub
End Class",
"Imports SomeNamespace
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class GenericClass(Of T)
End Class
Class Goo
    Sub Method1()
        Dim q As GenericClass(Of SomeClass)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestGenericMethodDefinitionAsClause() As Task
            Await TestInRegularAndScriptAsync(
"Imports SomeNamespace
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class Goo
    Sub Method1(Of T As [|SomeNamespace.SomeClass|])
    End Sub
End Class",
"Imports SomeNamespace
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class Goo
    Sub Method1(Of T As SomeClass)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestGenericMethodInvocationOfClause() As Task
            Await TestInRegularAndScriptAsync(
"Imports SomeNamespace
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class Goo
    Sub Method1(Of T)
    End Sub
    Sub Method2()
        Method1(Of [|SomeNamespace.SomeClass|])
    End Sub
End Class",
"Imports SomeNamespace
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class Goo
    Sub Method1(Of T)
    End Sub
    Sub Method2()
        Method1(Of SomeClass)
    End Sub
End Class")
        End Function

        <WorkItem(6872, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestAttributeApplication() As Task
            Await TestInRegularAndScriptAsync(
"Imports SomeNamespace
<[|SomeNamespace.Something|]()>
Class Goo
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits System.Attribute
    End Class
End Namespace",
"Imports SomeNamespace
<Something()>
Class Goo
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits System.Attribute
    End Class
End Namespace")
        End Function

        <WorkItem(6872, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMultipleAttributeApplicationBelow() As Task

            'IMPLEMENT NOT ESCAPE ATTRIBUTE DEPENDENT ON CONTEXT

            Await TestInRegularAndScriptAsync(
"Imports System
Imports SomeNamespace
<Existing()>
<[|SomeNamespace.Something|]()>
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
"Imports System
Imports SomeNamespace
<Existing()>
<Something()>
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

        <WorkItem(6872, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMultipleAttributeApplicationAbove() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports SomeNamespace
<[|SomeNamespace.Something|]()>
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
"Imports System
Imports SomeNamespace
<Something()>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifiedLeftmostQualifierIsEscapedWhenMatchesKeyword() As Task
            Await TestInRegularAndScriptAsync(
"Imports Outer
Class SomeClass
    Dim x As [|Outer.Namespace.Something|]
End Class
Namespace Outer
    Namespace [Namespace]
        Class Something
        End Class
    End Namespace
End Namespace",
"Imports Outer
Class SomeClass
    Dim x As [Namespace].Something
End Class
Namespace Outer
    Namespace [Namespace]
        Class Something
        End Class
    End Namespace
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestTypeNameIsEscapedWhenMatchingKeyword() As Task
            Await TestInRegularAndScriptAsync(
"Imports Outer
Class SomeClass
    Dim x As [|Outer.Class|]
End Class
Namespace Outer
    Class [Class]
    End Class
End Namespace",
"Imports Outer
Class SomeClass
    Dim x As [Class]
End Class
Namespace Outer
    Class [Class]
    End Class
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyNotSuggestedInImportsStatement() As Task
            Await TestMissingInRegularAndScriptAsync(
"[|Imports SomeNamespace
Imports SomeNamespace.InnerNamespace
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace|]")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNoSimplifyInGenericAsClauseIfConflictsWithTypeParameterName() As Task
            Await TestMissingInRegularAndScriptAsync(
"[|Imports SomeNamespace
Class Class1
    Sub Goo(Of SomeClass)(x As SomeNamespace.SomeClass)
    End Sub
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace|]")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyNotOfferedIfSimplifyingWouldCauseAmbiguity() As Task
            Await TestMissingInRegularAndScriptAsync(
"[|Imports SomeNamespace
Class SomeClass
End Class
Class Class1
    Dim x As SomeNamespace.SomeClass
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace|]")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyInGenericAsClauseIfNoConflictWithTypeParameterName() As Task
            Await TestInRegularAndScriptAsync(
"Imports SomeNamespace
Class Class1
    Sub Goo(Of T)(x As [|SomeNamespace.SomeClass|])
    End Sub
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace",
"Imports SomeNamespace
Class Class1
    Sub Goo(Of T)(x As SomeClass)
    End Sub
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestCaseInsensitivity() As Task
            Await TestInRegularAndScriptAsync(
"Imports SomeNamespace
Class Goo
    Dim x As [|SomeNamespace.someclass|]
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace",
"Imports SomeNamespace
Class Goo
    Dim x As someclass
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyGenericTypeWithArguments() As Task
            Dim source =
"Imports System.Collections.Generic
Class Goo
    Function F() As [|System.Collections.Generic.List(Of Integer)|]
    End Function
End Class"

            Await TestInRegularAndScriptAsync(source,
"Imports System.Collections.Generic
Class Goo
    Function F() As List(Of Integer)
    End Function
End Class")
            Await TestActionCountAsync(source, 1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestParameterType() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.IO
Module Program
    Sub Main(args As String(), f As [|System.IO.FileMode|])
    End Sub
End Module",
"Imports System.IO
Module Program
    Sub Main(args As String(), f As FileMode)
    End Sub
End Module")
        End Function

        <WorkItem(540565, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540565")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestLocation1() As Task
            Await TestInRegularAndScriptAsync(
"Imports Goo
Namespace Goo
    Class GooClass
    End Class
End Namespace
Module Program
    Sub Main(args As String())
        Dim x As [|Goo.GooClass|]
    End Sub
End Module",
"Imports Goo
Namespace Goo
    Class GooClass
    End Class
End Namespace
Module Program
    Sub Main(args As String())
        Dim x As GooClass
    End Sub
End Module")
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/9877"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllFixesUnrelatedTypes() As Task
            Await TestInRegularAndScriptAsync(
"Imports A
Imports B
Imports C
Module Program
    Sub Method1(a As [|A.GooA|], b As B.GooB, c As C.GooC)
        Dim qa As A.GooA
        Dim qb As B.GooB
        Dim qc As C.GooC
    End Sub
End Module
Namespace A
    Class GooA
    End Class
End Namespace
Namespace B
    Class GooB
    End Class
End Namespace
Namespace C
    Class GooC
    End Class
End Namespace",
"Imports A
Imports B
Imports C
Module Program
    Sub Method1(a As GooA, b As GooB, c As GooC)
        Dim qa As GooA
        Dim qb As GooB
        Dim qc As GooC
    End Sub
End Module
Namespace A
    Class GooA
    End Class
End Namespace
Namespace B
    Class GooB
    End Class
End Namespace
Namespace C
    Class GooC
    End Class
End Namespace")
        End Function

        <Fact(Skip:="1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestSimplifyFixesAllNestedTypeNames() As Task
            Dim source =
"Imports A
Imports B
Imports C
Module Program
    Sub Method1(a As [|A.GooA(Of B.GooB(Of C.GooC))|])
    End Sub
End Module
Namespace A
    Class GooA(Of T)
    End Class
End Namespace
Namespace B
    Class GooB(Of T)
    End Class
End Namespace
Namespace C
    Class GooC
    End Class
End Namespace"

            Await TestInRegularAndScriptAsync(source,
"Imports A
Imports B
Imports C
Module Program
    Sub Method1(a As GooA(Of GooB(Of GooC)))
    End Sub
End Module
Namespace A
    Class GooA(Of T)
    End Class
End Namespace
Namespace B
    Class GooB(Of T)
    End Class
End Namespace
Namespace C
    Class GooC
    End Class
End Namespace",
        index:=1)
            Await TestActionCountAsync(source, 1)
        End Function

        <WorkItem(551040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/551040")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyNestedType() As Task
            Dim source =
"Class Preserve
    Public Class X
        Public Shared Y
    End Class
End Class
Class Z(Of T)
    Inherits Preserve
End Class
Class M
    Public Shared Sub Main()
        Redim [|Z(Of Integer).X|].Y(1)
        End Function
 End Class"

            Await TestInRegularAndScriptAsync(source,
"Class Preserve
    Public Class X
        Public Shared Y
    End Class
End Class
Class Z(Of T)
    Inherits Preserve
End Class
Class M
    Public Shared Sub Main()
        Redim [Preserve].X.Y(1)
        End Function
 End Class")
            Await TestActionCountAsync(source, 1)
        End Function

        <WorkItem(551040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/551040")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyStaticMemberAccess() As Task
            Dim source =
"Class Preserve
    Public Shared Y
End Class
Class Z(Of T)
    Inherits Preserve
End Class
Class M
    Public Shared Sub Main()
        Redim [|Z(Of Integer).Y(1)|]
        End Function
 End Class"

            Await TestInRegularAndScriptAsync(source,
"Class Preserve
    Public Shared Y
End Class
Class Z(Of T)
    Inherits Preserve
End Class
Class M
    Public Shared Sub Main()
        Redim [Preserve].Y(1)
        End Function
 End Class")
            Await TestActionCountAsync(source, 1)
        End Function

        <WorkItem(540398, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540398")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestImplementsClause() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class Goo
    Implements IComparable(Of String)
    Public Function CompareTo(other As String) As Integer Implements [|System.IComparable(Of String).CompareTo|]
        Return Nothing
    End Function
End Class",
"Imports System
Class Goo
    Implements IComparable(Of String)
    Public Function CompareTo(other As String) As Integer Implements IComparable(Of String).CompareTo
        Return Nothing
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimpleArray() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Collections.Generic
Namespace N1
    Class Test
        Private a As [|System.Collections.Generic.List(Of System.String())|]
    End Class
End Namespace",
"Imports System.Collections.Generic
Namespace N1
    Class Test
        Private a As List(Of String())
    End Class
End Namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimpleMultiDimArray() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Collections.Generic
Namespace N1
    Class Test
        Private a As [|System.Collections.Generic.List(Of System.String()(,)(,,,))
|]
    End Class
End Namespace",
"Imports System.Collections.Generic
Namespace N1
    Class Test
        Private a As List(Of String()(,)(,,,))

    End Class
End Namespace")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyTypeInScriptCode() As Task
            Await TestAsync(
"Imports System
[|System.Console.WriteLine(0)|]",
"Imports System
Console.WriteLine(0)",
        parseOptions:=TestOptions.Script)
        End Function

        <WorkItem(542093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542093")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNoSimplificationOfParenthesizedPredefinedTypes() As Task
            Await TestMissingInRegularAndScriptAsync(
"[|Module M
    Sub Main()
        Dim x = (System.String).Equals("", "")
    End Sub
End Module|]")
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/9877"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestConflicts() As Task
            Await TestInRegularAndScriptAsync(
        <Text>
Namespace OuterNamespace
    Namespace InnerNamespace

        Class InnerClass1

        End Class
    End Namespace

    Class OuterClass1

        Function M1() As [|OuterNamespace.OuterClass1|]
            Dim c1 As OuterNamespace.OuterClass1
            OuterNamespace.OuterClass1.Equals(1, 2)
        End Function

        Function M2() As OuterNamespace.OuterClass2
            Dim c1 As OuterNamespace.OuterClass2
            OuterNamespace.OuterClass2.Equals(1, 2)
        End Function

        Function M3() As OuterNamespace.InnerNamespace.InnerClass1
            Dim c1 As OuterNamespace.InnerNamespace.InnerClass1
            OuterNamespace.InnerNamespace.InnerClass1.Equals(1, 2)
        End Function

        Function M3() As InnerNamespace.InnerClass1
            Dim c1 As InnerNamespace.InnerClass1
            InnerNamespace.InnerClass1.Equals(1, 2)
        End Function

        Sub OuterClass2()
        End Sub

        Sub InnerClass1()
        End Sub

        Sub InnerNamespace()
        End Sub
    End Class

    Class OuterClass2

        Function M1() As OuterNamespace.OuterClass1
            Dim c1 As OuterNamespace.OuterClass1
            OuterNamespace.OuterClass1.Equals(1, 2)
        End Function

        Function M2() As OuterNamespace.OuterClass2
            Dim c1 As OuterNamespace.OuterClass2
            OuterNamespace.OuterClass2.Equals(1, 2)
        End Function

        Function M3() As OuterNamespace.InnerNamespace.InnerClass1
            Dim c1 As OuterNamespace.InnerNamespace.InnerClass1
            OuterNamespace.InnerNamespace.InnerClass1.Equals(1, 2)
        End Function

        Function M3() As InnerNamespace.InnerClass1
            Dim c1 As InnerNamespace.InnerClass1
            InnerNamespace.InnerClass1.Equals(1, 2)
        End Function
    End Class
End Namespace

</Text>.Value.Replace(vbLf, vbCrLf),
        <Text>
Namespace OuterNamespace
    Namespace InnerNamespace

        Class InnerClass1

        End Class
    End Namespace

    Class OuterClass1

        Function M1() As OuterClass1
            Dim c1 As OuterClass1
            Equals(1, 2)
        End Function

        Function M2() As OuterClass2
            Dim c1 As OuterClass2
            Equals(1, 2)
        End Function

        Function M3() As InnerNamespace.InnerClass1
            Dim c1 As InnerNamespace.InnerClass1
            Equals(1, 2)
        End Function

        Function M3() As InnerNamespace.InnerClass1
            Dim c1 As InnerNamespace.InnerClass1
            InnerNamespace.InnerClass1.Equals(1, 2)
        End Function

        Sub OuterClass2()
        End Sub

        Sub InnerClass1()
        End Sub

        Sub InnerNamespace()
        End Sub
    End Class

    Class OuterClass2

        Function M1() As OuterClass1
            Dim c1 As OuterClass1
            Equals(1, 2)
        End Function

        Function M2() As OuterClass2
            Dim c1 As OuterClass2
            Equals(1, 2)
        End Function

        Function M3() As InnerNamespace.InnerClass1
            Dim c1 As InnerNamespace.InnerClass1
            Equals(1, 2)
        End Function

        Function M3() As InnerNamespace.InnerClass1
            Dim c1 As InnerNamespace.InnerClass1
            Equals(1, 2)
        End Function
    End Class
End Namespace

</Text>.Value.Replace(vbLf, vbCrLf),
        index:=1)
        End Function

        <WorkItem(542138, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542138")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyModuleWithReservedName() As Task
            Await TestInRegularAndScriptAsync(
"Namespace X
    Module [String]
        Sub Main()
            [|X.String.Main|]
        End Sub
    End Module
End Namespace",
"Namespace X
    Module [String]
        Sub Main()
            Main
        End Sub
    End Module
End Namespace")
        End Function

        <WorkItem(542348, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542348")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestPreserve1() As Task
            Await TestInRegularAndScriptAsync(
"Module M
    Dim preserve()
    Sub Main()
        ReDim [|M.preserve|](1)
    End Sub
End Module",
"Module M
    Dim preserve()
    Sub Main()
        ReDim [preserve](1)
    End Sub
End Module")
        End Function

        <WorkItem(551040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/551040")>
        <WorkItem(542348, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542348")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestPreserve3() As Task
            Await TestInRegularAndScriptAsync(
"Class Preserve
    Class X
        Public Shared Dim Y
    End Class
End Class
Class Z(Of T)
    Inherits Preserve
End Class
Module M
    Sub Main()
        ReDim [|Z(Of Integer).X.Y|](1) ' Simplify Z(Of Integer).X 
    End Sub
End Module",
"Class Preserve
    Class X
        Public Shared Dim Y
    End Class
End Class
Class Z(Of T)
    Inherits Preserve
End Class
Module M
    Sub Main()
        ReDim [Preserve].X.Y(1) ' Simplify Z(Of Integer).X 
    End Sub
End Module")
        End Function

        <WorkItem(545603, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableInImports1() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports [|System.Nullable(Of Integer)|]")
        End Function

        <WorkItem(545603, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545603")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableInImports2() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports [|System.Nullable(Of Integer)|]")
        End Function

        <WorkItem(545795, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545795")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestColorColor1() As Task
            Await TestInRegularAndScriptAsync(
"Namespace N
    Class Color
        Shared Sub Goo()
 End Class

    Class Program
        Shared Property Color As Color

        Shared Sub Main()
            Dim c = [|N.Color.Goo|]()
        End Sub
    End Class
End Namespace",
"Namespace N
    Class Color
        Shared Sub Goo()
 End Class

    Class Program
        Shared Property Color As Color

        Shared Sub Main()
            Dim c = Color.Goo()
        End Sub
    End Class
End Namespace")
        End Function

        <WorkItem(545795, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545795")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestColorColor2() As Task
            Await TestInRegularAndScriptAsync(
"Namespace N
    Class Color
        Shared Sub Goo()
 End Class

    Class Program
        Shared Property Color As Color

        Shared Sub Main()
            Dim c = [|N.Color.Goo|]()
        End Sub
    End Class
End Namespace",
"Namespace N
    Class Color
        Shared Sub Goo()
 End Class

    Class Program
        Shared Property Color As Color

        Shared Sub Main()
            Dim c = Color.Goo()
        End Sub
    End Class
End Namespace")
        End Function

        <WorkItem(545795, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545795")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestColorColor3() As Task
            Await TestInRegularAndScriptAsync(
"Namespace N
    Class Color
        Shared Sub Goo()
 End Class

    Class Program
        Shared Property Color As Color

        Shared Sub Main()
            Dim c = [|N.Color.Goo|]()
        End Sub
    End Class
End Namespace",
"Namespace N
    Class Color
        Shared Sub Goo()
 End Class

    Class Program
        Shared Property Color As Color

        Shared Sub Main()
            Dim c = Color.Goo()
        End Sub
    End Class
End Namespace")
        End Function

        <WorkItem(546829, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546829")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestKeyword1() As Task
            Await TestInRegularAndScriptAsync(
"Module m
    Sub main()
        Dim x = [|m.Equals|](1, 1)
    End Sub
End Module",
"Module m
    Sub main()
        Dim x = Equals(1, 1)
    End Sub
End Module")
        End Function

        <WorkItem(546844, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546844")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestKeyword2() As Task
            Await TestInRegularAndScriptAsync(
"Module M
    Sub main()
        Dim x = [|M.Class|]
    End Sub
    Dim [Class]
End Module",
"Module M
    Sub main()
        Dim x = [Class]
    End Sub
    Dim [Class]
End Module")
        End Function

        <WorkItem(546907, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546907")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDoNotSimplifyNullableInMemberAccessExpression() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System
Module Program
    Dim x = [|Nullable(Of Guid).op_Implicit|](Nothing)
End Module")
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMissingNullableSimplificationInsideCref() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System
''' <summary>
''' <see cref=""[|Nullable(Of T)|]""/>
''' </summary>
Class A
End Class")
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMissingNullableSimplificationInsideCref2() As Task
            Await TestMissingInRegularAndScriptAsync(
"''' <summary>
''' <see cref=""[|System.Nullable(Of T)|]""/>
''' </summary>
Class A
End Class")
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMissingNullableSimplificationInsideCref3() As Task
            Await TestMissingInRegularAndScriptAsync(
"''' <summary>
''' <see cref=""[|System.Nullable(Of T)|].Value""/>
''' </summary>
Class A
End Class")
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMissingNullableSimplificationInsideCref4() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System
''' <summary>
''' <see cref=""[|Nullable(Of Integer)|].Value""/>
''' </summary>
Class A
End Class")
        End Function

        <WorkItem(2196, "https://github.com/dotnet/roslyn/issues/2196")>
        <WorkItem(2197, "https://github.com/dotnet/roslyn/issues/2197")>
        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableSimplificationInsideCref() As Task
            ' NOTE: This will probably stop working if issues 2196 / 2197 related to VB compiler and semantic model are fixed.
            ' It is unclear whether Nullable(Of Integer) is legal in the below case. Currently the VB compiler allows this while
            ' C# doesn't allow similar case. If this Nullable(Of Integer) becomes illegal in VB in the below case then the simplification
            ' from Nullable(Of Integer) -> Integer will also stop working and the baseline for this test will have to be updated.
            Await TestInRegularAndScriptAsync(
"Imports System
''' <summary>
        ''' <see cref=""C(Of [|Nullable(Of Integer)|])""/>
        ''' </summary>
Class C(Of T)
End Class",
"Imports System
''' <summary>
''' <see cref=""C(Of Integer?)""/>
''' </summary>
Class C(Of T)
End Class")
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <WorkItem(2189, "https://github.com/dotnet/roslyn/issues/2189")>
        <WorkItem(2196, "https://github.com/dotnet/roslyn/issues/2196")>
        <WorkItem(2197, "https://github.com/dotnet/roslyn/issues/2197")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableSimplificationInsideCref2() As Task
            ' NOTE: This will probably stop working if issues 2196 / 2197 related to VB compiler and semantic model are fixed.
            ' It is unclear whether Nullable(Of Integer) is legal in the below case. Currently the VB compiler allows this while
            ' C# doesn't allow similar case. If this Nullable(Of Integer) becomes illegal in VB in the below case then the simplification
            ' from Nullable(Of Integer) -> Integer will also stop working and the baseline for this test will have to be updated.
            Await TestInRegularAndScriptAsync(
"Imports System
''' <summary>
''' <see cref=""C.M(Of [|Nullable(Of Integer)|])""/>
''' </summary>
Class C
    Sub M(Of T As Structure)()
    End Sub
End Class",
"Imports System
''' <summary>
''' <see cref=""C.M(Of Integer?)""/>
''' </summary>
Class C
    Sub M(Of T As Structure)()
    End Sub
End Class")
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableSimplificationInsideCref3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
''' <summary>
''' <see cref=""A.M([|Nullable(Of A)|])""/>
''' </summary>
Structure A
    Sub M(x As A?)
    End Sub
End Structure",
"Imports System
''' <summary>
''' <see cref=""A.M(A?)""/>
''' </summary>
Structure A
    Sub M(x As A?)
    End Sub
End Structure")
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableSimplificationInsideCref4() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
''' <summary>
''' <see cref=""A.M(List(Of [|Nullable(Of Integer)|]))""/>
''' </summary>
Structure A
    Sub M(x As List(Of Integer?))
    End Sub
End Structure",
"Imports System
Imports System.Collections.Generic
''' <summary>
''' <see cref=""A.M(List(Of Integer?))""/>
''' </summary>
Structure A
    Sub M(x As List(Of Integer?))
    End Sub
End Structure")
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableSimplificationInsideCref5() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
''' <summary>
''' <see cref=""A.M(Of T)(List(Of [|Nullable(Of T)|]))""/>
''' </summary>
Structure A
    Sub M(Of U As Structure)(x As List(Of U?))
    End Sub
End Structure",
"Imports System
Imports System.Collections.Generic
''' <summary>
''' <see cref=""A.M(Of T)(List(Of T?))""/>
''' </summary>
Structure A
    Sub M(Of U As Structure)(x As List(Of U?))
    End Sub
End Structure")
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableSimplificationInsideCref6() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
''' <summary>
''' <see cref=""A.M(Of U)(List(Of Nullable(Of Integer)), [|Nullable(Of U)|])""/>
''' </summary>
Structure A
    Sub M(Of U As Structure)(x As List(Of Integer?), y As U?)
    End Sub
End Structure",
"Imports System
Imports System.Collections.Generic
''' <summary>
''' <see cref=""A.M(Of U)(List(Of Nullable(Of Integer)), U?)""/>
''' </summary>
Structure A
    Sub M(Of U As Structure)(x As List(Of Integer?), y As U?)
    End Sub
End Structure")
        End Function

        <WorkItem(529930, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529930")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestReservedNameInAttribute1() As Task
            Await TestMissingInRegularAndScriptAsync(
"<[|Global.Assembly|]> ' Simplify 
Class Assembly
    Inherits Attribute
End Class")
        End Function

        <WorkItem(529930, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529930")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestReservedNameInAttribute2() As Task
            Await TestMissingInRegularAndScriptAsync(
"<[|Global.Assembly|]> ' Simplify 
Class Assembly
    Inherits Attribute
End Class")
        End Function

        <WorkItem(529930, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529930")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestReservedNameInAttribute3() As Task
            Await TestMissingInRegularAndScriptAsync(
"<[|Global.Module|]> ' Simplify 
Class Module
    Inherits Attribute
End Class")
        End Function

        <WorkItem(529930, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529930")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestReservedNameInAttribute4() As Task
            Await TestMissingInRegularAndScriptAsync(
"<[|Global.Module|]> ' Simplify 
Class Module
    Inherits Attribute
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestAliasedType() As Task
            Dim source =
"Class Program
    Sub Goo()
        Dim x As New [|Global.Program|]
    End Sub
End Class"
            Await TestAsync(
                source,
"Class Program
    Sub Goo()
        Dim x As New Program
    End Sub
End Class", parseOptions:=Nothing, index:=0)

            Await TestMissingAsync(source, New TestParameters(GetScriptOptions()))
        End Function

        <WorkItem(674789, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674789")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestCheckForAssemblyNameInFullWidthIdentifier() As Task
            Dim source =
        <Code>
Imports System

Namespace N
    &lt;[|N.ＡＳＳＥＭＢＬＹ|]&gt;
    Class ＡＳＳＥＭＢＬＹ
        Inherits Attribute
    End Class
End Namespace
</Code>

            Dim expected =
        <Code>
Imports System

Namespace N
    &lt;[ＡＳＳＥＭＢＬＹ]&gt;
    Class ＡＳＳＥＭＢＬＹ
        Inherits Attribute
    End Class
End Namespace
</Code>
            Await TestInRegularAndScriptAsync(source.Value, expected.Value)
        End Function

        <WorkItem(568043, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/568043")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDontSimplifyNamesWhenThereAreParseErrors() As Task
            Dim source =
        <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Sub Main(args() As String)
        Console.[||]
    End Sub
End Module
</Code>

            Await TestMissingInRegularAndScriptAsync(source.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestShowModuleNameAsUnnecessaryMemberAccess() As Task
            Dim source =
        <Code>
Imports System

Namespace goo
    Module Program
        Sub Main(args As String())
        End Sub
    End Module
End Namespace

Namespace bar
    Module b
        Sub m()
             [|goo.Program.Main|](Nothing)
        End Sub
    End Module
End Namespace
</Code>

            Dim expected =
            <Code>
Imports System

Namespace goo
    Module Program
        Sub Main(args As String())
        End Sub
    End Module
End Namespace

Namespace bar
    Module b
        Sub m()
            goo.Main(Nothing)
        End Sub
    End Module
End Namespace
</Code>
            Await TestInRegularAndScriptAsync(source.Value, expected.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestShowModuleNameAsUnnecessaryQualifiedName() As Task
            Dim source =
        <Code>
Imports System

Namespace goo
    Module Program
        Sub Main(args As String())
        End Sub
        Class C1
        End Class
    End Module
End Namespace

Namespace bar
    Module b
        Sub m()
             dim x as [|goo.Program.C1|]
        End Sub
    End Module
End Namespace
</Code>

            Dim expected =
        <Code>
Imports System

Namespace goo
    Module Program
        Sub Main(args As String())
        End Sub
        Class C1
        End Class
    End Module
End Namespace

Namespace bar
    Module b
        Sub m()
             dim x as goo.C1
        End Sub
    End Module
End Namespace
</Code>

            Await TestInRegularAndScriptAsync(source.Value, expected.Value)
        End Function

        <WorkItem(608200, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608200")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestBugfix_608200() As Task
            Dim source =
        <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

    End Sub
End Module

Module M
    Dim e = GetType([|System.Collections.Generic.List(Of ).Enumerator|])
End Module
</Code>

            Dim expected =
        <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

    End Sub
End Module

Module M
    Dim e = GetType(List(Of ).Enumerator)
End Module
</Code>

            Await TestInRegularAndScriptAsync(source.Value, expected.Value)
        End Function

        <WorkItem(578686, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578686")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDontUseAlias() As Task
            Dim source =
        <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Imports Goo = A.B
Imports Bar = C.D

Module Program
    Sub Main(args As String())
        Dim local1 = New A.B()
        Dim local2 = New C.D()

        Dim local3 = New List(Of NoAlias.Test)

        Dim local As NoAlias.Test
        For Each local In local3
            Dim x = [|local.prop|]
        Next
    End Sub
End Module

Namespace A
    Class B
    End Class
End Namespace

Namespace C
    Class D
    End Class
End Namespace

Namespace NoAlias
    Class Test
        Public Property prop As A.B
    End Class
End Namespace
</Code>

            Await TestMissingInRegularAndScriptAsync(source.Value)
        End Function

        <WorkItem(547246, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547246")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestCreateCodeIssueWithProperIssueSpan() As Task
            Dim source =
        <Code>
Imports Goo = System.Console

Module Program
    Sub Main(args As String())
        [|System.Console|].Read()
    End Sub
End Module
</Code>

            Dim expected =
            <Code>
Imports Goo = System.Console

Module Program
    Sub Main(args As String())
        Goo.Read()
    End Sub
End Module
</Code>

            Await TestInRegularAndScriptAsync(source.Value, expected.Value)

            Using workspace = TestWorkspace.CreateVisualBasic(source.Value)
                Dim diagnosticAndFixes = Await GetDiagnosticAndFixesAsync(workspace, New TestParameters())
                Dim span = diagnosticAndFixes.Item1.First().Location.SourceSpan
                Assert.NotEqual(span.Start, 0)
                Assert.NotEqual(span.End, 0)
            End Using
        End Function

        <WorkItem(629572, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629572")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDoNotIncludeAliasNameIfLastTargetNameIsTheSame_1() As Task
            Dim source =
        <Code>
Imports C = A.B.C

Module Program
    Sub Main(args As String())
        dim x = new [|A.B.C|]()
    End Sub
End Module

Namespace A
    Namespace B
        Class C
        End Class
    End Namespace
End Namespace
</Code>

            Dim expected =
            <Code>
Imports C = A.B.C

Module Program
    Sub Main(args As String())
        dim x = new C()
    End Sub
End Module

Namespace A
    Namespace B
        Class C
        End Class
    End Namespace
End Namespace
</Code>

            Await TestInRegularAndScriptAsync(source.Value, expected.Value)

            Using workspace = TestWorkspace.CreateVisualBasic(source.Value)
                Dim diagnosticAndFixes = Await GetDiagnosticAndFixesAsync(workspace, New TestParameters())
                Dim span = diagnosticAndFixes.Item1.First().Location.SourceSpan
                Assert.Equal(span.Start, expected.Value.ToString.Replace(vbLf, vbCrLf).IndexOf("new C", StringComparison.Ordinal) + 4)
                Assert.Equal(span.Length, "A.B".Length)
            End Using
        End Function

        <WorkItem(629572, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629572")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDoNotIncludeAliasNameIfLastTargetNameIsTheSame_2() As Task
            Dim source =
        <Code>
Imports Console = System.Console

Module Program
    Sub Main(args As String())
        [|System.Console|].WriteLine("goo")
    End Sub
End Module
</Code>

            Dim expected =
            <Code>
Imports Console = System.Console

Module Program
    Sub Main(args As String())
        Console.WriteLine("goo")
    End Sub
End Module
</Code>

            Await TestInRegularAndScriptAsync(source.Value, expected.Value)

            Using workspace = TestWorkspace.CreateVisualBasic(source.Value)
                Dim diagnosticAndFixes = Await GetDiagnosticAndFixesAsync(workspace, New TestParameters())
                Dim span = diagnosticAndFixes.Item1.First().Location.SourceSpan
                Assert.Equal(span.Start, expected.Value.ToString.Replace(vbLf, vbCrLf).IndexOf("Console.WriteLine(""goo"")", StringComparison.Ordinal))
                Assert.Equal(span.Length, "System".Length)
            End Using
        End Function

        <WorkItem(686306, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/686306")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDontSimplifyNameSyntaxToTypeSyntaxInVBCref() As Task
            Dim source =
        <Code>
Imports System
''' &lt;see cref="[|Object|]"/>
Module Program
End Module
</Code>

            Await TestMissingInRegularAndScriptAsync(source.Value)
        End Function

        <WorkItem(721817, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/721817")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDontSimplifyNameSyntaxToPredefinedTypeSyntaxInVBCref() As Task
            Dim source =
        <Code>
Public Class Test
    '''&lt;summary&gt;
    ''' &lt;see cref="[|Test.ReferenceEquals|](Object, Object)"/&gt;   
    ''' &lt;/Code&gt;
        Public Async Function TestGoo() As Task

        End Sub

    End Class


</Code>

            Await TestMissingInRegularAndScriptAsync(source.Value)
        End Function

        <WorkItem(721694, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/721694")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestEnableReducersInsideVBCref() As Task
            Dim source =
        <Code>
Public Class Test_Dev11
    '''&lt;summary&gt;
    ''' &lt;see cref="[|Global.Microsoft|].VisualBasic.Left"/&gt;
    ''' &lt;/Code&gt;
        Public Async Function Testtestscenarios() As Task
        End Sub
    End Class
</Code>

            Dim expected =
        <Code>
Public Class Test_Dev11
    '''&lt;summary&gt;
    ''' &lt;see cref="Microsoft.VisualBasic.Left"/&gt;
    ''' &lt;/Code&gt;
    Public Async Function Testtestscenarios() As Task
        End Sub
    End Class
</Code>
            Await TestInRegularAndScriptAsync(source.Value, expected.Value)
        End Function

        <WorkItem(736377, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/736377")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDontSimplifyTypeNameBrokenCode() As Task
            Dim source =
        <Code>
            <![CDATA[
Imports System.Collections.Generic

Class Program
    Public Shared GetA([|System.Diagnostics|])
    Public Shared Function GetAllFilesInSolution() As ISet(Of String)
		Return Nothing
    End Function
End Class
]]>
        </Code>

            Await TestMissingInRegularAndScriptAsync(source.Value)
        End Function

        <WorkItem(860565, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/860565")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyGenericTypeName_Bug860565() As Task
            Dim source =
        <Code>
            <![CDATA[
Interface A(Of T)
    Interface B(Of S)
        Inherits A(Of B(Of B(Of S)))
        Interface C(Of U)
            Inherits B(Of [|C(Of C(Of U)).B(Of T)|])
        End Interface
    End Interface
End Interface
]]>
        </Code>

            Await TestMissingInRegularAndScriptAsync(source.Value)
        End Function

        <WorkItem(813385, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/813385")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDontSimplifyAliases() As Task
            Dim source =
        <Code>
            <![CDATA[
Imports Goo = System.Int32

Class P
    Dim F As [|Goo|]
End Class
]]>
        </Code>

            Await TestMissingInRegularAndScriptAsync(source.Value)
        End Function

        <WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInLocalDeclarationDefaultValue_1() As Task
            Dim source =
        <Code>
Module Program
    Sub Main(args As String())
        Dim x As [|System.Int32|]
    End Sub
End Module
</Code>

            Dim expected =
        <Code>
Module Program
    Sub Main(args As String())
        Dim x As Integer
    End Sub
End Module
</Code>

            Await TestInRegularAndScriptAsync(source.Value, expected.Value, options:=PreferIntrinsicPredefinedTypeInDeclaration())
        End Function

        <WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInLocalDeclarationDefaultValue_2() As Task
            Dim source =
        <Code>
Module Program
    Sub Main(args As String())
        Dim s = New List(Of [|System.Int32|])()
    End Sub
End Module
</Code>

            Dim expected =
        <Code>
Module Program
    Sub Main(args As String())
        Dim s = New List(Of Integer)()
    End Sub
End Module
</Code>

            Await TestInRegularAndScriptAsync(source.Value, expected.Value, options:=PreferIntrinsicPredefinedTypeInDeclaration())
        End Function

        <WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")>
        <WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref1() As Task
            Dim source =
        <Code>
Imports System
''' &lt;see cref="[|Int32|]"/>
Module Program
End Module
</Code>
            Dim expected =
        <Code>
Imports System
''' &lt;see cref="Integer"/>
Module Program
End Module
</Code>

            Await TestInRegularAndScriptAsync(source.Value, expected.Value, options:=PreferIntrinsicTypeInMemberAccess())
        End Function

        <WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")>
        <WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref2() As Task
            Dim source =
        <Code>
''' &lt;see cref="[|System.Int32|]"/>
Module Program
End Module
</Code>
            Dim expected =
        <Code>
''' &lt;see cref="Integer"/>
Module Program
End Module
</Code>

            Await TestInRegularAndScriptAsync(source.Value, expected.Value, options:=PreferIntrinsicTypeInMemberAccess())
        End Function

        <WorkItem(1012713, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1012713")>
        <WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref3() As Task
            Dim source =
        <Code>
''' &lt;see cref="[|System.Int32|].MaxValue"/>
Module Program
End Module
</Code>

            Await TestMissingInRegularAndScriptAsync(source.Value,
                                                     New TestParameters(options:=PreferIntrinsicPredefinedTypeEverywhere()))
        End Function

        <WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInLocalDeclarationNonDefaultValue_1() As Task
            Dim source =
        <Code>
Class Program
    Private x As [|System.Int32|]
    Sub Main(args As System.Int32)
        Dim a As System.Int32 = 9
    End Sub
End Class
</Code>
            Await TestMissingInRegularAndScriptAsync(source.Value, New TestParameters(options:=[Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, False, NotificationOption.Error)))
        End Function

        <WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInLocalDeclarationNonDefaultValue_2() As Task
            Dim source =
        <Code>
Class Program
    Private x As System.Int32
    Sub Main(args As [|System.Int32|])
        Dim a As System.Int32 = 9
    End Sub
End Class
</Code>
            Await TestMissingInRegularAndScriptAsync(source.Value, New TestParameters(options:=[Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, False, NotificationOption.Error)))
        End Function

        <WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInLocalDeclarationNonDefaultValue_3() As Task
            Dim source =
        <Code>
Class Program
    Private x As System.Int32
    Sub Main(args As System.Int32)
        Dim a As [|System.Int32|] = 9
    End Sub
End Class
</Code>
            Await TestMissingInRegularAndScriptAsync(source.Value, New TestParameters(options:=[Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, False, NotificationOption.Error)))
        End Function

        <WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInMemberAccess_Default_1() As Task
            Dim source =
        <Code>
Imports System

Module Program
    Sub Main(args As String())
        Dim s = [|Int32|].MaxValue
    End Sub
End Module
</Code>
            Dim expected =
        <Code>
Imports System

Module Program
    Sub Main(args As String())
        Dim s = Integer.MaxValue
    End Sub
End Module
</Code>
            Await TestInRegularAndScriptAsync(source.Value, expected.Value, options:=PreferIntrinsicTypeInMemberAccess())
        End Function

        <WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInMemberAccess_Default_2() As Task
            Dim source =
        <Code>
Module Program
    Sub Main(args As String())
        Dim s = [|System.Int32|].MaxValue
    End Sub
End Module
</Code>
            Dim expected =
        <Code>
Module Program
    Sub Main(args As String())
        Dim s = Integer.MaxValue
    End Sub
End Module
</Code>
            Await TestInRegularAndScriptAsync(source.Value, expected.Value, options:=PreferIntrinsicTypeInMemberAccess())
        End Function

        <WorkItem(956667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/956667")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInMemberAccess_Default_3() As Task
            Dim source =
        <Code>
Imports System

Class Program1
    Sub Main(args As String())
        Dim s = [|Program2.memb|].ToString()
    End Sub
End Class

Class Program2
    Public Shared Property memb As Integer
End Class

</Code>
            Await TestMissingInRegularAndScriptAsync(source.Value)
        End Function

        <WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInMemberAccess_NonDefault_1() As Task
            Dim source =
        <Code>
Module Program
    Sub Main(args As String())
        Dim s = [|System.Int32|].MaxValue
    End Sub
End Module
</Code>
            Await TestMissingInRegularAndScriptAsync(source.Value, New TestParameters(options:=[Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, False, NotificationOption.Error)))
        End Function

        <WorkItem(942568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942568")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInMemberAccess_NonDefault_2() As Task
            Dim source =
        <Code>
Imports System
Module Program
    Sub Main(args As String())
        Dim s = [|Int32|].MaxValue
    End Sub
End Module
</Code>
            Await TestMissingInRegularAndScriptAsync(source.Value, New TestParameters(options:=[Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, False, NotificationOption.Error)))
        End Function

        <WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref_NonDefault_1() As Task
            Dim source =
        <Code>
''' &lt;see cref="[|System.Int32|]"/>
Module Program
End Module
</Code>

            Await TestMissingInRegularAndScriptAsync(source.Value, New TestParameters(options:=[Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, False, NotificationOption.Error)))
        End Function

        <WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref_NonDefault_2() As Task
            Dim source =
        <Code>
''' &lt;see cref="[|System.Int32|]"/>
Module Program
End Module
</Code>

            Dim expected =
        <Code>
''' &lt;see cref="Integer"/>
Module Program
End Module
</Code>

            Await TestInRegularAndScriptAsync(source.Value, expected.Value, options:=PreferIntrinsicTypeInMemberAccess())
        End Function

        <WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref_NonDefault_3() As Task
            Dim source =
        <Code>
''' &lt;see cref="System.Collections.Generic.List(Of T).CopyTo(Integer, T(), Integer, [|System.Int32|])"/>
Module Program
End Module
</Code>

            Await TestMissingInRegularAndScriptAsync(source.Value, New TestParameters(options:=[Option](CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, False, NotificationOption.Error)))
        End Function

        <WorkItem(954536, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954536")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref_NonDefault_4() As Task
            Dim source =
        <Code>
''' &lt;see cref="System.Collections.Generic.List(Of T).CopyTo(Integer, T(), Integer, [|System.Int32|])"/>
Module Program
End Module
</Code>

            Dim expected =
        <Code>
''' &lt;see cref="System.Collections.Generic.List(Of T).CopyTo(Integer, T(), Integer, Integer)"/>
Module Program
End Module
</Code>

            Await TestInRegularAndScriptAsync(source.Value, expected.Value, options:=PreferIntrinsicTypeInMemberAccess())
        End Function

        <WorkItem(965208, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/965208")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyDiagnosticId() As Task
            Dim source =
        <Code>
Imports System
Module Program
    Sub Main(args As String())
        [|System.Console.WriteLine|]("")
    End Sub
End Module
</Code>

            Dim parameters As New TestParameters()
            Using workspace = CreateWorkspaceFromFile(source.ToString(), parameters)
                Dim diagnostics = (Await GetDiagnosticsAsync(workspace, parameters)).Where(Function(d) d.Id = IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId)
                Assert.Equal(1, diagnostics.Count)
            End Using

            source =
        <Code>
Imports System
Module Program
    Sub Main(args As String())
        Dim a As [|System.Int32|]
    End Sub
End Module
</Code>

            Dim parameters2 As New TestParameters()
            Using workspace = CreateWorkspaceFromFile(source.ToString(), parameters2)
                workspace.ApplyOptions(PreferIntrinsicPredefinedTypeEverywhere())
                Dim diagnostics = (Await GetDiagnosticsAsync(workspace, parameters2)).Where(Function(d) d.Id = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId)
                Assert.Equal(1, diagnostics.Count)
            End Using
        End Function

        <WorkItem(995168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf1() As Task
            Await TestMissingInRegularAndScriptAsync("Imports System
Module Program
    Sub Main()
        Dim x = NameOf([|Int32|])
    End Sub
End Module")
        End Function

        <WorkItem(995168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf2() As Task
            Await TestMissingInRegularAndScriptAsync("
Module Program
    Sub Main()
        Dim x = NameOf([|System.Int32|])
    End Sub
End Module")
        End Function

        <WorkItem(995168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf3() As Task
            Await TestMissingInRegularAndScriptAsync("Imports System
Module Program
    Sub Main()
        Dim x = NameOf([|Int32|].MaxValue)
    End Sub
End Module")
        End Function

        <WorkItem(995168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995168")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyTypeNameInsideNameOf() As Task
            Await TestInRegularAndScriptAsync("Imports System
Module Program
    Sub Main()
        Dim x = NameOf([|System.Int32|])
    End Sub
End Module",
"Imports System
Module Program
    Sub Main()
        Dim x = NameOf(Int32)
    End Sub
End Module")
        End Function

        <WorkItem(19498, "https://github.com/dotnet/roslyn/issues/19498")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMyClassShouldNotBeRemoved() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class SomeType
    Overridable Sub Test()
    End Sub
    Overridable Sub Test2()
        [|MyClass|].Test()
    End Sub
End Class")
        End Function

        <WorkItem(19498, "https://github.com/dotnet/roslyn/issues/19498")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMyClassShouldBeRemoved() As Task
            Await TestInRegularAndScriptAsync(
"Class SomeType
    Sub Test()
    End Sub
    Sub Test2()
        [|MyClass|].Test()
    End Sub
End Class",
"Class SomeType
    Sub Test()
    End Sub
    Sub Test2()
        Test()
    End Sub
End Class")
        End Function

        <WorkItem(15996, "https://github.com/dotnet/roslyn/issues/15996")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMemberOfBuiltInType1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Module Module1
    Sub Main()
        Dim var As [|UInt32|] = UInt32.MinValue
    End Sub
End Module",
"Imports System
Module Module1
    Sub Main()
        Dim var As UInteger = UInt32.MinValue
    End Sub
End Module",
                options:=PreferIntrinsicPredefinedTypeInDeclaration())
        End Function

        <WorkItem(15996, "https://github.com/dotnet/roslyn/issues/15996")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMemberOfBuiltInType2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Module Module1
    Sub Main()
        Dim var As UInt32 = [|UInt32|].MinValue
    End Sub
End Module",
"Imports System
Module Module1
    Sub Main()
        Dim var As UInt32 = UInteger.MinValue
    End Sub
End Module",
                options:=PreferIntrinsicTypeInMemberAccess())
        End Function

        <WorkItem(15996, "https://github.com/dotnet/roslyn/issues/15996")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMemberOfBuiltInType3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Module Module1
    Sub Main()
        [|UInt32|].Parse(""Goo"")
    End Sub
End Module",
"Imports System
Module Module1
    Sub Main()
        UInteger.Parse(""Goo"")
    End Sub
End Module",
                options:=PreferIntrinsicTypeInMemberAccess())
        End Function
    End Class
End Namespace
