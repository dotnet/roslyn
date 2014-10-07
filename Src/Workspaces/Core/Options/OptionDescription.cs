using System;
using Roslyn.Utilities;

namespace Roslyn.Services
{
    /// <summary>
    /// give option's name and type information
    /// </summary>
    public struct OptionDescription : IEquatable<OptionDescription>
    {
        /// <summary>
        /// Name for a option
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Type of a option
        /// </summary>
        public Type Type { get; private set; }

        public OptionDescription(string name, Type type)
            : this()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name");
            }

            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            this.Name = name;
            this.Type = type;
        }

        public override bool Equals(object obj)
        {
            if (obj is OptionDescription)
            {
                var other = (OptionDescription)obj;
                return Equals(other);
            }

            return false;
        }

        public bool Equals(OptionDescription other)
        {
            return other.Name == this.Name &&
                   other.Type.Equals(this.Type);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Name, this.Type.GetHashCode());
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", this.Type, this.Name);
        }

        public static bool operator ==(OptionDescription left, OptionDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OptionDescription left, OptionDescription right)
        {
            return !(left == right);
        }
    }
}
