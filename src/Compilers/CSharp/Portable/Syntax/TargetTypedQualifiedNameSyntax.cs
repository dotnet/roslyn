// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class TargetTypedQualifiedNameSyntax : NameSyntax
    {
        // This override is only intended to support cases where a caller has a value statically typed as NameSyntax in
        // hand and neither knows nor cares to determine the exact shape of that name.  If a value is statically typed
        // as a TargetTypedQualifiedNameSyntax, calling Right directly is preferred.
        internal override SimpleNameSyntax GetUnqualifiedName()
        {
            return Right;
        }

        internal override string ErrorDisplayName()
        {
            return "." + Right.ErrorDisplayName();
        }
    }
}
