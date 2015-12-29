using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.FormatPendingChanges.DocumentActions
{
    [Export(typeof(DocumentAction))]
    internal sealed class HtmlFormatDocumentAction : FormatDocumentAction
    {
        private static readonly string[] HtmlBasedFileExtensions = { ".htm", "html" };

        [ImportingConstructor]
        public HtmlFormatDocumentAction(SVsServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public override bool CanExecute(ProjectItem projectItem)
        {
            return projectItem != null && HtmlBasedFileExtensions.Any(p => projectItem.Name.EndsWith(p, StringComparison.OrdinalIgnoreCase));
        }
    }
}
