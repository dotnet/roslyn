' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.Mocks
    Friend NotInheritable Class MockTextManagerAdapter
        Implements ITextManagerAdapter

        Public Function CreateTextPoint(fileCodeModel As FileCodeModel, point As VirtualTreePoint) As EnvDTE.TextPoint Implements ITextManagerAdapter.CreateTextPoint
            Return New MockTextPoint(point)
        End Function
    End Class
End Namespace
