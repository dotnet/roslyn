' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options.Formatting
    <Guid(Guids.VisualBasicOptionPageCodeStyleIdString)>
    Friend Class CodeStylePage
        Inherits AbstractOptionPage

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider) As AbstractOptionPageControl
            Return New GridOptionPreviewControl(serviceProvider,
                                                Function(o, s) New StyleViewModel(o, s),
                                                GetEditorConfigOptions(),
                                                LanguageNames.VisualBasic)
        End Function

        ' Friend for testing purposes.
        Friend Shared Function GetEditorConfigOptions() As ImmutableArray(Of (String, ImmutableArray(Of IOption)))
            Dim builder = ArrayBuilder(Of (String, ImmutableArray(Of IOption))).GetInstance()
            builder.AddRange(GridOptionPreviewControl.GetLanguageAgnosticEditorConfigOptions())
            builder.Add((BasicVSResources.VB_Coding_Conventions, VisualBasicCodeStyleOptions.AllOptions))
            Return builder.ToImmutableAndFree()
        End Function
    End Class
End Namespace
