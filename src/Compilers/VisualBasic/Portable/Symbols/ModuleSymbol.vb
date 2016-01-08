' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a module within an assembly. Every assembly contains one or more modules.
    ''' </summary>
    Friend MustInherit Class ModuleSymbol
        Inherits Symbol
        Implements IModuleSymbol

        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ' Changes to the public interface of this class should remain synchronized with the C# version of Symbol.
        ' Do not make any changes to the public interface without making the corresponding change
        ' to the C# version.
        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        Friend Sub New()
        End Sub

        ''' <summary>
        ''' Module's ordinal within containing assembly's Modules array.
        ''' 0 - for a source module, etc.
        ''' -1 - for a module that doesn't have containing assembly, or has it, but is not part of Modules array. 
        ''' </summary>
        Friend MustOverride ReadOnly Property Ordinal As Integer

        ''' <summary>
        ''' Target architecture of the machine.
        ''' </summary>
        Friend MustOverride ReadOnly Property Machine As System.Reflection.PortableExecutable.Machine

        ''' <summary>
        ''' Indicates that this PE file makes Win32 calls. See CorPEKind.pe32BitRequired for more information (http://msdn.microsoft.com/en-us/library/ms230275.aspx).
        ''' </summary>
        Friend MustOverride ReadOnly Property Bit32Required As Boolean

        Public NotOverridable Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.NotApplicable
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        ''' <summary>
        ''' Returns a NamespaceSymbol representing the global (root) namespace, with
        ''' module extent, that can be used to browse all of the symbols defined in this module.
        ''' </summary>
        Public MustOverride ReadOnly Property GlobalNamespace As NamespaceSymbol

        ''' <summary>
        ''' Returns the containing assembly. Modules are always directly contained by an assembly,
        ''' so this property always returns the same as ContainingSymbol.
        ''' </summary>
        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return DirectCast(ContainingSymbol, AssemblySymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return Nothing
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.NetModule
            End Get
        End Property

        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitModule(Me, arg)
        End Function

        ''' <summary>
        ''' Returns an array of assembly identities for assemblies referenced by this module.
        ''' Items at the same position from ReferencedAssemblies And from ReferencedAssemblySymbols 
        ''' correspond to each other.
        ''' </summary>
        Public ReadOnly Property ReferencedAssemblies As ImmutableArray(Of AssemblyIdentity) Implements IModuleSymbol.ReferencedAssemblies
            Get
                Return GetReferencedAssemblies()
            End Get
        End Property

        ''' <summary>
        ''' If this symbol represents a metadata module returns the underlying <see cref="ModuleMetadata"/>.
        ''' 
        ''' Otherwise, this returns <code>null</code>.
        ''' </summary>
        Public MustOverride Function GetMetadata() As ModuleMetadata Implements IModuleSymbol.GetMetadata

        ''' <summary>
        ''' Returns an array of assembly identities for assemblies referenced by this module.
        ''' Items at the same position from GetReferencedAssemblies and from GetReferencedAssemblySymbols 
        ''' should correspond to each other.
        ''' 
        ''' The array and its content is provided by ReferenceManager and must not be modified.
        ''' </summary>
        Friend MustOverride Function GetReferencedAssemblies() As ImmutableArray(Of AssemblyIdentity) ' TODO: Remove this method and make ReferencedAssemblies property abstract instead.

        ''' <summary>
        ''' Returns an array of AssemblySymbol objects corresponding to assemblies referenced 
        ''' by this module. Items at the same position from ReferencedAssemblies And 
        ''' from ReferencedAssemblySymbols correspond to each other.
        ''' </summary>
        Public ReadOnly Property ReferencedAssemblySymbols As ImmutableArray(Of AssemblySymbol)
            Get
                Return GetReferencedAssemblySymbols()
            End Get
        End Property

        ''' <summary>
        ''' Returns an array of AssemblySymbol objects corresponding to assemblies referenced 
        ''' by this module. Items at the same position from GetReferencedAssemblies and 
        ''' from GetReferencedAssemblySymbols should correspond to each other. If reference is 
        ''' not resolved by compiler, GetReferencedAssemblySymbols returns MissingAssemblySymbol in the
        ''' corresponding item.
        ''' 
        ''' The array and its content is provided by ReferenceManager and must not be modified.
        ''' </summary>
        Friend MustOverride Function GetReferencedAssemblySymbols() As ImmutableArray(Of AssemblySymbol) ' TODO: Remove this method and make ReferencedAssemblySymbols property abstract instead.

        ''' <summary>
        ''' A helper method for ReferenceManager to set assembly identities for assemblies
        ''' referenced by this module and corresponding AssemblySymbols.
        ''' </summary>
        ''' <param name="moduleReferences">A description of the assemblies referenced by this module.</param>
        '''
        ''' <param name="originatingSourceAssemblyDebugOnly">
        ''' Source assembly that triggered creation of this module symbol.
        ''' For debug purposes only, this assembly symbol should not be persisted within
        ''' this module symbol because the module can be shared across multiple source
        ''' assemblies. This method will only be called for the first one.
        ''' </param>
        Friend MustOverride Sub SetReferences(
            moduleReferences As ModuleReferences(Of AssemblySymbol),
            Optional originatingSourceAssemblyDebugOnly As SourceAssemblySymbol = Nothing)

        ''' <summary>
        ''' True if this module has any unified references.
        ''' </summary>
        Friend MustOverride ReadOnly Property HasUnifiedReferences As Boolean

        ''' <summary> 
        ''' Returns a unification use-site error (if any) for a symbol contained in this module 
        ''' that is referring to a specified <paramref name="dependentType"/>.
        ''' </summary> 
        ''' <remarks> 
        ''' If an assembly referenced by this module isn't exactly matching any reference given to compilation 
        ''' the Assembly Manager might decide to use another reference if it matches except for version 
        ''' (it unifies the version with the existing reference).  
        ''' </remarks>
        Friend MustOverride Function GetUnificationUseSiteErrorInfo(dependentType As TypeSymbol) As DiagnosticInfo

        ''' <summary>
        ''' Lookup a top level type referenced from metadata, names should be
        ''' compared case-sensitively.
        ''' </summary>
        ''' <param name="emittedName">
        ''' Full type name possibly with generic name mangling.
        ''' </param>
        ''' <returns>
        ''' Symbol for the type, or MissingMetadataSymbol if the type isn't found.
        ''' </returns>
        ''' <remarks></remarks>
        Friend MustOverride Function LookupTopLevelMetadataType(
            ByRef emittedName As MetadataTypeName
        ) As NamedTypeSymbol

        Friend MustOverride ReadOnly Property TypeNames As ICollection(Of String)

        Friend MustOverride ReadOnly Property NamespaceNames As ICollection(Of String)

        ''' <summary>
        ''' Returns true if there is any applied CompilationRelaxationsAttribute assembly attribute for this module.
        ''' </summary>
        Friend MustOverride ReadOnly Property HasAssemblyCompilationRelaxationsAttribute As Boolean

        ''' <summary>
        ''' Returns true if there is any applied RuntimeCompatibilityAttribute assembly attribute for this module.
        ''' </summary>
        Friend MustOverride ReadOnly Property HasAssemblyRuntimeCompatibilityAttribute As Boolean

        ''' <summary>
        ''' Default char set for contained types, or null if not specified.
        ''' </summary>
        ''' <remarks>
        ''' Determined based upon value specified via <see cref="DefaultCharSetAttribute"/> applied on this module.
        ''' </remarks>
        Friend MustOverride ReadOnly Property DefaultMarshallingCharSet As CharSet?

        Friend Overridable Function GetHash(algorithmId As AssemblyHashAlgorithm) As ImmutableArray(Of Byte)
            Throw ExceptionUtilities.Unreachable
        End Function

        ''' <summary>
        ''' Given a namespace symbol, returns the corresponding module specific namespace symbol
        ''' </summary>
        Public Function GetModuleNamespace(namespaceSymbol As INamespaceSymbol) As NamespaceSymbol
            If namespaceSymbol Is Nothing Then
                Throw New ArgumentNullException(NameOf(namespaceSymbol))
            End If

            Dim moduleNs = TryCast(namespaceSymbol, NamespaceSymbol)
            If moduleNs IsNot Nothing And moduleNs.Extent.Kind = NamespaceKind.Module And moduleNs.ContainingModule = Me Then
                Return moduleNs
            End If

            If namespaceSymbol.IsGlobalNamespace Or namespaceSymbol.ContainingNamespace Is Nothing Then
                Return Me.GlobalNamespace
            Else
                Dim cns = GetModuleNamespace(namespaceSymbol.ContainingNamespace)
                If cns IsNot Nothing Then
                    Return cns.GetNestedNamespace(namespaceSymbol.Name)
                End If
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Does this symbol represent a missing Module.
        ''' </summary>
        Friend MustOverride ReadOnly Property IsMissing As Boolean

        ''' <summary>
        ''' If this property returns false, it is certain that there are no extension
        ''' methods (from language perspective) inside this module. If this property returns true, 
        ''' it is highly likely (but not certain) that this type contains extension methods. 
        ''' This property allows the search for extension methods to be narrowed much more quickly.
        ''' 
        ''' !!! Note that this property can mutate during lifetime of the symbol !!!
        ''' !!! from True to False, as we learn more about the module.           !!! 
        ''' </summary>
        Friend MustOverride ReadOnly Property MightContainExtensionMethods As Boolean

        ''' <summary>
        ''' Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        ''' This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        ''' </summary>
        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

#Region "IModuleSymbol"
        Private ReadOnly Property IModuleSymbol_GlobalNamespace As INamespaceSymbol Implements IModuleSymbol.GlobalNamespace
            Get
                Return Me.GlobalNamespace
            End Get
        End Property

        Private Function IModuleSymbol_GetModuleNamespace(namespaceSymbol As INamespaceSymbol) As INamespaceSymbol Implements IModuleSymbol.GetModuleNamespace
            Return Me.GetModuleNamespace(namespaceSymbol)
        End Function

        Private ReadOnly Property IModuleSymbol_ReferencedAssemblySymbols As ImmutableArray(Of IAssemblySymbol) Implements IModuleSymbol.ReferencedAssemblySymbols
            Get
                Return ImmutableArray(Of IAssemblySymbol).CastUp(ReferencedAssemblySymbols)
            End Get
        End Property

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitModule(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitModule(Me)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitModule(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitModule(Me)
        End Function

#End Region

    End Class
End Namespace
