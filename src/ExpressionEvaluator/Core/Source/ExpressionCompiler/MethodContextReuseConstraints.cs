// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct MethodContextReuseConstraints
    {
        private readonly Guid _moduleVersionId;
        private readonly int _methodToken;
        private readonly int _methodVersion;
        private readonly uint _startOffset;
        private readonly uint _endOffsetExclusive;

        internal MethodContextReuseConstraints(Guid moduleVersionId, int methodToken, int methodVersion)
            : this(moduleVersionId, methodToken, methodVersion, 0, uint.MaxValue)
        {
        }

        internal MethodContextReuseConstraints(Guid moduleVersionId, int methodToken, int methodVersion, uint startOffset, uint endOffsetExclusive)
        {
            Debug.Assert(moduleVersionId != default(Guid));
            Debug.Assert(MetadataTokens.Handle(methodToken).Kind == HandleKind.MethodDefinition);
            Debug.Assert(methodVersion >= 1);
            Debug.Assert(startOffset <= endOffsetExclusive);

            _moduleVersionId = moduleVersionId;
            _methodToken = methodToken;
            _methodVersion = methodVersion;
            _startOffset = startOffset;
            _endOffsetExclusive = endOffsetExclusive;
        }

        public bool AreSatisfied(Guid moduleVersionId, int methodToken, int methodVersion, int ilOffset)
        {
            Debug.Assert(moduleVersionId != default(Guid));
            Debug.Assert(MetadataTokens.Handle(methodToken).Kind == HandleKind.MethodDefinition);
            Debug.Assert(methodVersion >= 1);
            Debug.Assert(ilOffset >= 0);

            return moduleVersionId == _moduleVersionId &&
                methodToken == _methodToken &&
                methodVersion == _methodVersion &&
                ilOffset >= _startOffset &&
                ilOffset < _endOffsetExclusive;
        }

        internal bool HasExpectedSpan(uint startOffset, uint endOffsetExclusive)
        {
            return _startOffset == startOffset && _endOffsetExclusive == endOffsetExclusive;
        }

        public override string ToString()
        {
            return $"0x{_methodToken:x8}v{_methodVersion} from {_moduleVersionId} [{_startOffset}, {_endOffsetExclusive})";
        }

        public class Builder
        {
            private readonly Guid _moduleVersionId;
            private readonly int _methodToken;
            private readonly int _methodVersion;
            private readonly int _ilOffset;

            private uint _startOffset;
            private uint _endOffsetExclusive;

            public Builder(Guid moduleVersionId, int methodToken, int methodVersion, int ilOffset)
            {
                Debug.Assert(moduleVersionId != default(Guid));
                Debug.Assert(MetadataTokens.Handle(methodToken).Kind == HandleKind.MethodDefinition);
                Debug.Assert(methodVersion >= 1);
                Debug.Assert(ilOffset >= 0);

                _moduleVersionId = moduleVersionId;
                _methodToken = methodToken;
                _methodVersion = methodVersion;
                _ilOffset = ilOffset;

                _startOffset = 0;
                _endOffsetExclusive = uint.MaxValue;
            }

            public Builder(MethodContextReuseConstraints existingConstraints, int ilOffset)
            {
                _moduleVersionId = existingConstraints._moduleVersionId;
                _methodToken = existingConstraints._methodToken;
                _methodVersion = existingConstraints._methodVersion;
                _ilOffset = ilOffset;

                _startOffset = existingConstraints._startOffset;
                _endOffsetExclusive = existingConstraints._endOffsetExclusive;
            }

            public void AddRange(uint startOffset, uint endOffsetExclusive)
            {
                Debug.Assert(startOffset >= 0);
                Debug.Assert(startOffset <= endOffsetExclusive);

                if (_ilOffset < startOffset)
                {
                    _endOffsetExclusive = Math.Min(_endOffsetExclusive, startOffset);
                }
                else if (_ilOffset >= endOffsetExclusive)
                {
                    _startOffset = Math.Max(_startOffset, endOffsetExclusive);
                }
                else
                {
                    _startOffset = Math.Max(_startOffset, startOffset);
                    _endOffsetExclusive = Math.Min(_endOffsetExclusive, endOffsetExclusive);
                }
            }

            public MethodContextReuseConstraints Build()
            {
                return new MethodContextReuseConstraints(
                    _moduleVersionId,
                    _methodToken,
                    _methodVersion,
                    _startOffset,
                    _endOffsetExclusive);
            }
        }
    }
}
