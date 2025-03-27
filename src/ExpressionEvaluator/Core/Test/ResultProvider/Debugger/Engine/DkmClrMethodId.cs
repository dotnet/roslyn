// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System;

namespace Microsoft.VisualStudio.Debugger.Clr
{
    public readonly struct DkmClrMethodId : IComparable<DkmClrMethodId>, IEquatable<DkmClrMethodId>
    {
        public int CompareTo(DkmClrMethodId other)
        {
            if (this.Token != other.Token)
            {
                if (this.Token < other.Token)
                    return -1;
                else
                    return 1;
            }
            if (this.Version != other.Version)
            {
                if (this.Version < other.Version)
                    return -1;
                else
                    return 1;
            }
            return 0;
        }

        public bool Equals(DkmClrMethodId other)
        {
            return (this.CompareTo(other) == 0);
        }

        public override bool Equals(object obj)
        {
            return obj is DkmClrMethodId && Equals((DkmClrMethodId)obj);
        }

        public static bool operator !=(DkmClrMethodId element0, DkmClrMethodId element1)
        {
            return element0.CompareTo(element1) != 0;
        }

        public static bool operator ==(DkmClrMethodId element0, DkmClrMethodId element1)
        {
            return element0.CompareTo(element1) == 0;
        }

        public static bool operator >(DkmClrMethodId element0, DkmClrMethodId element1)
        {
            return element0.CompareTo(element1) > 0;
        }

        public static bool operator <(DkmClrMethodId element0, DkmClrMethodId element1)
        {
            return element0.CompareTo(element1) < 0;
        }

        public static bool operator >=(DkmClrMethodId element0, DkmClrMethodId element1)
        {
            return element0.CompareTo(element1) >= 0;
        }

        public static bool operator <=(DkmClrMethodId element0, DkmClrMethodId element1)
        {
            return element0.CompareTo(element1) <= 0;
        }
        public override int GetHashCode()
        {
            return Token ^ ((int)Version);
        }

        public readonly int Token;

        public readonly uint Version;

        public DkmClrMethodId(int Token, uint Version)
        {
            this.Token = Token;
            this.Version = Version;
        }
    }
}
