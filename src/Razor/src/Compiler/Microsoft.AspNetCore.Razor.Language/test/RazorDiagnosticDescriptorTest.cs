// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public class RazorDiagnosticDescriptorTest
{
    [Fact]
    public void RazorDiagnosticDescriptor_Ctor()
    {
        // Arrange & Act
        var descriptor = new RazorDiagnosticDescriptor("RZ0001", "Hello, World!", RazorDiagnosticSeverity.Error);

        // Assert
        Assert.Equal("RZ0001", descriptor.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, descriptor.Severity);
        Assert.Equal("Hello, World!", descriptor.MessageFormat);
        Assert.Equal(0, descriptor.WarningLevel);
    }

    [Fact]
    public void RazorDiagnosticDescriptor_Ctor_WithWarningLevel()
    {
        // Arrange & Act
        var descriptor = new RazorDiagnosticDescriptor("RZ0001", "Hello, World!", RazorDiagnosticSeverity.Warning, warningLevel: 11);

        // Assert
        Assert.Equal("RZ0001", descriptor.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, descriptor.Severity);
        Assert.Equal("Hello, World!", descriptor.MessageFormat);
        Assert.Equal(11, descriptor.WarningLevel);
    }

    [Fact]
    public void RazorDiagnosticDescriptor_Equals()
    {
        // Arrange
        var descriptor1 = new RazorDiagnosticDescriptor("RZ0001", "a!", RazorDiagnosticSeverity.Error);
        var descriptor2 = new RazorDiagnosticDescriptor("RZ0001", "a!", RazorDiagnosticSeverity.Error);

        // Act
        var result = descriptor1.Equals(descriptor2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RazorDiagnosticDescriptor_NotEquals()
    {
        // Arrange
        var descriptor1 = new RazorDiagnosticDescriptor("RZ0001", "a!", RazorDiagnosticSeverity.Error);
        var descriptor2 = new RazorDiagnosticDescriptor("RZ0002", "b!", RazorDiagnosticSeverity.Error);

        // Act
        var result = descriptor1.Equals(descriptor2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RazorDiagnosticDescriptor_HashCodesEqual()
    {
        // Arrange
        var descriptor1 = new RazorDiagnosticDescriptor("RZ0001", "a!", RazorDiagnosticSeverity.Error);
        var descriptor2 = new RazorDiagnosticDescriptor("RZ0001", "a!", RazorDiagnosticSeverity.Error);

        // Act
        var result = descriptor1.GetHashCode() == descriptor2.GetHashCode();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RazorDiagnosticDescriptor_HashCodesNotEqual()
    {
        // Arrange
        var descriptor1 = new RazorDiagnosticDescriptor("RZ0001", "a!", RazorDiagnosticSeverity.Error);
        var descriptor2 = new RazorDiagnosticDescriptor("RZ0001", "b!", RazorDiagnosticSeverity.Error);

        // Act
        var result = descriptor1.GetHashCode() == descriptor2.GetHashCode();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void NoDuplicateDiagnosticIds()
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var factoryType = typeof(RazorDiagnosticFactory);

        addAllDescriptors(ids, factoryType, typeof(AspNetCore.Razor.Language.RazorDiagnosticDescriptor));
        addAllDescriptors(ids, factoryType, typeof(CodeAnalysis.Razor.RazorDiagnosticFactory));
        addAllDescriptors(ids, factoryType, typeof(AspNetCore.Razor.Language.Components.ComponentDiagnosticFactory));
        addAllDescriptors(ids, factoryType, typeof(AspNetCore.Mvc.Razor.Extensions.RazorExtensionsDiagnosticFactory));

        static void addAllDescriptors(HashSet<string> ids, Type factoryType, Type diagnosticDescriptorType)
        {
            foreach (var field in factoryType.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Where(f => f.FieldType == diagnosticDescriptorType))
            {
                var descriptor = (RazorDiagnosticDescriptor)field.GetValue(null)!;
                if (ids.Contains(descriptor.Id))
                {
                    Assert.Fail($"Duplicate diagnostic id '{descriptor.Id}' found.");
                }

                ids.Add(descriptor.Id);
            }
        }
    }
}
