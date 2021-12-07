// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using System;

namespace PostSharp.Engineering.BuildTools.Build.Model
{
    public enum Verbosity
    {
        Default,
        Minimal = Default,
        Standard,
        Detailed,
        Diagnostic
    }

    public static class VerbosityExtensions
    {
        public static string ToAlias( this Verbosity verbosity )
            => verbosity switch
            {
                Verbosity.Minimal => "m",
                Verbosity.Detailed => "detailed",
                Verbosity.Diagnostic => "diag",
                Verbosity.Standard => "s",
                _ => throw new ArgumentOutOfRangeException( nameof(verbosity), verbosity, null )
            };
    }
}