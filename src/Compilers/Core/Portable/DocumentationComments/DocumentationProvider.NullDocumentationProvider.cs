// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

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

            public override bool Equals(object obj)
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
