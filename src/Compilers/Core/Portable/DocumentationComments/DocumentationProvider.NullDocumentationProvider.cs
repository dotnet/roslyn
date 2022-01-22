// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    public partial class DocumentationProvider
    {
        /// <summary>
        /// A trivial DocumentationProvider which never returns documentation.
        /// </summary>
        private class NullDocumentationProvider : DocumentationProvider
        {
            protected internal override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default(CancellationToken))
            {
                return "";
            }

            public override bool Equals(object? obj)
            {
                // Only one instance is expected to exist, so reference equality is fine.
                return (object)this == obj;
            }

            public override int GetHashCode()
            {
                return RuntimeHelpers.GetHashCode(this);
            }
        }
    }
}
