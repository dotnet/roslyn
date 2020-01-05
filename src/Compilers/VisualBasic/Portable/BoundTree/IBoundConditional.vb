' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Interface IBoundConditional
        ReadOnly Property Condition As BoundExpression
        ReadOnly Property WhenTrue As BoundNode
        ReadOnly Property WhenFalseOpt As BoundNode
    End Interface
End Namespace
