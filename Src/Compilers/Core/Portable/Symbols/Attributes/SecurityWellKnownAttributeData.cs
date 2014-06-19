// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        private byte[] lazySecurityActions;
        // Array of resolved file paths corresponding to source PermissionSet security attributes needing fixup, null if there are no security attributes in source.
        // Fixup involves reading the file contents of the resolved file and emitting it in the permission set.
        private string[] lazyPathsForPermissionSetFixup;

        public void SetSecurityAttribute(int attributeIndex, Cci.SecurityAction action, int totalSourceAttributes)
        {
            Debug.Assert(attributeIndex >= 0 && attributeIndex < totalSourceAttributes);
            Debug.Assert(action != 0);

            if (lazySecurityActions == null)
            {
                Interlocked.CompareExchange(ref lazySecurityActions, new byte[totalSourceAttributes], null);
            }

            Debug.Assert(lazySecurityActions.Length == totalSourceAttributes);
            lazySecurityActions[attributeIndex] = (byte)action;
        }

        public void SetPathForPermissionSetAttributeFixup(int attributeIndex, string resolvedFilePath, int totalSourceAttributes)
        {
            Debug.Assert(attributeIndex >= 0 && attributeIndex < totalSourceAttributes);
            Debug.Assert(!String.IsNullOrEmpty(resolvedFilePath));
            Debug.Assert(PathUtilities.IsAbsolute(resolvedFilePath));

            if (lazyPathsForPermissionSetFixup == null)
            {
                Interlocked.CompareExchange(ref lazyPathsForPermissionSetFixup, new string[totalSourceAttributes], null);
            }

            Debug.Assert(lazyPathsForPermissionSetFixup.Length == totalSourceAttributes);
            lazyPathsForPermissionSetFixup[attributeIndex] = resolvedFilePath;
        }

        /// <summary>
        /// Used for retreiving applied source security attributes, i.e. attributes derived from well-known SecurityAttribute.
        /// </summary>
        public IEnumerable<Cci.SecurityAttribute> GetSecurityAttributes<T>(ImmutableArray<T> customAttributes)
            where T : Cci.ICustomAttribute
        {
            Debug.Assert(!customAttributes.IsDefault);
            Debug.Assert(lazyPathsForPermissionSetFixup == null || lazySecurityActions != null && lazyPathsForPermissionSetFixup.Length == lazySecurityActions.Length);

            if (lazySecurityActions != null)
            {
                Debug.Assert(lazySecurityActions != null);
                Debug.Assert(lazySecurityActions.Length == customAttributes.Length);

                for (int i = 0; i < customAttributes.Length; i++)
                {
                    if (lazySecurityActions[i] != 0)
                    {
                        var action = (Cci.SecurityAction)lazySecurityActions[i];
                        Cci.ICustomAttribute attribute = customAttributes[i];

                        if (lazyPathsForPermissionSetFixup != null && lazyPathsForPermissionSetFixup[i] != null)
                        {
                            attribute = new PermissionSetAttributeWithFileReference(attribute, lazyPathsForPermissionSetFixup[i]);
                        }

                        yield return new Cci.SecurityAttribute(action, attribute);
                    }
                }
            }
        }
    }
}