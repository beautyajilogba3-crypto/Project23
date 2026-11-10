using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

// Draws a translucent circular proximity-detection area on the floor around a guard.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProximityRadiusVisual : MonoBehaviour
{
    private Transform guardRoot;
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;

    private int segments = 48;
    private float radius = 1.5f;
    private float opacity = 0.3f;
    private float floorOffset = 0.06f;
    private Color visualColor = Color.red;

    public void Initialize(Material material, int circleSegments, float heightAboveFloor)
    {
        guardRoot = transform.parent;
        segments = Mathf.Max(12, circleSegments);
        floorOffset = heightAboveFloor;

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        mesh = new Mesh { name = "GuardProximityRadius" };
        mesh.MarkDynamic();

        // One centre vertex plus a ring of vertices.
        // The final ring vertex duplicates the first so the circle closes cleanly.
        vertices = new Vector3[segments + 2];
        colors = new Color[segments + 2];

        int[] triangles = new int[segments * 3];

        for (int i = 0; i < segments; i++)
        {
            int current = i + 1;
            int next = i + 2;

            // Winding faces upward.
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = next;
            triangles[i * 3 + 2] = current;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        meshFilter.sharedMesh = mesh;
    }

    public void Configure(float newRadius, Color newColor, float newOpacity)
    {
        radius = Mathf.Max(0.1f, newRadius);
        visualColor = newColor;
        opacity = Mathf.Clamp01(newOpacity);
    }

    private void LateUpdate()
    {
        if (mesh == null || guardRoot == null)
            return;

        Vector3 guardPosition = guardRoot.position;
        float groundY = guardPosition.y;

        // Prefer the NavMesh height so the circle follows sloped floors.
        if (NavMesh.SamplePosition(
            guardPosition,
            out NavMeshHit navHit,
            3f,
            NavMesh.AllAreas))
        {
            groundY = navHit.position.y;
        }
        else if (Physics.Raycast(
            guardPosition + Vector3.up * 0.5f,
            Vector3.down,
            out RaycastHit groundHit,
            5f,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            groundY = groundHit.point.y;
        }

        Vector3 center = new Vector3(
            guardPosition.x,
            groundY + floorOffset,
            guardPosition.z
        );

        vertices[0] = transform.InverseTransformPoint(center);

        Color centerColor = visualColor;
        centerColor.a = opacity;
        colors[0] = centerColor;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;

            Vector3 worldPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            vertices[i + 1] = transform.InverseTransformPoint(worldPoint);

            // Fade the disc smoothly to transparent at its outer edge.
            Color edgeColor = visualColor;
            edgeColor.a = Mathf.Lerp(opacity, 0f, i / (float)segments);
            colors[i + 1] = edgeColor;
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }
}