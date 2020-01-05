// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
{
    [ExportWorkspaceService(typeof(IChangeSignatureOptionsService), ServiceLayer.Default), Shared]
    internal class TestChangeSignatureOptionsService : IChangeSignatureOptionsService
    {
        public bool IsCancelled = true;
        public int[] UpdatedSignature = null;

        [ImportingConstructor]
        public TestChangeSignatureOptionsService()
        {
        }

        public ChangeSignatureOptionsResult GetChangeSignatureOptions(ISymbol symbol, ParameterConfiguration parameters, INotificationService notificationService)
        {
            var list = parameters.ToListOfParameters();

            return new ChangeSignatureOptionsResult
            {
                IsCancelled = IsCancelled,
                UpdatedSignature = new SignatureChange(
                    parameters,
                    UpdatedSignature == null ? parameters : ParameterConfiguration.Create(UpdatedSignature.Select(i => list[i]).ToList(), parameters.ThisParameter != null, selectedIndex: 0))
            };
        }
    }
}
