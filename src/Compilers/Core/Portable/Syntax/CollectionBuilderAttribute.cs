// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NET8_0_OR_GREATER

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    internal sealed class CollectionBuilderAttribute : Attribute
    {
        public CollectionBuilderAttribute(Type builderType, string methodName)
        {
            BuilderType = builderType;
            MethodName = methodName;
        }

        public Type BuilderType { get; }
        public string MethodName { get; }
    }
}

#else

using System.Runtime.CompilerServices;

#pragma warning disable RS0016 // Add public types and members to the declared API (this is a supporting forwarder for an internal polyfill API)
[assembly: TypeForwardedTo(typeof(CollectionBuilderAttribute))]
#pragma warning restore RS0016 // Add public types and members to the declared API

#endif
