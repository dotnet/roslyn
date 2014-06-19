'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Namespace Roslyn.Compilers.VisualBasic

    ''' <summary>
    ''' Instances of this class represent parameters of methods that have been synthesized. E.g. the parameters of delegate methods
    ''' </summary>
    Friend Class SynthesizedParameterSymbol
        Inherits ParameterSymbol

        Private ReadOnly _container As MethodSymbol
        Private ReadOnly _type As TypeSymbol
        Private ReadOnly _ordinal As Integer
        Private ReadOnly _isByRef As Boolean
        Private ReadOnly _name As String
        Private ReadOnly _customModifiers As ReadOnlyArray(Of CustomModifier)

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SynthesizedParameterSymbol" /> class.
        ''' </summary>
        ''' <param name="container">The containing symbol</param>
        ''' <param name="type">The type of this parameter</param>
        ''' <param name="ordinal">The ordinal number of this parameter</param>
        ''' <param name="isByRef">Whether the parameter is ByRef or not</param>
        ''' <param name="name">The name of this parameter</param>
        Public Sub New(container As MethodSymbol, type As TypeSymbol, ordinal As Integer, isByRef As Boolean, Optional name As String = "")
            Me.New(container, type, ordinal, isByRef, name, ReadOnlyArray(Of CustomModifier).Empty)
        End Sub

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SynthesizedParameterSymbol" /> class.
        ''' </summary>
        ''' <param name="container">The containing symbol</param>
        ''' <param name="type">The type of this parameter</param>
        ''' <param name="ordinal">The ordinal number of this parameter</param>
        ''' <param name="isByRef">Whether the parameter is ByRef or not</param>
        ''' <param name="name">The name of this parameter</param>
        ''' <param name="customModifiers">The custom modifiers of this parameter</param>
        Public Sub New(container As MethodSymbol, type As TypeSymbol, ordinal As Integer, isByRef As Boolean, name As String, customModifiers As ReadOnlyArray(Of CustomModifier))
            Me._container = container
            Me._type = type
            Me._ordinal = ordinal
            Me._isByRef = isByRef
            Me._name = name
            Me._customModifiers = customModifiers.NullToEmpty()
        End Sub

        ''' <summary>
        ''' Gets the containing symbol.
        ''' </summary>
        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me._container
            End Get
        End Property

        ''' <summary>
        ''' The list of custom modifiers, if any, associated with the parameter. Evaluate this property only if IsModified is true.
        ''' </summary>
        Public Overrides ReadOnly Property CustomModifiers As ReadOnlyArray(Of CustomModifier)
            Get
                Return Me._customModifiers
            End Get
        End Property

        ''' <summary>
        ''' A compile time constant value that should be supplied as the corresponding argument value by callers that do not explicitly specify an argument value for this parameter.
        ''' </summary>
        Public Overrides ReadOnly Property DefaultValue As Object
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' True if the parameter has a default value that should be supplied as the argument value by a caller for which the argument value has not been explicitly specified.
        ''' </summary>
        Public Overrides ReadOnly Property HasDefaultValue As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is by ref.
        ''' </summary>
        ''' <value>
        '''   <c>true</c> if this instance is by ref; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return _isByRef
            End Get
        End Property

        ''' <summary>
        ''' True if the argument value must be included in the marshalled arguments passed to a remote callee only if it is different from the default value (if there is one).
        ''' </summary>
        Public Overrides ReadOnly Property IsOptional As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is param array.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is param array; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsParamArray As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' A potentially empty collection of locations that correspond to this instance.
        ''' </summary>
        Public Overrides ReadOnly Property Locations As ReadOnlyArray(Of Location)
            Get
                Return ReadOnlyArray(Of Location).Empty
            End Get
        End Property

        ''' <summary>
        ''' Gets the ordinal.
        ''' </summary>
        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _ordinal
            End Get
        End Property

        ''' <summary>
        ''' Gets the type.
        ''' </summary>
        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return _type
            End Get
        End Property

        ''' <summary>
        ''' Gets the name.
        ''' </summary>
        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend Overrides ReadOnly Property IsFromSource As Boolean
            Get
                Return True
            End Get
        End Property
    End Class

End Namespace
