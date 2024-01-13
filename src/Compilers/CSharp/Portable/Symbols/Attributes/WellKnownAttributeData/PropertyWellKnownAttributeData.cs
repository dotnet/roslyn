// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a property.
    /// </summary>
    internal sealed class PropertyWellKnownAttributeData : CommonPropertyWellKnownAttributeData, ISkipLocalsInitAttributeTarget, IMemberNotNullAttributeTarget
    {
        private bool _hasDisallowNullAttribute;
        public bool HasDisallowNullAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasDisallowNullAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasDisallowNullAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasAllowNullAttribute;
        public bool HasAllowNullAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasAllowNullAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasAllowNullAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasMaybeNullAttribute;
        public bool HasMaybeNullAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasMaybeNullAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasMaybeNullAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasNotNullAttribute;
        public bool HasNotNullAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasNotNullAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasNotNullAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasSkipLocalsInitAttribute;
        public bool HasSkipLocalsInitAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasSkipLocalsInitAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasSkipLocalsInitAttribute = value;
                SetDataStored();
            }
        }

        private bool _hasUnscopedRefAttribute;
        public bool HasUnscopedRefAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasUnscopedRefAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasUnscopedRefAttribute = value;
                SetDataStored();
            }
        }

        private ImmutableArray<string> _memberNotNullAttributeData = ImmutableArray<string>.Empty;

        public void AddNotNullMember(string memberName)
        {
            VerifySealed(expected: false);
            _memberNotNullAttributeData = _memberNotNullAttributeData.Add(memberName);
            SetDataStored();
        }

        public void AddNotNullMember(ArrayBuilder<string> memberNames)
        {
            VerifySealed(expected: false);
            _memberNotNullAttributeData = _memberNotNullAttributeData.AddRange(memberNames);
            SetDataStored();
        }

        public ImmutableArray<string> NotNullMembers
        {
            get
            {
                VerifySealed(expected: true);
                return _memberNotNullAttributeData;
            }
        }

        private ImmutableArray<string> _memberNotNullWhenTrueAttributeData = ImmutableArray<string>.Empty;
        private ImmutableArray<string> _memberNotNullWhenFalseAttributeData = ImmutableArray<string>.Empty;

        public void AddNotNullWhenMember(bool sense, string memberName)
        {
            VerifySealed(expected: false);
            if (sense)
            {
                _memberNotNullWhenTrueAttributeData = _memberNotNullWhenTrueAttributeData.Add(memberName);
            }
            else
            {
                _memberNotNullWhenFalseAttributeData = _memberNotNullWhenFalseAttributeData.Add(memberName);
            }
            SetDataStored();
        }

        public void AddNotNullWhenMember(bool sense, ArrayBuilder<string> memberNames)
        {
            VerifySealed(expected: false);
            if (sense)
            {
                _memberNotNullWhenTrueAttributeData = _memberNotNullWhenTrueAttributeData.AddRange(memberNames);
            }
            else
            {
                _memberNotNullWhenFalseAttributeData = _memberNotNullWhenFalseAttributeData.AddRange(memberNames);
            }
            SetDataStored();
        }

        public ImmutableArray<string> NotNullWhenTrueMembers
        {
            get
            {
                VerifySealed(expected: true);
                return _memberNotNullWhenTrueAttributeData;
            }
        }

        public ImmutableArray<string> NotNullWhenFalseMembers
        {
            get
            {
                VerifySealed(expected: true);
                return _memberNotNullWhenFalseAttributeData;
            }
        }
    }
}
