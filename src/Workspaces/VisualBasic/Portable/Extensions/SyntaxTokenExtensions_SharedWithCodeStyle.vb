' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
