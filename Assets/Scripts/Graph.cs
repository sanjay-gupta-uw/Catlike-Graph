using UnityEngine;

public class Graph : MonoBehaviour
{
    [SerializeField] Transform pointPrefab;
    [SerializeField, Range(10, 1000)] int resolution = 200;

    [SerializeField] FunctionLibrary.FunctionName function;

    [SerializeField, Min(0f)] float functionDuration = 1f, transitionDuration = 1f;
    public enum TransitionMode { Cycle, Random }
    [SerializeField] TransitionMode transitionMode;
    Transform[] points;
    float duration;
    bool transitioning;
    FunctionLibrary.FunctionName transitionFunction;

    void Awake()
    {
        points = new Transform[resolution * resolution];

        float step = 2f / resolution;
        var scale = Vector3.one * step;

        for (int i = 0; i < points.Length; i++)
        {
            Transform point = points[i] = Instantiate(pointPrefab, Vector3.zero, Quaternion.identity, transform);
            point.localScale = scale;
            point.SetParent(transform, false);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if ((int)function > (int)FunctionLibrary.functions.Length - 1)
            function = FunctionLibrary.FunctionName.Wave;
    }

    // Update is called once per frame
    void Update()
    {
        duration += Time.deltaTime;
        if (transitioning)
        {
            if (duration >= transitionDuration)
            {
                duration -= transitionDuration;
                transitioning = false;
            }
        }
        else if (duration >= functionDuration)
        {
            Debug.Log("Transitioning");
            duration -= functionDuration;
            transitioning = true;
            transitionFunction = function;
            PickNextFunction();
        }

        if (transitioning)
        {
            Debug.Log("Updating transition function");
            UpdateFunctionTransition();
        }
        else
        {
            Debug.Log("Updating function");
            UpdateFunction();
        }
    }

    void PickNextFunction()
    {
        Debug.Log("Picking next function");
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(function);
        duration = 0f;
    }
    void UpdateFunctionTransition()
    {
        FunctionLibrary.Function
            from = FunctionLibrary.GetFunction(transitionFunction),
            to = FunctionLibrary.GetFunction(function);
        float time = Time.time;
        float step = 2f / resolution;
        float v = 0.5f * step - 1f;
        float progress = duration / transitionDuration;

        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z++;
                v = (z + 0.5f) * step - 1f;
            }
            float u = (x + 0.5f) * step - 1f;

            points[i].localPosition = FunctionLibrary.Morph(u, v, time, from, to, progress);
        }
    }
    void UpdateFunction()
    {
        FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);
        float time = Time.time;
        float step = 2f / resolution;

        float v = 0.5f * step - 1f;
        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {
            if (x == resolution)
            {
                x = 0;
                z++;
                v = (z + 0.5f) * step - 1f;
            }
            float u = (x + 0.5f) * step - 1f;

            points[i].localPosition = f(u, v, time);
        }
    }
}
