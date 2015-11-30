﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


' NOTE: VB does not support constant expressions in flow analysis during command-line compilation, but supports them when 
'       analysis is being called via public API. This distinction is governed by 'suppressConstantExpressions' flag

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend MustInherit Class AbstractFlowPass(Of LocalState As AbstractLocalState)
        Inherits BoundTreeVisitor

        ''' <summary>
        ''' Mutate 'self' flow analysis state to reflect the fact that there is a control-flow
        ''' convergence with the 'other' flow analysis state.  Return true if and only if the
        ''' state has changed as a result of the Join.
        ''' </summary>
        Protected MustOverride Function IntersectWith(ByRef self As LocalState, ByRef other As LocalState) As Boolean

        ''' <summary>
        ''' Mutate 'self' flow analysis state to reflect the fact that there is a control-flow
        ''' sequence with the 'other' flow analysis state - in other words, this occurs and then
        ''' the other.
        ''' </summary>
        Protected MustOverride Sub UnionWith(ByRef self As LocalState, ByRef other As LocalState)

        Friend Interface AbstractLocalState

            ''' <summary>
            ''' Produce a duplicate of this flow analysis state.
            ''' </summary>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Function Clone() As LocalState

        End Interface

    End Class
End Namespace
