// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    internal static class LightupHelpers
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<OperationKind, bool>> s_supportedOperationWrappers = new();

        internal static Func<TOperation, TProperty> CreateOperationPropertyAccessor<TOperation, TProperty>(Type? type, string propertyName, TProperty fallbackResult)
            where TOperation : IOperation
            => CreatePropertyAccessor<TOperation, TProperty>(type, "operation", propertyName, fallbackResult);

        internal static Func<TSyntax, TProperty> CreateSyntaxPropertyAccessor<TSyntax, TProperty>(Type? type, string propertyName, TProperty fallbackResult)
            where TSyntax : SyntaxNode
            => CreatePropertyAccessor<TSyntax, TProperty>(type, "syntax", propertyName, fallbackResult);

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
            Expression instance =
                type.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
                ? parameter
                : Expression.Convert(parameter, type);

            Expression result = Expression.Call(instance, property.GetMethod!);
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

        internal static Func<TSyntax, TProperty, TSyntax> CreateSyntaxWithPropertyAccessor<TSyntax, TProperty>(Type? type, string propertyName, TProperty fallbackResult)
            where TSyntax : SyntaxNode
            => CreateWithPropertyAccessor<TSyntax, TProperty>(type, "syntax", propertyName, fallbackResult);

        internal static Func<TSymbol, TProperty, TSymbol> CreateSymbolWithPropertyAccessor<TSymbol, TProperty>(Type? type, string propertyName, TProperty fallbackResult)
            where TSymbol : ISymbol
            => CreateWithPropertyAccessor<TSymbol, TProperty>(type, "symbol", propertyName, fallbackResult);

        private static Func<T, TProperty, T> CreateWithPropertyAccessor<T, TProperty>(Type? type, string parameterName, string propertyName, TProperty fallbackResult)
        {
            if (!TryGetProperty<T, TProperty>(type, propertyName, out var property))
            {
                return (instance, value) => FallbackAccessor(instance, value, fallbackResult);
            }

            var methodInfo = type.GetTypeInfo().GetDeclaredMethods("With" + propertyName)
                .SingleOrDefault(m => !m.IsStatic && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.Equals(property.PropertyType));
            if (methodInfo is null)
            {
                return (instance, value) => FallbackAccessor(instance, value, fallbackResult);
            }

            var parameter = Expression.Parameter(typeof(T), parameterName);
            var valueParameter = Expression.Parameter(typeof(TProperty), methodInfo.GetParameters()[0].Name);
            Expression instance =
                type.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
                ? parameter
                : Expression.Convert(parameter, type);
            Expression value =
                property.PropertyType.GetTypeInfo().IsAssignableFrom(typeof(TProperty).GetTypeInfo())
                ? valueParameter
                : Expression.Convert(valueParameter, property.PropertyType);

            Expression<Func<T, TProperty, T>> expression =
                Expression.Lambda<Func<T, TProperty, T>>(
                    Expression.Call(instance, methodInfo, value),
                    parameter,
                    valueParameter);
            return expression.Compile();

            // Local function
            static T FallbackAccessor(T instance, TProperty newValue, TProperty fallbackResult)
            {
                if (instance is null)
                {
                    // Unlike an extension method which would throw ArgumentNullException here, the light-up
                    // behavior needs to match behavior of the underlying property.
                    throw new NullReferenceException();
                }

                if (Equals(newValue, fallbackResult))
                {
                    return instance;
                }

                throw new NotSupportedException();
            }
        }

        internal static Func<T, TArg, TValue> CreateAccessorWithArgument<T, TArg, TValue>(Type? type, string parameterName, Type argumentType, string argumentName, string methodName, TValue fallbackResult)
        {
            if (!TryGetMethod<T, TValue>(type, methodName, out var method))
            {
                return (instance, _) => FallbackAccessor(instance, fallbackResult);
            }

            var parameter = Expression.Parameter(typeof(T), parameterName);
            var argument = Expression.Parameter(typeof(TArg), argumentName);
            Expression instance =
                type.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
                ? parameter
                : Expression.Convert(parameter, type);
            Expression convertedArgument =
                argumentType.GetTypeInfo().IsAssignableFrom(typeof(TArg).GetTypeInfo())
                ? argument
                : Expression.Convert(argument, type);

            Expression result = Expression.Call(instance, method, convertedArgument);
            if (!typeof(TValue).GetTypeInfo().IsAssignableFrom(method.ReturnType.GetTypeInfo()))
            {
                result = Expression.Convert(result, typeof(TValue));
            }

            Expression<Func<T, TArg, TValue>> expression = Expression.Lambda<Func<T, TArg, TValue>>(result, parameter, argument);
            return expression.Compile();

            // Local function
            static TValue FallbackAccessor(T instance, TValue fallbackResult)
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

        private static bool TryGetMethod<T, TReturn>([NotNullWhen(true)] Type? type, string methodName, [NotNullWhen(true)] out MethodInfo? methodInfo)
        {
            if (type is null)
            {
                methodInfo = null;
                return false;
            }

            VerifyTypeArgument<T>(type);

            methodInfo = type.GetTypeInfo().GetDeclaredMethod(methodName);
            if (methodInfo is null)
            {
                return false;
            }

            VerifyResultTypeCompatibility<TReturn>(methodInfo.ReturnType);
            return true;
        }
    }
}
