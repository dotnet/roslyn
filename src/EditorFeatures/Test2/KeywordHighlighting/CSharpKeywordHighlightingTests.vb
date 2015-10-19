' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting
    Public Class CSharpKeywordHighlightingTests
        Inherits AbstractKeywordHighlightingTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub VerifyNoHighlightsWhenOptionDisabled()
            VerifyHighlights(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Sub VerifyHighlightsWhenOptionEnabled()
            VerifyHighlights(
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
        End Sub
    End Class
End Namespace
