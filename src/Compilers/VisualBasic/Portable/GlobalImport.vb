' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Represents a Visual Basic global imports.
    ''' </summary>
    <DebuggerDisplay("{GetDebuggerDisplay(), nq}")>
    Partial Public NotInheritable Class GlobalImport
        Implements IEquatable(Of GlobalImport)

        Private ReadOnly _clause As SyntaxReference
        Private ReadOnly _importedName As String

        Friend Sub New(clause As ImportsClauseSyntax, importedName As String)
            Debug.Assert(clause IsNot Nothing)
            Debug.Assert(importedName IsNot Nothing)
            _clause = clause.SyntaxTree.GetReference(clause)
            _importedName = importedName
        End Sub

        ''' <summary>
        ''' The import clause (a namespace name, an alias, or an XML namespace alias).
        ''' </summary>
        Public ReadOnly Property Clause As ImportsClauseSyntax
            Get
                Return CType(_clause.GetSyntax(), ImportsClauseSyntax)
            End Get
        End Property

        Friend ReadOnly Property IsXmlClause As Boolean
            Get
                Return Clause.IsKind(SyntaxKind.XmlNamespaceImportsClause)
            End Get
        End Property

        ''' <summary>
        ''' The import name.
        ''' </summary>
        Public ReadOnly Property Name As String
            Get
                Return _importedName
            End Get
        End Property

        ''' <summary>
        ''' Parses a specified string to create a GlobalImport instance.
        ''' </summary>
        ''' <param name="importedNames">The string containing the text to be parsed.</param>
        ''' <returns>A GlobalImport instance. </returns>
        Public Shared Function Parse(importedNames As String) As GlobalImport
            Return Parse({importedNames})(0)
        End Function

        ''' <summary>
        ''' Parses a specified string to create a GlobalImport instance with diagnostics info.
        ''' </summary>
        ''' <param name="importedNames">The string containing the text to be parsed.</param>
        ''' <param name="diagnostics">An ImmutableArray of diagnostics created during parse.</param>
        ''' <returns>A GlobalImport instance.</returns>
        Public Shared Function Parse(importedNames As String, <Out()> ByRef diagnostics As ImmutableArray(Of Diagnostic)) As GlobalImport
            Return Parse({importedNames}, diagnostics)(0)
        End Function

        ''' <summary>
        ''' Parses a collection of strings representing imports to create collection of GlobalImports.
        ''' </summary>
        ''' <param name="importedNames">The collection of strings to be parsed.</param>
        ''' <returns>A collection of GlobalImports</returns>
        Public Shared Function Parse(importedNames As IEnumerable(Of String)) As IEnumerable(Of GlobalImport)
            Dim errors As DiagnosticBag = DiagnosticBag.GetInstance()
            Dim parsedImports = OptionsValidator.ParseImports(importedNames, errors)
            Dim firstError = errors.AsEnumerable().FirstOrDefault(Function(diag) diag.Severity = DiagnosticSeverity.Error)
            errors.Free()
            If firstError IsNot Nothing Then
                Throw New ArgumentException(firstError.GetMessage(CultureInfo.CurrentUICulture))
            End If
            Return parsedImports
        End Function

        ''' <summary>
        ''' Parses a parameter array of string arrays representing imports to create a collection of GlobalImports.
        ''' </summary>
        ''' <param name="importedNames">The string arrays to be parsed.</param>
        ''' <returns>A collection of GlobalImports.</returns>
        Public Shared Function Parse(ParamArray importedNames As String()) As IEnumerable(Of GlobalImport)
            Return Parse(DirectCast(importedNames, IEnumerable(Of String)))
        End Function

        ''' <summary>
        ''' Parses a collection of strings representing imports to create a collection of GlobalImport instance and diagnostics
        ''' </summary>
        ''' <param name="importedNames">A collection of strings to be parsed.</param>
        ''' <param name="diagnostics">A ImmutableArray of diagnostics.</param>
        ''' <returns>A collection of GlobalImports.</returns>
        Public Shared Function Parse(importedNames As IEnumerable(Of String), <Out()> ByRef diagnostics As ImmutableArray(Of Diagnostic)) As IEnumerable(Of GlobalImport)
            Dim errors As DiagnosticBag = DiagnosticBag.GetInstance()
            Dim parsedImports = OptionsValidator.ParseImports(importedNames, errors)
            diagnostics = errors.ToReadOnlyAndFree(Of Diagnostic)()
            Return parsedImports
        End Function

        ' Map a diagnostic to the diagnostic we want to give.
        Friend Function MapDiagnostic(unmappedDiag As Diagnostic) As Diagnostic
            If unmappedDiag.Code = ERRID.WRN_UndefinedOrEmptyNamespaceOrClass1 Then
                Return New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.WRN_UndefinedOrEmptyProjectNamespaceOrClass1, _importedName), NoLocation.Singleton)
            Else
                ' Determine the text of the import, plus the startIndex/length within that text
                ' that the error is.
                Dim unmappedSpan = unmappedDiag.Location.SourceSpan
                Dim startindex = unmappedSpan.Start - _clause.Span.Start
                Dim length = unmappedSpan.Length
                If (startindex < 0 OrElse length <= 0 OrElse startindex >= _importedName.Length) Then
                    ' startIndex, length are bad for some reason. Used the whole import text instead.
                    startindex = 0
                    length = _importedName.Length
                End If
                length = Math.Min(_importedName.Length - startindex, length)

                ' Create a diagnostic with no location that wrapped the actual parser diagnostic.
                Return New VBDiagnostic(New ImportDiagnosticInfo(DirectCast(unmappedDiag, DiagnosticWithInfo).Info, _importedName, startindex, length), NoLocation.Singleton)
            End If
        End Function

        Private Function GetDebuggerDisplay() As String
            Return Name
        End Function

        ''' <summary>
        ''' Determines if the current object is equal to another object.
        ''' </summary>
        ''' <param name="obj">An object to compare with this object</param>
        ''' <returns>A boolean value.  True if the current object is equal to the other parameter; otherwise, False.</returns>
        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, GlobalImport))
        End Function


        ''' <summary>
        ''' Determines whether the current object is equal to another object of the same type.
        ''' </summary>
        ''' <param name="other">A GlobalImport object to compare with this object</param>
        ''' <returns>A boolean value.  True if the current object is equal to the other parameter; otherwise, False.</returns>
        Public Overloads Function Equals(other As GlobalImport) As Boolean Implements IEquatable(Of GlobalImport).Equals
            If Me Is other Then
                Return True
            End If

            If other Is Nothing Then
                Return False
            End If

            Return String.Equals(Me.Name, other.Name, StringComparison.Ordinal) AndAlso
                String.Equals(Me.Clause.ToFullString(), other.Clause.ToFullString(), StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Creates a hashcode for this instance.
        ''' </summary>
        ''' <returns>A hashcode representing this instance.</returns>
        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Me.Name.GetHashCode(), StringComparer.Ordinal.GetHashCode(Me.Clause.ToFullString()))
        End Function

        ''' <summary>
        ''' Operator for Equality with GlobalImport Objects.
        ''' </summary>
        ''' <returns>True if the two items are Equal.</returns>
        Public Shared Operator =(left As GlobalImport, right As GlobalImport) As Boolean
            Return Object.Equals(left, right)
        End Operator

        ''' <summary>
        ''' Overloaded Operator for Inequality ith GlobalImport Objects.
        ''' </summary>
        ''' <returns>Returns True if the two items are not Equal.</returns>
        Public Shared Operator <>(left As GlobalImport, right As GlobalImport) As Boolean
            Return Not Object.Equals(left, right)
        End Operator
    End Class
End Namespace
