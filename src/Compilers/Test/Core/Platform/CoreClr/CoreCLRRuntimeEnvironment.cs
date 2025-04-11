﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using static Roslyn.Test.Utilities.RuntimeEnvironmentUtilities;

namespace Roslyn.Test.Utilities.CoreClr
{
    public class CoreCLRRuntimeEnvironment(ModuleData mainModule, ImmutableArray<ModuleData> modules) : IRuntimeEnvironment
    {
        public ModuleData MainModule { get; } = mainModule;
        public ImmutableArray<ModuleData> Modules { get; } = modules;
        internal TestExecutionLoadContext LoadContext { get; } = new TestExecutionLoadContext(modules);

        static CoreCLRRuntimeEnvironment()
        {
            SharedConsole.OverrideConsole();
        }

        public (int ExitCode, string Output, string ErrorOutput) Execute(string[] args, int? expectedLength) =>
            LoadContext.Execute(MainModule, args, expectedLength);

        public SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName)
        {
            return LoadContext.GetMemberSignaturesFromMetadata(fullyQualifiedTypeName, memberName, Modules.Select(x => x.Id));
        }

        public void Verify(Verification verification) =>
            CompilationVerifier.ILVerify(verification, MainModule, Modules);

        public string[] VerifyModules(string[] modulesToVerify) =>
            throw new NotSupportedException();

        public void Dispose()
        {
            LoadContext.Unload();
        }
    }
}
#endif
