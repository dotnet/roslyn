// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics;
using System.Reflection.Metadata;

namespace Microsoft.Cci
{
    /// <summary>
    /// A region representing an exception handler clause. The region exposes the type (catch or
    /// finally) and the bounds of the try block and catch or finally block as needed by 
    /// </summary>
    internal abstract class ExceptionHandlerRegion
    {
        /// <summary>
        /// Label instruction corresponding to the start of try block
        /// </summary>
        public int TryStartOffset { get; }

        /// <summary>
        /// Label instruction corresponding to the end of try block
        /// </summary>
        public int TryEndOffset { get; }

        /// <summary>
        /// Label instruction corresponding to the start of handler block
        /// </summary>
        public int HandlerStartOffset { get; }

        /// <summary>
        /// Label instruction corresponding to the end of handler block
        /// </summary>
        public int HandlerEndOffset { get; }

        public ExceptionHandlerRegion(
            int tryStartOffset,
            int tryEndOffset,
            int handlerStartOffset,
            int handlerEndOffset)
        {
            Debug.Assert(tryStartOffset < tryEndOffset);
            Debug.Assert(tryEndOffset <= handlerStartOffset);
            Debug.Assert(handlerStartOffset < handlerEndOffset);
            Debug.Assert(tryStartOffset >= 0);
            Debug.Assert(tryEndOffset >= 0);
            Debug.Assert(handlerStartOffset >= 0);
            Debug.Assert(handlerEndOffset >= 0);

            TryStartOffset = tryStartOffset;
            TryEndOffset = tryEndOffset;
            HandlerStartOffset = handlerStartOffset;
            HandlerEndOffset = handlerEndOffset;
        }

        public int HandlerLength => HandlerEndOffset - HandlerStartOffset;
        public int TryLength => TryEndOffset - TryStartOffset;

        /// <summary>
        /// Handler kind for this SEH info
        /// </summary>
        public abstract ExceptionRegionKind HandlerKind { get; }

        /// <summary>
        /// If HandlerKind == HandlerKind.Catch, this is the type of exception to catch. If HandlerKind == HandlerKind.Filter, this is System.Object.
        /// Otherwise this is a Dummy.TypeReference.
        /// </summary>
        public virtual ITypeReference? ExceptionType
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Label instruction corresponding to the start of filter decision block
        /// </summary>
        public virtual int FilterDecisionStartOffset
        {
            get { return 0; }
        }
    }

    internal sealed class ExceptionHandlerRegionFinally : ExceptionHandlerRegion
    {
        public ExceptionHandlerRegionFinally(
            int tryStartOffset,
            int tryEndOffset,
            int handlerStartOffset,
            int handlerEndOffset)
            : base(tryStartOffset, tryEndOffset, handlerStartOffset, handlerEndOffset)
        {
        }

        public override ExceptionRegionKind HandlerKind
        {
            get { return ExceptionRegionKind.Finally; }
        }
    }

    internal sealed class ExceptionHandlerRegionFault : ExceptionHandlerRegion
    {
        public ExceptionHandlerRegionFault(
            int tryStartOffset,
            int tryEndOffset,
            int handlerStartOffset,
            int handlerEndOffset)
            : base(tryStartOffset, tryEndOffset, handlerStartOffset, handlerEndOffset)
        {
        }

        public override ExceptionRegionKind HandlerKind
        {
            get { return ExceptionRegionKind.Fault; }
        }
    }

    internal sealed class ExceptionHandlerRegionCatch : ExceptionHandlerRegion
    {
        private readonly ITypeReference _exceptionType;

        public ExceptionHandlerRegionCatch(
            int tryStartOffset,
            int tryEndOffset,
            int handlerStartOffset,
            int handlerEndOffset,
            ITypeReference exceptionType)
            : base(tryStartOffset, tryEndOffset, handlerStartOffset, handlerEndOffset)
        {
            _exceptionType = exceptionType;
        }

        public override ExceptionRegionKind HandlerKind
        {
            get { return ExceptionRegionKind.Catch; }
        }

        public override ITypeReference ExceptionType
        {
            get { return _exceptionType; }
        }
    }

    internal sealed class ExceptionHandlerRegionFilter : ExceptionHandlerRegion
    {
        private readonly int _filterDecisionStartOffset;

        public ExceptionHandlerRegionFilter(
            int tryStartOffset,
            int tryEndOffset,
            int handlerStartOffset,
            int handlerEndOffset,
            int filterDecisionStartOffset)
            : base(tryStartOffset, tryEndOffset, handlerStartOffset, handlerEndOffset)
        {
            Debug.Assert(filterDecisionStartOffset >= 0);

            _filterDecisionStartOffset = filterDecisionStartOffset;
        }

        public override ExceptionRegionKind HandlerKind
        {
            get { return ExceptionRegionKind.Filter; }
        }

        public override int FilterDecisionStartOffset
        {
            get { return _filterDecisionStartOffset; }
        }
    }
}
