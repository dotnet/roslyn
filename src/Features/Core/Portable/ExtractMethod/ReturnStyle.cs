// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal sealed record class ReturnStyle(
    ParameterBehavior ParameterBehavior,
    ReturnBehavior ReturnBehavior,
    DeclarationBehavior DeclarationBehavior)
{
    public static readonly ReturnStyle None =
        new(ParameterBehavior.None, ReturnBehavior.None, DeclarationBehavior.None);

    public static readonly ReturnStyle AssignmentWithInput =
        new(ParameterBehavior.Input, ReturnBehavior.Assignment, DeclarationBehavior.None);

    public static readonly ReturnStyle AssignmentWithNoInput =
        new(ParameterBehavior.None, ReturnBehavior.Assignment, DeclarationBehavior.SplitIn);

    public static readonly ReturnStyle Initialization =
        new(ParameterBehavior.None, ReturnBehavior.Initialization, DeclarationBehavior.SplitOut);
}
