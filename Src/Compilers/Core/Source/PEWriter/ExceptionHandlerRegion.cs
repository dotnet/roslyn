// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly uint tryStartOffset;
        private readonly uint tryEndOffset;
        private readonly uint handlerStartOffset;
        private readonly uint handlerEndOffset;

        public ExceptionHandlerRegion(
            uint tryStartOffset,
            uint tryEndOffset,
            uint handlerStartOffset,
            uint handlerEndOffset)
        {
            Debug.Assert(tryStartOffset < tryEndOffset);
            Debug.Assert(tryEndOffset <= handlerStartOffset);
            Debug.Assert(handlerStartOffset < handlerEndOffset);

            this.tryStartOffset = tryStartOffset;
            this.tryEndOffset = tryEndOffset;
            this.handlerStartOffset = handlerStartOffset;
            this.handlerEndOffset = handlerEndOffset;
        }

        /// <summary>
        /// Handler kind for this SEH info
        /// </summary>
        public abstract ExceptionRegionKind HandlerKind { get; }

        /// <summary>
        /// If HandlerKind == HandlerKind.Catch, this is the type of expection to catch. If HandlerKind == HandlerKind.Filter, this is System.Object.
        /// Otherwise this is a Dummy.TypeReference.
        /// </summary>
        public virtual ITypeReference ExceptionType
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Label instruction corresponding to the start of filter decision block
        /// </summary>
        public virtual uint FilterDecisionStartOffset
        {
            get { return 0; }
        }

        /// <summary>
        /// Label instruction corresponding to the start of try block
        /// </summary>
        public uint TryStartOffset
        {
            get { return this.tryStartOffset; }
        }

        /// <summary>
        /// Label instruction corresponding to the end of try block
        /// </summary>
        public uint TryEndOffset
        {
            get { return this.tryEndOffset; }
        }

        /// <summary>
        /// Label instruction corresponding to the start of handler block
        /// </summary>
        public uint HandlerStartOffset
        {
            get { return this.handlerStartOffset; }
        }

        /// <summary>
        /// Label instruction corresponding to the end of handler block
        /// </summary>
        public uint HandlerEndOffset
        {
            get { return this.handlerEndOffset; }
        }
    }

    internal sealed class ExceptionHandlerRegionFinally : ExceptionHandlerRegion
    {
        public ExceptionHandlerRegionFinally(
            uint tryStartOffset,
            uint tryEndOffset,
            uint handlerStartOffset,
            uint handlerEndOffset)
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
            uint tryStartOffset,
            uint tryEndOffset,
            uint handlerStartOffset,
            uint handlerEndOffset)
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
        private readonly ITypeReference exceptionType;

        public ExceptionHandlerRegionCatch(
            uint tryStartOffset,
            uint tryEndOffset,
            uint handlerStartOffset,
            uint handlerEndOffset,
            ITypeReference exceptionType)
            : base(tryStartOffset, tryEndOffset, handlerStartOffset, handlerEndOffset)
        {
            this.exceptionType = exceptionType;
        }

        public override ExceptionRegionKind HandlerKind
        {
            get { return ExceptionRegionKind.Catch; }
        }

        public override ITypeReference ExceptionType
        {
            get { return exceptionType; }
        }
    }

    internal sealed class ExceptionHandlerRegionFilter : ExceptionHandlerRegion
    {
        private readonly uint filterDecisionStartOffset;

        public ExceptionHandlerRegionFilter(
            uint tryStartOffset,
            uint tryEndOffset,
            uint handlerStartOffset,
            uint handlerEndOffset,
            uint filterDecisionStartOffset)
            : base(tryStartOffset, tryEndOffset, handlerStartOffset, handlerEndOffset)
        {
            this.filterDecisionStartOffset = filterDecisionStartOffset;
        }

        public override ExceptionRegionKind HandlerKind
        {
            get { return ExceptionRegionKind.Filter; }
        }

        public override uint FilterDecisionStartOffset
        {
            get { return filterDecisionStartOffset; }
        }
    }
}
