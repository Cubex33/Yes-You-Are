#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class InfernoSlamGenerator
{
    private const string Folder = "Assets/InfernoSlam/Generated";
    [MenuItem("Tools/Inferno Slam/Generate Ground Slam Prefab")]
    public static void Generate()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        Texture2D ember = CreateRadialTexture();
        AssetDatabase.CreateAsset(ember, Folder + "/SoftEmber.asset");
        Material ringMat = CreateMaterial("Fire_Ring", new Color(9f, 1.5f, .06f, .9f));
        Material domeMat = CreateMaterial("Shockwave_Dome", new Color(2.4f, .42f, .06f, .22f));
        Material glowMat = CreateMaterial("Ground_Glow", new Color(5f, .7f, .05f, .35f));
        Material sparksMat = CreateParticleMaterial(ember);

        Mesh ringMesh = MakeRing(0.76f, 1f, 96);
        Mesh domeMesh = MakeDome(48, 16);
        Mesh diskMesh = MakeRing(0f, 1f, 64);
        AssetDatabase.CreateAsset(ringMesh, Folder + "/FireRingMesh.asset");
        AssetDatabase.CreateAsset(domeMesh, Folder + "/ShockwaveDomeMesh.asset");
        AssetDatabase.CreateAsset(diskMesh, Folder + "/GroundDiscMesh.asset");

        GameObject root = new GameObject("INFERNO_GROUND_SLAM");
        InfernoGroundSlam slam = root.AddComponent<InfernoGroundSlam>();
        GameObject ringObject = MeshObject(
            root.transform,
            "Expanding_Fire_Ring",
            ringMesh,
            ringMat,
            0.06f
        );
        Debug.Log("Slam: " + (slam == null ? "NULL" : "OK"));
        Debug.Log("RingObject: " + (ringObject == null ? "NULL" : "OK"));
        if (ringObject != null)
        {
            slam.ring = ringObject.transform;
        }
        else
        {
            Debug.LogError("Не удалось создать огненное кольцо!");
        }
        slam.dome = MeshObject(root.transform, "Shockwave_Dome", domeMesh, domeMat, .055f).transform;
        slam.groundGlow = MeshObject(root.transform, "Molten_Ground_Glow", diskMesh, glowMat, .026f).transform;

        slam.sparks = MakeParticles(root.transform, "Flying_Sparks", sparksMat, 200, .35f, 1.0f, 6f, 8f, new Color(1f, .46f, .07f, 1f), 3f, 28f);
        slam.blast = MakeParticles(root.transform, "Explosion_Flames", sparksMat, 170, .7f, 1.5f, 2.4f, 7f, new Color(1f, .24f, .025f, .93f), 1.1f, 55f);
        slam.smoke = MakeParticles(root.transform, "Rising_Embers", sparksMat, 65, 1.4f, 2.2f, 1.2f, 3f, new Color(.65f, .11f, .025f, .3f), 1.2f, 8f);

        var lightObj = new GameObject("Impact_Flash_Light");
        lightObj.transform.SetParent(root.transform, false);
        lightObj.transform.localPosition = Vector3.up * 1.3f;
        slam.flashLight = lightObj.AddComponent<Light>();
        slam.flashLight.type = LightType.Point;
        slam.flashLight.color = new Color(1f, .36f, .08f);
        slam.flashLight.intensity = 14f;
        slam.flashLight.range = 11f;
        slam.flashLight.shadows = LightShadows.None;

        string path = Folder + "/InfernoGroundSlam.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Inferno Slam", "Prefab created:\n" + path + "\n\nAssign it to BossSlamCaster or your Boss.cs.", "OK");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) shader = Shader.Find("Unlit/Color");
        var mat = new Material(shader) { name = name };
        mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHABLEND_ON");
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        AssetDatabase.CreateAsset(mat, Folder + "/" + name + ".mat");
        return mat;
    }

    private static Material CreateParticleMaterial(Texture2D tex)
    {
        Material mat = CreateMaterial("Particles_Embers", new Color(2.7f, .85f, .13f, 1f));
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        return mat;
    }

    private static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material mat, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.up * height;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.transform.localScale = Vector3.one * .001f;
        return go;
    }

    private static ParticleSystem MakeParticles(Transform parent, string name, Material mat, int burst, float lifeMin, float lifeMax, float speedMin, float speedMax, Color color, float size, float cone)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.up * .1f;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(.025f * size, .12f * size);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 600;
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new [] { new ParticleSystem.Burst(0f, (short)burst) });
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = cone;
        shape.radius = .6f;
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, .25f, 1, 0));
        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new [] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1,.2f,.025f), 1) },
                  new [] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.8f, .18f), new GradientAlphaKey(0,1) });
        colorLife.color = g;
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = mat;
        return ps;
    }

    private static Texture2D CreateRadialTexture()
    {
        var t = new Texture2D(128,128,TextureFormat.RGBA32,false) { name = "SoftEmber", wrapMode = TextureWrapMode.Clamp };
        for(int y=0;y<128;y++) for(int x=0;x<128;x++)
        {
            float dx=(x-63.5f)/63.5f, dy=(y-63.5f)/63.5f;
            float r=Mathf.Sqrt(dx*dx+dy*dy);
            float a=Mathf.Pow(Mathf.Clamp01(1-r),1.75f);
            t.SetPixel(x,y,new Color(1f,.5f,.12f,a));
        }
        t.Apply(); return t;
    }

    private static Mesh MakeRing(float inner, float outer, int n)
    {
        var vertices = new Vector3[(n+1)*2];
        var uv = new Vector2[vertices.Length];
        var tri = new int[n*6];
        for(int i=0;i<=n;i++)
        {
            float a = i * Mathf.PI*2/n;
            Vector3 direction = new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            vertices[i*2] = direction*inner;
            vertices[i*2+1] = direction*outer;
            uv[i*2]=new Vector2(i/(float)n,0);
            uv[i*2+1]=new Vector2(i/(float)n,1);
            if (i<n)
            {
                int k=i*6,v=i*2;
                tri[k]=v;tri[k+1]=v+1;tri[k+2]=v+2;
                tri[k+3]=v+1;tri[k+4]=v+3;tri[k+5]=v+2;
            }
        }
        var m=new Mesh{name="GroundRing"};m.vertices=vertices;m.uv=uv;m.triangles=tri;m.RecalculateNormals();return m;
    }
    private static Mesh MakeDome(int segments, int rings)
    {
        var v = new Vector3[(rings+1)*(segments+1)];
        var uv = new Vector2[v.Length];
        var tr = new int[rings*segments*6];
        for(int j=0;j<=rings;j++) for(int i=0;i<=segments;i++)
        {
            float elev=(Mathf.PI*.5f)*j/rings;
            float a=2*Mathf.PI*i/segments;
            int k=j*(segments+1)+i;
            v[k]=new Vector3(Mathf.Cos(a)*Mathf.Cos(elev),Mathf.Sin(elev),Mathf.Sin(a)*Mathf.Cos(elev));
            uv[k]=new Vector2(i/(float)segments,j/(float)rings);
        }
        int z=0;
        for(int j=0;j<rings;j++) for(int i=0;i<segments;i++)
        {
            int a=j*(segments+1)+i,b=a+segments+1;
            tr[z++]=a;tr[z++]=b;tr[z++]=a+1;
            tr[z++]=a+1;tr[z++]=b;tr[z++]=b+1;
        }
        var m=new Mesh{name="WaveDome"};m.vertices=v;m.uv=uv;m.triangles=tr;m.RecalculateNormals();return m;
    }
}
#endif
