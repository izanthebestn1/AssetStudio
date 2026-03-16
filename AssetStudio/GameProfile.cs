using System;

namespace AssetStudio
{
    public enum GameProfile
    {
        Auto,
        Generic,
        LimbusCompany,
    }

    public static class GameProfileResolver
    {
        public static GameProfile Resolve(GameProfile requestedProfile, string inputPath)
        {
            if (requestedProfile != GameProfile.Auto)
            {
                return requestedProfile;
            }

            return DetectFromPath(inputPath);
        }

        public static GameProfile DetectFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return GameProfile.Generic;
            }

            if (path.IndexOf("ProjectMoon_LimbusCompany", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf("LimbusCompany", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return GameProfile.LimbusCompany;
            }

            return GameProfile.Generic;
        }

        public static bool TryParse(string value, out GameProfile profile)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                profile = GameProfile.Auto;
                return true;
            }

            return Enum.TryParse(value.Trim(), true, out profile);
        }
    }
}