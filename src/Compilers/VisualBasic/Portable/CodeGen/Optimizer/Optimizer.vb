﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen

    ''' <summary>
    ''' Optimizer performs optimization of the bound tree performed before passing it to a codegen. Generally it may
    ''' include several phases like stack scheduling of local variables, etc...
    ''' </summary>
    ''' <remarks></remarks>
    Partial Friend Class Optimizer

        Public Shared Function Optimize(
                         container As Symbol,
                         src As BoundStatement,
                         debugFriendly As Boolean,
                         <Out> ByRef stackLocals As HashSet(Of LocalSymbol)) As BoundStatement

            ' TODO: run other optimizing passes here.
            '       stack scheduler must be the last one.

            Return StackScheduler.OptimizeLocalsOut(container, src, debugFriendly, stackLocals)
        End Function

    End Class

End Namespace

