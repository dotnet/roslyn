using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract class AbstractPackage : Package
    {
        protected ForegroundThreadAffinitizedObject ForegroundObject;

        protected override void Initialize()
        {
            base.Initialize();

            // Assume that we are being initialized on the UI thread at this point.
            var defaultForegroundThreadData = ForegroundThreadData.CreateDefault(
                defaultKind: ForegroundThreadDataKind.ForcedByPackageInitialize);
            ForegroundThreadAffinitizedObject.CurrentForegroundThreadData = defaultForegroundThreadData;
            ForegroundObject = new ForegroundThreadAffinitizedObject(defaultForegroundThreadData);
        }

        protected void LoadComponentsInUIContextOnceSolutionFullyLoaded()
        {
            ForegroundObject.AssertIsForeground();

            if (KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsActive)
            {
                // if we are already in the right UI context, load it right away
                LoadComponentsInUIContext();
            }
            else
            {
                // load them when it is a right context.
                KnownUIContexts.SolutionExistsAndFullyLoadedContext.UIContextChanged += OnSolutionExistsAndFullyLoadedContext;
            }
        }

        private void OnSolutionExistsAndFullyLoadedContext(object sender, UIContextChangedEventArgs e)
        {
            ForegroundObject.AssertIsForeground();

            if (e.Activated)
            {
                // unsubscribe from it
                KnownUIContexts.SolutionExistsAndFullyLoadedContext.UIContextChanged -= OnSolutionExistsAndFullyLoadedContext;

                // load components
                LoadComponentsInUIContext();
            }
        }

        protected abstract void LoadComponentsInUIContext();
    }
}
