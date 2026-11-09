using System.Collections.Generic;

namespace SkullCavernScanner;

/// <summary>
/// Returns every valid destination floor from the documented Skull Cavern shaft rules.
/// This is intentionally a range/set, not a seed-specific prediction.
/// </summary>
internal static class ShaftDropCalculator
{
    private static readonly int[] RawDropDistances = BuildRawDropDistances();

    private static int[] BuildRawDropDistances()
    {
        var values = new SortedSet<int>();
        for (int x = 3; x <= 8; x++)
        {
            values.Add(x);          // 90% branch: x floors.
            values.Add(2 * x - 1);  // 10% branch: 2x - 1 floors.
        }
        return values.ToArray();
    }

    public static IReadOnlyList<int> GetPossibleLandingFloors(int currentCavernFloor)
    {
        var destinations = new SortedSet<int>();
        foreach (int distance in RawDropDistances)
        {
            int destination = currentCavernFloor + distance;
            // The game clamps a shaft landing to Skull Cavern floor 100 if the run
            // hasn't yet reached that floor. The scanner only displays the normal
            // pre-100 clamp rule; floors already beyond 100 aren't clamped.
            if (currentCavernFloor < 100)
                destination = System.Math.Min(100, destination);
            destinations.Add(destination);
        }
        return destinations.ToArray();
    }

    public static IReadOnlyList<int> GetPossibleDropDistances(int currentCavernFloor)
    {
        var distances = new SortedSet<int>();
        foreach (int destination in GetPossibleLandingFloors(currentCavernFloor))
            distances.Add(destination - currentCavernFloor);
        return distances.ToArray();
    }
}
