// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Lightup;

internal static class LightupHelpers
{
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, bool>> s_supportedObjectWrappers = new();

    internal static bool CanWrapObject(object? obj, Type? underlyingType)
    {
        if (obj == null)
        {
            // The wrappers support a null instance
            return true;
        }

        if (underlyingType == null)
        {
            // The current runtime doesn't define the target type of the conversion, so no instance of it can exist
            return false;
        }

        var wrappedObject = s_supportedObjectWrappers.GetOrAdd(underlyingType, static _ => new ConcurrentDictionary<Type, bool>());

        // Avoid creating a delegate and capture class
        if (!wrappedObject.TryGetValue(obj.GetType(), out var canCast))
        {
            canCast = underlyingType.GetTypeInfo().IsAssignableFrom(obj.GetType().GetTypeInfo());
            wrappedObject.TryAdd(obj.GetType(), canCast);
        }

        return canCast;
    }

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

    /// <summary>
    /// Generates a compiled accessor method for a method with a signature compatible with <see cref="Action"/> which
    /// cannot be bound at compile time.
    /// </summary>
    /// <typeparam name="T">The compile-time type representing the instance on which the property is defined. This
    /// may be a superclass of the actual type on which the property is declared if the declaring type is not
    /// available at compile time.</typeparam>
    /// <param name="type">The runtime time on which the property is defined. If this value is <see langword="null"/>,
    /// the runtime time is assumed to not exist, and a fallback action which returns immediately is returned.</param>
    /// <param name="methodName">The name of the method to access.</param>
    /// <returns>An accessor method to access the specified runtime method.</returns>
    public static Action<T> CreateActionAccessor<T>(Type? type, string methodName)
    {
        if (methodName is null)
        {
            throw new ArgumentNullException(nameof(methodName));
        }

        if (type == null)
        {
            return CreateFallbackAction<T>();
        }

        if (!typeof(T).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
        {
            throw new InvalidOperationException($"Type '{type}' is not assignable to type '{typeof(T)}'");
        }

        var method = type.GetTypeInfo().GetDeclaredMethod(methodName);
        if (method == null)
        {
            return CreateFallbackAction<T>();
        }

        if (method.ReturnType != typeof(void))
        {
            throw new InvalidOperationException($"Method '{method}' produces an unexpected value of type '{method.ReturnType}'");
        }

        var parameter = Expression.Parameter(typeof(T), GenerateParameterName(typeof(T)));
        var instance =
            type.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
            ? (Expression)parameter
            : Expression.Convert(parameter, type);

        var expression =
            Expression.Lambda<Action<T>>(
                Expression.Call(instance, method),
                parameter);
        return expression.Compile();
    }

    /// <summary>
    /// Generates a compiled accessor method for a method which cannot be bound at compile time.
    /// </summary>
    /// <typeparam name="T">The compile-time type representing the instance on which the property is defined. This
    /// may be a superclass of the actual type on which the property is declared if the declaring type is not
    /// available at compile time.</typeparam>
    /// <typeparam name="TArg">The compile-time type representing the type of the first argument. This
    /// may be a superclass of the actual type of the argument if the declared type is not available at compile
    /// time.</typeparam>
    /// <param name="type">The runtime time on which the property is defined. If this value is <see langword="null"/>,
    /// the runtime time is assumed to not exist, and a fallback action which returns immediately is returned.</param>
    /// <param name="methodName">The name of the method to access.</param>
    /// <param name="argType">The runtime time of the parameter to the method. This value is allowed to be
    /// <see langword="null"/> if the method does not exist at runtime.</param>
    /// <returns>An accessor method to access the specified runtime method.</returns>
    public static Action<T, TArg> CreateActionAccessor<T, TArg>(Type? type, string methodName, Type? argType)
    {
        if (methodName is null)
        {
            throw new ArgumentNullException(nameof(methodName));
        }

        if (type == null)
        {
            return CreateFallbackAction<T, TArg>();
        }

        if (!typeof(T).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
        {
            throw new InvalidOperationException($"Type '{type}' is not assignable to type '{typeof(T)}'");
        }

        var method = type.GetTypeInfo().GetDeclaredMethod(methodName);
        if (method == null)
        {
            return CreateFallbackAction<T, TArg>();
        }

        var parameters = method.GetParameters();
        if (argType != parameters[0].ParameterType)
        {
            throw new ArgumentException($"Type '{argType}' was expected to match parameter type '{parameters[0].ParameterType}'", nameof(argType));
        }

        if (!typeof(TArg).GetTypeInfo().IsAssignableFrom(argType.GetTypeInfo()))
        {
            throw new InvalidOperationException($"Type '{argType}' is not assignable to type '{typeof(TArg)}'");
        }

        if (method.ReturnType != typeof(void))
        {
            throw new InvalidOperationException($"Method '{method}' produces an unexpected value of type '{method.ReturnType}'");
        }

        var parameter = Expression.Parameter(typeof(T), GenerateParameterName(typeof(T)));
        var argument = Expression.Parameter(typeof(TArg), parameters[0].Name);
        var instance =
            type.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
            ? (Expression)parameter
            : Expression.Convert(parameter, type);
        var convertedArgument =
            argType.GetTypeInfo().IsAssignableFrom(typeof(TArg).GetTypeInfo())
            ? (Expression)argument
            : Expression.Convert(argument, argType);

        var expression =
            Expression.Lambda<Action<T, TArg>>(
                Expression.Call(instance, method, convertedArgument),
                parameter,
                argument);
        return expression.Compile();
    }

    /// <summary>
    /// Generates a compiled accessor method for a method which cannot be bound at compile time.
    /// </summary>
    /// <typeparam name="T">The compile-time type representing the instance on which the property is defined. This
    /// may be a superclass of the actual type on which the property is declared if the declaring type is not
    /// available at compile time.</typeparam>
    /// <typeparam name="TResult">The compile-type type representing the result of the property. This may be a
    /// superclass of the actual type of the property if the property type is not available at compile
    /// time.</typeparam>
    /// <param name="type">The runtime time on which the property is defined. If this value is null, the runtime
    /// time is assumed to not exist, and a fallback accessor returning <paramref name="defaultValue"/> will be
    /// generated.</param>
    /// <param name="methodName">The name of the method to access.</param>
    /// <param name="defaultValue">The value to return if the method is not available at runtime.</param>
    /// <returns>An accessor method to access the specified runtime property.</returns>
    public static Func<T, TResult> CreateFunctionAccessor<T, TResult>(Type? type, string methodName, TResult defaultValue)
    {
        if (methodName is null)
        {
            throw new ArgumentNullException(nameof(methodName));
        }

        if (type == null)
        {
            return CreateFallbackFunction<T, TResult>(defaultValue);
        }

        if (!typeof(T).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
        {
            throw new InvalidOperationException($"Type '{type}' is not assignable to type '{typeof(T)}'");
        }

        var method = type.GetTypeInfo().GetDeclaredMethod(methodName);
        if (method == null)
        {
            return CreateFallbackFunction<T, TResult>(defaultValue);
        }

        if (!typeof(TResult).GetTypeInfo().IsAssignableFrom(method.ReturnType.GetTypeInfo()))
        {
            throw new InvalidOperationException($"Method '{method}' produces a value of type '{method.ReturnType}', which is not assignable to type '{typeof(TResult)}'");
        }

        var parameter = Expression.Parameter(typeof(T), GenerateParameterName(typeof(T)));
        var instance =
            type.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
            ? (Expression)parameter
            : Expression.Convert(parameter, type);

        var expression =
            Expression.Lambda<Func<T, TResult>>(
                Expression.Convert(Expression.Call(instance, method), typeof(TResult)),
                parameter);
        return expression.Compile();
    }

    /// <summary>
    /// Generates a compiled accessor method for a property which cannot be bound at compile time.
    /// </summary>
    /// <typeparam name="T">The compile-time type representing the instance on which the property is defined. This
    /// may be a superclass of the actual type on which the property is declared if the declaring type is not
    /// available at compile time.</typeparam>
    /// <typeparam name="TArg">The compile-time type representing the type of the first argument. This
    /// may be a superclass of the actual type of the argument if the declared type is not available at compile
    /// time.</typeparam>
    /// <typeparam name="TResult">The compile-type type representing the result of the property. This may be a
    /// superclass of the actual type of the property if the property type is not available at compile
    /// time.</typeparam>
    /// <param name="type">The runtime time on which the property is defined. If this value is null, the runtime
    /// time is assumed to not exist, and a fallback accessor returning <paramref name="defaultValue"/> will be
    /// generated.</param>
    /// <param name="methodName">The name of the method to access.</param>
    /// <param name="defaultValue">The value to return if the method is not available at runtime.</param>
    /// <returns>An accessor method to access the specified runtime property.</returns>
    public static Func<T, TArg, TResult> CreateFunctionAccessor<T, TArg, TResult>(Type? type, string methodName, Type? argType, TResult defaultValue)
    {
        if (methodName is null)
        {
            throw new ArgumentNullException(nameof(methodName));
        }

        if (type == null)
        {
            return CreateFallbackFunction<T, TArg, TResult>(defaultValue);
        }

        if (!typeof(T).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
        {
            throw new InvalidOperationException($"Type '{type}' is not assignable to type '{typeof(T)}'");
        }

        var method = type.GetTypeInfo().GetDeclaredMethods(methodName).Single(method =>
        {
            var parameters = method.GetParameters();
            return parameters is [{ ParameterType: var parameterType }] && parameterType == argType;
        });

        var parameters = method.GetParameters();
        if (argType != parameters[0].ParameterType)
        {
            throw new ArgumentException($"Type '{argType}' was expected to match parameter type '{parameters[0].ParameterType}'", nameof(argType));
        }

        if (!typeof(TResult).GetTypeInfo().IsAssignableFrom(method.ReturnType.GetTypeInfo()))
        {
            throw new InvalidOperationException($"Method '{method}' produces a value of type '{method.ReturnType}', which is not assignable to type '{typeof(TResult)}'");
        }

        var parameter = Expression.Parameter(typeof(T), GenerateParameterName(typeof(T)));
        var argument = Expression.Parameter(typeof(TArg), parameters[0].Name);
        var instance =
            type.GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())
            ? (Expression)parameter
            : Expression.Convert(parameter, type);
        var convertedArgument =
            argType.GetTypeInfo().IsAssignableFrom(typeof(TArg).GetTypeInfo())
            ? (Expression)argument
            : Expression.Convert(argument, argType);

        var expression =
            Expression.Lambda<Func<T, TArg, TResult>>(
                Expression.Convert(
                    Expression.Call(
                        instance,
                        method,
                        convertedArgument), typeof(TResult)),
                parameter,
                argument);
        return expression.Compile();
    }

    /// <summary>
    /// Generates a compiled accessor method for a method which cannot be bound at compile time.
    /// </summary>
    /// <typeparam name="TResult">The compile-type type representing the result of the property. This may be a
    /// superclass of the actual type of the property if the property type is not available at compile
    /// time.</typeparam>
    /// <param name="type">The runtime time on which the property is defined. If this value is null, the runtime
    /// time is assumed to not exist, and a fallback accessor returning <paramref name="defaultValue"/> will be
    /// generated.</param>
    /// <param name="methodName">The name of the method to access.</param>
    /// <param name="defaultValue">The value to return if the method is not available at runtime.</param>
    /// <returns>An accessor method to access the specified runtime property.</returns>
    public static Func<TResult> CreateStaticFunctionAccessor<TResult>(Type? type, string methodName, TResult defaultValue)
    {
        if (methodName is null)
        {
            throw new ArgumentNullException(nameof(methodName));
        }

        if (type == null)
        {
            return CreateFallbackStaticFunction<TResult>(defaultValue);
        }

        var method = type.GetTypeInfo().GetDeclaredMethod(methodName);
        if (method == null)
        {
            return CreateFallbackStaticFunction<TResult>(defaultValue);
        }

        if (!typeof(TResult).GetTypeInfo().IsAssignableFrom(method.ReturnType.GetTypeInfo()))
        {
            throw new InvalidOperationException($"Method '{method}' produces a value of type '{method.ReturnType}', which is not assignable to type '{typeof(TResult)}'");
        }

        var expression =
            Expression.Lambda<Func<TResult>>(
                Expression.Convert(Expression.Call(instance: null, method), typeof(TResult)));
        return expression.Compile();
    }

    /// <summary>
    /// Generates a compiled accessor method for a property which cannot be bound at compile time.
    /// </summary>
    /// <typeparam name="TArg">The compile-time type representing the type of the first argument. This
    /// may be a superclass of the actual type of the argument if the declared type is not available at compile
    /// time.</typeparam>
    /// <typeparam name="TResult">The compile-type type representing the result of the property. This may be a
    /// superclass of the actual type of the property if the property type is not available at compile
    /// time.</typeparam>
    /// <param name="type">The runtime time on which the property is defined. If this value is null, the runtime
    /// time is assumed to not exist, and a fallback accessor returning <paramref name="defaultValue"/> will be
    /// generated.</param>
    /// <param name="methodName">The name of the method to access.</param>
    /// <param name="defaultValue">The value to return if the method is not available at runtime.</param>
    /// <returns>An accessor method to access the specified runtime property.</returns>
    public static Func<TArg, TResult> CreateStaticFunctionAccessor<TArg, TResult>(Type? type, string methodName, Type? argType, TResult defaultValue)
    {
        if (methodName is null)
        {
            throw new ArgumentNullException(nameof(methodName));
        }

        if (type == null)
        {
            return CreateFallbackStaticFunction<TArg, TResult>(defaultValue);
        }

        var method = type.GetTypeInfo().GetDeclaredMethods(methodName).SingleOrDefault(method =>
        {
            var parameters = method.GetParameters();
            return parameters is [{ ParameterType: var parameterType }] && parameterType == argType;
        });

        if (method == null)
        {
            return CreateFallbackStaticFunction<TArg, TResult>(defaultValue);
        }

        var parameters = method.GetParameters();
        if (argType != parameters[0].ParameterType)
        {
            throw new ArgumentException($"Type '{argType}' was expected to match parameter type '{parameters[0].ParameterType}'", nameof(argType));
        }

        if (!typeof(TResult).GetTypeInfo().IsAssignableFrom(method.ReturnType.GetTypeInfo()))
        {
            throw new InvalidOperationException($"Method '{method}' produces a value of type '{method.ReturnType}', which is not assignable to type '{typeof(TResult)}'");
        }

        var argument = Expression.Parameter(typeof(TArg), parameters[0].Name);
        var convertedArgument =
            argType.GetTypeInfo().IsAssignableFrom(typeof(TArg).GetTypeInfo())
            ? (Expression)argument
            : Expression.Convert(argument, argType);

        var expression =
            Expression.Lambda<Func<TArg, TResult>>(
                Expression.Convert(
                    Expression.Call(
                        instance: null,
                        method,
                        convertedArgument), typeof(TResult)),
                argument);
        return expression.Compile();
    }

    /// <summary>
    /// Generates a compiled accessor method for a static method which cannot be bound at compile time.
    /// </summary>
    /// <typeparam name="TArg1">The compile-time type representing the type of the first argument. This
    /// may be a superclass of the actual type of the argument if the declared type is not available at compile
    /// time.</typeparam>
    /// <typeparam name="TArg2">The compile-time type representing the type of the second argument. This
    /// may be a superclass of the actual type of the argument if the declared type is not available at compile
    /// time.</typeparam>
    /// <typeparam name="TResult">The compile-type type representing the result of the property. This may be a
    /// superclass of the actual type of the property if the property type is not available at compile
    /// time.</typeparam>
    /// <param name="type">The runtime time on which the property is defined. If this value is null, the runtime
    /// time is assumed to not exist, and a fallback accessor returning <paramref name="defaultValue"/> will be
    /// generated.</param>
    /// <param name="methodName">The name of the method to access.</param>
    /// <param name="defaultValue">The value to return if the method is not available at runtime.</param>
    /// <returns>An accessor method to access the specified runtime property.</returns>
    public static Func<TArg1, TArg2, TResult> CreateStaticFunctionAccessor<TArg1, TArg2, TResult>(Type? type, string methodName, Type? argType1, Type? argType2, TResult defaultValue)
    {
        if (methodName is null)
        {
            throw new ArgumentNullException(nameof(methodName));
        }

        if (type == null)
        {
            return CreateFallbackStaticFunction<TArg1, TArg2, TResult>(defaultValue);
        }

        var method = type.GetTypeInfo().GetDeclaredMethods(methodName).SingleOrDefault(method =>
        {
            var parameters = method.GetParameters();
            return parameters is [{ ParameterType: var parameterType1 }, { ParameterType: var parameterType2 }]
                && parameterType1 == argType1
                && parameterType2 == argType2;
        });

        if (method == null)
        {
            return CreateFallbackStaticFunction<TArg1, TArg2, TResult>(defaultValue);
        }

        var parameters = method.GetParameters();
        if (argType1 != parameters[0].ParameterType)
        {
            throw new ArgumentException($"Type '{argType1}' was expected to match parameter type '{parameters[0].ParameterType}'", nameof(argType1));
        }

        if (argType2 != parameters[1].ParameterType)
        {
            throw new ArgumentException($"Type '{argType2}' was expected to match parameter type '{parameters[1].ParameterType}'", nameof(argType2));
        }

        if (!typeof(TResult).GetTypeInfo().IsAssignableFrom(method.ReturnType.GetTypeInfo()))
        {
            throw new InvalidOperationException($"Method '{method}' produces a value of type '{method.ReturnType}', which is not assignable to type '{typeof(TResult)}'");
        }

        var argument1 = Expression.Parameter(typeof(TArg1), parameters[0].Name);
        var argument2 = Expression.Parameter(typeof(TArg2), parameters[1].Name);
        var convertedArgument1 =
            argType1.GetTypeInfo().IsAssignableFrom(typeof(TArg1).GetTypeInfo())
            ? (Expression)argument1
            : Expression.Convert(argument1, argType1);
        var convertedArgument2 =
            argType2.GetTypeInfo().IsAssignableFrom(typeof(TArg2).GetTypeInfo())
            ? (Expression)argument2
            : Expression.Convert(argument2, argType2);

        var expression =
            Expression.Lambda<Func<TArg1, TArg2, TResult>>(
                Expression.Convert(
                    Expression.Call(
                        instance: null,
                        method,
                        convertedArgument1,
                        convertedArgument2), typeof(TResult)),
                argument1,
                argument2);
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

    private static Action<T> CreateFallbackAction<T>()
    {
        static void FallbackAction(T instance)
        {
            if (instance == null)
            {
                // Unlike an extension method which would throw ArgumentNullException here, the light-up
                // behavior needs to match behavior of the underlying property.
                throw new NullReferenceException();
            }
        }

        return FallbackAction;
    }

    private static Action<T, TArg> CreateFallbackAction<T, TArg>()
    {
        static void FallbackAction(T instance, TArg arg)
        {
            if (instance == null)
            {
                // Unlike an extension method which would throw ArgumentNullException here, the light-up
                // behavior needs to match behavior of the underlying property.
                throw new NullReferenceException();
            }
        }

        return FallbackAction;
    }

    private static Func<T, TResult> CreateFallbackFunction<T, TResult>(TResult defaultValue)
    {
        TResult FallbackFunction(T instance)
        {
            if (instance == null)
            {
                // Unlike an extension method which would throw ArgumentNullException here, the light-up
                // behavior needs to match behavior of the underlying property.
                throw new NullReferenceException();
            }

            return defaultValue;
        }

        return FallbackFunction;
    }

    private static Func<T, TArg, TResult> CreateFallbackFunction<T, TArg, TResult>(TResult defaultValue)
    {
        TResult FallbackFunction(T instance, TArg arg)
        {
            if (instance == null)
            {
                // Unlike an extension method which would throw ArgumentNullException here, the light-up
                // behavior needs to match behavior of the underlying property.
                throw new NullReferenceException();
            }

            return defaultValue;
        }

        return FallbackFunction;
    }

    private static Func<TResult> CreateFallbackStaticFunction<TResult>(TResult defaultValue)
    {
        TResult FallbackFunction()
        {
            return defaultValue;
        }

        return FallbackFunction;
    }

    private static Func<TArg, TResult> CreateFallbackStaticFunction<TArg, TResult>(TResult defaultValue)
    {
        TResult FallbackFunction(TArg arg)
        {
            return defaultValue;
        }

        return FallbackFunction;
    }

    private static Func<TArg1, TArg2, TResult> CreateFallbackStaticFunction<TArg1, TArg2, TResult>(TResult defaultValue)
    {
        TResult FallbackFunction(TArg1 arg1, TArg2 arg2)
        {
            return defaultValue;
        }

        return FallbackFunction;
    }
}
