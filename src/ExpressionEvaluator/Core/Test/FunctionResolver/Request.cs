// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal struct Address
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
