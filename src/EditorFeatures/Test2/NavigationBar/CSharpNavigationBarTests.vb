' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.NavigationBar)>
    Partial Public Class CSharpNavigationBarTests
        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545021")>
        Public Async Function TestGenericTypeVariance(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[interface C<in I, out O> { }]]></Document>
                    </Project>
                </Workspace>,
                host,
                Item("C<in I, out O>", Glyph.InterfaceInternal, children:={}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545284")>
        Public Async Function TestGenericMember(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[class Program { static void Swap<T>(T lhs, T rhs) { }}]]></Document>
                    </Project>
                </Workspace>,
                host,
                Item("Program", Glyph.ClassInternal, children:={
                     Item("Swap<T>(T lhs, T rhs)", Glyph.MethodPrivate)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545023")>
        Public Async Function TestNestedClasses(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { class Nested { } }</Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, children:={}),
                Item("C.Nested", Glyph.ClassPrivate, children:={}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545023")>
        Public Async Function TestSelectedItemForNestedClass(host As TestHost) As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { class Nested { $$ } }</Document>
                    </Project>
                </Workspace>,
                host,
                Item("C.Nested", Glyph.ClassPrivate), False,
                Nothing, False)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545019")>
        Public Async Function TestSelectedItemForEnumAfterComma(host As TestHost) As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>enum E { A,$$ B }</Document>
                    </Project>
                </Workspace>,
                host,
                Item("E", Glyph.EnumInternal), False,
                Item("A", Glyph.EnumMemberPublic), False)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545114")>
        Public Async Function TestSelectedItemForFieldAfterSemicolon(host As TestHost) As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { int goo;$$ }</Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal), False,
                Item("goo", Glyph.FieldPrivate), False)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545114")>
        Public Async Function TestSelectedItemForFieldInType(host As TestHost) As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { in$$t goo; }</Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal), False,
                Item("goo", Glyph.FieldPrivate), False)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545267")>
        Public Async Function TestSelectedItemAtEndOfFile(host As TestHost) As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { int goo; } $$</Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal), True,
                Item("goo", Glyph.FieldPrivate), True)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545012")>
        Public Async Function TestExplicitInterfaceImplementation(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System;
                            class C : IDisposable { void IDisposable.Dispose() { } }
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, children:={
                    Item("IDisposable.Dispose()", Glyph.MethodPrivate)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545007")>
        Public Async Function TestRefAndOutParameters(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C { void M(out string goo, ref string bar) { } }
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M(out string goo, ref string bar)", Glyph.MethodPrivate)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545001")>
        Public Async Function TestOptionalParameter(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C { void M(int i = 0) { } }
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M(int i = 0)", Glyph.MethodPrivate)}))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestOptionalParameter2(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C { void M(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { } }
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M(CancellationToken cancellationToken = default)", Glyph.MethodPrivate)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545274")>
        Public Async Function TestProperties(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C { private int Number { get; set; } }
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, children:={
                    Item("Number", Glyph.PropertyPrivate)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545220")>
        Public Async Function TestEnum(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            enum Goo { A, B, C }
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("Goo", Glyph.EnumInternal, children:={
                    Item("A", Glyph.EnumMemberPublic),
                    Item("B", Glyph.EnumMemberPublic),
                    Item("C", Glyph.EnumMemberPublic)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545220")>
        Public Async Function TestDelegate(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            delegate void Goo();
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("Goo", Glyph.DelegateInternal, children:={}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545114")>
        Public Async Function TestPartialClassWithFieldInOtherFile(host As TestHost) As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { $$ }</Document>
                        <Document>partial class C { int goo; }</Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal), False,
                Item("goo", Glyph.FieldPrivate, grayed:=True), True)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578100")>
        Public Async Function TestPartialClassWithBothPartialMethodParts1(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { $$partial void M(); }</Document>
                        <Document>partial class C { partial void M(){} }</Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M()", Glyph.MethodPrivate),
                    Item("M()", Glyph.MethodPrivate, grayed:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578100")>
        Public Async Function TestPartialClassWithBothPartialMethodParts2(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { partial void M(); }</Document>
                        <Document>partial class C { $$partial void M(){} }</Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M()", Glyph.MethodPrivate),
                    Item("M()", Glyph.MethodPrivate, grayed:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578100")>
        Public Async Function TestPartialClassWithBothExtendedPartialMethodParts1(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { $$public partial void M(); }</Document>
                        <Document>partial class C { public partial void M(){} }</Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M()", Glyph.MethodPublic),
                    Item("M()", Glyph.MethodPublic, grayed:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578100")>
        Public Async Function TestPartialClassWithBothExtendedPartialMethodParts2(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { public partial void M(); }</Document>
                        <Document>partial class C { $$public partial void M(){} }</Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M()", Glyph.MethodPublic),
                    Item("M()", Glyph.MethodPublic, grayed:=True)}))
        End Function

        <Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/37183")>
        Public Async Function TestNullableReferenceTypesInParameters(host As TestHost) As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>#nullable enable
                        using System.Collections.Generic;
                        class C { void M(string? s, IEnumerable&lt;string?&gt; e) { }</Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M(string? s, IEnumerable<string?> e)", Glyph.MethodPrivate)}))
        End Function

        <Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/59458")>
        Public Async Function TestCheckedBinaryOperator(host As TestHost) As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class C
{
    public static C operator +(C x, C y) => throw new System.Exception();

    public static C operator checked +(C x, C y) => throw new System.Exception();$$
}
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal), False,
                Item("operator checked +(C x, C y)", Glyph.Operator), False)
        End Function

        <Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/59458")>
        Public Async Function TestCheckedUnaryOperator(host As TestHost) As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class C
{
    public static C operator -(C x) => throw new System.Exception();

    public static C operator checked -(C x) => throw new System.Exception();$$
}
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal), False,
                Item("operator checked -(C x)", Glyph.Operator), False)
        End Function

        <Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/59458")>
        Public Async Function TestCheckedCastOperator(host As TestHost) As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
class C
{
    public static explicit operator string(C x) => throw new System.Exception();

    public static explicit operator checked string(C x) => throw new System.Exception();$$
}
                        </Document>
                    </Project>
                </Workspace>,
                host,
                Item("C", Glyph.ClassInternal), False,
                Item("explicit operator checked string(C x)", Glyph.Operator), False)
        End Function
    End Class
End Namespace
