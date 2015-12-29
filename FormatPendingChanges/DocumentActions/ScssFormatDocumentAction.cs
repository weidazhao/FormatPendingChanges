using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;

namespace Microsoft.FormatPendingChanges.DocumentActions
{
    // [Export(typeof(DocumentAction))]
    internal sealed class ScssFormatDocumentAction : FormatDocumentAction
    {
        private const string FileExtension = ".scss";

        [ImportingConstructor]
        public ScssFormatDocumentAction(SVsServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public override bool CanExecute(ProjectItem projectItem)
        {
            return projectItem.Name.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
