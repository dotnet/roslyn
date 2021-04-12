' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class LocalRewriter

        <Flags>
        Friend Enum RewritingFlags As Byte
            [Default] = 0
            AllowSequencePoints = 1
            AllowEndOfMethodReturnWithExpression = 2
            AllowCatchWithErrorLineNumberReference = 4
            AllowOmissionOfConditionalCalls = 8
        End Enum

        Private ReadOnly _flags As RewritingFlags

    End Class
End Namespace
