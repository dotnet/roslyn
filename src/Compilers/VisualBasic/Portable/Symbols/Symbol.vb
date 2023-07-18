' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Display = Microsoft.CodeAnalysis.VisualBasic.SymbolDisplay

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' The base class for all symbols (namespaces, classes, method, parameters, etc.) that are 
    ''' exposed by the compiler.
    ''' </summary>
    <DebuggerDisplay("{GetDebuggerDisplay(), nq}")>
    Friend MustInherit Class Symbol
        Implements ISymbol, ISymbolInternal, IFormattable

        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ' Changes to the public interface of this class should remain synchronized with the C# version of Symbol.
        ' Do not make any changes to the public interface without making the corresponding change
        ' to the C# version.
        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        ''' <summary>
        ''' Gets the name of this symbol.
        ''' </summary>
        ''' <returns>Returns the name of this symbol. Symbols without a name return the empty string;
        ''' Nothing is never returned.</returns>
        Public Overridable ReadOnly Property Name As String
            Get
                Return String.Empty
            End Get
        End Property

        ''' <summary>
        ''' Gets the name of a symbol as it appears in metadata. Most of the time, this
        ''' is the same as the Name property, with the following exceptions:
        ''' 1) The metadata name of generic types includes the "`1", "`2" etc. suffix that
        ''' indicates the number of type parameters (it does not include, however, names of
        ''' containing types or namespaces).
        ''' 2) The metadata name of methods that overload or override methods with the same
        ''' case-insensitive name but different case-sensitive names are adjusted so that
        ''' the overrides and overloads always have the same name in a case-sensitive way.
        ''' 
        ''' It should be noted that Visual Basic merges namespace declaration from source
        ''' and/or metadata with different casing into a single namespace symbol. Thus, for
        ''' namespace symbols this property may return incorrect information if multiple declarations
        ''' with different casing were found.
        ''' </summary>
        Public Overridable ReadOnly Property MetadataName As String Implements ISymbol.MetadataName, ISymbolInternal.MetadataName
            Get
                Return Name
            End Get
        End Property

        ''' <summary>
        ''' Gets the token for this symbol as it appears in metadata. Most of the time this Is 0,
        ''' as it Is when the symbol Is Not loaded from metadata.
        ''' </summary>
        Public Overridable ReadOnly Property MetadataToken As Integer Implements ISymbol.MetadataToken
            Get
                Return 0
            End Get
        End Property

        ''' <summary>
        ''' Set the metadata name for this symbol.
        ''' Called from <see cref="OverloadingHelper.SetMetadataNameForAllOverloads"/> for each symbol of the same name in a type.
        ''' </summary>
        Friend Overridable Sub SetMetadataName(metadataName As String)
            ' only user defined methods and properties can have their name changed
            Throw ExceptionUtilities.Unreachable
        End Sub

        ''' <summary>
        ''' Gets the kind of this symbol.
        ''' </summary>
        Public MustOverride ReadOnly Property Kind As SymbolKind

        ''' <summary>
        ''' Get the symbol that logically contains this symbol. 
        ''' </summary>
        Public MustOverride ReadOnly Property ContainingSymbol As Symbol

        ''' <summary>
        ''' Gets the nearest enclosing namespace for this namespace or type. For a nested type,
        ''' returns the namespace that contains its container.
        ''' </summary>
        Public ReadOnly Property ContainingNamespace As NamespaceSymbol
            Get
                Dim container = Me.ContainingSymbol
                While container IsNot Nothing
                    Dim ns = TryCast(container, NamespaceSymbol)
                    If ns IsNot Nothing Then
                        Return ns
                    End If
                    container = container.ContainingSymbol
                End While
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Returns the nearest lexically enclosing type, or Nothing if there is none.
        ''' </summary>
        Public Overridable ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Dim container As Symbol = Me.ContainingSymbol

                Dim containerAsType As NamedTypeSymbol = TryCast(container, NamedTypeSymbol)

                ' NOTE: container could be null, so we do not check 
                '       whether containerAsType is not null, but 
                '       instead check if it did not change after 
                '       the cast.
                If containerAsType Is container Then
                    ' this should be relatively uncommon
                    ' most symbols that may be contained in a type
                    ' know their containing type and can override ContainingType
                    ' with a more precise implementation
                    Return containerAsType
                End If

                ' this is recursive, but recursion should be very short 
                ' before we reach symbol that definitely knows its containing type.
                Return container.ContainingType
            End Get
        End Property

        ''' <summary>
        ''' Returns the containing type or namespace, if this symbol is immediately contained by it.
        ''' Otherwise returns Nothing.
        ''' </summary>
        Friend ReadOnly Property ContainingNamespaceOrType As NamespaceOrTypeSymbol
            Get
                Dim container = Me.ContainingSymbol

                If container IsNot Nothing Then
                    Select Case container.Kind
                        Case SymbolKind.Namespace
                            Return DirectCast(ContainingSymbol, NamespaceSymbol)
                        Case SymbolKind.NamedType
                            Return DirectCast(ContainingSymbol, NamedTypeSymbol)
                    End Select
                End If

                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Returns the assembly containing this symbol. If this symbol is shared
        ''' across multiple assemblies, or doesn't belong to an assembly, returns Nothing.
        ''' </summary>
        Public Overridable ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                ' Default implementation gets the containers assembly.
                Dim container As Symbol = Me.ContainingSymbol
                If container IsNot Nothing Then
                    Return container.ContainingAssembly
                Else
                    Return Nothing
                End If
            End Get
        End Property

        ''' <summary>
        ''' For a source assembly, the associated compilation.
        ''' For any other assembly, null.
        ''' For a source module, the DeclaringCompilation of the associated source assembly.
        ''' For any other module, null.
        ''' For any other symbol, the DeclaringCompilation of the associated module.
        ''' </summary>
        ''' <remarks>
        ''' We're going through the containing module, rather than the containing assembly,
        ''' because of /addmodule (symbols in such modules should return null).
        ''' 
        ''' Remarks, not "ContainingCompilation" because it isn't transitive.
        ''' </remarks>
        Friend Overridable ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Select Case Me.Kind
                    Case SymbolKind.ErrorType
                        Return Nothing
                    Case SymbolKind.Assembly
                        Debug.Assert(Not (TypeOf Me Is SourceAssemblySymbol), "SourceAssemblySymbol must override DeclaringCompilation")
                        Return Nothing
                    Case SymbolKind.NetModule
                        Debug.Assert(Not (TypeOf Me Is SourceModuleSymbol), "SourceModuleSymbol must override DeclaringCompilation")
                        Return Nothing
                End Select

                Dim sourceModuleSymbol = TryCast(Me.ContainingModule, SourceModuleSymbol)
                Return If(sourceModuleSymbol Is Nothing, Nothing, sourceModuleSymbol.DeclaringCompilation)
            End Get
        End Property

        ReadOnly Property ISymbolInternal_DeclaringCompilation As Compilation Implements ISymbolInternal.DeclaringCompilation
            Get
                Return DeclaringCompilation
            End Get
        End Property

        ''' <summary>
        ''' Returns the module containing this symbol. If this symbol is shared
        ''' across multiple modules, or doesn't belong to a module, returns Nothing.
        ''' </summary>
        Public Overridable ReadOnly Property ContainingModule As ModuleSymbol
            Get
                ' Default implementation gets the containers module.

                Dim container As Symbol = Me.ContainingSymbol

                If (container IsNot Nothing) Then
                    Return container.ContainingModule
                Else
                    Return Nothing
                End If
            End Get
        End Property

        ''' <summary>
        ''' Get the original definition of this symbol. If this symbol is derived from another
        ''' symbol by (say) type substitution, this gets the original symbol, as it was defined
        ''' in source or metadata.
        ''' </summary>
        Public ReadOnly Property OriginalDefinition As Symbol
            Get
                Return OriginalSymbolDefinition
            End Get
        End Property

        Protected Overridable ReadOnly Property OriginalSymbolDefinition As Symbol
            Get
                ' Default implements returns Me.
                Return Me
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this is the original definition of this symbol.
        ''' </summary>
        Public ReadOnly Property IsDefinition As Boolean
            Get
                ' TODO: Is "Is" or "Equals" correct here?
                Return OriginalDefinition Is Me
            End Get
        End Property

        ''' <summary>
        ''' <para>
        ''' Get a source location key for sorting. For performance, it's important that this
        ''' be able to be returned from a symbol without doing any additional allocations (even
        ''' if nothing is cached yet.)
        ''' </para>
        ''' <para>
        ''' Only (original) source symbols and namespaces that can be merged
        ''' need override this function if they want to do so for efficiency.
        ''' </para>
        ''' </summary>
        Friend Overridable Function GetLexicalSortKey() As LexicalSortKey
            Dim locations = Me.Locations
            Dim declaringCompilation = Me.DeclaringCompilation
            Debug.Assert(declaringCompilation IsNot Nothing) ' require that it is a source symbol
            Return If(locations.Length > 0, New LexicalSortKey(locations(0), declaringCompilation), LexicalSortKey.NotInSource)
        End Function

        ''' <summary>
        ''' Gets the locations where this symbol was originally defined, either in source
        ''' or metadata. Some symbols (for example, partial classes) may be defined in more
        ''' than one location.
        ''' </summary>
        Public MustOverride ReadOnly Property Locations As ImmutableArray(Of Location)

        ''' <summary>
        ''' Get the syntax node(s) where this symbol was declared in source. Some symbols (for example,
        ''' partial classes) may be defined in more than one location. This property should return
        ''' one or more syntax nodes only if the symbol was declared in source code and also was
        ''' not implicitly declared (see the IsImplicitlyDeclared property). 
        ''' 
        ''' Note that for namespace symbol, the declaring syntax might be declaring a nested namespace.
        ''' For example, the declaring syntax node for N1 in "Namespace N1.N2" is the 
        ''' NamespaceDeclarationSyntax for N1.N2. For the project namespace, the declaring syntax will
        ''' be the CompilationUnitSyntax.
        ''' </summary>
        ''' <returns>
        ''' The syntax node(s) that declared the symbol. If the symbol was declared in metadata
        ''' or was implicitly declared, returns an empty read-only array.
        ''' </returns>
        ''' <remarks>
        ''' To go the opposite direction (from syntax node to symbol), see <see cref="VBSemanticModel.GetDeclaredSymbol"/>.
        ''' </remarks>
        Public MustOverride ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)

        ''' <summary> 
        ''' Helper for implementing DeclaringSyntaxNodes for derived classes that store a location but not a  SyntaxNode or SyntaxReference. 
        ''' </summary>
        Friend Shared Function GetDeclaringSyntaxNodeHelper(Of TNode As VisualBasicSyntaxNode)(locations As ImmutableArray(Of Location)) As ImmutableArray(Of VisualBasicSyntaxNode)
            If locations.IsEmpty Then
                Return ImmutableArray(Of VisualBasicSyntaxNode).Empty
            Else
                Dim builder As ArrayBuilder(Of VisualBasicSyntaxNode) = ArrayBuilder(Of VisualBasicSyntaxNode).GetInstance()
                For Each location In locations
                    If location.IsInSource AndAlso location.SourceTree IsNot Nothing Then
                        Dim token = CType(location.SourceTree.GetRoot().FindToken(location.SourceSpan.Start), SyntaxToken)
                        If token.Kind <> SyntaxKind.None Then
                            Dim node As VisualBasicSyntaxNode = token.Parent.FirstAncestorOrSelf(Of TNode)()
                            If node IsNot Nothing Then
                                builder.Add(node)
                            End If
                        End If
                    End If
                Next

                Return builder.ToImmutableAndFree()
            End If
        End Function

        ''' <summary> 
        ''' Helper for implementing DeclaringSyntaxNodes for derived classes that store a location but not a  SyntaxNode or SyntaxReference. 
        ''' </summary>
        Friend Shared Function GetDeclaringSyntaxReferenceHelper(Of TNode As VisualBasicSyntaxNode)(locations As ImmutableArray(Of Location)) As ImmutableArray(Of SyntaxReference)
            Dim nodes = GetDeclaringSyntaxNodeHelper(Of TNode)(locations)

            If nodes.IsEmpty Then
                Return ImmutableArray(Of SyntaxReference).Empty
            Else
                Dim builder As ArrayBuilder(Of SyntaxReference) = ArrayBuilder(Of SyntaxReference).GetInstance()
                For Each node In nodes
                    builder.Add(node.GetReference())
                Next

                Return builder.ToImmutableAndFree()
            End If
        End Function

        ''' <summary> 
        ''' Helper for implementing DeclaringSyntaxNodes for derived classes that store SyntaxReferences. 
        ''' </summary>
        Friend Shared Function GetDeclaringSyntaxReferenceHelper(references As ImmutableArray(Of SyntaxReference)) As ImmutableArray(Of SyntaxReference)

            ' Optimize for the very common case of just one reference
            If references.Length = 1 Then
                Return GetDeclaringSyntaxReferenceHelper(references(0))
            End If

            Dim builder As ArrayBuilder(Of SyntaxReference) = ArrayBuilder(Of SyntaxReference).GetInstance()
            For Each reference In references
                Dim tree = reference.SyntaxTree
                If Not tree.IsEmbeddedOrMyTemplateTree() Then
                    builder.Add(New BeginOfBlockSyntaxReference(reference))
                End If
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Friend Shared Function GetDeclaringSyntaxReferenceHelper(reference As SyntaxReference) As ImmutableArray(Of SyntaxReference)
            If reference IsNot Nothing Then
                Dim tree = reference.SyntaxTree
                If Not tree.IsEmbeddedOrMyTemplateTree() Then
                    Return ImmutableArray.Create(Of SyntaxReference)(New BeginOfBlockSyntaxReference(reference))
                End If
            End If

            Return ImmutableArray(Of SyntaxReference).Empty
        End Function

        ''' <summary>
        ''' Get this accessibility that was declared on this symbol. For symbols that do
        ''' not have accessibility declared on them, returns NotApplicable.
        ''' </summary>
        Public MustOverride ReadOnly Property DeclaredAccessibility As Accessibility

        ''' <summary>
        ''' Returns true if this symbol is "shared"; i.e., declared with the "Shared"
        ''' modifier or implicitly always shared.
        ''' </summary>
        Public MustOverride ReadOnly Property IsShared As Boolean

        ''' <summary>
        ''' Returns true if this member is overridable, has an implementation,
        ''' and does not override a base class member; i.e., declared with the "Overridable"
        ''' modifier. Does not return true for members declared as MustOverride or Overrides.
        ''' </summary>
        Public MustOverride ReadOnly Property IsOverridable As Boolean

        ''' <summary>
        ''' Returns true if this symbol was declared to override a base class members; i.e., declared
        ''' with the "Overrides" modifier. Still returns true if the members was declared
        ''' to override something, but (erroneously) no member to override exists.
        ''' </summary>
        Public MustOverride ReadOnly Property IsOverrides As Boolean

        ''' <summary>
        ''' Returns true if this symbol was declared as requiring an override; i.e., declared
        ''' with the "MustOverride" modifier. Never returns true for types. 
        ''' Also methods, properties and events declared in interface are considered to have MustOverride.
        ''' </summary>
        Public MustOverride ReadOnly Property IsMustOverride As Boolean

        ''' <summary>
        ''' Returns true if this symbol was declared to override a base class members and was
        ''' also restricted from further overriding; i.e., declared with the "NotOverridable"
        ''' modifier. Never returns true for types.
        ''' </summary>
        Public MustOverride ReadOnly Property IsNotOverridable As Boolean

        ''' <summary>
        ''' Returns true if this symbol was automatically created by the compiler, and does not
        ''' have an explicit corresponding source code declaration.  
        ''' </summary>
        ''' <remarks>
        ''' This is intended for symbols that are ordinary symbols in the language sense,
        ''' and may be used by code, but that are simply declared implicitly rather than
        ''' with explicit language syntax.
        ''' 
        ''' Examples include (this list is not exhaustive):
        '''   the default constructor for a class or struct that is created if one is not provided,
        '''   the BeginInvoke/Invoke/EndInvoke methods for a delegate,
        '''   the generated backing field for an auto property or a field-like event,
        '''   the "this" parameter for non-static methods,
        '''   the "value" parameter for a property setter,
        '''   the parameters on indexer accessor methods (not on the indexer itself),
        '''   methods in anonymous types
        ''' </remarks>
        Public Overridable ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' True if this symbol has been marked with the Obsolete attribute. 
        ''' This property returns Unknown if the Obsolete Attribute hasn't been cracked yet.
        ''' </summary>
        Friend ReadOnly Property ObsoleteState As ThreeState
            Get
                Select Case ObsoleteKind
                    Case ObsoleteAttributeKind.None, ObsoleteAttributeKind.WindowsExperimental, ObsoleteAttributeKind.Experimental
                        Return ThreeState.False
                    Case ObsoleteAttributeKind.Uninitialized
                        Return ThreeState.Unknown
                    Case Else
                        Return ThreeState.True
                End Select
            End Get
        End Property

        Friend ReadOnly Property ObsoleteKind As ObsoleteAttributeKind
            Get
                Dim data = Me.ObsoleteAttributeData
                Return If(data Is Nothing, ObsoleteAttributeKind.None, data.Kind)
            End Get
        End Property

        ''' <summary>
        ''' Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        ''' This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        ''' </summary>
        Friend MustOverride ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData

        ''' <summary>
        ''' Returns the symbol that implicitly defined this symbol, or Nothing if this
        ''' symbol was declared explicitly. Examples of implicit symbols are property
        ''' accessors and the backing field for an automatically implemented property.
        ''' 
        ''' NOTE: there are scenarios in which ImplicitlyDefinedBy is called while bound members 
        '''       are not yet published. This typically happens if ImplicitlyDefinedBy while binding members.
        '''       In such case, if callee needs to refer to a member of enclosing type it must 
        '''       do that in the context of unpublished members that caller provides 
        '''       (asking encompassing type for members will cause infinite recursion).
        ''' 
        ''' NOTE: There could be several threads trying to bind and publish members, only one will succeed.
        '''       Reporting ImplicitlyDefinedBy within the set of members known to the caller guarantees
        '''       that if particular thread succeeds it will not have information that refers to something
        '''       built by another thread and discarded.
        ''' </summary>
        Friend Overridable ReadOnly Property ImplicitlyDefinedBy(Optional membersInProgress As Dictionary(Of String, ArrayBuilder(Of Symbol)) = Nothing) As Symbol
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary> 
        ''' Returns true if 'Shadows' is explicitly specified on the declaration if the symbol is from
        ''' source, or in cases of synthesized symbols, if 'Shadows' is specified on the associated
        ''' source symbol. (For instance, ShadowsExplicitly will be set on the backing fields and
        ''' accessors for properties and events based on the value from the property or event.)
        ''' Returns false in all other cases, in particular, for symbols not from source.
        ''' </summary>
        Friend Overridable ReadOnly Property ShadowsExplicitly As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true if this symbol can be referenced by its name in code. Examples of symbols
        ''' that cannot be referenced by name are:
        '''    constructors, operators, 
        '''    accessor methods for properties and events.
        ''' </summary>
        Public ReadOnly Property CanBeReferencedByName As Boolean
            Get
                Select Case Me.Kind
                    Case SymbolKind.Local,
                         SymbolKind.Label,
                         SymbolKind.Alias
                        ' Can't be imported, but might have syntax errors in which case we use an empty name:
                        Return Me.Name.Length > 0

                    Case SymbolKind.Namespace,
                         SymbolKind.Field,
                         SymbolKind.RangeVariable,
                         SymbolKind.Property,
                         SymbolKind.Event,
                         SymbolKind.Parameter,
                         SymbolKind.TypeParameter,
                         SymbolKind.ErrorType
                        Exit Select

                    Case SymbolKind.NamedType
                        If DirectCast(Me, NamedTypeSymbol).IsSubmissionClass Then
                            Return False
                        End If
                        Exit Select

                    Case SymbolKind.Method
                        Select Case DirectCast(Me, MethodSymbol).MethodKind
                            Case MethodKind.Ordinary, MethodKind.DeclareMethod, MethodKind.ReducedExtension
                                Exit Select
                            Case MethodKind.DelegateInvoke, MethodKind.UserDefinedOperator, MethodKind.Conversion
                                Return True
                            Case Else
                                Return False
                        End Select

                    Case SymbolKind.Assembly, SymbolKind.NetModule, SymbolKind.ArrayType
                        Return False

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(Me.Kind)
                End Select

                ' If we are from source, only need to check the first character, because all special
                ' names we create in source have a bad first character.
                ' NOTE: usually, the distinction we want to make is between the "current" compilation and another
                ' compilation, rather than between source and non-source.  In this case, however, it suffices
                ' to know that the symbol came from any compilation because we are just optimizing based on whether
                ' or not previous validation occurred.
                If Me.Dangerous_IsFromSomeCompilationIncludingRetargeting Then
                    Return Not String.IsNullOrEmpty(Me.Name) AndAlso SyntaxFacts.IsIdentifierStartCharacter(Me.Name(0))
                Else
                    Return SyntaxFacts.IsValidIdentifier(Me.Name)
                End If
            End Get
        End Property

        ''' <summary>
        ''' As an optimization, viability checking in the lookup code should use this property instead
        ''' of CanBeReferencedByName.
        ''' </summary>
        ''' <remarks>
        ''' This property exists purely for performance reasons.
        ''' </remarks>
        Friend ReadOnly Property CanBeReferencedByNameIgnoringIllegalCharacters As Boolean
            Get
                If Me.Kind = SymbolKind.Method Then
                    Select Case (DirectCast(Me, MethodSymbol)).MethodKind
                        Case MethodKind.Ordinary, MethodKind.DeclareMethod, MethodKind.ReducedExtension, MethodKind.DelegateInvoke, MethodKind.UserDefinedOperator, MethodKind.Conversion
                            Return True
                        Case Else
                            Return False
                    End Select
                End If
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Is this a symbol that is generated by the compiler and
        ''' automatically added to the compilation? Note that
        ''' only source symbols may be embedded symbols. 
        ''' 
        ''' Namespace symbol is considered to be an embedded symbol
        ''' if at least one of its declarations are embedded symbols.
        ''' </summary>
        Friend ReadOnly Property IsEmbedded As Boolean
            Get
                Return EmbeddedSymbolKind <> VisualBasic.Symbols.EmbeddedSymbolKind.None
            End Get
        End Property

        Friend Overridable ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
            Get
                Return EmbeddedSymbolKind.None
            End Get
        End Property

        ''' <summary>
        ''' <see cref="CharSet"/> effective for this symbol (type or DllImport method).
        ''' Nothing if <see cref="DefaultCharSetAttribute"/> isn't applied on the containing module or it doesn't apply on this symbol.
        ''' </summary>
        ''' <remarks>
        ''' Determined based upon value specified via <see cref="DefaultCharSetAttribute"/> applied on the containing module.
        ''' Symbols that are embedded are not affected by <see cref="DefaultCharSetAttribute"/> (see DevDiv bug #16434).
        ''' </remarks>
        Friend ReadOnly Property EffectiveDefaultMarshallingCharSet As CharSet?
            Get
                Return If(IsEmbedded, Nothing, Me.ContainingModule.DefaultMarshallingCharSet)
            End Get
        End Property

        Friend Function IsFromCompilation(compilation As VisualBasicCompilation) As Boolean
            Debug.Assert(compilation IsNot Nothing)
            Return compilation Is Me.DeclaringCompilation
        End Function

        ''' <summary>
        ''' Always prefer IsFromCompilation.
        ''' </summary>
        ''' <remarks>
        ''' This property is actually a triple workaround:
        ''' 
        ''' 1) Unfortunately, when determining overriding/hiding/implementation relationships, we don't
        '''    have the "current" compilation available.  We could, but that would clutter up the API
        '''    without providing much benefit.  As a compromise, we consider all compilations "current".
        ''' 
        ''' 2) TypeSymbol.Interfaces doesn't roundtrip in the presence of implicit interface implementation.
        '''    In particular, the metadata symbol may declare fewer interfaces than the source symbol so
        '''    that runtime implicit interface implementation will find the right symbol.  Thus, we need to
        '''    know what kind of symbol we are dealing with to be able to interpret the Interfaces property
        '''    properly.  Since a retargeting TypeSymbol will reflect the behavior of the underlying source
        '''    TypeSymbol, we need this property to match as well.  (C# does not have this problem.)
        ''' 
        ''' 3) The Dev12 VB compiler avoided loading private fields of structs from metadata, even though
        '''    they're supposed to affect definite assignment analysis.  For compatibility
        '''    we therefore ignore these fields when doing DA analysis.  (C# has a similar issue.)
        ''' </remarks>
        Friend ReadOnly Property Dangerous_IsFromSomeCompilationIncludingRetargeting As Boolean
            Get
                If Me.DeclaringCompilation IsNot Nothing Then
                    Return True
                End If

                If Me.Kind = SymbolKind.Assembly Then
                    Dim retargetingAssembly = TryCast(Me, Retargeting.RetargetingAssemblySymbol)
                    Return retargetingAssembly IsNot Nothing AndAlso retargetingAssembly.UnderlyingAssembly.DeclaringCompilation IsNot Nothing
                End If

                Dim [module] = If(Me.Kind = SymbolKind.NetModule, Me, Me.ContainingModule)
                Dim retargetingModule = TryCast([module], Retargeting.RetargetingModuleSymbol)
                Return retargetingModule IsNot Nothing AndAlso retargetingModule.UnderlyingModule.DeclaringCompilation IsNot Nothing
            End Get
        End Property

        ''' <summary>
        ''' Equivalent to MethodKind = MethodKind.LambdaMethod, but can be called on a symbol directly.
        ''' </summary>
        Friend Overridable ReadOnly Property IsLambdaMethod As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Is this an auto-generated property of a group class?
        ''' </summary>
        Friend Overridable ReadOnly Property IsMyGroupCollectionProperty As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Is this lambda method a query lambda? 
        ''' If it is, IsLambdaMethod == True as well.
        ''' </summary>
        Friend Overridable ReadOnly Property IsQueryLambdaMethod As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Returns true and a <see cref="String"/> from the first <see cref="GuidAttribute"/> on the symbol, 
        ''' the string might be null or an invalid guid representation. False, 
        ''' if there is no <see cref="GuidAttribute"/> with string argument.
        ''' </summary>
        Friend Function GetGuidStringDefaultImplementation(<Out> ByRef guidString As String) As Boolean
            For Each attrData In GetAttributes()
                If attrData.IsTargetAttribute(Me, AttributeDescription.GuidAttribute) Then
                    If attrData.TryGetGuidAttributeValue(guidString) Then
                        Return True
                    End If
                End If
            Next

            guidString = Nothing
            Return False
        End Function

        ''' <summary>
        ''' Returns the Documentation Comment ID for the symbol, or Nothing if the symbol
        ''' doesn't support documentation comments.
        ''' </summary>
        Public Overridable Function GetDocumentationCommentId() As String Implements ISymbol.GetDocumentationCommentId
            Dim pool = PooledStringBuilder.GetInstance()
            DocumentationCommentIdVisitor.Instance.Visit(Me, pool.Builder)

            Dim result = pool.ToStringAndFree()
            Return If(result.Length = 0, Nothing, result)
        End Function

        ''' <summary>
        ''' Fetches the documentation comment for this element with a cancellation token.
        ''' </summary>
        ''' <param name="preferredCulture">Optionally, retrieve the comments formatted for a particular culture. No impact on source documentation comments.</param>
        ''' <param name="expandIncludes">Optionally, expand <![CDATA[<include>]]> elements. No impact on non-source documentation comments.</param>
        ''' <param name="cancellationToken">Optionally, allow cancellation of documentation comment retrieval.</param>
        ''' <returns>The XML that would be written to the documentation file for the symbol.</returns>
        Public Overridable Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String Implements ISymbol.GetDocumentationCommentXml
            Return ""
        End Function

        ''' <summary>
        ''' Compare two symbol objects to see if they refer to the same symbol. You should always use
        ''' = and &lt;&gt; or the Equals method, to compare two symbols for equality.
        ''' </summary>
        Public Shared Operator =(left As Symbol, right As Symbol) As Boolean
            'PERF: this Function() Is often called with
            '      1) left referencing same object as the right 
            '      2) right being null
            '      The code attempts to check for these conditions before 
            '      resorting to .Equals

            If (right Is Nothing) Then
                Return left Is Nothing
            End If

            Return left Is right OrElse right.Equals(left)
        End Operator

        ''' <summary>
        ''' Compare two symbol objects to see if they refer to the same symbol. You should always use
        ''' = and &lt;&gt;, or the Equals method, to compare two symbols for equality.
        ''' </summary>
        Public Shared Operator <>(left As Symbol, right As Symbol) As Boolean
            'PERF: this Function() Is often called with
            '      1) left referencing same object as the right 
            '      2) right being null
            '      The code attempts to check for these conditions before 
            '      resorting to .Equals

            If (right Is Nothing) Then
                Return left IsNot Nothing
            End If

            Return left IsNot right AndAlso Not right.Equals(left)
        End Operator

        ' By default, we do reference equality. This can be overridden.
        Public Overrides Function [Equals](obj As Object) As Boolean
            Return Me Is obj
        End Function

        Private Overloads Function IEquatable_Equals(other As ISymbol) As Boolean Implements IEquatable(Of ISymbol).Equals
            Return Me.[Equals](TryCast(other, Symbol), SymbolEqualityComparer.Default.CompareKind)
        End Function

        Private Overloads Function ISymbol_Equals(other As ISymbol, equalityComparer As SymbolEqualityComparer) As Boolean Implements ISymbol.Equals
            Return Me.[Equals](TryCast(other, Symbol), equalityComparer.CompareKind)
        End Function

        Private Overloads Function ISymbolInternal_Equals(other As ISymbolInternal, compareKind As TypeCompareKind) As Boolean Implements ISymbolInternal.Equals
            Return Me.Equals(TryCast(other, Symbol), compareKind)
        End Function

        ' By default we don't consider the compareKind. This can be overridden.
        Public Overridable Overloads Function Equals(other As Symbol, compareKind As TypeCompareKind) As Boolean
            Return Me.Equals(other)
        End Function

        ' By default, we do reference equality. This can be overridden.
        Public Overrides Function GetHashCode() As Integer
            Return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Me)
        End Function

        Public NotOverridable Overrides Function ToString() As String
            Return ToDisplayString(SymbolDisplayFormat.VisualBasicErrorMessageFormat)
        End Function

        Public Function ToDisplayString(Optional format As SymbolDisplayFormat = Nothing) As String
            Return Display.ToDisplayString(Me, format)
        End Function

        Public Function ToDisplayParts(Optional format As SymbolDisplayFormat = Nothing) As ImmutableArray(Of SymbolDisplayPart)
            Return Display.ToDisplayParts(Me, format)
        End Function

        Public Function ToMinimalDisplayString(semanticModel As SemanticModel, position As Integer, Optional format As SymbolDisplayFormat = Nothing) As String
            Return Display.ToMinimalDisplayString(Me, semanticModel, position, format)
        End Function

        Public Function ToMinimalDisplayParts(semanticModel As SemanticModel, position As Integer, Optional format As SymbolDisplayFormat = Nothing) As ImmutableArray(Of SymbolDisplayPart)
            Return Display.ToMinimalDisplayParts(Me, semanticModel, position, format)
        End Function

        Private Function GetDebuggerDisplay() As String
            Return String.Format("{0} {1}", Me.Kind, Me.ToDisplayString(SymbolDisplayFormat.TestFormat))
        End Function

        ' ---- End of Public Definition ---
        ' Below here can be Friend members that are useful to the compiler, but we don't
        ' want to expose publicly. However, using a class derived from SymbolVisitor can be
        ' a way to add the equivalent of a virtual method, but without having to put it directly
        ' in the Symbol class.
        ' ---- End of Public Definition ---

        Friend MustOverride Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult

        ' Prevent anyone else from deriving from this class.
        Friend Sub New()
        End Sub

        ' Returns true if some or all of the symbol is defined in the given source tree.
        Friend Overridable Function IsDefinedInSourceTree(tree As SyntaxTree, definedWithinSpan As TextSpan?, Optional cancellationToken As CancellationToken = Nothing) As Boolean Implements ISymbolInternal.IsDefinedInSourceTree
            Dim declaringReferences = Me.DeclaringSyntaxReferences
            If Me.IsImplicitlyDeclared AndAlso declaringReferences.Length = 0 Then
                Return Me.ContainingSymbol.IsDefinedInSourceTree(tree, definedWithinSpan, cancellationToken)
            End If

            ' Default implementation: go through all locations and check for the definition.
            ' This is overridden for certain special cases (e.g., the implicit default constructor).
            For Each syntaxRef In declaringReferences
                cancellationToken.ThrowIfCancellationRequested()

                If syntaxRef.SyntaxTree Is tree AndAlso
                    (Not definedWithinSpan.HasValue OrElse syntaxRef.Span.IntersectsWith(definedWithinSpan.Value)) Then
                    Return True
                End If
            Next

            Return False
        End Function

        Friend Shared Function IsDefinedInSourceTree(syntaxNode As SyntaxNode, tree As SyntaxTree, definedWithinSpan As TextSpan?, Optional cancellationToken As CancellationToken = Nothing) As Boolean
            Return syntaxNode IsNot Nothing AndAlso
                syntaxNode.SyntaxTree Is tree AndAlso
                (Not definedWithinSpan.HasValue OrElse definedWithinSpan.Value.IntersectsWith(syntaxNode.FullSpan))
        End Function

        ''' <summary>
        ''' Force all declaration diagnostics to be generated for the symbol.
        ''' </summary>
        Friend Overridable Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
        End Sub

#Region "Use-Site Diagnostic"

        ''' <summary>
        ''' Returns dependencies and an error info for an error, if any, that should be reported at the use site of the symbol.
        ''' </summary>
        Friend Overridable Function GetUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            Return Nothing
        End Function

        Friend ReadOnly Property PrimaryDependency As AssemblySymbol
            Get
                Dim dependency As AssemblySymbol = Me.ContainingAssembly
                If dependency IsNot Nothing AndAlso dependency.CorLibrary = dependency Then
                    Return Nothing
                End If

                Return dependency
            End Get
        End Property

        ''' <summary>
        ''' Indicates that this symbol uses metadata that cannot be supported by the language.
        ''' 
        ''' Examples include:
        '''    - Pointer types in VB
        '''    - ByRef return type
        '''    - Required custom modifiers
        '''    
        ''' This is distinguished from, for example, references to metadata symbols defined in assemblies that weren't referenced.
        ''' Symbols where this returns true can never be used successfully, and thus should never appear in any IDE feature.
        ''' 
        ''' This is set for metadata symbols, as follows:
        ''' Type - if a type is unsupported (e.g., a pointer type, etc.)
        ''' Method - parameter or return type is unsupported
        ''' Field - type is unsupported
        ''' Event - type is unsupported
        ''' Property - type is unsupported
        ''' Parameter - type is unsupported
        ''' </summary>
        Public Overridable ReadOnly Property HasUnsupportedMetadata As Boolean Implements ISymbol.HasUnsupportedMetadata
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Derive dependencies and error info from a type symbol.
        ''' </summary>
        Friend Function DeriveUseSiteInfoFromType(type As TypeSymbol) As UseSiteInfo(Of AssemblySymbol)
            Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = type.GetUseSiteInfo()

            If useSiteInfo.DiagnosticInfo IsNot Nothing Then
                Select Case useSiteInfo.DiagnosticInfo.Code
                    Case ERRID.ERR_UnsupportedType1

                        GetSymbolSpecificUnsupportedMetadataUseSiteErrorInfo(useSiteInfo)

                    Case Else
                        ' Nothing to do, simply use the same error info.
                End Select
            End If

            Return useSiteInfo
        End Function

        Private Sub GetSymbolSpecificUnsupportedMetadataUseSiteErrorInfo(ByRef useSiteInfo As UseSiteInfo(Of AssemblySymbol))
            Select Case Me.Kind
                Case SymbolKind.Field
                    useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedField1, CustomSymbolDisplayFormatter.ShortErrorName(Me)))

                Case SymbolKind.Method
                    useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedMethod1, CustomSymbolDisplayFormatter.ShortErrorName(Me)))

                Case SymbolKind.Property
                    useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedProperty1, CustomSymbolDisplayFormatter.ShortErrorName(Me)))
            End Select
        End Sub

        ''' <summary>
        ''' Returns true if the error code is highest priority while calculating use site error for this symbol. 
        ''' </summary>
        Protected Overridable Function IsHighestPriorityUseSiteError(code As Integer) As Boolean ' Supposed to be ERRID, but it causes inconsistent accessibility error.
            Return False
        End Function

        Friend Function MergeUseSiteInfo(ByRef result As UseSiteInfo(Of AssemblySymbol), other As UseSiteInfo(Of AssemblySymbol)) As Boolean
            If other.DiagnosticInfo IsNot Nothing AndAlso IsHighestPriorityUseSiteError(other.DiagnosticInfo.Code) Then
                result = other
                Return True
            End If

            If result.DiagnosticInfo Is Nothing Then
                If other.DiagnosticInfo IsNot Nothing Then
                    result = other
                Else
                    Dim primaryDependency = result.PrimaryDependency
                    Dim secondaryDependency = result.SecondaryDependencies

                    other.MergeDependencies(primaryDependency, secondaryDependency)
                    result = New UseSiteInfo(Of AssemblySymbol)(diagnosticInfo:=Nothing, primaryDependency, secondaryDependency)
                End If

                Return False
            Else
                Return IsHighestPriorityUseSiteError(result.DiagnosticInfo.Code)
            End If
        End Function

        Friend Function DeriveUseSiteInfoFromParameter(param As ParameterSymbol) As UseSiteInfo(Of AssemblySymbol)
            Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = DeriveUseSiteInfoFromType(param.Type)

            If useSiteInfo.DiagnosticInfo IsNot Nothing AndAlso IsHighestPriorityUseSiteError(useSiteInfo.DiagnosticInfo.Code) Then
                Return useSiteInfo
            End If

            Dim refModifiersUseSiteInfo As UseSiteInfo(Of AssemblySymbol) = DeriveUseSiteInfoFromCustomModifiers(param.RefCustomModifiers)

            If refModifiersUseSiteInfo.DiagnosticInfo IsNot Nothing AndAlso IsHighestPriorityUseSiteError(refModifiersUseSiteInfo.DiagnosticInfo.Code) Then
                Return refModifiersUseSiteInfo
            End If

            Dim modifiersUseSiteInfo As UseSiteInfo(Of AssemblySymbol) = DeriveUseSiteInfoFromCustomModifiers(param.CustomModifiers)

            If modifiersUseSiteInfo.DiagnosticInfo IsNot Nothing AndAlso IsHighestPriorityUseSiteError(modifiersUseSiteInfo.DiagnosticInfo.Code) Then
                Return modifiersUseSiteInfo
            End If

            Dim errorInfo = If(useSiteInfo.DiagnosticInfo, If(refModifiersUseSiteInfo.DiagnosticInfo, modifiersUseSiteInfo.DiagnosticInfo))

            If errorInfo IsNot Nothing Then
                Return New UseSiteInfo(Of AssemblySymbol)(errorInfo)
            End If

            Dim primaryDependency = useSiteInfo.PrimaryDependency
            Dim secondaryDependency = useSiteInfo.SecondaryDependencies

            refModifiersUseSiteInfo.MergeDependencies(primaryDependency, secondaryDependency)
            modifiersUseSiteInfo.MergeDependencies(primaryDependency, secondaryDependency)

            Return New UseSiteInfo(Of AssemblySymbol)(diagnosticInfo:=Nothing, primaryDependency, secondaryDependency)
        End Function

        Friend Function DeriveUseSiteInfoFromParameters(parameters As ImmutableArray(Of ParameterSymbol)) As UseSiteInfo(Of AssemblySymbol)
            Dim paramsUseSiteInfo As UseSiteInfo(Of AssemblySymbol) = Nothing

            For Each param As ParameterSymbol In parameters
                If MergeUseSiteInfo(paramsUseSiteInfo, DeriveUseSiteInfoFromParameter(param)) Then
                    Exit For
                End If
            Next

            Return paramsUseSiteInfo
        End Function

        Friend Function DeriveUseSiteInfoFromCustomModifiers(
            customModifiers As ImmutableArray(Of CustomModifier),
            Optional allowIsExternalInit As Boolean = False
        ) As UseSiteInfo(Of AssemblySymbol)
            Dim modifiersUseSiteInfo As UseSiteInfo(Of AssemblySymbol) = Nothing

            For Each modifier As CustomModifier In customModifiers
                Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol)

                If Not modifier.IsOptional AndAlso
                   (Not allowIsExternalInit OrElse Not DirectCast(modifier, VisualBasicCustomModifier).ModifierSymbol.IsWellKnownTypeIsExternalInit()) Then

                    useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedType1, String.Empty))
                    GetSymbolSpecificUnsupportedMetadataUseSiteErrorInfo(useSiteInfo)

                    If MergeUseSiteInfo(modifiersUseSiteInfo, useSiteInfo) Then
                        Exit For
                    End If
                End If

                useSiteInfo = DeriveUseSiteInfoFromType(DirectCast(modifier, VisualBasicCustomModifier).ModifierSymbol)

                If MergeUseSiteInfo(modifiersUseSiteInfo, useSiteInfo) Then
                    Exit For
                End If
            Next

            Return modifiersUseSiteInfo
        End Function

        Friend Overloads Shared Function GetUnificationUseSiteDiagnosticRecursive(Of T As TypeSymbol)(types As ImmutableArray(Of T), owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
            For Each t In types
                Dim info = t.GetUnificationUseSiteDiagnosticRecursive(owner, checkedTypes)
                If info IsNot Nothing Then
                    Return info
                End If
            Next

            Return Nothing
        End Function

        Friend Overloads Shared Function GetUnificationUseSiteDiagnosticRecursive(modifiers As ImmutableArray(Of CustomModifier), owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
            For Each modifier In modifiers
                Dim info = DirectCast(modifier.Modifier, TypeSymbol).GetUnificationUseSiteDiagnosticRecursive(owner, checkedTypes)
                If info IsNot Nothing Then
                    Return info
                End If
            Next

            Return Nothing
        End Function

        Friend Overloads Shared Function GetUnificationUseSiteDiagnosticRecursive(parameters As ImmutableArray(Of ParameterSymbol), owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
            For Each parameter In parameters
                Dim info = If(parameter.Type.GetUnificationUseSiteDiagnosticRecursive(owner, checkedTypes),
                              If(GetUnificationUseSiteDiagnosticRecursive(parameter.RefCustomModifiers, owner, checkedTypes),
                                    GetUnificationUseSiteDiagnosticRecursive(parameter.CustomModifiers, owner, checkedTypes)))

                If info IsNot Nothing Then
                    Return info
                End If
            Next

            Return Nothing
        End Function

        Friend Overloads Shared Function GetUnificationUseSiteDiagnosticRecursive(typeParameters As ImmutableArray(Of TypeParameterSymbol), owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
            For Each typeParameter In typeParameters
                Dim info = GetUnificationUseSiteDiagnosticRecursive(typeParameter.ConstraintTypesNoUseSiteDiagnostics, owner, checkedTypes)
                If info IsNot Nothing Then
                    Return info
                End If
            Next

            Return Nothing
        End Function

#End Region

#Region "ISymbol"

        Public MustOverride Sub Accept(visitor As SymbolVisitor) Implements ISymbol.Accept

        Public MustOverride Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult Implements ISymbol.Accept

        Public MustOverride Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult Implements ISymbol.Accept

        Public MustOverride Sub Accept(visitor As VisualBasicSymbolVisitor)

        Public MustOverride Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult

        Private ReadOnly Property ISymbol_ContainingAssembly As IAssemblySymbol Implements ISymbol.ContainingAssembly
            Get
                Return Me.ContainingAssembly
            End Get
        End Property

        Private ReadOnly Property ISymbolInternal_ContainingAssembly As IAssemblySymbolInternal Implements ISymbolInternal.ContainingAssembly
            Get
                Return Me.ContainingAssembly
            End Get
        End Property

        Private ReadOnly Property ISymbol_ContainingModule As IModuleSymbol Implements ISymbol.ContainingModule
            Get
                Return Me.ContainingModule
            End Get
        End Property

        Private ReadOnly Property ISymbolInternal_ContainingModule As IModuleSymbolInternal Implements ISymbolInternal.ContainingModule
            Get
                Return Me.ContainingModule
            End Get
        End Property

        Private ReadOnly Property ISymbol_ContainingNamespace As INamespaceSymbol Implements ISymbol.ContainingNamespace
            Get
                Return Me.ContainingNamespace
            End Get
        End Property

        Private ReadOnly Property ISymbolInternal_ContainingNamespace As INamespaceSymbolInternal Implements ISymbolInternal.ContainingNamespace
            Get
                Return Me.ContainingNamespace
            End Get
        End Property

        Private ReadOnly Property ISymbol_ContainingSymbol As ISymbol Implements ISymbol.ContainingSymbol
            Get
                Return Me.ContainingSymbol
            End Get
        End Property

        Private ReadOnly Property ISymbolInternal_ContainingSymbol As ISymbolInternal Implements ISymbolInternal.ContainingSymbol
            Get
                Return Me.ContainingSymbol
            End Get
        End Property

        Private ReadOnly Property ISymbol_ContainingType As INamedTypeSymbol Implements ISymbol.ContainingType
            Get
                Return Me.ContainingType
            End Get
        End Property

        Private ReadOnly Property ISymbolInternal_ContainingType As INamedTypeSymbolInternal Implements ISymbolInternal.ContainingType
            Get
                Return Me.ContainingType
            End Get
        End Property

        Private ReadOnly Property ISymbol_DeclaredAccessibility As Accessibility Implements ISymbol.DeclaredAccessibility, ISymbolInternal.DeclaredAccessibility
            Get
                Return Me.DeclaredAccessibility
            End Get
        End Property

        Protected Overridable ReadOnly Property ISymbol_IsAbstract As Boolean Implements ISymbol.IsAbstract, ISymbolInternal.IsAbstract
            Get
                Return Me.IsMustOverride
            End Get
        End Property

        Private ReadOnly Property ISymbol_IsDefinition As Boolean Implements ISymbol.IsDefinition, ISymbolInternal.IsDefinition
            Get
                Return Me.IsDefinition
            End Get
        End Property

        Private ReadOnly Property ISymbol_IsOverride As Boolean Implements ISymbol.IsOverride, ISymbolInternal.IsOverride
            Get
                Return Me.IsOverrides
            End Get
        End Property

        Protected Overridable ReadOnly Property ISymbol_IsSealed As Boolean Implements ISymbol.IsSealed
            Get
                Return Me.IsNotOverridable
            End Get
        End Property

        Protected Overridable ReadOnly Property ISymbol_IsStatic As Boolean Implements ISymbol.IsStatic, ISymbolInternal.IsStatic
            Get
                Return Me.IsShared
            End Get
        End Property

        Private ReadOnly Property ISymbol_IsImplicitlyDeclared As Boolean Implements ISymbol.IsImplicitlyDeclared, ISymbolInternal.IsImplicitlyDeclared
            Get
                Return Me.IsImplicitlyDeclared
            End Get
        End Property

        Private ReadOnly Property ISymbol_IsVirtual As Boolean Implements ISymbol.IsVirtual, ISymbolInternal.IsVirtual
            Get
                Return Me.IsOverridable
            End Get
        End Property

        Private ReadOnly Property ISymbol_CanBeReferencedByName As Boolean Implements ISymbol.CanBeReferencedByName
            Get
                Return Me.CanBeReferencedByName
            End Get
        End Property

        Public ReadOnly Property Language As String Implements ISymbol.Language
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Private ReadOnly Property ISymbol_Locations As ImmutableArray(Of Location) Implements ISymbol.Locations, ISymbolInternal.Locations
            Get
                Return Me.Locations
            End Get
        End Property

        Private ReadOnly Property ISymbol_DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference) Implements ISymbol.DeclaringSyntaxReferences
            Get
                Return Me.DeclaringSyntaxReferences
            End Get
        End Property

        Private ReadOnly Property ISymbol_Name As String Implements ISymbol.Name, ISymbolInternal.Name
            Get
                Return Me.Name
            End Get
        End Property

        Private ReadOnly Property ISymbol_OriginalDefinition As ISymbol Implements ISymbol.OriginalDefinition
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        Private ReadOnly Property ISymbol_Kind As SymbolKind Implements ISymbol.Kind, ISymbolInternal.Kind
            Get
                Return Me.Kind
            End Get
        End Property

        Private Function ISymbol_ToDisplayString(Optional format As SymbolDisplayFormat = Nothing) As String Implements ISymbol.ToDisplayString
            Return Display.ToDisplayString(Me, format)
        End Function

        Private Function ISymbol_ToDisplayParts(Optional format As SymbolDisplayFormat = Nothing) As ImmutableArray(Of SymbolDisplayPart) Implements ISymbol.ToDisplayParts
            Return Display.ToDisplayParts(Me, format)
        End Function

        Private Function ISymbol_ToMinimalDisplayString(semanticModel As SemanticModel, position As Integer, Optional format As SymbolDisplayFormat = Nothing) As String Implements ISymbol.ToMinimalDisplayString
            Return Display.ToMinimalDisplayString(Me, semanticModel, position, format)
        End Function

        Private Function ISymbol_ToMinimalDisplayParts(semanticModel As SemanticModel, position As Integer, Optional format As SymbolDisplayFormat = Nothing) As ImmutableArray(Of SymbolDisplayPart) Implements ISymbol.ToMinimalDisplayParts
            Return Display.ToMinimalDisplayParts(Me, semanticModel, position, format)
        End Function

        Private ReadOnly Property ISymbol_IsExtern As Boolean Implements ISymbol.IsExtern
            Get
                Return False
            End Get
        End Property

        Private Function ISymbol_GetAttributes() As ImmutableArray(Of AttributeData) Implements ISymbol.GetAttributes
            Return StaticCast(Of AttributeData).From(Me.GetAttributes())
        End Function

#End Region

        Protected Shared Function ConstructTypeArguments(ParamArray typeArguments() As ITypeSymbol) As ImmutableArray(Of TypeSymbol)
            Dim builder = ArrayBuilder(Of TypeSymbol).GetInstance(typeArguments.Length)
            For Each typeArg In typeArguments
                builder.Add(typeArg.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(typeArguments)))
            Next
            Return builder.ToImmutableAndFree()
        End Function

        Protected Shared Function ConstructTypeArguments(typeArguments As ImmutableArray(Of ITypeSymbol), typeArgumentNullableAnnotations As ImmutableArray(Of CodeAnalysis.NullableAnnotation)) As ImmutableArray(Of TypeSymbol)
            If typeArguments.IsDefault Then
                Throw New ArgumentException(NameOf(typeArguments))
            End If

            Dim n = typeArguments.Length
            If Not typeArgumentNullableAnnotations.IsDefault AndAlso typeArgumentNullableAnnotations.Length <> n Then
                Throw New ArgumentException(NameOf(typeArgumentNullableAnnotations))
            End If

            Return typeArguments.SelectAsArray(Function(typeArg) typeArg.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(typeArguments)))
        End Function

        Private Overloads Function IFormattable_ToString(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Return ToString()
        End Function

        Private Function ISymbolInternal_GetISymbol() As ISymbol Implements ISymbolInternal.GetISymbol
            Return Me
        End Function

    End Class
End Namespace
