using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.Serialization;

public class Planet : MonoBehaviour
{
    public List<Vector3> vertices;
    [SerializeField] public List<Triangle> triangles = new List<Triangle>(0);
    [SerializeField] public List<Vertex> gridVertices = new List<Vertex>(0);

    [SerializeField] List<Triangle> subdividedTris = new List<Triangle>(0);
    [SerializeField] private List<Rectangle> rects = new List<Rectangle>(0);
    public MeshFilter filter;

    public float size;
    public float heightSize, elevationScaleFactor;
    public int subdivisions;
    public int maxHeight;

    public uint seed;
    public float noiseSize;

    [SerializeField] private Transform waterSphere;

    public Noise elevationNoise;

    public Dictionary<(int, int), int> midPointsDic = new Dictionary<(int, int), int>();

    private void OnValidate()
    {
    }

    private void Update()
    {
        /*
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
        

        for (int i = 0; i < gridVertices.Count; i++)
        {
            foreach (var tri in gridVertices[i].triangles)
            {
                Debug.DrawLine(vertices[i],triangles[tri].centralPoint,Color.HSVToRGB((float)i/gridVertices.Count,1,1));
            }
        }
        */

    }

    public async void Initialize()
    {
        elevationNoise = new Noise((int)seed);

        RandomGenerator.SetSeed(seed);
        
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
            vertices[i] = vertices[i].normalized * size;
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

        Debug.Log(vertices.Count);
        
        SetElevationGrouped();
        SetOrganicDisplacement();

        CreateAllRectangles();
        
        Mesh mesh = new Mesh();


        int[] tris = GetTriangleIndexesArray(triangles.ToArray());
        mesh.vertices = vertices.ToArray();
        mesh.triangles = tris;
        
        mesh.RecalculateNormals();
        filter.mesh = mesh;

        waterSphere.localScale = Vector3.one * (size * 2 + heightSize * 0.5f);
    }



    #region 2eme Cercle de l'enfer [ Algo par groupes ]

    void SetElevationGrouped()
    {
        Vector3 triPos;
        int it = 0;
        foreach (var tri in triangles)
        {
            
            //tri.heightLevel = RandomGenerator.GetRandomValueInt(0, 2) * 2;
            //tri.heightLevel = RandomGenerator.GetRandomValueInt(0, maxHeight + 1);
            //tri.heightLevel = maxHeight;
            
            triPos = tri.centralPoint;
            triPos *= noiseSize;
            float v = (elevationNoise.Evaluate(triPos) + 1) * 0.5f;

            //tri.heightLevel = (int) (v * (maxHeight + 1));
            tri.heightLevel = 0;
            
            if(it == 0) tri.heightLevel = maxHeight;
            it++;

            for (int i = 0; i < tri.heightLevel; i++)
            {
                tri.elevationTriangle.Add(Vector3Int.zero);
            }
        }


        // REFAIRE CA POUR CHAQUE NIVEAU D'ELEVATION

        for (int h = 0; h < maxHeight; h++)
        {
            int heightLevel = h + 1;

            for (int i = 0; i < gridVertices.Count; i++)
            {
                Vertex gridVertex = gridVertices[i];

                List<int> alreadyVisited = new List<int>(0);

                // POUR CHAQUE TRIANGLE DU VERTEX
                for (int j = 0; j < gridVertex.triangles.Count; j++)
                {
                    // SI LE TRIANGLE EST A LA BONNE HAUTEUR ET N'A PAS ETE VISITE
                    if (triangles[gridVertex.triangles[j]].heightLevel >= heightLevel && !alreadyVisited.Contains(j))
                    {
                        // CALCULE LE GROUPE DE VOISINS DE MEME HAUTEUR
                        List<int> group = new List<int>();
                        FormElevationGroup(gridVertex.triangles.ToArray(), j, heightLevel,ref group);
                    
                        Vector3 midPoint = Vector3.zero;

                        for (int k = 0; k < group.Count; k++)
                        {
                            // RENSEIGNE LES TRIANGLES VISITES
                            alreadyVisited.Add(group[k]);
                        
                            // CALCULE LE POINT D'ELEVATION
                            Vector3 heightPoint = (triangles[gridVertex.triangles[group[k]]].centralPoint + (vertices[i] - triangles[gridVertex.triangles[group[k]]].centralPoint) * elevationScaleFactor)+
                                                  triangles[gridVertex.triangles[group[k]]].normal * heightSize *
                                                  heightLevel;
                        
                            // L'AJOUTE A LA MOYENNE
                            midPoint += heightPoint;
                        }
                    
                        // FAIS LA MOYENNE DES POINTS DU GROUPE
                        midPoint /= group.Count;
                    
                        // AJOUTE LE MID POINT AUX VERTICES
                        vertices.Add(midPoint);
                    
                        // POUR TOUS LES TRIANGLES DU GROUPE...
                        for (int k = 0; k < group.Count; k++)
                        {
                            Vector3Int v = triangles[gridVertex.triangles[group[k]]].elevationTriangle[heightLevel - 1];
                            int vertIndex = 0;
                        
                            for (int l = 0; l < 3; l++)
                            {
                                if (triangles[gridVertex.triangles[group[k]]].indices[l] == i)
                                {
                                    vertIndex = l;
                                    break;
                                }
                            }
                        
                            // ... ON SET L'INDEX DU POINT D'ELEVATION ( mid Point calculé précédemment )
                            switch (vertIndex)
                            {
                                case 0:
                                    v.x = vertices.Count - 1;
                                    break;
                                case 1:
                                    v.y = vertices.Count - 1;
                                    break;
                                case 2:
                                    v.z = vertices.Count - 1;
                                    break;
                            }

                            triangles[gridVertex.triangles[group[k]]].elevationTriangle[heightLevel - 1] = v;
                        }

                        Debug.Log("GROUP FORMED AROUND VERTEX " + i + ", AMOUNT: " + group.Count);
                    
                    }
                }
            } 
        }
        
        
    }

    void FormElevationGroup(int[] vertexTris, int origin,int heightLvl, ref List<int> group)
    {
        group.Add(origin);
        
        // SI LE GROUPE CONTIENT DEJA 6 TRIANGLES ( maximum ) ON STOP
        if (group.Count >= 6) return;

        // POUR CHAQUE VOISIN DE ORIGIN
        for (int nIndex = 0; nIndex < 3; nIndex++)
        {
            int neighbour = -1;
            
            switch (nIndex)
            {
                case 0:
                    neighbour = triangles[vertexTris[origin]].neighbourA;
                    break;
                case 1:
                    neighbour = triangles[vertexTris[origin]].neighbourB;
                    break;
                case 2:
                    neighbour = triangles[vertexTris[origin]].neighbourC;
                    break;
            }
            
            // SI VOISIN DE ORIGIN EST PRESENT DANS L'ARRAY DE TRIANGLES FOURNI
            for (int testedIndex = 0; testedIndex < vertexTris.Length; testedIndex++)
            {
                if (vertexTris[testedIndex] == neighbour)
                {
                    // SI LE VOISIN DE ORIGIN EST A LA BONNE HAUTEUR ET N'EST PAS DEJA DANS LE GROUPE
                    if (triangles[neighbour].heightLevel >= heightLvl && !group.Contains(testedIndex))
                    {
                        // ALORS LE VOISIN EST AJOUTE AU GROUPE ET VA LUI AUSSI CHECKER CES VOISINS
                        FormElevationGroup(vertexTris,testedIndex,heightLvl,ref group);
                    }
                    break;
                }
            }
        }
    }

    #endregion

    #region 3eme Cercle de l'enfer ( L'enfer des reliaisons )

    void CreateAllRectangles()
    {
        // POUR CHAQUE TRIANGLE
        for (int i = 0; i < triangles.Count; i++)
        {
            // POUR CHAQUE VOISIN DU TRIANGLE
            for (int j = 0; j < 3; j++)
            {
                Triangle neighbour = null;
                switch (j)
                {
                    case 0:
                        neighbour = triangles[triangles[i].neighbourA];
                        break;
                    case 1:
                        neighbour = triangles[triangles[i].neighbourB];
                        break;
                    case 2:
                        neighbour = triangles[triangles[i].neighbourC];
                        break;
                }
                
                // SI TRIANGLE PLUS HAUT QUE NEIGHBOUR
                if (triangles[i].heightLevel > neighbour.heightLevel)
                {
                    
                    // CHERCHER LES VERTEX COMMUNS AVEC NEIGHBOUR
                    int vert1i = -1;
                    int vert2i = -1;
                    
                    
                    // POUR CHAQUE VERTEX DU TRIANGLE
                    for (int k = 0; k < 3; k++)
                    {
                        for (int l = 0; l < 3; l++)
                        {
                            if (triangles[i].indices[k] == neighbour.indices[l])
                            {
                                if (vert1i == -1) vert1i = k;
                                else vert2i = k;
                            }
                        }
                    }
                    
                    // CA C'EST UN ECHANGE DE VARIABLE ( niveau CP )
                    if (vert1i == 0 && vert2i == 2) (vert1i, vert2i) = (vert2i, vert1i);
                    
                    for (int h = triangles[i].heightLevel - 1; h >= neighbour.heightLevel; h--)
                    {
                        int a = vert1i switch
                        {
                            0 => triangles[i].elevationTriangle[h].x,
                            1 => triangles[i].elevationTriangle[h].y,
                            2 => triangles[i].elevationTriangle[h].z,
                        };
                        int b = vert2i switch
                        {
                            0 => triangles[i].elevationTriangle[h].x,
                            1 => triangles[i].elevationTriangle[h].y,
                            2 => triangles[i].elevationTriangle[h].z,
                        };
                        int c;
                        int d;
                        
                        if (h == 0)
                        {

                            c = triangles[i].indices[vert1i];
                            d = triangles[i].indices[vert2i];
                        }
                        else
                        {
                           
                            c = vert1i switch
                            {
                                0 => triangles[i].elevationTriangle[h-1].x,
                                1 => triangles[i].elevationTriangle[h-1].y,
                                2 => triangles[i].elevationTriangle[h-1].z,
                            };
                            d = vert2i switch
                            {
                                0 => triangles[i].elevationTriangle[h-1].x,
                                1 => triangles[i].elevationTriangle[h-1].y,
                                2 => triangles[i].elevationTriangle[h-1].z,
                            };
                        }
                        
                        CreateRect(a,b,c,d);
                    }
                }
            }
        }
        
        //CreateRect();
    }

    void CreateRect(int a,int b,int c,int d)
    {
        Rectangle rect = new Rectangle();
        rect.a = a;
        rect.b = b;
        rect.c = c;
        rect.d = d;
        rects.Add(rect);
    }

    #endregion
    
    
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
            Gizmos.DrawSphere(v, 0.1f);
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
        gridVertices.Add(new Vertex());
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
        
        gridVertices[a].triangles.Add(triangles.Count-1);
        gridVertices[b].triangles.Add(triangles.Count-1);
        gridVertices[c].triangles.Add(triangles.Count-1);

        SetNormal(triangle);
    }

    void SetNormal(Triangle tri)
    {
        tri.normal = tri.centralPoint.normalized;
    }

    void SubdivideSphere()
    {
        midPointsDic.Clear();
        subdividedTris.Clear();
        foreach (var vert in gridVertices)
        {
            vert.triangles.Clear();
        }

        for (int i = 0; i < triangles.Count; i++)
        {
            Triangle[] tris = SubdivideTriangle(triangles[i]);
        }
        
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
        
        gridVertices[middlePointA].triangles.Add(subdividedTris.Count - 4);
        gridVertices[middlePointB].triangles.Add(subdividedTris.Count - 4);
        gridVertices[middlePointC].triangles.Add(subdividedTris.Count - 4);

        #endregion

        #region Set First Child Triangle

        newTris[1].indices[0] = triangleParent.indices[0];
        newTris[1].indices[1] = middlePointA;
        newTris[1].indices[2] = middlePointC;

        triangleParent.vertexToChildren.Add(triangleParent.indices[0], newTris[0].neighbourA);

        newTris[1].centralPoint = (vertices[newTris[1].indices[0]] + vertices[newTris[1].indices[1]] +
                                   vertices[newTris[1].indices[2]]) / 3;
        
        gridVertices[triangleParent.indices[0]].triangles.Add(newTris[0].neighbourA);
        gridVertices[middlePointA].triangles.Add(newTris[0].neighbourA);
        gridVertices[middlePointC].triangles.Add(newTris[0].neighbourA);

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

        
        gridVertices[triangleParent.indices[1]].triangles.Add(newTris[0].neighbourB);
        gridVertices[middlePointB].triangles.Add(newTris[0].neighbourB);
        gridVertices[middlePointA].triangles.Add(newTris[0].neighbourB);
        
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

        gridVertices[triangleParent.indices[2]].triangles.Add(newTris[0].neighbourC);
        gridVertices[middlePointC].triangles.Add(newTris[0].neighbourC);
        gridVertices[middlePointB].triangles.Add(newTris[0].neighbourC);
        
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
        midPoint = (midPoint).normalized * size;
        vertices.Add(midPoint);
        gridVertices.Add(new Vertex());
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

        Vector3 newVert;
        foreach (var r in rects)
        {
            newVert = vertices[r.a];
            vertices.Add(newVert);
            
            newVert = vertices[r.b];
            vertices.Add(newVert);
            
            newVert = vertices[r.c];
            vertices.Add(newVert);
            
            newVert = vertices[r.d];
            vertices.Add(newVert);
            
            indexes.Add(vertices.Count-3);
            indexes.Add(vertices.Count-4);
            indexes.Add(vertices.Count-2);
            indexes.Add(vertices.Count-2);
            indexes.Add(vertices.Count-1);
            indexes.Add(vertices.Count-3);
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

[Serializable]
public class Vertex
{
    public List<int> triangles = new List<int>(0);
}

[Serializable]
public struct Rectangle
{
    public int a, b, c, d;
}