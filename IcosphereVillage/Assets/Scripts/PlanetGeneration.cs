using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class PlanetGeneration : MonoBehaviour
{
    public List<Vector3> vertices;
    [SerializeField] public List<Triangle> triangles = new List<Triangle>(0);

    [SerializeField] List<Triangle> subdividedTris = new List<Triangle>(0);
    public MeshFilter filter;

    public float size;
    public int subdivisions;

    public Dictionary<(int, int), int> midPointsDic = new Dictionary<(int, int), int>();

    private void Update()
    {
        foreach (var tri in subdividedTris)
        {
            if (tri.neighbourA != -1)
                Debug.DrawLine(tri.centralPoint, subdividedTris[tri.neighbourA].centralPoint, Color.red);
            if (tri.neighbourB != -1)
                Debug.DrawLine(tri.centralPoint, subdividedTris[tri.neighbourB].centralPoint, Color.green);
            if (tri.neighbourC != -1)
                Debug.DrawLine(tri.centralPoint, subdividedTris[tri.neighbourC].centralPoint, Color.blue);
        }
    }

    async void Start()
    {
        float t = (1 + Mathf.Sqrt(5)) / 2;

        // ICOSPHERE VERTEXS 
        AddVertex(-1, t, 0);
        AddVertex(1, t, 0);
        AddVertex(-1, -t, 0);
        AddVertex(1, -t, 0);

        AddVertex(0, -1, t);
        AddVertex(0, 1, t);
        AddVertex(0, -1, -t);
        AddVertex(0, 1, -t);

        AddVertex(t, 0, -1);
        AddVertex(t, 0, 1);
        AddVertex(-t, 0, -1);
        AddVertex(-t, 0, 1);

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] = transform.position + (vertices[i] - transform.position).normalized * size;
        }


        // ICOSPHERE TRIANGLES
        AddTriangle(0, 11, 5);
        AddTriangle(0, 5, 1);
        AddTriangle(0, 1, 7);
        AddTriangle(0, 7, 10);
        AddTriangle(0, 10, 11);

        LinkTriangles(0, 1);
        LinkTriangles(1, 2);
        LinkTriangles(2, 3);
        LinkTriangles(3, 4);
        LinkTriangles(4, 0);

        AddTriangle(3, 9, 4);
        AddTriangle(3, 4, 2);
        AddTriangle(3, 2, 6);
        AddTriangle(3, 6, 8);
        AddTriangle(3, 8, 9);

        LinkTriangles(5, 6);
        LinkTriangles(6, 7);
        LinkTriangles(7, 8);
        LinkTriangles(8, 9);
        LinkTriangles(9, 5);

        AddTriangle(1, 5, 9);
        AddTriangle(5, 11, 4);
        AddTriangle(11, 10, 2);
        AddTriangle(10, 7, 6);
        AddTriangle(7, 1, 8);

        LinkTriangles(10, 1);
        LinkTriangles(11, 0);
        LinkTriangles(12, 4);
        LinkTriangles(13, 3);
        LinkTriangles(14, 2);

        AddTriangle(4, 9, 5);
        AddTriangle(2, 4, 11);
        AddTriangle(6, 2, 10);
        AddTriangle(8, 6, 7);
        AddTriangle(9, 8, 1);

        LinkTriangles(15, 5);
        LinkTriangles(16, 6);
        LinkTriangles(17, 7);
        LinkTriangles(18, 8);
        LinkTriangles(19, 9);

        LinkTriangles(10, 15);
        LinkTriangles(11, 16);
        LinkTriangles(12, 17);
        LinkTriangles(13, 18);
        LinkTriangles(14, 19);

        LinkTriangles(10, 19);
        LinkTriangles(11, 15);
        LinkTriangles(12, 16);
        LinkTriangles(13, 17);
        LinkTriangles(14, 18);

        // SUBDIVIDE

        for (int i = 0; i < subdivisions; i++)
        {
            await Task.Delay(100);
            SubdivideSphere();
        }


        SetElevation();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = GetTriangleIndexesArray(triangles.ToArray());
        mesh.RecalculateNormals();
        filter.mesh = mesh;
    }

    private void SetElevation()
    {
        
    }

    private void LinkTriangles(int a, int b)
    {
        if (triangles[a].neighbourA == -1) triangles[a].neighbourA = b;
        else if (triangles[a].neighbourB == -1) triangles[a].neighbourB = b;
        else if (triangles[a].neighbourC == -1) triangles[a].neighbourC = b;
        else Debug.LogWarning("SHOULD NOT HAPPEN !");

        if (triangles[b].neighbourA == -1) triangles[b].neighbourA = a;
        else if (triangles[b].neighbourB == -1) triangles[b].neighbourB = a;
        else if (triangles[b].neighbourC == -1) triangles[b].neighbourC = a;
        else Debug.LogWarning("SHOULD NOT HAPPEN !");
    }

    private void LinkSubdivided(int a, int b)
    {
        if (subdividedTris[a].neighbourA == -1) subdividedTris[a].neighbourA = b;
        else if (subdividedTris[a].neighbourB == -1) subdividedTris[a].neighbourB = b;
        else if (subdividedTris[a].neighbourC == -1) subdividedTris[a].neighbourC = b;
        else Debug.LogWarning("SHOULD NOT HAPPEN !");
        
        if (subdividedTris[b].neighbourA == -1) subdividedTris[b].neighbourA = a;
        else if (subdividedTris[b].neighbourB == -1) subdividedTris[b].neighbourB = a;
        else if (subdividedTris[b].neighbourC == -1) subdividedTris[b].neighbourC = a;
        else Debug.LogWarning("SHOULD NOT HAPPEN !");
    }

    private void OnDrawGizmos()
    {
        foreach (var v in vertices)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(v, 0.1f);
        }

        foreach (var tri in triangles)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(tri.centralPoint, 0.1f);
        }
    }

    void AddVertex(float x, float y, float z)
    {
        vertices.Add(new Vector3(x, y, z));
    }


    void AddTriangle(int a, int b, int c)
    {
        Triangle triangle = new Triangle();
        triangle.a = a;
        triangle.b = b;
        triangle.c = c;
        triangle.neighbourA = triangle.neighbourB = triangle.neighbourC = -1;
        triangle.centralPoint = (vertices[a] + vertices[b] + vertices[c]) / 3;
        triangles.Add(triangle);
    }

     void SubdivideSphere()
    {
        midPointsDic.Clear();
        subdividedTris.Clear();

        for (int i = 0; i < triangles.Count; i++)
        {
            Triangle[] tris = SubdivideTriangle(triangles[i]);
        }

        Debug.Log(subdividedTris.Count);
        triangles = new List<Triangle>();
        foreach (var tr in subdividedTris)
        {
            triangles.Add(tr);
        }
    }

    Triangle[] SubdivideTriangle(Triangle triangleParent)
    {
        Triangle[] newTris = new Triangle[4];
        for (int i = 0; i < 4; i++)
        {
            newTris[i] = new Triangle();
            newTris[i].neighbourA = newTris[i].neighbourB = newTris[i].neighbourC = -1;
        }

        triangleParent.vertexToChildren = new Dictionary<int, int>();

        subdividedTris.Add(newTris[0]);
        subdividedTris.Add(newTris[1]);
        subdividedTris.Add(newTris[2]);
        subdividedTris.Add(newTris[3]);
        
        #region Get Middle Points

        int middlePointA = GetMiddlePoint(triangleParent.a, triangleParent.b);
        int middlePointB = GetMiddlePoint(triangleParent.b, triangleParent.c);
        int middlePointC = GetMiddlePoint(triangleParent.c, triangleParent.a);

        #endregion

        #region Set Middle Triangle

        newTris[0].a = middlePointA;
        newTris[0].b = middlePointB;
        newTris[0].c = middlePointC;

        newTris[0].centralPoint = (vertices[newTris[0].a] + vertices[newTris[0].b] + vertices[newTris[0].c]) / 3;
        
        LinkSubdivided(subdividedTris.Count - 4,subdividedTris.Count - 3);
        LinkSubdivided(subdividedTris.Count - 4,subdividedTris.Count - 2);
        LinkSubdivided(subdividedTris.Count - 4,subdividedTris.Count - 1);
        
        #endregion

        #region Set First Child Triangle

        newTris[1].a = triangleParent.a;
        newTris[1].b = middlePointA;
        newTris[1].c = middlePointC;

        triangleParent.vertexToChildren.Add(triangleParent.a, newTris[0].neighbourA);

        newTris[1].centralPoint = (vertices[newTris[1].a] + vertices[newTris[1].b] + vertices[newTris[1].c]) / 3;

        //LinkSubdivided(newTris[0].neighbourA, subdividedTris.Count - 4);
        
        if (triangles[triangleParent.neighbourA].vertexToChildren.ContainsKey(triangleParent.a))
        {
            LinkSubdivided(newTris[0].neighbourA,
                triangles[triangleParent.neighbourA].vertexToChildren[triangleParent.a]);
        }

        if (triangles[triangleParent.neighbourB].vertexToChildren.ContainsKey(triangleParent.a))
        {
            LinkSubdivided(newTris[0].neighbourA,
                triangles[triangleParent.neighbourB].vertexToChildren[triangleParent.a]);
        }

        if (triangles[triangleParent.neighbourC].vertexToChildren.ContainsKey(triangleParent.a))
        {
            LinkSubdivided(newTris[0].neighbourA,
                triangles[triangleParent.neighbourC].vertexToChildren[triangleParent.a]);
        }

        #endregion

        #region Set Second Child Triangle

        newTris[2].a = triangleParent.b;
        newTris[2].b = middlePointB;
        newTris[2].c = middlePointA;

        triangleParent.vertexToChildren.Add(triangleParent.b, newTris[0].neighbourB);

        newTris[2].centralPoint = (vertices[newTris[2].a] + vertices[newTris[2].b] + vertices[newTris[2].c]) / 3;
        
        //LinkSubdivided(newTris[0].neighbourB,subdividedTris.Count-4);
        

        if (triangles[triangleParent.neighbourA].vertexToChildren.ContainsKey(triangleParent.b))
        {
            LinkSubdivided(newTris[0].neighbourB,
                triangles[triangleParent.neighbourA].vertexToChildren[triangleParent.b]);
        }

        if (triangles[triangleParent.neighbourB].vertexToChildren.ContainsKey(triangleParent.b))
        {
            LinkSubdivided(newTris[0].neighbourB,
                triangles[triangleParent.neighbourB].vertexToChildren[triangleParent.b]);
        }

        if (triangles[triangleParent.neighbourC].vertexToChildren.ContainsKey(triangleParent.b))
        {
            LinkSubdivided(newTris[0].neighbourB,
                triangles[triangleParent.neighbourC].vertexToChildren[triangleParent.b]);
        }

        #endregion

        #region Set Third Child Triangle

        newTris[3].a = triangleParent.c;
        newTris[3].b = middlePointC;
        newTris[3].c = middlePointB;

        triangleParent.vertexToChildren.Add(triangleParent.c, newTris[0].neighbourC);

        newTris[3].centralPoint = (vertices[newTris[3].a] + vertices[newTris[3].b] + vertices[newTris[3].c]) / 3;

        //LinkSubdivided(newTris[0].neighbourC,subdividedTris.Count-4);
        

        if (triangles[triangleParent.neighbourA].vertexToChildren.ContainsKey(triangleParent.c))
        {
            LinkSubdivided(newTris[0].neighbourC,
                triangles[triangleParent.neighbourA].vertexToChildren[triangleParent.c]);
        }

        if (triangles[triangleParent.neighbourB].vertexToChildren.ContainsKey(triangleParent.c))
        {
            LinkSubdivided(newTris[0].neighbourC,
                triangles[triangleParent.neighbourB].vertexToChildren[triangleParent.c]);
        }

        if (triangles[triangleParent.neighbourC].vertexToChildren.ContainsKey(triangleParent.c))
        {
            LinkSubdivided(newTris[0].neighbourC,
                triangles[triangleParent.neighbourC].vertexToChildren[triangleParent.c]);
        }

        #endregion


        return newTris;
    }

    int GetMiddlePoint(int point1, int point2)
    {
        if (midPointsDic.ContainsKey((point1, point2)))
        {
            return midPointsDic[(point1, point2)];
        }

        if (midPointsDic.ContainsKey((point2, point1)))
        {
            return midPointsDic[(point2, point1)];
        }

        Vector3 midPoint = Vector3.Lerp(vertices[point1], vertices[point2], 0.5f);
        midPoint = transform.position + (midPoint - transform.position).normalized * size;
        vertices.Add(midPoint);
        midPointsDic.Add((point1,point2),vertices.Count - 1);

        return vertices.Count - 1;
    }

    int[] GetTriangleIndexesArray(Triangle[] tris)
    {
        List<int> indexes = new List<int>(0);
        foreach (var tri in tris)
        {
            indexes.Add(tri.a);
            indexes.Add(tri.b);
            indexes.Add(tri.c);
        }

        return indexes.ToArray();
    }
}

[Serializable]
public class Triangle
{
    public int a, b, c = -1;
    public Vector3 centralPoint;
    public int neighbourA, neighbourB, neighbourC;
    public Dictionary<int, int> vertexToChildren = new Dictionary<int, int>();
    public int heightLevel = 0;
}