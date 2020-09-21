﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
    <[UseExportProvider]>
    Partial Public Class CSharpNavigationBarTests
        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545021, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545021")>
        Public Async Function TestGenericTypeVariance() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[interface C<in I, out O> { }]]></Document>
                    </Project>
                </Workspace>,
                Item("C<in I, out O>", Glyph.InterfaceInternal, children:={}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545284, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545284")>
        Public Async Function TestGenericMember() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[class Program { static void Swap<T>(T lhs, T rhs) { }}]]></Document>
                    </Project>
                </Workspace>,
                Item("Program", Glyph.ClassInternal, children:={
                     Item("Swap<T>(T lhs, T rhs)", Glyph.MethodPrivate)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545023")>
        Public Async Function TestNestedClasses() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { class Nested { } }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={}),
                Item("C.Nested", Glyph.ClassPrivate, children:={}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545023")>
        Public Async Function TestSelectedItemForNestedClass() As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { class Nested { $$ } }</Document>
                    </Project>
                </Workspace>,
                Item("C.Nested", Glyph.ClassPrivate), False,
                Nothing, False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545019, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545019")>
        Public Async Function TestSelectedItemForEnumAfterComma() As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>enum E { A,$$ B }</Document>
                    </Project>
                </Workspace>,
                Item("E", Glyph.EnumInternal), False,
                Item("A", Glyph.EnumMemberPublic), False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545114")>
        Public Async Function TestSelectedItemForFieldAfterSemicolon() As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { int goo;$$ }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal), False,
                Item("goo", Glyph.FieldPrivate), False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545114")>
        Public Async Function TestSelectedItemForFieldInType() As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { in$$t goo; }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal), False,
                Item("goo", Glyph.FieldPrivate), False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545267")>
        Public Async Function TestSelectedItemAtEndOfFile() As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { int goo; } $$</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal), True,
                Item("goo", Glyph.FieldPrivate), True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545012, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545012")>
        Public Async Function TestExplicitInterfaceImplementation() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System;
                            class C : IDisposable { void IDisposable.Dispose() { } }
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("IDisposable.Dispose()", Glyph.MethodPrivate)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545007")>
        Public Async Function TestRefAndOutParameters() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C { void M(out string goo, ref string bar) { } }
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M(out string goo, ref string bar)", Glyph.MethodPrivate)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545001, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545001")>
        Public Async Function TestOptionalParameter() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C { void M(int i = 0) { } }
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M(int i = 0)", Glyph.MethodPrivate)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestOptionalParameter2() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C { void M(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { } }
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M(CancellationToken cancellationToken = default)", Glyph.MethodPrivate)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545274, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545274")>
        Public Async Function TestProperties() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C { private int Number { get; set; } }
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("Number", Glyph.PropertyPrivate)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545220")>
        Public Async Function TestEnum() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            enum Goo { A, B, C }
                        </Document>
                    </Project>
                </Workspace>,
                Item("Goo", Glyph.EnumInternal, children:={
                    Item("A", Glyph.EnumMemberPublic),
                    Item("B", Glyph.EnumMemberPublic),
                    Item("C", Glyph.EnumMemberPublic)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545220")>
        Public Async Function TestDelegate() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            delegate void Goo();
                        </Document>
                    </Project>
                </Workspace>,
                Item("Goo", Glyph.DelegateInternal, children:={}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545114")>
        Public Async Function TestPartialClassWithFieldInOtherFile() As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { $$ }</Document>
                        <Document>partial class C { int goo; }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal), False,
                Item("goo", Glyph.FieldPrivate, grayed:=True), True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(578100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578100")>
        Public Async Function TestPartialClassWithBothPartialMethodParts1() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { $$partial void M(); }</Document>
                        <Document>partial class C { partial void M(){} }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M()", Glyph.MethodPrivate),
                    Item("M()", Glyph.MethodPrivate, grayed:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(578100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578100")>
        Public Async Function TestPartialClassWithBothPartialMethodParts2() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { partial void M(); }</Document>
                        <Document>partial class C { $$partial void M(){} }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M()", Glyph.MethodPrivate),
                    Item("M()", Glyph.MethodPrivate, grayed:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(578100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578100")>
        Public Async Function TestPartialClassWithBothExtendedPartialMethodParts1() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { $$public partial void M(); }</Document>
                        <Document>partial class C { public partial void M(){} }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M()", Glyph.MethodPublic),
                    Item("M()", Glyph.MethodPublic, grayed:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(578100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578100")>
        Public Async Function TestPartialClassWithBothExtendedPartialMethodParts2() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { public partial void M(); }</Document>
                        <Document>partial class C { $$public partial void M(){} }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M()", Glyph.MethodPublic),
                    Item("M()", Glyph.MethodPublic, grayed:=True)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(37183, "https://github.com/dotnet/roslyn/issues/37183")>
        Public Async Function TestNullableReferenceTypesInParameters() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>#nullable enable
                        using System.Collections.Generic;
                        class C { void M(string? s, IEnumerable&lt;string?&gt; e) { }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M(string? s, IEnumerable<string?> e)", Glyph.MethodPrivate)}))
        End Function
    End Class
End Namespace
