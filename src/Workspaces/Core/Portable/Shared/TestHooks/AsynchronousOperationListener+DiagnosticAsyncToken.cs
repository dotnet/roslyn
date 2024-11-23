// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.TestHooks;

internal sealed partial class AsynchronousOperationListener
{
    /// <summary>
    /// Stores the source information for an <see cref="IAsyncToken"/> value.  Helpful when 
    /// tracking down tokens which aren't properly disposed.
    /// </summary>
    internal sealed class DiagnosticAsyncToken(
        AsynchronousOperationListener listener,
        string name,
        object? tag,
        string filePath,
        int lineNumber) : AsyncToken(listener)
    {
        public string Name { get; } = name;
        public string FilePath { get; } = filePath;
        public int LineNumber { get; } = lineNumber;
        public object? Tag { get; } = tag;
        public Task? Task { get; set; }

        internal void AssociateWithTask(Task task)
            => Task = task;

        public override string ToString() => $"{Name} {Path.GetFileName(FilePath)} {LineNumber}";
    }
}
