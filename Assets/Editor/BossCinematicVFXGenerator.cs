#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Unity 6.2 / URP. No external assets. Generates editable VFX prefabs and PNG textures.
public static class BossCinematicVFXGenerator
{
    const string Root = "Assets/BossCinematicVFX";
    static Material flame, ember, smoke, halo, ring;
    static Texture2D flameTex, emberTex, smokeTex, haloTex, ringTex;

    [MenuItem("Tools/Boss VFX/Generate CINEMATIC Fire Pack")]
    public static void Generate()
    {
        Directory.CreateDirectory(Root + "/Textures");
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Prefabs");
        AssetDatabase.Refresh();
        flameTex = MakeTexture("Flame", 128, FlamePixel);
        emberTex = MakeTexture("Ember", 64, EmberPixel);
        smokeTex = MakeTexture("Smoke", 128, SmokePixel);
        haloTex = MakeTexture("Glow", 128, GlowPixel);
        ringTex = MakeTexture("RunicRing", 256, RingPixel);
        flame = MakeMaterial("M_Flame", flameTex, true);
        ember = MakeMaterial("M_Ember", emberTex, true);
        smoke = MakeMaterial("M_Smoke", smokeTex, false);
        halo = MakeMaterial("M_Glow", haloTex, true);
        ring = MakeMaterial("M_Ring", ringTex, true);
        Create("FireSpawn", BuildSpawn);
        Create("FireCombo", BuildCombo);
        Create("FireSlam", BuildSlam);
        Create("FireSummon", BuildSummon);
        Create("FireProjectile", BuildProjectile);
        Create("FireDash", BuildDash);
        Create("FireRage", BuildRage);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Boss Fire VFX", "7 cinematic multi-layer fire prefabs generated in Assets/BossCinematicVFX/Prefabs.\n\nAssign these ParticleSystem roots to the matching Boss.cs slots, then turn on HDR + Bloom.", "OK");
    }

    delegate Color Paint(float x, float y);
    static Texture2D MakeTexture(string name, int size, Paint paint)
    {
        string path = Root + "/Textures/T_" + name + ".png";
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            px[y * size + x] = paint((x + .5f) / size * 2f - 1f, (y + .5f) / size * 2f - 1f);
        texture.SetPixels(px); texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static float Saturate(float v) => Mathf.Clamp01(v);
    static float Noise(float x, float y) => (Mathf.Sin(x * 28.3f + Mathf.Cos(y * 17.7f)) * Mathf.Sin(y * 35.9f - x * 12.3f) + 1f) * .5f;
    static Color FlamePixel(float x, float y)
    {
        float height = (y + 1f) * .5f;
        float bending = .16f * Mathf.Sin(y * 5f) * height;
        float width = Mathf.Lerp(.53f, .045f, height);
        float contour = Saturate((width - Mathf.Abs(x - bending)) * 9f);
        float alpha = contour * Saturate((1f - height) * 3.5f) * Saturate(height * 9f) * (.78f + .22f * Noise(x,y));
        return new Color(1f, Mathf.Lerp(.12f, .85f, 1f - height), .02f, alpha);
    }
    static Color EmberPixel(float x, float y)
    {
        float r = Mathf.Sqrt(x*x + y*y);
        return new Color(1f,.6f,.12f,Mathf.Pow(Saturate(1f-r),2.5f));
    }
    static Color GlowPixel(float x, float y)
    {
        float r = Mathf.Sqrt(x*x+y*y);
        return new Color(1f,.23f,.025f,Mathf.Pow(Saturate(1f-r), 2.7f)*.9f);
    }
    static Color SmokePixel(float x, float y)
    {
        float r = Mathf.Sqrt(x*x+y*y);
        float n = .65f + .35f * Noise(x*.4f,y*.4f);
        return new Color(.24f,.17f,.15f,Mathf.Pow(Saturate(1f-r),1.8f)*n*.4f);
    }
    static Color RingPixel(float x, float y)
    {
        float r = Mathf.Sqrt(x*x+y*y);
        float ang = Mathf.Atan2(y,x);
        float main = Saturate(1f - Mathf.Abs(r-.78f)*110f);
        float second = Saturate(1f - Mathf.Abs(r-.58f)*110f) * .65f;
        float sigils = (Mathf.Pow(Mathf.Abs(Mathf.Sin(ang*16f)), 35f)) * Saturate(1f-Mathf.Abs(r-.68f)*16f) * .85f;
        float spokes = Mathf.Pow(Mathf.Abs(Mathf.Sin(ang*8f)),48f) * Saturate(1f-Mathf.Abs(r-.44f)*8f) * .6f;
        return new Color(1f,.22f,.025f,Saturate(main+second+sigils+spokes));
    }
    static Material MakeMaterial(string name, Texture tex, bool additive)
    {
        string path=Root + "/Materials/" + name + ".mat";
        var old = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (old != null) AssetDatabase.DeleteAsset(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if(shader == null) throw new Exception("URP Particles Unlit shader not found. Install/enable Universal Render Pipeline.");
        var m = new Material(shader);
        m.SetTexture("_BaseMap",tex);
        m.SetColor("_BaseColor",Color.white);
        // Standard URP Particle Unlit shader surface configuration.
        m.SetFloat("_Surface",1f);
        m.SetFloat("_Blend", additive ? 2f : 0f); // 2 = additive, 0 = alpha
        m.SetFloat("_ZWrite",0f);
        m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend",(float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        m.renderQueue = (int)RenderQueue.Transparent;
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        AssetDatabase.CreateAsset(m,path);
        return m;
    }
    static ParticleSystem Burst(string name, Transform parent, Material material, int number, float lifetime, float speed, float size, Color color, ParticleSystemShapeType shape, float radius, bool looping=false, float rate=0f)
    {
        var go=new GameObject(name);
        go.transform.SetParent(parent,false);
        var ps=go.AddComponent<ParticleSystem>();
        var main=ps.main;
        main.playOnAwake=false;
        main.loop=looping;
        main.duration=Mathf.Max(.2f,lifetime);
        main.startLifetime=new ParticleSystem.MinMaxCurve(lifetime*.65f,lifetime);
        main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.55f,speed);
        main.startSize=new ParticleSystem.MinMaxCurve(size*.6f,size);
        main.startRotation=new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);
        main.startColor=color;
        main.maxParticles=2000;
        main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.stopAction=ParticleSystemStopAction.None;
        var emission=ps.emission;
        emission.enabled=true;
        emission.rateOverTime=looping?rate:0f;
        if(!looping) emission.SetBursts(new[]{new ParticleSystem.Burst(0f,(short)number)});
        var sh=ps.shape;
        sh.enabled=true; sh.shapeType=shape;
        sh.radius=radius;
        var colorOver=ps.colorOverLifetime;
        colorOver.enabled=true;
        Gradient grad=new Gradient();
        grad.SetKeys(new[]{new GradientColorKey(color,0f),new GradientColorKey(color,.45f),new GradientColorKey(new Color(.5f,.08f,.01f),1f)},
            new[]{new GradientAlphaKey(0f,0f),new GradientAlphaKey(1f,.12f),new GradientAlphaKey(.85f,.65f),new GradientAlphaKey(0f,1f)});
        colorOver.color=new ParticleSystem.MinMaxGradient(grad);
        var sizeOver=ps.sizeOverLifetime;
        sizeOver.enabled=true;
        sizeOver.size=new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f,.5f),new Keyframe(.3f,1f),new Keyframe(1f,.08f)));
        var renderer=ps.GetComponent<ParticleSystemRenderer>();
        renderer.material=material;
        renderer.renderMode=ParticleSystemRenderMode.Billboard;
        renderer.sortMode=ParticleSystemSortMode.Distance;
        return ps;
    }
    static ParticleSystem Flames(Transform t, float radius, bool loop=false, float rate=0f, float height=2f)
    {
        var p=Burst("01 FIRE TONGUES",t,flame,loop?0:100,1.15f, height*.75f, height,Color.white,ParticleSystemShapeType.Circle,radius,loop,rate);
        var v=p.velocityOverLifetime; v.enabled=true; v.y=new ParticleSystem.MinMaxCurve(height*.6f,height*1.2f);
        var n=p.noise; n.enabled=true; n.strength=.55f;n.frequency=.45f;n.scrollSpeed=.4f;
        return p;
    }
    static void Sparks(Transform t, int count, float radius, bool loop=false, float rate=0f)
    {
        var p=Burst("02 HOT EMBERS",t,ember,loop?0:count,1f,5f,.19f,new Color(1f,.7f,.25f,1f),ParticleSystemShapeType.Sphere,radius,loop,rate);
        var trails=p.trails; trails.enabled=true; trails.ratio=.7f; trails.lifetime=.18f; trails.minVertexDistance=.06f; trails.worldSpace=true;
        p.GetComponent<ParticleSystemRenderer>().trailMaterial=ember;
        var grav=p.main;grav.gravityModifier=.3f;
    }
    static void Smoke(Transform t, int count, float radius, bool loop=false, float rate=0f)
    {
        var p=Burst("03 HEAT SMOKE",t,smoke,loop?0:count,2f,1.7f,2.6f,Color.white,ParticleSystemShapeType.Sphere,radius,loop,rate);
        var n=p.noise;n.enabled=true;n.strength=.65f;n.frequency=.25f;
    }
    static void Core(Transform t, int count, float radius, bool loop=false, float rate=0f)
    {
        Burst("04 WHITE HOT CORE",t,halo,loop?0:count,.55f,1.5f,2.2f,new Color(1f,.9f,.48f),ParticleSystemShapeType.Sphere,radius,loop,rate);
    }
    static void Glyphs(Transform t, float scale=1f)
    {
        var p=Burst("05 RUNIC FIRE CIRCLE",t,ring,1,1.4f,0f,scale,Color.white,ParticleSystemShapeType.Sphere,0f);
        p.transform.localRotation=Quaternion.Euler(90f,0f,0f);
        var r=p.GetComponent<ParticleSystemRenderer>();r.alignment=ParticleSystemRenderSpace.Local;
        var m=p.main;m.startRotation=0f;
    }
    static void Flash(Transform t)
    {
        var p=Burst("06 FLARE",t,halo,9,.35f,.2f,4f,Color.white,ParticleSystemShapeType.Sphere,.25f);
    }
    static void Light(Transform parent, Color color, float intensity, float range)
    {
        var go=new GameObject("FIRE LIGHT");go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.up*.5f;
        var l=go.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;
    }
    static void BuildSpawn(Transform t){Glyphs(t,7f);Flames(t,2.1f,false,0,3f);Sparks(t,180,2f);Smoke(t,45,1.4f);Core(t,25,1f);Flash(t);Light(t,new Color(1f,.22f,.04f),8f,10f);}
    static void BuildCombo(Transform t){Flames(t,1.2f,false,0,2.5f);Sparks(t,95,1f);Core(t,15,.5f);Flash(t);Light(t,new Color(1f,.32f,.05f),7f,7f);}
    static void BuildSlam(Transform t){Glyphs(t,9f);Flames(t,3f,false,0,3.5f);Sparks(t,230,2.4f);Smoke(t,80,2.5f);Core(t,45,1.5f);Flash(t);Light(t,new Color(1f,.26f,.04f),12f,13f);}
    static void BuildSummon(Transform t){Glyphs(t,5f);Flames(t,1.4f,false,0,2.7f);Sparks(t,130,1.3f);Smoke(t,35,1.2f);Core(t,12,.8f);Light(t,new Color(1f,.1f,.025f),6f,7f);}
    static void BuildProjectile(Transform t){Flames(t,.22f,true,60,1.25f);Sparks(t,0,.18f,true,95f);Core(t,0,.1f,true,38f);Light(t,new Color(1f,.3f,.08f),5f,5f);}
    static void BuildDash(Transform t){Flames(t,.6f,false,0,2f);Sparks(t,160,.7f);Smoke(t,25,.5f);Flash(t);}
    static void BuildRage(Transform t){Flames(t,1.2f,true,55,2.4f);Sparks(t,0,1.3f,true,50f);Smoke(t,0,1.1f,true,10f);Core(t,0,.6f,true,12f);Light(t,new Color(1f,.08f,.015f),5f,7f);}
    delegate void Builder(Transform parent);
    static void Create(string name,Builder build)
    {
        GameObject root=new GameObject(name);
        // Root ParticleSystem so it can be assigned directly into existing Boss.cs fields.
        var ps=root.AddComponent<ParticleSystem>();
        var main=ps.main;main.playOnAwake=false;main.loop=false;main.startLifetime=.01f;main.maxParticles=1;
        var em=ps.emission;em.enabled=false;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.enabled=false;
        build(root.transform);
        string path=Root+"/Prefabs/"+name+".prefab";
        PrefabUtility.SaveAsPrefabAsset(root,path);
        UnityEngine.Object.DestroyImmediate(root);
    }
}
#endif
