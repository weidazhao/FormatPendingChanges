using EnvDTE;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.FormatPendingChanges.SourceControlProviders
{
    [Export(typeof(ISourceControlProvider))]
    internal sealed class GitProvider : ISourceControlProvider
    {
        public Task<bool> ContainsSolutionAsync(Solution solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            return Task.Run(() =>
            {
                string gitRepository = FindGitRepository(solution.FullName);

                if (Directory.Exists(gitRepository))
                {
                    using (var repository = new Repository(gitRepository))
                    {
                        var fileStatus = repository.RetrieveStatus(solution.FullName);

                        return !fileStatus.HasFlag(FileStatus.Nonexistent);
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

            string solutionFullName = solution.FullName;

            return Task.Run(() =>
            {
                var pendingChangeHashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                string gitRepository = FindGitRepository(solutionFullName);

                if (Directory.Exists(gitRepository))
                {
                    using (var repository = new Repository(gitRepository))
                    {
                        var repositoryStatus = repository.RetrieveStatus(new StatusOptions() { DetectRenamesInIndex = true, DetectRenamesInWorkDir = true });

                        foreach (var modified in repositoryStatus.Modified)
                        {
                            pendingChangeHashSet.Add(Path.Combine(Path.GetDirectoryName(gitRepository), modified.FilePath));
                        }

                        foreach (var added in repositoryStatus.Added)
                        {
                            pendingChangeHashSet.Add(Path.Combine(Path.GetDirectoryName(gitRepository), added.FilePath));
                        }

                        foreach (var staged in repositoryStatus.Staged)
                        {
                            pendingChangeHashSet.Add(Path.Combine(Path.GetDirectoryName(gitRepository), staged.FilePath));
                        }

                        foreach (var untracked in repositoryStatus.Untracked)
                        {
                            pendingChangeHashSet.Add(Path.Combine(Path.GetDirectoryName(gitRepository), untracked.FilePath));
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

            return Task.FromResult(true);
        }

        private string FindGitRepository(string path)
        {
            if (File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }

            while (Directory.Exists(path))
            {
                string gitRepository = Path.Combine(path, ".git");

                if (Directory.Exists(gitRepository))
                {
                    return gitRepository;
                }

                path = Path.GetDirectoryName(path);
            }

            return null;
        }
    }
}
