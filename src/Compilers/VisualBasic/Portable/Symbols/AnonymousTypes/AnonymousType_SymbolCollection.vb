' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Public Function ReportMissingOrErroneousSymbols(diagnostics As BindingDiagnosticBag, hasClass As Boolean, hasDelegate As Boolean, hasKeys As Boolean) As Boolean
            Debug.Assert(hasClass OrElse hasDelegate)
            Debug.Assert(Not hasKeys OrElse hasClass)

            Dim hasErrors As Boolean = False

            ' All symbols used with and without keys fields both for classes and delegates
            ReportErrorOnSymbol(System_Object, diagnostics, hasErrors)
            ReportErrorOnSymbol(System_Void, diagnostics, hasErrors)

            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__ctor))
            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))

            Dim vbEmbedRuntime = Compilation.Options.EmbedVbCoreRuntime

            If hasDelegate Then
                ' All symbols used for delegates.
                ReportErrorOnSymbol(System_IntPtr, diagnostics, hasErrors)
                ReportErrorOnSymbol(System_IAsyncResult, diagnostics, hasErrors)
                ReportErrorOnSymbol(System_AsyncCallback, diagnostics, hasErrors)
                ReportErrorOnSymbol(System_MulticastDelegate, diagnostics, hasErrors)
            End If

            If hasClass Then
                ' All symbols used with and without keys fields for classes
                ReportErrorOnSymbol(System_Int32, diagnostics, hasErrors)
                ReportErrorOnSymbol(System_String, diagnostics, hasErrors)

                ReportErrorOnSpecialMember(System_Object__ToString, SpecialMember.System_Object__ToString, diagnostics, hasErrors, vbEmbedRuntime)
                ReportErrorOnSpecialMember(System_String__Format_IFormatProvider, SpecialMember.System_String__Format_IFormatProvider, diagnostics, hasErrors, vbEmbedRuntime)

                ' Only symbols used if there are Key fields
                If hasKeys Then
                    ReportErrorOnSymbol(System_Boolean, diagnostics, hasErrors)
                    ReportErrorOnSpecialMember(System_Object__GetHashCode, SpecialMember.System_Object__GetHashCode, diagnostics, hasErrors, vbEmbedRuntime)
                    ReportErrorOnSpecialMember(System_Object__Equals, SpecialMember.System_Object__Equals, diagnostics, hasErrors, vbEmbedRuntime)

                    ReportErrorOnSymbol(System_IEquatable_T, diagnostics, hasErrors)
                    ReportErrorOnSymbol(System_IEquatable_T_Equals, diagnostics, hasErrors)
                End If
            End If

            Return hasErrors
        End Function

        Private Shared Sub ReportErrorOnSymbol(symbol As Symbol, diagnostics As BindingDiagnosticBag, ByRef hasError As Boolean)
            If symbol IsNot Nothing Then
                Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = symbol.GetUseSiteInfo()
                If diagnostics.Add(useSiteInfo, NoLocation.Singleton) Then
                    hasError = True
                End If
            End If
        End Sub

        Private Shared Sub ReportErrorOnSpecialMember(symbol As Symbol, member As SpecialMember, diagnostics As BindingDiagnosticBag, ByRef hasError As Boolean, embedVBCore As Boolean)
            If symbol Is Nothing Then
                Dim memberDescriptor As MemberDescriptor = SpecialMembers.GetDescriptor(member)
                Dim diagInfo = GetDiagnosticForMissingRuntimeHelper(memberDescriptor.DeclaringTypeMetadataName, memberDescriptor.Name, embedVBCore)
                diagnostics.Add(diagInfo, NoLocation.Singleton)
                hasError = True
            Else
                ReportErrorOnSymbol(symbol, diagnostics, hasError)
            End If
        End Sub

        ''' <summary>
        ''' Checks if all special and well-known symbols required for emitting anonymous types 
        ''' provided exist, if not reports errors and returns True.
        ''' </summary>
        Private Function CheckAndReportMissingSymbols(anonymousTypes As ArrayBuilder(Of AnonymousTypeOrDelegateTemplateSymbol), diagnostics As BindingDiagnosticBag) As Boolean
            Dim hasClass As Boolean = False
            Dim hasDelegate As Boolean = False
            Dim hasKeys As Boolean = False
            For Each t In anonymousTypes
                Select Case t.TypeKind
                    Case TypeKind.Class
                        hasClass = True
                        If DirectCast(t, AnonymousTypeTemplateSymbol).HasAtLeastOneKeyField Then
                            hasKeys = True
                            If hasDelegate Then
                                Exit For
                            End If
                        End If
                    Case TypeKind.Delegate
                        hasDelegate = True
                        If hasKeys Then
                            Exit For
                        End If
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(t.TypeKind)
                End Select
            Next

            Return If(hasClass OrElse hasDelegate, Me.ReportMissingOrErroneousSymbols(diagnostics, hasClass, hasDelegate, hasKeys), True)
        End Function

        Public ReadOnly Property System_Boolean As NamedTypeSymbol
            Get
                Return Compilation.GetSpecialType(SpecialType.System_Boolean)
            End Get
        End Property

        Public ReadOnly Property System_Int32 As NamedTypeSymbol
            Get
                Return Compilation.GetSpecialType(SpecialType.System_Int32)
            End Get
        End Property

        Public ReadOnly Property System_Object As NamedTypeSymbol
            Get
                Return Compilation.GetSpecialType(SpecialType.System_Object)
            End Get
        End Property

        Public ReadOnly Property System_IntPtr As NamedTypeSymbol
            Get
                Return Compilation.GetSpecialType(SpecialType.System_IntPtr)
            End Get
        End Property

        Public ReadOnly Property System_IAsyncResult As NamedTypeSymbol
            Get
                Return Compilation.GetSpecialType(SpecialType.System_IAsyncResult)
            End Get
        End Property

        Public ReadOnly Property System_AsyncCallback As NamedTypeSymbol
            Get
                Return Compilation.GetSpecialType(SpecialType.System_AsyncCallback)
            End Get
        End Property

        Public ReadOnly Property System_MulticastDelegate As NamedTypeSymbol
            Get
                Return Compilation.GetSpecialType(SpecialType.System_MulticastDelegate)
            End Get
        End Property

        Public ReadOnly Property System_String As NamedTypeSymbol
            Get
                Return Compilation.GetSpecialType(SpecialType.System_String)
            End Get
        End Property

        Public ReadOnly Property System_Void As NamedTypeSymbol
            Get
                Return Compilation.GetSpecialType(SpecialType.System_Void)
            End Get
        End Property

        Public ReadOnly Property System_String__Format_IFormatProvider As MethodSymbol
            Get
                Return DirectCast(Compilation.GetSpecialTypeMember(SpecialMember.System_String__Format_IFormatProvider), MethodSymbol)
            End Get
        End Property

        Public ReadOnly Property System_Object__ToString As MethodSymbol
            Get
                Return DirectCast(Me.ContainingModule.ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Object__ToString), MethodSymbol)
            End Get
        End Property

        Public ReadOnly Property System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor As MethodSymbol
            Get
                Return DirectCast(Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor), MethodSymbol)
            End Get
        End Property

        Public ReadOnly Property System_Diagnostics_DebuggerDisplayAttribute__ctor As MethodSymbol
            Get
                Return DirectCast(Compilation.GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__ctor), MethodSymbol)
            End Get
        End Property

        Public ReadOnly Property System_Diagnostics_DebuggerDisplayAttribute__Type As PropertySymbol
            Get
                Return DirectCast(Compilation.GetWellKnownTypeMember(WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__Type), PropertySymbol)
            End Get
        End Property

        Public ReadOnly Property System_Object__GetHashCode As MethodSymbol
            Get
                Return DirectCast(Me.ContainingModule.ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Object__GetHashCode), MethodSymbol)
            End Get
        End Property

        Public ReadOnly Property System_Object__Equals As MethodSymbol
            Get
                Return DirectCast(Me.ContainingModule.ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Object__Equals), MethodSymbol)
            End Get
        End Property

        Public ReadOnly Property System_IEquatable_T As NamedTypeSymbol
            Get
                Return Compilation.GetWellKnownType(WellKnownType.System_IEquatable_T)
            End Get
        End Property

        Public ReadOnly Property System_IEquatable_T_Equals As MethodSymbol
            Get
                Return DirectCast(Compilation.GetWellKnownTypeMember(WellKnownMember.System_IEquatable_T__Equals), MethodSymbol)
            End Get
        End Property

    End Class

End Namespace
