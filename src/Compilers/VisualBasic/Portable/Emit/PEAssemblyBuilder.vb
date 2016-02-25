' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend MustInherit Class PEAssemblyBuilderBase
        Inherits PEModuleBuilder
        Implements Cci.IAssembly

        Protected ReadOnly m_SourceAssembly As SourceAssemblySymbol
        Private ReadOnly _additionalTypes As ImmutableArray(Of NamedTypeSymbol)
        Private _lazyFiles As ImmutableArray(Of Cci.IFileReference)

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

            Me.m_SourceAssembly = sourceAssembly
            Me._additionalTypes = additionalTypes.NullToEmpty()
            Me._metadataName = If(emitOptions.OutputNameOverride Is Nothing, sourceAssembly.MetadataName, FileNameUtilities.ChangeExtension(emitOptions.OutputNameOverride, extension:=Nothing))
            m_AssemblyOrModuleSymbolToModuleRefMap.Add(sourceAssembly, Me)
        End Sub

        Public Overrides Sub Dispatch(visitor As Cci.MetadataVisitor)
            visitor.Visit(DirectCast(Me, Cci.IAssembly))
        End Sub

        Friend Overrides Function GetAdditionalTopLevelTypes() As ImmutableArray(Of NamedTypeSymbol)
            Return Me._additionalTypes
        End Function

        Private Function IAssemblyGetFiles(context As EmitContext) As IEnumerable(Of Cci.IFileReference) Implements Cci.IAssembly.GetFiles
            If _lazyFiles.IsDefault Then
                Dim builder = ArrayBuilder(Of Cci.IFileReference).GetInstance()
                Try
                    Dim modules = m_SourceAssembly.Modules

                    For i As Integer = 1 To modules.Length - 1
                        builder.Add(DirectCast(Translate(modules(i), context.Diagnostics), Cci.IFileReference))
                    Next

                    For Each resource In ManifestResources
                        If Not resource.IsEmbedded Then
                            builder.Add(resource)
                        End If
                    Next

                    ' Dev12 compilers don't report ERR_CryptoHashFailed if there are no files to be hashed.
                    If ImmutableInterlocked.InterlockedInitialize(_lazyFiles, builder.ToImmutable()) AndAlso _lazyFiles.Length > 0 Then
                        If Not CryptographicHashProvider.IsSupportedAlgorithm(m_SourceAssembly.AssemblyHashAlgorithm) Then
                            context.Diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_CryptoHashFailed), NoLocation.Singleton))
                        End If
                    End If
                Finally
                    ' Clean up so we don't get a leak report from the unit tests.
                    builder.Free()
                End Try
            End If

            Return _lazyFiles
        End Function

        Private Shared Function Free(builder As ArrayBuilder(Of Cci.IFileReference)) As Boolean
            builder.Free()
            Return False
        End Function

        Private ReadOnly Property IAssemblyFlags As UInteger Implements Cci.IAssembly.Flags
            Get
                Dim result As System.Reflection.AssemblyNameFlags = m_SourceAssembly.Flags And Not System.Reflection.AssemblyNameFlags.PublicKey

                If Not m_SourceAssembly.PublicKey.IsDefaultOrEmpty Then
                    result = result Or System.Reflection.AssemblyNameFlags.PublicKey
                End If

                Return CUInt(result)
            End Get
        End Property

        Private ReadOnly Property IAssemblySignatureKey As String Implements Cci.IAssembly.SignatureKey
            Get
                Return m_SourceAssembly.AssemblySignatureKeyAttributeSetting
            End Get
        End Property

        Private ReadOnly Property IAssemblyPublicKey As ImmutableArray(Of Byte) Implements Cci.IAssembly.PublicKey
            Get
                Return m_SourceAssembly.Identity.PublicKey
            End Get
        End Property

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

        Private ReadOnly Property Identity As AssemblyIdentity Implements Cci.IAssemblyReference.Identity
            Get
                Return m_SourceAssembly.Identity
            End Get
        End Property

        Private ReadOnly Property AssemblyVersionPattern As Version Implements Cci.IAssemblyReference.AssemblyVersionPattern
            Get
                Return m_SourceAssembly.AssemblyVersionPattern
            End Get
        End Property

        Friend Overrides ReadOnly Property Name As String
            Get
                Return _metadataName
            End Get
        End Property

        Private ReadOnly Property IAssemblyHashAlgorithm As AssemblyHashAlgorithm Implements Cci.IAssembly.HashAlgorithm
            Get
                Return m_SourceAssembly.AssemblyHashAlgorithm
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

        Public Overrides ReadOnly Property CurrentGenerationOrdinal As Integer
            Get
                Return 0
            End Get
        End Property
    End Class
End Namespace
