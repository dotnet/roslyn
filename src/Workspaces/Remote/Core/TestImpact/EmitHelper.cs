// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.TestImpact.BuildManagement
{
    internal static class EmitHelper
    {
        private static readonly Dictionary<string, MetadataReference> _runtimeReferenceCache = new Dictionary<string, MetadataReference>(capacity: 2);

        public static async Task<EmitResult> EmitAsync(
            this Project project,
            string outputFilePath,
            string win32ResourcePath,
            ImmutableArray<ResourceDescription> manifestResources,
            EmitOptions options,
            string runtimeReferencePath,
            CancellationToken cancellationToken)
        {
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            if (runtimeReferencePath != null)
            {
                MetadataReference runtimeReference;
                if (!_runtimeReferenceCache.TryGetValue(runtimeReferencePath, out runtimeReference))
                {
                    runtimeReference = MetadataReference.CreateFromFile(runtimeReferencePath);
                    _runtimeReferenceCache[runtimeReferencePath] = runtimeReference;
                }

                compilation = compilation.AddReferences(runtimeReference);
            }

            Stream peStream = null, pdbStream = null, win32Resources = null;
            try
            {
                peStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
                pdbStream = new FileStream(Path.ChangeExtension(outputFilePath, ".pdb"), FileMode.Create, FileAccess.Write);
                win32Resources = !string.IsNullOrWhiteSpace(win32ResourcePath) ? new FileStream(win32ResourcePath, FileMode.Open, FileAccess.Read) : null;

                var result = compilation.Emit(
                    peStream,
                    pdbStream,
                    xmlDocumentationStream: null, // Do not generate XML documentation.
                    win32Resources: win32Resources,
                    manifestResources: GetResourceDescriptions(manifestResources),
                    options: GetEmitOptions(options),
                    cancellationToken: cancellationToken);

                ArrayBuilder<Diagnostic> builder = null;
                foreach (var diagnostic in result.Diagnostics)
                {
                    if (diagnostic.Severity != DiagnosticSeverity.Hidden)
                    {
                        if (builder == null)
                        {
                            builder = ArrayBuilder<Diagnostic>.GetInstance();
                        }

                        builder.Add(new Diagnostic() { Severity = diagnostic.Severity, Message = diagnostic.ToString() });
                    }
                }

                return new EmitResult() { Success = result.Success, Diagnostics = builder != null ? builder.ToImmutableAndFree() : ImmutableArray<Diagnostic>.Empty };
            }
            finally
            {
                peStream?.Dispose();
                pdbStream?.Dispose();
                win32Resources?.Dispose();
            }
        }

        private static Microsoft.CodeAnalysis.Emit.EmitOptions GetEmitOptions(EmitOptions options)
        {
            SubsystemVersion subsystemVersion;
            SubsystemVersion.TryParse(options.SubsystemVersion, out subsystemVersion);
            return new Microsoft.CodeAnalysis.Emit.EmitOptions(
                        metadataOnly: false,
                        debugInformationFormat: DebugInformationFormat.Pdb,
                        pdbFilePath: null,
                        outputNameOverride: null,
                        fileAlignment: options.FileAlignment,
                        baseAddress: options.BaseAddress,
                        highEntropyVirtualAddressSpace: options.HighEntropyVirtualAddressSpace,
                        subsystemVersion: subsystemVersion,
                        runtimeMetadataVersion: null,
                        tolerateErrors: false,
                        includePrivateMembers: true,
                        instrumentationKinds: ImmutableArray.Create(options.InstrumentationKinds));
        }

        private static IEnumerable<Microsoft.CodeAnalysis.ResourceDescription> GetResourceDescriptions(ImmutableArray<ResourceDescription> resources)
        {
            if (resources.IsDefaultOrEmpty)
            {
                return null;
            }

            var builder = ArrayBuilder<Microsoft.CodeAnalysis.ResourceDescription>.GetInstance();
            foreach (var item in resources)
            {
                Func<Stream> dataProvider = () => File.Open(item.File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                builder.Add(item.IsLinkResource ?
                    new Microsoft.CodeAnalysis.ResourceDescription(item.Name, item.File, dataProvider, item.IsPublic) :
                    new Microsoft.CodeAnalysis.ResourceDescription(item.Name, dataProvider, item.IsPublic));
            }

            return builder?.ToImmutableAndFree();
        }
    }
}