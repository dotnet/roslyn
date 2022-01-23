// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal static class RoslynGraphCategories
    {
        public static readonly GraphSchema Schema;

        public static readonly GraphCategory Overrides;

        static RoslynGraphCategories()
        {
            Schema = RoslynGraphProperties.Schema;

            Overrides = Schema.Categories.AddNewCategory(
                "Overrides",
                () => new GraphMetadata(GraphMetadataOptions.Sharable));
        }
    }
}
