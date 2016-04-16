' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
