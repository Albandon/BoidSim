using UnityEngine;

public class BitonicSortTest : MonoBehaviour
{
    public ComputeShader sortShader;

    void Start()
    {
        TestArray(new uint[] { 20, 5, 12, 7, 18, 2, 15, 1 });
        TestArray(new uint[] { 102, 54, 33, 76, 12, 9, 88, 44, 27, 99, 3, 18, 61, 72, 7, 5 });
        TestArray(new uint[] { 512, 256, 128, 64, 32, 16, 8, 4, 2, 1, 1023, 511, 255, 127, 63, 31 });
        TestRandomArray(1024);
        TestRandomArray(2048);
        TestRandomArray(4096);
        TestRandomArray(8192);
        TestRandomArray(16384);
    }

    private void TestArray(uint[] keys)
    {
        var count = keys.Length;
        var values = new uint[count];
        for (uint i = 0; i < count; i++) values[i] = i;

        var keyBuffer = new ComputeBuffer(count, sizeof(uint));
        var valBuffer = new ComputeBuffer(count, sizeof(uint));

        keyBuffer.SetData(keys);
        valBuffer.SetData(values);

        var sorter = new BitonicSorter(sortShader);
        sorter.Dispatch(keyBuffer, valBuffer, count);

        keyBuffer.GetData(keys);
        valBuffer.GetData(values);

        for (var i = 1; i < count; i++)
        {
            Debug.Assert(keys[i - 1] <= keys[i], $"Keys not sorted at index {i - 1} ({keys[i-1]} > {keys[i]})");
        }

        for (var i = 0; i < count; i++)
        {
            var originalKey = values[i];
            Debug.Assert(originalKey < count, "Value out of bounds");
        }

        Debug.Log($"Array of size {count} sorted successfully.");

        keyBuffer.Release();
        valBuffer.Release();
    }

    private void TestRandomArray(int count)
    {
        var keys = new uint[count];
        var values = new uint[count];

        var rand = new System.Random();

        for (var i = 0; i < count; i++)
        {
            keys[i] = (uint)rand.Next(0, 10000);
            values[i] = (uint)i;
        }

        var keyBuffer = new ComputeBuffer(count, sizeof(uint));
        var valBuffer = new ComputeBuffer(count, sizeof(uint));

        keyBuffer.SetData(keys);
        valBuffer.SetData(values);

        var sorter = new BitonicSorter(sortShader);
        sorter.Dispatch(keyBuffer, valBuffer, count);

        keyBuffer.GetData(keys);
        valBuffer.GetData(values);


        for (var i = 1; i < count; i++) {
            Debug.Assert(keys[i - 1] <= keys[i], $"Keys not sorted at index {i - 1} ({keys[i-1]} > {keys[i]})");
            if (keys[i - 1] > keys[i]) return;
        }

        // 2. Values should remain valid indices
        for (var i = 0; i < count; i++)
        {
            Debug.Assert(values[i] < count, $"Value out of bounds at index {i} ({values[i]})");
        }

        Debug.Log($"Array of length {count} sorted successfully.");

        Debug.Log(string.Join(", ", keys));
        
        keyBuffer.Release();
        valBuffer.Release();
    }
}