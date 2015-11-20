' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.GraphModel
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    Public Class CSharpSymbolLabelTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestNamedType() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class $$C { }
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "C", "C")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestGenericNamedType() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs"><![CDATA[[
                                class $$C<T> { }
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "C<T>", "C<T>")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestGenericMethod() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs"><![CDATA[[
                                class C { void $$M<T>() { } }
                            ]]></Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "M<T>() : void", "C.M<T>() : void")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestMethodWithParamsParameter() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { void $$M(params string[] foo) { } }
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "M(params string[]) : void", "C.M(params string[]) : void")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestMethodWithOptionalParameter() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { void $$M(int i = 0) { } }
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "M([int]) : void", "C.M([int]) : void")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Async Function TestMethodWithRefAndOutParameters() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { void $$M(out string foo, ref string bar) { } }
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "M(out string, ref string) : void", "C.M(out string, ref string) : void")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(545017)>
        Public Async Function TestEnumMember() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                enum E { $$M }
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "M", "E.M")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(545014)>
        Public Async Function TestConstructor() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { $$C() { } }
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "C()", "C.C()")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(545014)>
        Public Async Function TestDestructor() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { ~$$C() { } }
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "~C()", "C.~C()")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(545013)>
        Public Async Function TestExplicitlyImplementedInterface() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                using System;
                                class C : IDisposable { void IDisposable.$$Dispose() { } }
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "IDisposable.Dispose() : void", "C.Dispose() : void")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(13229, "DevDiv_Projects/Roslyn"), WorkItem(545353)>
        Public Async Function TestFixedFieldInStruct() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                struct C { fixed int $$f[42]; }
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "f : int*", "C.f : int*")
            End Using
        End Function

        <WorkItem(545011)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Progression), WorkItem(13229, "DevDiv_Projects/Roslyn")>
        Public Async Function TestDelegateStyle() As Task
            Using testState = Await ProgressionTestState.CreateAsync(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                delegate void $$Foo();
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "Foo() : void", "Foo : void")
            End Using
        End Function
    End Class
End Namespace
