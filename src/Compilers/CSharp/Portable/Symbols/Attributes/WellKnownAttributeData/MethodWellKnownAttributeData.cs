// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on a method.
    /// </summary>
    internal sealed class MethodWellKnownAttributeData : CommonMethodWellKnownAttributeData, ISkipLocalsInitAttributeTarget, IMemberNotNullAttributeTarget
    {
        private bool _hasDoesNotReturnAttribute;
        public bool HasDoesNotReturnAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasDoesNotReturnAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasDoesNotReturnAttribute = value;
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

        private bool _hasRequiresUnsafeAttribute;
        public bool HasRequiresUnsafeAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasRequiresUnsafeAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasRequiresUnsafeAttribute = value;
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

        private UnmanagedCallersOnlyAttributeData? _unmanagedCallersOnlyAttributeData;
        public UnmanagedCallersOnlyAttributeData? UnmanagedCallersOnlyAttributeData
        {
            get
            {
                VerifySealed(expected: true);
                return _unmanagedCallersOnlyAttributeData;
            }
            set
            {
                VerifySealed(expected: false);
                _unmanagedCallersOnlyAttributeData = value;
                SetDataStored();
            }
        }

        private ThreeState _runtimeAsyncMethodGenerationSetting;
        public ThreeState RuntimeAsyncMethodGenerationSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _runtimeAsyncMethodGenerationSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _runtimeAsyncMethodGenerationSetting = value;
                SetDataStored();
            }
        }
    }
}
