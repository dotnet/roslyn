// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    /// <summary>
    /// VS for mac has some restrictions on the assemblies that they can load.
    /// These tests are created to make sure we don't accidentally add references to the dlls that they cannot load.
    /// </summary>
    public class AssemblyReferenceTests
    {
        [Fact, WorkItem(26642, "https://github.com/dotnet/roslyn/issues/26642")]
        public void TestNoReferenceToImageCatalog()
        {
            var editorsFeatureAssembly = typeof(Microsoft.CodeAnalysis.Editor.Shared.Extensions.GlyphExtensions).Assembly;
            var dependencies = editorsFeatureAssembly.GetReferencedAssemblies();
            Assert.Empty(dependencies.Where(a => a.FullName.Contains("Microsoft.VisualStudio.ImageCatalog")));
        }
    }
}
