// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Test.Utilities;
using static Roslyn.Test.Utilities.RuntimeUtilities; 

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class CoreCLRRuntimeEnvironment : IRuntimeEnvironment, IInternalRuntimeEnvironment
    {
        private IEnumerable<ModuleData> _additionalDependencies;
        private CompilationTestData _testData = new CompilationTestData();
        private ImmutableArray<byte> _mainImage;
        private ImmutableArray<byte> _pdb;
        private ImmutableArray<Diagnostic> _diagnostics;

        public CoreCLRRuntimeEnvironment(IEnumerable<ModuleData> additionalDependencies = null)
        {
            _additionalDependencies = additionalDependencies;
        }

        public void Emit(Compilation mainCompilation, IEnumerable<ResourceDescription> manifestResources, bool usePdbForDebugging = false)
        {
            _testData.Methods.Clear();
            var diagnostics = DiagnosticBag.GetInstance();
            var dependencies = new List<ModuleData>();
            var mainOutput = RuntimeUtilities.EmitCompilation(mainCompilation, manifestResources, dependencies, diagnostics, _testData);

            if (mainOutput.HasValue)
            {
                _diagnostics = diagnostics.ToReadOnly();
                _mainImage = mainOutput.Value.Assembly;
                _pdb = mainOutput.Value.Pdb;
            }
            else
            {
                _mainImage = default(ImmutableArray<byte>);
                _pdb = default(ImmutableArray<byte>);
                _diagnostics = default(ImmutableArray<Diagnostic>);

                string dumpDir;
                DumpAssemblyData(dependencies, out dumpDir);
                throw new EmitException(diagnostics.ToReadOnly(), dumpDir);
            }
        }

        public int Execute(string moduleName, string expectedOutput)
        {
            throw new NotImplementedException();
        }

        public IList<ModuleData> GetAllModuleData()
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<Diagnostic> GetDiagnostics() => _diagnostics;
        public ImmutableArray<byte> GetMainImage() => _mainImage;
        public ImmutableArray<byte> GetMainPdb() => _pdb;

        public SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName)
        {
            throw new NotImplementedException();
        }

        public void PeVerify()
        {
            throw new NotImplementedException();
        }

        public string[] PeVerifyModules(string[] modulesToVerify, bool throwOnError = true)
        {
            throw new NotImplementedException();
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
    }
}
