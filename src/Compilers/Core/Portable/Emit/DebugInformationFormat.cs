// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Emit
{
    public enum DebugInformationFormat
    {
        Pdb = 1,
        PortablePdb = 2,
        Embedded = 3,
    }

    internal static partial class DebugInformationFormatExtensions
    {
        internal static bool IsValid(this DebugInformationFormat value)
        {
            return value >= DebugInformationFormat.Pdb && value <= DebugInformationFormat.Embedded;
        }
    }
}
