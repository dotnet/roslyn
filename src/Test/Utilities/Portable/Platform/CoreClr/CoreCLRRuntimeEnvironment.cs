// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#if NETCOREAPP2_1
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using static Roslyn.Test.Utilities.RuntimeEnvironmentUtilities;

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
        private CompilationTestData _testData = new CompilationTestData();

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

        public void Verify(Verification verification)
        {
            var emitData = GetEmitData();
            emitData.RuntimeData.PeverifyRequested = true;
            // TODO(https://github.com/dotnet/coreclr/issues/295): Implement peverify
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
