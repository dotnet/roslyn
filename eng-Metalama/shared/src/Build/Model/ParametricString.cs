// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using System;

namespace PostSharp.Engineering.BuildTools.Build.Model
{
    public readonly struct ParametricString
    {
        private readonly string? _value;

        private ParametricString( string value )
        {
            this._value = value;
        }

        public override string ToString() => this._value ?? "<null>";

        public string ToString( VersionInfo parameters )
            => this._value?
                .Replace( "$(PackageVersion)", parameters.PackageVersion, StringComparison.OrdinalIgnoreCase )
                .Replace( "$(Configuration)", parameters.Configuration, StringComparison.OrdinalIgnoreCase ) ?? "";

        public static implicit operator ParametricString( string value ) => new( value );
    }
}