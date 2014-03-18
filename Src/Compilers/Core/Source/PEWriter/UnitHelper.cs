// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using System.Text;

namespace Microsoft.Cci
{
    /// <summary>
    /// Class containing helper routines for Units
    /// </summary>
    internal static class UnitHelper
    {
        /// <summary>
        /// Computes the string representing the strong name of the given assembly reference.
        /// </summary>
        public static string StrongName(IAssemblyReference assemblyReference)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(assemblyReference.Name);
            sb.AppendFormat(CultureInfo.InvariantCulture, ", Version={0}.{1}.{2}.{3}", assemblyReference.Version.Major, assemblyReference.Version.Minor, assemblyReference.Version.Build, assemblyReference.Version.Revision);
            if (assemblyReference.Culture != null && assemblyReference.Culture.Length > 0)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ", Culture={0}", assemblyReference.Culture);
            }
            else
            {
                sb.Append(", Culture=neutral");
            }

            sb.Append(", PublicKeyToken=");
            if (IteratorHelper.EnumerableIsNotEmpty(assemblyReference.PublicKeyToken))
            {
                foreach (byte b in assemblyReference.PublicKeyToken)
                {
                    sb.Append(b.ToString("x2"));
                }
            }
            else
            {
                sb.Append("null");
            }

            if (assemblyReference.IsRetargetable)
            {
                sb.Append(", Retargetable=Yes");
            }

            return sb.ToString();
        }
    }
}