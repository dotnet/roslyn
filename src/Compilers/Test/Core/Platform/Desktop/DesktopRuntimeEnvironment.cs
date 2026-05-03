// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace Roslyn.Test.Utilities.Desktop
{
    public sealed class DesktopRuntimeEnvironment(ModuleData mainModule, ImmutableArray<ModuleData> modules) : IDisposable, IRuntimeEnvironment
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

        /// <summary>
        /// Profiling demonstrates the creation of AppDomains take up a significant amount of time in the
        /// test run time.  Hence we re-use them so long as there are no conflicts with the existing loaded
        /// modules.
        /// </summary>
        private static readonly List<RuntimeData> s_runtimeDataCache = new List<RuntimeData>();
        private const int MaxCachedRuntimeData = 5;

        private bool _disposed;
        public ModuleData MainModule { get; } = mainModule;
        public ImmutableArray<ModuleData> Modules { get; } = modules;
        private RuntimeData Data { get; set; } = CreateAndInitializeRuntimeData(mainModule.Id, modules);

        private static RuntimeData CreateAndInitializeRuntimeData(ModuleDataId mainModuleId, ImmutableArray<ModuleData> runtimeModuleDataList)
        {
            var runtimeData = GetOrCreateRuntimeData(runtimeModuleDataList);

            // Many prominent assemblies like mscorlib are already in the RuntimeAssemblyManager.  Only
            // add in the delta values to reduce serialization overhead going across AppDomains.
            var manager = runtimeData.Manager;
            var missingList = manager
                .GetMissing(runtimeModuleDataList.Select(x => new RuntimeModuleDataId(x.Id)).ToList())
                .Select(x => x.Id);
            var deltaList = runtimeModuleDataList
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

        public (int ExitCode, string Output, string ErrorOutput) Execute(string[] args)
        {
            var exitCode = Data.Manager.Execute(MainModule.FullName, args, out string output, out string errorOutput);
            return (exitCode, output, errorOutput);
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

            try
            {
                Data.PeverifyRequested = true;
                Data.Manager.PeVerifyModules([MainModule.FullName], throwOnError: true);
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
            Data.PeverifyRequested = true;
            return Data.Manager.PeVerifyModules(modulesToVerify, throwOnError: false);
        }

        public SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName)
        {
            var searchIds = Modules.Select(x => new RuntimeModuleDataId(x.Id)).ToList();
            return Data.Manager.GetMemberSignaturesFromMetadata(fullyQualifiedTypeName, memberName, searchIds);
        }

        void IDisposable.Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (s_runtimeDataCache)
            {
                if (Data != null && s_runtimeDataCache.Count < MaxCachedRuntimeData)
                {
                    s_runtimeDataCache.Add(Data);
                    Data = null;
                }
            }

            if (Data != null)
            {
                Data.Dispose();
            }

            Data = null;
            _disposed = true;
        }
    }
}
#endif
