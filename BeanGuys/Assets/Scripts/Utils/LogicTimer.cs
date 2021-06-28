using System;
using System.Diagnostics;

public class LogicTimer
{
    public float FramesPerSecond = GameLogic.Tickrate;
    public float FixedDeltaTime = Utils.TickInterval();

    private double _accumulator;
    private long _lastTime;

    private readonly Stopwatch _stopwatch;
    private readonly Action _action;

    public float LerpAlpha => (float)_accumulator / FixedDeltaTime;

    public LogicTimer(Action action)
    {
        _stopwatch = new Stopwatch();
        _action = action;
    }

    public void Start()
    {
        _lastTime = 0;
        _accumulator = 0.0;
        _stopwatch.Restart();
    }

    public void Stop()
    {
        _stopwatch.Stop();
    }

    public void Update()
    {
        FixedDeltaTime = Utils.TickInterval();
        long elapsedTicks = _stopwatch.ElapsedTicks;
        _accumulator += (double)(elapsedTicks - _lastTime) / Stopwatch.Frequency;
        _lastTime = elapsedTicks;

        while (_accumulator >= FixedDeltaTime)
        {
            _action();
            _accumulator -= FixedDeltaTime;
        }
    }
}
