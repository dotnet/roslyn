// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    // Simulates a sensible override of object.Equals.
    internal class TestDocumentationProviderEquals : DocumentationProvider
    {
        protected internal override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken) => "";
        public override bool Equals(object obj) => obj != null && this.GetType() == obj.GetType();
        public override int GetHashCode() => GetType().GetHashCode();
    }

    // Simulates no override of object.Equals.
    internal class TestDocumentationProviderNoEquals : DocumentationProvider
    {
        protected internal override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken) => "";
        public override bool Equals(object obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}