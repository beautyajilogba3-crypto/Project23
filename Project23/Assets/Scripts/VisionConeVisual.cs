using UnityEngine;
using UnityEngine.AI;

// Draws a translucent 3D "vision cone" volume in front of the guard.
public class VisionConeVisual : MonoBehaviour
{
    private int rayCount = 40;
    private float floorHeight = 0.05f;
    [HideInInspector] public float coneHeight = 1.5f;
    private float wallCheckHeight = 1f;

    private float range = 8f;
    private float angle = 90f;
    private Color color = Color.red;
    private float startOpacity = 0.35f;
    private float endOpacity = 0.03f;
    private LayerMask obstacleMask = ~0;
    private Transform playerRoot;

    private Transform guardRoot;
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors;
    private readonly RaycastHit[] hitBuffer = new RaycastHit[8];

    public void Initialize(Material material, int rays, float heightAboveFloor, float height, float wallRayHeight)
    {
        guardRoot = transform.parent;
        rayCount = Mathf.Max(4, rays);
        floorHeight = heightAboveFloor;
        coneHeight = Mathf.Max(0.1f, height);
        wallCheckHeight = wallRayHeight;

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        mesh = new Mesh { name = "VisionCone3D" };
        mesh.MarkDynamic();

        int totalVertices = (rayCount + 2) * 2;
        vertices = new Vector3[totalVertices];
        colors = new Color[totalVertices];

        int totalTriangles = (rayCount * 3) * 2 + (rayCount * 6);
        int[] triangles = new int[totalTriangles];
        int triIndex = 0;

        int bCenter = 0;
        int tCenter = 1;

        // Bottom fan triangles
        for (int i = 0; i < rayCount; i++)
        {
            triangles[triIndex++] = bCenter;
            triangles[triIndex++] = 2 + i * 2;
            triangles[triIndex++] = 2 + (i + 1) * 2;
        }

        // Top fan triangles (reversed winding)
        for (int i = 0; i < rayCount; i++)
        {
            triangles[triIndex++] = tCenter;
            triangles[triIndex++] = 3 + (i + 1) * 2;
            triangles[triIndex++] = 3 + i * 2;
        }

        // Side walls connecting bottom and top outer edges
        for (int i = 0; i < rayCount; i++)
        {
            int b0 = 2 + i * 2;
            int b1 = 2 + (i + 1) * 2;
            int t0 = 3 + i * 2;
            int t1 = 3 + (i + 1) * 2;

            triangles[triIndex++] = b0;
            triangles[triIndex++] = t0;
            triangles[triIndex++] = b1;

            triangles[triIndex++] = b1;
            triangles[triIndex++] = t0;
            triangles[triIndex++] = t1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        meshFilter.sharedMesh = mesh;
    }

    public void Configure(float newRange, float newAngle, Color newColor, float newStartOpacity, float newHeight, LayerMask mask, Transform player)
    {
        range = newRange;
        angle = newAngle;
        color = newColor;
        startOpacity = newStartOpacity;
        coneHeight = Mathf.Max(0.1f, newHeight);
        obstacleMask = mask;
        playerRoot = player;
    }

    void LateUpdate()
    {
        if (mesh == null || guardRoot == null)
            return;

        Vector3 pos = guardRoot.position;
        float groundY = GetGroundY(pos);

        Vector3 flatForward = guardRoot.forward;
        flatForward.y = 0f;
        flatForward = flatForward.sqrMagnitude > 0.0001f ? flatForward.normalized : Vector3.forward;

        Vector3 rayOrigin = new Vector3(pos.x, groundY + wallCheckHeight, pos.z);
        float half = angle * 0.5f;

        float bottomY = groundY + floorHeight;
        float topY = bottomY + coneHeight;

        vertices[0] = transform.InverseTransformPoint(new Vector3(pos.x, bottomY, pos.z));
        vertices[1] = transform.InverseTransformPoint(new Vector3(pos.x, topY, pos.z));

        Color startColorBase = color;
        startColorBase.a = startOpacity;
        colors[0] = startColorBase;
        colors[1] = startColorBase;

        for (int i = 0; i <= rayCount; i++)
        {
            float rayAngle = Mathf.Lerp(-half, half, i / (float)rayCount);
            Vector3 direction = Quaternion.AngleAxis(rayAngle, Vector3.up) * flatForward;

            float distance = CastDistance(rayOrigin, direction);

            Vector3 worldBottomPoint = new Vector3(pos.x + direction.x * distance, bottomY, pos.z + direction.z * distance);
            Vector3 worldTopPoint = new Vector3(pos.x + direction.x * distance, topY, pos.z + direction.z * distance);

            int bIndex = 2 + i * 2;
            int tIndex = 3 + i * 2;

            vertices[bIndex] = transform.InverseTransformPoint(worldBottomPoint);
            vertices[tIndex] = transform.InverseTransformPoint(worldTopPoint);

            float alphaFade = Mathf.Lerp(startOpacity, endOpacity, range > 0f ? distance / range : 1f);
            Color vertexColor = color;
            vertexColor.a = alphaFade;

            colors[bIndex] = vertexColor;
            colors[tIndex] = vertexColor;
        }

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.RecalculateBounds();
    }

    private float CastDistance(Vector3 origin, Vector3 direction)
    {
        int count = Physics.RaycastNonAlloc(origin, direction, hitBuffer, range, obstacleMask, QueryTriggerInteraction.Ignore);
        float nearest = range;

        for (int i = 0; i < count; i++)
        {
            if (IsIgnored(hitBuffer[i].collider.transform))
                continue;

            if (hitBuffer[i].distance < nearest)
                nearest = hitBuffer[i].distance;
        }

        return nearest;
    }

    private bool IsIgnored(Transform t)
    {
        if (guardRoot != null && t.IsChildOf(guardRoot))
            return true;
        if (playerRoot != null && t.IsChildOf(playerRoot))
            return true;
        return t.GetComponentInParent<GuardDetection>() != null;
    }

    private float GetGroundY(Vector3 pos)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
            return navHit.position.y;

        if (Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 5f, obstacleMask, QueryTriggerInteraction.Ignore)
            && !IsIgnored(groundHit.collider.transform))
            return groundHit.point.y;

        return pos.y;
    }

    void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }
}