// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
namespace Microsoft.CodeAnalysis.Editor.Interactive
{
    internal sealed class InteractiveEvaluatorResetOptions
    {
        public bool? Is64Bit;

        public InteractiveEvaluatorResetOptions(bool? is64Bit)
        {
            Is64Bit = is64Bit;
        }
    }
}
