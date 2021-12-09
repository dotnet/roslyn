// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

namespace PostSharp.Engineering.BuildTools.Build.Model
{
    public readonly struct VersionSpec
    {
        public VersionKind Kind { get; }

        public int Number { get; }

        public VersionSpec( VersionKind kind, int number = 0 )
        {
            this.Kind = kind;
            this.Number = number;
        }
    }
}