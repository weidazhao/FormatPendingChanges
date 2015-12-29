using EnvDTE;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.FormatPendingChanges.SourceControlProviders
{
    [Export(typeof(ISourceControlProvider))]
    internal sealed class TfvcProvider : ISourceControlProvider
    {
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public TfvcProvider(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<bool> ContainsSolutionAsync(Solution solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            return Task.Run(() =>
            {
                foreach (var workspaceInfo in Workstation.Current.GetAllLocalWorkspaceInfo())
                {
                    using (var teamProjectCollection = new TfsTeamProjectCollection(workspaceInfo.ServerUri))
                    {
                        var versionControlServer = (VersionControlServer)teamProjectCollection.GetService(typeof(VersionControlServer));

                        var workspace = versionControlServer.GetWorkspace(workspaceInfo);

                        if (workspace.IsLocalPathMapped(solution.FullName))
                        {
                            return true;
                        }
                    }
                }

                return false;
            });
        }

        public Task<string[]> QueryPendingChangesAsync(Solution solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            return Task.Run(() =>
            {
                var pendingChangeHashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var workspaceInfo in Workstation.Current.GetAllLocalWorkspaceInfo())
                {
                    using (var teamProjectCollection = new TfsTeamProjectCollection(workspaceInfo.ServerUri))
                    {
                        var versionControlServer = (VersionControlServer)teamProjectCollection.GetService(typeof(VersionControlServer));

                        var workspace = versionControlServer.GetWorkspace(workspaceInfo);

                        var pendingSets = workspace.QueryPendingSets(new string[0], RecursionType.Full, workspace.Name, workspace.OwnerName, false);

                        var pendingChanges = from pendingSet in pendingSets
                                             where pendingSet.Type == PendingSetType.Workspace
                                             from pendingChange in pendingSet.PendingChanges
                                             where pendingChange.ItemType == ItemType.File && (pendingChange.ChangeType & ChangeType.Edit) != 0
                                             select pendingChange.LocalItem;

                        foreach (var pendingChange in pendingChanges)
                        {
                            pendingChangeHashSet.Add(pendingChange);
                        }
                    }
                }

                return pendingChangeHashSet.ToArray();
            });
        }

        public Task UndoUnmodifiedChangesAsync(Solution solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            return Task.Run(async () =>
            {
                foreach (var workspaceInfo in Workstation.Current.GetAllLocalWorkspaceInfo())
                {
                    using (var teamProjectCollection = new TfsTeamProjectCollection(workspaceInfo.ServerUri))
                    {
                        var versionControlServer = (VersionControlServer)teamProjectCollection.GetService(typeof(VersionControlServer));

                        var workspace = versionControlServer.GetWorkspace(workspaceInfo);

                        var pendingSets = workspace.QueryPendingSets(new string[0], RecursionType.Full, workspace.Name, workspace.OwnerName, false);

                        var pendingChanges = from pendingSet in pendingSets
                                             where pendingSet.Type == PendingSetType.Workspace
                                             from pendingChange in pendingSet.PendingChanges
                                             where pendingChange.ItemType == ItemType.File && (pendingChange.ChangeType & ChangeType.Edit) != 0
                                             select pendingChange;

                        var pendingChangesToUndo = await Task.WhenAll(pendingChanges.Select(p => ShouldUndoPendingChangeAsync(p)));

                        pendingChangesToUndo = pendingChangesToUndo.Where(p => p != null).ToArray();

                        if (pendingChangesToUndo.Any())
                        {
                            workspace.Undo(pendingChangesToUndo);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Returns the given pending change if it should be undone, otherwise null.
        /// </summary>
        private Task<PendingChange> ShouldUndoPendingChangeAsync(PendingChange pendingChange)
        {
            return Task.Run(() =>
            {
                if (pendingChange.IsAdd || pendingChange.IsDelete || pendingChange.IsLocalItemDelete || pendingChange.IsUndelete)
                {
                    return null;
                }

                byte[] baseItemHashCode;

                try
                {
                    using (var baseFileStream = pendingChange.DownloadBaseFile())
                    {
                        using (var hashAlgorithem = new SHA1Cng())
                        {
                            baseItemHashCode = hashAlgorithem.ComputeHash(baseFileStream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    const string ErrorMessageFormat = "Error occurred during computing hash for the base item of {0}: {1}";

                    LoggerUtilities.LogError(string.Format(CultureInfo.CurrentCulture, ErrorMessageFormat, pendingChange.ServerItem, ex.ToString()));

                    return null;
                }

                byte[] localItemHashCode;

                try
                {
                    using (var localFileStream = new FileStream(Path.GetFullPath(pendingChange.LocalItem), FileMode.Open, FileAccess.Read))
                    {
                        using (var hashAlgorithem = new SHA1Cng())
                        {
                            localItemHashCode = hashAlgorithem.ComputeHash(localFileStream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    const string ErrorMessageFormat = "Error occurred during computing hash for the local item of {0}: {1}";

                    LoggerUtilities.LogError(string.Format(CultureInfo.CurrentCulture, ErrorMessageFormat, pendingChange.ServerItem, ex.ToString()));

                    return null;
                }

                return Enumerable.SequenceEqual(baseItemHashCode, localItemHashCode) ? pendingChange : null;
            });
        }
    }
}
