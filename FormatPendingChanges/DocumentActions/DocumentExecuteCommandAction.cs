using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.FormatPendingChanges.DocumentActions
{
    internal abstract class DocumentExecuteCommandAction : DocumentAction
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _command;

        protected DocumentExecuteCommandAction(IServiceProvider serviceProvider, string command)
        {
            _serviceProvider = serviceProvider;
            _command = command;
        }

        public sealed override async Task ExecuteAsync(ProjectItem projectItem)
        {
            const int MAX_RETRY = 5;
            const int E_FAIL = unchecked((int)0x80004005);
            const int RPC_E_CALL_REJECTED = unchecked((int)0x80010001);

            if (projectItem != null)
            {
                var statusBar = _serviceProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar;
                statusBar.SetText("Updating " + projectItem.get_FileNames(1));

                for (int retry = 0; retry < MAX_RETRY; retry++)
                {
                    try
                    {
                        projectItem.Document.DTE.ExecuteCommand(_command);

                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex is COMException &&
                            (((COMException)ex).ErrorCode == E_FAIL || ((COMException)ex).ErrorCode == RPC_E_CALL_REJECTED) &&
                            retry < MAX_RETRY - 1)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1));

                            continue;
                        }
                        else
                        {
                            const string ErrorMessageFormat = "Error occurred during the command {0} running on the item {1}: {2}";

                            LoggerUtilities.LogError(string.Format(CultureInfo.CurrentCulture, ErrorMessageFormat, _command, projectItem.get_FileNames(1), ex.ToString()));

                            break;
                        }
                    }
                }
            }
        }
    }
}
