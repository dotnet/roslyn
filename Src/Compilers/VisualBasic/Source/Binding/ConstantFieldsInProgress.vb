Imports System.Diagnostics

Namespace Roslyn.Compilers.VisualBasic

    ''' <summary>
    ''' This is used while computing the values of constant fields.  Since they can depend on each other,
    ''' we need to keep track of which ones we are currently computing in order to avoid (and report) cycles.
    ''' </summary>
    Friend NotInheritable Class ConstantFieldsInProgress

        Private ReadOnly _fields As IImmutableSet(Of FieldSymbol)

        Friend Shared ReadOnly Empty As ConstantFieldsInProgress = New ConstantFieldsInProgress(ImmutableSet(Of FieldSymbol).Empty)

        Private Sub New(fields As IImmutableSet(Of FieldSymbol))
            Me._fields = fields
        End Sub

        Friend Function Add(field As FieldSymbol) As ConstantFieldsInProgress
            Debug.Assert(field IsNot Nothing)
            Debug.Assert((TypeOf (field) Is SourceFieldSymbol) OrElse (TypeOf (field) Is SynthesizedEnumConstantSymbol))

            Return New ConstantFieldsInProgress(Me._fields.Add(field))
        End Function

        Friend Function GetStartOfCycleIfAny(field As FieldSymbol) As FieldSymbol
            If Not _fields.Contains(field) Then
                Return Nothing
            End If

            ' Return the field from the cycle with the best error location.
            ' _fields will contain all dependent fields, potentially including
            ' fields that are not part of the cycle. (For instance, when evaluating A
            ' in Enum E : A = B : B = C : C = B : End Enum, the set of fields will be
            ' { A, B, C } although only { B, C } represent a cycle.) The loop below
            ' skips any fields before the cycle (before the occurrence of 'field').
            Dim errorField As FieldSymbol = Nothing
            For Each orderedField In _fields.InOrder
                If orderedField = field Then
                    Debug.Assert(errorField Is Nothing)
                    errorField = orderedField
                ElseIf errorField IsNot Nothing Then
                    If IsBetterErrorLocation(errorField, orderedField) Then
                        errorField = orderedField
                    End If
                End If
            Next

            Debug.Assert(errorField IsNot Nothing)
            Return errorField
        End Function

        Private Shared Function IsBetterErrorLocation(errorField As FieldSymbol, field As FieldSymbol) As Boolean
            ' Ignore locations from other compilations.
            Dim compilation = DirectCast(field.ContainingAssembly, SourceAssemblySymbol).Compilation
            Dim errorFieldCompilation = DirectCast(errorField.ContainingAssembly, SourceAssemblySymbol).Compilation
            Return (compilation Is errorFieldCompilation) AndAlso (compilation.CompareSourceLocations(errorField.Locations(0), field.Locations(0)) > 0)
        End Function

    End Class
End Namespace

