// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    [Serializable]
    public readonly struct VisualStudioInstanceKey : IEquatable<VisualStudioInstanceKey>
    {
        public static readonly VisualStudioInstanceKey Unspecified = new(VisualStudioVersion.Unspecified, rootSuffix: string.Empty, maxAttempts: 1, environmentVariables: new string[0]);

        public VisualStudioInstanceKey(VisualStudioVersion version, string rootSuffix, int maxAttempts, string[] environmentVariables)
        {
            Version = version;
            RootSuffix = rootSuffix;
            MaxAttempts = maxAttempts;
            EnvironmentVariables = environmentVariables;
        }

        public VisualStudioVersion Version { get; }

        public string RootSuffix { get; }

        public int MaxAttempts { get; }

        public IReadOnlyList<string> EnvironmentVariables { get; }

        public static bool operator ==(VisualStudioInstanceKey left, VisualStudioInstanceKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VisualStudioInstanceKey left, VisualStudioInstanceKey right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            return obj is VisualStudioInstanceKey other
                && Equals(other);
        }

        public bool Equals(VisualStudioInstanceKey other)
        {
            return Version == other.Version
                && RootSuffix == other.RootSuffix
                && MaxAttempts == other.MaxAttempts
                && EnvironmentVariables.SequenceEqual(other.EnvironmentVariables);
        }

        public override int GetHashCode()
        {
            var hashCode = 223772477;
            hashCode = (hashCode * -1521134295) + Version.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(RootSuffix);
            hashCode = (hashCode * -1521134295) + MaxAttempts.GetHashCode();
            hashCode = (hashCode * -1521134295) + EnvironmentVariables.Count.GetHashCode();
            return hashCode;
        }

        public string SerializeToString()
        {
            var builder = new StringBuilder();
            builder.Append(EnvironmentVariables.Count);
            foreach (var environmentVariable in EnvironmentVariables)
            {
                builder.Append(';');
                builder.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(environmentVariable)));
            }

            return $"{(int)Version};{RootSuffix};{MaxAttempts};{builder}";
        }

        public static VisualStudioInstanceKey DeserializeFromString(string s)
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            var elements = s.Split(new[] { ';' }, 5);
            var environmentVariables = new string[0];

            if (elements.Length >= 5)
            {
                var environmentVariableCount = int.Parse(elements[3]);
                Debug.Assert(environmentVariableCount > 0, $"Assertion failed: {nameof(environmentVariableCount)} > 0");

                environmentVariables = new string[environmentVariableCount];
                var currentStartSeparator = -1;
                for (var i = 0; i < environmentVariableCount; i++)
                {
                    var nextSeparator = elements[4].IndexOf(';', currentStartSeparator + 1);
                    var currentVariable = nextSeparator > 0
                        ? elements[4].Substring(currentStartSeparator + 1, nextSeparator - currentStartSeparator - 1)
                        : elements[4].Substring(currentStartSeparator + 1);
                    environmentVariables[i] = Encoding.UTF8.GetString(Convert.FromBase64String(currentVariable));
                    currentStartSeparator = nextSeparator;
                }
            }

            return new VisualStudioInstanceKey(
                version: (VisualStudioVersion)int.Parse(elements[0]),
                rootSuffix: elements[1],
                maxAttempts: int.Parse(elements[2]),
                environmentVariables: environmentVariables);
        }
    }
}
