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

namespace Microsoft.CodeAnalysis.Scripting.Emit
{
    internal static class CommonCompilationExtensions
    {
        /// <summary>
        /// Emits the compilation into given <see cref="ModuleBuilder"/> using Reflection.Emit APIs.
        /// </summary>
        /// <param name="compilation">Compilation.</param>
        /// <param name="moduleBuilder">
        /// The module builder to add the types into. Can be reused for multiple compilation units.
        /// </param>
        /// <param name="assemblyLoader">
        /// Loads an assembly given an <see cref="AssemblyIdentity"/>. 
        /// This callback is used for loading assemblies referenced by the compilation.
        /// <see cref="System.Reflection.Assembly.Load(AssemblyName)"/> is used if not specified.
        /// </param>
        /// <param name="assemblySymbolMapper">
        /// Applied when converting assembly symbols to assembly references.
        /// <see cref="IAssemblySymbol"/> is mapped to its <see cref="IAssemblySymbol.Identity"/> by default.
        /// </param>
        /// <param name="cancellationToken">Can be used to cancel the emit process.</param>
        /// <param name="recoverOnError">If false the method returns an unsuccessful result instead of falling back to CCI writer.</param>
        /// <param name="compiledAssemblyImage">Assembly image, returned only if we fallback to CCI writer.</param>
        /// <param name="entryPoint">An entry point or null if not applicable or on failure.</param>
        /// <param name="diagnostics">Diagnostics.</param>
        /// <returns>True on success, false if a compilation error occurred or the compilation doesn't contain any code or declarations.</returns>
        /// <remarks>
        /// Reflection.Emit doesn't support all metadata constructs. If an unsupported construct is
        /// encountered a metadata writer that procudes uncollectible code is used instead. This is
        /// indicated by 
        /// <see cref="ReflectionEmitResult.IsUncollectible"/> flag on the result. 
        /// 
        /// Reusing <see cref="System.Reflection.Emit.ModuleBuilder"/> may be beneficial in certain
        /// scenarios. For example, when emitting a sequence of code snippets one at a time (like in
        /// REPL). All the snippets can be compiled into a single module as long as the types being
        /// emitted have unique names. Reusing a single module/assembly reduces memory overhead. On
        /// the other hand, collectible assemblies are units of collection. Defining too many
        /// unrelated types in a single assemly might prevent the unused types to be collected. 
        /// 
        /// No need to provide a name override when using Reflection.Emit, since the assembly already
        /// exists.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Referenced assembly can't be resolved.</exception>
        internal static bool Emit(
            this Compilation compilation,
            ModuleBuilder moduleBuilder,
            AssemblyLoader assemblyLoader,
            Func<IAssemblySymbol, AssemblyIdentity> assemblySymbolMapper,
            bool recoverOnError,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken,
            out MethodInfo entryPoint,
            out byte[] compiledAssemblyImage)
        {
            compiledAssemblyImage = default(byte[]);

            var moduleBeingBuilt = compilation.CreateModuleBuilder(
                emitOptions: EmitOptions.Default,
                manifestResources: null,
                assemblySymbolMapper: assemblySymbolMapper,
                testData: null,
                diagnostics: diagnostics,
                cancellationToken: cancellationToken);

            if (moduleBeingBuilt == null)
            {
                entryPoint = null;
                return false;
            }

            if (!compilation.Compile(
                moduleBeingBuilt,
                win32Resources: null,
                xmlDocStream: null,
                generateDebugInfo: false,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: cancellationToken))
            {
                entryPoint = null;
                return false;
            }

            Cci.IMethodReference cciEntryPoint = moduleBeingBuilt.EntryPoint;

            cancellationToken.ThrowIfCancellationRequested();

            DiagnosticBag metadataDiagnostics = DiagnosticBag.GetInstance();

            var context = new EmitContext((Cci.IModule)moduleBeingBuilt, null, metadataDiagnostics);

            // try emit via Reflection.Emit
            try
            {
                var referencedAssemblies = from referencedAssembly in compilation.GetBoundReferenceManager().GetReferencedAssemblies()
                                           let peReference = referencedAssembly.Key as PortableExecutableReference
                                           select KeyValuePair.Create(
                                               moduleBeingBuilt.Translate(referencedAssembly.Value, metadataDiagnostics),
                                               (peReference != null) ? peReference.FilePath : null);

                entryPoint = ReflectionEmitter.Emit(
                    context,
                    referencedAssemblies,
                    moduleBuilder,
                    assemblyLoader ?? AssemblyLoader.Default,
                    cciEntryPoint,
                    cancellationToken);

                // translate metadata errors.
                return compilation.FilterAndAppendAndFreeDiagnostics(diagnostics, ref metadataDiagnostics);
            }
            catch (TypeLoadException)
            {
                // attempted to emit reference to a type that can't be loaded (has invalid metadata)
            }
            catch (NotSupportedException)
            {
                // nop
            }

            // TODO (tomat):
            //
            // Another possible approach would be to just return an error, that we can't emit via
            // Ref.Emit and let the user choose another method of emitting. For that we would want
            // to preserve the state of the Emit.Assembly object with all the compiled methods so
            // that the subsequent emit doesn't need to compile method bodies again.

            // TODO (tomat):
            //
            // If Ref.Emit fails to emit the code the type builders already created will stay
            // defined on the module builder. Ideally we would clean them up but Ref.Emit doesn't
            // provide any API to do so. In fact it also keeps baked TypeBuilders alive as well.

            if (!recoverOnError)
            {
                metadataDiagnostics.Free();
                entryPoint = null;
                return false;
            }

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

            var compiledAssembly = Assembly.Load(compiledAssemblyImage);
            entryPoint = (cciEntryPoint != null) ? ReflectionEmitter.ResolveEntryPoint(compiledAssembly, cciEntryPoint, context) : null;

            // translate metadata errors.
            return compilation.FilterAndAppendAndFreeDiagnostics(diagnostics, ref metadataDiagnostics);
        }
    }
}
