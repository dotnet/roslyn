' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend NotInheritable Class PENetModuleBuilder
        Inherits PEModuleBuilder

        Friend Sub New(
               sourceModule As SourceModuleSymbol,
               emitOptions As EmitOptions,
               serializationProperties As ModulePropertiesForSerialization,
               manifestResources As IEnumerable(Of ResourceDescription))

            MyBase.New(sourceModule, emitOptions, OutputKind.NetModule, serializationProperties, manifestResources, assemblySymbolMapper:=Nothing)
        End Sub

        Protected Overrides Sub AddEmbeddedResourcesFromAddedModules(builder As ArrayBuilder(Of Cci.ManagedResource), diagnostics As DiagnosticBag)
            Throw ExceptionUtilities.Unreachable
        End Sub
    End Class

End Namespace
