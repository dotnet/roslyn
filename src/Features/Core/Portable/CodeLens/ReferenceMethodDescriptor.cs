// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.CodeLens
{
    /// <summary>
    /// A caller method of a callee
    /// </summary>
    [DataContract]
    internal sealed class ReferenceMethodDescriptor(string fullName, string filePath, string outputFilePath)
    {
        /// <summary>
        ///  Returns method's fully quilified name without parameters
        /// </summary>
        [DataMember(Order = 0)]
        public string FullName { get; private set; } = fullName;

        /// <summary>
        /// Returns method's file path.
        /// </summary>
        [DataMember(Order = 1)]
        public string FilePath { get; private set; } = filePath;

        /// <summary>
        /// Returns output file path for the project containing the method.
        /// </summary>
        [DataMember(Order = 2)]
        public string OutputFilePath { get; private set; } = outputFilePath;
    }
}
