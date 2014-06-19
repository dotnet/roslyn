using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    internal abstract partial class Binder
    {
        /// <summary>
        /// Verify if the attribute type can be applied to given owner symbol.
        /// Generate a diagnostic if it cannot be applied
        /// </summary>
        /// <param name="ownerSymbol">Symbol on which the attribute is applied</param>
        /// <param name="attributeType">Attribute class for the attribute</param>
        /// <param name="node">Syntax node for attribute specification</param>
        /// <param name="attributeLocation">Attribute target specifier location</param>
        /// <param name="diagnostics">Diagnostics</param>
        /// <returns>Whether attribute specification is allowed for the given symbol</returns>
        internal bool VerifyAttributeUsageTarget(Symbol ownerSymbol, NamedTypeSymbol attributeType, AttributeSyntax node, AttributeLocation attributeLocation, DiagnosticBag diagnostics)
        {
            var attributeUsageInfo = attributeType.AttributeUsageInfo(Compilation);
            if (attributeUsageInfo != null)
            {
                AttributeTargets attributeTarget;
                if (attributeLocation == AttributeLocation.Return)
                {
                    // attribute on return type
                    attributeTarget = AttributeTargets.ReturnValue;
                }
                else
                {
                    attributeTarget = ownerSymbol.GetAttributeTarget();
                }
                
                if ((attributeTarget & attributeUsageInfo.ValidTargets) != 0)
                {
                    return true;
                }

                // generate error
                Error(diagnostics, ErrorCode.ERR_AttributeOnBadSymbolType, node, GetAttributeNameFromSyntax(node), attributeUsageInfo.GetValidTargetsString());
            }

            return false;
        }

        #region Process well-known attributes

        // Process the given well known attribute applied to the given ownerSymbol
        private void ProcessWellKnownAttribute(SourceAttributeData attribute, Symbol ownerSymbol, AttributeSyntax node, DiagnosticBag diagnostics, bool validTarget)
        {
            var wellKnownAttribute = attribute.WellKnownAttribute;
            Debug.Assert(WellKnownTypes.IsWellKnownAttribute(wellKnownAttribute));

            switch (wellKnownAttribute)
            {
                case WellKnownType.System_AttributeUsageAttribute:
                    ProcessAttributeUsageAttribute(attribute, ownerSymbol, node, diagnostics);
                    return;

                case WellKnownType.System_Runtime_CompilerServices_ExtensionAttribute:
                    ProcessExtensionAttribute(attribute, ownerSymbol, node, diagnostics, validTarget);
                    return;

                case WellKnownType.System_Reflection_DefaultMemberAttribute:
                    ProcessDefaultMemberAttribute(ownerSymbol, node, diagnostics, validTarget);
                    return;

                case WellKnownType.System_Runtime_CompilerServices_DateTimeConstantAttribute:
                case WellKnownType.System_Runtime_CompilerServices_DecimalConstantAttribute:
                case WellKnownType.System_Runtime_CompilerServices_InternalsVisibleToAttribute:
                    // TODO semantic analysis of well known attributes
                    return;
            }
        }

        // Process the specified AttributeUsage attribute on the given ownerSymbol
        private void ProcessAttributeUsageAttribute(SourceAttributeData attributeUsageAttribute, Symbol ownerSymbol, AttributeSyntax node, DiagnosticBag diagnostics)
        {
            // Validate first ctor argument for AtributeUsage specification is a valid AttributeTargets enum member
            Debug.Assert(attributeUsageAttribute.AttributeConstructor == GetWellKnownTypeMember(WellKnownMember.System_AttributeUsageAttribute__ctor, diagnostics, node));
            var targetArgument = (int)attributeUsageAttribute.PositionalArguments[0].Value;
            if (!AttributeUsageInfo.IsValidValueForAttributeTargets(targetArgument))
            {
                // invalid attribute target
                Error(diagnostics, ErrorCode.ERR_InvalidAttributeArgument, node.ArgumentList.Arguments[0], GetAttributeNameFromSyntax(node));
            }

            // AttributeUsage can only be specified for attribute classes
            if (ownerSymbol.Kind == SymbolKind.NamedType &&
                !((NamedTypeSymbol)ownerSymbol).IsAttributeType(Compilation))
            {
                Error(diagnostics, ErrorCode.ERR_AttributeUsageOnNonAttributeClass, node, GetAttributeNameFromSyntax(node));
            }
        }

        private void ProcessExtensionAttribute(SourceAttributeData attributeUsageAttribute, Symbol ownerSymbol, AttributeSyntax node, DiagnosticBag diagnostics, bool validTarget)
        {
            if (!validTarget)
            {
                return;
            }

            // [Extension] attribute should not be set explicitly.
            Error(diagnostics, ErrorCode.ERR_ExplicitExtension, node, WellKnownType.System_Runtime_CompilerServices_ExtensionAttribute.GetMetadataName());
        }

        private void ProcessDefaultMemberAttribute(Symbol ownerSymbol, AttributeSyntax node, DiagnosticBag diagnostics, bool validTarget)
        {
            if (!validTarget)
            {
                return;
            }

            NamedTypeSymbol ownerType = (NamedTypeSymbol)ownerSymbol; //cast should succeed since validTarget is true
            if (ownerType.Indexers.Any())
            {
                Error(diagnostics, ErrorCode.ERR_DefaultMemberOnIndexedType, node);
            }
        }

        #endregion
    }
}