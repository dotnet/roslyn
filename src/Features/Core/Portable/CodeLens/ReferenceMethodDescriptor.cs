// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeLens
{
    /// <summary>
    /// A caller method of a callee
    /// </summary>
    internal sealed class ReferenceMethodDescriptor
    {
        /// <summary>
        /// Describe a caller method of a callee
        /// </summary>
        /// <param name="methodFullName">Method's fully qualified name</param>
        /// <param name="methodFullPath">Method full path</param>
        /// <remarks>
        ///  Method full name is expected to be in the .NET full name type convention. That is,
        ///  namespace/type is delimited by '.' and nested type is delimited by '+'
        /// </remarks>
        public ReferenceMethodDescriptor(string methodFullName, string methodFullPath)
        {
            FullName = methodFullName;
            FullPath = methodFullPath;
        }

        /// <summary>
        ///  Returns method's fully quilified name without parameters
        /// </summary>
        public string FullName { get; private set; }

        /// <summary>
        /// Returns method's file path.
        /// </summary>
        public string FullPath { get; private set; }
    }
}
