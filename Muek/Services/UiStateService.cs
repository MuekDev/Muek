using System;

namespace Muek.Services;

public static class UiStateService
{
    public static int GlobalTimelineScale { get; set; } = 100;
    public static double GlobalTimelineOffsetX { get; set; }
    public const int AnimationDuration = 500;
    public const int AnimationDelay = 100;
    
    public static double GlobalPlayHeadPosX  { get; set; }
    public static event EventHandler<double> GlobalPlayHeadPosXUpdated;

    public static void InvokeUpdatePlayheadPos(double replyTime)
    {
        GlobalPlayHeadPosXUpdated?.Invoke(null, replyTime);
    }
}