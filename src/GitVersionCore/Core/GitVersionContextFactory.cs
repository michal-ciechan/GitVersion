using System;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitVersionContextFactory : IGitVersionContextFactory
    {
        private readonly IConfigProvider configProvider;
        private readonly IRepositoryMetadataProvider repositoryMetadataProvider;
        private readonly IBranchConfigurationCalculator branchConfigurationCalculator;
        private readonly IOptions<GitVersionOptions> options;
        private readonly ICurrentBuildAgent buildAgent;
        private readonly ILog log;

        public GitVersionContextFactory(IConfigProvider configProvider, IRepositoryMetadataProvider repositoryMetadataProvider, IBranchConfigurationCalculator branchConfigurationCalculator, IOptions<GitVersionOptions> options, ICurrentBuildAgent buildAgent, ILog log)
        {
            this.configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            this.repositoryMetadataProvider = repositoryMetadataProvider ?? throw new ArgumentNullException(nameof(repositoryMetadataProvider));
            this.branchConfigurationCalculator = branchConfigurationCalculator ?? throw new ArgumentNullException(nameof(branchConfigurationCalculator));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.buildAgent = buildAgent;
            this.log = log;
        }

        public GitVersionContext Create(GitVersionOptions gitVersionOptions)
        {
            var currentBranch = gitVersionOptions.Settings.UseBuildAgentBranch
                ? ResolveCurrentBranch()
                : gitVersionOptions.RepositoryInfo.TargetBranch;

            var targetBranch = repositoryMetadataProvider.GetTargetBranch(currentBranch);
            return Init(targetBranch, gitVersionOptions.RepositoryInfo.CommitId, gitVersionOptions.Settings.OnlyTrackedBranches);
        }

        public string ResolveCurrentBranch()
        {
            var gitVersionOptions = options.Value;
            var targetBranch = gitVersionOptions.RepositoryInfo.TargetBranch;
            if (buildAgent == null)
            {
                return targetBranch;
            }

            var isDynamicRepository = !string.IsNullOrWhiteSpace(gitVersionOptions.RepositoryInfo.DynamicRepositoryClonePath);
            var currentBranch = buildAgent.GetCurrentBranch(isDynamicRepository) ?? targetBranch;
            log.Info("Branch from build environment: " + currentBranch);

            return currentBranch;
        }

        private GitVersionContext Init(Branch currentBranch, string commitId = null, bool onlyTrackedBranches = false)
        {
            if (currentBranch == null)
                throw new InvalidOperationException("Need a branch to operate on");

            var configuration = configProvider.Provide(overrideConfig: options.Value.ConfigInfo.OverrideConfig);

            var currentCommit = repositoryMetadataProvider.GetCurrentCommit(currentBranch, commitId);

            if (currentBranch.IsDetachedHead())
            {
                var branchForCommit = repositoryMetadataProvider.GetBranchesContainingCommit(currentCommit, onlyTrackedBranches: onlyTrackedBranches).OnlyOrDefault();
                currentBranch = branchForCommit ?? currentBranch;
            }

            var currentBranchConfig = branchConfigurationCalculator.GetBranchConfiguration(currentBranch, currentCommit, configuration);
            var effectiveConfiguration = configuration.CalculateEffectiveConfiguration(currentBranchConfig);
            var currentCommitTaggedVersion = repositoryMetadataProvider.GetCurrentCommitTaggedVersion(currentCommit, effectiveConfiguration);

            return new GitVersionContext(currentBranch, currentCommit, configuration, effectiveConfiguration, currentCommitTaggedVersion);
        }
    }
}
