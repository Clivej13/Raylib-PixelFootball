using RaylibPixelFootball.Game;

namespace RaylibPixelFootball.Tests;

internal sealed class ScriptedActionRandom(params int[] values) : IActionRandom
{
    private readonly Queue<int> _values = new(values);
    public int Calls { get; private set; }

    public int NextInclusive(int minimum, int maximum)
    {
        Calls++;
        return _values.Dequeue();
    }
}
