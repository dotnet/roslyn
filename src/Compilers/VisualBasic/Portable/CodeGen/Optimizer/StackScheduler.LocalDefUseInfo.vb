﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen
    Partial Friend Class StackScheduler

        ''' <summary>
        ''' Represents a local and its Def-Use-Use chain
        ''' 
        ''' NOTE: stack local reads are destructive to the locals so 
        ''' if the read is not last one, it must be immediately followed by another definition. 
        ''' For the rewriting purposes it is irrelevant if definition was created by a write or 
        ''' a subsequent read. These cases are not ambiguous because when rewriting, definition 
        ''' will match to a single node and  we always know if given node is reading or writing.
        ''' </summary>
        ''' <remarks></remarks>
        Private Class LocalDefUseInfo

            ''' <summary>
            ''' stack at variable declaration, may be > 0 in sequences.
            ''' </summary>
            Public ReadOnly StackAtDeclaration As Integer

            ''' <summary>
            ''' value definitions for this variable
            ''' </summary>
            Public ReadOnly localDefs As List(Of LocalDefUseSpan) = New List(Of LocalDefUseSpan)(8)

            Private _cannotSchedule As Boolean = False

            ''' <summary>
            ''' once this goes to true we are no longer interested in this variable.
            ''' </summary>
            Public ReadOnly Property CannotSchedule As Boolean
                Get
                    Return _cannotSchedule
                End Get
            End Property

            Public Sub ShouldNotSchedule()
                _cannotSchedule = True
            End Sub

            Public Sub New(stackAtDeclaration As Integer)
                Debug.Assert(stackAtDeclaration >= 0)
                Me.StackAtDeclaration = stackAtDeclaration
            End Sub
        End Class

    End Class
End Namespace

