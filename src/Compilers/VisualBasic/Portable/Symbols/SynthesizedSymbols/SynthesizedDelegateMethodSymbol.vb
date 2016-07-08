' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' This class represent a compiler generated method
    ''' </summary>
    Friend NotInheritable Class SynthesizedDelegateMethodSymbol
        Inherits MethodSymbol

        Private ReadOnly _name As String
        Private ReadOnly _containingType As NamedTypeSymbol
        Private ReadOnly _returnType As TypeSymbol
        Private ReadOnly _flags As SourceMemberFlags
        Private _parameters As ImmutableArray(Of ParameterSymbol)

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SynthesizedDelegateMethodSymbol" /> class. The parameters are not initialized and need to be set 
        ''' by using the <see cref="SetParameters" /> method.
        ''' </summary>
        ''' <param name="name">The name of this method.</param>
        ''' <param name="containingSymbol">The containing symbol.</param>
        ''' <param name="flags">The flags for this method.</param>
        ''' <param name="returnType">The return type.</param>
        Public Sub New(name As String,
                       containingSymbol As NamedTypeSymbol,
                       flags As SourceMemberFlags,
                       returnType As TypeSymbol)

            _name = name
            _containingType = containingSymbol
            _flags = flags
            _returnType = returnType
        End Sub

        ''' <summary>
        ''' Sets the parameters.
        ''' </summary>
        ''' <param name="parameters">The parameters.</param>
        ''' <remarks>
        ''' Note: This should be called at most once, immediately after the symbol is constructed. The parameters aren't 
        ''' Note: passed to the constructor because they need to have their container set correctly.
        ''' </remarks>
        Friend Sub SetParameters(parameters As ImmutableArray(Of ParameterSymbol))
            Debug.Assert(Not parameters.IsDefault)
            Debug.Assert(_parameters.IsDefault)

            _parameters = parameters
        End Sub

        ''' <summary>
        ''' Returns the arity of this method, or the number of type parameters it takes.
        ''' A non-generic method has zero arity.
        ''' </summary>
        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return 0
            End Get
        End Property

        ''' <summary>
        ''' If this method has MethodKind of MethodKind.PropertyGet or MethodKind.PropertySet,
        ''' returns the property that this method is the getter or setter for.
        ''' If this method has MethodKind of MethodKind.EventAdd or MethodKind.EventRemove,
        ''' returns the event that this method is the adder or remover for.
        ''' </summary>
        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Calling convention of the signature.
        ''' </summary>
        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return Microsoft.Cci.CallingConvention.HasThis
            End Get
        End Property

        ''' <summary>
        ''' Gets the <see cref="ISymbol" /> for the immediately containing symbol.
        ''' </summary>
        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return _containingType
            End Get
        End Property

        ''' <summary>
        ''' Gets a <see cref="Accessibility" /> indicating the declared accessibility for the symbol.
        ''' Returns NotApplicable if no accessibility is declared.
        ''' </summary>
        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Public
            End Get
        End Property

        ''' <summary>
        ''' Returns interface methods explicitly implemented by this method.
        ''' </summary>
        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Return ImmutableArray(Of MethodSymbol).Empty
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this method is an extension method.
        ''' </summary>
        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is external method.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is external method; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides Function GetDllImportData() As DllImportData
            Return Nothing
        End Function

        Friend Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                Return Reflection.MethodImplAttributes.Runtime
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Throw ExceptionUtilities.Unreachable
        End Function

        ''' <summary>
        ''' Gets a value indicating whether this instance is abstract or not.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is abstract; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is not overridable.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is not overridable; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is overloads.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is overloads; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is overridable.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is overridable; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return (_flags And SourceMemberFlags.Overridable) <> 0
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is overrides.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is overrides; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is shared.
        ''' </summary>
        ''' <value>
        '''   <c>true</c> if this instance is shared; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is a sub.
        ''' </summary>
        ''' <value>
        '''   <c>true</c> if this instance is sub; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return _returnType.SpecialType = SpecialType.System_Void
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is vararg.
        ''' </summary>
        ''' <value>
        '''   <c>true</c> if this instance is vararg; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            Return _containingType.GetLexicalSortKey()
        End Function

        ''' <summary>
        ''' A potentially empty collection of locations that correspond to this instance.
        ''' </summary>
        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _containingType.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        ''' <summary>
        ''' Gets what kind of method this is. There are several different kinds of things in the
        ''' VB language that are represented as methods. This property allow distinguishing those things
        ''' without having to decode the name of the method.
        ''' </summary>
        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return _flags.ToMethodKind()
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' The parameters forming part of this signature.
        ''' </summary>
        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                If Not _parameters.IsDefault Then
                    Return _parameters
                Else
                    Return ImmutableArray(Of ParameterSymbol).Empty
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets the return type of the method.
        ''' </summary>
        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _returnType
            End Get
        End Property

        ''' <summary>
        ''' Returns the list of attributes, if any, associated with the return type.
        ''' </summary>
        Public Overrides Function GetReturnTypeAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return ImmutableArray(Of VisualBasicAttributeData).Empty
        End Function

        ''' <summary>
        ''' Returns the list of custom modifiers, if any, associated with the returned value.
        ''' </summary>
        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        ''' <summary>
        ''' Returns the type arguments that have been substituted for the type parameters.
        ''' If nothing has been substituted for a given type parameter,
        ''' then the type parameter itself is consider the type argument.
        ''' </summary>
        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return ImmutableArray(Of TypeSymbol).Empty
            End Get
        End Property

        ''' <summary>
        ''' Get the type parameters on this method. If the method has not generic,
        ''' returns an empty list.
        ''' </summary>
        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether the symbol was generated by the compiler
        ''' rather than declared explicitly.
        ''' </summary>
        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Gets the symbol name. Returns the empty string if unnamed.
        ''' </summary>
        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return Me.MethodKind = MethodKind.Constructor
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return ImmutableArray(Of String).Empty
        End Function

        Friend Overrides ReadOnly Property Syntax As VisualBasicSyntaxNode
            Get
                Return Nothing ' The methods are runtime implemented
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            ' Dev11 emits DebuggerNonUserCodeAttribute on methods of anon delegates but not of user defined delegates.
            ' In both cases the debugger hides these methods so no attribute is necessary.
            ' We expect other tools to also special case delegate methods.
        End Sub

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace
