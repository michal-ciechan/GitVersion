namespace GitVersion
{
    public class Settings
    {
        public bool NoFetch;
        public bool NoCache;
        public bool NoNormalize;
        public bool OnlyTrackedBranches = false;
        public bool UseBuildAgentBranch;
        public bool AzurePipelinesSetParamSkipIsOutput;
    }
}
