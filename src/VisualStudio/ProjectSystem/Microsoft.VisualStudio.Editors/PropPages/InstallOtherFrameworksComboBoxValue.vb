'-----------------------------------------------------------------------------------------------------------
'
'  Copyright (c) Microsoft Corporation.  All rights reserved.
'
'-----------------------------------------------------------------------------------------------------------

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Represents the 'Install other frameworks...' item that appears in the target framework combo box
    ''' </summary>
    Class InstallOtherFrameworksComboBoxValue

        Public Overrides Function ToString() As String
            Return My.Resources.Strings.InstallOtherFrameworks
        End Function

    End Class

End Namespace
