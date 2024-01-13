' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Composition
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeStyle
    <ExportLanguageService(GetType(ICodeStyleService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicCodeStyleService
        Implements ICodeStyleService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public ReadOnly Property DefaultOptions As IdeCodeStyleOptions Implements ICodeStyleService.DefaultOptions
            Get
                Return VisualBasicIdeCodeStyleOptions.Default
            End Get
        End Property

        Public Function GetIdeCodeStyleOptions(options As IOptionsReader, fallbackOptions As IdeCodeStyleOptions) As IdeCodeStyleOptions Implements ICodeStyleService.GetIdeCodeStyleOptions
            Return New VisualBasicIdeCodeStyleOptions(options, DirectCast(fallbackOptions, VisualBasicIdeCodeStyleOptions))
        End Function
    End Class
End Namespace
