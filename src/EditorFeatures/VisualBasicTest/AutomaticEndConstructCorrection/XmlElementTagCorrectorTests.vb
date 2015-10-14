' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticEndConstructCorrection
    Public Class XmlElementTagCorrectorTests
        Inherits AbstractCorrectorTests

        Friend Overrides Function CreateCorrector(buffer As ITextBuffer, waitIndicator As TestWaitIndicator) As ICorrector
            Return New XmlElementTagCorrector(buffer, waitIndicator)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestXmlStartTag()
            Dim code = <code><![CDATA[
''' <[|summary|]></[|summary|]>
Structure A
End Structure
]]></code>.Value

            VerifyBegin(code, "see")
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.AutomaticEndConstructCorrection)>
        Public Sub TestContinuousEdit1()
            Dim code = <code><![CDATA[
''' <[|$$summary|]></[|summary|]>
Structure A
End Structure
]]></code>.Value

            VerifyContinuousEdits(code, "see", Function(s) s, removeOriginalContent:=True)
        End Sub
    End Class
End Namespace