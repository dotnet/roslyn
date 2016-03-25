using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    class CoreCLRRuntimeEnvironment : IRuntimeUtility, IInternalRuntimeUtility
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
                throw new EmitException(diagnostics.ToReadOnly(), null);
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

        CompilationTestData IInternalRuntimeUtility.GetCompilationTestData()
        {
            return _testData;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CoreCLRRuntimeEnvironment() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
