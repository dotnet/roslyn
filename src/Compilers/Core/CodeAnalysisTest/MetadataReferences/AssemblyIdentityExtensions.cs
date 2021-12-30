// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Microsoft.CodeAnalysis.UnitTests
{
    internal static class AssemblyIdentityExtensions
    {
        // CultureInfo is not portable.

        /// <summary>
        /// Converts this identity to <see cref="AssemblyName"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="AssemblyName"/>.</returns>
        /// <exception cref="System.Globalization.CultureNotFoundException">The culture specified in <see cref="AssemblyIdentity.CultureName"/> is not available on the current platform.</exception>
        public static AssemblyName ToAssemblyName(this AssemblyIdentity identity)
        {
            var result = new AssemblyName();
            result.Name = identity.Name;
            result.Version = identity.Version;
            result.Flags = identity.Flags;
            result.ContentType = identity.ContentType;
            result.CultureInfo = CultureInfo.GetCultureInfo(identity.CultureName);

            if (identity.PublicKey.Length > 0)
            {
                result.SetPublicKey(identity.PublicKey.ToArray());
            }

            if (!identity.PublicKeyToken.IsDefault)
            {
                result.SetPublicKeyToken(identity.PublicKeyToken.ToArray());
            }

            return result;
        }
    }
}
