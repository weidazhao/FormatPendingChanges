using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using VSLangProj;

namespace Microsoft.FormatPendingChanges.DocumentActions
{
    [Export(typeof(DocumentAction))]
    internal sealed class CSharpFormatDocumentAction : FormatDocumentAction
    {
        private const string FileExtension = ".cs";
        private const string DesignerFileExtension = ".designer.cs";

        [ImportingConstructor]
        public CSharpFormatDocumentAction(SVsServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        public override bool CanExecute(ProjectItem projectItem)
        {
            return projectItem != null &&
                   projectItem.Name.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase) &&
                   !projectItem.Name.EndsWith(DesignerFileExtension, StringComparison.OrdinalIgnoreCase) &&
                   IsBuildActionCompile(projectItem);
        }

        private bool IsBuildActionCompile(ProjectItem projectItem)
        {
            if (projectItem.Properties != null)
            {
                // Unfortunately, the code throws if the property doesn't exist...
                try
                {
                    var buildActionProperty = projectItem.Properties.Item("BuildAction");

                    if (buildActionProperty != null)
                    {
                        prjBuildAction buildAction = (prjBuildAction)buildActionProperty.Value;

                        return buildAction == prjBuildAction.prjBuildActionCompile;
                    }
                }
                catch
                {
                }
            }

            return false;
        }
    }
}
