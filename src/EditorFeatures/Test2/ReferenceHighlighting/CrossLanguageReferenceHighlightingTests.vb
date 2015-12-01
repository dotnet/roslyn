' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.ReferenceHighlighting
    Public Class CrossLanguageReferenceHighlightingTests
        Inherits AbstractReferenceHighlightingTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.ReferenceHighlighting)>
        Public Async Function VerifyHighlightsWithNonCompilationProject() As System.Threading.Tasks.Task
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
                </Workspace>)
        End Function
    End Class
End Namespace
