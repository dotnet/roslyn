// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal readonly struct ResultProperties
    {
        public readonly DkmClrCompilationResultFlags Flags;
        public readonly DkmEvaluationResultCategory Category;
        public readonly DkmEvaluationResultAccessType AccessType;
        public readonly DkmEvaluationResultStorageType StorageType;
        public readonly DkmEvaluationResultTypeModifierFlags ModifierFlags;

        public ResultProperties(
            DkmClrCompilationResultFlags flags,
            DkmEvaluationResultCategory category,
            DkmEvaluationResultAccessType accessType,
            DkmEvaluationResultStorageType storageType,
            DkmEvaluationResultTypeModifierFlags modifierFlags)
        {
            Flags = flags;
            Category = category;
            AccessType = accessType;
            StorageType = storageType;
            ModifierFlags = modifierFlags;
        }

        /// <remarks>
        /// For statements and assignments, we are only interested in <see cref="DkmClrCompilationResultFlags"/>.
        /// </remarks>
        public ResultProperties(DkmClrCompilationResultFlags flags)
            : this(flags, category: default, accessType: default, storageType: default, modifierFlags: default)
        {
        }
    }
}
