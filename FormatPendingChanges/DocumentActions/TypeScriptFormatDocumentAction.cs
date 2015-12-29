using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;

namespace Microsoft.FormatPendingChanges.DocumentActions
{
    [Export(typeof(DocumentAction))]
    internal sealed class TypeScriptFormatDocumentAction : FormatDocumentAction
    {
        private const string FileExtension = ".ts";
        private const string DesignerFileExtension = ".d.ts";

        [ImportingConstructor]
        public TypeScriptFormatDocumentAction(SVsServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public override bool CanExecute(ProjectItem projectItem)
        {
            return projectItem != null &&
                   projectItem.Name.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase) &&
                   !projectItem.Name.EndsWith(DesignerFileExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
