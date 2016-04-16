' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting
    Public Class VisualBasicKeywordHighlightingTests
        Inherits AbstractKeywordHighlightingTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function VerifyNoHighlightsWhenOptionDisabled() As System.Threading.Tasks.Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class Foo
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)>
        Public Async Function VerifyHighlightsWhenOptionEnabled() As System.Threading.Tasks.Task
            Await VerifyHighlightsAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Class Foo
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
