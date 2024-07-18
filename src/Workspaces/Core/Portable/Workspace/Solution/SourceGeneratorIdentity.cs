// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis;

/// <remarks>
/// Assembly path is used as a part of a generator identity to deal with the case that the user accidentally installed
/// the same generator twice from two different paths, or actually has two different generators that just happened to
/// use the same name. In the wild we've seen cases where a user has a broken project or build that results in the same
/// generator being added twice; we aren't going to try to deduplicate those anywhere since currently the compiler
/// does't do any deduplication either: you'll simply get duplicate outputs which might collide and cause compile
/// errors. If https://github.com/dotnet/roslyn/issues/56619 is addressed, we can potentially match the compiler
/// behavior by taking a different approach here.
/// </remarks>
[DataContract]
internal readonly record struct SourceGeneratorIdentity(
    [property: DataMember(Order = 0)] string AssemblyName,
    [property: DataMember(Order = 1)] string? AssemblyPath,
    [property: DataMember(Order = 2)] Version AssemblyVersion,
    [property: DataMember(Order = 3)] string TypeName)
{
    public static SourceGeneratorIdentity Create(ISourceGenerator generator, AnalyzerReference analyzerReference)
    {
        var generatorType = generator.GetGeneratorType();
        var assembly = generatorType.Assembly;
        var assemblyName = assembly.GetName();
        return new SourceGeneratorIdentity(
            assemblyName.Name!, analyzerReference.FullPath, assemblyName.Version!, generatorType.FullName!);
    }
}
