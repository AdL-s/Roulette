using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CombineMesh : MonoBehaviour
{
    [ContextMenu("Optimize")]
    public void Optimize()
    {
        MeshFilter ownFilter = GetComponent<MeshFilter>();
        MeshRenderer ownRenderer = GetComponent<MeshRenderer>();
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        List<Material> materials = new List<Material>();
        List<List<CombineInstance>> groups = new List<List<CombineInstance>>();
        List<GameObject> toDisable = new List<GameObject>();

        Matrix4x4 toLocal = transform.worldToLocalMatrix;

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == ownFilter || mf.sharedMesh == null)
                continue;

            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null)
                continue;

            Mesh sourceMesh = mf.sharedMesh;
            Material[] sourceMats = mr.sharedMaterials;
            Matrix4x4 matrix = toLocal * mf.transform.localToWorldMatrix;

            int subCount = Mathf.Min(sourceMesh.subMeshCount, sourceMats.Length);
            for (int sub = 0; sub < subCount; sub++)
            {
                Material mat = sourceMats[sub];
                if (mat == null)
                    continue;

                int index = materials.IndexOf(mat);
                if (index < 0)
                {
                    index = materials.Count;
                    materials.Add(mat);
                    groups.Add(new List<CombineInstance>());
                }

                groups[index].Add(new CombineInstance
                {
                    mesh = sourceMesh,
                    subMeshIndex = sub,
                    transform = matrix
                });
            }

            toDisable.Add(mf.gameObject);
        }

        if (materials.Count == 0)
        {
            Debug.LogWarning($"CombineMesh on '{name}': nothing to combine.", this);
            return;
        }

        Mesh[] perMaterial = new Mesh[materials.Count];
        CombineInstance[] final = new CombineInstance[materials.Count];

        for (int m = 0; m < materials.Count; m++)
        {
            perMaterial[m] = new Mesh { indexFormat = IndexFormat.UInt32 };
            perMaterial[m].CombineMeshes(groups[m].ToArray(), true, true);

            final[m] = new CombineInstance
            {
                mesh = perMaterial[m],
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            };
        }

        Mesh mesh = new Mesh
        {
            name = gameObject.name + "_Combined",
            indexFormat = IndexFormat.UInt32
        };
        mesh.CombineMeshes(final, false, false);
        mesh.RecalculateBounds();

        foreach (Mesh temp in perMaterial)
        {
            if (Application.isPlaying) Destroy(temp);
            else DestroyImmediate(temp);
        }

        foreach (GameObject go in toDisable)
            go.SetActive(false);

        ownFilter.sharedMesh = mesh;
        ownRenderer.sharedMaterials = materials.ToArray();
        gameObject.SetActive(true);
    }
}