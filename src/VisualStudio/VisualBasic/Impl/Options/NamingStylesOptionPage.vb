' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Windows
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <Guid(Guids.VisualBasicOptionPageNamingStyleIdString)>
    Friend Class NamingStylesOptionPage
        Inherits AbstractOptionPage

        Private _grid As NamingStyleOptionGrid

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider) As AbstractOptionPageControl
            _grid = New NamingStyleOptionGrid(serviceProvider, LanguageNames.VisualBasic)
            Return _grid
        End Function

        Protected Overrides Sub OnApply(e As PageApplyEventArgs)
            If _grid.ContainsErrors() Then
                MessageBox.Show(BasicVSResources.Some_naming_rules_are_incomplete)
                e.ApplyBehavior = ApplyKind.Cancel
                Return
            End If

            MyBase.OnApply(e)
        End Sub
    End Class
End Namespace
