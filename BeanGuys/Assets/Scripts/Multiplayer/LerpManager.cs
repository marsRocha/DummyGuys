using UnityEngine;

public class LerpManager : MonoBehaviour
{
    void Update()
    {
        // We dont want to lag behind the real tick by too much,
        // so just teleport to the next tick
        // The cases where this can happen are high ping/low fps
        GameLogic.clientTick = Mathf.Clamp(GameLogic.clientTick, GameLogic.serverTick - 2, GameLogic.serverTick);

        // Client (simulated) tick >= Server (real) tick, return
        if (GameLogic.clientTick >= GameLogic.serverTick)
            return;

        // While lerp amount is or more than 1, we move to the next clientTick and reset the lerp amount
        GameLogic.lerpAmount = (GameLogic.lerpAmount * Utils.TickInterval() + Time.deltaTime) / Utils.TickInterval();
        while (GameLogic.lerpAmount >= 1f)
        {
            // Client (simulated) tick >= Server (real) tick, break
            if (GameLogic.clientTick >= GameLogic.serverTick)
                break;

            GameLogic.lerpAmount = (GameLogic.lerpAmount * Utils.TickInterval() - Utils.TickInterval()) / Utils.TickInterval();
            GameLogic.lerpAmount = Mathf.Max(0f, GameLogic.lerpAmount);
            GameLogic.clientTick++;
        }
    }
}