// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// This a factory type for creating <see cref="IRuntimeEnvironment" /> instances for the current runtime.
    /// </summary>
    public static class RuntimeEnvironmentFactory
    {
        private static IRuntimeEnvironmentFactory Instance { get; } = RuntimeUtilities.GetRuntimeEnvironmentFactory();

        internal static IRuntimeEnvironment Create(ModuleData mainModule, ImmutableArray<ModuleData> modules = default) =>
            Instance.Create(mainModule, modules);

        public static (string Output, string ErrorOutput) CaptureOutput(Action action, int? maxOutputLength) =>
            Instance.CaptureOutput(action, maxOutputLength);
    }

    public interface IRuntimeEnvironmentFactory
    {
        IRuntimeEnvironment Create(ModuleData mainModule, ImmutableArray<ModuleData> modules);
        (string Output, string ErrorOutput) CaptureOutput(Action action, int? maxOutputLength);
    }

    /// <summary>
    /// This is used for executing a set of modules in an isolated runtime environment for the current .NET
    /// runtime.
    /// </summary>
    public interface IRuntimeEnvironment : IDisposable
    {
        (int ExitCode, string Output, string ErrorOutput) Execute(string[] args, int? maxOutputLength);
        SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName);
        void Verify(Verification verification);
        string[] VerifyModules(string[] modulesToVerify);
    }
}
