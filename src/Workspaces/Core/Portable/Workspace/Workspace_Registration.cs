// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /* This is the static API on Workspace that lets you associate text containers with workspace instances */
    public abstract partial class Workspace
    {
        private static readonly ConditionalWeakTable<SourceTextContainer, WorkspaceRegistration> s_bufferToWorkspaceRegistrationMap =
            new ConditionalWeakTable<SourceTextContainer, WorkspaceRegistration>();

        /// <summary>
        /// Gets the workspace associated with the specific text container.
        /// </summary>
        public static bool TryGetWorkspace(SourceTextContainer textContainer, out Workspace workspace)
        {
            if (textContainer == null)
            {
                throw new ArgumentNullException(nameof(textContainer));
            }

            var registration = GetWorkspaceRegistration(textContainer);
            workspace = registration.Workspace;

            return workspace != null;
        }

        /// <summary>
        /// Register a correspondence between a text container and a workspace.
        /// </summary>
        protected void RegisterText(SourceTextContainer textContainer)
        {
            if (textContainer == null)
            {
                throw new ArgumentNullException(nameof(textContainer));
            }

            var registration = GetWorkspaceRegistration(textContainer);

            SetAndRaiseTextRegistrationChangedEvents(registration, newWorkspace: this);
        }

        /// <summary>
        /// Unregister a correspondence between a text container and a workspace.
        /// </summary>
        protected void UnregisterText(SourceTextContainer textContainer)
        {
            if (textContainer == null)
            {
                throw new ArgumentNullException(nameof(textContainer));
            }

            var registration = GetWorkspaceRegistration(textContainer);

            // guard us from being called with wrong text container.
            if (registration.Workspace == this)
            {
                SetAndRaiseTextRegistrationChangedEvents(registration, newWorkspace: null);
            }
        }

        private void SetAndRaiseTextRegistrationChangedEvents(WorkspaceRegistration registration, Workspace newWorkspace, [CallerMemberName] string eventName = null)
        {
            var oldWorkspace = registration.Workspace;
            registration.SetWorkspace(newWorkspace);

            // workspace change event is synchronous events. and it doesn't use workspace events queue since 
            // WorkspaceRegistration is not associated with 1 specific workspace. workspace events queue is specific to 1 workspace
            // using multiple queues that belong to multiple workspace will cause events to be out of order
            registration.RaiseEvents(oldWorkspace, newWorkspace);
        }

        private static WorkspaceRegistration CreateRegistration(SourceTextContainer container)
        {
            return new WorkspaceRegistration();
        }

        private static readonly ConditionalWeakTable<SourceTextContainer, WorkspaceRegistration>.CreateValueCallback s_createRegistration = CreateRegistration;

        /// <summary>
        /// Returns a <see cref="WorkspaceRegistration" /> for a given text container.
        /// </summary>
        public static WorkspaceRegistration GetWorkspaceRegistration(SourceTextContainer textContainer)
        {
            if (textContainer == null)
            {
                throw new ArgumentNullException(nameof(textContainer));
            }

            return s_bufferToWorkspaceRegistrationMap.GetValue(textContainer, s_createRegistration);
        }
    }
}
