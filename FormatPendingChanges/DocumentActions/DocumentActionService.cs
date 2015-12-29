using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using Constants = EnvDTE.Constants;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.FormatPendingChanges.DocumentActions
{
    [Export(typeof(DocumentActionService))]
    internal sealed class DocumentActionService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DTE _dte;
        private readonly DocumentAction[] _documentActions;

        /// <summary>
        /// Constructor
        /// </summary>
        [ImportingConstructor]
        public DocumentActionService([Import] SVsServiceProvider serviceProvider, [ImportMany] IEnumerable<DocumentAction> documentActions)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (documentActions == null)
            {
                throw new ArgumentNullException(nameof(documentActions));
            }

            _serviceProvider = serviceProvider;
            _dte = _serviceProvider.GetService(typeof(SDTE)) as DTE;
            _documentActions = documentActions.ToArray();
        }

        /// <summary>
        /// Applies the document actions to the given set of documents.
        /// </summary>
        public async Task ApplyDocumentActionsAsync(IEnumerable<string> paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            var remainingPaths = new HashSet<string>(paths);

            try
            {
                var activeWindow = _dte.ActiveWindow;

                foreach (var path in paths)
                {
                    await ApplyDocumentActionsAsync(path);

                    remainingPaths.Remove(path);
                }

                if (activeWindow != null)
                {
                    activeWindow.Activate();
                }
            }
            finally
            {
                if (remainingPaths.Any())
                {
                    string ErrorMessageFormat = @"The following files were not formatted: " + Environment.NewLine + "{0}";
                    LoggerUtilities.LogError(string.Format(CultureInfo.CurrentCulture, ErrorMessageFormat, string.Join(Environment.NewLine, remainingPaths)));
                }
            }
        }

        /// <summary>
        /// Applies the document actions to the given document.
        /// </summary>
        public async Task ApplyDocumentActionsAsync(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var projectItem = _dte.Solution.FindProjectItem(path);

            if (projectItem == null)
            {
                return;
            }

            var executableDocumentActions = _documentActions.Where(p => p.CanExecute(projectItem)).ToList();

            if (!executableDocumentActions.Any())
            {
                return;
            }

            bool isOpen = projectItem.IsOpen;

            projectItem.Open(Constants.vsViewKindCode).Activate();

            foreach (var executableDocumentAction in executableDocumentActions)
            {
                await executableDocumentAction.ExecuteAsync(projectItem);
            }

            projectItem.Document.Save();

            if (!isOpen)
            {
                projectItem.Document.Close();
            }
        }
    }
}
