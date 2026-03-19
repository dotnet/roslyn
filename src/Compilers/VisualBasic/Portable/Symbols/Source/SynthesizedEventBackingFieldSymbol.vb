' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a compiler generated backing field for an event.
    ''' </summary>
    Friend NotInheritable Class SynthesizedEventBackingFieldSymbol
        Inherits SynthesizedBackingFieldBase(Of SourceEventSymbol)

        Private _lazyType As TypeSymbol

        Public Sub New(propertyOrEvent As SourceEventSymbol, name As String, isShared As Boolean)
            MyBase.New(propertyOrEvent, name, isShared)
        End Sub

        ''' <summary>
        ''' System.NonSerializedAttribute applied on an event and determines serializability of its backing field.
        ''' </summary>
        Friend Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Dim eventData = _propertyOrEvent.GetDecodedWellKnownAttributeData()
                Return eventData IsNot Nothing AndAlso eventData.HasNonSerializedAttribute
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                If _lazyType Is Nothing Then

                    Dim diagnostics = BindingDiagnosticBag.GetInstance()
                    Dim result = _propertyOrEvent.Type

                    If _propertyOrEvent.IsWindowsRuntimeEvent Then
                        Dim tokenType = Me.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T)
                        diagnostics.Add(Binder.GetUseSiteInfoForWellKnownType(tokenType), _propertyOrEvent.GetFirstLocation())

                        result = tokenType.Construct(result)
                    End If

                    DirectCast(ContainingModule, SourceModuleSymbol).AtomicStoreReferenceAndDiagnostics(_lazyType, result, diagnostics)
                    diagnostics.Free()
                End If

                Debug.Assert(_lazyType IsNot Nothing)
                Return _lazyType
            End Get
        End Property

        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            MyBase.GenerateDeclarationErrors(cancellationToken)

            cancellationToken.ThrowIfCancellationRequested()
            Dim unusedType = Me.Type
        End Sub
    End Class
End Namespace
