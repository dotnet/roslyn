// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information decoded from security attributes, i.e. attributes derived from well-known SecurityAttribute, applied on a method/type/assembly.
    /// </summary>
    internal sealed class SecurityWellKnownAttributeData
    {
        // data from Security attributes:
        // Array of decoded security actions corresponding to source security attributes, null if there are no security attributes in source.
        private byte[] _lazySecurityActions;
        // Array of resolved file paths corresponding to source PermissionSet security attributes needing fixup, null if there are no security attributes in source.
        // Fixup involves reading the file contents of the resolved file and emitting it in the permission set.
        private string[] _lazyPathsForPermissionSetFixup;

        public void SetSecurityAttribute(int attributeIndex, DeclarativeSecurityAction action, int totalSourceAttributes)
        {
            Debug.Assert(attributeIndex >= 0 && attributeIndex < totalSourceAttributes);
            Debug.Assert(action != 0);

            if (_lazySecurityActions == null)
            {
                Interlocked.CompareExchange(ref _lazySecurityActions, new byte[totalSourceAttributes], null);
            }

            Debug.Assert(_lazySecurityActions.Length == totalSourceAttributes);
            _lazySecurityActions[attributeIndex] = (byte)action;
        }

        public void SetPathForPermissionSetAttributeFixup(int attributeIndex, string resolvedFilePath, int totalSourceAttributes)
        {
            Debug.Assert(attributeIndex >= 0 && attributeIndex < totalSourceAttributes);
            Debug.Assert(resolvedFilePath != null);

            if (_lazyPathsForPermissionSetFixup == null)
            {
                Interlocked.CompareExchange(ref _lazyPathsForPermissionSetFixup, new string[totalSourceAttributes], null);
            }

            Debug.Assert(_lazyPathsForPermissionSetFixup.Length == totalSourceAttributes);
            _lazyPathsForPermissionSetFixup[attributeIndex] = resolvedFilePath;
        }

        /// <summary>
        /// Used for retrieving applied source security attributes, i.e. attributes derived from well-known SecurityAttribute.
        /// </summary>
        public IEnumerable<Cci.SecurityAttribute> GetSecurityAttributes<T>(ImmutableArray<T> customAttributes)
            where T : Cci.ICustomAttribute
        {
            Debug.Assert(!customAttributes.IsDefault);
            Debug.Assert(_lazyPathsForPermissionSetFixup == null || _lazySecurityActions != null && _lazyPathsForPermissionSetFixup.Length == _lazySecurityActions.Length);

            if (_lazySecurityActions != null)
            {
                Debug.Assert(_lazySecurityActions != null);
                Debug.Assert(_lazySecurityActions.Length == customAttributes.Length);

                for (int i = 0; i < customAttributes.Length; i++)
                {
                    if (_lazySecurityActions[i] != 0)
                    {
                        var action = (DeclarativeSecurityAction)_lazySecurityActions[i];
                        Cci.ICustomAttribute attribute = customAttributes[i];

                        if (_lazyPathsForPermissionSetFixup?[i] != null)
                        {
                            attribute = new PermissionSetAttributeWithFileReference(attribute, _lazyPathsForPermissionSetFixup[i]);
                        }

                        yield return new Cci.SecurityAttribute(action, attribute);
                    }
                }
            }
        }
    }
}
