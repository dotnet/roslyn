' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' NoOpStatementFlavor specifies additional info that NoOp statement may be carrying;
    ''' Such info may be used in rewriting or code gen phases to perform some special actions
    ''' </summary>
    Friend Enum NoOpStatementFlavor
        [Default]

        ''' <summary> 
        ''' Marks a control yield point for emitted await operator; is processed by codegen; 
        ''' only allowed inside MoveNext methods generated for Async methods
        ''' </summary>
        AwaitYieldPoint

        ''' <summary> 
        ''' Marks a control resume point for emitted await operator; is processed by codegen; 
        ''' only allowed inside MoveNext methods generated for Async methods
        ''' </summary>
        AwaitResumePoint
    End Enum

End Namespace
