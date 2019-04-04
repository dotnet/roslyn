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
            if (setCount == 0 || !set.Contains(explicitInterfaceNamedType))
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

            var interfaceMembers = explicitInterfaceNamedType.GetMembers(interfaceMemberName);


            var (foundMatchingMember, implementedMember) = FindMatchingInterfaceMember(implementingMember, interfaceMembers, diagnostics);

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

        /// <summary>
        /// Finds the member of an interface that an implementing member matches, and warns if there are multiple such members.
        /// </summary>
        private static (bool foundMatchingMember, Symbol implementedMember) FindMatchingInterfaceMember(Symbol implementingMember, ImmutableArray<Symbol> interfaceMembers, DiagnosticBag diagnostics)
        {
            var comparer = MemberSignatureComparer.ExplicitImplementationComparer;
            var foundMatchingMember = false;
            Symbol matchingMember = null;

            foreach (Symbol interfaceMember in interfaceMembers)
            {
                var isRuntimeCollision = false;
                if (matchingMember != null)
                {
                    // In constructed types, it is possible that two method signatures could differ by only ref/out
                    // after substitution. We look for this as part of explicit implementation because, if someone
                    // tried to implement the ambiguous interface implicitly, we would separately raise an error about
                    // the implicit implementation methods differing by only ref/out.
                    // The runtime also sometimes may match an explicit interface implementation to the wrong interface member.
                    // We warn if that can occur. See https://blogs.msdn.microsoft.com/ericlippert/2006/04/06/odious-ambiguous-overloads-part-two/
                    isRuntimeCollision = CheckExplicitImplementationRuntimeCollision(implementingMember, matchingMember, interfaceMember, diagnostics);
                }

                // At this point, we know that explicitInterfaceNamedType is an interface.
                // However, metadata interface members can be static - we ignore them, as does Dev10.
                if (interfaceMember.Kind != implementingMember.Kind || !interfaceMember.IsImplementableInterfaceMember())
                {
                    continue;
                }

                if (comparer.Equals(implementingMember, interfaceMember))
                {
                    foundMatchingMember = true;
                    // Cannot implement accessor directly unless
                    // the accessor is from an indexed property.
                    if (interfaceMember.IsAccessor() && !((MethodSymbol)interfaceMember).IsIndexedPropertyAccessor())
                    {
                        diagnostics.Add(ErrorCode.ERR_ExplicitMethodImplAccessor, implementingMember.Locations[0], implementingMember, interfaceMember);
                    }
                    else
                    {
                        if (interfaceMember.MustCallMethodsDirectly())
                        {
                            diagnostics.Add(ErrorCode.ERR_BogusExplicitImpl, implementingMember.Locations[0], implementingMember, interfaceMember);
                        }
                        else if (implementingMember.HasParamsParameter() && !interfaceMember.HasParamsParameter())
                        {
                            // Note: no error for !hasParamsParam && interfaceMethod.HasParamsParameter()
                            // Still counts as an implementation.
                            diagnostics.Add(ErrorCode.ERR_ExplicitImplParams, implementingMember.Locations[0], implementingMember, interfaceMember);
                        }

                        if (matchingMember != null)
                        {
                            // Warn that the correct matching interface member is ambiguous to the compiler.
                            // To avoid duplicated diagnostics, we don't report this if we've already reported that it's ambiguous to the runtime.
                            if (!isRuntimeCollision)
                            {
                                diagnostics.Add(ErrorCode.WRN_ExplicitImplCollision, implementingMember.Locations[0], implementingMember);
                            }
                        }
                        else
                        {
                            matchingMember = interfaceMember;
                        }
                    }
                }
            }

            // Above we warned about runtime collisions for members that appeared in the interfaceMembers list after the matchingMember was found.
            // Now we warn about runtime collisions for members that appear in the interfaceMembers list before the matchingMember.
            // We have to do this in two stages because we can't check this till we know the matchingMember,
            // but have to know if we report a runtime collision before we report a compiler collision to avoid duplicate diagnostics.
            if (matchingMember != null)
            {
                foreach (Symbol interfaceMember in interfaceMembers)
                {
                    if (interfaceMember == matchingMember)
                        break;

                    // In constructed types, it is possible that two method signatures could differ by only ref/out
                    // after substitution. We look for this as part of explicit implementation because, if someone
                    // tried to implement the ambiguous interface implicitly, we would separately raise an error about
                    // the implicit implementation methods differing by only ref/out.
                    // The runtime also sometimes may match an explicit interface implementation to the wrong interface member.
                    // We warn if that can occur. See https://blogs.msdn.microsoft.com/ericlippert/2006/04/06/odious-ambiguous-overloads-part-two/
                    CheckExplicitImplementationRuntimeCollision(implementingMember, matchingMember, interfaceMember, diagnostics);
                }
            }

            return (foundMatchingMember, matchingMember);
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
        }

        /// <summary>
        /// Given two members on an interface, check if they will not be distinguishable by the runtime.
        /// Returns true if they won't be distinguished, false otherwise.
        /// </summary>
        private static bool CheckExplicitImplementationRuntimeCollision(Symbol implementingMember, Symbol implementedMember, Symbol collisionCandidateMember, DiagnosticBag diagnostics)
        {
            Debug.Assert(implementedMember != null);
            Debug.Assert(collisionCandidateMember != null);
            Debug.Assert(implementedMember != collisionCandidateMember);

            NamedTypeSymbol explicitInterfaceType = implementedMember.ContainingType;

            //no runtime ref/out ambiguities if this is true
            if (explicitInterfaceType.IsDefinition)
                return false;

            if (collisionCandidateMember.Kind != implementingMember.Kind)
                return false;

            // NOTE: we are more precise than Dev10 - we will not generate a diagnostic if the return types differ 
            // because that is enough to distinguish them in the runtime.
            if (!MemberSignatureComparer.RuntimeSignatureComparer.Equals(implementedMember, collisionCandidateMember))
                return false;

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

            return true;
        }
    }
}
