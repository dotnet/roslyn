'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports System.Runtime.CompilerServices

Namespace Roslyn.Compilers.VisualBasic

    Friend Module VbCoreSymbolExtensions

        ''' <summary>
        ''' True if the syntax tree is an embedded syntax tree
        ''' </summary>
        <Extension()>
        Public Function IsVbCoreSyntaxTree(tree As SyntaxTree) As Boolean
            Debug.Assert(tree IsNot Nothing)
            Return VbCoreSymbolManager.GetEmbeddedKind(tree) <> EmbeddedSymbolKind.None
        End Function

    End Module

End Namespace
