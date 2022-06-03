// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.DiaSymReader
{
    /// <summary>
    /// Exception reported when PDB write operation fails.
    /// </summary>
    internal sealed class SymUnmanagedWriterException : Exception
    {
        /// <summary>
        /// The name of the module that implements the underlying PDB writer (e.g. diasymreader.dll), or null if not available.
        /// </summary>
        public string ImplementationModuleName { get; }

        public SymUnmanagedWriterException()
        {
        }

        public SymUnmanagedWriterException(string message) : base(message)
        {
        }

        public SymUnmanagedWriterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public SymUnmanagedWriterException(string message, Exception innerException, string implementationModuleName)
            : base(message, innerException)
        {
            ImplementationModuleName = implementationModuleName;
        }

        internal SymUnmanagedWriterException(Exception innerException, string implementationModuleName)
            : this(innerException.Message, innerException, implementationModuleName)
        {
        }
    }
}
