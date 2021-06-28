
public class GameLogic
{
    public static int clientTick = 0;
    public static int serverTick = 0;
    public static float lerpAmount = 0.1f;

    public static int Tickrate = 30; // Ticks per second 30 - 128;
    public static int TicksPer100ms = (int)(Tickrate * .1f);
    public static float SecPerTick = 0.033333f; // ( 1 / Tickrate);

    public static int Tick = 0;
    public static int PredictionTick { get; private set; } = 0;
    public static int InterpolationTick { get; private set; } = 0;

    public static void SetTick(int _serverTick)
    {
        Tick = _serverTick;
        PredictionTick = _serverTick + TicksPer100ms;
        InterpolationTick = _serverTick - TicksPer100ms;
    }
}
