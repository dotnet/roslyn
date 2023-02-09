' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' represents a single item in Handles list.
    ''' </summary>
    Public NotInheritable Class HandledEvent
        Friend Sub New(kind As HandledEventKind,
                       eventSymbol As EventSymbol,
                       withEventsContainerOpt As PropertySymbol,
                       withEventsSourcePropertyOpt As PropertySymbol,
                       delegateCreation As BoundExpression,
                       hookupMethod As MethodSymbol)

            Me._kind = kind

            Debug.Assert(eventSymbol IsNot Nothing)
            Me._eventSymbol = eventSymbol

            Debug.Assert((withEventsContainerOpt Is Nothing) Or kind = HandledEventKind.WithEvents)
            Me._WithEventsContainerOpt = withEventsContainerOpt

            Me._WithEventsSourcePropertyOpt = withEventsSourcePropertyOpt

            Me.delegateCreation = delegateCreation
            Me.hookupMethod = hookupMethod
        End Sub

        ' kind of Handles
        Private ReadOnly _kind As HandledEventKind

        ' E1 in  "Handles obj.E1"
        Private ReadOnly _eventSymbol As EventSymbol

        ' obj in  "Handles obj.E1"
        ' only makes sense when kind is WithEvents. 
        Private ReadOnly _WithEventsContainerOpt As PropertySymbol

        ' P1 in  "Handles obj.P1.E1"
        ' only makes sense when kind is WithEvents. 
        Private ReadOnly _WithEventsSourcePropertyOpt As PropertySymbol

        ''' <summary>
        ''' Kind of Handles event container. (Me, MyBase, MyClass or a WithEvents variable)
        ''' </summary>
        Public ReadOnly Property HandlesKind As HandledEventKind
            Get
                Return _kind
            End Get
        End Property

        ''' <summary>
        ''' Symbol for the event handled in current Handles item.
        ''' </summary>
        Public ReadOnly Property EventSymbol As IEventSymbol
            Get
                Return _eventSymbol
            End Get
        End Property

        Public ReadOnly Property EventContainer As IPropertySymbol
            Get
                Return _WithEventsContainerOpt
            End Get
        End Property

        Public ReadOnly Property WithEventsSourceProperty As IPropertySymbol
            Get
                Return _WithEventsSourcePropertyOpt
            End Get
        End Property

        ' delegate creation expression used to hook/unhook handlers
        ' note that it may contain relaxation lambdas and will need to be injected 
        ' into the host method before lowering.
        ' Used in rewriter.
        Friend ReadOnly delegateCreation As BoundExpression

        ' this is the host method into which hookups will be injected
        ' Used in rewriter.
        Friend ReadOnly hookupMethod As MethodSymbol
    End Class

    ''' <summary>
    ''' Kind of a Handles item represented by a HandledEvent
    ''' </summary>
    Public Enum HandledEventKind
        ''' <summary>
        ''' Handles Me.Event1
        ''' </summary>
        [Me] = 0

        ''' <summary>
        ''' Handles MyClass.Event1
        ''' </summary>
        [MyClass] = 1

        ''' <summary>
        ''' Handles MyBase.Event1
        ''' </summary>
        [MyBase] = 2

        ''' <summary>
        ''' Handles SomeWithEventsVariable.Event1
        ''' </summary>
        [WithEvents] = 3
    End Enum

End Namespace
