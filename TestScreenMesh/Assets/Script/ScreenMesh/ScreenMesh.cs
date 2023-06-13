using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FastGeo;
using Ray = FastGeo.Ray;
using Plane = FastGeo.Plane;

class ScreenMeshParam
{
    public Vector3 p00, p10, p01, p11;
    public ScreenMeshParam(Vector3 p00_, Vector3 p10_, Vector3 p01_, Vector3 p11_)
    {
        p00 = p00_;
        p10 = p10_;
        p01 = p01_;
        p11 = p11_;
    }
}

public class ScreenMesh : MonoBehaviour
{
    public float seaHeight = 0;
    public Vector2Int gridNum = new Vector2Int(2,2);
    public Material material;
    public Camera mainCamera;
    MeshRenderer mr;
    MeshFilter mf;
    //[HideInInspector]
    public Mesh mesh;

    List<Vector3> debugPnts = new List<Vector3>();
    List<Line> debugLines = new List<Line>();

    const float MINPOS = -1000000.0f;
    public float maxBound = 30000.0f;

    // Start is called before the first frame update
    void Start()
    {
        mr = gameObject.AddComponent<MeshRenderer>();
        mf = gameObject.AddComponent<MeshFilter>();
        InitMesh();
        mf.mesh = mesh;
        mr.material = material;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        UpdateCameraParam();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        foreach (var p in debugPnts)
        {
            Gizmos.DrawWireCube(p, 0.1f * Vector3.one);
        }
        Gizmos.color = Color.black;
        foreach (var line in debugLines)
        {
            Gizmos.DrawLine(line.a,line.b);
        }
    }

    public void InitMesh()
    {
        if(mesh!=null)
        {
            return;
        }
        Vector2Int vertNum = gridNum + new Vector2Int(1, 1);
        Vector3[] vertexArr = new Vector3[vertNum.x * vertNum.y];
        Vector2[] uvArr = new Vector2[vertexArr.Length];
        int[] idxArr = new int[gridNum.x * gridNum.y * 6];

        for (int j=0;j < vertNum.y; j++)
        {
            for(int i=0;i<vertNum.x;i++)
            {
                Vector2 p = new Vector2(i / (float)gridNum.x,j/(float)gridNum.y);
                vertexArr[i + j * vertNum.x] = new Vector3(p.x,p.y,0);
                uvArr[i + j * vertNum.x] = p;
            }
        }

        for (int j = 0; j < gridNum.y; j++)
        {
            for (int i = 0; i < gridNum.x; i++)
            {
                int v00 = i + j * vertNum.x;
                int v10 = v00 + 1;
                int v01 = v00 + vertNum.x;
                int v11 = v01 + 1;

                int gridInx = i + j * gridNum.x;
                //服了,unity.Mesh 是顺时针顺序
                idxArr[6 * gridInx + 0] = v00;
                idxArr[6 * gridInx + 1] = v01;
                idxArr[6 * gridInx + 2] = v10;
                idxArr[6 * gridInx + 3] = v10;
                idxArr[6 * gridInx + 4] = v01;
                idxArr[6 * gridInx + 5] = v11;
            }
        }

        mesh = new Mesh();

        mesh.vertices = vertexArr;
        mesh.uv = uvArr;
        mesh.triangles = idxArr;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * maxBound);
    }

    void UpdateCameraParam()
    {
        var cam = mainCamera;
        var obj = cam.gameObject;
        var near = cam.nearClipPlane;
        int w = Screen.width;
        int h = Screen.height;

        // 根据near和fov算出pixH
        float verticleFOV = cam.fieldOfView / 180.0f * Mathf.PI;
        //tan(half) = (0.5*H)/(near)
        float screenH = Mathf.Tan(verticleFOV * 0.5f) * near * 2;
        float screenW = screenH * cam.aspect;
        float pixW = screenW / w;
        float pixH = screenH / h;

        var camPos = obj.transform.position;
        var camForward = obj.transform.forward;

        var screenPos = camPos + near * camForward;
        Vector3 screenU = obj.transform.right;
        Vector3 screenV = obj.transform.up;

        //!!! offset a bit to ensure cover
        //because maxBound!=infinite
        Vector3 screenDownLeft = screenPos + screenU * (-w / 2.0f)*1.1f * pixW + screenV * (-h / 2.0f) * 1.1f * pixH;
        Vector3 screenDownRight = screenPos + screenU * (w / 2.0f) * 1.1f * pixW + screenV * (-h / 2.0f) * 1.1f * pixH;
        Vector3 screenTopLeft = screenPos + screenU * (-w / 2.0f) * 1.1f * pixW + screenV * (h / 2.0f) * 1.1f * pixH;
        Vector3 screenTopRight = screenPos + screenU * (w / 2.0f) * 1.1f * pixW + screenV * (h / 2.0f) * 1.1f * pixH;

        debugPnts.Clear();
        debugPnts.Add(screenDownLeft);
        debugPnts.Add(screenDownRight);
        debugPnts.Add(screenTopLeft);
        debugPnts.Add(screenTopRight);


        Vector3 dir00 = (screenDownLeft - camPos).normalized;
        Ray ray00 = new Ray(camPos, dir00);
        Vector3 dir10 = (screenDownRight - camPos).normalized;
        Ray ray10 = new Ray(camPos, dir10);
        Vector3 dir01 = (screenTopLeft - camPos).normalized;
        Ray ray01 = new Ray(camPos, dir01);
        Vector3 dir11 = (screenTopRight - camPos).normalized;
        Ray ray11 = new Ray(camPos, dir11);

        var plane = new Plane();
        plane.p = new Vector3(0, seaHeight, 0);
        plane.n = new Vector3(0, 1, 0);

        float f00 = RayMath.RayCastPlane(ray00, plane);
        Vector3 p00 = Vector3.one * float.MinValue;
        if(f00>0)
        {
            p00 = ray00.pos + ray00.dir * f00;
            debugPnts.Add(p00);
        }
        else
        {
            p00 = ray00.pos + ray00.dir * maxBound;
            p00.y = seaHeight;
        }

        float f10 = RayMath.RayCastPlane(ray10, plane);
        Vector3 p10 = Vector3.one * float.MinValue;
        if (f10 > 0)
        {
            p10 = ray10.pos + ray10.dir * f10;
            debugPnts.Add(p10);
        }
        else
        {
            p10 = ray10.pos + ray10.dir * maxBound;
            p10.y = seaHeight;
        }

        float f01 = RayMath.RayCastPlane(ray01, plane);
        Vector3 p01 = Vector3.one * float.MinValue;
        if (f01 > 0)
        {
            p01 = ray01.pos + ray01.dir * f01;
            debugPnts.Add(p01);
        }
        else
        {
            p01 = ray10.pos + ray01.dir * maxBound;
            p01.y = seaHeight;
        }

        float f11 = RayMath.RayCastPlane(ray11, plane);
        Vector3 p11 = Vector3.one * float.MinValue;
        if (f11 > 0)
        {
            p11 = ray11.pos + ray11.dir * f11;
            debugPnts.Add(p11);
        }
        else
        {
            p11 = ray11.pos + ray11.dir * maxBound;
            p11.y = seaHeight;
        }

        debugLines.Clear();
        int lineCount = 0;
        if (p00.x > MINPOS)
        {
            debugLines.Add(new Line(screenDownLeft, p00));
            lineCount += 1;
        }
        if (p10.x > MINPOS)
        {
            debugLines.Add(new Line(screenDownRight, p10));
            lineCount += 1;
        }
        if (p01.x > MINPOS)
        {
            debugLines.Add(new Line(screenTopLeft, p01));
            lineCount += 1;
        }
        if (p11.x > MINPOS)
        {
            debugLines.Add(new Line(screenTopRight, p11));
            lineCount += 1;
        }

        if(lineCount ==4)
        {
            UpdateShaderParam(new ScreenMeshParam(p00,p10,p01,p11));
        }
    }

    void UpdateShaderParam(ScreenMeshParam param)
    {
        Debug.Log("UpdateShader");
        mr.material.SetVector("_p00", param.p00);
        mr.material.SetVector("_p10", param.p10);
        mr.material.SetVector("_p01", param.p01);
        mr.material.SetVector("_p11", param.p11);
    }
}
