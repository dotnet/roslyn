// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Shared.Lightup
{
    internal static class LightupHelpers
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<SymbolKind, bool>> SupportedWrappers
            = new ConcurrentDictionary<Type, ConcurrentDictionary<SymbolKind, bool>>();

        internal static bool CanWrapSymbol(ISymbol? symbol, [NotNullWhen(true)] Type? underlyingType)
        {
            if (symbol is null)
            {
                // The wrappers support a null instance
                return true;
            }

            if (underlyingType is null)
            {
                // The current runtime doesn't define the target type of the conversion, so no instance of it can exist
                return false;
            }

            var wrappedSyntax = SupportedWrappers.GetOrAdd(underlyingType, _ => new ConcurrentDictionary<SymbolKind, bool>());

            // Avoid creating the delegate if the value already exists
            if (!wrappedSyntax.TryGetValue(symbol.Kind, out var canCast))
            {
                canCast = wrappedSyntax.GetOrAdd(
                    symbol.Kind,
                    kind => underlyingType.GetTypeInfo().IsAssignableFrom(symbol.GetType().GetTypeInfo()));
            }

            return canCast;
        }

        internal static Func<TSymbol, TProperty> CreateSymbolPropertyAccessor<TSymbol, TProperty>(Type? type, string propertyName, TProperty fallbackResult)
            where TSymbol : ISymbol
            => CreatePropertyAccessor<TSymbol, TProperty>(type, "symbol", propertyName, fallbackResult);

        private static Func<T, TProperty> CreatePropertyAccessor<T, TProperty>(Type? type, string parameterName, string propertyName, TProperty fallbackResult)
        {
            if (!TryGetProperty<T, TProperty>(type, propertyName, out var property))
            {
                return instance => FallbackAccessor(instance, fallbackResult);
            }

            var parameter = Expression.Parameter(typeof(T), parameterName);
            var instance =
                type.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
                ? (Expression)parameter
                : Expression.Convert(parameter, type);

            Expression result = Expression.Call(instance, property.GetMethod);
            if (!typeof(TProperty).GetTypeInfo().IsAssignableFrom(property.PropertyType.GetTypeInfo()))
            {
                result = Expression.Convert(result, typeof(TProperty));
            }

            Expression<Func<T, TProperty>> expression = Expression.Lambda<Func<T, TProperty>>(result, parameter);
            return expression.Compile();

            // Local function
            static TProperty FallbackAccessor(T instance, TProperty fallbackResult)
            {
                if (instance is null)
                {
                    // Unlike an extension method which would throw ArgumentNullException here, the light-up
                    // behavior needs to match behavior of the underlying property.
                    throw new NullReferenceException();
                }

                return fallbackResult;
            }
        }

        private static void VerifyTypeArgument<T>(Type type)
        {
            if (!typeof(T).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                throw new InvalidOperationException();
            }
        }

        private static void VerifyResultTypeCompatibility<TValue>(Type resultType)
        {
            if (!typeof(TValue).GetTypeInfo().IsAssignableFrom(resultType.GetTypeInfo()))
            {
                if (resultType.GetTypeInfo().IsEnum
                    && typeof(TValue).GetTypeInfo().IsEnum
                    && Enum.GetUnderlyingType(typeof(TValue)).GetTypeInfo().IsAssignableFrom(Enum.GetUnderlyingType(resultType).GetTypeInfo()))
                {
                    // Allow this
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private static bool TryGetProperty<T, TProperty>([NotNullWhen(true)] Type? type, string propertyName, [NotNullWhen(true)] out PropertyInfo? propertyInfo)
        {
            if (type is null)
            {
                propertyInfo = null;
                return false;
            }

            VerifyTypeArgument<T>(type);

            propertyInfo = type.GetTypeInfo().GetDeclaredProperty(propertyName);
            if (propertyInfo is null)
            {
                return false;
            }

            VerifyResultTypeCompatibility<TProperty>(propertyInfo.PropertyType);
            return true;
        }
    }
}
