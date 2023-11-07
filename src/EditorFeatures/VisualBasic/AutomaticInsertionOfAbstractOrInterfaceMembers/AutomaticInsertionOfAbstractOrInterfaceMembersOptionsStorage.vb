' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.VisualBasic.AutomaticInsertionOfAbstractOrInterfaceMembers
    Friend Class AutomaticInsertionOfAbstractOrInterfaceMembersOptionsStorage
        'This value is only used by Visual Basic, and so is using the old serialization name that was used by vb.
        Public Shared ReadOnly AutomaticInsertionOfAbstractOrInterfaceMembers As PerLanguageOption2(Of Boolean) = New PerLanguageOption2(Of Boolean)(
            "visual_basic_insert_abstract_or_interface_members_on_return", defaultValue:=True)
    End Class
End Namespace
