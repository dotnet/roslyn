// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct LocationlessDiagnostic
    {
        public readonly DiagnosticDescriptor Descriptor;
        public readonly object[] MessageArgs;

        public LocationlessDiagnostic(DiagnosticDescriptor descriptor, object[] messageArgs)
        {
            Descriptor = descriptor;
            MessageArgs = messageArgs;
        }

        public Diagnostic ToDiagnostic(Location location)
            => Diagnostic.Create(Descriptor, location, MessageArgs);
    }
}
