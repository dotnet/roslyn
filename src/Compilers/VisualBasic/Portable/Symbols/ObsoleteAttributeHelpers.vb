' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics
Imports System.Reflection.Metadata
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Enum ObsoleteDiagnosticKind
        NotObsolete
        Suppressed
        Diagnostic
        Lazy
        LazyPotentiallySuppressed
    End Enum

    Friend NotInheritable Class ObsoleteAttributeHelpers

        ''' <summary>
        ''' Initialize the ObsoleteAttributeData by fetching attributes and decoding ObsoleteAttributeData. This can be 
        ''' done for Metadata symbol easily whereas trying to do this for source symbols could result in cycles.
        ''' </summary>
        Friend Shared Sub InitializeObsoleteDataFromMetadata(ByRef data As ObsoleteAttributeData, token As EntityHandle, containingModule As PEModuleSymbol)
            If data Is ObsoleteAttributeData.Uninitialized Then
                Dim obsoleteAttributeData As ObsoleteAttributeData = GetObsoleteDataFromMetadata(token, containingModule)
                Interlocked.CompareExchange(data, obsoleteAttributeData, ObsoleteAttributeData.Uninitialized)
            End If
        End Sub

        Friend Shared Function GetObsoleteDataFromMetadata(token As EntityHandle, containingModule As PEModuleSymbol) As ObsoleteAttributeData
            Dim obsoleteAttributeData As ObsoleteAttributeData = Nothing
            ' ignoreByRefLikeMarker := False, since VB does not support ref-like types
            obsoleteAttributeData = containingModule.Module.TryGetDeprecatedOrExperimentalOrObsoleteAttribute(token, New MetadataDecoder(containingModule), ignoreByRefLikeMarker:=False, ignoreRequiredMemberMarker:=True)
            Debug.Assert(obsoleteAttributeData Is Nothing OrElse Not obsoleteAttributeData.IsUninitialized)
            Return obsoleteAttributeData
        End Function

        ''' <summary>
        ''' This method checks to see if the given symbol is Obsolete or if any symbol in the parent hierarchy is Obsolete.
        ''' </summary>
        ''' <returns>
        ''' True if some symbol in the parent hierarchy is known to be Obsolete. Unknown if any
        ''' symbol's Obsoleteness is Unknown. False, if we are certain that no symbol in the parent
        ''' hierarchy is Obsolete.
        ''' </returns>
        Private Shared Function GetObsoleteContextState(symbol As Symbol, forceComplete As Boolean) As ThreeState
            While symbol IsNot Nothing
                If forceComplete Then
                    symbol.ForceCompleteObsoleteAttribute()
                End If

                Dim state = symbol.ObsoleteState
                If state <> ThreeState.False Then
                    Return state
                End If

                ' For property or event accessors, check the associated property or event instead.
                If symbol.IsAccessor() Then
                    symbol = DirectCast(symbol, MethodSymbol).AssociatedSymbol
                Else
                    symbol = symbol.ContainingSymbol
                End If
            End While

            Return ThreeState.False
        End Function

        Friend Shared Function GetObsoleteDiagnosticKind(context As Symbol, symbol As Symbol, Optional forceComplete As Boolean = False) As ObsoleteDiagnosticKind
            Debug.Assert(context IsNot Nothing)
            Debug.Assert(symbol IsNot Nothing)

            Select Case symbol.ObsoleteKind
                Case ObsoleteAttributeKind.None
                    Return ObsoleteDiagnosticKind.NotObsolete
                Case ObsoleteAttributeKind.WindowsExperimental, ObsoleteAttributeKind.Experimental
                    Return ObsoleteDiagnosticKind.Diagnostic
                Case ObsoleteAttributeKind.Uninitialized
                    ' If we haven't cracked attributes on the symbol at all or we haven't
                    ' cracked attribute arguments enough to be able to report diagnostics for
                    ' ObsoleteAttribute, store the symbol so that we can report diagnostics at a 
                    ' later stage.
                    Return ObsoleteDiagnosticKind.Lazy
            End Select

            Select Case GetObsoleteContextState(context, forceComplete)
                Case ThreeState.False
                    Return ObsoleteDiagnosticKind.Diagnostic
                Case ThreeState.True
                    ' If we are in a context that is already obsolete, there is no point reporting
                    ' more obsolete diagnostics.
                    Return ObsoleteDiagnosticKind.Suppressed
                Case Else
                    ' If the context is unknown, then store the symbol so that we can do this check at a
                    ' later stage
                    Return ObsoleteDiagnosticKind.LazyPotentiallySuppressed
            End Select
        End Function

        ''' <summary>
        ''' Create a diagnostic for the given symbol. This could be an error or a warning based on
        ''' the ObsoleteAttribute's arguments.
        ''' </summary>
        Friend Shared Function CreateObsoleteDiagnostic(symbol As Symbol) As DiagnosticInfo
            Dim data = symbol.ObsoleteAttributeData
            Debug.Assert(data IsNot Nothing)

            If data Is Nothing Then
                Return Nothing
            End If

            ' At this point, we are going to issue diagnostics and therefore the data shouldn't be
            ' uninitialized.
            Debug.Assert(Not data.IsUninitialized)

            If data.Kind = ObsoleteAttributeKind.WindowsExperimental Then
                Debug.Assert(data.Message Is Nothing)
                Debug.Assert(Not data.IsError)
                ' Provide an explicit format for fully-qualified type names.
                Return ErrorFactory.ErrorInfo(ERRID.WRN_Experimental, New FormattedSymbol(symbol, SymbolDisplayFormat.VisualBasicErrorMessageFormat))
            End If

            If data.Kind = ObsoleteAttributeKind.Experimental Then
                Debug.Assert(data.Message Is Nothing)
                Debug.Assert(Not data.IsError)
                ' Provide an explicit format for fully-qualified type names.
                Return New CustomObsoleteDiagnosticInfo(MessageProvider.Instance, ERRID.WRN_Experimental,
                    data, New FormattedSymbol(symbol, SymbolDisplayFormat.VisualBasicErrorMessageFormat))
            End If

            ' For property accessors we report a special diagnostic which indicates whether the getter or setter is obsolete.
            ' For all other symbols, report the regular diagnostic.
            If symbol.IsAccessor() AndAlso (DirectCast(symbol, MethodSymbol).AssociatedSymbol).Kind = SymbolKind.Property Then
                Dim accessorSymbol = DirectCast(symbol, MethodSymbol)
                Dim accessorString = If(accessorSymbol.MethodKind = MethodKind.PropertyGet, "Get", "Set")

                If String.IsNullOrEmpty(data.Message) Then
                    Return ErrorFactory.ObsoleteErrorInfo(If(data.IsError, ERRID.ERR_UseOfObsoletePropertyAccessor2, ERRID.WRN_UseOfObsoletePropertyAccessor2), data,
                                        accessorString, accessorSymbol.AssociatedSymbol)
                Else
                    Return ErrorFactory.ObsoleteErrorInfo(If(data.IsError, ERRID.ERR_UseOfObsoletePropertyAccessor3, ERRID.WRN_UseOfObsoletePropertyAccessor3), data,
                                        accessorString, accessorSymbol.AssociatedSymbol, data.Message)
                End If
            Else
                If String.IsNullOrEmpty(data.Message) Then
                    Return ErrorFactory.ObsoleteErrorInfo(If(data.IsError, ERRID.ERR_UseOfObsoleteSymbolNoMessage1, ERRID.WRN_UseOfObsoleteSymbolNoMessage1), data, symbol)
                Else
                    Return ErrorFactory.ObsoleteErrorInfo(If(data.IsError, ERRID.ERR_UseOfObsoleteSymbol2, ERRID.WRN_UseOfObsoleteSymbol2), data, symbol, data.Message)
                End If
            End If

        End Function

    End Class
End Namespace
