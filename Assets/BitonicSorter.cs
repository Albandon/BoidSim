using UnityEngine;

public readonly struct BitonicSorter
{
    private static readonly int Keys = Shader.PropertyToID("keys");
    private static readonly int Values = Shader.PropertyToID("values");
    private static readonly int H = Shader.PropertyToID("h");
    private static readonly int Algorithm = Shader.PropertyToID("algorithm");
    private static readonly int Count = Shader.PropertyToID("count");

    private readonly ComputeShader _shader;
    private readonly int _localSize;
    private readonly int _localCapacity;
    private readonly int _kernel;

    public BitonicSorter(ComputeShader shader, int localSize = 512)
    {
        _shader = shader;
        _localSize = localSize;
        _localCapacity = localSize * 2;
        _kernel = shader.FindKernel("sort");
    }

    public void Dispatch(ComputeBuffer keys, ComputeBuffer values, int count)
    {
        if ((count & (count - 1)) != 0)
            throw new System.ArgumentException("Count must be power of 2");

        var numGroups = Mathf.Max(1, count / _localCapacity);

        // 1. Local sort for each workgroup
        _shader.SetBuffer(_kernel, Keys, keys);
        _shader.SetBuffer(_kernel, Values, values);
        _shader.SetInt(H, _localCapacity);
        _shader.SetInt(Algorithm, (int)BitonicSortAlgorithms.LocalBitonicMergeSortExample);
        _shader.SetInt(Count, count);
        _shader.Dispatch(_kernel, numGroups, 1, 1);

        // 2. Global merges
        for (var h = _localCapacity * 2; h <= count; h <<= 1)
        {
            var bigDispatch = Mathf.Max(1, count / 2 / _localSize);

            _shader.SetInt(H, h);
            _shader.SetInt(Algorithm, (int)BitonicSortAlgorithms.BigFlip);
            _shader.Dispatch(_kernel, bigDispatch, 1, 1);

            for (var hh = h >> 1; hh >= _localCapacity; hh >>= 1)
            {
                _shader.SetInt(H, hh);
                _shader.SetInt(Algorithm, (int)BitonicSortAlgorithms.BigDisperse);
                _shader.Dispatch(_kernel, bigDispatch, 1, 1);
            }

            _shader.SetInt(H, _localCapacity);
            _shader.SetInt(Algorithm, (int)BitonicSortAlgorithms.LocalDisperse);
            _shader.Dispatch(_kernel, numGroups, 1, 1);
        }
    }
}