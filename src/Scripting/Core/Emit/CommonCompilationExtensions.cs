// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;
using Microsoft.CodeAnalysis.Emit;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Scripting.Emit
{
    internal static class CommonCompilationExtensions
    {
        /// <summary>
        /// Emits the compilation into given <see cref="ModuleBuilder"/> using Reflection.Emit APIs.
        /// </summary>
        /// <param name="compilation">Compilation.</param>
        /// <param name="cancellationToken">Can be used to cancel the emit process.</param>
        /// <param name="compiledAssemblyImage">Assembly image, returned only if we fallback to CCI writer.</param>
        /// <param name="entryPointTypeName">An entry point or null on failure.</param>
        /// <param name="entryPointMethodName">An entry point or null on failure.</param>
        /// <param name="diagnostics">Diagnostics.</param>
        /// <returns>True on success, false if a compilation error occurred or the compilation doesn't contain any code or declarations.</returns>
        /// <exception cref="InvalidOperationException">Referenced assembly can't be resolved.</exception>
        internal static bool Emit(
            this Compilation compilation,
            DiagnosticBag diagnostics,
            out string entryPointTypeName,
            out string entryPointMethodName,
            out byte[] compiledAssemblyImage,
            CancellationToken cancellationToken)
        {
            compiledAssemblyImage = null;

            var moduleBeingBuilt = compilation.CreateModuleBuilder(
                emitOptions: EmitOptions.Default,
                manifestResources: null,
                testData: null,
                diagnostics: diagnostics,
                cancellationToken: cancellationToken);

            if (moduleBeingBuilt == null)
            {
                entryPointTypeName = null;
                entryPointMethodName = null;
                return false;
            }

            if (!compilation.Compile(
                moduleBeingBuilt,
                win32Resources: null,
                xmlDocStream: null,
                emittingPdb: false,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: cancellationToken))
            {
                entryPointTypeName = null;
                entryPointMethodName = null;
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            DiagnosticBag metadataDiagnostics = DiagnosticBag.GetInstance();

            var context = new EmitContext((Cci.IModule)moduleBeingBuilt, null, metadataDiagnostics);

            using (var stream = new System.IO.MemoryStream())
            {
                Cci.PeWriter.WritePeToStream(
                    context,
                    compilation.MessageProvider,
                    () => stream,
                    nativePdbWriterOpt: null,
                    pdbPathOpt: null,
                    allowMissingMethodBodies: false,
                    deterministic: false,
                    cancellationToken: cancellationToken);

                compiledAssemblyImage = stream.ToArray();
            }

            var containingType = (Cci.INamespaceTypeReference)moduleBeingBuilt.EntryPoint.GetContainingType(context);
            entryPointTypeName = MetadataHelpers.BuildQualifiedName(containingType.NamespaceName, Cci.MetadataWriter.GetMangledName(containingType));
            entryPointMethodName = moduleBeingBuilt.EntryPoint.Name;

            // translate metadata errors.
            return compilation.FilterAndAppendAndFreeDiagnostics(diagnostics, ref metadataDiagnostics);
        }
    }
}
