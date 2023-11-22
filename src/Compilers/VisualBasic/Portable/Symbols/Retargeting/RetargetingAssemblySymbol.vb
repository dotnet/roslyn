' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Globalization
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting

    ''' <summary>
    ''' Essentially this is a wrapper around another AssemblySymbol that is responsible for retargeting
    ''' symbols from one assembly to another. It can retarget symbols for multiple assemblies at the same time. 
    ''' 
    ''' For example, compilation C1 references v1 of Lib.dll and compilation C2 references C1 and v2 of Lib.dll. 
    ''' In this case, in context of C2, all types from v1 of Lib.dll leaking through C1 (through method 
    ''' signatures, etc.) must be retargeted to the types from v2 of Lib.dll. This is what 
    ''' RetargetingAssemblySymbol is responsible for. In the example above, modules in C2 do not 
    ''' reference C1.m_AssemblySymbol, but reference a special RetargetingAssemblySymbol created for 
    ''' C1 by ReferenceManager.
    ''' 
    ''' Here is how retargeting is implemented in general:
    ''' - Symbols from underlying assembly are substituted with retargeting symbols.
    ''' - Symbols from referenced assemblies that can be reused as is (i.e. doesn't have to be retargeted) are
    '''   used as is.
    ''' - Symbols from referenced assemblies that must be retargeted are substituted with result of retargeting.
    ''' </summary>
    Friend NotInheritable Class RetargetingAssemblySymbol
        Inherits NonMissingAssemblySymbol

        ''' <summary>
        ''' The underlying AssemblySymbol, it leaks symbols that should be retargeted.
        ''' This cannot be an instance of RetargetingAssemblySymbol.
        ''' </summary>
        Private ReadOnly _underlyingAssembly As SourceAssemblySymbol

        ''' <summary>
        ''' The list of contained ModuleSymbol objects. First item in the list
        ''' is RetargetingModuleSymbol that wraps corresponding SourceModuleSymbol 
        ''' from m_UnderlyingAssembly.Modules list, the rest are PEModuleSymbols for 
        ''' added modules.
        ''' </summary>
        Private ReadOnly _modules As ImmutableArray(Of ModuleSymbol)

        ''' <summary>
        ''' An array of assemblies involved in canonical type resolution of
        ''' NoPia local types defined within this assembly. In other words, all 
        ''' references used by a compilation referencing this assembly.
        ''' The array and its content is provided by ReferenceManager and must not be modified.
        ''' </summary>
        Private _noPiaResolutionAssemblies As ImmutableArray(Of AssemblySymbol)

        ''' <summary>
        ''' An array of assemblies referenced by this assembly, which are linked (/l-ed) by 
        ''' each compilation that is using this AssemblySymbol as a reference. 
        ''' If this AssemblySymbol is linked too, it will be in this array too.
        ''' The array and its content is provided by ReferenceManager and must not be modified.
        ''' </summary>
        Private _linkedReferencedAssemblies As ImmutableArray(Of AssemblySymbol)

        ''' <summary>
        ''' A map from a local NoPia type to corresponding canonical type.
        ''' </summary>
        Friend ReadOnly m_NoPiaUnificationMap As New ConcurrentDictionary(Of NamedTypeSymbol, NamedTypeSymbol)()

        ''' <summary>
        ''' Assembly is /l-ed by compilation that is using it as a reference.
        ''' </summary>
        Private ReadOnly _isLinked As Boolean

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="underlyingAssembly">
        ''' The underlying AssemblySymbol, cannot be an instance of RetargetingAssemblySymbol.
        ''' </param>
        ''' <param name="isLinked">
        ''' Assembly is /l-ed by compilation that is using it as a reference.
        ''' </param>
        Public Sub New(underlyingAssembly As SourceAssemblySymbol, isLinked As Boolean)
            Debug.Assert(underlyingAssembly IsNot Nothing)

            _underlyingAssembly = underlyingAssembly

            Dim modules(underlyingAssembly.Modules.Length - 1) As ModuleSymbol

            modules(0) = New RetargetingModuleSymbol(Me, DirectCast(underlyingAssembly.Modules(0), SourceModuleSymbol))

            For i As Integer = 1 To underlyingAssembly.Modules.Length - 1 Step 1
                Dim peModuleSym = DirectCast(underlyingAssembly.Modules(i), PEModuleSymbol)
                modules(i) = New PEModuleSymbol(Me, peModuleSym.Module, peModuleSym.ImportOptions, i)
            Next

            _modules = modules.AsImmutableOrNull()
            _isLinked = isLinked
        End Sub

        ''' <summary>
        ''' The underlying AssemblySymbol.
        ''' This cannot be an instance of RetargetingAssemblySymbol.
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public ReadOnly Property UnderlyingAssembly As SourceAssemblySymbol
            Get
                Return _underlyingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property Identity As AssemblyIdentity
            Get
                Return _underlyingAssembly.Identity
            End Get
        End Property

        Public Overrides ReadOnly Property AssemblyVersionPattern As Version
            Get
                Return _underlyingAssembly.AssemblyVersionPattern
            End Get
        End Property

        Friend Overrides ReadOnly Property PublicKey As ImmutableArray(Of Byte)
            Get
                Return _underlyingAssembly.PublicKey
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return _underlyingAssembly.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(_underlyingAssembly, _lazyCustomAttributes)
        End Function

        Friend ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return DirectCast(Modules(0), RetargetingModuleSymbol).RetargetingTranslator
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _underlyingAssembly.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property Modules As ImmutableArray(Of ModuleSymbol)
            Get
                Return _modules
            End Get
        End Property

        Friend Overrides ReadOnly Property KeepLookingForDeclaredSpecialTypes As Boolean
            Get
                ' RetargetingAssemblySymbol never represents Core library. 
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property HasImportedFromTypeLibAttribute As Boolean
            Get
                Return _underlyingAssembly.HasImportedFromTypeLibAttribute
            End Get
        End Property

        Public Overrides ReadOnly Property HasPrimaryInteropAssemblyAttribute As Boolean
            Get
                Return _underlyingAssembly.HasPrimaryInteropAssemblyAttribute
            End Get
        End Property

        ''' <summary>
        ''' Lookup declaration for FX type in this Assembly.
        ''' </summary>
        ''' <param name="type"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Overrides Function GetDeclaredSpecialType(type As SpecialType) As NamedTypeSymbol
            ' Cor library should not have any references and, therefore, should never be
            ' wrapped by a RetargetingAssemblySymbol.
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function GetNoPiaResolutionAssemblies() As ImmutableArray(Of AssemblySymbol)
            Return _noPiaResolutionAssemblies
        End Function

        Friend Overrides Sub SetNoPiaResolutionAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))
            _noPiaResolutionAssemblies = assemblies
        End Sub

        Friend Overrides Sub SetLinkedReferencedAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))
            _linkedReferencedAssemblies = assemblies
        End Sub

        Friend Overrides Function GetLinkedReferencedAssemblies() As ImmutableArray(Of AssemblySymbol)
            Return _linkedReferencedAssemblies
        End Function

        Friend Overrides ReadOnly Property IsLinked As Boolean
            Get
                Return _isLinked
            End Get
        End Property

        Public Overrides ReadOnly Property TypeNames As ICollection(Of String)
            Get
                Return _underlyingAssembly.TypeNames
            End Get
        End Property

        Public Overrides ReadOnly Property NamespaceNames As ICollection(Of String)
            Get
                Return _underlyingAssembly.NamespaceNames
            End Get
        End Property

        Friend Overrides Function GetInternalsVisibleToPublicKeys(simpleName As String) As IEnumerable(Of ImmutableArray(Of Byte))
            Return _underlyingAssembly.GetInternalsVisibleToPublicKeys(simpleName)
        End Function

        Friend Overrides Function GetInternalsVisibleToAssemblyNames() As IEnumerable(Of String)
            Return _underlyingAssembly.GetInternalsVisibleToAssemblyNames()
        End Function

        Friend Overrides Function AreInternalsVisibleToThisAssembly(potentialGiverOfAccess As AssemblySymbol) As Boolean
            Return _underlyingAssembly.AreInternalsVisibleToThisAssembly(potentialGiverOfAccess)
        End Function

        Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return _underlyingAssembly.MightContainExtensionMethods
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

        Friend Overrides Function GetGuidString(ByRef guidString As String) As Boolean
            Return _underlyingAssembly.GetGuidString(guidString)
        End Function

        Friend Overrides Function TryLookupForwardedMetadataTypeWithCycleDetection(ByRef emittedName As MetadataTypeName, visitedAssemblies As ConsList(Of AssemblySymbol), ignoreCase As Boolean) As NamedTypeSymbol
            Dim underlying As NamedTypeSymbol = UnderlyingAssembly.TryLookupForwardedMetadataType(emittedName, ignoreCase)

            If underlying Is Nothing Then
                Return Nothing
            End If

            Return Me.RetargetingTranslator.Retarget(underlying, RetargetOptions.RetargetPrimitiveTypesByName)
        End Function

        Friend Overrides Iterator Function GetAllTopLevelForwardedTypes() As IEnumerable(Of NamedTypeSymbol)
            For Each underlying As NamedTypeSymbol In UnderlyingAssembly.GetAllTopLevelForwardedTypes()
                Yield Me.RetargetingTranslator.Retarget(underlying, RetargetOptions.RetargetPrimitiveTypesByName)
            Next
        End Function

        Public Overrides Function GetMetadata() As AssemblyMetadata
            Return _underlyingAssembly.GetMetadata()
        End Function

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return _underlyingAssembly.ObsoleteAttributeData
            End Get
        End Property

    End Class
End Namespace
