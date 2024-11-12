// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers.DeclarationName;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class)]
internal sealed class ExportDeclarationNameRecommenderAttribute(string name) : ExportAttribute(typeof(IDeclarationNameRecommender))
{
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
}
