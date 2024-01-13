' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class BoundDoLoopStatement

        ''' <summary>
        ''' Gets a value indicating whether this do loop is a DoTopLoop or not. In syntax error cases
        ''' where both conditions are used, priority is given to the first one.
        ''' It's recommended to consistently use this property instead of checking the TopConditionOpt and BottomConditionOpt
        ''' directly.
        ''' </summary>
        ''' <value>
        '''   <c>true</c> if this loop is a DoTopLoop; otherwise, <c>false</c>.
        ''' </value>
        Public ReadOnly Property ConditionIsTop As Boolean
            Get
                Return Me.TopConditionOpt IsNot Nothing
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether the condition of this do loop is &quot;until&quot; or not. In syntax error cases
        ''' where both conditions are used, priority is given to the first one.
        ''' It's recommended to consistently use this property instead of checking TopConditionIsUntil and BottomConditionIsUntil
        ''' directly.
        ''' </summary>
        ''' <value>
        '''   <c>true</c> if this loop is a DoTopLoop; otherwise, <c>false</c>.
        ''' </value>
        Public ReadOnly Property ConditionIsUntil As Boolean
            Get
                If Me.TopConditionOpt IsNot Nothing Then
                    Return Me.TopConditionIsUntil
                Else
                    Return Me.BottomConditionIsUntil
                End If
            End Get
        End Property

        ''' <summary>
        ''' Gets the optional bound condition expression for this do loop statement. In syntax error cases
        ''' where both conditions are used, priority is given to the first one.
        ''' It's recommended to consistently use this property instead of accessing TopConditionOpt or BottomConditionOpt
        ''' directly.
        ''' </summary>
        Public ReadOnly Property ConditionOpt As BoundExpression
            Get
                If Me.TopConditionOpt IsNot Nothing Then
                    Return Me.TopConditionOpt
                Else
                    Return Me.BottomConditionOpt
                End If
            End Get
        End Property
    End Class
End Namespace
