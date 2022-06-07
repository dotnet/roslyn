' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend MustInherit Class PEAssemblyBuilderBase
        Inherits PEModuleBuilder
        Implements Cci.IAssemblyReference

        Protected ReadOnly m_SourceAssembly As SourceAssemblySymbol
        Private ReadOnly _additionalTypes As ImmutableArray(Of NamedTypeSymbol)
        Private _lazyFiles As ImmutableArray(Of Cci.IFileReference)

        ''' <summary>This is a cache of a subset of <seealso cref="_lazyFiles"/>. We don't include manifest resources in ref assemblies</summary>
        Private _lazyFilesWithoutManifestResources As ImmutableArray(Of Cci.IFileReference)

        ''' <summary>
        ''' This value will override m_SourceModule.MetadataName.
        ''' </summary>
        ''' <remarks>
        ''' This functionality exists for parity with C#, which requires it for
        ''' legacy reasons (see Microsoft.CodeAnalysis.CSharp.Emit.PEAssemblyBuilderBase.metadataName).
        ''' </remarks>
        Private ReadOnly _metadataName As String

        Public Sub New(sourceAssembly As SourceAssemblySymbol,
                       emitOptions As EmitOptions,
                       outputKind As OutputKind,
                       serializationProperties As Cci.ModulePropertiesForSerialization,
                       manifestResources As IEnumerable(Of ResourceDescription),
                       additionalTypes As ImmutableArray(Of NamedTypeSymbol))

            MyBase.New(DirectCast(sourceAssembly.Modules(0), SourceModuleSymbol),
                       emitOptions,
                       outputKind,
                       serializationProperties,
                       manifestResources)

            Debug.Assert(sourceAssembly IsNot Nothing)
            Debug.Assert(manifestResources IsNot Nothing)

            m_SourceAssembly = sourceAssembly
            _additionalTypes = additionalTypes.NullToEmpty()
            _metadataName = If(emitOptions.OutputNameOverride Is Nothing, sourceAssembly.MetadataName, FileNameUtilities.ChangeExtension(emitOptions.OutputNameOverride, extension:=Nothing))
            m_AssemblyOrModuleSymbolToModuleRefMap.Add(sourceAssembly, Me)
        End Sub

        Public Overrides ReadOnly Property SourceAssemblyOpt As ISourceAssemblySymbolInternal
            Get
                Return m_SourceAssembly
            End Get
        End Property

        Public Overrides Function GetAdditionalTopLevelTypes() As ImmutableArray(Of NamedTypeSymbol)
            Return _additionalTypes
        End Function

        Public Overrides Function GetEmbeddedTypes(diagnostics As DiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public NotOverridable Overrides Function GetFiles(context As EmitContext) As IEnumerable(Of Cci.IFileReference)
            If Not context.IsRefAssembly Then
                Return GetFilesCore(context, _lazyFiles)
            End If

            Return GetFilesCore(context, _lazyFilesWithoutManifestResources)
        End Function

        Private Function GetFilesCore(context As EmitContext, ByRef lazyFiles As ImmutableArray(Of Cci.IFileReference)) As IEnumerable(Of Cci.IFileReference)
            If lazyFiles.IsDefault Then
                Dim builder = ArrayBuilder(Of Cci.IFileReference).GetInstance()
                Try
                    Dim modules = m_SourceAssembly.Modules

                    For i As Integer = 1 To modules.Length - 1
                        builder.Add(DirectCast(Translate(modules(i), context.Diagnostics), Cci.IFileReference))
                    Next

                    If Not context.IsRefAssembly Then
                        ' resources are not emitted into ref assemblies
                        For Each resource In ManifestResources
                            If Not resource.IsEmbedded Then
                                builder.Add(resource)
                            End If
                        Next
                    End If

                    ' Dev12 compilers don't report ERR_CryptoHashFailed if there are no files to be hashed.
                    If ImmutableInterlocked.InterlockedInitialize(lazyFiles, builder.ToImmutable()) AndAlso lazyFiles.Length > 0 Then
                        If Not CryptographicHashProvider.IsSupportedAlgorithm(m_SourceAssembly.HashAlgorithm) Then
                            context.Diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_CryptoHashFailed), NoLocation.Singleton))
                        End If
                    End If
                Finally
                    ' Clean up so we don't get a leak report from the unit tests.
                    builder.Free()
                End Try
            End If

            Return lazyFiles
        End Function

        Private Shared Function Free(builder As ArrayBuilder(Of Cci.IFileReference)) As Boolean
            builder.Free()
            Return False
        End Function

        Protected Overrides Sub AddEmbeddedResourcesFromAddedModules(builder As ArrayBuilder(Of Cci.ManagedResource), diagnostics As DiagnosticBag)
            Dim modules = m_SourceAssembly.Modules

            For i As Integer = 1 To modules.Length - 1
                Dim file = DirectCast(Translate(modules(i), diagnostics), Cci.IFileReference)

                Try
                    For Each resource In DirectCast(modules(i), Symbols.Metadata.PE.PEModuleSymbol).Module.GetEmbeddedResourcesOrThrow()
                        builder.Add(New Cci.ManagedResource(
                            resource.Name,
                            (resource.Attributes And ManifestResourceAttributes.Public) <> 0,
                            Nothing,
                            file,
                            resource.Offset))
                    Next
                Catch mrEx As BadImageFormatException
                    diagnostics.Add(ERRID.ERR_UnsupportedModule1, NoLocation.Singleton, modules(i))
                End Try
            Next
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _metadataName
            End Get
        End Property

        Public ReadOnly Property Identity As AssemblyIdentity Implements Cci.IAssemblyReference.Identity
            Get
                Return m_SourceAssembly.Identity
            End Get
        End Property

        Public ReadOnly Property AssemblyVersionPattern As Version Implements Cci.IAssemblyReference.AssemblyVersionPattern
            Get
                Return m_SourceAssembly.AssemblyVersionPattern
            End Get
        End Property
    End Class

    Friend NotInheritable Class PEAssemblyBuilder
        Inherits PEAssemblyBuilderBase

        Public Sub New(sourceAssembly As SourceAssemblySymbol,
                       emitOptions As EmitOptions,
                       outputKind As OutputKind,
                       serializationProperties As Cci.ModulePropertiesForSerialization,
                       manifestResources As IEnumerable(Of ResourceDescription),
                       Optional additionalTypes As ImmutableArray(Of NamedTypeSymbol) = Nothing)

            MyBase.New(sourceAssembly, emitOptions, outputKind, serializationProperties, manifestResources, additionalTypes)
        End Sub

        Friend Overrides ReadOnly Property AllowOmissionOfConditionalCalls As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property EncSymbolChanges As SymbolChanges
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property PreviousGeneration As EmitBaseline
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace
