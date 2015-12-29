using System;

namespace Microsoft.FormatPendingChanges.DocumentActions
{
    internal abstract class FormatDocumentAction : DocumentExecuteCommandAction
    {
        protected FormatDocumentAction(IServiceProvider serviceProvider)
            : base(serviceProvider, "Edit.FormatDocument")
        {
        }
    }
}
