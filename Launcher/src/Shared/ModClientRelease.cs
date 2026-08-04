using System;

namespace SubnauticaSpeedrunningMod.Shared
{
    public static class ModClientRelease
    {
        public const string ChannelName = "Beta";
        public const string SemanticVersion = "0.9.0";
        public const string DisplayVersion = "Beta-0.10.1";
        public const string NumericVersion = "0.10.1.0";
        public const string RepositoryOwner = "ItsFrostyYo";
        public const string RepositoryName = "Subnautica-Speedrunning-Mod";
        public const string ReleaseBranchName = "main";
        public const string ReleaseManifestFileName = "latest.json";
        public const string ReleaseManifestUrl =
            "https://raw.githubusercontent.com/" + RepositoryOwner + "/" + RepositoryName + "/" + ReleaseBranchName + "/release/" + ReleaseManifestFileName;

        public static string BuildReleaseAssetUrl(string versionLabel, string assetFileName)
        {
            if (string.IsNullOrEmpty(versionLabel) || versionLabel.Trim().Length == 0)
            {
                throw new ArgumentException("A version label is required to build the GitHub release asset URL.", nameof(versionLabel));
            }

            if (string.IsNullOrEmpty(assetFileName) || assetFileName.Trim().Length == 0)
            {
                throw new ArgumentException("An asset file name is required to build the GitHub release asset URL.", nameof(assetFileName));
            }

            return "https://github.com/"
                   + RepositoryOwner
                   + "/"
                   + RepositoryName
                   + "/releases/download/"
                   + versionLabel
                   + "/"
                   + assetFileName;
        }
    }
}
