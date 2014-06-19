Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

Namespace Roslyn.Compilers.VisualBasic

    Friend Module ConversionKindExtensions
        <Extension()>
        Public Function IsImplicitConversion(ByVal kind As ConversionKind) As Boolean
            Select Case kind
                Case ConversionKind.AnonymousFunction, ConversionKind.Boxing, ConversionKind.Identity, ConversionKind.ImplicitConstant, ConversionKind.ImplicitEnumeration, ConversionKind.ImplicitNullable, ConversionKind.ImplicitNumeric, ConversionKind.ImplicitReference, ConversionKind.ImplicitUserDefined, ConversionKind.MethodGroup, ConversionKind.NullLiteral
                    Return True
                Case ConversionKind.ExplicitEnumeration, ConversionKind.ExplicitNullable, ConversionKind.ExplicitNumeric, ConversionKind.ExplicitReference, ConversionKind.ExplicitUserDefined, ConversionKind.NoConversion, ConversionKind.Unboxing
                    Return False
                Case Else
                    Debug.Fail("Bad conversion kind!")
                    Return False
            End Select
        End Function
    End Module
End Namespace

