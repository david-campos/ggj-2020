using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterCamera : MonoBehaviour
{
    public Shader depthShader;

    // Start is called before the first frame update
    void Start()
    {
        if (depthShader)
            GetComponent<Camera>().SetReplacementShader(depthShader, ""); //Replace all shaders with depth-only
    }
}
