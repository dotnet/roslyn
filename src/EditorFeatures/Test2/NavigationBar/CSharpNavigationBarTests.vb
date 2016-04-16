' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports System.Threading.Tasks

#Disable Warning RS0007 ' Avoid zero-length array allocations. This is non-shipping test code.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
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
                Item("A", Glyph.EnumMember), False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545114")>
        Public Async Function TestSelectedItemForFieldAfterSemicolon() As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { int foo;$$ }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal), False,
                Item("foo", Glyph.FieldPrivate), False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545114")>
        Public Async Function TestSelectedItemForFieldInType() As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { in$$t foo; }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal), False,
                Item("foo", Glyph.FieldPrivate), False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545267")>
        Public Async Function TestSelectedItemAtEndOfFile() As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { int foo; } $$</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal), True,
                Item("foo", Glyph.FieldPrivate), True)
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
                            class C { void M(out string foo, ref string bar) { } }
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M(out string foo, ref string bar)", Glyph.MethodPrivate)}))
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
                            enum Foo { A, B, C }
                        </Document>
                    </Project>
                </Workspace>,
                Item("Foo", Glyph.EnumInternal, children:={
                    Item("A", Glyph.EnumMember),
                    Item("B", Glyph.EnumMember),
                    Item("C", Glyph.EnumMember)}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545220, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545220")>
        Public Async Function TestDelegate() As Task
            Await AssertItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            delegate void Foo();
                        </Document>
                    </Project>
                </Workspace>,
                Item("Foo", Glyph.DelegateInternal, children:={}))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545114")>
        Public Async Function TestPartialClassWithFieldInOtherFile() As Task
            Await AssertSelectedItemsAreAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { $$ }</Document>
                        <Document>partial class C { int foo; }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal), False,
                Item("foo", Glyph.FieldPrivate, grayed:=True), True)
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
    End Class
End Namespace
