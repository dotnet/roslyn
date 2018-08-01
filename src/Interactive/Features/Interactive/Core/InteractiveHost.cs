// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal abstract class InteractiveHost : IDisposable
    {
        public const bool DefaultIs64Bit = true;

        public abstract InteractiveHostOptions OptionsOpt { get; }

        public abstract event Action<bool> ProcessStarting;
        public abstract event Action<Process> InteractiveHostProcessCreated;

        public abstract void SetOutput(TextWriter value);
        public abstract void SetErrorOutput(TextWriter value);

        public abstract Task<bool> AddReferenceAsync(string reference);
        public abstract Task<InteractiveExecutionResult> ResetAsync(InteractiveHostOptions options);
        public abstract Task<InteractiveExecutionResult> ExecuteAsync(string code);
        public abstract Task<InteractiveExecutionResult> ExecuteFileAsync(string path);
        public abstract Task<InteractiveExecutionResult> SetPathsAsync(ImmutableArray<string> referenceSearchPaths, ImmutableArray<string> sourceSearchPaths, string baseDirectory);

        public abstract void Dispose();

        // test hooks:
        public abstract Process TryGetProcess();
        public abstract event Action<char[], int> OutputReceived;
        public abstract event Action<char[], int> ErrorOutputReceived;
        public abstract void RemoteConsoleWrite(byte[] data, bool isError);
        public abstract void EmulateClientExit();
        public abstract void HookMaliciousAssemblyResolve();
    }
}
