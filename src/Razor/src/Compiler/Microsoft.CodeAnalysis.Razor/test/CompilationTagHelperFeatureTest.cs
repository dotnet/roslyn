// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor;

public class CompilationTagHelperFeatureTest
{
    [Fact]
    public void IsValidCompilation_ReturnsTrueIfTagHelperInterfaceCannotBeFound()
    {
        // Arrange
        var references = new[]
        {
            ReferenceUtil.NetLatestSystemRuntime,
        };

        var compilation = CSharpCompilation.Create("Test", references: references);

        // Act
        var result = CompilationTagHelperFeature.IsValidCompilation(compilation);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidCompilation_ReturnsFalseIfSystemStringCannotBeFound()
    {
        // Arrange
        var references = new[]
        {
            ReferenceUtil.AspNetLatestRazor,
        };

        var compilation = CSharpCompilation.Create("Test", references: references);

        // Act
        var result = CompilationTagHelperFeature.IsValidCompilation(compilation);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidCompilation_ReturnsTrueIfWellKnownTypesAreFound()
    {
        // Arrange
        var references = new[]
        {
            ReferenceUtil.NetLatestSystemRuntime,
            ReferenceUtil.AspNetLatestRazor,
        };

        var compilation = CSharpCompilation.Create("Test", references: references);

        // Act
        var result = CompilationTagHelperFeature.IsValidCompilation(compilation);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetTagHelpers_DoesNotSetCompilation_IfCompilationIsInvalid()
    {
        // Arrange
        var serviceMock = new Mock<ITagHelperDiscoveryService>();
        serviceMock
            .Setup(service => service.GetTagHelpers(It.IsAny<Compilation>(), It.IsAny<CancellationToken>()))
            .Returns(TagHelperCollection.Empty);

        var engine = RazorProjectEngine.Create(
            builder =>
            {
                builder.ConfigureParserOptions(static builder =>
                {
                    builder.UseRoslynTokenizer = true;
                });

                builder.Features.Add(new DefaultMetadataReferenceFeature());
                builder.Features.Add(new CompilationTagHelperFeature());

                var oldFeature = builder.Features.OfType<ITagHelperDiscoveryService>().Single();
                builder.Features.Replace(oldFeature, serviceMock.Object);
            });

        var feature = engine.Engine.GetFeatures<CompilationTagHelperFeature>().First();

        // Act
        var result = feature.GetTagHelpers();

        // Assert
        Assert.Empty(result);
        serviceMock.Verify(c => c.GetTagHelpers(It.IsAny<Compilation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void GetTagHelpers_SetsCompilation_IfCompilationIsValid()
    {
        // Arrange
        Compilation? compilation = null;
        var serviceMock = new Mock<ITagHelperDiscoveryService>();
        serviceMock
            .Setup(service => service.GetTagHelpers(It.IsAny<Compilation>(), It.IsAny<CancellationToken>()))
            .Callback((Compilation c, CancellationToken ct) => compilation = c)
            .Returns(TagHelperCollection.Empty)
            .Verifiable();

        var references = new[]
        {
            ReferenceUtil.NetLatestSystemRuntime,
            ReferenceUtil.AspNetLatestRazor,
        };

        var engine = RazorProjectEngine.Create(
            builder =>
            {
                builder.ConfigureParserOptions(static builder =>
                {
                    builder.UseRoslynTokenizer = true;
                });

                builder.Features.Add(new DefaultMetadataReferenceFeature { References = references });
                builder.Features.Add(new CompilationTagHelperFeature());

                var oldFeature = builder.Features.OfType<ITagHelperDiscoveryService>().Single();
                builder.Features.Replace(oldFeature, serviceMock.Object);
            });

        var feature = engine.Engine.GetFeatures<CompilationTagHelperFeature>().First();

        // Act
        var result = feature.GetTagHelpers();

        // Assert
        Assert.Empty(result);
        serviceMock.Verify();
        Assert.NotNull(compilation);
    }
}
