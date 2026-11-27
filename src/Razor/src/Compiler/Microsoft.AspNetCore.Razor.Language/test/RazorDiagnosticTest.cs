// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorDiagnosticTest
{
    [Fact]
    public void Create_WithDescriptor_CreatesDefaultRazorDiagnostic()
    {
        // Arrange
        var descriptor = new RazorDiagnosticDescriptor("RZ0001", "a", RazorDiagnosticSeverity.Error);
        var span = new SourceSpan("test.cs", 15, 1, 8, 5);

        // Act
        var diagnostic = RazorDiagnostic.Create(descriptor, span);

        // Assert
        Assert.Equal("RZ0001", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(span, diagnostic.Span);
    }

    [Fact]
    public void Create_WithDescriptor_AndArgs_CreatesDefaultRazorDiagnostic()
    {
        // Arrange
        var descriptor = new RazorDiagnosticDescriptor("RZ0001", "a", RazorDiagnosticSeverity.Error);
        var span = new SourceSpan("test.cs", 15, 1, 8, 5);

        // Act
        var diagnostic = RazorDiagnostic.Create(descriptor, span, "Hello", "World");

        // Assert
        Assert.Equal("RZ0001", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(span, diagnostic.Span);
    }

    [Fact]
    public void DefaultRazorDiagnostic_GetMessage_WithArgs()
    {
        // Arrange
        var descriptor = new RazorDiagnosticDescriptor("RZ0000", "this is an {0}", RazorDiagnosticSeverity.Error);
        var span = new SourceSpan("test.cs", 15, 1, 8, 5);

        var diagnostic = RazorDiagnostic.Create(descriptor, span, "error");

        // Act
        var result = diagnostic.GetMessage(CultureInfo.CurrentCulture);

        // Assert
        Assert.Equal("this is an error", result);
    }

    [Fact]
    public void DefaultRazorDiagnostic_GetMessage_WithArgs_FormatProvider()
    {
        // Arrange
        var descriptor = new RazorDiagnosticDescriptor("RZ0000", "this is an {0}", RazorDiagnosticSeverity.Error);
        var span = new SourceSpan("test.cs", 15, 1, 8, 5);

        var diagnostic = RazorDiagnostic.Create(descriptor, span, 1.3m);

        // Act
        var result = diagnostic.GetMessage(new CultureInfo("fr-FR"));

        // Assert
        Assert.Equal("this is an 1,3", result);
    }


    [Fact]
    public void DefaultRazorDiagnostic_ToString()
    {
        // Arrange
        var descriptor = new RazorDiagnosticDescriptor("RZ0000", "this is an error", RazorDiagnosticSeverity.Error);
        var span = new SourceSpan("test.cs", 15, 1, 8, 5);

        var diagnostic = RazorDiagnostic.Create(descriptor, span);

        // Act
        var result = diagnostic.ToString();

        // Assert
        Assert.Equal("test.cs(2,9): Error RZ0000: this is an error", result);
    }

    [Fact]
    public void DefaultRazorDiagnostic_ToString_FormatProvider()
    {
        // Arrange
        var descriptor = new RazorDiagnosticDescriptor("RZ0000", "this is an {0}", RazorDiagnosticSeverity.Error);
        var span = new SourceSpan("test.cs", 15, 1, 8, 5);

        var diagnostic = RazorDiagnostic.Create(descriptor, span, 1.3m);

        // Act
        var result = ((IFormattable)diagnostic).ToString("ignored", new CultureInfo("fr-FR"));

        // Assert
        Assert.Equal("test.cs(2,9): Error RZ0000: this is an 1,3", result);
    }

    [Fact]
    public void DefaultRazorDiagnostic_Equals()
    {
        // Arrange
        var descriptor = new RazorDiagnosticDescriptor("RZ0000", "this is an {0}", RazorDiagnosticSeverity.Error);
        var span = new SourceSpan("test.cs", 15, 1, 8, 5);

        var diagnostic1 = RazorDiagnostic.Create(descriptor, span);
        var diagnostic2 = RazorDiagnostic.Create(descriptor, span);

        // Act
        var result = diagnostic1.Equals(diagnostic2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DefaultRazorDiagnostic_NotEquals_DifferentLocation()
    {
        // Arrange
        var descriptor = new RazorDiagnosticDescriptor("RZ0000", "this is an {0}", RazorDiagnosticSeverity.Error);
        var span1 = new SourceSpan("test.cs", 15, 1, 8, 5);
        var span2 = new SourceSpan("test.cs", 15, 1, 8, 3);

        var diagnostic1 = RazorDiagnostic.Create(descriptor, span1);
        var diagnostic2 = RazorDiagnostic.Create(descriptor, span2);

        // Act
        var result = diagnostic1.Equals(diagnostic2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DefaultRazorDiagnostic_NotEquals_DifferentId()
    {
        // Arrange
        var descriptor1 = new RazorDiagnosticDescriptor("RZ0001", "this is an {0}", RazorDiagnosticSeverity.Error);
        var descriptor2 = new RazorDiagnosticDescriptor("RZ0002", "this is an {0}", RazorDiagnosticSeverity.Error);
        var span = new SourceSpan("test.cs", 15, 1, 8, 5);

        var diagnostic1 = RazorDiagnostic.Create(descriptor1, span);
        var diagnostic2 = RazorDiagnostic.Create(descriptor2, span);

        // Act
        var result = diagnostic1.Equals(diagnostic2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DefaultRazorDiagnostic_HashCodesEqual()
    {
        // Arrange
        var descriptor = new RazorDiagnosticDescriptor("RZ0000", "this is an {0}", RazorDiagnosticSeverity.Error);
        var span = new SourceSpan("test.cs", 15, 1, 8, 5);

        var diagnostic1 = RazorDiagnostic.Create(descriptor, span);
        var diagnostic2 = RazorDiagnostic.Create(descriptor, span);

        // Act
        var result = diagnostic1.GetHashCode() == diagnostic2.GetHashCode();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DefaultRazorDiagnostic_HashCodesNotEqual_DifferentLocation()
    {
        // Arrange
        var descriptor = new RazorDiagnosticDescriptor("RZ0000", "this is an {0}", RazorDiagnosticSeverity.Error);
        var span1 = new SourceSpan("test.cs", 15, 1, 8, 5);
        var span2 = new SourceSpan("test.cs", 15, 1, 8, 3);

        var diagnostic1 = RazorDiagnostic.Create(descriptor, span1);
        var diagnostic2 = RazorDiagnostic.Create(descriptor, span2);

        // Act
        var result = diagnostic1.GetHashCode() == diagnostic2.GetHashCode();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DefaultRazorDiagnostic_HashCodesNotEqual_DifferentId()
    {
        // Arrange
        var descriptor1 = new RazorDiagnosticDescriptor("RZ0001", "this is an {0}", RazorDiagnosticSeverity.Error);
        var descriptor2 = new RazorDiagnosticDescriptor("RZ0002", "this is an {0}", RazorDiagnosticSeverity.Error);
        var span = new SourceSpan("test.cs", 15, 1, 8, 5);

        var diagnostic1 = RazorDiagnostic.Create(descriptor1, span);
        var diagnostic2 = RazorDiagnostic.Create(descriptor2, span);

        // Act
        var result = diagnostic1.GetHashCode() == diagnostic2.GetHashCode();

        // Assert
        Assert.False(result);
    }
}
