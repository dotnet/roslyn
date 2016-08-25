' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Interface ISyntaxFactoryContext
        ReadOnly Property IsWithinAsyncMethodOrLambda As Boolean
        ReadOnly Property IsWithinIteratorContext As Boolean
    End Interface
End Namespace