' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting
    Public Class CSharpKeywordHighlightingTests
        Inherits AbstractKeywordHighlightingTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestVerifyNoHighlightsWhenOptionDisabled() As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class Foo
                            {
                                void M()
                                {
                                    $$if (true) { }
                                    else { }
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>,
                optionIsEnabled:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function TestVerifyHighlightsWhenOptionEnabled() As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class Foo
                            {
                                void M()
                                {
                                    $$[|if|] (true) { }
                                    [|else|] { }
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>)
        End Function
    End Class
End Namespace
