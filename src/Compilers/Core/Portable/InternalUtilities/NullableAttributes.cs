﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This was copied from https://github.com/dotnet/coreclr/blob/60f1e6265bd1039f023a82e0643b524d6aaf7845/src/System.Private.CoreLib/shared/System/Diagnostics/CodeAnalysis/NullableAttributes.cs
// and updated to have the scope of the attributes be internal.

#if !NETCOREAPP
#nullable enable

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Specifies that null is allowed as an input even if the corresponding type disallows it.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class AllowNullAttribute : Attribute { }

    /// <summary>Specifies that null is disallowed as an input even if the corresponding type allows it.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class DisallowNullAttribute : Attribute { }

    /// <summary>Specifies that an output may be null even if the corresponding type disallows it.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class MaybeNullAttribute : Attribute { }

    /// <summary>Specifies that an output will not be null even if the corresponding type allows it.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute { }

    /// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter may be null even if the corresponding type disallows it.</summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class MaybeNullWhenAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified return value condition.</summary>
        /// <param name="returnValue">
        /// The return value condition. If the method returns this value, the associated parameter may be null.
        /// </param>
        public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }
    }

    /// <summary>Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null even if the corresponding type allows it.</summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified return value condition.</summary>
        /// <param name="returnValue">
        /// The return value condition. If the method returns this value, the associated parameter will not be null.
        /// </param>
        public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;

        /// <summary>Gets the return value condition.</summary>
        public bool ReturnValue { get; }
    }

    /// <summary>Specifies that the output will be non-null if the named parameter is non-null.</summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    internal sealed class NotNullIfNotNullAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the associated parameter name.</summary>
        /// <param name="parameterName">
        /// The associated parameter name.  The output will be non-null if the argument to the parameter specified is non-null.
        /// </param>
        public NotNullIfNotNullAttribute(string parameterName) => ParameterName = parameterName;

        /// <summary>Gets the associated parameter name.</summary>
        public string ParameterName { get; }
    }

    /// <summary>Applied to a method that will never return under any circumstance.</summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class DoesNotReturnAttribute : Attribute { }

    /// <summary>Specifies that the method will not return if the associated Boolean parameter is passed the specified value.</summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class DoesNotReturnIfAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified parameter value.</summary>
        /// <param name="parameterValue">
        /// The condition parameter value. Code after the method will be considered unreachable by diagnostics if the argument to
        /// the associated parameter matches this value.
        /// </param>
        public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;

        /// <summary>Gets the condition parameter value.</summary>
        public bool ParameterValue { get; }
    }
}

#endif