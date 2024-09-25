// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

internal static class AttributeValidation
{
    internal static void AssertReferencedIsUnmanagedAttribute(Accessibility accessibility, TypeParameterSymbol typeParameter, string assemblyName)
    {
        var attributes = ((PEModuleSymbol)typeParameter.ContainingModule).GetCustomAttributesForToken(((PETypeParameterSymbol)typeParameter).Handle);
        NamedTypeSymbol attributeType = attributes.Single().AttributeClass;

        Assert.Equal("IsUnmanagedAttribute", attributeType.Name);
        Assert.Equal(assemblyName, attributeType.ContainingAssembly.Name);
        Assert.Equal(accessibility, attributeType.DeclaredAccessibility);

        switch (accessibility)
        {
            case Accessibility.Internal:
                {
                    var isUnmanagedTypeAttributes = attributeType.GetAttributes().OrderBy(attribute => attribute.AttributeClass.Name).ToArray();
                    Assert.Equal(2, isUnmanagedTypeAttributes.Length);

                    Assert.Equal(WellKnownTypes.GetMetadataName(WellKnownType.System_Runtime_CompilerServices_CompilerGeneratedAttribute), isUnmanagedTypeAttributes[0].AttributeClass.ToDisplayString());
                    Assert.Equal(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName, isUnmanagedTypeAttributes[1].AttributeClass.ToDisplayString());
                    break;
                }

            case Accessibility.Public:
                {
                    var refSafetyRulesAttribute = attributeType.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.RefSafetyRulesAttribute.FullName);
                    var embeddedAttribute = attributeType.ContainingAssembly.GetTypeByMetadataName(AttributeDescription.CodeAnalysisEmbeddedAttribute.FullName);
                    Assert.Equal(refSafetyRulesAttribute is null, embeddedAttribute is null);
                    break;
                }

            default:
                throw ExceptionUtilities.UnexpectedValue(accessibility);
        }

    }
}
