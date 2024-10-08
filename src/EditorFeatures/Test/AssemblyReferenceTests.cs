// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests;

/// <summary>
/// VS for mac has some restrictions on the assemblies that they can load.
/// These tests are created to make sure we don't accidentally add references to the dlls that they cannot load.
/// </summary>
public class AssemblyReferenceTests
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26642")]
    public void TestNoReferenceToImageCatalog()
    {
        var editorsFeatureAssembly = typeof(Microsoft.CodeAnalysis.Editor.Shared.Extensions.GlyphExtensions).Assembly;
        var dependencies = editorsFeatureAssembly.GetReferencedAssemblies();
        Assert.Empty(dependencies.Where(a => a.FullName.Contains("Microsoft.VisualStudio.ImageCatalog")));
    }

    [Fact]
    public void TestNoReferenceToImagingInterop()
    {
        var editorsFeatureAssembly = typeof(Microsoft.CodeAnalysis.Editor.Shared.Extensions.GlyphExtensions).Assembly;
        var dependencies = editorsFeatureAssembly.GetReferencedAssemblies();
        Assert.Empty(dependencies.Where(a => a.FullName.Contains("Microsoft.VisualStudio.Imaging.Interop")));
    }
}
