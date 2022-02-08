' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
    <Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
    Public Class CrossLanguageReferenceHighlightingTests
        Inherits AbstractReferenceHighlightingTests

        <WpfTheory>
        <CombinatorialData>
        Public Async Function VerifyHighlightsWithNonCompilationProject(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using System;
                            class C
                            {
                                void Blah()
                                {
                                    {|Reference:$$Console|}.WriteLine();
                                }
                            }
                        </Document>
                    </Project>
                    <Project Language="NoCompilation">
                        <Document>
                            class C {
                            }
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function

        <WpfTheory>
        <CombinatorialData>
        Public Async Function VerifyHighlightsWithNonCompilationProject_P2P(testHost As TestHost) As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" Name="CSharpProject">
                        <Document>
                            using System;
                            public class {|Definition:$$C|}
                            {
                                void Blah()
                                {
                                    Console.WriteLine();
                                }
                            }
                        </Document>
                    </Project>
                    <Project Language="NoCompilation">
                        <ProjectReference>CSharpProject</ProjectReference>
                        <Document>
                            class C {
                            }
                        </Document>
                    </Project>
                </Workspace>, testHost)
        End Function
    End Class
End Namespace
