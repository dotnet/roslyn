// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Lightup;

internal static class LightupHelpers
{
    /// <summary>
    /// Generates a compiled accessor method for a property which cannot be bound at compile time.
    /// </summary>
    /// <typeparam name="T">The compile-time type representing the instance on which the property is defined. This
    /// may be a superclass of the actual type on which the property is declared if the declaring type is not
    /// available at compile time.</typeparam>
    /// <typeparam name="TResult">The compile-type type representing the result of the property. This may be a
    /// superclass of the actual type of the property if the property type is not available at compile
    /// time.</typeparam>
    /// <param name="type">The runtime time on which the property is defined. If this value is <see langword="null"/>,
    /// the runtime time is assumed to not exist, and a fallback accessor returning <paramref name="defaultValue"/> will
    /// be generated.</param>
    /// <param name="propertyName">The name of the property to access.</param>
    /// <param name="defaultValue">The value to return if the property is not available at runtime.</param>
    /// <returns>An accessor method to access the specified runtime property.</returns>
    public static Func<T, TResult> CreatePropertyAccessor<T, TResult>(Type? type, string propertyName, TResult defaultValue)
    {
        if (propertyName is null)
        {
            throw new ArgumentNullException(nameof(propertyName));
        }

        if (type == null)
        {
            return CreateFallbackAccessor<T, TResult>(defaultValue);
        }

        if (!typeof(T).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
        {
            throw new InvalidOperationException($"Type '{type}' is not assignable to type '{typeof(T)}'");
        }

        var property = type.GetTypeInfo().GetDeclaredProperty(propertyName);
        if (property == null)
        {
            return CreateFallbackAccessor<T, TResult>(defaultValue);
        }

        if (property.GetMethod is null)
        {
            throw new InvalidOperationException($"Property '{property}' does not have a get accessor.");
        }

        if (!typeof(TResult).GetTypeInfo().IsAssignableFrom(property.PropertyType.GetTypeInfo()))
        {
            throw new InvalidOperationException($"Property '{property}' produces a value of type '{property.PropertyType}', which is not assignable to type '{typeof(TResult)}'");
        }

        var parameter = Expression.Parameter(typeof(T), GenerateParameterName(typeof(T)));
        var instance =
            type.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
            ? (Expression)parameter
            : Expression.Convert(parameter, type);

        var expression =
            Expression.Lambda<Func<T, TResult>>(
                Expression.Convert(Expression.Call(instance, property.GetMethod), typeof(TResult)),
                parameter);
        return expression.Compile();
    }

    private static string GenerateParameterName(Type parameterType)
    {
        var typeName = parameterType.Name;
        return char.ToLower(typeName[0]) + typeName.Substring(1);
    }

    private static Func<T, TResult> CreateFallbackAccessor<T, TResult>(TResult defaultValue)
    {
        TResult FallbackAccessor(T instance)
        {
            if (instance == null)
            {
                // Unlike an extension method which would throw ArgumentNullException here, the light-up
                // behavior needs to match behavior of the underlying property.
                throw new NullReferenceException();
            }

            return defaultValue;
        }

        return FallbackAccessor;
    }
}
