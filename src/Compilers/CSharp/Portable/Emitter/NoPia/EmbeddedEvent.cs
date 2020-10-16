﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal sealed class EmbeddedEvent : EmbeddedTypesManager.CommonEmbeddedEvent
    {
        public EmbeddedEvent(EventSymbol underlyingEvent, EmbeddedMethod adder, EmbeddedMethod remover) :
            base(underlyingEvent, adder, remover, null)
        {
        }

        protected override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return UnderlyingEvent.GetCustomAttributesToEmit(moduleBuilder);
        }

        protected override bool IsRuntimeSpecial
        {
            get
            {
                return UnderlyingEvent.HasRuntimeSpecialName;
            }
        }

        protected override bool IsSpecialName
        {
            get
            {
                return UnderlyingEvent.HasSpecialName;
            }
        }

        protected override Cci.ITypeReference GetType(PEModuleBuilder moduleBuilder, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            return moduleBuilder.Translate(UnderlyingEvent.Type, syntaxNodeOpt, diagnostics);
        }

        protected override EmbeddedType ContainingType
        {
            get { return AnAccessor.ContainingType; }
        }

        protected override Cci.TypeMemberVisibility Visibility
        {
            get
            {
                return PEModuleBuilder.MemberVisibility(UnderlyingEvent);
            }
        }

        protected override string Name
        {
            get
            {
                return UnderlyingEvent.MetadataName;
            }
        }

        protected override void EmbedCorrespondingComEventInterfaceMethodInternal(SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics, bool isUsedForComAwareEventBinding)
        {
            // If the event happens to belong to a class with a ComEventInterfaceAttribute, there will also be
            // a paired method living on its source interface. The ComAwareEventInfo class expects to find this 
            // method through reflection. If we embed an event, therefore, we must ensure that the associated source
            // interface method is also included, even if it is not otherwise referenced in the embedding project.
            NamedTypeSymbol underlyingContainingType = ContainingType.UnderlyingNamedType;

            foreach (var attrData in underlyingContainingType.GetAttributes())
            {
                if (attrData.IsTargetAttribute(underlyingContainingType, AttributeDescription.ComEventInterfaceAttribute))
                {
                    bool foundMatch = false;
                    NamedTypeSymbol sourceInterface = null;

                    if (attrData.CommonConstructorArguments.Length == 2)
                    {
                        sourceInterface = attrData.CommonConstructorArguments[0].ValueInternal as NamedTypeSymbol;

                        if ((object)sourceInterface != null)
                        {
                            foundMatch = EmbedMatchingInterfaceMethods(sourceInterface, syntaxNodeOpt, diagnostics);

                            foreach (NamedTypeSymbol source in sourceInterface.AllInterfacesNoUseSiteDiagnostics)
                            {
                                if (EmbedMatchingInterfaceMethods(source, syntaxNodeOpt, diagnostics))
                                {
                                    foundMatch = true;
                                }
                            }
                        }
                    }

                    if (!foundMatch && isUsedForComAwareEventBinding)
                    {
                        if ((object)sourceInterface == null)
                        {
                            // ERRID_SourceInterfaceMustBeInterface/ERR_MissingSourceInterface
                            EmbeddedTypesManager.Error(diagnostics, ErrorCode.ERR_MissingSourceInterface, syntaxNodeOpt, underlyingContainingType, UnderlyingEvent);
                        }
                        else
                        {
                            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                            sourceInterface.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
                            diagnostics.Add(syntaxNodeOpt == null ? NoLocation.Singleton : syntaxNodeOpt.Location, useSiteDiagnostics);

                            // ERRID_EventNoPIANoBackingMember/ERR_MissingMethodOnSourceInterface
                            EmbeddedTypesManager.Error(diagnostics, ErrorCode.ERR_MissingMethodOnSourceInterface, syntaxNodeOpt, sourceInterface, UnderlyingEvent.MetadataName, UnderlyingEvent);
                        }
                    }

                    break;
                }
            }
        }

        private bool EmbedMatchingInterfaceMethods(NamedTypeSymbol sourceInterface, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
            bool foundMatch = false;
            foreach (Symbol m in sourceInterface.GetMembers(UnderlyingEvent.MetadataName))
            {
                if (m.Kind == SymbolKind.Method)
                {
                    TypeManager.EmbedMethodIfNeedTo((MethodSymbol)m, syntaxNodeOpt, diagnostics);
                    foundMatch = true;
                }
            }
            return foundMatch;
        }
    }
}
