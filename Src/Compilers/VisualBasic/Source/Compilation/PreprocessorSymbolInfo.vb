Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.Common.Semantics
Imports Microsoft.CodeAnalysis.Common.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Semantics
    ''' <summary>
    '''  Structure containing all semantic information about a preprocessor symbol.
    ''' </summary>
    Public Structure PreprocessorSymbolInfo

        Private ReadOnly m_name As String
        Private ReadOnly m_value As Object
        Private ReadOnly m_valueTypeCode As TypeCode

        ''' <summary>
        ''' Initializes a new instance of the <see cref="PreprocessorSymbolInfo" /> structure.
        ''' </summary>
        Friend Sub New(name As String, value As Object, valueTypeCode As TypeCode)
            m_name = name
            m_value = value
            m_valueTypeCode = valueTypeCode
        End Sub

        ''' <summary>
        ''' Gets the name of the symbol.
        ''' </summary>
        Public ReadOnly Property Name As String
            Get
                Return m_name
            End Get
        End Property

        ''' <summary>
        ''' Gets the constant value assigned to the symbol.
        ''' </summary>
        Public ReadOnly Property Value As Object
            Get
                Return m_value
            End Get
        End Property

        ''' <summary>
        ''' Gets the <see cref="TypeCode"/> of the constant value assigned to the symbol.
        ''' </summary>
        Public ReadOnly Property ValueTypeCode As TypeCode
            Get
                Return m_valueTypeCode
            End Get
        End Property
    End Structure
End Namespace