'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports Roslyn.Compilers.Collections
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic
    Friend MustInherit Class SyntaxBinding
        ''' <summary>
        ''' Gets the associated binding info for an expression node
        ''' </summary>
        ''' <param name="expression"></param>
        ''' <returns></returns>
        Public MustOverride Function GetBindingInfo(ByVal expression As ExpressionSyntax) As SymbolInfo

        ''' <summary>
        ''' Gets the associated binding info for an expression node as seen from the perspective of the node's parent.
        ''' Calling this method will allow you to observe the result of implicit user defined coercions.
        ''' </summary>
        Public MustOverride Function GetBindingInfoInParent(ByVal expression As ExpressionSyntax) As SymbolInfo
    End Class
End Namespace

