' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Partial Class BoundDoLoopStatement

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
                Return TopConditionOpt IsNot Nothing
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
                If TopConditionOpt IsNot Nothing Then
                    Return TopConditionIsUntil
                Else
                    Return BottomConditionIsUntil
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
                If TopConditionOpt IsNot Nothing Then
                    Return TopConditionOpt
                Else
                    Return BottomConditionOpt
                End If
            End Get
        End Property

    End Class
End Namespace
