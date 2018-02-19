﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE.CodeEnum))]
    public sealed class ExternalCodeEnum : AbstractExternalCodeType, EnvDTE.CodeEnum
    {
        internal static EnvDTE.CodeEnum Create(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol)
        {
            var element = new ExternalCodeEnum(state, projectId, typeSymbol);
            return (EnvDTE.CodeEnum)ComAggregate.CreateAggregatedObject(element);
        }

        private ExternalCodeEnum(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol)
            : base(state, projectId, typeSymbol)
        {
        }

        public override vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementEnum; }
        }

        public EnvDTE.CodeVariable AddMember(string name, object value, object position)
        {
            throw Exceptions.ThrowEFail();
        }
    }
}
