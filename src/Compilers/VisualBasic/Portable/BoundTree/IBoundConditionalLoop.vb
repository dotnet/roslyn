' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Interface IBoundConditionalLoop
        ReadOnly Property Condition As BoundExpression
        ReadOnly Property IgnoredCondition As BoundExpression
        ReadOnly Property Body As BoundNode
    End Interface
End Namespace
