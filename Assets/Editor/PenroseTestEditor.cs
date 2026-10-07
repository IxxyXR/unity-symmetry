using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(PenroseTest))]
public sealed class PenroseTestEditor : Editor
{
    private PreviewRenderUtility preview;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var demo = (PenroseTest)target;
        EditorGUILayout.LabelField("Tiles", demo.TileCount.ToString());
        EditorGUILayout.LabelField("Motif transforms", demo.TransformCount.ToString());
        EditorGUILayout.LabelField("Clipped boundary tiles", demo.BoundaryTileCount.ToString());
        DrawPropertiesExcluding(serializedObject, "m_Script");
        if (serializedObject.ApplyModifiedProperties()) SceneView.RepaintAll();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        var rect = GUILayoutUtility.GetRect(200, 220, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint) RenderModelPreview(rect);
    }

    private void RenderModelPreview(Rect rect)
    {
        var demo = (PenroseTest)target;
        if (!demo.isActiveAndEnabled)
        {
            GUI.Label(rect, "Enable the component to preview the tiling.");
            return;
        }
        var filter = demo.GetComponent<MeshFilter>();
        var renderer = demo.GetComponent<MeshRenderer>();
        if (filter == null || renderer == null || filter.sharedMesh == null || renderer.sharedMaterial == null)
        {
            GUI.Label(rect, "Add a mesh and material to preview the copies.");
            return;
        }

        var mesh = filter.sharedMesh;
        var matrices = demo.GetDrawMatrices();
        var bounds = demo.OutlineMesh.bounds;
        foreach (var matrix in matrices)
        {
            for (var corner = 0; corner < 8; corner++)
            {
                var offset = Vector3.Scale(mesh.bounds.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                bounds.Encapsulate(matrix.MultiplyPoint3x4(mesh.bounds.center + offset));
            }
        }

        if (preview == null)
        {
            preview = new PreviewRenderUtility();
            preview.camera.orthographic = true;
            preview.camera.nearClipPlane = 0.01f;
            preview.ambientColor = new Color(0.35f, 0.35f, 0.35f);
            preview.lights[0].intensity = 1f;
            preview.lights[0].transform.rotation = Quaternion.Euler(50, 50, 0);
        }
        var radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
        preview.camera.orthographicSize = radius * 1.1f / Mathf.Min(1f, rect.width / rect.height);
        preview.camera.farClipPlane = radius * 10f + 10f;
        preview.camera.transform.position = bounds.center + new Vector3(1, 0.8f, -1).normalized * (radius * 3f + 1f);
        preview.camera.transform.LookAt(bounds.center);

        preview.BeginPreview(rect, GUIStyle.none);
        try
        {
            if (demo.showMotifs)
            foreach (var matrix in matrices)
            for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                // Preserve reflections: Unity 2022's preview DrawMesh decomposes matrices and loses negative scale.
                Graphics.DrawMesh(mesh, matrix, renderer.sharedMaterial, 0, preview.camera, submesh,
                    null, ShadowCastingMode.Off, false);
            }
            if (demo.showOutlines)
                Graphics.DrawMesh(demo.OutlineMesh, Matrix4x4.identity, demo.OutlineMaterial, 0, preview.camera,
                    0, null, ShadowCastingMode.Off, false);
            preview.Render();
        }
        finally
        {
            preview.EndAndDrawPreview(rect);
        }
    }

    private void OnDisable()
    {
        if (preview != null) preview.Cleanup();
        preview = null;
    }
}
