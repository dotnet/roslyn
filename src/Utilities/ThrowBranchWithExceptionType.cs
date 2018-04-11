// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.Operations.DataFlow
{
    internal class ThrowBranchWithExceptionType : IEquatable<ThrowBranchWithExceptionType>
    {
        public ThrowBranchWithExceptionType(BranchWithInfo branch, INamedTypeSymbol exceptionType)
        {
            Branch = branch;
            ExceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
        }

        public BranchWithInfo Branch { get; }
        public INamedTypeSymbol ExceptionType { get; }

        public bool Equals(ThrowBranchWithExceptionType other) => other != null && Branch.Equals(other.Branch) && ExceptionType == other.ExceptionType;
        public override bool Equals(object obj) => Equals(obj as ThrowBranchWithExceptionType);
        public override int GetHashCode() => HashUtilities.Combine(Branch.GetHashCode(), ExceptionType.GetHashCode());
    }
}
