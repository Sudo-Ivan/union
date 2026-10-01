namespace NzbDrone.Core.Qualities
{
    public enum QualitySource
    {
        // Shared canonical values
        UNKNOWN = 0,
        CAM = 1,
        TELESYNC = 2,
        TELECINE = 3,
        WORKPRINT = 4,
        DVD = 5,
        TV = 6,
        WEBDL = 7,
        WEBRIP = 8,
        BLURAY = 9,
        TELEVISION_RAW = 10,
        BLURAY_RAW = 11,

        // Sonarr-vocabulary aliases onto the shared values
        Unknown = UNKNOWN,
        Television = TV,
        TelevisionRaw = TELEVISION_RAW,
        Web = WEBDL,
        WebRip = WEBRIP,
        Bluray = BLURAY,
        BlurayRaw = BLURAY_RAW
    }
}
