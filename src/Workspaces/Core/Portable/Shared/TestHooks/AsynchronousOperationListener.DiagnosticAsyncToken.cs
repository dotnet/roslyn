// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.IO;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal sealed partial class AsynchronousOperationListener
    {
        /// <summary>
        /// Stores the source information for an <see cref="IAsyncToken"/> value.  Helpful when 
        /// tracking down tokens which aren't properly disposed.
        /// </summary>
        internal sealed class DiagnosticAsyncToken : AsyncToken
        {
            public string Name { get; }
            public string FilePath { get; }
            public int LineNumber { get; }
            public object Tag { get; }
            public Task Task { get; set; }

            public DiagnosticAsyncToken(
                AsynchronousOperationListener listener,
                string name,
                object tag,
                string filePath,
                int lineNumber)
                : base(listener)
            {
                Name = name;
                Tag = tag;
                FilePath = filePath;
                LineNumber = lineNumber;
            }

            internal void AssociateWithTask(Task task)
                => Task = task;

            public override string ToString() => $"{Name} {Path.GetFileName(FilePath)} {LineNumber}";
        }
    }
}
