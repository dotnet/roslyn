// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
namespace Microsoft.CodeAnalysis.Editor.Interactive
{
    internal sealed class InteractiveEvaluatorResetOptions
    {
        public bool? Is64Bit;

        public InteractiveEvaluatorResetOptions(bool? is64Bit)
            => Is64Bit = is64Bit;
    }
}
