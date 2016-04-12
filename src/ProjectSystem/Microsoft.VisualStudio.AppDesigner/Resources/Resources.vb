' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace My.Resources


    'Hide the Microsoft_VisualStudio_AppDesigner_Designer class.  To keep the .resources file
    '  with the same fully-qualified name in the assembly manifest, we need to have the
    '  Designer.resx file actually named "Microsoft.VisualStudio.Editors.Designer.resx",
    '  or else change the project's root namespace which I don't want to do at this point.
    '  But then the class name gets generated as "Microsoft_VisualStudio_AppDesigner_Designer".
    'So hide that one and introduce a "Designer" class instead.
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Partial Friend Class Microsoft_VisualStudio_AppDesigner_Designer
    End Class

    ''' <summary>
    ''' String resource values for MS.VS.Editors.dll.  To edit the strings in this class,
    '''   edit the Microsoft.VisualStudio.Editors.resx file.
    ''' </summary>
    ''' <remarks>
    ''' </remarks>
    Friend Class Designer
        Inherits Global.My.Resources.Microsoft_VisualStudio_AppDesigner_Designer

        ''' <summary>
        ''' These are some string resource IDs (just the resource ID name, not the 
        '''   actual string value).  These are not automatically kept up to date from
        '''   the .resx file, so they must be edited manually.
        ''' </summary>
        ''' <remarks></remarks>
        Public Class ConstantResourceIDs

            'IMPORTANT: These must be kept manually up to date, they are not automatically
            '  synchronized with the .resx file.

            Public Const PPG_WebReferenceNameDescription As String = "PPG_WebReferenceNameDescription"
            Public Const PPG_ServiceReferenceNamespaceDescription As String = "PPG_ServiceReferenceNamespaceDescription"
            Public Const PPG_UrlBehaviorName As String = "PPG_UrlBehaviorName"
            Public Const PPG_UrlBehaviorDescription As String = "PPG_UrlBehaviorDescription"
            Public Const PPG_WebReferenceUrlName As String = "PPG_WebReferenceUrlName"
            Public Const PPG_WebReferenceUrlDescription As String = "PPG_WebReferenceUrlDescription"
            Public Const PPG_ServiceReferenceUrlName As String = "PPG_ServiceReferenceUrlName"
            Public Const PPG_ServiceReferenceUrlDescription As String = "PPG_ServiceReferenceUrlDescription"

        End Class
    End Class
End Namespace

Namespace Microsoft.VisualStudio.Editors

    ''' <summary>
    ''' Compatibility-only class for string resources.  Newer code should use My.Resources.Designer instead,
    '''   along with String.Format when needed.
    ''' 
    ''' </summary>
    ''' <remarks>
    ''' 
    ''' IMPORTANT: The old SR constants were simply string constants to the IDs.  You still had to call
    '''   SR.GetString() to get the actual string value.
    ''' The auto-generated My.Resources properties, on the other hand, return the actual string value,
    '''   and never the ID constant.
    ''' This compatibility class does *not* return the string IDs, but rather the string values.
    '''   Therefore, GetString() has been changed to simply return the string value unless there are
    '''   arguments passed in, in which case String.Format() is called.
    ''' 
    ''' </remarks>
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
    Friend Class SR
        Inherits Global.My.Resources.Microsoft_VisualStudio_AppDesigner_Designer

        ''' <summary>
        ''' Temporary compatibility function to make converting from Designer.txt to Designer.resx easier.
        ''' Just returns the input string unless there are arguments, in which case it calls String.Format.
        ''' </summary>
        ''' <param name="s"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)> _
        Public Shared Function GetString(ByVal s As String, ByVal ParamArray Arguments() As Object) As String
            If Arguments Is Nothing OrElse Arguments.Length = 0 Then
                Return s
            Else
                Return String.Format(s, Arguments)
            End If
        End Function

    End Class

End Namespace

