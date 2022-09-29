// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [DataContract]
    internal sealed class UnitTestingSearchQuery
    {
        /// <summary>
        /// Fully qualified metadata name for the type being searched for, or the containing type of the method being
        /// searched for (if <see cref="MethodName"/> is provided).  Should follow .Net metadata naming, e.g. <c>`</c>
        /// for arity and <c>+</c> for nested types.
        /// </summary>
        [DataMember(Order = 0)]
        public readonly string FullyQualifiedTypeName;

        /// <summary>
        /// Optional name of method within <see cref="FullyQualifiedTypeName"/> being searched for.  Should not include arity.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly string? MethodName;

        /// <summary>
        /// Arity of the method being searched for.  Only valid if <see cref="MethodName"/> is non-null.
        /// </summary>
        [DataMember(Order = 2)]
        public readonly int MethodArity;

        /// <summary>
        /// Parameter count of the method being searched for.  Only valid if <see cref="MethodName"/> is non-null.
        /// </summary>
        [DataMember(Order = 3)]
        public readonly int MethodParameterCount;

        public static UnitTestingSearchQuery ForType(string fullyQualifiedTypeName)
            => new(fullyQualifiedTypeName, methodName: null, methodArity: 0, methodParameterCount: 0);

        public static UnitTestingSearchQuery ForMethod(string fullyQualifiedTypeName, string methodName, int methodArity, int methodParameterCount)
            => new(fullyQualifiedTypeName, methodName, methodArity, methodParameterCount);

        private UnitTestingSearchQuery(string fullyQualifiedTypeName, string? methodName, int methodArity, int methodParameterCount)
        {
            FullyQualifiedTypeName = fullyQualifiedTypeName;
            MethodName = methodName;
            MethodArity = methodArity;
            MethodParameterCount = methodParameterCount;
        }
    }
}
