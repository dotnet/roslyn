// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

// This was copied from https://github.com/dotnet/runtime/blob/d47f4adf361bbf7bed2ac02e747e115011cf1d35/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/OverloadResolutionPriorityAttribute.cs
// and updated to have the scope of the attributes be internal.

#if NET9_0_OR_GREATER

using System.Runtime.CompilerServices;

#pragma warning disable RS0016 // Add public types and members to the declared API (this is a supporting forwarder for an internal polyfill API)
[assembly: TypeForwardedTo(typeof(OverloadResolutionPriorityAttribute))]
#pragma warning restore RS0016 // Add public types and members to the declared API

#else

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Specifies the priority of a member in overload resolution. When unspecified, the default priority is 0.
    /// </summary>
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
}

#endif
