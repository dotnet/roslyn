// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Scripting
{
    using static ParameterValidationHelpers;

    public sealed class ScriptSourceResolver : SourceFileResolver, IEquatable<ScriptSourceResolver>
    {
        public static new ScriptSourceResolver Default { get; } = new ScriptSourceResolver(ImmutableArray<string>.Empty, null);

        private ScriptSourceResolver(ImmutableArray<string> sourcePaths, string baseDirectory)
            : base(sourcePaths, baseDirectory)
        {
        }

        public ScriptSourceResolver WithSearchPaths(params string[] searchPaths)
            => WithSearchPaths(searchPaths.AsImmutableOrEmpty());

        public ScriptSourceResolver WithSearchPaths(IEnumerable<string> searchPaths)
            => WithSearchPaths(searchPaths.AsImmutableOrEmpty());

        public ScriptSourceResolver WithSearchPaths(ImmutableArray<string> searchPaths)
        {
            if (SearchPaths == searchPaths)
            {
                return this;
            }

            return new ScriptSourceResolver(ToImmutableArrayChecked(searchPaths, nameof(searchPaths)), BaseDirectory);
        }

        public ScriptSourceResolver WithBaseDirectory(string baseDirectory)
        {
            if (BaseDirectory == baseDirectory)
            {
                return this;
            }

            // absolute path check is done in the base class

            return new ScriptSourceResolver(SearchPaths, baseDirectory);
        }

        public bool Equals(ScriptSourceResolver other) => base.Equals(other);
        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object obj) => base.Equals(obj);
    }
}
