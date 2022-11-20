using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Slime : MonoBehaviour {

    public ComputeShader shader;

    public TMP_InputField agentField;

    public Slider rangeSlider;
    public Slider lengthSlider;
    public Slider angleSlider;

    public TextMeshProUGUI rangeText;
    public TextMeshProUGUI lengthText;
    public TextMeshProUGUI angleText;

    [SerializeField]
    private int width = 1920, height = 1080;
    // [SerializeField]
    private int numAgents = 1000000;
    [SerializeField]
    private float moveSpeed = 50f;
    [SerializeField]
    private float diffuseSpeed = 10.0f;
    [SerializeField]
    private float evaporateSpeed = 0.3f;

    [SerializeField]
    private int senseRange = 3;
    [SerializeField]
    private float sensorLength = 8.0f;
    [SerializeField]
    private float sensorAngleSpacing = 30.0f;
    [SerializeField]
    private float turnSpeed = 50.0f;
    [SerializeField]
    private float marchingError = 0.1f;

    private RenderTexture trailMap;
    private RenderTexture trailMapProcessed;
    private ComputeBuffer agentsBuffer;

    private Dictionary<string, int> kernelIndices;

    public struct Agent {
        public Vector2 position;
        public float angle;
        // public Vector4 type;
    }

    private Agent[] agents;

    void Start() {
        kernelIndices = new Dictionary<string, int>();
        kernelIndices.Add("Update", shader.FindKernel("Update")); // Thread Shape [16, 1, 1]
        kernelIndices.Add("Postprocess", shader.FindKernel("Postprocess")); // Thread Shape [8, 8, 1]

        createNewTexture(ref trailMap);

        // byte[] bytes = toTexture2D(trailMap).EncodeToPNG();
        // System.IO.File.WriteAllBytes("E:/Code/C#/Unity/Slime/Assets/trailmap.png", bytes);

        agents = new Agent[numAgents];
        for(int i = 0; i < numAgents; i++) {
            float angle = Random.Range(0, 2 * Mathf.PI);
            float len = Random.value * height * 0.9f / 2.0f;
            float x = Mathf.Cos(angle) * len;
            float y = Mathf.Sin(angle) * len;

            agents[i].position = new Vector2(width/2 + x, height/2 + y);
            agents[i].angle = angle + Mathf.PI;

            // Vector4 type = Vector4.zero;
            // type[Random.Range(0, 3)] = 1;
            // agents[i].type = type;
        }

        agentsBuffer = new ComputeBuffer(numAgents, sizeof(float) * 3);
        agentsBuffer.SetData(agents);
    }

    void Update() {
        if(agentField.text != "") numAgents = int.Parse(agentField.text);
        senseRange = (int)rangeSlider.value;
        sensorLength = lengthSlider.value;
        sensorAngleSpacing = angleSlider.value;

        rangeText.text = ""+senseRange;
        lengthText.text = ""+sensorLength;
        angleText.text = ""+sensorAngleSpacing;

        shader.SetTexture(kernelIndices["Update"], "TrailMap", trailMap);

        shader.SetInt("width", width);
        shader.SetInt("height", height);
        shader.SetInt("numAgents", numAgents);
        shader.SetFloat("moveSpeed", moveSpeed);
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.SetInt("senseRange", senseRange);
        shader.SetFloat("sensorLength", sensorLength);
        shader.SetFloat("sensorAngleSpacing", sensorAngleSpacing * Mathf.Deg2Rad);
        shader.SetFloat("turnSpeed", turnSpeed);
        shader.SetFloat("marchingError", marchingError);
        shader.SetBuffer(kernelIndices["Update"], "agents", agentsBuffer);

        shader.Dispatch(kernelIndices["Update"], numAgents / 1024, 1, 1);

        createNewTexture(ref trailMapProcessed);

        shader.SetFloat("evaporateSpeed", evaporateSpeed);
        shader.SetFloat("diffuseSpeed", diffuseSpeed);
        shader.SetTexture(kernelIndices["Postprocess"], "TrailMap", trailMap);
        shader.SetTexture(kernelIndices["Postprocess"], "TrailMapProcessed", trailMapProcessed);

        shader.Dispatch(kernelIndices["Postprocess"], width / 8, height / 8, 1);

        trailMap.Release();
        trailMap = trailMapProcessed;
    }

    private void createNewTexture(ref RenderTexture texture) {
        texture = new RenderTexture(width, height, 1);
        texture.enableRandomWrite = true;
        texture.filterMode = FilterMode.Point;
        texture.Create();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        Graphics.Blit(trailMapProcessed, destination);
    }

    private void OnDestroy() {
        trailMap.Release();
        trailMapProcessed.Release();
        agentsBuffer.Release();
    }

    Texture2D toTexture2D(RenderTexture rTex) {
        Texture2D tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }
}
