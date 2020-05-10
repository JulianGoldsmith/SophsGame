using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{



    [Header("Background Setup")]
    public List<GameObject> levelBoundsQuad;
    public List<Vector3> parallaxReferencePositions = new List<Vector3>();
    public List<BackgroundLayer> backgroundLayers = new List<BackgroundLayer>();
    public List<ParallaxCloud> parallaxClouds = new List<ParallaxCloud>();

    [Header("Platforms Setup")]
    public List<MovingPlatform> movingPlatfomrs = new List<MovingPlatform>();
    [HideInInspector]
    public WorldConstrints worldConstraints;

    Transform cam;
    Vector3 cameraPrevPos;

    [Header("Positional Triggers")]
    public List<PositionTrigger> positionTriggers = new List<PositionTrigger>();

    [HideInInspector]
    public int parallaxRefPos;

    [HideInInspector]
    public GameObject player;

    // Start is called before the first frame update
    void Start()
    {

        cam = Camera.main.transform;
        cameraPrevPos = cam.position;
        //parallaxReferencePos = cam.position;
        CalculateLevelConstaints();
        CalculateLayerParallexOffsets();
    }


    void FixedUpdate()
    {
        CheckPositionalTriggers();
        CalculateLevelConstaints();
        MovingPlatforms();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        BackgroundParallax();
    }

    void BackgroundParallax()
    {
        foreach (BackgroundLayer layer in backgroundLayers)
        {
            float parallexX = (parallaxReferencePositions[parallaxRefPos].x - cam.transform.position.x) * -layer.distance / 100f;
            float parallexY = (parallaxReferencePositions[parallaxRefPos].y - cam.transform.position.y) * -layer.distance / 100f;
            Vector3 targetPos = new Vector3(layer.layerStartingPos.x + parallexX, layer.layerStartingPos.y + parallexY, layer.layerObject.transform.position.z);
            layer.layerObject.transform.position = targetPos;
        }
        foreach (ParallaxCloud cloud in parallaxClouds)
        {
            float parallexX = (parallaxReferencePositions[parallaxRefPos].x - cam.transform.position.x) * -cloud.distance / 100f;
            float parallexY = (parallaxReferencePositions[parallaxRefPos].y - cam.transform.position.y) * -cloud.distance / 100f;

            cloud.offset += cloud.speed * Time.fixedDeltaTime;
            Vector3 targetPos = new Vector3(cloud.layerStartingPos.x + parallexX + cloud.offset, cloud.layerStartingPos.y + parallexY, cloud.layerObject.transform.position.z);
            cloud.layerObject.transform.position = targetPos;
            Camera camera = cam.GetComponent<Camera>();
            if (cloud.speed > 0 && cloud.layerObject.transform.position.x > worldConstraints.maxX + (camera.orthographicSize * camera.aspect) * 2)
            {
                cloud.offset = ((worldConstraints.minX - (camera.orthographicSize * camera.aspect) * 2) - cloud.layerStartingPos.x);
            }
            if (cloud.speed < 0 && cloud.layerObject.transform.position.x < worldConstraints.minX - (camera.orthographicSize * camera.aspect) * 2)
            {
                cloud.offset = ((worldConstraints.maxX + (camera.orthographicSize * camera.aspect) * 2) - cloud.layerStartingPos.x);
            }
        }
        cameraPrevPos = cam.position;
    }

    void CalculateLayerParallexOffsets()
    {
        for (int i = 0; i < backgroundLayers.Count; i++)
        {
            backgroundLayers[i].layerStartingPos = backgroundLayers[i].layerObject.transform.position;
        }
        for (int i = 0; i < parallaxClouds.Count; i++)
        {
            parallaxClouds[i].layerStartingPos = parallaxClouds[i].layerObject.transform.position;
        }
    }

    public void CalculateLevelConstaints()
    {
        Camera camera = cam.GetComponent<Camera>();
        worldConstraints.minX = levelBoundsQuad[parallaxRefPos].transform.position.x - (levelBoundsQuad[parallaxRefPos].transform.localScale.x / 2) + camera.orthographicSize * camera.aspect;
        worldConstraints.maxX = levelBoundsQuad[parallaxRefPos].transform.position.x + (levelBoundsQuad[parallaxRefPos].transform.localScale.x / 2) - camera.orthographicSize * camera.aspect;
        worldConstraints.minY = levelBoundsQuad[parallaxRefPos].transform.position.y - (levelBoundsQuad[parallaxRefPos].transform.localScale.y / 2) + camera.orthographicSize;
        worldConstraints.maxY = levelBoundsQuad[parallaxRefPos].transform.position.y + (levelBoundsQuad[parallaxRefPos].transform.localScale.y / 2) - camera.orthographicSize; ;
    }

    void MovingPlatforms()
    {
        for (int i = 0; i < movingPlatfomrs.Count; i++)
        {
            MovingPlatform mp = movingPlatfomrs[i];
            //mp.platformObject.transform.position = Vector3.SmoothDamp(mp.platformObject.transform.position, mp.wayPoints[mp.targetIndex].position, ref mp.smoothVelocity, mp.smoothTime);
            mp.platformObject.transform.position = mp.platformObject.transform.position + (mp.wayPoints[mp.targetIndex].position - mp.platformObject.transform.position).normalized * mp.speed * Time.fixedDeltaTime;
            if ((mp.platformObject.transform.position - mp.wayPoints[mp.targetIndex].position).magnitude < 0.1f)
            {
                if (mp.targetIndex != mp.wayPoints.Length - 1)
                {
                    mp.targetIndex++;
                }
                else
                {
                    mp.targetIndex = 0;
                }

            }
        }
    }

    void CheckPositionalTriggers()
    {
        Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.y);
        for (int i = 0; i < positionTriggers.Count; i++)
        {
            PositionTrigger pt = positionTriggers[i];
            bool reject = false;
            foreach (Argument arg in pt.conditions)
            {
                switch (arg.argument)
                {
                    case ArgumentType.XLessThan:
                        reject = !(playerPos.x < arg.value);
                        break;
                    case ArgumentType.XGreaterThan:
                        reject = !(playerPos.x > arg.value);
                        break;
                    case ArgumentType.XEqualToo:
                        reject = !(playerPos.x == arg.value);
                        break;
                    case ArgumentType.YLessThan:
                        reject = !(playerPos.y < arg.value);
                        break;
                    case ArgumentType.YGreaterThan:
                        reject = !(playerPos.y > arg.value);
                        break;
                    case ArgumentType.YEqualToo:
                        reject = !(playerPos.y == arg.value);
                        break;
                }
            }
            if (!reject)
            {
                parallaxRefPos = pt.targetParallaxRefPos;
            }
        }
    }
}

[System.Serializable]
public class BackgroundLayer
{
    public GameObject layerObject;
    [Range(0, 100)]
    public float distance;
    [HideInInspector]
    public Vector3 layerStartingPos;
}

[System.Serializable]
public class ParallaxCloud
{
    public GameObject layerObject;
    [Range(0, 100)]
    public float distance;
    [Range(-1, 1)]
    public float speed;
    [HideInInspector]
    public Vector3 layerStartingPos;
    [HideInInspector]
    public float offset;
}

public struct WorldConstrints
{
    public float minX, maxX, minY, maxY;
}

[System.Serializable]
public class MovingPlatform
{
    public GameObject platformObject;
    public Transform[] wayPoints;
    public float speed = 3;

    public int targetIndex;
}

[System.Serializable]
public class PositionTrigger
{
    public List<Argument> conditions;
    public int targetParallaxRefPos = 1;
}

[System.Serializable]
public class Argument
{
    public float value;
    public ArgumentType argument;
}

public enum ArgumentType
{
    XGreaterThan, XLessThan, XEqualToo, YGreaterThan, YLessThan, YEqualToo
}