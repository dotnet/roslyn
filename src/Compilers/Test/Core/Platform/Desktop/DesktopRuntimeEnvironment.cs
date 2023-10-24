// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if NET472

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using static Roslyn.Test.Utilities.RuntimeEnvironmentUtilities;

namespace Roslyn.Test.Utilities.Desktop
{
    public sealed class DesktopRuntimeEnvironment : IDisposable, IRuntimeEnvironment, IInternalRuntimeEnvironment
    {
        private sealed class RuntimeData : IDisposable
        {
            internal RuntimeAssemblyManager Manager { get; }
            internal AppDomain AppDomain { get; }
            internal bool PeverifyRequested { get; set; }
            internal bool ExecuteRequested { get; set; }
            internal bool Disposed { get; set; }
            internal int ConflictCount { get; set; }

            internal RuntimeData(RuntimeAssemblyManager manager, AppDomain appDomain)
            {
                Manager = manager;
                AppDomain = appDomain;
            }

            public void Dispose()
            {
                if (Disposed)
                {
                    return;
                }

                Manager.Dispose();

                // A workaround for known bug DevDiv 369979 - don't unload the AppDomain if we may have loaded a module
                var safeToUnload = !(Manager.ContainsNetModules() && (PeverifyRequested || ExecuteRequested));
                if (safeToUnload && AppDomain != null)
                {
                    AppDomain.Unload(AppDomain);
                }

                Disposed = true;
            }
        }

        private sealed class EmitData
        {
            internal RuntimeData RuntimeData;

            internal RuntimeAssemblyManager Manager => RuntimeData?.Manager;

            // All of the <see cref="ModuleData"/> created for this Emit
            internal List<ModuleData> AllModuleData;

            // Main module for this emit
            internal ModuleData MainModule;
            internal ImmutableArray<byte> MainModulePdb;

            internal ImmutableArray<Diagnostic> Diagnostics;

            internal EmitData()
            {
            }
        }

        /// <summary>
        /// Profiling demonstrates the creation of AppDomains take up a significant amount of time in the
        /// test run time.  Hence we re-use them so long as there are no conflicts with the existing loaded
        /// modules.
        /// </summary>
        private static readonly List<RuntimeData> s_runtimeDataCache = new List<RuntimeData>();
        private const int MaxCachedRuntimeData = 5;

        private EmitData _emitData;
        private bool _disposed;
        private readonly CompilationTestData _testData = new CompilationTestData();
        private readonly IEnumerable<ModuleData> _additionalDependencies;

        public DesktopRuntimeEnvironment(IEnumerable<ModuleData> additionalDependencies = null)
        {
            _additionalDependencies = additionalDependencies;
        }

        private RuntimeData CreateAndInitializeRuntimeData(IEnumerable<ModuleData> compilationDependencies, ModuleDataId mainModuleId)
        {
            var allModules = compilationDependencies;
            if (_additionalDependencies != null)
            {
                allModules = allModules.Concat(_additionalDependencies);
            }

            allModules = allModules.ToArray();

            var runtimeData = GetOrCreateRuntimeData(allModules);

            // Many prominent assemblies like mscorlib are already in the RuntimeAssemblyManager.  Only
            // add in the delta values to reduce serialization overhead going across AppDomains.
            var manager = runtimeData.Manager;
            var missingList = manager
                .GetMissing(allModules.Select(x => new RuntimeModuleDataId(x.Id)).ToList())
                .Select(x => x.Id);
            var deltaList = allModules
                .Where(x => missingList.Contains(x.Id))
                .Select(x => new RuntimeModuleData(x))
                .ToList();
            manager.AddModuleData(deltaList);
            manager.AddMainModuleMvid(mainModuleId.Mvid);

            return runtimeData;
        }

        private static RuntimeData GetOrCreateRuntimeData(IEnumerable<ModuleData> modules)
        {
            var data = TryGetCachedRuntimeData(modules);
            if (data != null)
            {
                return data;
            }

            return CreateRuntimeData();
        }

        private static RuntimeData TryGetCachedRuntimeData(IEnumerable<ModuleData> modules)
        {
            lock (s_runtimeDataCache)
            {
                var i = 0;
                while (i < s_runtimeDataCache.Count)
                {
                    var data = s_runtimeDataCache[i];
                    var manager = data.Manager;
                    if (!manager.HasConflicts(modules.Select(x => new RuntimeModuleDataId(x.Id)).ToList()))
                    {
                        s_runtimeDataCache.RemoveAt(i);
                        return data;
                    }

                    data.ConflictCount++;
                    if (data.ConflictCount > 5)
                    {
                        // Once a RuntimeAssemblyManager is proven to have conflicts it's likely subsequent runs
                        // will also have conflicts.  Take it out of the cache.
                        data.Dispose();
                        s_runtimeDataCache.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }

            return null;
        }

        private static RuntimeData CreateRuntimeData()
        {
            AppDomain appDomain = null;
            try
            {
                var appDomainProxyType = typeof(RuntimeAssemblyManager);
                var thisAssembly = appDomainProxyType.Assembly;
                appDomain = AppDomainUtils.Create("HostedRuntimeEnvironment");
                var manager = (RuntimeAssemblyManager)appDomain.CreateInstanceAndUnwrap(thisAssembly.FullName, appDomainProxyType.FullName);
                return new RuntimeData(manager, appDomain);
            }
            catch
            {
                if (appDomain != null)
                {
                    AppDomain.Unload(appDomain);
                }
                throw;
            }
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
                var corLibIdentity = mainCompilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly.Identity;
                var identity = mainCompilation.Assembly.Identity;
                _emitData.MainModule = new ModuleData(
                    identity,
                    mainCompilation.Options.OutputKind,
                    mainImage,
                    pdb: usePdbForDebugging ? mainPdb : default(ImmutableArray<byte>),
                    inMemoryModule: true,
                    isCorLib: corLibIdentity == identity);
                _emitData.MainModulePdb = mainPdb;
                _emitData.AllModuleData = dependencies;

                // We need to add the main module so that it gets checked against already loaded assembly names.
                // If an assembly is loaded directly via PEVerify(image) another assembly of the same full name
                // can't be loaded as a dependency (via Assembly.ReflectionOnlyLoad) in the same domain.
                _emitData.AllModuleData.Insert(0, _emitData.MainModule);
                _emitData.RuntimeData = CreateAndInitializeRuntimeData(dependencies, _emitData.MainModule.Id);
            }
            else
            {
                DumpAssemblyData(dependencies, out var dumpDir);

                // This method MUST throw if compilation did not succeed.  If compilation succeeded and there were errors, that is bad.
                // Please see KevinH if you intend to change this behavior as many tests expect the Exception to indicate failure.
                throw new EmitException(_emitData.Diagnostics, dumpDir);
            }
        }

        public int Execute(string moduleName, string[] args, string expectedOutput, bool trimOutput = true)
        {
            try
            {
                var emitData = GetEmitData();
                emitData.RuntimeData.ExecuteRequested = true;
                var resultCode = emitData.Manager.Execute(moduleName, args, expectedOutputLength: expectedOutput?.Length, out var output);

                if (expectedOutput != null)
                {
                    if (trimOutput ? (expectedOutput.Trim() != output.Trim()) : (expectedOutput != output))
                    {
                        GetEmitData().Manager.DumpAssemblyData(out var dumpDir);
                        throw new ExecutionException(expectedOutput, output, moduleName);
                    }
                }

                return resultCode;
            }
            catch (TargetInvocationException tie)
            {
                if (_emitData.Manager == null)
                {
                    throw;
                }

                _emitData.Manager.DumpAssemblyData(out var dumpDir);
                throw new ExecutionException(tie.InnerException, dumpDir);
            }
        }

        private EmitData GetEmitData()
        {
            if (_emitData == null)
            {
                throw new InvalidOperationException("You must call Emit before calling this method.");
            }

            return _emitData;
        }

        public ImmutableArray<Diagnostic> GetDiagnostics()
        {
            return GetEmitData().Diagnostics;
        }

        public ImmutableArray<byte> GetMainImage()
        {
            return GetEmitData().MainModule.Image;
        }

        public ImmutableArray<byte> GetMainPdb()
        {
            return GetEmitData().MainModulePdb;
        }

        public IList<ModuleData> GetAllModuleData()
        {
            return GetEmitData().AllModuleData;
        }

        public void Verify(Verification verification)
        {
            // Verification is only done on windows desktop 
            if (!ExecutionConditionUtil.IsWindowsDesktop)
            {
                return;
            }

            if (verification.Status.HasFlag(VerificationStatus.Skipped))
            {
                return;
            }

            var shouldSucceed = !verification.Status.HasFlag(VerificationStatus.FailsPEVerify);
            var emitData = GetEmitData();

            try
            {
                emitData.RuntimeData.PeverifyRequested = true;
                emitData.Manager.PeVerifyModules(new[] { emitData.MainModule.FullName }, throwOnError: true);
                if (!shouldSucceed)
                {
                    throw new Exception("PE Verify succeeded unexpectedly");
                }
            }
            catch (RuntimePeVerifyException ex)
            {
                if (shouldSucceed)
                {
                    throw new Exception("Verification failed", ex);
                }

                var expectedMessage = verification.PEVerifyMessage;
                if (expectedMessage != null && !IsEnglishLocal.Instance.ShouldSkip)
                {
                    var actualMessage = ex.Output;

                    if (!verification.IncludeTokensAndModuleIds)
                    {
                        actualMessage = Regex.Replace(ex.Output, @"\[mdToken=0x[0-9a-fA-F]+\]", "");
                    }

                    AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedMessage, actualMessage);
                }
            }
        }

        public string[] VerifyModules(string[] modulesToVerify)
        {
            var emitData = GetEmitData();
            emitData.RuntimeData.PeverifyRequested = true;
            return emitData.Manager.PeVerifyModules(modulesToVerify, throwOnError: false);
        }

        public SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName)
        {
            var emitData = GetEmitData();
            var searchIds = emitData.AllModuleData.Select(x => new RuntimeModuleDataId(x.Id)).ToList();
            return GetEmitData().Manager.GetMemberSignaturesFromMetadata(fullyQualifiedTypeName, memberName, searchIds);
        }

        void IDisposable.Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_emitData != null)
            {
                lock (s_runtimeDataCache)
                {
                    if (_emitData.RuntimeData != null && s_runtimeDataCache.Count < MaxCachedRuntimeData)
                    {
                        s_runtimeDataCache.Add(_emitData.RuntimeData);
                        _emitData.RuntimeData = null;
                    }
                }

                if (_emitData.RuntimeData != null)
                {
                    _emitData.RuntimeData.Dispose();
                }

                _emitData = null;
            }

            _disposed = true;
        }

        CompilationTestData IInternalRuntimeEnvironment.GetCompilationTestData()
        {
            if (_testData.Module == null)
            {
                throw new InvalidOperationException("You must call Emit before calling GetCompilationTestData.");
            }
            return _testData;
        }

        private static readonly object s_consoleGuard = new object();

        internal static void Capture(Action action, int expectedLength, out string output, out string errorOutput)
        {
            TextWriter errorOutputWriter = new CappedStringWriter(expectedLength);
            TextWriter outputWriter = new CappedStringWriter(expectedLength);

            lock (s_consoleGuard)
            {
                TextWriter originalOut = Console.Out;
                TextWriter originalError = Console.Error;
                try
                {
                    Console.SetOut(outputWriter);
                    Console.SetError(errorOutputWriter);
                    action();
                }
                finally
                {
                    Console.SetOut(originalOut);
                    Console.SetError(originalError);
                }
            }

            output = outputWriter.ToString();
            errorOutput = errorOutputWriter.ToString();
        }

        public void CaptureOutput(Action action, int expectedLength, out string output, out string errorOutput) =>
            Capture(action, expectedLength, out output, out errorOutput);
    }
}
#endif
