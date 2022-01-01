// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if NETCOREAPP
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using static Roslyn.Test.Utilities.RuntimeEnvironmentUtilities;
using System.Reflection.PortableExecutable;
using System.Reflection;
using System.Reflection.Metadata;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Diagnostics.Tracing;

namespace Roslyn.Test.Utilities.CoreClr
{
    public class CoreCLRRuntimeEnvironment : IRuntimeEnvironment, IInternalRuntimeEnvironment
    {
        static CoreCLRRuntimeEnvironment()
        {
            SharedConsole.OverrideConsole();
        }

        private readonly IEnumerable<ModuleData> _additionalDependencies;
        private EmitData _emitData;
        private readonly CompilationTestData _testData = new CompilationTestData();

        public CoreCLRRuntimeEnvironment(IEnumerable<ModuleData> additionalDependencies = null)
        {
            _additionalDependencies = additionalDependencies;
        }

        public void Emit(
            Compilation mainCompilation,
            IEnumerable<ResourceDescription> manifestResources,
            EmitOptions emitOptions,
            bool usePdbForDebugging = false)
        {
            _testData.Methods.Clear();

            var diagnostics = DiagnosticBag.GetInstance();
            var dependencies = new List<ModuleData>();
            var mainOutput = EmitCompilation(mainCompilation, manifestResources, dependencies, diagnostics, _testData, emitOptions);

            _emitData = new EmitData();
            _emitData.Diagnostics = diagnostics.ToReadOnlyAndFree();

            if (mainOutput.HasValue)
            {
                var mainImage = mainOutput.Value.Assembly;
                var mainPdb = mainOutput.Value.Pdb;
                _emitData.MainModule = new ModuleData(
                    mainCompilation.Assembly.Identity,
                    mainCompilation.Options.OutputKind,
                    mainImage,
                    pdb: usePdbForDebugging ? mainPdb : default(ImmutableArray<byte>),
                    inMemoryModule: true);
                _emitData.MainModulePdb = mainPdb;
                _emitData.AllModuleData = dependencies;

                // We need to add the main module so that it gets checked against already loaded assembly names.
                // If an assembly is loaded directly via PEVerify(image) another assembly of the same full name
                // can't be loaded as a dependency (via Assembly.ReflectionOnlyLoad) in the same domain.
                _emitData.AllModuleData.Insert(0, _emitData.MainModule);
                _emitData.RuntimeData = new RuntimeData(dependencies);
            }
            else
            {
                DumpAssemblyData(dependencies, out var dumpDir);

                // This method MUST throw if compilation did not succeed.  If compilation succeeded and there were errors, that is bad.
                // Please see KevinH if you intend to change this behavior as many tests expect the Exception to indicate failure.
                throw new EmitException(_emitData.Diagnostics, dumpDir);
            }
        }

        public int Execute(string moduleName, string[] args, string expectedOutput)
        {
            var emitData = GetEmitData();
            emitData.RuntimeData.ExecuteRequested = true;
            var (ExitCode, Output) = emitData.LoadContext.Execute(GetMainImage(), args, expectedOutput?.Length);

            if (expectedOutput != null && expectedOutput.Trim() != Output.Trim())
            {
                throw new ExecutionException(expectedOutput, Output, moduleName);
            }

            return ExitCode;
        }

        private EmitData GetEmitData() => _emitData ?? throw new InvalidOperationException("Must call Emit before calling this method");

        public IList<ModuleData> GetAllModuleData() => GetEmitData().AllModuleData;

        public ImmutableArray<Diagnostic> GetDiagnostics() => GetEmitData().Diagnostics;

        public ImmutableArray<byte> GetMainImage() => GetEmitData().MainModule.Image;

        public ImmutableArray<byte> GetMainPdb() => GetEmitData().MainModulePdb;

        public SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName) =>
            GetEmitData().GetMemberSignaturesFromMetadata(fullyQualifiedTypeName, memberName);

        private class Resolver : ILVerify.ResolverBase
        {
            private readonly Dictionary<string, ImmutableArray<byte>> imagesByName = new Dictionary<string, ImmutableArray<byte>>();

            internal Resolver(EmitData emitData)
            {
                foreach (var module in emitData.AllModuleData)
                {
                    string name = module.FullName;

                    //var image = name == "mscorlib"
                    //    ? ImmutableArray<byte>.Empty // TODO2 TestResources.NetFX.v4_6_1038_0.mscorlib.AsImmutable()
                    //    : module.Image;
                    var image = module.Image;

                    // TODO2 figure out why we need both the simple name and full name
                    imagesByName.Add(name, image);
                    imagesByName.TryAdd(module.SimpleName, image);
                }
            }

            protected override PEReader ResolveCore(string name)
            {
                if (imagesByName.TryGetValue(name, out var image))
                {
                    return new PEReader(image);
                }

                return null;
            }
        }

        public void Verify(Verification verification)
        {
            var emitData = GetEmitData();
            emitData.RuntimeData.PeverifyRequested = true;
            // TODO(https://github.com/dotnet/coreclr/issues/295): Implement peverify

            // IL Verify
            if (verification == Verification.Skipped)
            {
                return;
            }

            var resolver = new Resolver(emitData);
            var verifier = new ILVerify.Verifier(resolver);
            var mscorlibModules = emitData.AllModuleData.Where(m => m.SimpleName == "mscorlib").ToArray();
            if (mscorlibModules.Length == 1)
            {
                verifier.SetSystemModuleName(new AssemblyName(mscorlibModules[0].FullName));
            }
            else
            {
                // auto-detect which module is the "corlib"
                foreach (var module in emitData.AllModuleData)
                {
                    var name = module.SimpleName;
                    var metadataReader = resolver.Resolve(name).GetMetadataReader();
                    if (metadataReader.AssemblyReferences.Count == 0)
                    {
                        try
                        {
                            verifier.SetSystemModuleName(new AssemblyName(name));
                        }
                        catch (Exception ex)
                        {
                            // ILVerify checks that corlib contains certain types
                            if ((verification & Verification.FailsIlVerify_MissingStringType) == Verification.FailsIlVerify_MissingStringType)
                            {
                                if (!ex.Message.Contains("Failed to load type 'System.String' from assembly"))
                                {
                                    throw new Exception("Expected: Failed to load type 'System.String' from assembly ...");
                                }
                                return;
                            }

                            throw;
                        }
                    }
                }
            }

            var result = verifier.Verify(resolver.Resolve(emitData.MainModule.FullName));
            if (result.Count() == 0)
            {
                if ((verification & Verification.FailsIlVerify) == 0)
                {
                    return;
                }

                throw new Exception("IL Verify succeeded unexpectedly");
            }

            string message = printVerificationResult(result);
            if ((verification & Verification.FailsIlVerify_TypedReference) == Verification.FailsIlVerify_TypedReference
                && message.Contains("TypedReference not supported in .NET Core"))
            {
                return;
            }

            if ((verification & Verification.FailsIlVerify_NotImplemented) == Verification.FailsIlVerify_NotImplemented
                && message.Contains("The method or operation is not implemented."))
            {
                return;
            }

            if ((verification & Verification.Fails_InitOnly) == Verification.Fails_InitOnly
                && message.Contains("Cannot change initonly field outside its .ctor."))
            {
                return;
            }

            if ((verification & Verification.FailsIlVerify_NotVisible) == Verification.FailsIlVerify_NotVisible
                && message.Contains(" is not visible."))
            {
                return;
            }

            if ((verification & Verification.FailsIlVerify_UnrecognizedArgDelegate) == Verification.FailsIlVerify_UnrecognizedArgDelegate
                && message.Contains("Unrecognized arguments for delegate .ctor."))
            {
                return;
            }

            if ((verification & Verification.FailsIlVerify_MissingAssembly) == Verification.FailsIlVerify_MissingAssembly
                && message.Contains("Assembly or module not found: "))
            {
                return;
            }

            if ((verification & Verification.FailsIlVerify_UnexpectedReadonlyAddressOnStack) == Verification.FailsIlVerify_UnexpectedReadonlyAddressOnStack
                && message.Contains("Unexpected type on the stack.")
                && message.Contains("Found = readonly address of")
                && message.Contains("Expected = address of"))
            {
                return;
            }

            if ((verification & Verification.FailsIlVerify_BadReturnType) == Verification.FailsIlVerify_BadReturnType
                && message.Contains("Return type is ByRef, TypedReference, ArgHandle, or ArgIterator."))
            {
                return;
            }

            if ((verification & Verification.FailsIlVerify_UnexpectedTypeOnStack) == Verification.FailsIlVerify_UnexpectedTypeOnStack
                && message.Contains("Unexpected type on the stack."))
            {
                return;
            }

            // TODO2 remove?
            if ((verification & Verification.FailsIlVerify_ImportCalli) == Verification.FailsIlVerify_ImportCalli
                && message.Contains("ImportCalli not implemented"))
            {
                return;
            }

            if ((verification & Verification.FailsIlVerify_UnspecifiedError) == Verification.FailsIlVerify_UnspecifiedError)
            {
                return;
            }

            throw new Exception("IL Verify failed unexpectedly: \r\n" + message);

            static string printVerificationResult(IEnumerable<ILVerify.VerificationResult> result)
            {
                return string.Join("\r\n", result.Select(r => r.Message + printErrorArguments(r.ErrorArguments)));
            }

            static string printErrorArguments(ILVerify.ErrorArgument[] errorArguments)
            {
                if (errorArguments is null
                    || errorArguments.Length == 0)
                {
                    return "";
                }

                var pooledBuilder = PooledStringBuilder.GetInstance();
                var builder = pooledBuilder.Builder;
                builder.Append(" { ");
                builder.AppendJoin(", ", errorArguments.Select(a => a.Name + " = " + a.Value.ToString()));
                builder.Append(" }");

                return pooledBuilder.ToStringAndFree();
            }
        }

        public string[] VerifyModules(string[] modulesToVerify)
        {
            // TODO(https://github.com/dotnet/coreclr/issues/295): Implement peverify
            return null;
        }

        CompilationTestData IInternalRuntimeEnvironment.GetCompilationTestData()
        {
            return _testData;
        }

        public void Dispose()
        {
            // We need Dispose to satisfy the IRuntimeEnvironment interface, but 
            // we don't really need it.
        }

        public void CaptureOutput(Action action, int expectedLength, out string output, out string errorOutput)
            => SharedConsole.CaptureOutput(action, expectedLength, out output, out errorOutput);

        private sealed class RuntimeData
        {
            internal TestExecutionLoadContext LoadContext { get; }
            internal bool PeverifyRequested { get; set; }
            internal bool ExecuteRequested { get; set; }
            internal bool Disposed { get; set; }
            internal int ConflictCount { get; set; }

            public RuntimeData(IList<ModuleData> dependencies)
            {
                LoadContext = new TestExecutionLoadContext(dependencies);
            }
        }

        private sealed class EmitData
        {
            internal RuntimeData RuntimeData;

            internal TestExecutionLoadContext LoadContext => RuntimeData?.LoadContext;

            // All of the <see cref="ModuleData"/> created for this Emit
            internal List<ModuleData> AllModuleData;

            // Main module for this emit
            internal ModuleData MainModule;
            internal ImmutableArray<byte> MainModulePdb;

            internal ImmutableArray<Diagnostic> Diagnostics;

            public SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName)
            {
                return LoadContext.GetMemberSignaturesFromMetadata(fullyQualifiedTypeName, memberName, AllModuleData.Select(x => x.Id));
            }
        }
    }
}
#endif
