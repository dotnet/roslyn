// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.CodeLens;

/// <summary>
/// A caller method of a callee
/// </summary>
/// <remarks>
/// Describe a caller method of a callee
/// </remarks>
/// <param name="fullName">Method's fully qualified name</param>
/// <param name="filePath">Method full path</param>
/// <remarks>
///  Method full name is expected to be in the .NET full name type convention. That is,
///  namespace/type is delimited by '.' and nested type is delimited by '+'
/// </remarks>
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
