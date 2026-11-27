// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copied from https://github.com/dotnet/runtime

#if !NET9_0_OR_GREATER

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class OverloadResolutionPriorityAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OverloadResolutionPriorityAttribute"/> class.
    /// </summary>
    /// <param name="priority">The priority of the attributed member. Higher numbers are prioritized, lower numbers are deprioritized. 0 is the default if no attribute is present.</param>
    public OverloadResolutionPriorityAttribute(int priority)
    {
        Priority = priority;
    }

    /// <summary>
    /// The priority of the member.
    /// </summary>
    public int Priority { get; }
}

#else

using System.Runtime.CompilerServices;

#pragma warning disable RS0016 // Add public types and members to the declared API (this is a supporting forwarder for an internal polyfill API)
[assembly: TypeForwardedTo(typeof(OverloadResolutionPriorityAttribute))]
#pragma warning restore RS0016 // Add public types and members to the declared API

#endif
