' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents an event.
    ''' </summary>
    Partial Friend MustInherit Class EventSymbol
        Inherits Symbol
        Implements IEventSymbol, IEventSymbolInternal

        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ' Changes to the public interface of this class should remain synchronized with the C# version.
        ' Do not make any changes to the public interface without making the corresponding change
        ' to the C# version.
        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        Friend Sub New()
        End Sub

        ''' <summary>
        ''' Get the original definition of this symbol. If this symbol is derived from another
        ''' symbol by (say) type substitution, this gets the original symbol, as it was defined
        ''' in source or metadata.
        ''' </summary>
        Public Overridable Shadows ReadOnly Property OriginalDefinition As EventSymbol
            Get
                ' Default implements returns Me.
                Return Me
            End Get
        End Property

        Protected NotOverridable Overrides ReadOnly Property OriginalSymbolDefinition As Symbol
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        Public MustOverride ReadOnly Property IsWindowsRuntimeEvent As Boolean Implements IEventSymbol.IsWindowsRuntimeEvent

        Public MustOverride ReadOnly Property Type As TypeSymbol

        Public MustOverride ReadOnly Property AddMethod As MethodSymbol

        Public MustOverride ReadOnly Property RemoveMethod As MethodSymbol

        Public MustOverride ReadOnly Property RaiseMethod As MethodSymbol

        ''' <summary>
        ''' True if the event itself Is excluded from code coverage instrumentation.
        ''' True for source events marked with <see cref="AttributeDescription.ExcludeFromCodeCoverageAttribute"/>.
        ''' </summary>
        Friend Overridable ReadOnly Property IsDirectlyExcludedFromCodeCoverage As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        '''  True if this symbol has a special name (metadata flag SpecialName is set).
        ''' </summary>
        Friend MustOverride ReadOnly Property HasSpecialName As Boolean

        Friend ReadOnly Property HasAssociatedField As Boolean
            Get
                Return Me.AssociatedField IsNot Nothing
            End Get
        End Property

        ''' <summary>
        ''' Gets the attributes on event's associated field, if any.
        ''' </summary>
        ''' <returns>Returns an array of <see cref="VisualBasicAttributeData"/> or an empty array if there are no attributes.</returns>
        Public Function GetFieldAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Dim field = Me.AssociatedField
            Return If(field Is Nothing, ImmutableArray(Of VisualBasicAttributeData).Empty, field.GetAttributes())
        End Function

        ''' <summary>
        ''' Backing field of the event, or Nothing if the event doesn't have any.
        ''' </summary>
        ''' <remarks>
        ''' Events imported from metadata return Nothing.
        ''' </remarks>
        Friend MustOverride ReadOnly Property AssociatedField As FieldSymbol

        Public ReadOnly Property OverriddenEvent As EventSymbol
            Get
                If Me.IsOverrides Then
                    If IsDefinition Then
                        Return OverriddenOrHiddenMembers.OverriddenMember
                    End If

                    Return OverriddenMembersResult(Of EventSymbol).GetOverriddenMember(Me, Me.OriginalDefinition.OverriddenEvent)
                End If

                Return Nothing
            End Get
        End Property

        Friend Overridable ReadOnly Property OverriddenOrHiddenMembers As OverriddenMembersResult(Of EventSymbol)
            Get
                ' To save space, the default implementation does not cache its result.  We expect a very large number 
                'of MethodSymbols not override anything.
                Return OverrideHidingHelper(Of EventSymbol).MakeOverriddenMembers(Me)
            End Get
        End Property

        Friend Overridable ReadOnly Property IsExplicitInterfaceImplementation As Boolean
            Get
                Return ExplicitInterfaceImplementations.Any()
            End Get
        End Property

        Public MustOverride ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)

        Public NotOverridable Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.[Event]
            End Get
        End Property

        ''' <summary>
        ''' Gets the parameters of this event. If this event has no parameters, returns
        ''' an empty list.
        ''' </summary>
        Friend Overridable ReadOnly Property DelegateParameters As ImmutableArray(Of ParameterSymbol)
            Get
                Dim invoke = DelegateInvokeMethod()
                If invoke IsNot Nothing Then
                    Return invoke.Parameters
                Else
                    Return ImmutableArray(Of ParameterSymbol).Empty
                End If
            End Get
        End Property

        ''' <summary>
        ''' Gets the return type of the event (typically System.Void). 
        ''' </summary>
        Friend ReadOnly Property DelegateReturnType As TypeSymbol
            Get
                Dim invoke = DelegateInvokeMethod()
                If invoke IsNot Nothing Then
                    Return invoke.ReturnType
                Else
                    Return ContainingAssembly.GetSpecialType(SpecialType.System_Void)
                End If
            End Get
        End Property

        ''' <summary>
        ''' Can be null in error cases.
        ''' </summary>
        Private Function DelegateInvokeMethod() As MethodSymbol
            Dim type = TryCast(Me.Type, NamedTypeSymbol)
            If type IsNot Nothing AndAlso type.TypeKind = TypeKind.Delegate Then
                Return type.DelegateInvokeMethod
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Implements visitor pattern.
        ''' </summary>
        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitEvent(Me, argument)
        End Function

#Region "Use-site Diagnostics"

        Friend Overrides Function GetUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            If Me.IsDefinition Then
                Return New UseSiteInfo(Of AssemblySymbol)(PrimaryDependency)
            End If

            Return Me.OriginalDefinition.GetUseSiteInfo()
        End Function

        Friend Function CalculateUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            Debug.Assert(Me.IsDefinition)
            ' Check event type.
            Dim useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(PrimaryDependency)
            MergeUseSiteInfo(useSiteInfo, DeriveUseSiteInfoFromType(Me.Type))
            Dim errorInfo As DiagnosticInfo = useSiteInfo.DiagnosticInfo

            If errorInfo IsNot Nothing Then
                Select Case errorInfo.Code
                    Case ERRID.ERR_UnreferencedAssembly3
                        ' NOTE: interestingly the error in Dev10 and thus here refers to the definition of the event
                        '       although it is the definition of the event's type that is contained in missing assembly
                        ' TODO: Perhaps the error wording could be changed a bit to say "type of event..." ?
                        '
                        ' Reference required to assembly '{0}' containing the definition for event '{1}'. Add one to your project.
                        useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(ErrorFactory.ErrorInfo(ERRID.ERR_UnreferencedAssemblyEvent3, errorInfo.Arguments(0), Me))

                    Case ERRID.ERR_UnreferencedModule3
                        ' NOTE: interestingly the error in Dev10 and thus here refers to the definition of the event
                        '       although it is the definition of the event's type that is contained in missing assembly
                        ' TODO: Perhaps the error wording could be changed a bit to say "type of event..." ?
                        '
                        ' Reference required to module '{0}' containing the definition for event '{1}'. Add one to your project.
                        useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(ErrorFactory.ErrorInfo(ERRID.ERR_UnreferencedModuleEvent3, errorInfo.Arguments(0), Me))

                    Case ERRID.ERR_UnsupportedType1
                        If errorInfo.Arguments(0).Equals(String.Empty) Then
                            useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedType1, CustomSymbolDisplayFormatter.ShortErrorName(Me)))
                        End If

                    Case Else
                        ' Nothing to do, simply use the same error info.
                End Select
            ElseIf Me.ContainingModule.HasUnifiedReferences Then
                ' If the member is in an assembly with unified references, 
                ' we check if its definition depends on a type from a unified reference.
                errorInfo = Me.Type.GetUnificationUseSiteDiagnosticRecursive(Me, checkedTypes:=Nothing)
                If errorInfo IsNot Nothing Then
                    Debug.Assert(errorInfo.Severity = DiagnosticSeverity.Error)
                    useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(errorInfo)
                End If
            End If

            Return useSiteInfo
        End Function

        Protected Overrides Function IsHighestPriorityUseSiteError(code As Integer) As Boolean
            Return code = ERRID.ERR_UnsupportedType1 OrElse code = ERRID.ERR_UnsupportedCompilerFeature
        End Function

        Public NotOverridable Overrides ReadOnly Property HasUnsupportedMetadata As Boolean
            Get
                Dim info As DiagnosticInfo = GetUseSiteInfo().DiagnosticInfo
                Return info IsNot Nothing AndAlso (info.Code = ERRID.ERR_UnsupportedType1 OrElse info.Code = ERRID.ERR_UnsupportedEvent1 OrElse info.Code = ERRID.ERR_UnsupportedCompilerFeature)
            End Get
        End Property

#End Region

        Friend Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
            Get
                Return Me.ContainingSymbol.EmbeddedSymbolKind
            End Get
        End Property

        ''' <summary>
        ''' Is this an event of a tuple type?
        ''' </summary>
        Public Overridable ReadOnly Property IsTupleEvent() As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' If this is an event of a tuple type, return corresponding underlying event from the
        ''' tuple underlying type. Otherwise, Nothing. 
        ''' </summary>
        Public Overridable ReadOnly Property TupleUnderlyingEvent() As EventSymbol
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IEventSymbol_Type As ITypeSymbol Implements IEventSymbol.Type
            Get
                Return Me.Type
            End Get
        End Property

        Private ReadOnly Property IEventSymbol_NullableAnnotation As NullableAnnotation Implements IEventSymbol.NullableAnnotation
            Get
                Return NullableAnnotation.None
            End Get
        End Property

        Private ReadOnly Property IEventSymbol_AddMethod As IMethodSymbol Implements IEventSymbol.AddMethod
            Get
                Return Me.AddMethod
            End Get
        End Property

        Private ReadOnly Property IEventSymbol_RemoveMethod As IMethodSymbol Implements IEventSymbol.RemoveMethod
            Get
                Return Me.RemoveMethod
            End Get
        End Property

        Private ReadOnly Property IEventSymbol_RaiseMethod As IMethodSymbol Implements IEventSymbol.RaiseMethod
            Get
                Return Me.RaiseMethod
            End Get
        End Property

        Private ReadOnly Property IEventSymbol_OriginalDefinition As IEventSymbol Implements IEventSymbol.OriginalDefinition
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        Private ReadOnly Property IEventSymbol_OverriddenEvent As IEventSymbol Implements IEventSymbol.OverriddenEvent
            Get
                Return Me.OverriddenEvent
            End Get
        End Property

        Private ReadOnly Property IEventSymbol_ExplicitInterfaceImplementations As ImmutableArray(Of IEventSymbol) Implements IEventSymbol.ExplicitInterfaceImplementations
            Get
                Return StaticCast(Of IEventSymbol).From(Me.ExplicitInterfaceImplementations)
            End Get
        End Property

        Private ReadOnly Property IEventSymbol_PartialDefinitionPart As IEventSymbol Implements IEventSymbol.PartialDefinitionPart
            Get
                ' Feature not supported in VB
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IEventSymbol_PartialImplementationPart As IEventSymbol Implements IEventSymbol.PartialImplementationPart
            Get
                ' Feature not supported in VB
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IEventSymbol_IsPartialDefinition As Boolean Implements IEventSymbol.IsPartialDefinition
            Get
                ' Feature not supported in VB
                Return False
            End Get
        End Property

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitEvent(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitEvent(Me)
        End Function

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitEvent(Me, argument)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitEvent(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitEvent(Me)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim other As EventSymbol = TryCast(obj, EventSymbol)
            If Nothing Is other Then
                Return False
            End If

            If Me Is other Then
                Return True
            End If

            Return TypeSymbol.Equals(Me.ContainingType, other.ContainingType, TypeCompareKind.ConsiderEverything) AndAlso Me.OriginalDefinition Is other.OriginalDefinition
        End Function

        Public Overrides Function GetHashCode() As Integer
            Dim code As Integer = 1
            code = Hash.Combine(Me.ContainingType, code)
            code = Hash.Combine(Me.Name, code)
            Return code
        End Function

    End Class

End Namespace

