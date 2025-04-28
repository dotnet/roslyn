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
    /// This is used for executing a set of modules in an isolated runtime environment for the current .NET
    /// runtime.
    /// </summary>
    /// <remarks>
    /// Obtain instances of this interface from <see cref="RuntimeUtilities.CreateRuntimeEnvironment(ModuleData, ImmutableArray{ModuleData})"/> 
    /// </remarks>
    public interface IRuntimeEnvironment : IDisposable
    {
        (int ExitCode, string Output, string ErrorOutput) Execute(string[] args);
        SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName);
        void Verify(Verification verification);
        string[] VerifyModules(string[] modulesToVerify);
    }
}
