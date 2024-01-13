// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a bag of custom attributes and the associated decoded well-known attribute data.
    /// </summary>
    internal sealed class CustomAttributesBag<T>
        where T : AttributeData
    {
        private ImmutableArray<T> _customAttributes;
        private WellKnownAttributeData _decodedWellKnownAttributeData;
        private EarlyWellKnownAttributeData _earlyDecodedWellKnownAttributeData;
        private int _state;

        /// <summary>
        /// Instance representing sealed custom attribute bag with no attributes.
        /// </summary>
        public static readonly CustomAttributesBag<T> Empty = new CustomAttributesBag<T>(CustomAttributeBagCompletionPart.All, ImmutableArray<T>.Empty);

        private CustomAttributesBag(CustomAttributeBagCompletionPart part, ImmutableArray<T> customAttributes)
        {
            _customAttributes = customAttributes;
            this.NotePartComplete(part);
        }

        public CustomAttributesBag()
            : this(CustomAttributeBagCompletionPart.None, default(ImmutableArray<T>))
        {
        }

        /// <summary>
        /// Returns a non-sealed custom attribute bag with null initialized <see cref="_earlyDecodedWellKnownAttributeData"/>, null initialized <see cref="_decodedWellKnownAttributeData"/> and uninitialized <see cref="_customAttributes"/>.
        /// </summary>
        public static CustomAttributesBag<T> WithEmptyData()
        {
            return new CustomAttributesBag<T>(CustomAttributeBagCompletionPart.EarlyDecodedWellKnownAttributeData | CustomAttributeBagCompletionPart.DecodedWellKnownAttributeData, default(ImmutableArray<T>));
        }

        public bool IsEmpty
        {
            get
            {
                return
                    this.IsSealed &&
                    _customAttributes.IsEmpty &&
                    _decodedWellKnownAttributeData == null &&
                    _earlyDecodedWellKnownAttributeData == null;
            }
        }

        /// <summary>
        /// Sets the early decoded well-known attribute data on the bag in a thread safe manner.
        /// Stored early decoded data is immutable and cannot be updated further.
        /// </summary>
        /// <returns>Returns true if early decoded data were stored into the bag on this thread.</returns>
        public bool SetEarlyDecodedWellKnownAttributeData(EarlyWellKnownAttributeData data)
        {
            WellKnownAttributeData.Seal(data);
            // Early decode must complete before full decode
            Debug.Assert(!IsPartComplete(CustomAttributeBagCompletionPart.DecodedWellKnownAttributeData) || IsPartComplete(CustomAttributeBagCompletionPart.EarlyDecodedWellKnownAttributeData));
            var setOnOurThread = Interlocked.CompareExchange(ref _earlyDecodedWellKnownAttributeData, data, null) == null;
            NotePartComplete(CustomAttributeBagCompletionPart.EarlyDecodedWellKnownAttributeData);
            return setOnOurThread;
        }

        /// <summary>
        /// Sets the decoded well-known attribute data (except the early data) on the bag in a thread safe manner. 
        /// Stored decoded data is immutable and cannot be updated further.
        /// </summary>
        /// <returns>Returns true if decoded data were stored into the bag on this thread.</returns>
        public bool SetDecodedWellKnownAttributeData(WellKnownAttributeData data)
        {
            WellKnownAttributeData.Seal(data);
            // Early decode must complete before full decode
            Debug.Assert(IsPartComplete(CustomAttributeBagCompletionPart.EarlyDecodedWellKnownAttributeData));
            var setOnOurThread = Interlocked.CompareExchange(ref _decodedWellKnownAttributeData, data, null) == null;
            NotePartComplete(CustomAttributeBagCompletionPart.DecodedWellKnownAttributeData);
            return setOnOurThread;
        }

        /// <summary>
        /// Sets the bound attributes on the bag in a thread safe manner.
        /// If store succeeds, it seals the bag and makes the bag immutable.
        /// </summary>
        /// <returns>Returns true if bound attributes were stored into the bag on this thread.</returns>
        public bool SetAttributes(ImmutableArray<T> newCustomAttributes)
        {
            Debug.Assert(!newCustomAttributes.IsDefault);
            var setOnOurThread = ImmutableInterlocked.InterlockedCompareExchange(ref _customAttributes, newCustomAttributes, default(ImmutableArray<T>)) == default(ImmutableArray<T>);
            NotePartComplete(CustomAttributeBagCompletionPart.Attributes);
            return setOnOurThread;
        }

        /// <summary>
        /// Gets the stored bound attributes in the bag.
        /// </summary>
        /// <remarks>This property can only be accessed on a sealed bag.</remarks>
        public ImmutableArray<T> Attributes
        {
            get
            {
                Debug.Assert(IsPartComplete(CustomAttributeBagCompletionPart.Attributes));
                Debug.Assert(!_customAttributes.IsDefault);
                return _customAttributes;
            }
        }

        /// <summary>
        /// Gets the decoded well-known attribute data (except the early data) in the bag. 
        /// </summary>
        /// <remarks>This property can only be accessed on the bag after <see cref="SetDecodedWellKnownAttributeData"/> has been invoked.</remarks>
        public WellKnownAttributeData DecodedWellKnownAttributeData
        {
            get
            {
                Debug.Assert(IsPartComplete(CustomAttributeBagCompletionPart.DecodedWellKnownAttributeData));
                return _decodedWellKnownAttributeData;
            }
        }

        /// <summary>
        /// Gets the early decoded well-known attribute data in the bag. 
        /// </summary>
        /// <remarks>This property can only be accessed on the bag after <see cref="SetEarlyDecodedWellKnownAttributeData"/> has been invoked.</remarks>
        public EarlyWellKnownAttributeData EarlyDecodedWellKnownAttributeData
        {
            get
            {
                Debug.Assert(IsPartComplete(CustomAttributeBagCompletionPart.EarlyDecodedWellKnownAttributeData));
                return _earlyDecodedWellKnownAttributeData;
            }
        }

        private CustomAttributeBagCompletionPart State
        {
            get
            {
                return (CustomAttributeBagCompletionPart)_state;
            }
            set
            {
                _state = (int)value;
            }
        }

        private void NotePartComplete(CustomAttributeBagCompletionPart part)
        {
            ThreadSafeFlagOperations.Set(ref _state, (int)(this.State | part));
        }

        internal bool IsPartComplete(CustomAttributeBagCompletionPart part)
        {
            return (this.State & part) == part;
        }

        internal bool IsSealed
        {
            get { return IsPartComplete(CustomAttributeBagCompletionPart.All); }
        }

        /// <summary>
        /// Return whether early decoded attribute data has been computed and stored on the bag and it is safe to access <see cref="EarlyDecodedWellKnownAttributeData"/> from this bag.
        /// Return value of true doesn't guarantee that bound attributes or remaining decoded attribute data has also been initialized.
        /// </summary>
        internal bool IsEarlyDecodedWellKnownAttributeDataComputed
        {
            get { return IsPartComplete(CustomAttributeBagCompletionPart.EarlyDecodedWellKnownAttributeData); }
        }

        /// <summary>
        /// Return whether all decoded attribute data has been computed and stored on the bag and it is safe to access <see cref="DecodedWellKnownAttributeData"/> from this bag.
        /// Return value of true doesn't guarantee that bound attributes have also been initialized.
        /// </summary>
        internal bool IsDecodedWellKnownAttributeDataComputed
        {
            get { return IsPartComplete(CustomAttributeBagCompletionPart.DecodedWellKnownAttributeData); }
        }

        /// <summary>
        /// Enum representing the current state of attribute binding/decoding for a corresponding CustomAttributeBag.
        /// </summary>
        [Flags]
        internal enum CustomAttributeBagCompletionPart : byte
        {
            /// <summary>
            /// Bag has been created, but no decoded data or attributes have been stored.
            /// CustomAttributeBag is in this state during early decoding phase.
            /// </summary>
            None = 0,

            /// <summary>
            /// Early decoded attribute data has been computed and stored on the bag, but bound attributes or remaining decoded attribute data is not stored.
            /// Only <see cref="EarlyDecodedWellKnownAttributeData"/> can be accessed from this bag.
            /// </summary>
            EarlyDecodedWellKnownAttributeData = 1 << 0,

            /// <summary>
            /// All decoded attribute data has been computed and stored on the bag, but bound attributes are not yet stored.
            /// Both <see cref="EarlyDecodedWellKnownAttributeData"/> and <see cref="DecodedWellKnownAttributeData"/> can be accessed from this bag.
            /// </summary>
            DecodedWellKnownAttributeData = 1 << 1,

            /// <summary>
            /// Bound attributes have been computed and stored on this bag.
            /// </summary>
            Attributes = 1 << 2,

            /// <summary>
            /// CustomAttributeBag is completely initialized and immutable.
            /// </summary>
            All = EarlyDecodedWellKnownAttributeData | DecodedWellKnownAttributeData | Attributes,
        }
    }
}
