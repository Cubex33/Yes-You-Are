using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Lucy.Core
{
  public class CrushableItem : Entity
  {
    [Header("Crushable Settings")]
    [Tooltip("Базовая сила взрыва, применяемая к фрагментам")] public float explosionForce = 5f;
    [Tooltip("Радиус применения взрыва")] public float explosionRadius = 2f;
    [Tooltip("Вертикальный модификатор взрыва (upwards)")] public float upwardModifier = 0.5f;
    [Tooltip("Добавлять Rigidbody к фрагментам, если их нет")] public bool addRigidbodies = true;
    [Tooltip("Добавлять Collider к фрагментам, если их нет")] public bool addColliders = true;
    [Tooltip("Через сколько секунд фрагменты будут уничтожены (если > 0)")] public float fragmentsLifeTime = 10f;
    [Tooltip("Максимальный случайный дополнительный импульс (0 — нет случайности)")] public float randomExtraForce = 2f;
    [Tooltip("Добавлять случайный крутящий момент фрагментам")] public bool randomTorque = true;
    [Tooltip("Масса добавляемых Rigidbody")] public float fragmentMass = 1f;
    [Header("Optional")]
    [Tooltip("Если true — все дочерние трансформы будут откреплены рекурсивно (включая вложенные)")] public bool detachRecursively = false;
    [Tooltip("Если true — скрывать рендереры родителя перед уничтожением (чтобы не было двойного отображения)")] public bool hideParentRenderers = true;
    protected override void HandleDeath(object source)
    {
      DetachAndScatter();
      if (hideParentRenderers) HideRenderersRecursively(transform);
      base.HandleDeath(source);
    }
    private void DetachAndScatter()
    {
      List<Transform> fragments = new();
      if (detachRecursively)
      {
        foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
        {
          if (child == transform) continue;
          if (child.GetComponent<Light>() != null) continue;
          if (child.GetComponent<ParticleSystem>() != null) continue;
          fragments.Add(child);
        }
      }
      else for (int i = transform.childCount - 1; i >= 0; i--) {
        if (transform.GetChild(i).GetComponent<Renderer>() == null) continue;
        fragments.Add(transform.GetChild(i));
      }
      Vector3 explosionOrigin = transform.position;
      foreach (Transform frag in fragments)
      {
        frag.SetParent(null, true);
        GameObject go = frag.gameObject;
        SetupFragmentPhysics(go);
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
          float forceToApply = explosionForce + ((randomExtraForce > 0f) ? Random.Range(0f, randomExtraForce) : 0f);
          rb.AddExplosionForce(forceToApply, explosionOrigin, explosionRadius, upwardModifier, ForceMode.Impulse);
          if (randomTorque)
          {
            Vector3 torque = Random.insideUnitSphere * (forceToApply * 0.2f);
            rb.AddTorque(torque, ForceMode.Impulse);
          }
        }
        if (fragmentsLifeTime > 0f) Destroy(go, fragmentsLifeTime + Random.Range(0f, 2f)); 
      }
    }
    private void SetupFragmentPhysics(GameObject go)
    {
      if (addRigidbodies)
      {
        if (go.GetComponent<Rigidbody>() == null)
        {
          Rigidbody rb = go.AddComponent<Rigidbody>();
          rb.mass = Mathf.Max(0.01f, fragmentMass);
          rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
          rb.interpolation = RigidbodyInterpolation.None;
        }
      }
      if (addColliders)
      {
        if (go.GetComponent<Collider>() == null)
        {
          Mesh mesh = null;
          MeshFilter mf = go.GetComponent<MeshFilter>();
          if (mf != null && mf.sharedMesh != null) mesh = mf.sharedMesh;
          else
          {
            SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null) mesh = smr.sharedMesh;
          }
          if (mesh != null)
          {
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.convex = true;
          }
          else go.AddComponent<BoxCollider>();
        }
      }
    }
    private void HideRenderersRecursively(Transform root)
    {
      Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
      foreach (Renderer r in rends) r.enabled = false;
    }
  }
}