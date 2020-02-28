' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Partial Friend Module SyntaxTokenExtensions
        <Extension()>
        Public Function IsKind(token As SyntaxToken, kind1 As SyntaxKind, kind2 As SyntaxKind) As Boolean
            Return token.Kind = kind1 OrElse
                   token.Kind = kind2
        End Function

        <Extension()>
        Public Function IsKind(token As SyntaxToken, ParamArray kinds As SyntaxKind()) As Boolean
            Return kinds.Contains(token.Kind)
        End Function
    End Module
End Namespace
