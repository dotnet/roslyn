﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Class SyntaxTriviaFunctions
        Friend Shared ReadOnly Skipped As Func(Of SyntaxTrivia, Boolean) = Function(t) t.Kind = SyntaxKind.SkippedTokensTrivia
    End Class

End Namespace
