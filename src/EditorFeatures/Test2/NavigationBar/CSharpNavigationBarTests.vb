' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

#Disable Warning RS0007 ' Avoid zero-length array allocations. This is non-shipping test code.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
    Partial Public Class CSharpNavigationBarTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545021)>
        Public Sub GenericTypeVariance()
            AssertItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[interface C<in I, out O> { }]]></Document>
                    </Project>
                </Workspace>,
                Item("C<in I, out O>", Glyph.InterfaceInternal, children:={}))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545284)>
        Public Sub GenericMember()
            AssertItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[class Program { static void Swap<T>(T lhs, T rhs) { }}]]></Document>
                    </Project>
                </Workspace>,
                Item("Program", Glyph.ClassInternal, children:={
                     Item("Swap<T>(T lhs, T rhs)", Glyph.MethodPrivate)}))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545023)>
        Public Sub NestedClasses()
            AssertItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { class Nested { } }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={}),
                Item("C.Nested", Glyph.ClassPrivate, children:={}))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545023)>
        Public Sub SelectedItemForNestedClass()
            AssertSelectedItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { class Nested { $$ } }</Document>
                    </Project>
                </Workspace>,
                Item("C.Nested", Glyph.ClassPrivate), False,
                Nothing, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545019)>
        Public Sub SelectedItemForEnumAfterComma()
            AssertSelectedItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>enum E { A,$$ B }</Document>
                    </Project>
                </Workspace>,
                Item("E", Glyph.EnumInternal), False,
                Item("A", Glyph.EnumMember), False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545114)>
        Public Sub SelectedItemForFieldAfterSemicolon()
            AssertSelectedItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { int foo;$$ }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal), False,
                Item("foo", Glyph.FieldPrivate), False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545114)>
        Public Sub SelectedItemForFieldInType()
            AssertSelectedItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { in$$t foo; }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal), False,
                Item("foo", Glyph.FieldPrivate), False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545267)>
        Public Sub SelectedItemAtEndOfFile()
            AssertSelectedItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { int foo; } $$</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal), True,
                Item("foo", Glyph.FieldPrivate), True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545012)>
        Public Sub ExplicitInterfaceImplementation()
            AssertItemsAre(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545007)>
        Public Sub RefAndOutParameters()
            AssertItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C { void M(out string foo, ref string bar) { } }
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M(out string foo, ref string bar)", Glyph.MethodPrivate)}))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545001)>
        Public Sub OptionalParameter()
            AssertItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C { void M(int i = 0) { } }
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M(int i = 0)", Glyph.MethodPrivate)}))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545274)>
        Public Sub Properties()
            AssertItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class C { private int Number { get; set; } }
                        </Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("Number", Glyph.PropertyPrivate)}))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545220)>
        Public Sub [Enum]()
            AssertItemsAre(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545220)>
        Public Sub [Delegate]()
            AssertItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            delegate void Foo();
                        </Document>
                    </Project>
                </Workspace>,
                Item("Foo", Glyph.DelegateInternal, children:={}))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(545114)>
        Public Sub PartialClassWithFieldInOtherFile()
            AssertSelectedItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { $$ }</Document>
                        <Document>partial class C { int foo; }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal), False,
                Item("foo", Glyph.FieldPrivate, grayed:=True), True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(578100)>
        Public Sub PartialClassWithBothPartialMethodParts1()
            AssertItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { $$partial void M(); }</Document>
                        <Document>partial class C { partial void M(){} }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M()", Glyph.MethodPrivate),
                    Item("M()", Glyph.MethodPrivate, grayed:=True)}))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(578100)>
        Public Sub PartialClassWithBothPartialMethodParts2()
            AssertItemsAre(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>partial class C { partial void M(); }</Document>
                        <Document>partial class C { $$partial void M(){} }</Document>
                    </Project>
                </Workspace>,
                Item("C", Glyph.ClassInternal, children:={
                    Item("M()", Glyph.MethodPrivate),
                    Item("M()", Glyph.MethodPrivate, grayed:=True)}))
        End Sub
    End Class
End Namespace
