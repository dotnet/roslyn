' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Linq.Enumerable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' Represents an assembly imported from a PE.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class PEAssemblySymbol
        Inherits MetadataOrSourceAssemblySymbol

        ''' <summary>
        ''' An Assembly object providing metadata for the assembly.
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly m_Assembly As PEAssembly

        ''' <summary>
        ''' A MetadataDocumentationProvider providing XML documentation for this assembly.
        ''' </summary>
        Private ReadOnly m_DocumentationProvider As DocumentationProvider

        ''' <summary>
        ''' The list of contained PEModuleSymbol objects.
        ''' The list doesn't use type ReadOnlyCollection(Of PEModuleSymbol) so that we
        ''' can return it from Modules property as is.
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly m_Modules As ImmutableArray(Of ModuleSymbol)

        ''' <summary>
        ''' An array of assemblies involved in canonical type resolution of
        ''' NoPia local types defined within this assembly. In other words, all 
        ''' references used by a compilation referencing this assembly.
        ''' The array and its content is provided by ReferenceManager and must not be modified.
        ''' </summary>
        Private m_NoPiaResolutionAssemblies As ImmutableArray(Of AssemblySymbol)

        ''' <summary>
        ''' An array of assemblies referenced by this assembly, which are linked (/l-ed) by 
        ''' each compilation that is using this AssemblySymbol as a reference. 
        ''' If this AssemblySymbol is linked too, it will be in this array too.
        ''' The array and its content is provided by ReferenceManager and must not be modified.
        ''' </summary>
        Private m_LinkedReferencedAssemblies As ImmutableArray(Of AssemblySymbol)

        ''' <summary>
        ''' Assembly is /l-ed by compilation that is using it as a reference.
        ''' </summary>
        Private ReadOnly m_IsLinked As Boolean

        Private m_LazyMightContainExtensionMethods As Byte = ThreeState.Unknown

        Private m_lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Friend Sub New(assembly As PEAssembly, documentationProvider As DocumentationProvider,
                       isLinked As Boolean, importOptions As MetadataImportOptions)
            Debug.Assert(assembly IsNot Nothing)
            m_Assembly = assembly

            m_DocumentationProvider = documentationProvider

            Dim modules(assembly.Modules.Length - 1) As ModuleSymbol

            For i As Integer = 0 To assembly.Modules.Length - 1 Step 1
                modules(i) = New PEModuleSymbol(Me, assembly.Modules(i), importOptions, i)
            Next

            m_Modules = modules.AsImmutableOrNull()
            m_IsLinked = isLinked
        End Sub

        Friend ReadOnly Property Assembly As PEAssembly
            Get
                Return m_Assembly
            End Get
        End Property

        Public Overrides ReadOnly Property Identity As AssemblyIdentity
            Get
                Return m_Assembly.Identity
            End Get
        End Property

        Friend Overrides ReadOnly Property PublicKey As ImmutableArray(Of Byte)
            Get
                Return m_Assembly.Identity.PublicKey
            End Get
        End Property

        Friend Overrides Function GetGuidString(ByRef guidString As String) As Boolean
            Return Assembly.Modules(0).HasGuidAttribute(Assembly.Handle, guidString)
        End Function

        Friend Overrides Function AreInternalsVisibleToThisAssembly(potentialGiverOfAccess As AssemblySymbol) As Boolean
            Return MakeFinalIVTDetermination(potentialGiverOfAccess) = IVTConclusion.Match
        End Function

        Friend Overrides Function GetInternalsVisibleToPublicKeys(simpleName As String) As IEnumerable(Of ImmutableArray(Of Byte))
            Return Assembly.GetInternalsVisibleToPublicKeys(simpleName)
        End Function

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If m_lazyCustomAttributes.IsDefault Then
                PrimaryModule.LoadCustomAttributes(Me.Assembly.Handle, m_lazyCustomAttributes)
            End If
            Return m_lazyCustomAttributes
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return StaticCast(Of Location).From(PrimaryModule.MetadataLocation)
            End Get
        End Property

        Public Overrides ReadOnly Property Modules As ImmutableArray(Of ModuleSymbol)
            Get
                Return m_Modules
            End Get
        End Property

        Friend ReadOnly Property PrimaryModule As PEModuleSymbol
            Get
                Return DirectCast(Me.Modules(0), PEModuleSymbol)
            End Get
        End Property

        ''' <summary>
        ''' Look up the assembly to which the given metadata type Is forwarded.
        ''' </summary>
        ''' <param name="emittedName"></param>
        ''' <param name="ignoreCase">Pass true to look up fullName case-insensitively.  WARNING: more expensive.</param>
        ''' <param name="matchedName">Returns the actual casing of the matching name.</param>
        ''' <returns>
        ''' The assembly to which the given type Is forwarded Or null, if there isn't one.
        ''' </returns>
        ''' <remarks>
        ''' The returned assembly may also forward the type.
        ''' </remarks>
        Friend Function LookupAssemblyForForwardedMetadataType(ByRef emittedName As MetadataTypeName, ignoreCase As Boolean, <Out> ByRef matchedName As String) As AssemblySymbol
            ' Look in the type forwarders of the primary module of this assembly, clr does not honor type forwarder
            ' in non-primary modules.

            ' Examine the type forwarders, but only from the primary module.
            Return PrimaryModule.GetAssemblyForForwardedType(emittedName, ignoreCase, matchedName)
        End Function

        Friend Overrides Function TryLookupForwardedMetadataTypeWithCycleDetection(ByRef emittedName As MetadataTypeName, visitedAssemblies As ConsList(Of AssemblySymbol), ignoreCase As Boolean) As NamedTypeSymbol
            ' Check if it is a forwarded type.
            Dim matchedName As String = Nothing
            Dim forwardedToAssembly = LookupAssemblyForForwardedMetadataType(emittedName, ignoreCase, matchedName)
            ' Don't bother to check the forwarded-to assembly if we've already seen it.
            If forwardedToAssembly IsNot Nothing Then
                If visitedAssemblies IsNot Nothing AndAlso visitedAssemblies.Contains(forwardedToAssembly) Then
                    Return CreateCycleInTypeForwarderErrorTypeSymbol(emittedName)
                Else
                    visitedAssemblies = New ConsList(Of AssemblySymbol)(Me, If(visitedAssemblies, ConsList(Of AssemblySymbol).Empty))

                    If ignoreCase AndAlso Not String.Equals(emittedName.FullName, matchedName, StringComparison.Ordinal) Then
                        emittedName = MetadataTypeName.FromFullName(matchedName, emittedName.UseCLSCompliantNameArityEncoding, emittedName.ForcedArity)
                    End If

                    Return forwardedToAssembly.LookupTopLevelMetadataTypeWithCycleDetection(emittedName, visitedAssemblies, digThroughForwardedTypes:=True)
                End If
            End If

            Return Nothing
        End Function

        Friend Overrides Function GetNoPiaResolutionAssemblies() As ImmutableArray(Of AssemblySymbol)
            Return m_NoPiaResolutionAssemblies
        End Function

        Friend Overrides Sub SetNoPiaResolutionAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))
            m_NoPiaResolutionAssemblies = assemblies
        End Sub

        Friend Overrides Sub SetLinkedReferencedAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))
            m_LinkedReferencedAssemblies = assemblies
        End Sub

        Friend Overrides Function GetLinkedReferencedAssemblies() As ImmutableArray(Of AssemblySymbol)
            Return m_LinkedReferencedAssemblies
        End Function

        Friend Overrides ReadOnly Property IsLinked As Boolean
            Get
                Return m_IsLinked
            End Get
        End Property

        Friend ReadOnly Property DocumentationProvider As DocumentationProvider
            Get
                Return m_DocumentationProvider
            End Get
        End Property

        Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                If m_LazyMightContainExtensionMethods = ThreeState.Unknown Then
                    Dim primaryModuleSymbol = PrimaryModule
                    If primaryModuleSymbol.Module.HasExtensionAttribute(m_Assembly.Handle, ignoreCase:=True) Then
                        m_LazyMightContainExtensionMethods = ThreeState.True
                    Else
                        m_LazyMightContainExtensionMethods = ThreeState.False
                    End If
                End If

                Return m_LazyMightContainExtensionMethods = ThreeState.True
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
    End Class
End Namespace

