// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal readonly struct Address
    {
        internal readonly Module Module;
        internal readonly int Token;
        internal readonly int Version;
        internal readonly int ILOffset;

        internal Address(Module module, int token, int version, int ilOffset)
        {
            Module = module;
            Token = token;
            Version = version;
            ILOffset = ilOffset;
        }
    }

    internal sealed class Request
    {
        private readonly List<Address> _resolvedAddresses;

        internal Request(string moduleName, RequestSignature signature, Guid languageId = default(Guid))
        {
            ModuleName = moduleName;
            Signature = signature;
            LanguageId = languageId;
            _resolvedAddresses = new List<Address>();
        }

        internal readonly string ModuleName;
        internal readonly RequestSignature Signature;
        internal readonly Guid LanguageId;

        internal void OnFunctionResolved(Module module, int token, int version, int ilOffset)
        {
            _resolvedAddresses.Add(new Address(module, token, version, ilOffset));
        }

        internal ImmutableArray<Address> GetResolvedAddresses()
        {
            return ImmutableArray.CreateRange(_resolvedAddresses);
        }
    }
}
