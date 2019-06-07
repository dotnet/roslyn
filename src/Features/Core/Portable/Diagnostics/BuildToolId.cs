// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Support ErrorSource information.
    /// </summary>
    internal abstract class BuildToolId
    {
        internal abstract class Base<T> : BuildToolId
        {
            protected readonly T _Field1;

            public Base(T field)
            {
                _Field1 = field;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Base<T> other))
                {
                    return false;
                }

                return object.Equals(_Field1, other._Field1);
            }

            public override int GetHashCode()
            {
                return _Field1?.GetHashCode() ?? 0;
            }
        }

        internal abstract class Base<T1, T2> : Base<T2>
        {
            private readonly T1 _Field2;

            public Base(T1 field1, T2 field2) : base(field2)
            {
                _Field2 = field1;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Base<T1, T2> other))
                {
                    return false;
                }

                return object.Equals(_Field2, other._Field2) && base.Equals(other);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_Field2?.GetHashCode() ?? 0, base.GetHashCode());
            }
        }
    }
}
