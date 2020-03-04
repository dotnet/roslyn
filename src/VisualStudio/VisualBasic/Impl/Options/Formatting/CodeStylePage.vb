﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider, optionStore As OptionStore) As AbstractOptionPageControl
            Return New GridOptionPreviewControl(serviceProvider,
                                                optionStore,
                                                Function(o, s) New StyleViewModel(o, s),
                                                GetEditorConfigOptions(),
                                                LanguageNames.VisualBasic)
        End Function

        Private Shared Function GetEditorConfigOptions() As ImmutableArray(Of (String, ImmutableArray(Of IOption)))
            Dim builder = ArrayBuilder(Of (String, ImmutableArray(Of IOption))).GetInstance()
            builder.AddRange(GridOptionPreviewControl.GetLanguageAgnosticEditorConfigOptions())
            builder.Add((BasicVSResources.VB_Coding_Conventions, VisualBasicCodeStyleOptions.AllOptions))
            Return builder.ToImmutableAndFree()
        End Function

        Friend Structure TestAccessor
            Friend Shared Function GetEditorConfigOptions() As ImmutableArray(Of (String, ImmutableArray(Of IOption)))
                Return CodeStylePage.GetEditorConfigOptions()
            End Function
        End Structure
    End Class
End Namespace
