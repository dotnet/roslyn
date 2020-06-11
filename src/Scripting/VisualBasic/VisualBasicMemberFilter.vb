' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Scripting.Hosting

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Friend Class VisualBasicMemberFilter
        Inherits CommonMemberFilter

        Protected Overrides Function IsGeneratedMemberName(name As String) As Boolean
            ' TODO (https://github.com/dotnet/roslyn/issues/8241)
            Return MyBase.IsGeneratedMemberName(name)
        End Function
    End Class

End Namespace
