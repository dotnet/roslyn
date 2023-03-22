' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    Friend Class EndConstructGenerationOptionsStorage
        Public Shared ReadOnly EndConstruct As New PerLanguageOption2(Of Boolean)("visual_basic_generate_end_construct", defaultValue:=True)
    End Class
End Namespace
