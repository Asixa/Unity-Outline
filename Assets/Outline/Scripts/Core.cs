using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace ADK
{
    public class _Outline : MonoBehaviour
    {
        
        public  bool On =true;
        public Color color = Color.green;
        Color oldColor;
        [Range(0f, 30f)]
        public float Line_width = 1;
        bool _on;
        private SubMeshCombiner[] m_subMeshCombiners = null;

        private const float OUTLINE_INV_WIDTH = 300f;
        private Material m_outlineMaterial = null;
        void Update()
        {
            if (On != _on) { _on = On; Set(On); }
            if (m_outlineMaterial != null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    if (cam.orthographic)
                    {
                        m_outlineMaterial.SetFloat("_Outline", 1.5f / OUTLINE_INV_WIDTH * Line_width);
                        m_outlineMaterial.SetFloat("_IsOrthogonal", 1f);
                    }
                    else
                    {
                        m_outlineMaterial.SetFloat("_Outline", (cam.transform.position - transform.position).magnitude / OUTLINE_INV_WIDTH * Line_width);
                        m_outlineMaterial.SetFloat("_IsOrthogonal", 0f);
                    }
                }
            }
            if (oldColor != color) { oldColor = color; ReSetColor(); }
        }
        [HideInInspector] public bool m_isSelectedChanged;
        private void LateUpdate()
        {
            if (m_isSelectedChanged)
            {
                m_isSelectedChanged = false;

                // instantiate outline material when needed
                if (m_outlineMaterial == null)
                {
                    m_outlineMaterial = new Material(Shader.Find("Hidden/OutlineShader"));
                    m_outlineMaterial.color = color;
                }
                // instantiate sub mesh combiners when needed
                if (m_subMeshCombiners == null)
                {
                    MeshFilter[] meshFiltersRaw = GetComponentsInChildren<MeshFilter>();
                    SkinnedMeshRenderer[] smrsRaw = GetComponentsInChildren<SkinnedMeshRenderer>();
                    if (meshFiltersRaw.Length == 0 && smrsRaw.Length == 0)
                    {
                        Debug.LogError("could not add selection outline, because neither a MeshFilter nor a SkinnedMeshRenderer was found!");
                        return;
                    }
                    // remove mesh filter of the edit handles and not readable meshes
                    List<MeshFilter> meshFilters = new List<MeshFilter>();
                    for (int i = 0; i < meshFiltersRaw.Length; i++)
                    {
                        if (meshFiltersRaw[i].GetComponentInParent<ObjectEditHandle>() == null) // don't select edit handles
                        {
                            if (meshFiltersRaw[i].sharedMesh.isReadable) // check if mesh is readable
                            {
                                meshFilters.Add(meshFiltersRaw[i]);
                            }
                            else
                            {
                                Debug.LogError(" Please enable the 'Read/Write Enabled' option in the mesh importer settings of '" + meshFiltersRaw[i].name + "'!");
                            }
                        }
                    }
                    // remove not readable meshes
                    List<SkinnedMeshRenderer> smrs = new List<SkinnedMeshRenderer>();
                    for (int i = 0; i < smrsRaw.Length; i++)
                    {
                        if (smrsRaw[i].sharedMesh.isReadable)
                        {
                            smrs.Add(smrsRaw[i]);
                        }
                        else
                        {
                            Debug.LogError("Please enable the 'Read/Write Enabled' option in the mesh importer settings of '" + smrsRaw[i].name + "'!");
                        }
                    }
                    // instantiate the filtered meshes to prevent project assets from being changed
                    for (int i = 0; i < meshFilters.Count; i++)
                    {
                        if (meshFilters[i].sharedMesh != null)
                        {
                            string oldName = meshFilters[i].sharedMesh.name;
                            meshFilters[i].sharedMesh = (Mesh)Instantiate(meshFilters[i].sharedMesh);
                            meshFilters[i].sharedMesh.name = oldName;
                        }
                        else
                        {
                            Debug.LogError(" missing mesh in object '" + name + "' in mesh filter of '" + meshFilters[i].name + "'");
                        }
                    }
                    // instantiate the skinned meshes to prevent project assets from being changed
                    for (int i = 0; i < smrs.Count; i++)
                    {
                        if (smrs[i].sharedMesh != null)
                        {
                            string oldName = smrs[i].sharedMesh.name;
                            smrs[i].sharedMesh = (Mesh)Instantiate(smrs[i].sharedMesh);
                            smrs[i].sharedMesh.name = oldName;
                        }
                        else
                        {
                            Debug.LogError(" missing mesh in object '" + name + "' in skinned mesh renderer of '" + smrs[i].name + "'");
                        }
                    }
                    m_subMeshCombiners = new SubMeshCombiner[meshFilters.Count + smrs.Count];
                    for (int i = 0; i < m_subMeshCombiners.Length; i++)
                    {
                        m_subMeshCombiners[i] = new SubMeshCombiner(m_outlineMaterial,
                                                                       i < meshFilters.Count ? meshFilters[i].sharedMesh : smrs[i - meshFilters.Count].sharedMesh,
                                                                       i < meshFilters.Count ? (Renderer)meshFilters[i].GetComponent<MeshRenderer>() : (Renderer)smrs[i - meshFilters.Count]);
                    }
                }
                ApplySelectionState();

            }
            if (!setenabled) { setenabled = true; enabled = false; }

        }
        [HideInInspector]
        public bool m_isSelected = false;
        [HideInInspector]
        public bool IsSelected
        {
            get { return m_isSelected; }
            set
            {
                if (m_isSelected != value)
                {
                    m_isSelected = value;
                    m_isSelectedChanged = true;
                }
            }
        }
        public void ReSetColor()
        {
           
            if (m_outlineMaterial != null) m_outlineMaterial.color = color;
        }
        public void ApplySelectionState()
        {
           
            if (m_subMeshCombiners != null)
            {
                for (int i = 0; i < m_subMeshCombiners.Length; i++)
                {
                    if (m_isSelected)
                    {
                        m_subMeshCombiners[i].ShowCombinedSubMesh();
                    }
                    else
                    {
                        m_subMeshCombiners[i].HideCombinedSubMesh();
                    }
                }
            }
        }
        [HideInInspector] public bool setenabled = true;
        public void Set(bool tf)
        {

            IsSelected = tf;
            m_isSelectedChanged = true;
            //   m_outlineMaterial.SetColor("Outline Color", color);
        }
        public void SetEnabled(bool tf) { setenabled = tf; }
    }

    public class ObjectEditHandle : MonoBehaviour
    {
#if (UNITY_ANDROID || UNITY_IPHONE || UNITY_WINRT) && !UNITY_EDITOR

#else
        private const int SKIP_FRAMES = 0;
#endif

        private const float MIN_SCALE_VALUE = 0.05f;


        private Vector3 m_activeEditAxis = Vector3.zero;
        private int m_lastDragFrame = -1;
        private Vector3 m_lastCursorPos = -1f * Vector3.one;
        private int m_dragSkipCounter = 0;

        private Camera m_cam = null;
        private Camera Cam
        {
            get
            {
                if (m_cam == null)
                {
                    m_cam = Camera.main;
                }
                return m_cam;
            }
        }

        public System.EventHandler m_onTransformed;

        public bool IsDrag { get { return m_lastDragFrame == Time.frameCount; } }

        public void OnMyMouseDrag(Vector3 p_axis)
        {
            m_activeEditAxis = p_axis;
            m_lastDragFrame = Time.frameCount;
        }

        public void DisableAxisX()
        {
            DisableAxis(Vector3.right);
        }

        public void DisableAxisY()
        {
            DisableAxis(Vector3.up);
        }

        public void DisableAxisZ()
        {
            DisableAxis(Vector3.forward);
        }

        private void DisableAxis(Vector3 p_axis)
        {
            ObjectEditHandleCollider[] editColliders = GetComponentsInChildren<ObjectEditHandleCollider>();
            for (int i = 0; i < editColliders.Length; i++)
            {
                if (editColliders[i].Axis == p_axis)
                {
                    Destroy(editColliders[i].gameObject);
                }
                else if (Vector3.Dot(editColliders[i].Axis, p_axis) > 0.0001f)
                {
                    Vector3 newAxis = editColliders[i].Axis;
                    newAxis.Scale(Vector3.one - p_axis);
                    editColliders[i].Axis = newAxis;
                }
            }
        }

        private void LateUpdate()
        {
            if (m_lastDragFrame < Time.frameCount)
            {
                m_activeEditAxis = Vector3.zero;
                m_dragSkipCounter = 0;
            }

            m_lastCursorPos = GetCursorPosition();
        }

        private void OnDestroy()
        {
            m_onTransformed = null;
        }

        private void Move()
        {
            float editDelta = GetEditDelta();
            if (m_dragSkipCounter == SKIP_FRAMES)
            {
                Vector3 worldAxis = transform.TransformDirection(m_activeEditAxis);
                transform.parent.position += worldAxis * editDelta;
                transform.parent.SendMessage("SolveCollisionAndDeactivateRigidbody");

                if (m_onTransformed != null) { m_onTransformed(this, System.EventArgs.Empty); }
            }
            else if (Mathf.Abs(editDelta) > 0.0005f)
            {
                // skip first frame (on mobile jumps possible)
                m_dragSkipCounter++;
            }
        }

        private void Scale()
        {
            float editDelta = GetEditDelta();
            if (m_dragSkipCounter == SKIP_FRAMES)
            {
                transform.parent.localScale += m_activeEditAxis * editDelta;
                transform.parent.localScale = Vector3.Max(Vector3.one * MIN_SCALE_VALUE, transform.parent.localScale);
                transform.parent.SendMessage("SolveCollisionAndDeactivateRigidbody");

                if (m_onTransformed != null) { m_onTransformed(this, System.EventArgs.Empty); }
            }
            else if (Mathf.Abs(editDelta) > 0.0005f)
            {
                // skip first frame (on mobile jumps possible)
                m_dragSkipCounter++;
            }
        }

        private float GetEditDelta()
        {
            Camera cam = Cam;
            if (cam != null)
            {
                Vector3 editAxisInScreenCoords = cam.WorldToScreenPoint(transform.position + transform.TransformDirection(m_activeEditAxis)) - cam.WorldToScreenPoint(transform.position);
              
                editAxisInScreenCoords.z *= editAxisInScreenCoords.z; 
                editAxisInScreenCoords.z *= editAxisInScreenCoords.z; 
                float zFactor = 1f / (1 - Mathf.Clamp(editAxisInScreenCoords.z, 0f, 0.8f));
                editAxisInScreenCoords.z = 0;
                editAxisInScreenCoords.Normalize();
                if (cam.orthographic)
                {
                    return GetEditDeltaOrthogonal(editAxisInScreenCoords, zFactor, cam);
                }
                else
                {
                    float distToCam = (cam.transform.position - transform.position).magnitude;
                    return GetEditDeltaPerspective(editAxisInScreenCoords, zFactor * distToCam, cam);
                }
            }
            else
            {
                return 0f;
            }
        }

        private void Rotate()
        {
            float editDelta = GetEditDeltaRotation();
            if (m_dragSkipCounter == SKIP_FRAMES)
            {
                transform.parent.Rotate(m_activeEditAxis * editDelta);
                transform.parent.SendMessage("SolveCollisionAndDeactivateRigidbody");

                if (m_onTransformed != null) { m_onTransformed(this, System.EventArgs.Empty); }
            }
            else if (Mathf.Abs(editDelta) > 0.0005f)
            {
               
                m_dragSkipCounter++;
            }
        }

        private float GetEditDeltaRotation()
        {
            Camera cam = Cam;
            if (cam != null)
            {
                Vector3 worldDir = transform.TransformDirection(m_activeEditAxis);
                Vector3 pivotScreenPos = cam.WorldToScreenPoint(transform.position);
                pivotScreenPos.z = 0;
                Vector3 screenDirection;
                float axisCameraAngle = Vector3.Dot(worldDir, cam.transform.forward);
                if (Mathf.Abs(axisCameraAngle) > 0.5f)
                {
                    float sign = Mathf.Sign(Vector3.Dot(cam.transform.forward, worldDir));
                    if (sign == 0) { sign = 1; }
                    Vector3 mouseDelta = (GetCursorPosition() - pivotScreenPos).normalized;
                    screenDirection = new Vector3(-Mathf.Sign(mouseDelta.y), Mathf.Sign(mouseDelta.x), 0f);
                    screenDirection *= sign;
                }
                else
                {
                    screenDirection = Vector3.Cross(cam.transform.forward, worldDir);
                    screenDirection = cam.transform.InverseTransformDirection(screenDirection);
                    screenDirection.z = 0;
                }
                screenDirection.Normalize();

                if (cam.orthographic)
                {
                    return GetEditDeltaOrthogonal(screenDirection, 360f / cam.orthographicSize, cam);
                }
                else
                {
                    return GetEditDeltaPerspective(screenDirection, 360f, cam);
                }
            }
            else
            {
                return 0f;
            }
        }

        private float GetEditDeltaPerspective(Vector3 p_screenAxis, float p_multiplier, Camera p_cam)
        {
            float mouseDeltaInAxisDir = Vector3.Dot(p_screenAxis, GetCursorPosition() - m_lastCursorPos);
            Ray rayAfter = p_cam.ScreenPointToRay(m_lastCursorPos + mouseDeltaInAxisDir * p_screenAxis);
            Ray rayBefore = p_cam.ScreenPointToRay(m_lastCursorPos);
            Vector3 rayDirAfter = rayAfter.direction;
            Vector3 rayDirBefore = rayBefore.direction;
            return (rayDirAfter - rayDirBefore).magnitude * Mathf.Sign(mouseDeltaInAxisDir) * p_multiplier;
        }

        private float GetEditDeltaOrthogonal(Vector3 p_screenAxis, float p_multiplier, Camera p_cam)
        {
            float mouseDeltaInAxisDir = Vector3.Dot(p_screenAxis, GetCursorPosition() - m_lastCursorPos);
            Vector3 pointAfter = p_cam.ScreenToWorldPoint(m_lastCursorPos + mouseDeltaInAxisDir * p_screenAxis);
            Vector3 pointBefore = p_cam.ScreenToWorldPoint(m_lastCursorPos);
            return (pointAfter - pointBefore).magnitude * Mathf.Sign(mouseDeltaInAxisDir) * p_multiplier;
        }

        private Vector3 GetCursorPosition()
        {
            if (Input.touchCount > 0)
            {
                return Input.GetTouch(0).position;
            }
            else
            {
                return Input.mousePosition;
            }
        }
    }
     

    public class ObjectEditHandleCollider : MonoBehaviour
    {
        [SerializeField]
        private ObjectEditHandle m_parent;
        [SerializeField]
        private Vector3 m_axis;
        public Vector3 Axis
        {
            get { return m_axis; }
            set { m_axis = value; }
        }
        [SerializeField]
        private bool m_isRotationHandle = false;
        [SerializeField]
        private Vector3 m_rotationHandleDir;

        private Renderer m_renderer;
        private Collider[] m_colliders;
        private Color m_originalColor;
        private bool m_isDrag = false;
        private float m_lastCamDist = -1;
        private Vector3 m_lastParentScale = Vector3.one;
        private Vector3 m_lastParentParentScale = Vector3.one;

        private bool m_isMouseDown = false;
        private bool m_isMouseDownOnMe = false;

        private Camera m_cam = null;
        private Camera Cam
        {
            get
            {
                if (m_cam == null)
                {
                    m_cam = Camera.main;
                }
                return m_cam;
            }
        }

        private void Start()
        {
            m_colliders = GetComponentsInChildren<Collider>();
            m_renderer = GetComponent<Renderer>();
            m_originalColor = m_renderer.material.color;
        }

        private void Update()
        {
            bool isMouseDownThisFrame = (Input.GetMouseButton(0) && Input.touchCount < 2) || Input.touchCount == 1;
            if (!m_isMouseDown && !isMouseDownThisFrame)
            {
                return;
            }
            if (m_isMouseDownOnMe && !isMouseDownThisFrame)
            {
                OnMyMouseUp();
            }
            else if (m_isMouseDownOnMe && isMouseDownThisFrame)
            {
                OnMyMouseDrag();
            }
            else
            {

                Camera cam = Cam;
                if (m_colliders != null && cam != null)
                {
                    RaycastHit hit;
                    for (int i = 0; i < m_colliders.Length; i++)
                    {
                        Collider collider = m_colliders[i];
                        if (collider != null &&
                            !m_isMouseDown &&
                            isMouseDownThisFrame &&
                            !m_parent.IsDrag &&
                            (collider.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity) ||
                            (Input.touchCount == 1 && collider.Raycast(cam.ScreenPointToRay(Input.GetTouch(0).position), out hit, Mathf.Infinity)))) // touch
                        {
                            m_isMouseDownOnMe = true;
                            OnMyMouseDown();
                            break;
                        }
                    }
                }
            }
            m_isMouseDown = isMouseDownThisFrame;
            if (!m_isMouseDown)
            {
                m_isMouseDownOnMe = false;
            }
        }

        private void LateUpdate()
        {
            Camera cam = Cam;
            if (cam != null)
            {
                if (!m_isDrag && m_isRotationHandle)
                {
                    Vector3 dirToCam = transform.parent.InverseTransformPoint(cam.transform.position);
                    Vector3 relCamDir = dirToCam - Vector3.Dot(m_axis, dirToCam) * m_axis;
                    relCamDir.Normalize();
                    float sin = Vector3.Dot(m_rotationHandleDir, relCamDir);
                    Vector3 angleAxis = Vector3.Cross(m_rotationHandleDir, relCamDir);
                    angleAxis.Scale(m_axis);
                    float angle = Mathf.Rad2Deg * Mathf.Asin(angleAxis.magnitude);
                    angleAxis.Normalize();
                    if (sin < 0)
                    {
                        angle = Mathf.Abs(-180 + angle);
                    }
                    angleAxis *= angle;
                    transform.localRotation = Quaternion.Euler(angleAxis);
                }


                float camDist = (cam.transform.position - transform.position).magnitude;
                if (!m_isDrag &&
                    (cam.orthographic ||
                    Mathf.Abs(camDist - m_lastCamDist) > 0.0001 ||
                    m_lastParentScale != m_parent.transform.localScale ||
                     m_lastParentParentScale != m_parent.transform.parent.localScale))
                {
                    float camScaleFactor;
                    if (cam.orthographic)
                    {
                        camScaleFactor = cam.orthographicSize / 50f;
                    }
                    else
                    {
                        camScaleFactor = camDist / 75f;
                    }
                    transform.localScale = new Vector3(
                        camScaleFactor / (m_parent.transform.parent.localScale.x * m_parent.transform.localScale.x),
                        camScaleFactor / (m_parent.transform.parent.localScale.y * m_parent.transform.localScale.y),
                        camScaleFactor / (m_parent.transform.parent.localScale.z * m_parent.transform.localScale.z));
                    m_lastCamDist = camDist;
                    m_lastParentScale = m_parent.transform.localScale;
                    m_lastParentParentScale = m_parent.transform.parent.localScale;
                }
            }
        }

        private void OnMyMouseDown()
        {
            m_parent.OnMyMouseDrag(m_axis);
            m_renderer.material.color = Color.yellow;
            m_isDrag = true;
        }

        private void OnMyMouseDrag()
        {
            m_parent.OnMyMouseDrag(m_axis);
            m_isDrag = true;
        }

        private void OnMyMouseUp()
        {
            m_renderer.material.color = m_originalColor;
            m_parent.transform.localScale = new Vector3(1f / m_parent.transform.parent.localScale.x, 1f / m_parent.transform.parent.localScale.y, 1f / m_parent.transform.parent.localScale.z);
            m_isDrag = false;
        }
    }


    public class SubMeshCombiner
    {
        private const string MESH_NAME_POSTFIX = "_combSubMesh";

        private readonly Material m_material;
        private readonly Mesh m_mesh;
        private readonly Renderer m_renderer;

        private bool m_isCombinedSubMeshVisible = false;
        private bool m_isCombinedSubMeshGenerated = false;

        public SubMeshCombiner(Material p_material, Mesh p_mesh, Renderer p_renderer)
        {
            m_material = p_material;
            m_mesh = p_mesh;
            m_renderer = p_renderer;
            if (m_mesh != null)
            {
                
                m_isCombinedSubMeshGenerated =
                    m_mesh.subMeshCount <= 1 || 
                    m_mesh.name.EndsWith(MESH_NAME_POSTFIX); 
                                                             
                if (m_isCombinedSubMeshGenerated &&
                    m_material.name == m_renderer.sharedMaterials[m_renderer.sharedMaterials.Length - 1].name &&
                    m_material.shader.name == m_renderer.sharedMaterials[m_renderer.sharedMaterials.Length - 1].shader.name)
                {
                    m_isCombinedSubMeshVisible = true;
                    HideCombinedSubMesh();
                }
            }
            else
            {
                Debug.LogError("mesh is null!");
            }
        }

        public void GenerateCombinedSubMesh()
        {
            if (m_mesh != null && !m_isCombinedSubMeshGenerated)
            {
                m_isCombinedSubMeshGenerated = true;
                m_mesh.name += MESH_NAME_POSTFIX;
                List<int> triangles = new List<int>();
                
                for (int i = 0; i < m_mesh.subMeshCount; i++)
                {
                    triangles.AddRange(m_mesh.GetTriangles(i));
                }
                m_mesh.subMeshCount = m_mesh.subMeshCount + 1;
                m_mesh.SetTriangles(triangles.ToArray(), m_mesh.subMeshCount - 1);
            }
        }

        public void ShowCombinedSubMesh()
        {
            if (m_mesh != null && !m_isCombinedSubMeshVisible)
            {
                if (m_renderer != null)
                {
                    GenerateCombinedSubMesh();
                    Material[] materials = new Material[m_renderer.sharedMaterials.Length + 1];
                    System.Array.Copy(m_renderer.sharedMaterials, materials, m_renderer.sharedMaterials.Length);
                    materials[materials.Length - 1] = m_material;
                    m_renderer.sharedMaterials = materials;
                    m_isCombinedSubMeshVisible = true;
                    m_mesh.RecalculateBounds();
                }
                else
                {
                    Debug.LogError("lost reference to renderer!");
                }
            }
        }

        public void HideCombinedSubMesh()
        {
            if (m_mesh != null && m_isCombinedSubMeshVisible)
            {
                if (m_renderer != null)
                {
                    m_isCombinedSubMeshVisible = false;
                    Material[] materials = new Material[m_renderer.sharedMaterials.Length - 1];
                    System.Array.Copy(m_renderer.sharedMaterials, materials, materials.Length);
                    m_renderer.sharedMaterials = materials;
                    m_mesh.RecalculateBounds();
                }
                else
                {
                    Debug.LogError("lost reference to renderer!");
                }
            }
        }
    }


    
}


