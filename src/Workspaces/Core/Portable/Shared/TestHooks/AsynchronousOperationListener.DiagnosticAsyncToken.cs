// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.IO;
using Roslyn.Utilities;

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
            {
                Task = task;
            }

            public override string ToString() => $"{Name} {Path.GetFileName(FilePath)} {LineNumber}";
        }
    }
}
