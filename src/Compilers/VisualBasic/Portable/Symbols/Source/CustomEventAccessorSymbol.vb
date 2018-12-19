' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend NotInheritable Class CustomEventAccessorSymbol
        Inherits SourceNonPropertyAccessorMethodSymbol

        Private ReadOnly _event As SourceEventSymbol
        Private ReadOnly _name As String
        Private _lazyExplicitImplementations As ImmutableArray(Of MethodSymbol) ' lazily populated with explicit implementations

        Friend Sub New(container As SourceMemberContainerTypeSymbol,
                       [event] As SourceEventSymbol,
                       name As String,
                       flags As SourceMemberFlags,
                       syntaxRef As SyntaxReference,
                       location As Location)
            MyBase.New(container, flags, syntaxRef, locations:=ImmutableArray.Create(location))

            _event = [event]
            _name = name
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                ' Event symbols aren't affected if the output kind is winmd, mark false
                ' (N.B., events only emits helpers named add_ and remove_, not set_)
                Return Binder.GetAccessorName(_event.MetadataName, Me.MethodKind, isWinMd:=False)
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                ' Raise is always private
                If Me.MethodKind = MethodKind.EventRaise Then
                    Return Accessibility.Private
                End If

                Return _event.DeclaredAccessibility
            End Get
        End Property

        Protected Overrides Function GetParameters(sourceModule As SourceModuleSymbol, diagBag As DiagnosticBag) As ImmutableArray(Of ParameterSymbol)
            Dim type = DirectCast(Me.ContainingType, SourceMemberContainerTypeSymbol)
            Dim binder As Binder = BinderBuilder.CreateBinderForType(sourceModule, Me.SyntaxTree, type)
            binder = New LocationSpecificBinder(BindingLocation.EventAccessorSignature, Me, binder)

            Return BindParameters(Me.Locations.FirstOrDefault, binder, BlockSyntax.BlockStatement.ParameterList, diagBag)
        End Function

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return _event
            End Get

        End Property

        Friend Overrides ReadOnly Property ShadowsExplicitly As Boolean
            Get
                Return _event.ShadowsExplicitly
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                If _lazyExplicitImplementations.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(
                        _lazyExplicitImplementations,
                        _event.GetAccessorImplementations(Me.MethodKind),
                        Nothing)
                End If

                Return _lazyExplicitImplementations
            End Get
        End Property

        Public Overrides ReadOnly Property OverriddenMethod As MethodSymbol
            Get
                ' custom event methods do not override
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                ' custom event methods do not have explicit returns and never implement or override
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                ' Event accessors can never be an extension method.
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property MayBeReducibleExtensionMethod As Boolean
            Get
                ' Event accessors can never be an extension method.
                Return False
            End Get
        End Property

        Protected Overrides Function GetAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            Return OneOrMany.Create(AttributeDeclarationSyntaxList)
        End Function

        Protected Overrides Function GetReturnTypeAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            Return Nothing
        End Function

        ''' <remarks>
        ''' We're going to ignore SourceMemberFlags.MethodIsSub and override IsSub explicitly.  We do this because
        ''' the flags have to be set at construction time, but IsSub depends on IsWindowsRuntimeEvent, which depends
        ''' on interface implementations, which we don't want to bind until the member list is complete.  (It's probably 
        ''' okay now (2012/12/17), but it would be very fragile to take a dependency on the exact mechanism by which
        '''  interface members are looked up.)
        ''' </remarks>
        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return Not (Me.MethodKind = MethodKind.EventAdd AndAlso _event.IsWindowsRuntimeEvent)
            End Get
        End Property

        ''' <summary>
        ''' Bind and validate parameters declared on the accessor.
        ''' </summary>
        Private Function BindParameters(location As Location,
                                        binder As Binder,
                                        parameterListOpt As ParameterListSyntax,
                                        diagnostics As DiagnosticBag) As ImmutableArray(Of ParameterSymbol)

            Dim parameterListSyntax = If(parameterListOpt Is Nothing, Nothing, parameterListOpt.Parameters)
            Dim nParameters = parameterListSyntax.Count
            Dim paramBuilder = ArrayBuilder(Of ParameterSymbol).GetInstance(nParameters)

            ' Bind all parameters (even though we know how many to expect), 
            ' to ensure all diagnostics are generated and ensure parameter symbols are available for binding the method body.
            binder.DecodeParameterList(
                Me,
                False,
                SourceMemberFlags.None,
                parameterListSyntax,
                paramBuilder,
                If(Me.MethodKind = MethodKind.EventRaise,
                   s_checkRaiseParameterModifierCallback,
                   s_checkAddRemoveParameterModifierCallback),
                diagnostics)

            Dim parameters = paramBuilder.ToImmutableAndFree()

            If Me.MethodKind = MethodKind.EventRaise Then
                ' Dev10 does something weird here - it checks for method conversion, but does it 
                ' backwards - it allows delegate Invoke to be more specific than signature of Raise. 
                ' Example: delegate may take int, but Raise may take long argument.
                '
                ' For backwards compatibility we will do the same.
                '
                ' NOTE: no change in raise event shape for WinRT events.
                Dim eventType = TryCast(_event.Type, NamedTypeSymbol)
                If eventType IsNot Nothing AndAlso
                    Not eventType.IsErrorType Then

                    Dim delInvoke = eventType.DelegateInvokeMethod

                    ' If delegate is a function method we should already have diagnostics about that
                    If delInvoke IsNot Nothing AndAlso delInvoke.IsSub Then
                        Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                        Dim conversion = Conversions.ClassifyMethodConversionForEventRaise(
                                                            delInvoke,
                                                            parameters,
                                                            useSiteDiagnostics)

                        If Not diagnostics.Add(location, useSiteDiagnostics) AndAlso
                            (Not Conversions.IsDelegateRelaxationSupportedFor(conversion) OrElse
                             (binder.OptionStrict = OptionStrict.On AndAlso Conversions.IsNarrowingMethodConversion(conversion, False))) Then

                            diagnostics.Add(ERRID.ERR_RaiseEventShapeMismatch1, location, eventType)
                        End If

                    End If
                End If
            Else
                If parameters.Length <> 1 Then
                    diagnostics.Add(ERRID.ERR_EventAddRemoveHasOnlyOneParam, location)
                Else
                    Dim eventType = _event.Type
                    Debug.Assert(eventType IsNot Nothing)
                    Dim parameterType = parameters(0).Type
                    Debug.Assert(parameterType IsNot Nothing)

                    If Me.MethodKind = MethodKind.EventAdd Then
                        If Not eventType.IsErrorType AndAlso Not TypeSymbol.Equals(eventType, parameterType, TypeCompareKind.ConsiderEverything) Then
                            Dim errid As ERRID = If(_event.IsWindowsRuntimeEvent, ERRID.ERR_AddParamWrongForWinRT, ERRID.ERR_AddRemoveParamNotEventType)
                            diagnostics.Add(errid, location)
                        End If
                    Else
                        Debug.Assert(Me.MethodKind = MethodKind.EventRemove)

                        If _event.ExplicitInterfaceImplementations.Any() Then
                            ' Reporting diagnostics when this type is missing will only ever result in cascading, so don't bother.
                            Dim registrationTokenType As NamedTypeSymbol =
                                binder.Compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken)

                            Dim firstImplementedEvent As EventSymbol = _event.ExplicitInterfaceImplementations(0)
                            If Not registrationTokenType.IsErrorType AndAlso firstImplementedEvent.IsWindowsRuntimeEvent <> (TypeSymbol.Equals(parameterType, registrationTokenType, TypeCompareKind.ConsiderEverything)) Then
                                diagnostics.Add(ERRID.ERR_EventImplRemoveHandlerParamWrong, location, _event.Name, firstImplementedEvent.Name, firstImplementedEvent.ContainingType)
                            End If
                        ElseIf _event.IsWindowsRuntimeEvent Then
                            ' Reporting diagnostics when this type is missing will only ever result in cascading, so don't bother.
                            Dim registrationTokenType As NamedTypeSymbol =
                                binder.Compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken)
                            If Not registrationTokenType.IsErrorType AndAlso Not TypeSymbol.Equals(parameterType, registrationTokenType, TypeCompareKind.ConsiderEverything) Then
                                diagnostics.Add(ERRID.ERR_RemoveParamWrongForWinRT, location)
                            End If
                        Else
                            If Not eventType.IsErrorType AndAlso Not TypeSymbol.Equals(eventType, parameterType, TypeCompareKind.ConsiderEverything) Then
                                diagnostics.Add(ERRID.ERR_AddRemoveParamNotEventType, location)
                            End If
                        End If
                    End If
                End If
            End If

            Return parameters
        End Function

        Private Shared ReadOnly s_checkAddRemoveParameterModifierCallback As Binder.CheckParameterModifierDelegate = AddressOf CheckAddRemoveParameterModifier
        Private Shared ReadOnly s_checkRaiseParameterModifierCallback As Binder.CheckParameterModifierDelegate = AddressOf CheckEventMethodParameterModifier

        ' applicable to all event methods
        Private Shared Function CheckEventMethodParameterModifier(container As Symbol, token As SyntaxToken, flag As SourceParameterFlags, diagnostics As DiagnosticBag) As SourceParameterFlags
            If (flag And SourceParameterFlags.Optional) <> 0 Then
                Dim location = token.GetLocation()
                diagnostics.Add(ERRID.ERR_EventMethodOptionalParamIllegal1, location, token.ToString())
                flag = flag And (Not SourceParameterFlags.Optional)
            End If

            If (flag And SourceParameterFlags.ParamArray) <> 0 Then
                Dim location = token.GetLocation()
                diagnostics.Add(ERRID.ERR_EventMethodOptionalParamIllegal1, location, token.ToString())
                flag = flag And (Not SourceParameterFlags.ParamArray)
            End If

            Return flag
        End Function

        ' additional rules for Add and Remove
        Private Shared Function CheckAddRemoveParameterModifier(container As Symbol, token As SyntaxToken, flag As SourceParameterFlags, diagnostics As DiagnosticBag) As SourceParameterFlags
            If (flag And SourceParameterFlags.ByRef) <> 0 Then
                Dim location = token.GetLocation()
                diagnostics.Add(ERRID.ERR_EventAddRemoveByrefParamIllegal, location, token.ToString())
                flag = flag And (Not SourceParameterFlags.ByRef)
            End If

            Return CheckEventMethodParameterModifier(container, token, flag, diagnostics)
        End Function
    End Class

End Namespace
