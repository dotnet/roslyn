// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Support ErrorSource information.
    /// </summary>
    internal abstract class BuildToolId
    {
        public abstract string BuildTool { get; }

        internal abstract class Base<T>(T? field) : BuildToolId
        {
            protected readonly T? _Field1 = field;

            public override bool Equals(object? obj)
            {
                if (obj is not Base<T> other)
                {
                    return false;
                }

                return object.Equals(_Field1, other._Field1);
            }

            public override int GetHashCode()
                => _Field1?.GetHashCode() ?? 0;
        }

        internal abstract class Base<T1, T2>(T1? field1, T2? field2) : Base<T2>(field2)
        {
            private readonly T1? _Field2 = field1;

            public override bool Equals(object? obj)
            {
                if (obj is not Base<T1, T2> other)
                {
                    return false;
                }

                return object.Equals(_Field2, other._Field2) && base.Equals(other);
            }

            public override int GetHashCode()
                => Hash.Combine(_Field2?.GetHashCode() ?? 0, base.GetHashCode());
        }
    }
}
