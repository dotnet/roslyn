// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//////////////////////////////////////////////////////////////////////////////////////////////////////
// Note: This implementation is copied from $/DevDiv/Feature/VSPro_1/debugger/concord/Dispatcher/Managed/
//////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Watson
{
    /// <summary>
    /// Describes non-fatal exception from a given component.
    /// </summary>
    /// <remarks>
    /// This is intended to be used by callers of <see cref="WatsonErrorReport"/> to represent information about the non-fatal error.
    /// </remarks>
    internal class ExceptionInfo
    {
        protected ExceptionInfo()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="ExceptionInfo"/>
        /// </summary>
        /// <param name="exception">[Required] Exception that triggered this non-fatal error</param>
        /// <param name="implementationName">
        ///     [Required] Name of the component / implementation that triggered the error. 
        ///     This parameter is included in the watson bucket parameters to uniquely identify this error.
        /// </param>
        public ExceptionInfo(Exception exception, string implementationName)
        {
            Exception = exception;
            ImplementationName = implementationName;
        }

        /// <summary>
        /// Exception that triggered this non-fatal error
        /// </summary>
        public virtual Exception Exception
        {
            get;
            protected set;
        }

        /// <summary>
        /// The name of the component that triggered this error
        /// </summary>
        public virtual string ComponentName
        {
            get
            {
                return "Roslyn";
            }
        }

        /// <summary>
        /// The Fully qualified identifier to what triggered this error
        /// This is appended to the ModName parameter on the Watson bucket
        /// </summary>
        public virtual string ImplementationName
        {
            get;
            protected set;
        }
    }
}
