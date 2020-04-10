' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Interface IBoundConditionalLoop
        ReadOnly Property Condition As BoundExpression
        ReadOnly Property IgnoredCondition As BoundExpression
        ReadOnly Property Body As BoundNode
    End Interface
End Namespace
