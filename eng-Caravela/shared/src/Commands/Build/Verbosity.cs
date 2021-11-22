using System;

namespace PostSharp.Engineering.BuildTools.Commands.Build
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