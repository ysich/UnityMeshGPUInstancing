using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TestProfiler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    private List<List<int>> aa = new List<List<int>>();
    void Update()
    {
        // UnityEngine.Profiling.Profiler.enabled = true;
        UnityEngine.Profiling.Profiler.BeginSample("Test!!!!Test!!!!!");
        for (int i = 0; i < 1000; i++)
        {
            List<int> alist = new List<int>(){1,2,3,4,5,6,7,8};
            aa.Add(alist);
        }
        UnityEngine.Profiling.Profiler.EndSample();
    }
}
