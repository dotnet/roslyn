// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class ExplicitInterfaceHelpers
    {
        public static string GetMemberName(
            Binder binder,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifierOpt,
            string name)
        {
            DiagnosticBag discardedDiagnostics = DiagnosticBag.GetInstance();
            TypeSymbol discardedExplicitInterfaceType;
            string discardedAliasOpt;
            string methodName = GetMemberNameAndInterfaceSymbol(binder, explicitInterfaceSpecifierOpt, name, discardedDiagnostics, out discardedExplicitInterfaceType, out discardedAliasOpt);
            discardedDiagnostics.Free();

            return methodName;
        }

        public static string GetMemberNameAndInterfaceSymbol(
            Binder binder,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifierOpt,
            string name,
            DiagnosticBag diagnostics,
            out TypeSymbol explicitInterfaceTypeOpt,
            out string aliasQualifierOpt)
        {
            if (explicitInterfaceSpecifierOpt == null)
            {
                explicitInterfaceTypeOpt = null;
                aliasQualifierOpt = null;
                return name;
            }

            // Avoid checking constraints context when binding explicit interface type since
            // that might result in a recursive attempt to bind the containing class.
            binder = binder.WithAdditionalFlags(BinderFlags.SuppressConstraintChecks | BinderFlags.SuppressObsoleteChecks);

            NameSyntax explicitInterfaceName = explicitInterfaceSpecifierOpt.Name;
            explicitInterfaceTypeOpt = binder.BindType(explicitInterfaceName, diagnostics).Type;
            aliasQualifierOpt = explicitInterfaceName.GetAliasQualifierOpt();
            return GetMemberName(name, explicitInterfaceTypeOpt, aliasQualifierOpt);
        }

        public static string GetMemberName(string name, TypeSymbol explicitInterfaceTypeOpt, string aliasQualifierOpt)
        {
            if ((object)explicitInterfaceTypeOpt == null)
            {
                return name;
            }

            // TODO: Revisit how explicit interface implementations are named.
            // CONSIDER (vladres): we should never generate identical names for different methods.

            string interfaceName = explicitInterfaceTypeOpt.ToDisplayString(SymbolDisplayFormat.ExplicitInterfaceImplementationFormat);

            PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
            StringBuilder builder = pooled.Builder;

            if (!string.IsNullOrEmpty(aliasQualifierOpt))
            {
                builder.Append(aliasQualifierOpt);
                builder.Append("::");
            }

            foreach (char ch in interfaceName)
            {
                // trim spaces to match metadata name (more closely - could still be truncated)
                if (ch != ' ')
                {
                    builder.Append(ch);
                }
            }

            builder.Append(".");
            builder.Append(name);

            return pooled.ToStringAndFree();
        }

        public static string GetMethodNameWithoutInterfaceName(this MethodSymbol method)
        {
            if (method.MethodKind != MethodKind.ExplicitInterfaceImplementation)
            {
                return method.Name;
            }

            return GetMemberNameWithoutInterfaceName(method.Name);
        }

        public static string GetMemberNameWithoutInterfaceName(string fullName)
        {
            var idx = fullName.LastIndexOf('.');
            Debug.Assert(idx < fullName.Length);
            return (idx > 0) ? fullName.Substring(idx + 1) : fullName; //don't consider leading dots
        }

        public static ImmutableArray<T> SubstituteExplicitInterfaceImplementations<T>(ImmutableArray<T> unsubstitutedExplicitInterfaceImplementations, TypeMap map) where T : Symbol
        {
            var builder = ArrayBuilder<T>.GetInstance();
            foreach (var unsubstitutedPropertyImplemented in unsubstitutedExplicitInterfaceImplementations)
            {
                var unsubstitutedInterfaceType = unsubstitutedPropertyImplemented.ContainingType;
                Debug.Assert((object)unsubstitutedInterfaceType != null);
                var explicitInterfaceType = map.SubstituteNamedType(unsubstitutedInterfaceType);
                Debug.Assert((object)explicitInterfaceType != null);
                var name = unsubstitutedPropertyImplemented.Name; //should already be unqualified

                T substitutedMemberImplemented = null;
                foreach (var candidateMember in explicitInterfaceType.GetMembers(name))
                {
                    if (candidateMember.OriginalDefinition == unsubstitutedPropertyImplemented.OriginalDefinition)
                    {
                        substitutedMemberImplemented = (T)candidateMember;
                        break;
                    }
                }
                Debug.Assert((object)substitutedMemberImplemented != null); //if it was an explicit implementation before the substitution, it should still be after
                builder.Add(substitutedMemberImplemented);
            }

            return builder.ToImmutableAndFree();
        }

        internal static MethodSymbol FindExplicitlyImplementedMethod(
            this MethodSymbol implementingMethod,
            TypeSymbol explicitInterfaceType,
            string interfaceMethodName,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifierSyntax,
            DiagnosticBag diagnostics)
        {
            return (MethodSymbol)FindExplicitlyImplementedMember(implementingMethod, explicitInterfaceType, interfaceMethodName, explicitInterfaceSpecifierSyntax, diagnostics);
        }

        internal static PropertySymbol FindExplicitlyImplementedProperty(
            this PropertySymbol implementingProperty,
            TypeSymbol explicitInterfaceType,
            string interfacePropertyName,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifierSyntax,
            DiagnosticBag diagnostics)
        {
            return (PropertySymbol)FindExplicitlyImplementedMember(implementingProperty, explicitInterfaceType, interfacePropertyName, explicitInterfaceSpecifierSyntax, diagnostics);
        }

        internal static EventSymbol FindExplicitlyImplementedEvent(
            this EventSymbol implementingEvent,
            TypeSymbol explicitInterfaceType,
            string interfaceEventName,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifierSyntax,
            DiagnosticBag diagnostics)
        {
            return (EventSymbol)FindExplicitlyImplementedMember(implementingEvent, explicitInterfaceType, interfaceEventName, explicitInterfaceSpecifierSyntax, diagnostics);
        }

        private static Symbol FindExplicitlyImplementedMember(
            Symbol implementingMember,
            TypeSymbol explicitInterfaceType,
            string interfaceMemberName,
            ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifierSyntax,
            DiagnosticBag diagnostics)
        {
            if ((object)explicitInterfaceType == null)
            {
                return null;
            }

            var memberLocation = implementingMember.Locations[0];
            var containingType = implementingMember.ContainingType;

            switch (containingType.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Interface:
                    break;

                default:
                    diagnostics.Add(ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct, memberLocation, implementingMember);
                    return null;
            }

            if (!explicitInterfaceType.IsInterfaceType())
            {
                //we'd like to highlight just the type part of the name
                var explicitInterfaceSyntax = explicitInterfaceSpecifierSyntax.Name;
                var location = new SourceLocation(explicitInterfaceSyntax);

                diagnostics.Add(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, location, explicitInterfaceType);
                return null;
            }

            var explicitInterfaceNamedType = (NamedTypeSymbol)explicitInterfaceType;

            // 13.4.1: "For an explicit interface member implementation to be valid, the class or struct must name an
            // interface in its base class list that contains a member ..."
            MultiDictionary<NamedTypeSymbol, NamedTypeSymbol>.ValueSet set = containingType.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics[explicitInterfaceNamedType];
            int setCount = set.Count;
            if (setCount == 0 || !set.Contains(explicitInterfaceNamedType, TypeSymbol.EqualsObliviousNullableModifierMatchesAny))
            {
                //we'd like to highlight just the type part of the name
                var explicitInterfaceSyntax = explicitInterfaceSpecifierSyntax.Name;
                var location = new SourceLocation(explicitInterfaceSyntax);

                if (setCount > 0 && set.Contains(explicitInterfaceNamedType, TypeSymbol.EqualsIgnoringNullableComparer))
                {
                    diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInExplicitlyImplementedInterface, location);
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_ClassDoesntImplementInterface, location, implementingMember, explicitInterfaceNamedType);
                }

                //do a lookup anyway
            }

            var hasParamsParam = implementingMember.HasParamsParameter();

            // Setting this flag to true does not imply that an interface member has been successfully implemented.
            // It just indicates that a corresponding interface member has been found (there may still be errors).
            var foundMatchingMember = false;

            Symbol implementedMember = null;

            foreach (Symbol interfaceMember in explicitInterfaceNamedType.GetMembers(interfaceMemberName))
            {
                // At this point, we know that explicitInterfaceNamedType is an interface.
                // However, metadata interface members can be static - we ignore them, as does Dev10.
                if (interfaceMember.Kind != implementingMember.Kind || !interfaceMember.IsImplementableInterfaceMember())
                {
                    continue;
                }

                if (MemberSignatureComparer.ExplicitImplementationComparer.Equals(implementingMember, interfaceMember))
                {
                    foundMatchingMember = true;
                    // Cannot implement accessor directly unless
                    // the accessor is from an indexed property.
                    if (interfaceMember.IsAccessor() && !((MethodSymbol)interfaceMember).IsIndexedPropertyAccessor())
                    {
                        diagnostics.Add(ErrorCode.ERR_ExplicitMethodImplAccessor, memberLocation, implementingMember, interfaceMember);
                    }
                    else
                    {
                        if (interfaceMember.MustCallMethodsDirectly())
                        {
                            diagnostics.Add(ErrorCode.ERR_BogusExplicitImpl, memberLocation, implementingMember, interfaceMember);
                        }
                        else if (hasParamsParam && !interfaceMember.HasParamsParameter())
                        {
                            // Note: no error for !hasParamsParam && interfaceMethod.HasParamsParameter()
                            // Still counts as an implementation.
                            diagnostics.Add(ErrorCode.ERR_ExplicitImplParams, memberLocation, implementingMember, interfaceMember);
                        }

                        implementedMember = interfaceMember;
                        break;
                    }
                }
            }

            if (!foundMatchingMember)
            {
                // CONSIDER: we may wish to suppress this error in the event that another error
                // has been reported about the signature.
                diagnostics.Add(ErrorCode.ERR_InterfaceMemberNotFound, memberLocation, implementingMember);
            }

            // Make sure implemented member is accessible
            if ((object)implementedMember != null)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;

                if (!AccessCheck.IsSymbolAccessible(implementedMember, implementingMember.ContainingType, ref useSiteDiagnostics, throughTypeOpt: null))
                {
                    diagnostics.Add(ErrorCode.ERR_BadAccess, memberLocation, implementedMember);
                }
                else
                {
                    switch (implementedMember.Kind)
                    {
                        case SymbolKind.Property:
                            var propertySymbol = (PropertySymbol)implementedMember;
                            checkAccessorIsAccessibleIfImplementable(propertySymbol.GetMethod);
                            checkAccessorIsAccessibleIfImplementable(propertySymbol.SetMethod);
                            break;

                        case SymbolKind.Event:
                            var eventSymbol = (EventSymbol)implementedMember;
                            checkAccessorIsAccessibleIfImplementable(eventSymbol.AddMethod);
                            checkAccessorIsAccessibleIfImplementable(eventSymbol.RemoveMethod);
                            break;
                    }

                    void checkAccessorIsAccessibleIfImplementable(MethodSymbol accessor)
                    {
                        if (accessor.IsImplementable() &&
                            !AccessCheck.IsSymbolAccessible(accessor, implementingMember.ContainingType, ref useSiteDiagnostics, throughTypeOpt: null))
                        {
                            diagnostics.Add(ErrorCode.ERR_BadAccess, memberLocation, accessor);
                        }
                    }
                }

                diagnostics.Add(memberLocation, useSiteDiagnostics);
            }

            return implementedMember;
        }

        internal static void FindExplicitlyImplementedMemberVerification(
            this Symbol implementingMember,
            Symbol implementedMember,
            DiagnosticBag diagnostics)
        {
            if ((object)implementedMember == null)
            {
                return;
            }

            if (implementingMember.ContainsTupleNames() && MemberSignatureComparer.ConsideringTupleNamesCreatesDifference(implementingMember, implementedMember))
            {
                // it is ok to explicitly implement with no tuple names, for compatibility with C# 6, but otherwise names should match
                var memberLocation = implementingMember.Locations[0];
                diagnostics.Add(ErrorCode.ERR_ImplBadTupleNames, memberLocation, implementingMember, implementedMember);
            }

            // In constructed types, it is possible that two method signatures could differ by only ref/out
            // after substitution.  We look for this as part of explicit implementation because, if someone
            // tried to implement the ambiguous interface implicitly, we would separately raise an error about
            // the implicit implementation methods differing by only ref/out.
            FindExplicitImplementationCollisions(implementingMember, implementedMember, diagnostics);
        }

        /// <summary>
        /// Given a member, look for other members contained in the same type with signatures that will
        /// not be distinguishable by the runtime.
        /// </summary>
        private static void FindExplicitImplementationCollisions(Symbol implementingMember, Symbol implementedMember, DiagnosticBag diagnostics)
        {
            if ((object)implementedMember == null)
            {
                return;
            }

            NamedTypeSymbol explicitInterfaceType = implementedMember.ContainingType;
            bool explicitInterfaceTypeIsDefinition = explicitInterfaceType.IsDefinition; //no runtime ref/out ambiguities if this is true

            foreach (Symbol collisionCandidateMember in explicitInterfaceType.GetMembers(implementedMember.Name))
            {
                if (collisionCandidateMember.Kind == implementingMember.Kind && implementedMember != collisionCandidateMember)
                {
                    // NOTE: we are more precise than Dev10 - we will not generate a diagnostic if the return types differ 
                    // because that is enough to distinguish them in the runtime.
                    if (!explicitInterfaceTypeIsDefinition && MemberSignatureComparer.RuntimeSignatureComparer.Equals(implementedMember, collisionCandidateMember))
                    {
                        bool foundMismatchedRefKind = false;
                        ImmutableArray<ParameterSymbol> implementedMemberParameters = implementedMember.GetParameters();
                        ImmutableArray<ParameterSymbol> collisionCandidateParameters = collisionCandidateMember.GetParameters();
                        int numParams = implementedMemberParameters.Length;
                        for (int i = 0; i < numParams; i++)
                        {
                            if (implementedMemberParameters[i].RefKind != collisionCandidateParameters[i].RefKind)
                            {
                                foundMismatchedRefKind = true;
                                break;
                            }
                        }

                        if (foundMismatchedRefKind)
                        {
                            diagnostics.Add(ErrorCode.ERR_ExplicitImplCollisionOnRefOut, explicitInterfaceType.Locations[0], explicitInterfaceType, implementedMember);
                        }
                        else
                        {
                            //UNDONE: related locations for conflicting members - keep iterating to find others?
                            diagnostics.Add(ErrorCode.WRN_ExplicitImplCollision, implementingMember.Locations[0], implementingMember);
                        }
                        break;
                    }
                    else
                    {
                        if (MemberSignatureComparer.ExplicitImplementationComparer.Equals(implementedMember, collisionCandidateMember))
                        {
                            // NOTE: this is different from the same error code above.  Above, the diagnostic means that
                            // the runtime behavior is ambiguous because the runtime cannot distinguish between two or
                            // more interface members.  This diagnostic means that *C#* cannot distinguish between two
                            // or more interface members (because of custom modifiers).
                            diagnostics.Add(ErrorCode.WRN_ExplicitImplCollision, implementingMember.Locations[0], implementingMember);
                        }
                    }
                }
            }
        }
    }
}
