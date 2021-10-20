// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal readonly struct MethodContextReuseConstraints
    {
        private readonly Guid _moduleVersionId;
        private readonly int _methodToken;
        private readonly int _methodVersion;
        private readonly ILSpan _span;

        internal MethodContextReuseConstraints(Guid moduleVersionId, int methodToken, int methodVersion, ILSpan span)
        {
            Debug.Assert(moduleVersionId != Guid.Empty);
            Debug.Assert(MetadataTokens.Handle(methodToken).Kind == HandleKind.MethodDefinition);
            Debug.Assert(methodVersion >= 1);

            _moduleVersionId = moduleVersionId;
            _methodToken = methodToken;
            _methodVersion = methodVersion;
            _span = span;
        }

        public bool AreSatisfied(Guid moduleVersionId, int methodToken, int methodVersion, int ilOffset)
        {
            Debug.Assert(moduleVersionId != Guid.Empty);
            Debug.Assert(MetadataTokens.Handle(methodToken).Kind == HandleKind.MethodDefinition);
            Debug.Assert(methodVersion >= 1);
            Debug.Assert(ilOffset >= 0);

            return moduleVersionId == _moduleVersionId &&
                methodToken == _methodToken &&
                methodVersion == _methodVersion &&
                _span.Contains(ilOffset);
        }

        public override string ToString()
        {
            return $"0x{_methodToken:x8}v{_methodVersion} from {_moduleVersionId} {_span}";
        }

        /// <summary>
        /// Finds a span of IL containing the specified offset where local variables and imports are guaranteed to be the same.
        /// Examples:
        /// scopes: [   [   ) x [  )  )
        /// result:         [   )
        /// 
        /// scopes: [ x [   )   [  )  )
        /// result: [   )     
        /// 
        /// scopes: [   [ x )   [  )  )
        /// result:     [   )     
        /// </summary>
        public static ILSpan CalculateReuseSpan(int ilOffset, ILSpan initialSpan, IEnumerable<ILSpan> scopes)
        {
            Debug.Assert(ilOffset >= 0);

            uint _startOffset = initialSpan.StartOffset;
            uint _endOffsetExclusive = initialSpan.EndOffsetExclusive;

            foreach (ILSpan scope in scopes)
            {
                if (ilOffset < scope.StartOffset)
                {
                    _endOffsetExclusive = Math.Min(_endOffsetExclusive, scope.StartOffset);
                }
                else if (ilOffset >= scope.EndOffsetExclusive)
                {
                    _startOffset = Math.Max(_startOffset, scope.EndOffsetExclusive);
                }
                else
                {
                    _startOffset = Math.Max(_startOffset, scope.StartOffset);
                    _endOffsetExclusive = Math.Min(_endOffsetExclusive, scope.EndOffsetExclusive);
                }
            }

            return new ILSpan(_startOffset, _endOffsetExclusive);
        }
    }
}
