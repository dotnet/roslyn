' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.Common

    ''' <summary>
    ''' This interface wraps the code marker class in a way that it can be replaced by a mock
    '''   for unit testing.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Interface ICodeMarkers
        Sub CodeMarker(ByVal nTimerID As Integer)
    End Interface

    ''' <summary>
    ''' Standard implementation of ICodeMarker, which hooks in to the real VS code marker utility.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class CodeMarkers
        Implements ICodeMarkers

        Private Sub CodeMarker(ByVal nTimerID As Integer) Implements ICodeMarkers.CodeMarker
            Microsoft.Internal.Performance.CodeMarkers.Instance.CodeMarker(nTimerID)
        End Sub
    End Class

End Namespace
