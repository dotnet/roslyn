// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    public interface ICompilerServerHost
    {
        IAnalyzerAssemblyLoader AnalyzerAssemblyLoader { get; }

        /// <summary>
        /// Directory from which mscorlib can be loaded.  This can be null on platforms which don't have a concept of mscorlib
        /// </summary>
        string GetSdkDirectory();

        Task<IClientConnection> CreateListenTask(CancellationToken cancellationToken);

        bool CheckAnalyzers(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzers, out BuildResponse response);
    }
}
