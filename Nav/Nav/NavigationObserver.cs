using System;

namespace Nav
{
    public interface NavigationObserver
    {
        void OnDestinationReached(DestType type, Vec3 dest);
        void OnDestinationReachFailed(DestType type, Vec3 dest);
    }
}
