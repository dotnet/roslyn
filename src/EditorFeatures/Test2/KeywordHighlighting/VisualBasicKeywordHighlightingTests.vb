' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting
    <Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
    Public Class VisualBasicKeywordHighlightingTests
        Inherits AbstractKeywordHighlightingTests

        <WpfFact>
        Public Async Function VerifyNoHighlightsWhenOptionDisabled() As System.Threading.Tasks.Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class Goo
                                Sub M()
                                    $$If True Then
                                    Else
                                    End If
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>,
                optionIsEnabled:=False)
        End Function

        <WpfFact>
        Public Async Function VerifyHighlightsWhenOptionEnabled() As System.Threading.Tasks.Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class Goo
                                Sub M()
                                    $$[|If|] True [|Then|]
                                    [|Else|]
                                    [|End If|]
                                End Sub
                            End Class
                        </Document>
                    </Project>
                </Workspace>)
        End Function
    End Class
End Namespace
