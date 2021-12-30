' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Tracks synthesized fields that are needed in a submission being compiled.
    ''' </summary>
    ''' <remarks>
    ''' For every other submission referenced by this submission we add a field, so that we can access members of the target submission.
    ''' A field is also needed for the host object, if provided.
    ''' </remarks>
    Friend Class SynthesizedSubmissionFields
        Private ReadOnly _declaringSubmissionClass As NamedTypeSymbol
        Private ReadOnly _compilation As VisualBasicCompilation

        Private _hostObjectField As FieldSymbol
        Private _previousSubmissionFieldMap As Dictionary(Of ImplicitNamedTypeSymbol, FieldSymbol)

        Public Sub New(compilation As VisualBasicCompilation, submissionClass As NamedTypeSymbol)
            Debug.Assert(compilation IsNot Nothing)
            Debug.Assert(submissionClass.IsSubmissionClass)
            _declaringSubmissionClass = submissionClass
            _compilation = compilation
        End Sub

        Friend ReadOnly Property Count As Integer
            Get
                Return If(_previousSubmissionFieldMap Is Nothing, 0, _previousSubmissionFieldMap.Count)
            End Get
        End Property

        Friend ReadOnly Property FieldSymbols As IEnumerable(Of FieldSymbol)
            Get
                Return If(_previousSubmissionFieldMap Is Nothing,
                          Array.Empty(Of FieldSymbol)(),
                          DirectCast(_previousSubmissionFieldMap.Values, IEnumerable(Of FieldSymbol)))
            End Get
        End Property

        Friend Function GetHostObjectField() As FieldSymbol
            If _hostObjectField IsNot Nothing Then
                Return _hostObjectField
            End If

            ' TODO (tomat): Dim hostObjectTypeSymbol = compilation.GetHostObjectTypeSymbol()
            Dim hostObjectTypeSymbol As TypeSymbol = Nothing
            If hostObjectTypeSymbol IsNot Nothing AndAlso hostObjectTypeSymbol.Kind <> SymbolKind.ErrorType Then
                _hostObjectField = New SynthesizedFieldSymbol(_declaringSubmissionClass, _declaringSubmissionClass, hostObjectTypeSymbol, "<host-object>", accessibility:=Accessibility.Private, isReadOnly:=True, isShared:=False)
                Return _hostObjectField
            End If

            Return Nothing
        End Function

        Friend Function GetOrMakeField(previousSubmissionType As ImplicitNamedTypeSymbol) As FieldSymbol
            If _previousSubmissionFieldMap Is Nothing Then
                _previousSubmissionFieldMap = New Dictionary(Of ImplicitNamedTypeSymbol, FieldSymbol)()
            End If

            Dim previousSubmissionField As FieldSymbol = Nothing
            If Not _previousSubmissionFieldMap.TryGetValue(previousSubmissionType, previousSubmissionField) Then
                previousSubmissionField = New SynthesizedFieldSymbol(
                    _declaringSubmissionClass,
                    implicitlyDefinedBy:=_declaringSubmissionClass,
                    Type:=previousSubmissionType,
                    name:="<" + previousSubmissionType.Name + ">",
                    isReadOnly:=True)

                _previousSubmissionFieldMap.Add(previousSubmissionType, previousSubmissionField)
            End If

            Return previousSubmissionField
        End Function

        Friend Sub AddToType(containingType As NamedTypeSymbol, moduleBeingBuilt As PEModuleBuilder)
            For Each field In FieldSymbols
                moduleBeingBuilt.AddSynthesizedDefinition(containingType, field.GetCciAdapter())
            Next

            Dim hostObjectField As FieldSymbol = GetHostObjectField()
            If hostObjectField IsNot Nothing Then
                moduleBeingBuilt.AddSynthesizedDefinition(containingType, hostObjectField.GetCciAdapter())
            End If
        End Sub
    End Class
End Namespace

