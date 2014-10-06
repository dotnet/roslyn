' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Class SyntaxTriviaFunctions
        Friend Shared ReadOnly Skipped As Func(Of SyntaxTrivia, Boolean) = Function(t) t.VBKind = SyntaxKind.SkippedTokensTrivia
    End Class

End Namespace