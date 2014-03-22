' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.Serialization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class DiagnosticAnalyzerTests
        Inherits BasicTestBase

        <Serializable>
        Class TestDiagnostic
            Inherits Diagnostic
            Implements ISerializable

            Private ReadOnly m_id As String
            Private ReadOnly m_kind As String
            Private ReadOnly m_severity As DiagnosticSeverity
            Private ReadOnly m_location As Location
            Private ReadOnly m_message As String
            Private ReadOnly m_isWarningAsError As Boolean
            Private ReadOnly m_arguments As Object()

            Public Sub New(id As String,
                           kind As String,
                           severity As DiagnosticSeverity,
                           location As Location,
                           message As String,
                           isWarningAsError As Boolean,
                           ParamArray arguments As Object())
                Me.m_id = id
                Me.m_kind = kind
                Me.m_severity = severity
                Me.m_location = location
                Me.m_message = message
                Me.m_isWarningAsError = isWarningAsError
                Me.m_arguments = arguments
            End Sub

            Private Sub New(info As SerializationInfo, context As StreamingContext)
                Me.m_id = info.GetString("id")
                Me.m_kind = info.GetString("kind")
                Me.m_message = info.GetString("message")
                Me.m_location = CType(info.GetValue("location", GetType(Location)), Location)
                Me.m_severity = CType(info.GetValue("severity", GetType(DiagnosticSeverity)), DiagnosticSeverity)
                Me.m_isWarningAsError = info.GetBoolean("isWarningAsError")
                Me.m_arguments = CType(info.GetValue("arguments", GetType(Object())), Object())
            End Sub

            Public Overrides ReadOnly Property AdditionalLocations As IReadOnlyList(Of Location)
                Get
                    Dim loc As Location() = New Location(0) {}
                    Return loc
                End Get
            End Property

            Public Overrides ReadOnly Property Id As String
                Get
                    Return m_id
                End Get
            End Property

            Public Overrides ReadOnly Property Category As String
                Get
                    Return m_id
                End Get
            End Property

            Public Overrides ReadOnly Property Location As Location
                Get
                    Return m_location
                End Get
            End Property

            Public Overrides ReadOnly Property Severity As DiagnosticSeverity
                Get
                    Return m_severity
                End Get
            End Property

            Public Overrides ReadOnly Property WarningLevel As Integer
                Get
                    Return 2
                End Get
            End Property

            Public Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
                info.AddValue("id", Me.m_id)
                info.AddValue("kind", Me.m_kind)
                info.AddValue("message", Me.m_message)
                info.AddValue("location", Me.m_location, GetType(Location))
                info.AddValue("severity", Me.m_severity, GetType(DiagnosticSeverity))
                info.AddValue("isWarningAsError", Me.m_isWarningAsError)
                info.AddValue("arguments", Me.m_arguments, GetType(Object()))
            End Sub

            Friend Overrides Function WithLocation(location As Location) As Diagnostic
                Throw New NotImplementedException()
            End Function

            Friend Overrides Function WithWarningAsError(isWarningAsError As Boolean) As Diagnostic
                If isWarningAsError AndAlso Severity = DiagnosticSeverity.Warning Then
                    Return New TestDiagnostic(Id, Category, DiagnosticSeverity.Error, m_location, m_message, isWarningAsError, m_arguments)
                Else
                    Return Me
                End If
            End Function

            Public Overrides Function GetMessage(Optional culture As CultureInfo = Nothing) As String
                Return String.Format(m_message, m_arguments)
            End Function

            Public Overrides Function Equals(obj As Diagnostic) As Boolean
                If obj Is Nothing OrElse Me.GetType() <> obj.GetType() Then Return False
                Dim other As TestDiagnostic = CType(obj, TestDiagnostic)
                Return Me.m_id = other.m_id AndAlso
                    Me.m_kind = other.m_kind AndAlso
                    Me.m_location = other.m_location AndAlso
                    Me.m_message = other.m_message AndAlso
                    SameData(Me.m_arguments, other.m_arguments)
            End Function

            Private Shared Function SameData(d1 As Object(), d2 As Object()) As Boolean
                Return (d1 Is Nothing) = (d2 Is Nothing) AndAlso (d1 Is Nothing OrElse d1.SequenceEqual(d2))
            End Function
        End Class

        Class ComplainAboutX
            Implements ISyntaxNodeAnalyzer(Of SyntaxKind)

            Private Shared ReadOnly m_kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(Of SyntaxKind)(SyntaxKind.IdentifierName)

            Public ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).SyntaxKindsOfInterest
                Get
                    Return m_kindsOfInterest
                End Get
            End Property

            Private Shared ReadOnly CA9999_UseOfVariableThatStartsWithX As DiagnosticDescriptor = New DiagnosticDescriptor(id:="CA9999", description:="CA9999_UseOfVariableThatStartsWithX", messageFormat:="Use of variable whose name starts with 'x': '{0}'", category:="Test", defaultSeverity:=DiagnosticSeverity.Warning)

            Private ReadOnly Property IDiagnosticAnalyzer_SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor) Implements IDiagnosticAnalyzer.SupportedDiagnostics
                Get
                    Return ImmutableArray.Create(CA9999_UseOfVariableThatStartsWithX)
                End Get
            End Property

            Public Sub AnalyzeNode(node As SyntaxNode, semanticModel As SemanticModel, addDiagnostic As Action(Of Diagnostic), cancellationToken As CancellationToken) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).AnalyzeNode
                Dim id = CType(node, IdentifierNameSyntax)
                If id.Identifier.ValueText.StartsWith("x") Then
                    addDiagnostic(New TestDiagnostic("CA9999_UseOfVariableThatStartsWithX", "CsTest", DiagnosticSeverity.Warning, id.GetLocation(), "Use of variable whose name starts with 'x': '{0}'", False, id.Identifier.ValueText))
                End If
            End Sub
        End Class
    End Class
End Namespace
