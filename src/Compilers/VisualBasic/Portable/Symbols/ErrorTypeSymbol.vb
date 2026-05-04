' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Class ErrorTypeSymbol
        Inherits NamedTypeSymbol
        Implements IErrorTypeSymbol

        Friend Shared ReadOnly UnknownResultType As ErrorTypeSymbol = New ErrorTypeSymbol()

        ''' <summary>
        ''' Returns information about the reason that this type is in error.
        ''' </summary>
        Friend Overridable ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function GetUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            If Me.IsDefinition Then
                Return New UseSiteInfo(Of AssemblySymbol)(Me.ErrorInfo)
            End If

            ' Base class handles constructed types.
            Return MyBase.GetUseSiteInfo()
        End Function

        Friend Overrides Function GetUnificationUseSiteDiagnosticRecursive(owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
            Return Nothing
        End Function

        Friend Overrides Function MakeDeclaredBase(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
            Return Nothing
        End Function

        Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides Function MakeAcyclicBaseType(diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
            Return Nothing
        End Function

        Friend Overrides Function MakeAcyclicInterfaces(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides ReadOnly Property IsReferenceType As Boolean
            Get
                ' TODO: Consider returning False.
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property IsValueType As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
            Get
                Return SpecializedCollections.EmptyEnumerable(Of String)()
            End Get
        End Property

        Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
            Return SpecializedCollections.EmptyEnumerable(Of FieldSymbol)()
        End Function

        Public NotOverridable Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.ErrorType
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return TypeKind.Error
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return 0
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return String.Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Debug.Assert(Arity = 0)
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsSerializable As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property Layout As TypeLayout
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingCharSet As CharSet
            Get
                Return DefaultMarshallingCharSet
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeArgumentsNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                Return ImmutableArray(Of TypeSymbol).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property HasTypeArgumentsCustomModifiers As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Function GetTypeArgumentCustomModifiers(ordinal As Integer) As ImmutableArray(Of CustomModifier)
            Return GetEmptyTypeArgumentCustomModifiers(ordinal)
        End Function

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol
            Get
                Return Me
            End Get
        End Property

        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitErrorType(Me, arg)
        End Function

        ' Only compiler creates an error symbol.
        Friend Sub New()
        End Sub

        Public NotOverridable Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Public
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsMustInherit As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsNotInheritable As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasCodeAnalysisEmbeddedAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasVisualBasicEmbeddedAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasCompilerLoweringPreserveAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsComImport As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property CoClassType As TypeSymbol
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return ImmutableArray(Of String).Empty
        End Function

        Friend Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Throw ExceptionUtilities.Unreachable
        End Function

        ''' <summary>
        ''' Substitute the given type substitution within this type, returning a new type. If the
        ''' substitution had no effect, return Me. 
        ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
        ''' !!! All other code should use Construct methods.                                        !!! 
        ''' </summary>
        Friend Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
            Return New TypeWithModifiers(Me)
        End Function

        Friend Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property CanConstruct As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As NamedTypeSymbol
            ' An error symbol has no type parameters, so construction isn't allowed.
            ' TDOD: Remove temporary workaround from constructor of ArrayTypeSymbol.
            Throw New InvalidOperationException()
        End Function

        Friend Overrides ReadOnly Property DefaultPropertyName As String
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary> 
        ''' If we believe we know which symbol the user intended, then we should retain that information
        ''' in the corresponding error symbol - it can be useful for deciding how to handle the error.
        ''' </summary>
        Friend ReadOnly Property NonErrorGuessType As NamedTypeSymbol
            Get
                Dim candidates = Me.CandidateSymbols
                If candidates.Length = 1 Then  ' Only return a guess if its unambiguous.
                    Return TryCast(candidates(0), NamedTypeSymbol)
                Else
                    Return Nothing
                End If
            End Get
        End Property

        ''' <summary>
        ''' Return why the candidate symbols were bad.
        ''' </summary>
        Friend Overridable ReadOnly Property ResultKind As LookupResultKind
            Get
                Return LookupResultKind.Empty
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' When constructing this ErrorTypeSymbol, there may have been symbols that seemed to
        ''' be what the user intended, but were unsuitable. For example, a type might have been
        ''' inaccessible, or ambiguous. This property returns the possible symbols that the user
        ''' might have intended. It will return no symbols if no possible symbols were found.
        ''' See the CandidateReason property to understand why the symbols were unsuitable.
        ''' </summary>
        Public Overridable ReadOnly Property CandidateSymbols As ImmutableArray(Of Symbol)
            Get
                Return ImmutableArray(Of Symbol).Empty
            End Get
        End Property

        Public ReadOnly Property CandidateReason As CandidateReason Implements IErrorTypeSymbol.CandidateReason
            Get
                If CandidateSymbols.IsEmpty Then
                    Return Microsoft.CodeAnalysis.CandidateReason.None
                Else
                    Debug.Assert(ResultKind <> LookupResultKind.Good, "Shouldn't have good result kind on error symbol")
                    Return ResultKind.ToCandidateReason()
                End If
            End Get
        End Property

        Public Overrides Function Equals(obj As TypeSymbol, comparison As TypeCompareKind) As Boolean
            Return Me Is obj
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Me)
        End Function

        ''' <summary>
        ''' Force all declaration errors to be generated.
        ''' </summary>
        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend NotOverridable Overrides Function GetSynthesizedWithEventsOverrides() As IEnumerable(Of PropertySymbol)
            Return SpecializedCollections.EmptyEnumerable(Of PropertySymbol)()
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasAnyDeclaredRequiredMembers As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetGuidString(ByRef guidString As String) As Boolean
            guidString = Nothing
            Return False
        End Function

#Region "IErrorTypeSymbol members"

        Public ReadOnly Property IErrorTypeSymbol_CandidateSymbols As ImmutableArray(Of ISymbol) Implements IErrorTypeSymbol.CandidateSymbols
            Get
                Return StaticCast(Of ISymbol).From(Me.CandidateSymbols)
            End Get
        End Property

#End Region

    End Class
End Namespace

