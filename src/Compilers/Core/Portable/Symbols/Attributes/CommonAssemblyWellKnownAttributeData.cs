// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from well-known custom attributes applied on an assembly.
    /// </summary>
    internal class CommonAssemblyWellKnownAttributeData<TNamedTypeSymbol> : WellKnownAttributeData, ISecurityAttributeTarget
    {
        #region AssemblySignatureKeyAttributeSetting
        private string _assemblySignatureKeyAttributeSetting;
        public string AssemblySignatureKeyAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblySignatureKeyAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblySignatureKeyAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyDelaySignAttributeSetting
        private ThreeState _assemblyDelaySignAttributeSetting;
        public ThreeState AssemblyDelaySignAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyDelaySignAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyDelaySignAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyKeyFileAttributeSetting
        private string _assemblyKeyFileAttributeSetting = StringMissingValue;
        public string AssemblyKeyFileAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyKeyFileAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyKeyFileAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyKeyContainerAttributeSetting
        private string _assemblyKeyContainerAttributeSetting = StringMissingValue;
        public string AssemblyKeyContainerAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyKeyContainerAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyKeyContainerAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyVersionAttributeSetting
        private Version _assemblyVersionAttributeSetting;

        /// <summary>
        /// Raw assembly version as specified in the AssemblyVersionAttribute, or Nothing if none specified.
        /// If the string passed to AssemblyVersionAttribute contains * the version build and/or revision numbers are set to <see cref="ushort.MaxValue"/>.
        /// </summary>
        public Version AssemblyVersionAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyVersionAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyVersionAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyFileVersionAttributeSetting
        private string _assemblyFileVersionAttributeSetting;
        public string AssemblyFileVersionAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyFileVersionAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyFileVersionAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyTitleAttributeSetting
        private string _assemblyTitleAttributeSetting;
        public string AssemblyTitleAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyTitleAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyTitleAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyDescriptionAttributeSetting
        private string _assemblyDescriptionAttributeSetting;
        public string AssemblyDescriptionAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyDescriptionAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyDescriptionAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyCultureAttributeSetting
        private string _assemblyCultureAttributeSetting;
        public string AssemblyCultureAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyCultureAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyCultureAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyCompanyAttributeSetting
        private string _assemblyCompanyAttributeSetting;
        public string AssemblyCompanyAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyCompanyAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyCompanyAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyProductAttributeSetting
        private string _assemblyProductAttributeSetting;
        public string AssemblyProductAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyProductAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyProductAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyInformationalVersionAttributeSetting
        private string _assemblyInformationalVersionAttributeSetting;
        public string AssemblyInformationalVersionAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyInformationalVersionAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyInformationalVersionAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyCopyrightAttributeSetting
        private string _assemblyCopyrightAttributeSetting;
        public string AssemblyCopyrightAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyCopyrightAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyCopyrightAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyTrademarkAttributeSetting
        private string _assemblyTrademarkAttributeSetting;
        public string AssemblyTrademarkAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyTrademarkAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyTrademarkAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyFlagsAttributeSetting
        private AssemblyFlags _assemblyFlagsAttributeSetting;
        public AssemblyFlags AssemblyFlagsAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyFlagsAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyFlagsAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region AssemblyAlgorithmIdAttribute
        private AssemblyHashAlgorithm? _assemblyAlgorithmIdAttributeSetting;
        public AssemblyHashAlgorithm? AssemblyAlgorithmIdAttributeSetting
        {
            get
            {
                VerifySealed(expected: true);
                return _assemblyAlgorithmIdAttributeSetting;
            }
            set
            {
                VerifySealed(expected: false);
                _assemblyAlgorithmIdAttributeSetting = value;
                SetDataStored();
            }
        }
        #endregion

        #region CompilationRelaxationsAttribute
        private bool _hasCompilationRelaxationsAttribute;
        public bool HasCompilationRelaxationsAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasCompilationRelaxationsAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasCompilationRelaxationsAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region ReferenceAssemblyAttribute
        private bool _hasReferenceAssemblyAttribute;
        public bool HasReferenceAssemblyAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasReferenceAssemblyAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasReferenceAssemblyAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region RuntimeCompatibilityAttribute

        private bool? _runtimeCompatibilityWrapNonExceptionThrows;

        // By default WrapNonExceptionThrows is considered to be true.
        internal const bool WrapNonExceptionThrowsDefault = true;

        public bool HasRuntimeCompatibilityAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _runtimeCompatibilityWrapNonExceptionThrows.HasValue;
            }
        }

        public bool RuntimeCompatibilityWrapNonExceptionThrows
        {
            get
            {
                VerifySealed(expected: true);

                return _runtimeCompatibilityWrapNonExceptionThrows ?? WrapNonExceptionThrowsDefault;
            }
            set
            {
                VerifySealed(expected: false);
                _runtimeCompatibilityWrapNonExceptionThrows = value;
                SetDataStored();
            }
        }

        #endregion

        #region DebuggableAttribute
        private bool _hasDebuggableAttribute;
        public bool HasDebuggableAttribute
        {
            get
            {
                VerifySealed(expected: true);
                return _hasDebuggableAttribute;
            }
            set
            {
                VerifySealed(expected: false);
                _hasDebuggableAttribute = value;
                SetDataStored();
            }
        }
        #endregion

        #region Security Attributes
        private SecurityWellKnownAttributeData _lazySecurityAttributeData;

        SecurityWellKnownAttributeData ISecurityAttributeTarget.GetOrCreateData()
        {
            VerifySealed(expected: false);

            if (_lazySecurityAttributeData == null)
            {
                _lazySecurityAttributeData = new SecurityWellKnownAttributeData();
                SetDataStored();
            }

            return _lazySecurityAttributeData;
        }

        /// <summary>
        /// Returns data decoded from security attributes or null if there are no security attributes.
        /// </summary>
        public SecurityWellKnownAttributeData SecurityInformation
        {
            get
            {
                VerifySealed(expected: true);
                return _lazySecurityAttributeData;
            }
        }
        #endregion

        #region ForwardedTypes

        private HashSet<TNamedTypeSymbol> _forwardedTypes;
        public HashSet<TNamedTypeSymbol> ForwardedTypes
        {
            get
            {
                return _forwardedTypes;
            }
            set
            {
                VerifySealed(expected: false);
                _forwardedTypes = value;
                SetDataStored();
            }
        }
        #endregion

        #region ExperimentalAttribute
        private ObsoleteAttributeData _experimentalAttributeData = ObsoleteAttributeData.Uninitialized;
        public ObsoleteAttributeData ExperimentalAttributeData
        {
            get
            {
                VerifySealed(expected: true);
                return _experimentalAttributeData.IsUninitialized ? null : _experimentalAttributeData;
            }
            set
            {
                VerifySealed(expected: false);
                Debug.Assert(value != null);
                Debug.Assert(!value.IsUninitialized);
                Debug.Assert(value.Kind == ObsoleteAttributeKind.Experimental);

                _experimentalAttributeData = value;
                SetDataStored();
            }
        }
        #endregion
    }
}
