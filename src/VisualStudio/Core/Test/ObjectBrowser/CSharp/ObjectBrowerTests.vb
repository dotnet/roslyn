' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ObjectBrowser.CSharp
    <Trait(Traits.Feature, Traits.Features.ObjectBrowser)>
    Public Class ObjectBrowserTests
        Inherits AbstractObjectBrowserTests

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

        Friend Overrides Function CreateLibraryManager(serviceProvider As IServiceProvider, componentModel As IComponentModel, workspace As VisualStudioWorkspace) As AbstractObjectBrowserLibraryManager
            Return New ObjectBrowserLibraryManager(serviceProvider, componentModel, workspace)
        End Function

        <WpfFact>
        Public Sub TestSimpleContent_NamespaceTypeAndMember()
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

        <WpfFact>
        Public Sub TestSimpleContent_NoNamespaceWithoutType()
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

        <WpfFact>
        Public Sub TestSimpleContent_InlcudePrivateNestedTypeMembersInSourceCode()
            Dim code =
<Code>
namespace N
{
    class C
    {
        private enum PrivateEnumTest { }
        private class PrivateClassTest { }
        private struct PrivateStructTest { }
    }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0).GetTypeList(0)
                list.VerifyNames("C", "C.PrivateStructTest", "C.PrivateClassTest", "C.PrivateEnumTest")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestSimpleContent_InlcudePrivateDoubleNestedTypeMembersInSourceCode()
            Dim code =
<Code>
namespace N
{
    internal class C
    {
        private class NestedClass
        {
            private class NestedNestedClass { }
        }
    }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetNamespaceList(0).GetTypeList(0)
                list.VerifyNames("C", "C.NestedClass", "C.NestedClass.NestedNestedClass")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestSimpleContent_InlcudeNoPrivateNestedTypeOfMetaData()
            Dim workspace =
                <Workspace>
                    <Project Language="C#" CommonReferences="false" Features="noRefSafetyRulesAttribute">
                        <Document></Document>
                        <MetadataReferenceFromSource Language="C#" CommonReferences="true" Features="noRefSafetyRulesAttribute">
                            <Document>namespace N
{
    public class C
    {
        public enum PublicEnumTest { }
        private class PrivateClassTest { }
        private struct PrivateStructTest { }
    }
}</Document>
                        </MetadataReferenceFromSource>
                    </Project>
                </Workspace>

            Using state = CreateLibraryManager(workspace)
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList().GetReferenceList(0).GetNamespaceList(0).GetTypeList(0)
                list.VerifyNames("C", "C.PublicEnumTest")
            End Using
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932387")>
        Public Sub TestContent_InheritedMembers1()
            Dim code =
<Code>
class A
{
    protected virtual void Goo()
    {
    }
}

class B : A
{
    protected override void Goo()
    {
    }

    public virtual void Bar()
    {
    }
}

class C : B
{
    protected override void Goo()
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
                    "Goo()",
                    "ToString()",
                    "Equals(object)",
                    "Equals(object, object)",
                    "ReferenceEquals(object, object)",
                    "GetHashCode()",
                    "GetType()",
                    "MemberwiseClone()")
            End Using
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932387")>
        Public Sub TestContent_InheritedMembers2()
            Dim code =
<Code>
class A
{
    protected virtual void Goo()
    {
    }
}

class B : A
{
    protected override void Goo()
    {
    }

    public virtual void Bar()
    {
    }
}

class C : B
{
    protected override void Goo()
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
                    "Goo()",
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

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932387")>
        Public Sub TestContent_InheritedMembers3()
            Dim code =
<Code>
class A
{
    protected virtual void Goo()
    {
    }
}

class B : A
{
    protected override void Goo()
    {
    }

    public virtual void Bar()
    {
    }
}

class C : B
{
    protected override void Goo()
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
                    "Goo()",
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

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932387")>
        Public Sub TestContent_HelpKeyword_Ctor()
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

        <WpfFact>
        Public Sub TestDescription_Project()
            Dim code =
<Code>
namespace N { }
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()

                list.VerifyDescriptions($"{ServicesVSResources.Project}CSharpAssembly1")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Namespace()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "CSharpAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Class1()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "CSharpAssembly1")}",
"internal sealed class C : B" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "CSharpAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Class2()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "CSharpAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_ClassWithConstraints()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "CSharpAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Interfaces()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "CSharpAssembly1")}",
"internal interface I2" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "CSharpAssembly1")}",
"internal interface I3" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "CSharpAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Struct1()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "CSharpAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Method()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/939739")>
        Public Sub TestDescription_MethodInInterface()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "I")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_ExtensionMethod()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_MethodWithParameters()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_MethodWithOptionalParameter1()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_MethodWithOptionalParameter2()
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
                    testCulture As New CultureContext(New CultureInfo("en-US", useUserOverride:=False))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"private void M([double x = 3.14159265358979])" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_MethodWithOptionalParameter3()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_MethodWithOptionalParameter4()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_MethodWithOptionalParameter5()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_UnsafeMethod()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "UnsafeC")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_MethodWithConstraints()
            Dim code =
<Code>
using System.Collections.Generic;
class C
{
    T M1&lt;T, U, V&gt;()
        where T : class, new()
        where U : V
        where V : List&lt;T&gt;
    {
        return null;
    }

    void M2&lt;T&gt;(T t)
        where T : IDisposable, allows ref struct
    {
    }
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                Dim memberDescription1 =
"private T M1<T, U, V>()" & vbCrLf &
vbTab & "where T : class, new()" & vbCrLf &
vbTab & "where U : V" & vbCrLf &
vbTab & "where V : System.Collections.Generic.List<T>" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}"

                Dim memberDescription2 =
"private void M2<T>(T t)" & vbCrLf &
vbTab & "where T : IDisposable, allows ref struct" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}"

                list.VerifyImmediateMemberDescriptions(memberDescription1, memberDescription2)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_ReadOnlyField()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_ConstField()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property1()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property2()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property3()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property4()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property5()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Property6()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Indexer1()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Indexer2()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Indexer3()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Enum1()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "CSharpAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Enum2()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "CSharpAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Enum3()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "CSharpAssembly1")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_EnumMember()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "E")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Event1()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Event2()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_XmlDocComment()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}" & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Summary_colon & vbCrLf &
"The M method." & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Returns_colon & vbCrLf &
"Returns a TResult.")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_XmlDocComment_Returns1()
            Dim code =
<Code>
    <![CDATA[
class C
{
    /// <summary>
    /// Describes the method.
    /// </summary>
    /// <returns>Returns a value.</returns>
    public int M()
    {
        return 0;
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
"public int M()" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}" & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Summary_colon & vbCrLf &
"Describes the method." & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Returns_colon & vbCrLf &
"Returns a value.")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_XmlDocComment_Returns2()
            Dim code =
<Code>
    <![CDATA[
class C
{
    /// <summary>
    /// Gets a value.
    /// </summary>
    /// <returns>Returns a value.</returns>
    public int M { get; }
}
]]>
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public int M { get; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}" & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Summary_colon & vbCrLf &
"Gets a value." & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Returns_colon & vbCrLf &
"Returns a value.")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_XmlDocComment_Value()
            Dim code =
<Code>
    <![CDATA[
class C
{
    /// <summary>
    /// Gets a value.
    /// </summary>
    /// <value>An integer value.</value>
    public int M { get; }
}
]]>
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()
                Dim list = library.GetProjectList()
                list = list.GetTypeList(0)
                list = list.GetMemberList(0)

                list.VerifyImmediateMemberDescriptions(
"public int M { get; }" & vbCrLf &
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}" & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Summary_colon & vbCrLf &
"Gets a value." & vbCrLf &
"" & vbCrLf &
ServicesVSResources.Value_colon & vbCrLf &
"An integer value.")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Add()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Subtract()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Multiply()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Divide()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Implicit()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_Operator_Explicit()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact>
        Public Sub TestDescription_ExternMethod()
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
$"    {String.Format(ServicesVSResources.Member_of_0, "C")}")
            End Using
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942021")>
        Public Sub TestNavInfo_Class()
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

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/942021")>
        Public Sub TestNavInfo_NestedEnum()
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

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59458")>
        Public Sub TestCheckedBinaryOperator()
            Dim code =
<Code>
class C
{
    public static C operator +(C x, C y) => throw new System.Exception();

    public static C operator checked +(C x, C y) => throw new System.Exception();
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()

                Dim list = library.GetProjectList()
                list.VerifyNames("CSharpAssembly1")

                list = list.GetTypeList(0)
                list.VerifyNames("C")

                list = list.GetMemberList(0)
                list.VerifyNames(AddressOf IsImmediateMember, "operator +(C, C)", "operator checked +(C, C)")
            End Using
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59458")>
        Public Sub TestCheckedUnaryOperator()
            Dim code =
<Code>
class C
{
    public static C operator -(C x) => throw new System.Exception();

    public static C operator checked -(C x) => throw new System.Exception();
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()

                Dim list = library.GetProjectList()
                list.VerifyNames("CSharpAssembly1")

                list = list.GetTypeList(0)
                list.VerifyNames("C")

                list = list.GetMemberList(0)
                list.VerifyNames(AddressOf IsImmediateMember, "operator -(C)", "operator checked -(C)")
            End Using
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/59458")>
        Public Sub TestCheckedCastOperator()
            Dim code =
<Code>
class C
{
    public static explicit operator string(C x) => throw new System.Exception();

    public static explicit operator checked string(C x) => throw new System.Exception();$$
}
</Code>

            Using state = CreateLibraryManager(GetWorkspaceDefinition(code))
                Dim library = state.GetLibrary()

                Dim list = library.GetProjectList()
                list.VerifyNames("CSharpAssembly1")

                list = list.GetTypeList(0)
                list.VerifyNames("C")

                list = list.GetMemberList(0)
                list.VerifyNames(AddressOf IsImmediateMember, "explicit operator string(C)", "explicit operator checked string(C)")
            End Using
        End Sub

    End Class
End Namespace
