﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundLocalDeclaration

        Public Sub New(syntax As SyntaxNode, localSymbol As LocalSymbol, initializerOpt As BoundExpression)
            MyClass.New(syntax, localSymbol, initializerOpt, False, False)
        End Sub

#If DEBUG Then
        Private Sub Validate()
            If InitializerOpt IsNot Nothing Then

                InitializerOpt.AssertRValue()

                If Not HasErrors Then
                    If InitializerOpt.Type Is Nothing Then
                        Debug.Assert(LocalSymbol.IsConst AndAlso InitializerOpt.IsStrictNothingLiteral())
                    Else
                        Debug.Assert(LocalSymbol.Type.IsSameTypeIgnoringAll(InitializerOpt.Type) OrElse InitializerOpt.Type.IsErrorType() OrElse
                                     (LocalSymbol.IsConst AndAlso LocalSymbol.Type.SpecialType = SpecialType.System_Object AndAlso
                                      InitializerOpt.IsConstant AndAlso InitializerOpt.ConstantValueOpt.IsNothing))
                    End If
                End If
            End If
        End Sub
#End If

    End Class

End Namespace
