Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

Namespace Roslyn.Compilers.VisualBasic

    ' TODO: These were ported from C#. Remove if we will not use them at the end.
    Friend Module TypeParameterSymbolExtensions

        <Extension()>
        Public Function DependsOn(typeParameter1 As TypeParameterSymbol, typeParameter2 As TypeSymbol) As Boolean
            Debug.Assert(typeParameter1 IsNot Nothing)
            Debug.Assert(typeParameter2 IsNot Nothing)
            Dim t2 As TypeParameterSymbol = TryCast(typeParameter2, TypeParameterSymbol)
            If t2 Is Nothing Then
                Return False
            End If
            Dim dependencies As Func(Of TypeParameterSymbol, IEnumerable(Of TypeParameterSymbol)) = Function(x) x.ConstraintTypes.OfType(Of TypeParameterSymbol)()
            Return dependencies.TransitiveClosure(typeParameter1).Contains(t2)
        End Function

        <Extension()>
        Public Function GetEffectiveBaseClass(type As TypeParameterSymbol) As NamedTypeSymbol
            Return type.BaseType
        End Function
    End Module
End Namespace

