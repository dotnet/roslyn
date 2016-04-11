// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal class MoveTypeOptionsResult
    {
        public static readonly MoveTypeOptionsResult Cancelled = new MoveTypeOptionsResult(isCancelled: true);

        public bool IsCancelled { get; }
        public string NewFileName { get; }

        public MoveTypeOptionsResult(string newFileName, bool isCancelled = false)
        {
            this.NewFileName = newFileName;
            this.IsCancelled = isCancelled;
        }

        private MoveTypeOptionsResult(bool isCancelled)
        {
            this.IsCancelled = isCancelled;
        }
    }
}
