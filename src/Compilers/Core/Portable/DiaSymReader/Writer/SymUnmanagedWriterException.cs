// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
