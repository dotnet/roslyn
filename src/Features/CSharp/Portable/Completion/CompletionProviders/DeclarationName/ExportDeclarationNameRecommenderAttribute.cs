// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers.DeclarationName
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportDeclarationNameRecommenderAttribute : ExportAttribute
    {
        public string Name { get; }

        public ExportDeclarationNameRecommenderAttribute(string name)
            : base(typeof(IDeclarationNameRecommender))
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
