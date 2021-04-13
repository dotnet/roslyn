// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE.CodeInterface))]
    public sealed class ExternalCodeInterface : AbstractExternalCodeType, EnvDTE80.CodeInterface2, EnvDTE.CodeInterface, EnvDTE.CodeType, EnvDTE.CodeElement, EnvDTE80.CodeElement2
    {
        internal static EnvDTE.CodeInterface Create(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol)
        {
            var newElement = new ExternalCodeInterface(state, projectId, typeSymbol);
            return (EnvDTE.CodeInterface)ComAggregate.CreateAggregatedObject(newElement);
        }

        private ExternalCodeInterface(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol)
            : base(state, projectId, typeSymbol)
        {
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementInterface; }
        }

        #region EnvDTE.CodeInterface members

        public EnvDTE.CodeFunction AddFunction(string name, EnvDTE.vsCMFunction kind, object type, object position, EnvDTE.vsCMAccess access)
            => throw Exceptions.ThrowEFail();

        public EnvDTE.CodeProperty AddProperty(string getterName, string putterName, object type, object position, EnvDTE.vsCMAccess access, object location)
            => throw Exceptions.ThrowEFail();

        #endregion

        #region EnvDTE.CodeInterface2 members

        public EnvDTE80.CodeEvent AddEvent(string name, string fullDelegateName, bool createPropertyStyleEvent, object position, EnvDTE.vsCMAccess access)
            => throw Exceptions.ThrowEFail();

        public EnvDTE80.vsCMDataTypeKind DataTypeKind
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public bool IsGeneric
        {
            get { throw new NotImplementedException(); }
        }

        public EnvDTE.CodeElements Parts
        {
            get
            {
                throw Exceptions.ThrowEFail();
            }
        }

        #endregion
    }
}
