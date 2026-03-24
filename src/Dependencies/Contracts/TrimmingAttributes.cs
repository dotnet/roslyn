// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

// Polyfill for trimming/AOT attributes that are not available in netstandard2.0.

#if NET

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable RS0016 // Add public types and members to the declared API (this is a supporting forwarder for an internal polyfill API)
[assembly: TypeForwardedTo(typeof(RequiresUnreferencedCodeAttribute))]
[assembly: TypeForwardedTo(typeof(RequiresDynamicCodeAttribute))]
[assembly: TypeForwardedTo(typeof(RequiresAssemblyFilesAttribute))]
[assembly: TypeForwardedTo(typeof(UnconditionalSuppressMessageAttribute))]
[assembly: TypeForwardedTo(typeof(DynamicallyAccessedMembersAttribute))]
[assembly: TypeForwardedTo(typeof(DynamicallyAccessedMemberTypes))]
#pragma warning restore RS0016 // Add public types and members to the declared API

#else

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class RequiresUnreferencedCodeAttribute : Attribute
    {
        public RequiresUnreferencedCodeAttribute(string message)
        {
            Message = message;
        }

        public string Message { get; }
        public string? Url { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class RequiresDynamicCodeAttribute : Attribute
    {
        public RequiresDynamicCodeAttribute(string message)
        {
            Message = message;
        }

        public string Message { get; }
        public string? Url { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, Inherited = false)]
    internal sealed class RequiresAssemblyFilesAttribute : Attribute
    {
        public RequiresAssemblyFilesAttribute() { }

        public RequiresAssemblyFilesAttribute(string message)
        {
            Message = message;
        }

        public string? Message { get; }
        public string? Url { get; set; }
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    internal sealed class UnconditionalSuppressMessageAttribute : Attribute
    {
        public UnconditionalSuppressMessageAttribute(string category, string checkId)
        {
            Category = category;
            CheckId = checkId;
        }

        public string Category { get; }
        public string CheckId { get; }
        public string? Scope { get; set; }
        public string? Target { get; set; }
        public string? MessageId { get; set; }
        public string? Justification { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, Inherited = false)]
    internal sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
        {
            MemberTypes = memberTypes;
        }

        public DynamicallyAccessedMemberTypes MemberTypes { get; }
    }

    [Flags]
    internal enum DynamicallyAccessedMemberTypes
    {
        None = 0,
        PublicParameterlessConstructor = 0x0001,
        PublicConstructors = 0x0003,
        NonPublicConstructors = 0x0004,
        PublicMethods = 0x0008,
        NonPublicMethods = 0x0010,
        PublicFields = 0x0020,
        NonPublicFields = 0x0040,
        PublicNestedTypes = 0x0080,
        NonPublicNestedTypes = 0x0100,
        PublicProperties = 0x0200,
        NonPublicProperties = 0x0400,
        PublicEvents = 0x0800,
        NonPublicEvents = 0x1000,
        Interfaces = 0x2000,
        All = ~None,
    }
}

#endif
