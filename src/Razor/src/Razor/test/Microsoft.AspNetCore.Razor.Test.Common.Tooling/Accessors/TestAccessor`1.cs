// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github.com/dotnet/winforms/blob/8812d0e14e2cc9fd9870d70e31bfa083add7e541/src/Common/tests/TestUtilities/TestAccessor.cs

using System;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Razor.Test.Common.Accessors;

/// <summary>
///  Internals (including privates) access wrapper for tests.
/// </summary>
/// <typeparam name="T">The type of the class being accessed.</typeparam>
/// <remarks>
///  <para>
///   Does not allow access to public members- use the object directly.
///  </para>
///  <para>
///   One should strive to *not* access internal state where otherwise avoidable.
///   Ask yourself if you can test the contract of the object in question
///   *without* manipulating internals directly. Often you can.
///  </para>
///  <para>
///   Where internals access is more useful are testing building blocks of more
///   complicated objects, such as internal helper methods or classes.
///  </para>
///  <para>
///   This can be used to access private/internal objects as well via
///  </para>
/// </remarks>
/// <example>
///  This class can also be derived from to create a strongly typed wrapper
///  that can then be associated via an extension method for the given type
///  to provide consistent discovery and access.
///
///  <![CDATA[
///   public class GuidTestAccessor : TestAccessor<Guid>
///   {
///     public TestAccessor(Guid instance) : base(instance) {}
///
///     public int A => Dynamic._a;
///   }
///
///   public static partial class TestAccessors
///   {
///       public static GuidTestAccessor TestAccessor(this Guid guid)
///           => new GuidTestAccessor(guid);
///   }
///  ]]>
/// </example>
public class TestAccessor<T> : ITestAccessor
{
    private static readonly Type s_type = typeof(T);

    protected readonly T? Instance;
    private readonly DynamicWrapper _dynamicWrapper;

    /// <param name="instance">The type instance, can be null for statics.</param>
    public TestAccessor(T? instance)
    {
        Instance = instance;
        _dynamicWrapper = new DynamicWrapper(Instance);
    }

    /// <inheritdoc/>
    public TDelegate CreateDelegate<TDelegate>(string? methodName = null)
        where TDelegate : Delegate
    {
        var type = typeof(TDelegate);
        var invokeMethodInfo = type.GetMethod("Invoke");
        var types = invokeMethodInfo is null ? Array.Empty<Type>() : invokeMethodInfo.GetParameters().Select(pi => pi.ParameterType).ToArray();

        // To make it easier to write a class wrapper with a number of delegates,
        // we'll take the name from the delegate itself when unspecified.
        methodName ??= type.Name;

        var methodInfo = s_type.GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
            binder: null,
            types,
            modifiers: null);

        if (methodInfo is null)
        {
            throw new ArgumentException($"Could not find non public method {methodName}.");
        }

        return (TDelegate)methodInfo.CreateDelegate(type, methodInfo.IsStatic ? null : Instance);
    }

    /// <inheritdoc/>
    public dynamic Dynamic => _dynamicWrapper;

    private sealed class DynamicWrapper(object? instance) : DynamicObject
    {
        private readonly object? _instance = instance;

        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (binder is null)
            {
                throw new ArgumentNullException(nameof(binder));
            }

            result = null;

            MethodInfo? methodInfo = null;
            var type = s_type;

            do
            {
                try
                {
                    methodInfo = type?.GetMethod(
                        binder.Name,
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                }
                catch (AmbiguousMatchException)
                {
                    // More than one match for the name, specify the arguments.
                    // We currently do not have a scenario where we are trying to pass null as an argument
                    // to an overloaded method. This will need to be updated once we have a scenario.
                    methodInfo = type?.GetMethod(
                        binder.Name,
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                        binder: null,
                        args.Select(a => a!.GetType()).ToArray(),
                        modifiers: null);
                }

                if (methodInfo is not null || type == typeof(object))
                {
                    // Found something, or already at the top of the type hierarchy
                    break;
                }

                // Walk up the hierarchy
                type = type?.BaseType;
            }
            while (true);

            if (methodInfo is null)
                return false;

            result = methodInfo.Invoke(_instance, args);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            var info = GetFieldOrPropertyInfo(binder.Name);
            if (info is null)
                return false;

            SetValue(info, value);
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = null;

            var info = GetFieldOrPropertyInfo(binder.Name);
            if (info is null)
                return false;

            result = GetValue(info);
            return true;
        }

        private MemberInfo? GetFieldOrPropertyInfo(string memberName)
        {
            var type = s_type;
            MemberInfo? info;

            do
            {
                info = (MemberInfo?)type?.GetField(
                    memberName,
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? type?.GetProperty(
                        memberName,
                        BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

                if (info is not null || type == typeof(object))
                {
                    // Found something, or already at the top of the type hierarchy
                    break;
                }

                // Walk up the type hierarchy
                type = type?.BaseType;
            }
            while (true);

            return info;
        }

        private object? GetValue(MemberInfo memberInfo)
            => memberInfo switch
            {
                FieldInfo fieldInfo => fieldInfo.GetValue(_instance),
                PropertyInfo propertyInfo => propertyInfo.GetValue(_instance),
                _ => throw new InvalidOperationException()
            };

        private void SetValue(MemberInfo memberInfo, object? value)
        {
            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    fieldInfo.SetValue(_instance, value);
                    break;
                case PropertyInfo propertyInfo:
                    propertyInfo.SetValue(_instance, value);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
