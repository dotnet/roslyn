using System;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Immutable representation of a line and characters difference.  
    /// </summary>
    public struct LineDelta : IEquatable<LineDelta>
    {
        private readonly int lines;
        private readonly int characters;

        public LineDelta(int lines, int characters)
        {
            if (lines < 0)
            {
                throw new ArgumentOutOfRangeException("lines");
            }

            if (characters < 0)
            {
                throw new ArgumentOutOfRangeException("characters");
            }

            this.lines = lines;
            this.characters = characters;
        }

        public int Lines
        {
            get { return this.lines; }
        }

        public int Characters
        {
            get { return this.characters; }
        }

        public LineDelta Add(LineDelta delta)
        {
            if (delta.Lines > 0)
            {
                return new LineDelta(this.lines + delta.Lines, delta.Characters);
            }
            else
            {
                return new LineDelta(this.lines, this.characters + delta.Characters);
            }
        }

        public static LineDelta operator +(LineDelta left, LineDelta right)
        {
            return left.Add(right);
        }

        public static LineDelta operator +(LineDelta left, int characters)
        {
            return new LineDelta(left.Lines, left.Characters + characters);
        }

        public static bool operator ==(LineDelta left, LineDelta right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LineDelta left, LineDelta right)
        {
            return !left.Equals(right);
        }

        public static readonly LineDelta Zero = new LineDelta(0, 0);

        public bool Equals(LineDelta other)
        {
            return Lines == other.lines && Characters == other.Characters;
        }

        public override bool Equals(object obj)
        {
            return obj is LineDelta && Equals((LineDelta)obj);
        }

        public override int GetHashCode()
        {
            return Roslyn.Compilers.HashFunctions.CombineHashKey(Lines, Characters);
        }
    }
}