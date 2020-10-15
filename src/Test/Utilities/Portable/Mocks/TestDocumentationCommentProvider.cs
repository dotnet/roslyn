// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
