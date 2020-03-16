// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    internal static class LightupHelpers
    {
        internal static Func<TSyntax, TProperty> CreateSyntaxPropertyAccessor<TSyntax, TProperty>(Type? type, string propertyName)
            where TSyntax : SyntaxNode
            => CreatePropertyAccessor<TSyntax, TProperty>(type, "syntax", propertyName);

        internal static Func<TSymbol, TProperty> CreateSymbolPropertyAccessor<TSymbol, TProperty>(Type? type, string propertyName)
            where TSymbol : ISymbol
            => CreatePropertyAccessor<TSymbol, TProperty>(type, "symbol", propertyName);

        private static Func<T, TProperty> CreatePropertyAccessor<T, TProperty>(Type? type, string parameterName, string propertyName)
        {
            if (type is null)
            {
                return FallbackAccessor;
            }

            VerifyTypeArgument<T>(type);

            var property = type.GetTypeInfo().GetDeclaredProperty(propertyName);
            if (property == null)
            {
                return FallbackAccessor;
            }

            if (!typeof(TProperty).GetTypeInfo().IsAssignableFrom(property.PropertyType.GetTypeInfo()))
            {
                if (property.PropertyType.GetTypeInfo().IsEnum
                    && typeof(TProperty).GetTypeInfo().IsEnum
                    && Enum.GetUnderlyingType(typeof(TProperty)).GetTypeInfo().IsAssignableFrom(Enum.GetUnderlyingType(property.PropertyType).GetTypeInfo()))
                {
                    // Allow this
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            var parameter = Expression.Parameter(typeof(T), parameterName);
            Expression instance =
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
            static TProperty FallbackAccessor(T instance)
            {
                if (instance == null)
                {
                    // Unlike an extension method which would throw ArgumentNullException here, the light-up
                    // behavior needs to match behavior of the underlying property.
                    throw new NullReferenceException();
                }

                return default!;
            }
        }

        internal static Func<TSyntax, TProperty, TSyntax> CreateSyntaxWithPropertyAccessor<TSyntax, TProperty>(Type? type, string propertyName)
            where TSyntax : SyntaxNode
            => CreateWithPropertyAccessor<TSyntax, TProperty>(type, "syntax", propertyName);

        internal static Func<TSymbol, TProperty, TSymbol> CreateSymbolWithPropertyAccessor<TSymbol, TProperty>(Type? type, string propertyName)
            where TSymbol : ISymbol
            => CreateWithPropertyAccessor<TSymbol, TProperty>(type, "symbol", propertyName);

        private static Func<T, TProperty, T> CreateWithPropertyAccessor<T, TProperty>(Type? type, string parameterName, string propertyName)
        {
            if (type is null)
            {
                return FallbackAccessor;
            }

            VerifyTypeArgument<T>(type);

            var property = type.GetTypeInfo().GetDeclaredProperty(propertyName);
            if (property == null)
            {
                return FallbackAccessor;
            }

            if (!typeof(TProperty).GetTypeInfo().IsAssignableFrom(property.PropertyType.GetTypeInfo()))
            {
                if (property.PropertyType.GetTypeInfo().IsEnum
                    && typeof(TProperty).GetTypeInfo().IsEnum
                    && Enum.GetUnderlyingType(typeof(TProperty)).GetTypeInfo().IsAssignableFrom(Enum.GetUnderlyingType(property.PropertyType).GetTypeInfo()))
                {
                    // Allow this
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            var methodInfo = type.GetTypeInfo().GetDeclaredMethods("With" + propertyName)
                .SingleOrDefault(m => !m.IsStatic && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.Equals(property.PropertyType));
            if (methodInfo is null)
            {
                return FallbackAccessor;
            }

            var parameter = Expression.Parameter(typeof(T), parameterName);
            var valueParameter = Expression.Parameter(typeof(TProperty), methodInfo.GetParameters()[0].Name);
            Expression instance =
                type.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
                ? (Expression)parameter
                : Expression.Convert(parameter, type);
            Expression value =
                property.PropertyType.GetTypeInfo().IsAssignableFrom(typeof(TProperty).GetTypeInfo())
                ? (Expression)valueParameter
                : Expression.Convert(valueParameter, property.PropertyType);

            Expression<Func<T, TProperty, T>> expression =
                Expression.Lambda<Func<T, TProperty, T>>(
                    Expression.Call(instance, methodInfo, value),
                    parameter,
                    valueParameter);
            return expression.Compile();

            // Local function
            static T FallbackAccessor(T instance, TProperty newValue)
            {
                if (instance == null)
                {
                    // Unlike an extension method which would throw ArgumentNullException here, the light-up
                    // behavior needs to match behavior of the underlying property.
                    throw new NullReferenceException();
                }

                if (Equals(newValue, default(TProperty)))
                {
                    return instance;
                }

                throw new NotSupportedException();
            }
        }

        internal static Func<T, TArg, TValue> CreateAccessorWithArgument<T, TArg, TValue>(Type? type, string parameterName, Type argumentType, string argumentName, string methodName)
        {
            if (type is null)
            {
                return FallbackAccessor;
            }

            VerifyTypeArgument<T>(type);

            var method = type.GetTypeInfo().GetDeclaredMethod(methodName);
            if (method == null)
            {
                return FallbackAccessor;
            }

            if (!typeof(TValue).GetTypeInfo().IsAssignableFrom(method.ReturnType.GetTypeInfo()))
            {
                if (method.ReturnType.GetTypeInfo().IsEnum
                    && typeof(TValue).GetTypeInfo().IsEnum
                    && Enum.GetUnderlyingType(typeof(TValue)).GetTypeInfo().IsAssignableFrom(Enum.GetUnderlyingType(method.ReturnType).GetTypeInfo()))
                {
                    // Allow this
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            var parameter = Expression.Parameter(typeof(T), parameterName);
            var argument = Expression.Parameter(typeof(TArg), argumentName);
            Expression instance =
                type.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
                ? (Expression)parameter
                : Expression.Convert(parameter, type);
            Expression convertedArgument =
                argumentType.GetTypeInfo().IsAssignableFrom(typeof(TArg).GetTypeInfo())
                ? (Expression)argument
                : Expression.Convert(argument, type);

            Expression result = Expression.Call(instance, method, convertedArgument);
            if (!typeof(TValue).GetTypeInfo().IsAssignableFrom(method.ReturnType.GetTypeInfo()))
            {
                result = Expression.Convert(result, typeof(TValue));
            }

            Expression<Func<T, TArg, TValue>> expression = Expression.Lambda<Func<T, TArg, TValue>>(result, parameter, argument);
            return expression.Compile();

            // Local function
            static TValue FallbackAccessor(T instance, TArg argument)
            {
                if (instance == null)
                {
                    // Unlike an extension method which would throw ArgumentNullException here, the light-up
                    // behavior needs to match behavior of the underlying property.
                    throw new NullReferenceException();
                }

                return default!;
            }
        }

        private static void VerifyTypeArgument<T>(Type type)
        {
            if (!typeof(T).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                throw new InvalidOperationException();
            }
        }
    }
}
