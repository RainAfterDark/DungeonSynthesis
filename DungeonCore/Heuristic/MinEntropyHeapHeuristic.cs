using DungeonCore.Model;
using DungeonCore.Topology;

namespace DungeonCore.Heuristic;

public sealed class MinEntropyHeapHeuristic : IHeuristic
{
    private Random _random = new();

    // Entropy Data
    private double[] _stateWeights = [];
    private double[] _stateWlw = [];
    
    // Grid Data
    private double[] _cellSumWeights = [];
    private double[] _cellSumWlw = [];
    
    // Min Heap <CellID, Score>
    private readonly PriorityQueue<int, double> _heap = new();
    
    // The "Source of Truth" for the current valid score of a cell.
    private double[] _cellScores = [];

    private bool _initialized;

    public void Initialize(WaveGrid grid, IModel model, Random random)
    {
        _random = random;
        var cellCount = grid.CellCount;
        var maxStates = model.StateCount;
        
        // Precompute State Data
        _stateWeights = new double[maxStates];
        _stateWlw = new double[maxStates];
        
        double globalWlwSum = 0;
        double globalWeightSum = 0;

        for (var i = 0; i < maxStates; i++)
        {
            var w = model.GetWeight(i);
            var wlw = w > 1e-9 ? w * Math.Log(w) : 0;
        
            _stateWeights[i] = w;
            _stateWlw[i] = wlw;
            
            globalWeightSum += w;
            globalWlwSum += wlw;
        }

        // Initialize Shadow Arrays and Heap
        _cellSumWeights = new double[cellCount];
        _cellSumWlw = new double[cellCount];
        _cellScores = new double[cellCount];
        _heap.Clear();
        _heap.EnsureCapacity(cellCount);

        // Fill Initial Data
        for (var id = 0; id < cellCount; id++)
        {
            var cell = grid.Cells[id];
            if (cell.Observed != -1) continue;

            if (cell.DomainCount == maxStates)
            {
                _cellSumWeights[id] = globalWeightSum;
                _cellSumWlw[id] = globalWlwSum;
            }
            else
            {
                double currentW = 0;
                double currentWlw = 0;
                for (var s = 0; s < maxStates; s++)
                {
                    if (!cell.Domain[s]) continue;
                    currentW += _stateWeights[s];
                    currentWlw += _stateWlw[s];
                }
                _cellSumWeights[id] = currentW;
                _cellSumWlw[id] = currentWlw;
            }
            
            UpdateScoreAndPush(id);
        }
        
        _initialized = true;
    }

    public void OnBanned(int cellId, int stateId)
    {
        if (!_initialized) return;

        // Update our Shadow Data
        _cellSumWlw[cellId] -= _stateWlw[stateId];
        _cellSumWeights[cellId] -= _stateWeights[stateId];

        // Clamp to 0 to prevent floating point drift causing negative weights
        if (_cellSumWlw[cellId] < 1e-9) _cellSumWlw[cellId] = 0;
        if (_cellSumWeights[cellId] < 1e-9) _cellSumWeights[cellId] = 0;

        // Recalculate and Push to Heap
        UpdateScoreAndPush(cellId);
    }

    private void UpdateScoreAndPush(int cellId)
    {
        var sumW = _cellSumWeights[cellId];
        var sumWlw = _cellSumWlw[cellId];

        // Safety for dead cells (contradictions)
        if (sumW <= 1e-9) return;

        // Shannon Entropy
        // H = log(SumWeights) - (SumWlw / SumWeights)
        var entropy = Math.Log(sumW) - sumWlw / sumW;
        
        // Add noise for organic selection
        var noise = _random.NextDouble() * 1e-6;
        var score = entropy + noise;

        // Lazy Update:
        // We store this score as the "Current Valid Score".
        // When we pop from the heap later, if the popped score != _currentScores[id],
        // we know it was an old entry and ignore it.
        _cellScores[cellId] = score;
        _heap.Enqueue(cellId, score);
    }

    public int PickNextCell(WaveGrid grid)
    {
        if (!_initialized) return -1;

        while (_heap.Count > 0)
        {
            // Pop the lowest entropy cell
            if (!_heap.TryDequeue(out var cellId, out var score)) return -1;

            // Skip if already collapsed
            if (grid.Cells[cellId].Observed != -1) continue;

            // Skip if stale entry (old score)
            if (Math.Abs(score - _cellScores[cellId]) > 1e-9) continue;
            
            return cellId;
        }

        return -1;
    }
}