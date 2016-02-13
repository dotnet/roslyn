' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
Imports System.Threading.Tasks

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ObjectBrowser.CSharp
    Public Class ObjectBrowserTests
        Inherits AbstractObjectBrowserTests

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

        Friend Overrides Function CreateLibraryManager(serviceProvider As IServiceProvider) As AbstractObjectBrowserLibraryManager
            Return New ObjectBrowserLibraryManager(serviceProvider)
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestSimpleContent_NamespaceTypeAndMember() As Task
            Dim code =
<Code>
namespace N
{
    class C
    {
        void M()
        {
        }
    }
}
</Code>


            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()

                Dim list = library.GetProjectList()
                list.VerifyNames("CSharpAssembly1")

                list = list.GetNamespaceList(0)
                list.VerifyNames("N")

                list = list.GetTypeList(0)
                list.VerifyNames("C")

                list = list.GetMemberList(0)
                list.VerifyNames(AddressOf IsImmediateMember, "M()")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestSimpleContent_NoNamespaceWithoutType() As Task
            Dim code =
<Code>
namespace N
{
}
</Code>


            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)

                list.VerifyEmpty()
            End Using
        End Function

        <WorkItem(932387, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932387")>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestContent_InheritedMembers1() As Task
            Dim code =
<Code>
class A
{
    protected virtual void Foo()
    {
    }
}

class B : A
{
    protected override void Foo()
    {
    }

    public virtual void Bar()
    {
    }
}

class C : B
{
    protected override void Foo()
    {
    }

    public override void Bar()
    {
    }
}
</Code>


            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyNames(
                    "Foo()",
                    "ToString()",
                    "Equals(object)",
                    "Equals(object, object)",
                    "ReferenceEquals(object, object)",
                    "GetHashCode()",
                    "GetType()",
                    "MemberwiseClone()")
            End Using
        End Function

        <WorkItem(932387, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932387")>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestContent_InheritedMembers2() As Task
            Dim code =
<Code>
class A
{
    protected virtual void Foo()
    {
    }
}

class B : A
{
    protected override void Foo()
    {
    }

    public virtual void Bar()
    {
    }
}

class C : B
{
    protected override void Foo()
    {
    }

    public override void Bar()
    {
    }
}
</Code>


            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(1)

                list.VerifyNames(
                    "Foo()",
                    "Bar()",
                    "ToString()",
                    "Equals(object)",
                    "Equals(object, object)",
                    "ReferenceEquals(object, object)",
                    "GetHashCode()",
                    "GetType()",
                    "MemberwiseClone()")
            End Using
        End Function

        <WorkItem(932387, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932387")>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestContent_InheritedMembers3() As Task
            Dim code =
<Code>
class A
{
    protected virtual void Foo()
    {
    }
}

class B : A
{
    protected override void Foo()
    {
    }

    public virtual void Bar()
    {
    }
}

class C : B
{
    protected override void Foo()
    {
    }

    public override void Bar()
    {
    }
}
</Code>


            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(2)

                list.VerifyNames(
                    "Foo()",
                    "Bar()",
                    "ToString()",
                    "Equals(object)",
                    "Equals(object, object)",
                    "ReferenceEquals(object, object)",
                    "GetHashCode()",
                    "GetType()",
                    "MemberwiseClone()")
            End Using
        End Function

        <WorkItem(932387, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932387")>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestContent_HelpKeyword_Ctor() As Task
            Dim code =
<Code>
namespace N
{
    class C
    {
        public C() { }
    }
}
</Code>


            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyHelpKeywords("N.C.#ctor")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Project() As Task
            Dim code =
<Code>
namespace N { }
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()

                list.VerifyDescriptions($"{ServicesVSResources.Library_Project}CSharpAssembly1")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Namespace() As Task
            Dim code =
<Code>
namespace N
{
    class C { }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)

                list.VerifyDescriptions(
"namespace N" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Class1() As Task
            Dim code =
<Code>
abstract class B { }
sealed class C : B { }
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal abstract class B" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}",
"internal sealed class C : B" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Class2() As Task
            Dim code =
<Code>
static class C { }
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal static class C" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_ClassWithConstraints() As Task
            Dim code =
<Code>
using System.Collections.Generic;

class Z&lt;T,U,V&gt; : Dictionary&lt;U,V&gt;
    where T : struct
    where U : V
    where V : List&lt;T&gt;
</Code>


            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()

                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal class Z<T, U, V> : System.Collections.Generic.Dictionary<U, V>" & vbCrLf &
vbTab & "where T : struct" & vbCrLf &
vbTab & "where U : V" & vbCrLf &
vbTab & "where V : System.Collections.Generic.List<T>" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Interfaces() As Task
            Dim code =
<Code>
interface I1 { }
interface I2 : I1 { }
interface I3 : I2, I1 { }
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal interface I1" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}",
"internal interface I2" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}",
"internal interface I3" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Struct1() As Task
            Dim code =
<Code>
struct S { }
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal struct S" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Method() As Task
            Dim code =
<Code>
class C
{
    void M()
    {
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M()" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <WorkItem(939739, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939739")>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_MethodInInterface() As Task
            Dim code =
<Code>
interface I
{
    void M()
    {
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"void M()" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "I")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_ExtensionMethod() As Task
            Dim code =
<Code>
static class C
{
    public static void M(this C c)
    {
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static void M(this C c)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_MethodWithParameters() As Task
            Dim code =
<Code>
using System.Collections.Generic
class C
{
    bool M(int x, ref string y, out List&lt;int&gt; z)
    {
        z = null;
        return true;
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private bool M(int x, ref string y, out System.Collections.Generic.List<int> z)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_MethodWithOptionalParameter1() As Task
            Dim code =
<Code>
class C
{
    void M(int x = 42)
    {
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M([int x = 42])" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_MethodWithOptionalParameter2() As Task
            Dim code =
<Code>
using System;
class C
{
    void M(double x = Math.PI)
    {
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code)),
                    testCulture As New CultureContext("en-US")
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M([double x = 3.14159265358979])" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_MethodWithOptionalParameter3() As Task
            Dim code =
<Code>
class C
{
    void M(double? x = null)
    {
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M([double? x = null])" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_MethodWithOptionalParameter4() As Task
            Dim code =
<Code>
class C
{
    void M(double? x = 42)
    {
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M([double? x = 42])" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_MethodWithOptionalParameter5() As Task
            Dim code =
<Code>
class C
{
    void M(C c = default(C))
    {
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M([C c = null])" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_UnsafeMethod() As Task
            Dim code =
<Code>
unsafe class UnsafeC
{
    unsafe int* M()
    {
        return null;
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private unsafe int* M()" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "UnsafeC")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_MethodWithConstraints() As Task
            Dim code =
<Code>
using System.Collections.Generic;
class C
{
    T M&lt;T, U, V&gt;()
        where T : class, new()
        where U : V
        where V : List&lt;T&gt;
    {
        return null;
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private T M<T, U, V>()" & vbCrLf &
vbTab & "where T : class, new()" & vbCrLf &
vbTab & "where U : V" & vbCrLf &
vbTab & "where V : System.Collections.Generic.List<T>" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_ReadOnlyField() As Task
            Dim code =
<Code>
class C
{
    internal readonly int x;
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"internal readonly int x" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_ConstField() As Task
            Dim code =
<Code>
class C
{
    protected internal const int x = 42;
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"protected internal const int x" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Property1() As Task
            Dim code =
<Code>
class C
{
    public int P
    {
        get { return 0; }
        set { }
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public int P { get; set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Property2() As Task
            Dim code =
<Code>
class C
{
    public int P
    {
        get { return 0; }
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public int P { get; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Property3() As Task
            Dim code =
<Code>
class C
{
    public int P
    {
        set { }
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public int P { set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Property4() As Task
            Dim code =
<Code>
class C
{
    public int P { get; set; }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public int P { get; set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Property5() As Task
            Dim code =
<Code>
class C
{
    public int P { get; private set; }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public int P { get; private set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Property6() As Task
            Dim code =
<Code>
class C
{
    internal int P { get; private set; }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"internal int P { get; private set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Indexer1() As Task
            Dim code =
<Code>
class C
{
    int this[int index]
    {
        get { return 42; }
        set { }
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private int this[int index] { get; set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Indexer2() As Task
            Dim code =
<Code>
class C
{
    private int this[int index]
    {
        get { return 42; }
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private int this[int index] { get; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Indexer3() As Task
            Dim code =
<Code>
class C
{
    protected int this[int index]
    {
        set { }
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"protected int this[int index] { set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Enum1() As Task
            Dim code =
<Code>
enum E
{
    A
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal enum E" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Enum2() As Task
            Dim code =
<Code>
enum E : int
{
    A
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal enum E" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Enum3() As Task
            Dim code =
<Code>
enum E : byte
{
    A
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal enum E : byte" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_EnumMember() As Task
            Dim code =
<Code>
enum E
{
    A
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"A" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "E")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Event1() As Task
            Dim code =
<Code>
using System;
class C
{
    public event EventHandler E;
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public event System.EventHandler E" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Event2() As Task
            Dim code =
<Code>
using System;
class C
{
    public event EventHandler E
    {
        add { }
        remove { }
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public event System.EventHandler E" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_XmlDocComment() As Task
            Dim code =
<Code>
    <![CDATA[
using System.Collections.Generic;
class C
{
    /// <summary>The M method.</summary>
    /// <returns>Returns a <typeparamref name="TResult"/>.</returns>
    public TResult M<T, TResult>(T x, T y)
        where TResult : class
    {
        return null;
    }
}
]]>
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public TResult M<T, TResult>(T x, T y)" & vbCrLf &
vbTab & "where TResult : class" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}" & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Library_Summary & vbCrLf &
"The M method." & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Library_Returns & vbCrLf &
"Returns a TResult.")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Operator_Add() As Task
            Dim code =
<Code>
class C
{
    public static int operator +(C c, int i)
    {
        return 42;
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static int operator +(C c, int i)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Operator_Subtract() As Task
            Dim code =
<Code>
class C
{
    public static int operator -(C c, int i)
    {
        return 42;
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static int operator -(C c, int i)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Operator_Multiply() As Task
            Dim code =
<Code>
class C
{
    public static int operator *(C c, int i)
    {
        return 42;
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static int operator *(C c, int i)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Operator_Divide() As Task
            Dim code =
<Code>
class C
{
    public static int operator /(C c, int i)
    {
        return 42;
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static int operator /(C c, int i)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Operator_Implicit() As Task
            Dim code =
<Code>
class C
{
    public static implicit operator bool(C c)
    {
        return true;
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static implicit operator bool(C c)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_Operator_Explicit() As Task
            Dim code =
<Code>
class C
{
    public static explicit operator bool(C c)
    {
        return true;
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static explicit operator bool(C c)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestDescription_ExternMethod() As Task
            Dim code =
<Code>
using System.Runtime.InteropServices;
class C
{
    [DllImport("User32.dll", CharSet=CharSet.Unicode)] 
    public static extern int MessageBox(System.IntPtr h, string m, string c, int type);
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static extern int MessageBox(System.IntPtr h, string m, string c, int type)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Function

        <WorkItem(942021, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942021")>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestNavInfo_Class() As Task
            Dim code =
<Code>
namespace EditorFunctionalityHelper
{
    public class EditorFunctionalityHelper
    {
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)

                list.VerifyCanonicalNodes(0,
                    ProjectNode("CSharpAssembly1"),
                    NamespaceNode("EditorFunctionalityHelper"),
                    TypeNode("EditorFunctionalityHelper"))
            End Using
        End Function

        <WorkItem(942021, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942021")>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Async Function TestNavInfo_NestedEnum() As Task
            Dim code =
<Code>
namespace EditorFunctionalityHelper
{
    public class EditorFunctionalityHelper
    {
        public enum Mapping
        {
            EnumOne,
            EnumTwo,
            EnumThree
        }
    }
}
</Code>

            Using state = Await CreateLibraryManagerAsync(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)

                list.VerifyCanonicalNodes(1, ' Mapping
                    ProjectNode("CSharpAssembly1"),
                    NamespaceNode("EditorFunctionalityHelper"),
                    TypeNode("EditorFunctionalityHelper"),
                    TypeNode("Mapping"))
            End Using
        End Function

    End Class
End Namespace
