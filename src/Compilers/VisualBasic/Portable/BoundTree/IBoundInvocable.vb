' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Interface IBoundInvocable
        ReadOnly Property CallOpt As BoundCall
        ReadOnly Property InstanceOpt As BoundExpression
    End Interface
End Namespace
