/*
 * FloatingIconManager.cs - wirted by ThunderWire Games
 * ver. 2.0
*/

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ThunderWire.Utility;

public class FloatingIconManager : Singleton<FloatingIconManager>
{
    public class IconObjectPair
    {
        public GameObject FollowObject;
        public FloatingIcon Icon;

        public IconObjectPair(GameObject obj, FloatingIcon icon)
        {
            FollowObject = obj;
            Icon = icon;
        }
    }

    private HFPS_GameManager gameManager;

    [Header("Floating Icon Objects")]
    public List<GameObject> FloatingIcons = new List<GameObject>();
    public List<IconObjectPair> FloatingIconCache = new List<IconObjectPair>();

    [Header("Raycasting")]
    public LayerMask Layer;

    [Header("UI")]
    public GameObject FloatingIconPrefab;
    public Transform FloatingIconUI;

    [Header("Properties")]
    public float followSmooth = 8;
    public float distanceShow = 3;
    public float distanceKeep = 4.5f;
    public float distanceRemove = 6;

    private GameObject Player;
    private GameObject Cam;

    void Awake()
    {
        gameManager = GetComponent<HFPS_GameManager>();
        Player = gameManager.Player;
        Cam = Tools.MainCamera().gameObject;
    }

    void Update()
    {
        if (FloatingIcons.Count > 0)
        {
            foreach (var obj in FloatingIcons)
            {
                if (Vector3.Distance(obj.transform.position, Player.transform.position) <= distanceShow)
                {
                    if (!ContainsFloatingIcon(obj) && IsObjectVisibleByCamera(obj))
                    {
                        AddFloatingIcon(obj);
                    }
                }
            }
        }

        if (FloatingIconCache.Count > 0)
        {
            for (int i = 0; i < FloatingIconCache.Count; i++)
            {
                IconObjectPair Pair = FloatingIconCache[i];

                if (Pair.FollowObject == null)
                {
                    Destroy(Pair.Icon.gameObject);
                    FloatingIconCache.RemoveAt(i);
                    return;
                }
                else if(Pair.FollowObject.GetComponent<Renderer>() && !Pair.FollowObject.GetComponent<Renderer>().enabled)
                {
                    Destroy(Pair.Icon.gameObject);
                    FloatingIconCache.RemoveAt(i);
                    return;
                }

                if (Vector3.Distance(Pair.FollowObject.transform.position, Player.transform.position) <= distanceKeep && IsObjectVisibleByCamera(Pair.FollowObject))
                {
                    Pair.Icon.OutOfDIstance(false);
                }
                else
                {
                    Pair.Icon.OutOfDIstance(true);
                }

                if (Vector3.Distance(Pair.FollowObject.transform.position, Player.transform.position) >= distanceRemove)
                {
                    Destroy(Pair.Icon.gameObject);
                    FloatingIconCache.RemoveAt(i);
                }

                if (Pair.FollowObject.GetComponent<InteractiveItem>())
                {
                    InteractiveItem interactiveItem = Pair.FollowObject.GetComponent<InteractiveItem>();

                    if (interactiveItem.floatingIconEnabled && !gameManager.isLocked)
                    {
                        Pair.Icon.SetIconVisible(true);
                    }
                    else
                    {
                        Pair.Icon.SetIconVisible(false);
                    }
                }
            }
        }
    }

    private void AddFloatingIcon(GameObject FollowObject)
    {
        Vector3 screenPos = gameManager.scriptManager.MainCamera.WorldToScreenPoint(FollowObject.transform.position);
        GameObject icon = Instantiate(FloatingIconPrefab, screenPos, Quaternion.identity, FloatingIconUI);
        icon.GetComponent<FloatingIcon>().FollowObject = FollowObject;
        icon.transform.position = new Vector2(-20, -20);
        FloatingIconCache.Add(new IconObjectPair(FollowObject, icon.GetComponent<FloatingIcon>()));
    }

    private bool IsObjectVisibleByCamera(GameObject FollowObject)
    {
        if (Physics.Linecast(Cam.transform.position, FollowObject.transform.position, out RaycastHit hit, Layer))
        {
            if (hit.collider.gameObject == FollowObject && FollowObject.GetComponent<Renderer>().isVisible)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Check if object is in distance
    /// </summary>
    public bool ContainsFloatingIcon(GameObject FollowObject)
    {
        return FloatingIconCache.Any(icon => icon.FollowObject == FollowObject);
    }

    /// <summary>
    /// Get IconObject Pair by object
    /// </summary>
    public IconObjectPair GetIcon(GameObject FollowObject)
    {
        return FloatingIconCache.SingleOrDefault(icon => icon.FollowObject == FollowObject);
    }

    /// <summary>
    /// Set visibility state of FollowObject
    /// </summary>
    public void SetIconVisible(GameObject FollowObject, bool state)
    {
        FloatingIconCache.SingleOrDefault(icon => icon.FollowObject == FollowObject).Icon.SetIconVisible(state);
    }

    /// <summary>
    /// Set visibility state of all FloatingIcons
    /// </summary>
    public void SetAllIconsVisible(bool state)
    {
        if (FloatingIconCache.Count > 0)
        {
            foreach (var item in FloatingIconCache)
            {
                item.Icon.SetIconVisible(state);
            }
        }
    }
}
