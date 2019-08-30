' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Partial Class BoundDoLoopStatement
        Implements IBoundConditionalLoop

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

        Private ReadOnly Property IBoundConditionalLoop_Condition As BoundExpression Implements IBoundConditionalLoop.Condition
            Get
                Return ConditionOpt
            End Get
        End Property

        Private ReadOnly Property IBoundConditionalLoop_IgnoredCondition As BoundExpression Implements IBoundConditionalLoop.IgnoredCondition
            Get
                If TopConditionOpt IsNot Nothing AndAlso BottomConditionOpt IsNot Nothing Then
                    Return BottomConditionOpt
                Else
                    Return Nothing
                End If
            End Get
        End Property

        Private ReadOnly Property IBoundConditionalLoop_Body As BoundNode Implements IBoundConditionalLoop.Body
            Get
                Return Body
            End Get
        End Property
    End Class
End Namespace
