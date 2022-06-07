// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis
{
    internal static class GreenNode
    {
        /// <summary>
        /// Required by <see cref="SyntaxKind"/>.
        /// </summary>
        public const int ListKind = 1;
    }
}

// This needs to be re-defined here to avoid ambiguity, because we allow this project to target .NET 4.0 on machines without 2.0 installed.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal class ExtensionAttribute : Attribute
    {
    }
}
