
namespace Coretronic.Reality.Clients
{
    public enum ServiceState : sbyte
    {
        HandTrackingError  = -6,
        SlamError          = -5,
        StereoError        = -4,
        FrameProviderError = -3,
        MemoryError        = -2,
        ProxyNull          = -1,
        Initial            =  0,
        Success            =  1
    }

    public enum LogicalCameraType : byte
    {
        None              = 0,
        RsColorDepth      = 1,
        RsStereoIr        = 2,
        QvrStereoColor    = 3,
        QvrStereoTracking = 4
    }
}