// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    internal enum UsingPlacementPreference
    {
        ///<summary>
        /// No preference whether usings should be placed inside or outside namespaces.
        ///</summary>
        NoPreference,

        ///<summary>
        /// Prefer usings placed inside namespaces.
        ///</summary>
        InsideNamespace,

        ///<summary>
        /// Prefer usings placed outside namespaces.
        ///</summary>
        OutsideNamespace
    }
}
