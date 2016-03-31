Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Validation Result,
    '''   Warning means this can be postponed for delay-validation
    '''   Failed means the user must fix this before leaving the page/field...
    ''' </summary>
    ''' <remarks></remarks>
    Public Enum ValidationResult
        Succeeded = 0
        Warning = 1
        Failed = 2
    End Enum

End Namespace
