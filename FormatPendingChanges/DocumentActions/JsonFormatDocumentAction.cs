using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;

namespace Microsoft.FormatPendingChanges.DocumentActions
{
    // [Export(typeof(DocumentAction))]
    internal sealed class JsonFormatDocumentAction : FormatDocumentAction
    {
        private const string FileExtension = ".json";

        [ImportingConstructor]
        public JsonFormatDocumentAction(SVsServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public override bool CanExecute(ProjectItem projectItem)
        {
            return projectItem != null && projectItem.Name.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
