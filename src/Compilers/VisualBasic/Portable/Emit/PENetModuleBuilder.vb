' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
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

        Public Overrides ReadOnly Property CurrentGenerationOrdinal As Integer
            Get
                Return 0
            End Get
        End Property

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
