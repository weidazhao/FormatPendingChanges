using EnvDTE;
using Microsoft.FormatPendingChanges.DocumentActions;
using Microsoft.FormatPendingChanges.SourceControlProviders;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.FormatPendingChanges
{
    internal sealed class FormatPendingChangesService
    {
        private IServiceProvider _serviceProvider;
        private DTE _dte;
        private DocumentActionService _documentActionService;
        private Dictionary<string, ISourceControlProvider> _sourceControlProviders;
        private bool _isExecutingCommand;

        public FormatPendingChangesService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _dte = _serviceProvider.GetService(typeof(SDTE)) as DTE;

            var componentModel = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel2;

            _documentActionService = componentModel.GetService<DocumentActionService>();

            _sourceControlProviders = new Dictionary<string, ISourceControlProvider>(StringComparer.OrdinalIgnoreCase);
        }

        public void QueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = (OleMenuCommand)sender;

            menuCommand.Visible = true;
            menuCommand.Enabled = !_isExecutingCommand && KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive;
        }

        public async void FormatPendingChangesAsync(object sender, EventArgs e)
        {
            await ExecuteCommandExclusivelyAsync(async () =>
            {
                var solution = _dte.Solution;

                if (solution == null)
                {
                    return;
                }

                var sourceControlProvider = await GetSourceControlProviderAsync(solution);

                if (sourceControlProvider == null)
                {
                    return;
                }

                var filesToFormat = await sourceControlProvider.QueryPendingChangesAsync(_dte.Solution);

                await _documentActionService.ApplyDocumentActionsAsync(filesToFormat);
            },
            "Format Pending Changes");
        }

        public async void FormatSolutionAsync(object sender, EventArgs e)
        {
            await ExecuteCommandExclusivelyAsync(async () =>
            {
                var solution = _dte.Solution;

                if (solution == null)
                {
                    return;
                }

                var filesToFormat = EnumerateProjectItemsInSolution().ToList();

                await _documentActionService.ApplyDocumentActionsAsync(filesToFormat);
            },
            "Format Solution");
        }

        public async void UndoUnmodifiedChangesAsync(object sender, EventArgs e)
        {
            await ExecuteCommandExclusivelyAsync(async () =>
            {
                var solution = _dte.Solution;

                if (solution == null)
                {
                    return;
                }

                var sourceControlProvider = await GetSourceControlProviderAsync(solution);

                if (sourceControlProvider == null)
                {
                    return;
                }

                await sourceControlProvider.UndoUnmodifiedChangesAsync(_dte.Solution);
            },
            "Undo Unmodified Changes");
        }

        private async Task ExecuteCommandExclusivelyAsync(Func<Task> executeCommandAsync, string commandText)
        {
            if (_isExecutingCommand)
            {
                return;
            }

            var statusBar = _serviceProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar;

            try
            {
                _isExecutingCommand = true;

                statusBar.SetText(string.Format(CultureInfo.CurrentCulture, @"""{0}"" ...", commandText));

                await executeCommandAsync();
            }
            catch (Exception ex)
            {
                const string ErrorMessageFormat = "Big catch: {0}";

                LoggerUtilities.LogError(string.Format(CultureInfo.CurrentCulture, ErrorMessageFormat, ex.ToString()));
            }
            finally
            {
                statusBar.SetText(string.Format(CultureInfo.CurrentCulture, @"""{0}"" completed.", commandText));

                _isExecutingCommand = false;
            }
        }

        private async Task<ISourceControlProvider> GetSourceControlProviderAsync(Solution solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            ISourceControlProvider targetSourceControlProvider;

            if (!_sourceControlProviders.TryGetValue(solution.FullName, out targetSourceControlProvider))
            {
                var componentModel = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel2;

                foreach (var sourceControlProvider in componentModel.GetExtensions<ISourceControlProvider>())
                {
                    if (await sourceControlProvider.ContainsSolutionAsync(solution))
                    {
                        targetSourceControlProvider = sourceControlProvider;

                        _sourceControlProviders[solution.FullName] = targetSourceControlProvider;

                        break;
                    }
                }
            }

            return targetSourceControlProvider;
        }

        private static IEnumerable<string> EnumerateProjectItemsInSolution()
        {
            return EnumerateProjectsInSolution().SelectMany(p => EnumerateProjectItems(p));
        }

        private static IEnumerable<string> EnumerateProjectItems(Project project)
        {
            return EnumerateProjectItems(project.ProjectItems);
        }

        private static IEnumerable<string> EnumerateProjectItems(ProjectItems projectItems)
        {
            return projectItems.Cast<ProjectItem>().SelectMany(p => EnumerateProjectItems(p));
        }

        private static IEnumerable<string> EnumerateProjectItems(ProjectItem projectItem)
        {
            if (projectItem == null)
            {
                return Enumerable.Empty<string>();
            }

            return Enumerable.Repeat(projectItem.get_FileNames(1), 1).
                              Concat(projectItem.ProjectItems != null ? EnumerateProjectItems(projectItem.ProjectItems) : Enumerable.Empty<string>());
        }

        private static IEnumerable<Project> EnumerateProjectsInSolution()
        {
            var solution = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            if (solution != null)
            {
                Guid projectType = Guid.Empty;
                IEnumHierarchies hierEnum;
                if (ErrorHandler.Succeeded(solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, ref projectType, out hierEnum)))
                {
                    hierEnum.Reset();
                    IVsHierarchy[] vsHierarchies = new IVsHierarchy[1];
                    uint numReturned;
                    while (ErrorHandler.Succeeded(hierEnum.Next(1, vsHierarchies, out numReturned)) && numReturned == 1)
                    {
                        object project;
                        if (ErrorHandler.Succeeded(vsHierarchies[0].GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out project)))
                        {
                            if (project is Project)
                            {
                                yield return (Project)project;
                            }
                        }
                    }
                }
            }
        }
    }
}
