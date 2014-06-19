using System;
using System.Linq;
using System.Diagnostics;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.MetadataReader;

namespace Roslyn.Compilers.CSharp
{
    internal static class WellKnownAttributes
    {
        // compares by namespace and type name, ignores signatures
        private static bool EarlyDecodeIsTargetAttribute(NamedTypeSymbol attributeType, AttributeSyntax attributeSyntax, AttributeDescription description, bool skipParamCheck = false)
        {
            if (!skipParamCheck)
            {
                int parameterCount = description.GetParameterCount(signatureIndex: 0);
                int argumentCount = (attributeSyntax.ArgumentList != null) ? attributeSyntax.ArgumentList.Arguments.Count : 0;

                if (argumentCount != parameterCount)
                {
                    return false;
                }
            }

            Debug.Assert(!attributeType.IsErrorType());
            string actualNamespaceName = attributeType.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat);
            return actualNamespaceName.Equals(description.Namespace) && attributeType.Name.Equals(description.Name);
        }

        public static bool EarlyDecodeIsOptionalAttribute(NamedTypeSymbol attributeType, AttributeSyntax attributeSyntax)
        {
            return EarlyDecodeIsTargetAttribute(attributeType, attributeSyntax, AttributeDescription.OptionalAttribute);
        }

        public static bool EarlyDecodeIsComImportAttribute(NamedTypeSymbol attributeType, AttributeSyntax attributeSyntax)
        {
            return EarlyDecodeIsTargetAttribute(attributeType, attributeSyntax, AttributeDescription.ComImportAttribute);
        }

        public static bool EarlyDecodeIsConditionalAttribute(NamedTypeSymbol attributeType, AttributeSyntax attributeSyntax)
        {
            return EarlyDecodeIsTargetAttribute(attributeType, attributeSyntax, AttributeDescription.ConditionalAttribute);
        }

        public static bool EarlyDecodeIsObsoleteAttribute(NamedTypeSymbol attributeType, AttributeSyntax attributeSyntax)
        {
            return EarlyDecodeIsTargetAttribute(attributeType, attributeSyntax, AttributeDescription.ObsoleteAttribute, skipParamCheck:true);
        }
    }
}