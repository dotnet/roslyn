' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class PENetModuleBuilder
        Inherits PEModuleBuilder

        Friend Sub New(
               sourceModule As SourceModuleSymbol,
               emitOptions As EmitOptions,
               serializationProperties As Cci.ModulePropertiesForSerialization,
               manifestResources As IEnumerable(Of ResourceDescription))

            MyBase.New(sourceModule, emitOptions, OutputKind.NetModule, serializationProperties, manifestResources)
        End Sub

        Protected Overrides Sub AddEmbeddedResourcesFromAddedModules(builder As ArrayBuilder(Of Cci.ManagedResource), diagnostics As DiagnosticBag)
            Throw ExceptionUtilities.Unreachable
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

        Public Overrides ReadOnly Property FieldRvaSupported As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides Function TryGetOrCreateSynthesizedHotReloadExceptionType() As INamedTypeSymbolInternal
            Return Nothing
        End Function

        Public Overrides Function GetOrCreateHotReloadExceptionConstructorDefinition() As IMethodSymbolInternal
            ' Should only be called when compiling EnC delta. EnC does not support emitting netmodules.
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides Function GetUsedSynthesizedHotReloadExceptionType() As INamedTypeSymbolInternal
            Return Nothing
        End Function

        Public Overrides Function GetFiles(context As EmitContext) As IEnumerable(Of Cci.IFileReference)
            Return SpecializedCollections.EmptyEnumerable(Of Cci.IFileReference)()
        End Function

        Public Overrides ReadOnly Property SourceAssemblyOpt As ISourceAssemblySymbolInternal
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace
