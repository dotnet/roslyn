' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.GraphModel
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.Progression)>
    Public Class CSharpSymbolLabelTests
        <WpfFact>
        Public Async Function TestNamedType() As Task
            Using testState = ProgressionTestState.Create(
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

        <WpfFact>
        Public Async Function TestGenericNamedType() As Task
            Using testState = ProgressionTestState.Create(
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

        <WpfFact>
        Public Async Function TestGenericMethod() As Task
            Using testState = ProgressionTestState.Create(
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

        <WpfFact>
        Public Async Function TestMethodWithParamsParameter() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { void $$M(params string[] goo) { } }
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "M(params string[]) : void", "C.M(params string[]) : void")
            End Using
        End Function

        <WpfFact>
        Public Async Function TestMethodWithOptionalParameter() As Task
            Using testState = ProgressionTestState.Create(
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

        <WpfFact>
        Public Async Function TestMethodWithRefAndOutParameters() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                class C { void $$M(out string goo, ref string bar) { } }
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "M(out string, ref string) : void", "C.M(out string, ref string) : void")
            End Using
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545017")>
        Public Async Function TestEnumMember() As Task
            Using testState = ProgressionTestState.Create(
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

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545014")>
        Public Async Function TestConstructor() As Task
            Using testState = ProgressionTestState.Create(
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

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545014")>
        Public Async Function TestDestructor() As Task
            Using testState = ProgressionTestState.Create(
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

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545013")>
        Public Async Function TestExplicitlyImplementedInterface() As Task
            Using testState = ProgressionTestState.Create(
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

        <WpfFact, WorkItem(13229, "DevDiv_Projects/Roslyn"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545353")>
        Public Async Function TestFixedFieldInStruct() As Task
            Using testState = ProgressionTestState.Create(
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545011")>
        <WpfFact, WorkItem(13229, "DevDiv_Projects/Roslyn")>
        Public Async Function TestDelegateStyle() As Task
            Using testState = ProgressionTestState.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" FilePath="Z:\Project.csproj">
                            <Document FilePath="Z:\Project.cs">
                                delegate void $$Goo();
                            </Document>
                        </Project>
                    </Workspace>)

                Await testState.AssertMarkedSymbolLabelIsAsync(GraphCommandDefinition.Contains.Id, "Goo() : void", "Goo : void")
            End Using
        End Function
    End Class
End Namespace
