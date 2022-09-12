' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class CSharpKeywordHighlightingTests
        Inherits AbstractKeywordHighlightingTests

        <WpfFact>
        Public Async Function TestVerifyNoHighlightsWhenOptionDisabled() As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class Goo
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

        <WpfFact>
        Public Async Function TestVerifyHighlightsWhenOptionEnabled() As Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            class Goo
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
