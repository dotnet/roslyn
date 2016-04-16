' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Runtime.InteropServices
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind
Imports System.Globalization
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting

    ''' <summary>
    ''' Represents a primary module of a <see cref="RetargetingAssemblySymbol"/>. Essentially this is a wrapper around 
    ''' another <see cref="SourceModuleSymbol"/> that is responsible for retargeting symbols from one assembly to another. 
    ''' It can retarget symbols for multiple assemblies at the same time.
    ''' 
    ''' Here is how retargeting is implemented in general:
    ''' - Symbols from underlying module are substituted with retargeting symbols.
    ''' - Symbols from referenced assemblies that can be reused as is (i.e. don't have to be retargeted) are
    '''   used as is.
    ''' - Symbols from referenced assemblies that must be retargeted are substituted with result of retargeting.
    ''' </summary>
    Friend NotInheritable Class RetargetingModuleSymbol
        Inherits NonMissingModuleSymbol

        ''' <summary>
        ''' Owning <see cref="RetargetingAssemblySymbol"/>.
        ''' </summary>
        Private ReadOnly _retargetingAssembly As RetargetingAssemblySymbol

        ''' <summary>
        ''' The underlying <see cref="ModuleSymbol"/>, cannot be another <see cref="RetargetingModuleSymbol"/>.
        ''' </summary>
        Private ReadOnly _underlyingModule As SourceModuleSymbol

        ''' <summary>
        ''' The map that captures information about what assembly should be retargeted 
        ''' to what assembly. Key is the <see cref="AssemblySymbol"/> referenced by the underlying module,
        ''' value is the corresponding <see cref="AssemblySymbol"/> referenced by this module, and corresponding
        ''' retargeting map for symbols.
        ''' </summary>
        Private ReadOnly _retargetingAssemblyMap As New Dictionary(Of AssemblySymbol, DestinationData)()

        Private Structure DestinationData
            Public [To] As AssemblySymbol
            Public SymbolMap As ConcurrentDictionary(Of NamedTypeSymbol, NamedTypeSymbol)
        End Structure


        Friend ReadOnly RetargetingTranslator As RetargetingSymbolTranslator

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="retargetingAssembly">
        ''' Owning assembly.
        ''' </param>
        ''' <param name="underlyingModule">
        ''' Underlying <see cref="ModuleSymbol"/>, cannot be another <see cref="RetargetingModuleSymbol"/>.
        ''' </param>
        ''' <remarks></remarks>
        Public Sub New(retargetingAssembly As RetargetingAssemblySymbol, underlyingModule As SourceModuleSymbol)
            Debug.Assert(retargetingAssembly IsNot Nothing)
            Debug.Assert(underlyingModule IsNot Nothing)

            _retargetingAssembly = retargetingAssembly
            _underlyingModule = underlyingModule
            RetargetingTranslator = New RetargetingSymbolTranslator(Me)

            Me._createRetargetingMethod = AddressOf CreateRetargetingMethod
            Me._createRetargetingNamespace = AddressOf CreateRetargetingNamespace
            Me._createRetargetingNamedType = AddressOf CreateRetargetingNamedType
            Me._createRetargetingField = AddressOf CreateRetargetingField
            Me._createRetargetingTypeParameter = AddressOf CreateRetargetingTypeParameter
            Me._createRetargetingProperty = AddressOf CreateRetargetingProperty
            Me._createRetargetingEvent = AddressOf CreateRetargetingEvent
        End Sub

        Friend Overrides ReadOnly Property Ordinal As Integer
            Get
                Debug.Assert(_underlyingModule.Ordinal = 0)
                Return 0
            End Get
        End Property

        Friend Overrides ReadOnly Property Machine As System.Reflection.PortableExecutable.Machine
            Get
                Return _underlyingModule.Machine
            End Get
        End Property

        Friend Overrides ReadOnly Property Bit32Required As Boolean
            Get
                Return _underlyingModule.Bit32Required
            End Get
        End Property

        ''' <summary>
        ''' The underlying <see cref="ModuleSymbol"/>, cannot be another <see cref="RetargetingModuleSymbol"/>.
        ''' </summary>
        Public ReadOnly Property UnderlyingModule As SourceModuleSymbol
            Get
                Return _underlyingModule
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _retargetingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return _retargetingAssembly
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(_underlyingModule, _lazyCustomAttributes)
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return _underlyingModule.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return _underlyingModule.MetadataName
            End Get
        End Property

        Public Overrides ReadOnly Property GlobalNamespace As NamespaceSymbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingModule.GlobalNamespace)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _underlyingModule.Locations
            End Get
        End Property

        ''' <summary>
        ''' A helper method for ReferenceManager to set AssemblySymbols for assemblies 
        ''' referenced by this module.
        ''' </summary>
        Friend Overrides Sub SetReferences(
            moduleReferences As ModuleReferences(Of AssemblySymbol),
            Optional originatingSourceAssemblyDebugOnly As SourceAssemblySymbol = Nothing)
            MyBase.SetReferences(moduleReferences, originatingSourceAssemblyDebugOnly)

            ' Build the retargeting map
            _retargetingAssemblyMap.Clear()

            Dim underlyingBoundReferences As ImmutableArray(Of AssemblySymbol) = _underlyingModule.GetReferencedAssemblySymbols()
            Dim referencedAssemblySymbols As ImmutableArray(Of AssemblySymbol) = moduleReferences.Symbols
            Dim referencedAssemblies As ImmutableArray(Of AssemblyIdentity) = moduleReferences.Identities

            Debug.Assert(referencedAssemblySymbols.Length = referencedAssemblies.Length)
            Debug.Assert(referencedAssemblySymbols.Length <= underlyingBoundReferences.Length) ' Linked references are filtered out.

            Dim i As Integer = -1
            Dim j As Integer = -1

            Do
                i += 1
                j += 1

                If i >= referencedAssemblySymbols.Length Then
                    Exit Do
                End If

                ' Skip linked assemblies for source module
                While underlyingBoundReferences(j).IsLinked
                    j += 1
                End While

#If DEBUG Then
                Dim identityComparer = _underlyingModule.DeclaringCompilation.Options.AssemblyIdentityComparer
                Dim definitionIdentity = If(referencedAssemblySymbols(i) Is originatingSourceAssemblyDebugOnly,
                                            New AssemblyIdentity(name:=originatingSourceAssemblyDebugOnly.Name),
                                            referencedAssemblySymbols(i).Identity)

                Debug.Assert(identityComparer.Compare(referencedAssemblies(i), definitionIdentity) <> AssemblyIdentityComparer.ComparisonResult.NotEquivalent)
                Debug.Assert(identityComparer.Compare(referencedAssemblies(i), underlyingBoundReferences(j).Identity) <> AssemblyIdentityComparer.ComparisonResult.NotEquivalent)
#End If

                If referencedAssemblySymbols(i) IsNot underlyingBoundReferences(j) Then
                    Dim destinationData As DestinationData = Nothing

                    If Not _retargetingAssemblyMap.TryGetValue(underlyingBoundReferences(j), destinationData) Then
                        Dim symbolMap = New ConcurrentDictionary(Of NamedTypeSymbol, NamedTypeSymbol)()

                        _retargetingAssemblyMap.Add(underlyingBoundReferences(j),
                            New DestinationData With {.To = referencedAssemblySymbols(i), .SymbolMap = symbolMap})
                    Else
                        Debug.Assert(destinationData.To Is referencedAssemblySymbols(i))
                    End If
                End If
            Loop

#If DEBUG Then
            While (j < underlyingBoundReferences.Length AndAlso underlyingBoundReferences(j).IsLinked)
                j += 1
            End While

            Debug.Assert(j = underlyingBoundReferences.Length)
#End If

        End Sub

        Friend Overrides ReadOnly Property TypeNames As ICollection(Of String)
            Get
                Return _underlyingModule.TypeNames
            End Get
        End Property

        Friend Overrides ReadOnly Property NamespaceNames As ICollection(Of String)
            Get
                Return _underlyingModule.NamespaceNames
            End Get
        End Property

        Friend Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return _underlyingModule.MightContainExtensionMethods
            End Get
        End Property

        Friend Overrides ReadOnly Property HasAssemblyCompilationRelaxationsAttribute As Boolean
            Get
                Return _underlyingModule.HasAssemblyCompilationRelaxationsAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property HasAssemblyRuntimeCompatibilityAttribute As Boolean
            Get
                Return _underlyingModule.HasAssemblyRuntimeCompatibilityAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property DefaultMarshallingCharSet As CharSet?
            Get
                Return _underlyingModule.DefaultMarshallingCharSet
            End Get
        End Property

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return _underlyingModule.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        Public Overrides Function GetMetadata() As ModuleMetadata
            Return _underlyingModule.GetMetadata()
        End Function
    End Class
End Namespace
