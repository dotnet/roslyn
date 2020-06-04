﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// SymbolExtensions for member symbols.
    /// </summary>
    internal static partial class SymbolExtensions
    {
        internal static bool HasParamsParameter(this Symbol member)
        {
            var @params = member.GetParameters();
            return !@params.IsEmpty && @params.Last().IsParams;
        }

        /// <summary>
        /// Get the parameters of a member symbol.  Should be a method, property, or event.
        /// </summary>
        internal static ImmutableArray<ParameterSymbol> GetParameters(this Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)member).Parameters;
                case SymbolKind.Property:
                    return ((PropertySymbol)member).Parameters;
                case SymbolKind.Event:
                    return ImmutableArray<ParameterSymbol>.Empty;
                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }

        /// <summary>
        /// Get the types of the parameters of a member symbol.  Should be a method, property, or event.
        /// </summary>
        internal static ImmutableArray<TypeWithAnnotations> GetParameterTypes(this Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)member).ParameterTypesWithAnnotations;
                case SymbolKind.Property:
                    return ((PropertySymbol)member).ParameterTypesWithAnnotations;
                case SymbolKind.Event:
                    return ImmutableArray<TypeWithAnnotations>.Empty;
                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }

        internal static bool GetIsVararg(this Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)member).IsVararg;
                case SymbolKind.Property:
                case SymbolKind.Event:
                    return false;
                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }

        /// <summary>
        /// Get the ref kinds of the parameters of a member symbol.  Should be a method, property, or event.
        /// </summary>
        internal static ImmutableArray<RefKind> GetParameterRefKinds(this Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)member).ParameterRefKinds;
                case SymbolKind.Property:
                    return ((PropertySymbol)member).ParameterRefKinds;
                case SymbolKind.Event:
                    return default(ImmutableArray<RefKind>);
                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }

        internal static int GetParameterCount(this Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)member).ParameterCount;
                case SymbolKind.Property:
                    return ((PropertySymbol)member).ParameterCount;
                case SymbolKind.Event:
                case SymbolKind.Field:
                    return 0;
                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }

        internal static bool HasUnsafeParameter(this Symbol member)
        {
            foreach (var parameterType in member.GetParameterTypes())
            {
                if (parameterType.Type.IsUnsafe())
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsEventOrPropertyWithImplementableNonPublicAccessor(this Symbol symbol)
        {
            Debug.Assert(symbol.ContainingType.IsInterface);

            switch (symbol.Kind)
            {
                case SymbolKind.Property:
                    var propertySymbol = (PropertySymbol)symbol;
                    return isImplementableAndNotPublic(propertySymbol.GetMethod) || isImplementableAndNotPublic(propertySymbol.SetMethod);

                case SymbolKind.Event:
                    var eventSymbol = (EventSymbol)symbol;
                    return isImplementableAndNotPublic(eventSymbol.AddMethod) || isImplementableAndNotPublic(eventSymbol.RemoveMethod);
            }

            return false;

            bool isImplementableAndNotPublic(MethodSymbol accessor)
            {
                return accessor.IsImplementable() && accessor.DeclaredAccessibility != Accessibility.Public;
            }
        }

        public static bool IsImplementable(this MethodSymbol methodOpt)
        {
            return (object)methodOpt != null && !methodOpt.IsSealed && (methodOpt.IsAbstract || methodOpt.IsVirtual);
        }

        public static bool IsAccessor(this MethodSymbol methodSymbol)
        {
            return (object)methodSymbol.AssociatedSymbol != null;
        }

        public static bool IsAccessor(this Symbol symbol)
        {
            return symbol.Kind == SymbolKind.Method && IsAccessor((MethodSymbol)symbol);
        }

        public static bool IsIndexedPropertyAccessor(this MethodSymbol methodSymbol)
        {
            var propertyOrEvent = methodSymbol.AssociatedSymbol;
            return ((object)propertyOrEvent != null) && propertyOrEvent.IsIndexedProperty();
        }

        public static bool IsOperator(this MethodSymbol methodSymbol)
        {
            return methodSymbol.MethodKind == MethodKind.UserDefinedOperator || methodSymbol.MethodKind == MethodKind.Conversion;
        }

        public static bool IsOperator(this Symbol symbol)
        {
            return symbol.Kind == SymbolKind.Method && IsOperator((MethodSymbol)symbol);
        }

        public static bool IsIndexer(this Symbol symbol)
        {
            return symbol.Kind == SymbolKind.Property && ((PropertySymbol)symbol).IsIndexer;
        }

        public static bool IsIndexedProperty(this Symbol symbol)
        {
            return symbol.Kind == SymbolKind.Property && ((PropertySymbol)symbol).IsIndexedProperty;
        }

        public static bool IsUserDefinedConversion(this Symbol symbol)
        {
            return symbol.Kind == SymbolKind.Method && ((MethodSymbol)symbol).MethodKind == MethodKind.Conversion;
        }

        /// <summary>
        /// Count the number of custom modifiers in/on the return type
        /// and parameters of the specified method.
        /// </summary>
        public static int CustomModifierCount(this MethodSymbol method)
        {
            int count = 0;

            var methodReturnType = method.ReturnTypeWithAnnotations;
            count += methodReturnType.CustomModifiers.Length + method.RefCustomModifiers.Length;
            count += methodReturnType.Type.CustomModifierCount();

            foreach (ParameterSymbol param in method.Parameters)
            {
                var paramType = param.TypeWithAnnotations;
                count += paramType.CustomModifiers.Length + param.RefCustomModifiers.Length;
                count += paramType.Type.CustomModifierCount();
            }

            return count;
        }

        public static int CustomModifierCount(this Symbol m)
        {
            switch (m.Kind)
            {
                case SymbolKind.ArrayType:
                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                case SymbolKind.PointerType:
                case SymbolKind.TypeParameter:
                case SymbolKind.FunctionPointer:
                    return ((TypeSymbol)m).CustomModifierCount();
                case SymbolKind.Event:
                    return ((EventSymbol)m).CustomModifierCount();
                case SymbolKind.Method:
                    return ((MethodSymbol)m).CustomModifierCount();
                case SymbolKind.Property:
                    return ((PropertySymbol)m).CustomModifierCount();
                default:
                    throw ExceptionUtilities.UnexpectedValue(m.Kind);
            }
        }

        public static int CustomModifierCount(this EventSymbol e)
        {
            return e.Type.CustomModifierCount();
        }

        /// <summary>
        /// Count the number of custom modifiers in/on the type
        /// and parameters (for indexers) of the specified property.
        /// </summary>
        public static int CustomModifierCount(this PropertySymbol property)
        {
            int count = 0;

            var type = property.TypeWithAnnotations;
            count += type.CustomModifiers.Length + property.RefCustomModifiers.Length;
            count += type.Type.CustomModifierCount();

            foreach (ParameterSymbol param in property.Parameters)
            {
                var paramType = param.TypeWithAnnotations;
                count += paramType.CustomModifiers.Length + param.RefCustomModifiers.Length;
                count += paramType.Type.CustomModifierCount();
            }

            return count;
        }

        internal static Symbol SymbolAsMember(this Symbol s, NamedTypeSymbol newOwner)
        {
            switch (s.Kind)
            {
                case SymbolKind.Field:
                    return ((FieldSymbol)s).AsMember(newOwner);
                case SymbolKind.Method:
                    return ((MethodSymbol)s).AsMember(newOwner);
                case SymbolKind.NamedType:
                    return ((NamedTypeSymbol)s).AsMember(newOwner);
                case SymbolKind.Property:
                    return ((PropertySymbol)s).AsMember(newOwner);
                case SymbolKind.Event:
                    return ((EventSymbol)s).AsMember(newOwner);
                default:
                    throw ExceptionUtilities.UnexpectedValue(s.Kind);
            }
        }

        /// <summary>
        /// Return the arity of a member.
        /// </summary>
        internal static int GetMemberArity(this Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)symbol).Arity;

                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    return ((NamedTypeSymbol)symbol).Arity;

                default:
                    return 0;
            }
        }

        internal static NamespaceOrTypeSymbol OfMinimalArity(this IEnumerable<NamespaceOrTypeSymbol> symbols)
        {
            NamespaceOrTypeSymbol minAritySymbol = null;
            int minArity = Int32.MaxValue;
            foreach (var symbol in symbols)
            {
                int arity = GetMemberArity(symbol);
                if (arity < minArity)
                {
                    minArity = arity;
                    minAritySymbol = symbol;
                }
            }

            return minAritySymbol;
        }

        internal static ImmutableArray<TypeParameterSymbol> GetMemberTypeParameters(this Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)symbol).TypeParameters;
                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    return ((NamedTypeSymbol)symbol).TypeParameters;
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    return ImmutableArray<TypeParameterSymbol>.Empty;
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        internal static ImmutableArray<TypeSymbol> GetMemberTypeArgumentsNoUseSiteDiagnostics(this Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)symbol).TypeArgumentsWithAnnotations.SelectAsArray(TypeMap.AsTypeSymbol);
                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    return ((NamedTypeSymbol)symbol).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.SelectAsArray(TypeMap.AsTypeSymbol);
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    return ImmutableArray<TypeSymbol>.Empty;
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        internal static bool IsConstructor(this MethodSymbol method)
        {
            switch (method.MethodKind)
            {
                case MethodKind.Constructor:
                case MethodKind.StaticConstructor:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// NOTE: every struct has a public parameterless constructor either used-defined or default one
        /// </summary>
        internal static bool IsParameterlessConstructor(this MethodSymbol method)
        {
            return method.MethodKind == MethodKind.Constructor && method.ParameterCount == 0;
        }

        /// <summary>
        /// default zero-init constructor symbol is added to a struct when it does not define 
        /// its own parameterless public constructor.
        /// We do not emit this constructor and do not call it 
        /// </summary>
        internal static bool IsDefaultValueTypeConstructor(this MethodSymbol method)
        {
            return method.IsImplicitlyDeclared &&
                   method.ContainingType.IsValueType &&
                   method.IsParameterlessConstructor();
        }

        /// <summary>
        /// Indicates whether the method should be emitted.
        /// </summary>
        internal static bool ShouldEmit(this MethodSymbol method)
        {
            // Don't emit the default value type constructor - the runtime handles that
            if (method.IsDefaultValueTypeConstructor())
            {
                return false;
            }

            if (method is SynthesizedStaticConstructor cctor && !cctor.ShouldEmit())
            {
                return false;
            }

            // Don't emit partial methods without an implementation part.
            if (method.IsPartialMethod() && method.PartialImplementationPart is null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// If the event has a AddMethod, return that.  Otherwise check the overridden
        /// event, if any.  Repeat for each overridden event.
        /// </summary>
        /// <remarks>
        /// This method exists to mimic the behavior of GetOwnOrInheritedGetMethod, but it
        /// should only ever look at the overridden event in error scenarios.
        /// </remarks>
        internal static MethodSymbol GetOwnOrInheritedAddMethod(this EventSymbol @event)
        {
            while ((object)@event != null)
            {
                MethodSymbol addMethod = @event.AddMethod;
                if ((object)addMethod != null)
                {
                    return addMethod;
                }

                @event = @event.IsOverride ? @event.OverriddenEvent : null;
            }

            return null;
        }

        /// <summary>
        /// If the event has a RemoveMethod, return that.  Otherwise check the overridden
        /// event, if any.  Repeat for each overridden event.
        /// </summary>
        /// <remarks>
        /// This method exists to mimic the behavior of GetOwnOrInheritedSetMethod, but it
        /// should only ever look at the overridden event in error scenarios.
        /// </remarks>
        internal static MethodSymbol GetOwnOrInheritedRemoveMethod(this EventSymbol @event)
        {
            while ((object)@event != null)
            {
                MethodSymbol removeMethod = @event.RemoveMethod;
                if ((object)removeMethod != null)
                {
                    return removeMethod;
                }

                @event = @event.IsOverride ? @event.OverriddenEvent : null;
            }

            return null;
        }

        internal static bool IsExplicitInterfaceImplementation(this Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)member).IsExplicitInterfaceImplementation;
                case SymbolKind.Property:
                    return ((PropertySymbol)member).IsExplicitInterfaceImplementation;
                case SymbolKind.Event:
                    return ((EventSymbol)member).IsExplicitInterfaceImplementation;
                default:
                    return false;
            }
        }

        internal static bool IsPartialMethod(this Symbol member)
        {
            var sms = member as SourceMemberMethodSymbol;
            return sms?.IsPartial == true;
        }

        internal static bool IsPartialImplementation(this Symbol member)
        {
            var sms = member as SourceOrdinaryMethodSymbol;
            return sms?.IsPartialImplementation == true;
        }

        internal static bool IsPartialDefinition(this Symbol member)
        {
            var sms = member as SourceOrdinaryMethodSymbol;
            return sms?.IsPartialDefinition == true;
        }

        internal static bool ContainsTupleNames(this Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    var method = (MethodSymbol)member;
                    return method.ReturnType.ContainsTupleNames() || method.Parameters.Any(p => p.Type.ContainsTupleNames());
                case SymbolKind.Property:
                    return ((PropertySymbol)member).Type.ContainsTupleNames();
                case SymbolKind.Event:
                    return ((EventSymbol)member).Type.ContainsTupleNames();
                default:
                    // We currently don't need to use this method for fields or locals
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }

        internal static ImmutableArray<Symbol> GetExplicitInterfaceImplementations(this Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)member).ExplicitInterfaceImplementations.Cast<MethodSymbol, Symbol>();
                case SymbolKind.Property:
                    return ((PropertySymbol)member).ExplicitInterfaceImplementations.Cast<PropertySymbol, Symbol>();
                case SymbolKind.Event:
                    return ((EventSymbol)member).ExplicitInterfaceImplementations.Cast<EventSymbol, Symbol>();
                default:
                    return ImmutableArray<Symbol>.Empty;
            }
        }

        internal static Symbol GetOverriddenMember(this Symbol member)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    return ((MethodSymbol)member).OverriddenMethod;
                case SymbolKind.Property:
                    return ((PropertySymbol)member).OverriddenProperty;
                case SymbolKind.Event:
                    return ((EventSymbol)member).OverriddenEvent;
                default:
                    throw ExceptionUtilities.UnexpectedValue(member.Kind);
            }
        }

        internal static Symbol GetLeastOverriddenMember(this Symbol member, NamedTypeSymbol accessingTypeOpt)
        {
            switch (member.Kind)
            {
                case SymbolKind.Method:
                    var method = (MethodSymbol)member;
                    return method.GetConstructedLeastOverriddenMethod(accessingTypeOpt);

                case SymbolKind.Property:
                    var property = (PropertySymbol)member;
                    return property.GetLeastOverriddenProperty(accessingTypeOpt);

                case SymbolKind.Event:
                    var evnt = (EventSymbol)member;
                    return evnt.GetLeastOverriddenEvent(accessingTypeOpt);

                default:
                    return member;
            }
        }

        internal static bool IsFieldOrFieldLikeEvent(this Symbol member, out FieldSymbol field)
        {
            switch (member.Kind)
            {
                case SymbolKind.Field:
                    field = (FieldSymbol)member;
                    return true;
                case SymbolKind.Event:
                    field = ((EventSymbol)member).AssociatedField;
                    return (object)field != null;
                default:
                    field = null;
                    return false;
            }
        }

        internal static string GetMemberCallerName(this Symbol member)
        {
            if (member.Kind == SymbolKind.Method)
            {
                member = ((MethodSymbol)member).AssociatedSymbol ?? member;
            }

            return member.IsIndexer() ? member.MetadataName :
                member.IsExplicitInterfaceImplementation() ? ExplicitInterfaceHelpers.GetMemberNameWithoutInterfaceName(member.Name) :
                member.Name;
        }
    }
}
