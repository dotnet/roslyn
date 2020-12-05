' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.SimplifyInterpolation

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyInterpolation
    Friend NotInheritable Class VisualBasicSimplifyInterpolationHelpers
        Inherits AbstractSimplifyInterpolationHelpers

        Protected Overrides ReadOnly Property PermitNonLiteralAlignmentComponents As Boolean = False
    End Class
End Namespace
