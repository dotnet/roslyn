' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser

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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub SimpleContent_NamespaceTypeAndMember()
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


            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub SimpleContent_NoNamespaceWithoutType()
            Dim code =
<Code>
namespace N
{
}
</Code>


            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)

                list.VerifyEmpty()
            End Using
        End Sub

        <WorkItem(932387)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Content_InheritedMembers1()
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


            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
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
        End Sub

        <WorkItem(932387)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Content_InheritedMembers2()
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


            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
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
        End Sub

        <WorkItem(932387)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Content_InheritedMembers3()
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


            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
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
        End Sub

        <WorkItem(932387)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Content_HelpKeyword_Ctor()
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


            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyHelpKeywords("N.C.#ctor")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Project()
            Dim code =
<Code>
namespace N { }
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()

                list.VerifyDescriptions($"{ServicesVSResources.Library_Project}CSharpAssembly1")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Namespace()
            Dim code =
<Code>
namespace N
{
    class C { }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)

                list.VerifyDescriptions(
"namespace N" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Class1()
            Dim code =
<Code>
abstract class B { }
sealed class C : B { }
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal abstract class B" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}",
"internal sealed class C : B" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Class2()
            Dim code =
<Code>
static class C { }
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal static class C" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_ClassWithConstraints()
            Dim code =
<Code>
using System.Collections.Generic;

class Z&lt;T,U,V&gt; : Dictionary&lt;U,V&gt;
    where T : struct
    where U : V
    where V : List&lt;T&gt;
</Code>


            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Interfaces()
            Dim code =
<Code>
interface I1 { }
interface I2 : I1 { }
interface I3 : I2, I1 { }
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Struct1()
            Dim code =
<Code>
struct S { }
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal struct S" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Method()
            Dim code =
<Code>
class C
{
    void M()
    {
    }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M()" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <WorkItem(939739)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_MethodInInterface()
            Dim code =
<Code>
interface I
{
    void M()
    {
    }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"void M()" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "I")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_ExtensionMethod()
            Dim code =
<Code>
static class C
{
    public static void M(this C c)
    {
    }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static void M(this C c)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_MethodWithParameters()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private bool M(int x, ref string y, out System.Collections.Generic.List<int> z)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_MethodWithOptionalParameter1()
            Dim code =
<Code>
class C
{
    void M(int x = 42)
    {
    }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M([int x = 42])" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_MethodWithOptionalParameter2()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code)),
                    testCulture As New CultureContext("en-US")
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M([double x = 3.14159265358979])" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_MethodWithOptionalParameter3()
            Dim code =
<Code>
class C
{
    void M(double? x = null)
    {
    }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M([double? x = null])" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_MethodWithOptionalParameter4()
            Dim code =
<Code>
class C
{
    void M(double? x = 42)
    {
    }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M([double? x = 42])" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_MethodWithOptionalParameter5()
            Dim code =
<Code>
class C
{
    void M(C c = default(C))
    {
    }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M([C c = null])" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_UnsafeMethod()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private unsafe int* M()" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "UnsafeC")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_MethodWithConstraints()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_ReadOnlyField()
            Dim code =
<Code>
class C
{
    internal readonly int x;
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"internal readonly int x" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_ConstField()
            Dim code =
<Code>
class C
{
    protected internal const int x = 42;
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"protected internal const int x" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Property1()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public int P { get; set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Property2()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public int P { get; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Property3()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public int P { set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Property4()
            Dim code =
<Code>
class C
{
    public int P { get; set; }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public int P { get; set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Property5()
            Dim code =
<Code>
class C
{
    public int P { get; private set; }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public int P { get; private set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Property6()
            Dim code =
<Code>
class C
{
    internal int P { get; private set; }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"internal int P { get; private set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Indexer1()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private int this[int index] { get; set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Indexer2()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private int this[int index] { get; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Indexer3()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"protected int this[int index] { set; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Enum1()
            Dim code =
<Code>
enum E
{
    A
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal enum E" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Enum2()
            Dim code =
<Code>
enum E : int
{
    A
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal enum E" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Enum3()
            Dim code =
<Code>
enum E : byte
{
    A
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)

                list.VerifyDescriptions(
"internal enum E : byte" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "CSharpAssembly1")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_EnumMember()
            Dim code =
<Code>
enum E
{
    A
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"A" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "E")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Event1()
            Dim code =
<Code>
using System;
class C
{
    public event EventHandler E;
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public event System.EventHandler E" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Event2()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public event System.EventHandler E" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_XmlDocComment()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
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
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Operator_Add()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static int operator +(C c, int i)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Operator_Subtract()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static int operator -(C c, int i)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Operator_Multiply()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static int operator *(C c, int i)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Operator_Divide()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static int operator /(C c, int i)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Operator_Implicit()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static implicit operator bool(C c)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_Operator_Explicit()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static explicit operator bool(C c)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub Description_ExternMethod()
            Dim code =
<Code>
using System.Runtime.InteropServices;
class C
{
    [DllImport("User32.dll", CharSet=CharSet.Unicode)] 
    public static extern int MessageBox(System.IntPtr h, string m, string c, int type);
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public static extern int MessageBox(System.IntPtr h, string m, string c, int type)" & vbCrLf &
$"    {String.Format(ServicesVSResources.Library_MemberOf, "C")}")
            End Using
        End Sub

        <WorkItem(942021)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub NavInfo_Class()
            Dim code =
<Code>
namespace EditorFunctionalityHelper
{
    public class EditorFunctionalityHelper
    {
    }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0)
                list = list.GetTypeList(0)

                list.VerifyCanonicalNodes(0,
                    ProjectNode("CSharpAssembly1"),
                    NamespaceNode("EditorFunctionalityHelper"),
                    TypeNode("EditorFunctionalityHelper"))
            End Using
        End Sub

        <WorkItem(942021)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
        Public Sub NavInfo_NestedEnum()
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

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
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
        End Sub

    End Class
End Namespace
