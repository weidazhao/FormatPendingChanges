using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.FormatPendingChanges.DocumentActions
{
    [Export(typeof(DocumentAction))]
    internal sealed class XmlFormatDocumentAction : FormatDocumentAction
    {
        private static readonly string[] XmlBasedFileExtensions = { ".xml", ".vsixmanifest", ".vstemplate", ".vsct", ".props", ".targets", ".wxs", ".wxl", ".wxi" };

        [ImportingConstructor]
        public XmlFormatDocumentAction(SVsServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public override bool CanExecute(ProjectItem projectItem)
        {
            return projectItem != null && XmlBasedFileExtensions.Any(p => projectItem.Name.EndsWith(p, StringComparison.OrdinalIgnoreCase));
        }
    }
}
