using DungeonCore.Model;
using DungeonCore.Topology;

namespace DungeonCore.Heuristic;

public sealed class MinEntropyHeuristic : IHeuristic
{
    private Random _random = new();
    private double[] _stateWlw = []; // Precomputed (Weight * Log(Weight)) for each state ID
    private double[] _cellWlwSums = []; // The shadow array tracking Sum(WLW) per cell
    private bool _initialized; 

    public void Initialize(WaveGrid grid, IModel model, Random random)
    {
        _random = random;
        _stateWlw = new double[model.StateCount];
        for (var i = 0; i < model.StateCount; i++)
        {
            var w = model.GetWeight(i);
            // Handle 0 weight safely to avoid NaN
            _stateWlw[i] = w > 1e-9 ? w * Math.Log(w) : 0;
        }
        
        _cellWlwSums = new double[grid.CellCount];
        var totalWlw = _stateWlw.Sum();
        Array.Fill(_cellWlwSums, totalWlw);
        _initialized = true;
    }
    
    public void OnBanned(int cellId, int stateId)
    {
        if (!_initialized) return;
        _cellWlwSums[cellId] -= _stateWlw[stateId];
        if (_cellWlwSums[cellId] < 1e-9) _cellWlwSums[cellId] = 0;
    }

    public int PickNextCell(WaveGrid grid)
    {
        if (!_initialized) return -1;
        var candidate = -1;
        var minEntropy = double.PositiveInfinity;

        for (var i = 0; i < grid.CellCount; i++)
        {
            var cell = grid.Cells[i];

            // Skip if collapsed
            if (cell.Observed != -1) continue;

            // Skip if contradiction (safety check)
            if (cell.DomainCount < 1) continue;

            // Shannon Entropy:
            // H = log(SumWeights) - (SumWlw / SumWeights)
            var sumW = cell.SumWeights;
            var sumWlw = _cellWlwSums[i];
            var entropy = Math.Log(sumW) - (sumWlw / sumW);

            // Add noise for organic selection
            var noise = _random.NextDouble() * 1e-4;
            var score = entropy + noise;

            if (!(score < minEntropy)) continue;
            minEntropy = score;
            candidate = i;
        }

        return candidate;
    }
}