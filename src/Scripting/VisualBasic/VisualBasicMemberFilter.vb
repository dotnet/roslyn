' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Scripting.Hosting

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Friend Class VisualBasicMemberFilter
        Inherits CommonMemberFilter

        Protected Overrides Function IsGeneratedMemberName(name As String) As Boolean
            ' TODO (tomat)
            Return MyBase.IsGeneratedMemberName(name)
        End Function
    End Class

End Namespace
