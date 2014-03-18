// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on an assembly.
    /// </summary>
    internal class CommonAssemblyWellKnownAttributeData<TNamedTypeSymbol> : WellKnownAttributeData, ISecurityAttributeTarget
    {
        #region AssemblySignatureKeyAttributeSetting
        private string assemblySignatureKeyAttributeSetting;
        public string AssemblySignatureKeyAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblySignatureKeyAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblySignatureKeyAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyDelaySignAttributeSetting
        private ThreeState assemblyDelaySignAttributeSetting;
        public ThreeState AssemblyDelaySignAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyDelaySignAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyDelaySignAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyKeyFileAttributeSetting
        private string assemblyKeyFileAttributeSetting = StringMissingValue;
        public string AssemblyKeyFileAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyKeyFileAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyKeyFileAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyKeyContainerAttributeSetting
        private string assemblyKeyContainerAttributeSetting = StringMissingValue;
        public string AssemblyKeyContainerAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyKeyContainerAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyKeyContainerAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyVersionAttributeSetting
        private Version assemblyVersionAttributeSetting;
        public Version AssemblyVersionAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyVersionAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyVersionAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyFileVersionAttributeSetting
        private string assemblyFileVersionAttributeSetting;
        public string AssemblyFileVersionAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyFileVersionAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyFileVersionAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyTitleAttributeSetting
        private string assemblyTitleAttributeSetting;
        public string AssemblyTitleAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyTitleAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyTitleAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyDescriptionAttributeSetting
        private string assemblyDescriptionAttributeSetting;
        public string AssemblyDescriptionAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyDescriptionAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyDescriptionAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyCultureAttributeSetting
        private string assemblyCultureAttributeSetting;
        public string AssemblyCultureAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyCultureAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyCultureAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyCompanyAttributeSetting
        private string assemblyCompanyAttributeSetting;
        public string AssemblyCompanyAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyCompanyAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyCompanyAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyProductAttributeSetting
        private string assemblyProductAttributeSetting;
        public string AssemblyProductAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyProductAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyProductAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyInformationalVersionAttributeSetting
        private string assemblyInformationalVersionAttributeSetting;
        public string AssemblyInformationalVersionAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyInformationalVersionAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyInformationalVersionAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyCopyrightAttributeSetting
        private string assemblyCopyrightAttributeSetting;
        public string AssemblyCopyrightAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyCopyrightAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyCopyrightAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyTrademarkAttributeSetting
        private string assemblyTrademarkAttributeSetting;
        public string AssemblyTrademarkAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyTrademarkAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyTrademarkAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyFlagsAttributeSetting
        private AssemblyNameFlags assemblyFlagsAttributeSetting;
        public AssemblyNameFlags AssemblyFlagsAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyFlagsAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyFlagsAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyAlgorithmIdAttribute
        private AssemblyHashAlgorithm? assemblyAlgorithmIdAttributeSetting;
        public AssemblyHashAlgorithm? AssemblyAlgorithmIdAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return this.assemblyAlgorithmIdAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                this.assemblyAlgorithmIdAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region CompilationRelaxationsAttribute
        private bool hasCompilationRelaxationsAttribute;
        public bool HasCompilationRelaxationsAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasCompilationRelaxationsAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasCompilationRelaxationsAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region RuntimeCompatibilityAttribute

        private bool? runtimeCompatibilityWrapNonExceptionThrows;

        // By default WrapNonExceptionThrows is considered to be true.
        internal const bool WrapNonExceptionThrowsDefault = true;

        public bool HasRuntimeCompatibilityAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return runtimeCompatibilityWrapNonExceptionThrows.HasValue;
            }
        }

        public bool RuntimeCompatibilityWrapNonExceptionThrows
        {
            get
            {
                VerifySealed(expected: true);

                return this.runtimeCompatibilityWrapNonExceptionThrows ?? WrapNonExceptionThrowsDefault;
            }
            set
            {
                VerifySealed(expected: false);
                this.runtimeCompatibilityWrapNonExceptionThrows = value;
                SetDataStored();
            }
        }

        #endregion

        #region DebuggableAttribute
        private bool hasDebuggableAttribute;
        public bool HasDebuggableAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return this.hasDebuggableAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                this.hasDebuggableAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region Security Attributes
        private SecurityWellKnownAttributeData lazySecurityAttributeData;

        SecurityWellKnownAttributeData ISecurityAttributeTarget.GetOrCreateData()
        {
            VerifySealed(expected: false);

            if (lazySecurityAttributeData == null)
            {
                lazySecurityAttributeData = new SecurityWellKnownAttributeData();
                SetDataStored();
            }

            return lazySecurityAttributeData;
        }

        /// <summary>
        /// Returns data decoded from security attributes or null if there are no security attributes.
        /// </summary>
        public SecurityWellKnownAttributeData SecurityInformation
        {
            get
            {
                VerifySealed(expected: true);
                return lazySecurityAttributeData;
            }
        }
        #endregion

        #region ForwardedTypes

        private HashSet<TNamedTypeSymbol> forwardedTypes;
        public HashSet<TNamedTypeSymbol> ForwardedTypes
        {
            get
            {
                return this.forwardedTypes;
            }
            set
            {
                VerifySealed(expected: false);
                this.forwardedTypes = value;
                SetDataStored();
            }
        }
        #endregion
    }
}