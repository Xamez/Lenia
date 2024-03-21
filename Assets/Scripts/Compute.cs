using System.Collections;
using UnityEngine;

public class Compute : MonoBehaviour
{

    [SerializeField] private int width = 1920;
    [SerializeField] private int height = 1080;
    
    [SerializeField] private ComputeShader _computeShader;
    [SerializeField] private int _radius = 30;
    [Range(0, 128)] [SerializeField] private int _kernelSize = 32;

    private RenderTexture _texture;
    private RenderTexture _previousValues;
    private RenderTexture _values;
    private RenderTexture _kernel;
    private int _kernelIndexLenia;
    private int _kernelIndexCircle;
    private int _kernelIndexEmpty;
    private int _kernelIndexDrawKernel;

    private Vector2Int _threadGroupsSize;

    private static readonly int Result = Shader.PropertyToID("Result");
    private static readonly int Radius = Shader.PropertyToID("Radius");
    private static readonly int KernelSize = Shader.PropertyToID("KernelSize");
    private static readonly int Values = Shader.PropertyToID("Values");
    private static readonly int PreviousValues = Shader.PropertyToID("PreviousValues");
    private static readonly int MousePosition = Shader.PropertyToID("MousePosition");
    private static readonly int Kernel = Shader.PropertyToID("Kernel");

    void Start()
    {
        _kernelIndexLenia = _computeShader.FindKernel("Lenia");
        _kernelIndexCircle = _computeShader.FindKernel("Circle");
        _kernelIndexEmpty = _computeShader.FindKernel("Empty");
        _kernelIndexDrawKernel = _computeShader.FindKernel("DrawKernel");
        _texture = RenderTexture.GetTemporary(width, height);
        _texture.enableRandomWrite = true;
        _previousValues = RenderTexture.GetTemporary(width, height);
        _previousValues.enableRandomWrite = true;
        _values = RenderTexture.GetTemporary(width, height);
        _values.enableRandomWrite = true;
        _kernel = RenderTexture.GetTemporary(_kernelSize, _kernelSize);
        _kernel.enableRandomWrite = true;
        CalculateThreadGroups();
        StartCoroutine(Step());
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Mouse0))
        {
            _computeShader.SetTexture(_kernelIndexCircle, Values, _values);
            _computeShader.Dispatch(_kernelIndexCircle, _threadGroupsSize.x, _threadGroupsSize.y, 1);
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            _computeShader.SetTexture(_kernelIndexEmpty, Values, _values);
            _computeShader.Dispatch(_kernelIndexEmpty, _threadGroupsSize.x, _threadGroupsSize.y, 1);
        }
    }

    IEnumerator Step()
    {
        while (true)
        {
            Graphics.CopyTexture(_values, _previousValues);
            
            _computeShader.SetTexture(_kernelIndexDrawKernel, Kernel, _kernel);
            _computeShader.Dispatch(_kernelIndexDrawKernel, 1, 1, 1);

            _computeShader.SetTexture(_kernelIndexLenia, Result, _texture);
            _computeShader.SetTexture(_kernelIndexLenia, Values, _values);
            _computeShader.SetTexture(_kernelIndexLenia, PreviousValues, _previousValues);
            _computeShader.SetInt(Radius, _radius);
            _computeShader.SetInt(KernelSize, _kernelSize);
            _computeShader.SetVector(MousePosition, Input.mousePosition);
            _computeShader.Dispatch(_kernelIndexLenia, _threadGroupsSize.x, _threadGroupsSize.y, 1);

            yield return new WaitForSeconds(0.05f);
        }
    }

    private void OnDestroy()
    {
        RenderTexture.ReleaseTemporary(_texture);
        RenderTexture.ReleaseTemporary(_values);
        RenderTexture.ReleaseTemporary(_previousValues);
        RenderTexture.ReleaseTemporary(_kernel);
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _texture);
        GUI.DrawTexture(new Rect(0, 0, 128, 128), _kernel);
    }

    private void CalculateThreadGroups()
    {
        uint x, y;
        _computeShader.GetKernelThreadGroupSizes(_kernelIndexLenia, out x, out y, out _);
        _threadGroupsSize = new Vector2Int(
            Mathf.CeilToInt((float)_texture.width / x),
            Mathf.CeilToInt((float)_texture.height / y));
    }
}