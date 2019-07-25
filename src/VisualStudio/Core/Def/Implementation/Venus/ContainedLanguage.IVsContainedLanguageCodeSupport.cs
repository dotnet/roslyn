// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal partial class ContainedLanguage<TPackage, TLanguageService> : IVsContainedLanguageCodeSupport
    {
        public int CreateUniqueEventName(string pszClassName, string pszObjectName, string pszNameOfEvent, out string pbstrEventHandlerName)
        {
            string result = null;

            var waitIndicator = ComponentModel.GetService<IWaitIndicator>();
            waitIndicator.Wait(
                "Intellisense",
                allowCancel: false,
                action: c =>
                    result = ContainedLanguageCodeSupport.CreateUniqueEventName(GetThisDocument(), pszClassName, pszObjectName, pszNameOfEvent, c.CancellationToken));

            pbstrEventHandlerName = result;
            return VSConstants.S_OK;
        }

        public int EnsureEventHandler(
            string pszClassName,
            string pszObjectTypeName,
            string pszNameOfEvent,
            string pszEventHandlerName,
            uint itemidInsertionPoint,
            out string pbstrUniqueMemberID,
            out string pbstrEventBody,
            TextSpan[] pSpanInsertionPoint)
        {
            var thisDocument = GetThisDocument();
            var targetDocumentId = this.ContainedDocument.FindProjectDocumentIdWithItemId(itemidInsertionPoint);
            var targetDocument = thisDocument.Project.Solution.GetDocument(targetDocumentId);
            if (targetDocument == null)
            {
                // Can't generate into this itemid
                pbstrUniqueMemberID = null;
                pbstrEventBody = null;
                return VSConstants.E_FAIL;
            }

            Tuple<string, string, TextSpan> idBodyAndInsertionPoint = null;
            var waitIndicator = ComponentModel.GetService<IWaitIndicator>();
            waitIndicator.Wait(
                "Intellisense",
                allowCancel: false,
                action: c => idBodyAndInsertionPoint = ContainedLanguageCodeSupport.EnsureEventHandler(
                    thisDocument,
                    targetDocument,
                    pszClassName,
                    null /*objectName*/,
                    pszObjectTypeName,
                    pszNameOfEvent,
                    pszEventHandlerName,
                    itemidInsertionPoint,
                    useHandlesClause: false,
                    additionalFormattingRule: targetDocument.Project.LanguageServices.GetService<IAdditionalFormattingRuleLanguageService>().GetAdditionalCodeGenerationRule(),
                    cancellationToken: c.CancellationToken));

            pbstrUniqueMemberID = idBodyAndInsertionPoint.Item1;
            pbstrEventBody = idBodyAndInsertionPoint.Item2;
            pSpanInsertionPoint[0] = idBodyAndInsertionPoint.Item3;
            return VSConstants.S_OK;
        }

        public int GetBaseClassName(string pszClassName, out string pbstrBaseClassName)
        {
            var result = false;
            var waitIndicator = this.ComponentModel.GetService<IWaitIndicator>();
            string baseClassName = null;
            waitIndicator.Wait(
                "Intellisense",
                allowCancel: false,
                action: c => result = ContainedLanguageCodeSupport.TryGetBaseClassName(GetThisDocument(), pszClassName, c.CancellationToken, out baseClassName));

            pbstrBaseClassName = baseClassName;
            return result ? VSConstants.S_OK : VSConstants.E_FAIL;
        }

        public int GetCompatibleEventHandlers(
            string pszClassName,
            string pszObjectTypeName,
            string pszNameOfEvent,
            out int pcMembers,
            IntPtr ppbstrEventHandlerNames,
            IntPtr ppbstrMemberIDs)
        {
            IEnumerable<Tuple<string, string>> membersAndIds = null;

            var waitIndicator = this.ComponentModel.GetService<IWaitIndicator>();
            waitIndicator.Wait(
                "Intellisense",
                allowCancel: false,
                action: c => membersAndIds = ContainedLanguageCodeSupport.GetCompatibleEventHandlers(GetThisDocument(), pszClassName, pszObjectTypeName, pszNameOfEvent, c.CancellationToken));

            pcMembers = membersAndIds.Count();
            CreateBSTRArray(ppbstrEventHandlerNames, membersAndIds.Select(t => t.Item1));
            CreateBSTRArray(ppbstrMemberIDs, membersAndIds.Select(t => t.Item2));

            return VSConstants.S_OK;
        }

        public int GetEventHandlerMemberID(string pszClassName, string pszObjectTypeName, string pszNameOfEvent, string pszEventHandlerName, out string pbstrUniqueMemberID)
        {
            string memberId = null;

            var waitIndicator = this.ComponentModel.GetService<IWaitIndicator>();
            waitIndicator.Wait(
                "Intellisense",
                allowCancel: false,
                action: c => memberId = ContainedLanguageCodeSupport.GetEventHandlerMemberId(GetThisDocument(), pszClassName, pszObjectTypeName, pszNameOfEvent, pszEventHandlerName, c.CancellationToken));

            pbstrUniqueMemberID = memberId;
            return pbstrUniqueMemberID == null ? VSConstants.S_FALSE : VSConstants.S_OK;
        }

        public int GetMemberNavigationPoint(string pszClassName, string pszUniqueMemberID, TextSpan[] pSpanNavPoint, out uint pItemID)
        {
            uint itemId = 0;
            TextSpan textSpan = default;
            var succeeded = false;

            var waitIndicator = this.ComponentModel.GetService<IWaitIndicator>();
            waitIndicator.Wait(
                "Intellisense",
                allowCancel: false,
                action: c =>
                {
                    if (ContainedLanguageCodeSupport.TryGetMemberNavigationPoint(GetThisDocument(), pszClassName, pszUniqueMemberID, out textSpan, out var targetDocument, c.CancellationToken))
                    {
                        succeeded = true;
                        itemId = this.ContainedDocument.FindItemIdOfDocument(targetDocument);
                    }
                });

            pItemID = itemId;
            pSpanNavPoint[0] = textSpan;
            return succeeded ? VSConstants.S_OK : VSConstants.E_FAIL;
        }

        public int GetMembers(string pszClassName, uint dwFlags, out int pcMembers, IntPtr ppbstrDisplayNames, IntPtr ppbstrMemberIDs)
        {
            IEnumerable<Tuple<string, string>> membersAndIds = null;

            var waitIndicator = this.ComponentModel.GetService<IWaitIndicator>();
            waitIndicator.Wait(
                "Intellisense",
                allowCancel: false,
                action: c => membersAndIds = ContainedLanguageCodeSupport.GetMembers(GetThisDocument(), pszClassName, (CODEMEMBERTYPE)dwFlags, c.CancellationToken));

            pcMembers = membersAndIds.Count();
            CreateBSTRArray(ppbstrDisplayNames, membersAndIds.Select(t => t.Item1));
            CreateBSTRArray(ppbstrMemberIDs, membersAndIds.Select(t => t.Item2));

            return VSConstants.S_OK;
        }

        public int IsValidID(string bstrID, out bool pfIsValidID)
        {
            pfIsValidID = ContainedLanguageCodeSupport.IsValidId(GetThisDocument(), bstrID);
            return VSConstants.S_OK;
        }

        public int OnRenamed(ContainedLanguageRenameType clrt, string bstrOldID, string bstrNewID)
        {
            var result = 0;

            var waitIndicator = this.ComponentModel.GetService<IWaitIndicator>();
            waitIndicator.Wait(
                "Intellisense",
                allowCancel: false,
                action: c =>
                    {
                        var refactorNotifyServices = this.ComponentModel.DefaultExportProvider.GetExportedValues<IRefactorNotifyService>();

                        if (!ContainedLanguageCodeSupport.TryRenameElement(GetThisDocument(), clrt, bstrOldID, bstrNewID, refactorNotifyServices, c.CancellationToken))
                        {
                            result = s_CONTAINEDLANGUAGE_CANNOTFINDITEM;
                        }
                        else
                        {
                            result = VSConstants.S_OK;
                        }
                    });

            return result;
        }

        protected Document GetThisDocument()
        {
            var document = this.ContainedDocument.GetOpenTextContainer().CurrentText.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                throw new InvalidOperationException();
            }

            return document;
        }

        private static readonly int s_CONTAINEDLANGUAGE_CANNOTFINDITEM = MakeHResult(1, FACILITY_ITF, 0x8003);

        private const int FACILITY_ITF = 4;
        private static int MakeHResult(uint sev, uint facility, uint code)
        {
            return unchecked((int)((sev << 31) | (facility << 16) | code));
        }

        protected static void CreateBSTRArray(IntPtr dest, IEnumerable<string> source)
        {
            if (dest != IntPtr.Zero)
            {
                var current = Marshal.AllocCoTaskMem(source.Count() * IntPtr.Size);
                Marshal.WriteIntPtr(dest, current);
                foreach (var s in source)
                {
                    Marshal.WriteIntPtr(current, Marshal.StringToBSTR(s));
                    current = IntPtr.Add(current, IntPtr.Size);
                }
            }
        }
    }
}
