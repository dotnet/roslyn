// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ActiveInstructionId
    {
        public readonly Guid ModuleId;
        public readonly int MethodToken;
        public readonly uint MethodVersion;
        public readonly uint ILOffset;

        public ActiveInstructionId(Guid moduleId, int methodToken, uint methodVersion, uint ilOffset)
        {
            ModuleId = moduleId;
            MethodToken = methodToken;
            MethodVersion = methodVersion;
            ILOffset = ilOffset;
        }
    }
}
