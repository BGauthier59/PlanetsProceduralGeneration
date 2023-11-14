using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.Serialization;

public class PlanetGeneration : MonoBehaviour
{
    public List<Vector3> vertices;
    [SerializeField] public List<Triangle> triangles = new List<Triangle>(0);

    [SerializeField] List<Triangle> subdividedTris = new List<Triangle>(0);
    public MeshFilter filter;

    public float size;
    public float heightSize;
    public int subdivisions;

    public TMP_Text label;

    public Dictionary<(int, int), int> midPointsDic = new Dictionary<(int, int), int>();

    private void OnValidate()
    {
    }

    private void Update()
    {
        return;
        foreach (var tri in subdividedTris)
        {
            Debug.DrawRay(tri.centralPoint, tri.normal, Color.magenta);
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
        filter.mesh = null;

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

        await SetElevation();
        SetOrganicDisplacement();

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = GetTriangleIndexesArray(triangles.ToArray());
        mesh.RecalculateNormals();
        filter.mesh = mesh;
    }


    private async Task SetElevation()
    {
        Vector3 a, b, c;
        foreach (var tri in triangles)
        {
            //tri.heightLevel = RandomGenerator.GetRandomValueInt(0, 2);
            tri.heightLevel = 1;
            int indexA, indexB, indexC;
            indexA = indexB = indexC = -1;

            // Elevates vertices
            for (int i = 0; i < tri.heightLevel; i++)
            {
                a = vertices[tri.indices[0]] + tri.normal * heightSize * (i + 1);
                b = vertices[tri.indices[1]] + tri.normal * heightSize * (i + 1);
                c = vertices[tri.indices[2]] + tri.normal * heightSize * (i + 1);

                #region Check Neighbours

                // Check neighbours
                CorrectTrianglePositions(tri, a, b, c, ref indexA, ref indexB, ref indexC);

                int a_add = 0;
                if (indexA == -1)
                {
                    vertices.Add(a);
                    a_add = 1;
                }

                int b_add = 0;
                if (indexB == -1)
                {
                    vertices.Add(b);
                    b_add = 1;
                }

                int c_add = 0;
                if (indexC == -1)
                {
                    vertices.Add(c);
                    c_add = 1;
                }

                Vector3Int vertex = new Vector3Int(indexA == -1 ? vertices.Count - a_add - b_add - c_add : indexA,
                    indexB == -1 ? vertices.Count - b_add - c_add : indexB,
                    indexC == -1 ? vertices.Count - c_add : indexC);

                tri.elevationTriangle.Add(vertex);

                #endregion
            }
        }
    }


    private void CorrectTrianglePositions(Triangle tri, Vector3 a, Vector3 b, Vector3 c,
        ref int iA, ref int iB, ref int iC)
    {
        for (int w = 0; w < tri.indices.Length; w++)
        {
            int uniqueVertexIndex = -1;

            if (triangles[tri.neighbourA].heightLevel == tri.heightLevel)
            {
                CorrectVertexPosition(tri, triangles[tri.neighbourA], w, a, b, c, ref iA, ref iB,
                    ref iC, ref uniqueVertexIndex);
            }

            if (triangles[tri.neighbourB].heightLevel == tri.heightLevel)
            {
                CorrectVertexPosition(tri, triangles[tri.neighbourB], w, a, b, c, ref iA, ref iB,
                    ref iC, ref uniqueVertexIndex);
            }

            if (triangles[tri.neighbourC].heightLevel == tri.heightLevel)
            {
                CorrectVertexPosition(tri, triangles[tri.neighbourC], w, a, b, c, ref iA, ref iB,
                    ref iC, ref uniqueVertexIndex);
            }
        }
    }

    private void CorrectVertexPosition(Triangle tri, Triangle neighbour, int w, Vector3 a, Vector3 b, Vector3 c,
        ref int iA, ref int iB, ref int iC, ref int unique)
    {

        int index = tri.indices[w];

        for (int j = 0; j < neighbour.indices.Length; j++)
        {
            int nindex = neighbour.indices[j];

            if (index == nindex)
            {
                
                
                int neighbourVertexIndex = j switch
                {
                    0 => neighbour.elevationTriangle[tri.heightLevel - 1].x,
                    1 => neighbour.elevationTriangle[tri.heightLevel - 1].y,
                    2 => neighbour.elevationTriangle[tri.heightLevel - 1].z,
                };

                if (unique == -1) unique = neighbourVertexIndex;
                
                Vector3 neighbourVertexPos = vertices[unique];

                Vector3 thisVertexPos = w switch
                {
                    0 => a,
                    1 => b,
                    2 => c,
                };
                
                
                Vector3 correctedVertexPos = (neighbourVertexPos + thisVertexPos) * 0.5f;

                vertices[neighbourVertexIndex] = correctedVertexPos;

                Vector3Int v;
                switch (j)
                {
                    case 0:
                        v = neighbour.elevationTriangle[tri.heightLevel - 1];
                        v.x = unique;
                        neighbour.elevationTriangle[tri.heightLevel - 1] = v;
                        break;
                    case 1:
                        v = neighbour.elevationTriangle[tri.heightLevel - 1];
                        v.y = unique;
                        neighbour.elevationTriangle[tri.heightLevel - 1] = v;
                        break;
                    case 2:
                        v = neighbour.elevationTriangle[tri.heightLevel - 1];
                        v.z = unique;
                        neighbour.elevationTriangle[tri.heightLevel - 1] = v;
                        break;
                }

                switch (w)
                {
                    case 0:
                        iA = unique;
                        break;
                    case 1:
                        iB = unique;
                        break;
                    case 2:
                        iC = unique;
                        break;
                }


                Debug.DrawLine(neighbourVertexPos, correctedVertexPos, Color.yellow, 200);
                Debug.DrawLine(thisVertexPos, correctedVertexPos, Color.red, 200);

                break;
            }
        }
    }

    private void SetOrganicDisplacement()
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
            Gizmos.DrawSphere(v, 0.01f);
        }

        foreach (var tri in triangles)
        {
            Gizmos.color = Color.red;
            //Gizmos.DrawSphere(tri.centralPoint, 0.1f);
        }
    }

    void AddVertex(float x, float y, float z)
    {
        vertices.Add(new Vector3(x, y, z));
    }


    void AddTriangle(int a, int b, int c)
    {
        Triangle triangle = new Triangle();
        triangle.indices[0] = a;
        triangle.indices[1] = b;
        triangle.indices[2] = c;
        triangle.neighbourA = triangle.neighbourB = triangle.neighbourC = -1;
        triangle.centralPoint = (vertices[a] + vertices[b] + vertices[c]) / 3;

        triangles.Add(triangle);

        SetNormal(triangle);
    }

    void SetNormal(Triangle tri)
    {
        tri.normal = (tri.centralPoint - transform.position).normalized;
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

        int middlePointA = GetMiddlePoint(triangleParent.indices[0], triangleParent.indices[1]);
        int middlePointB = GetMiddlePoint(triangleParent.indices[1], triangleParent.indices[2]);
        int middlePointC = GetMiddlePoint(triangleParent.indices[2], triangleParent.indices[0]);

        #endregion

        #region Set Middle Triangle

        newTris[0].indices[0] = middlePointA;
        newTris[0].indices[1] = middlePointB;
        newTris[0].indices[2] = middlePointC;

        newTris[0].centralPoint = (vertices[newTris[0].indices[0]] + vertices[newTris[0].indices[1]] +
                                   vertices[newTris[0].indices[2]]) / 3;

        LinkSubdivided(subdividedTris.Count - 4, subdividedTris.Count - 3);
        LinkSubdivided(subdividedTris.Count - 4, subdividedTris.Count - 2);
        LinkSubdivided(subdividedTris.Count - 4, subdividedTris.Count - 1);

        #endregion

        #region Set First Child Triangle

        newTris[1].indices[0] = triangleParent.indices[0];
        newTris[1].indices[1] = middlePointA;
        newTris[1].indices[2] = middlePointC;

        triangleParent.vertexToChildren.Add(triangleParent.indices[0], newTris[0].neighbourA);

        newTris[1].centralPoint = (vertices[newTris[1].indices[0]] + vertices[newTris[1].indices[1]] +
                                   vertices[newTris[1].indices[2]]) / 3;

        //LinkSubdivided(newTris[0].neighbourA, subdividedTris.Count - 4);

        if (triangles[triangleParent.neighbourA].vertexToChildren.ContainsKey(triangleParent.indices[0]))
        {
            LinkSubdivided(newTris[0].neighbourA,
                triangles[triangleParent.neighbourA].vertexToChildren[triangleParent.indices[0]]);
        }

        if (triangles[triangleParent.neighbourB].vertexToChildren.ContainsKey(triangleParent.indices[0]))
        {
            LinkSubdivided(newTris[0].neighbourA,
                triangles[triangleParent.neighbourB].vertexToChildren[triangleParent.indices[0]]);
        }

        if (triangles[triangleParent.neighbourC].vertexToChildren.ContainsKey(triangleParent.indices[0]))
        {
            LinkSubdivided(newTris[0].neighbourA,
                triangles[triangleParent.neighbourC].vertexToChildren[triangleParent.indices[0]]);
        }

        #endregion

        #region Set Second Child Triangle

        newTris[2].indices[0] = triangleParent.indices[1];
        newTris[2].indices[1] = middlePointB;
        newTris[2].indices[2] = middlePointA;

        triangleParent.vertexToChildren.Add(triangleParent.indices[1], newTris[0].neighbourB);

        newTris[2].centralPoint = (vertices[newTris[2].indices[0]] + vertices[newTris[2].indices[1]] +
                                   vertices[newTris[2].indices[2]]) / 3;

        //LinkSubdivided(newTris[0].neighbourB,subdividedTris.Count-4);


        if (triangles[triangleParent.neighbourA].vertexToChildren.ContainsKey(triangleParent.indices[1]))
        {
            LinkSubdivided(newTris[0].neighbourB,
                triangles[triangleParent.neighbourA].vertexToChildren[triangleParent.indices[1]]);
        }

        if (triangles[triangleParent.neighbourB].vertexToChildren.ContainsKey(triangleParent.indices[1]))
        {
            LinkSubdivided(newTris[0].neighbourB,
                triangles[triangleParent.neighbourB].vertexToChildren[triangleParent.indices[1]]);
        }

        if (triangles[triangleParent.neighbourC].vertexToChildren.ContainsKey(triangleParent.indices[1]))
        {
            LinkSubdivided(newTris[0].neighbourB,
                triangles[triangleParent.neighbourC].vertexToChildren[triangleParent.indices[1]]);
        }

        #endregion

        #region Set Third Child Triangle

        newTris[3].indices[0] = triangleParent.indices[2];
        newTris[3].indices[1] = middlePointC;
        newTris[3].indices[2] = middlePointB;

        triangleParent.vertexToChildren.Add(triangleParent.indices[2], newTris[0].neighbourC);

        newTris[3].centralPoint = (vertices[newTris[3].indices[0]] + vertices[newTris[3].indices[1]] +
                                   vertices[newTris[3].indices[2]]) / 3;

        //LinkSubdivided(newTris[0].neighbourC,subdividedTris.Count-4);


        if (triangles[triangleParent.neighbourA].vertexToChildren.ContainsKey(triangleParent.indices[2]))
        {
            LinkSubdivided(newTris[0].neighbourC,
                triangles[triangleParent.neighbourA].vertexToChildren[triangleParent.indices[2]]);
        }

        if (triangles[triangleParent.neighbourB].vertexToChildren.ContainsKey(triangleParent.indices[2]))
        {
            LinkSubdivided(newTris[0].neighbourC,
                triangles[triangleParent.neighbourB].vertexToChildren[triangleParent.indices[2]]);
        }

        if (triangles[triangleParent.neighbourC].vertexToChildren.ContainsKey(triangleParent.indices[2]))
        {
            LinkSubdivided(newTris[0].neighbourC,
                triangles[triangleParent.neighbourC].vertexToChildren[triangleParent.indices[2]]);
        }

        #endregion

        #region Set Normals

        SetNormal(newTris[0]);
        SetNormal(newTris[1]);
        SetNormal(newTris[2]);
        SetNormal(newTris[3]);

        Instantiate(label, newTris[0].centralPoint, quaternion.identity).text = (subdividedTris.Count-4).ToString();
        Instantiate(label, newTris[1].centralPoint, quaternion.identity).text = (subdividedTris.Count-3).ToString();
        Instantiate(label, newTris[2].centralPoint, quaternion.identity).text = (subdividedTris.Count-2).ToString();
        Instantiate(label, newTris[3].centralPoint, quaternion.identity).text = (subdividedTris.Count-1).ToString();
        
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
        midPointsDic.Add((point1, point2), vertices.Count - 1);

        return vertices.Count - 1;
    }

    int[] GetTriangleIndexesArray(Triangle[] tris)
    {
        List<int> indexes = new List<int>(0);

        foreach (var tri in tris)
        {
            if (tri.heightLevel == 0)
            {
                indexes.Add(tri.indices[0]);
                indexes.Add(tri.indices[1]);
                indexes.Add(tri.indices[2]);
            }
            else
            {
                indexes.Add(tri.elevationTriangle[tri.heightLevel - 1].x);
                indexes.Add(tri.elevationTriangle[tri.heightLevel - 1].y);
                indexes.Add(tri.elevationTriangle[tri.heightLevel - 1].z);
            }
        }

        return indexes.ToArray();
    }
}

[Serializable]
public class Triangle
{
    public int[] indices = new int[3] { -1, -1, -1 };
    public Vector3 centralPoint;
    public Vector3 normal;
    public int neighbourA, neighbourB, neighbourC;
    public Dictionary<int, int> vertexToChildren = new Dictionary<int, int>();

    public List<Vector3Int> elevationTriangle = new List<Vector3Int>();
    public int heightLevel = 0;
}