using UnityEngine;

public class GPUGraph : MonoBehaviour
{
    [SerializeField] Material material;
    [SerializeField] Mesh mesh;
    [SerializeField] ComputeShader computeShader;

    static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        stepId = Shader.PropertyToID("_Step"),
        timeId = Shader.PropertyToID("_Time"),
        transitionProgressId = Shader.PropertyToID("_TransitionProgress");

    const int maxResolution = 1000;
    [SerializeField, Range(10, maxResolution)] int resolution = 200;


    [SerializeField, Min(0f)] float functionDuration = 1f, transitionDuration = 1f;
    public enum TransitionMode { Cycle, Random }

    [SerializeField] FunctionLibrary.FunctionName function;
    [SerializeField] TransitionMode transitionMode;

    FunctionLibrary.FunctionName transitionFunction;
    float duration;
    bool transitioning;

    ComputeBuffer positionsBuffer;

    void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, sizeof(float) * 3);
    }
    void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

        UpdateFunctionOnGPU();
    }

    void PickNextFunction()
    {
        Debug.Log("Picking next function");
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(function);
        duration = 0f;
    }

    void UpdateFunctionOnGPU()
    {
        float step = 2f / resolution;

        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);

        if (transitioning)
        {
            computeShader.SetFloat(transitionProgressId, Mathf.SmoothStep(0f, 1f, duration / transitionDuration));
        }

        var kernel = (int)function + (int)(transitioning ? transitionFunction : function) * 5;
        computeShader.SetBuffer(kernel, positionsId, positionsBuffer);

        computeShader.Dispatch(kernel, Mathf.CeilToInt(resolution / 8f), Mathf.CeilToInt(resolution / 8f), 1);

        material.SetBuffer(positionsId, positionsBuffer);
        material.SetFloat(stepId, step);

        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution);
    }
}
