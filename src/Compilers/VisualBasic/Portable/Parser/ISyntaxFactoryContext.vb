' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Interface ISyntaxFactoryContext
        ReadOnly Property IsWithinAsyncMethodOrLambda As Boolean
        ReadOnly Property IsWithinIteratorContext As Boolean
    End Interface
End Namespace
