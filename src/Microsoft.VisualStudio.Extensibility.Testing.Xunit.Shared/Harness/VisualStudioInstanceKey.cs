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
            var semicolon1 = s.IndexOf(';');
            var semicolon2 = s.IndexOf(';', semicolon1 + 1);
            var semicolon3 = s.IndexOf(';', semicolon2 + 1);
            var semicolon4 = s.IndexOf(';', semicolon3 + 1);
            var environmentVariables = new string[0];

            if (semicolon4 > 0)
            {
                var environmentVariableCount = int.Parse(s.Substring(semicolon3 + 1, semicolon4 - semicolon3 - 1));
                Debug.Assert(environmentVariableCount > 0, $"Assertion failed: {nameof(environmentVariableCount)} > 0");

                environmentVariables = new string[environmentVariableCount];
                var currentStartSeparator = semicolon4;
                for (var i = 0; i < environmentVariableCount; i++)
                {
                    var nextSeparator = s.IndexOf(';', currentStartSeparator + 1);
                    var currentVariable = nextSeparator > 0
                        ? s.Substring(currentStartSeparator + 1, nextSeparator - currentStartSeparator - 1)
                        : s.Substring(currentStartSeparator + 1);
                    environmentVariables[i] = Encoding.UTF8.GetString(Convert.FromBase64String(currentVariable));
                    currentStartSeparator = nextSeparator;
                }
            }

            return new VisualStudioInstanceKey(
                version: (VisualStudioVersion)int.Parse(s.Substring(0, semicolon1)),
                rootSuffix: s.Substring(semicolon1 + 1, semicolon2 - semicolon1 - 1),
                maxAttempts: int.Parse(s.Substring(semicolon2 + 1, semicolon3 - semicolon2 - 1)),
                environmentVariables: environmentVariables);
        }
    }
}
