' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.Mocks
    Friend NotInheritable Class MockTextManagerAdapter
        Implements ITextManagerAdapter

        Public Function CreateTextPoint(fileCodeModel As FileCodeModel, point As VirtualTreePoint) As EnvDTE.TextPoint Implements ITextManagerAdapter.CreateTextPoint
            Dim options = fileCodeModel.GetDocument().GetOptionsAsync().WaitAndGetResult_CodeModel(CancellationToken.None)
            Return New MockTextPoint(point, options.GetOption(FormattingOptions.TabSize))
        End Function
    End Class
End Namespace
