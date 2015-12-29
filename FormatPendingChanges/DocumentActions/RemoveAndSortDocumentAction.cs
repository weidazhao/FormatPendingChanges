using System;

namespace Microsoft.FormatPendingChanges.DocumentActions
{
    internal abstract class RemoveAndSortDocumentAction : DocumentExecuteCommandAction
    {
        protected RemoveAndSortDocumentAction(IServiceProvider serviceProvider)
            : base(serviceProvider, "Edit.RemoveAndSort")
        {
        }
    }
}
