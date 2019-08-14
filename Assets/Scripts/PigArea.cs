using MLAgents;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PigArea : Area
{
    [Header("Pig Area Objects")]
    public GameObject pigAgent;
    public GameObject ground;
    public Material successMaterial;
    public Material failureMaterial;
    public TextMeshPro scoreText;

    [Header("Prefabs")]
    public GameObject trufflePrefab;
    public GameObject stumpPrefab;

    [HideInInspector]
    public int numTruffles;
    [HideInInspector]
    public int numStumps;
    [HideInInspector]
    public float spawnRange;
    
    private List<GameObject> spawnedTruffles;
    private List<GameObject> spawnedStumps;

    // A list of (position, radius) tuples of occupied spots in the area
    private List<Tuple<Vector3, float>> occupiedPositions;

    private Renderer groundRenderer;
    private Material groundMaterial;

    private void Start()
    {
        // Get the ground renderer so we can change the material when a goal is scored
        groundRenderer = ground.GetComponent<Renderer>();

        // Store the starting material
        groundMaterial = groundRenderer.material;
    }

    /// <summary>
    /// Resets the area
    /// </summary>
    /// <param name="agents"></param>
    public override void ResetArea()
    {
        occupiedPositions = new List<Tuple<Vector3, float>>();
        ResetAgent();
        ResetTruffles();
        ResetStumps();
    }

    private void FixedUpdate()
    {
        // Make sure the pig has not left the area
        Vector3 pigLocalPosition = pigAgent.transform.localPosition;
        if (Mathf.Abs(pigLocalPosition.x) > 13f || Mathf.Abs(pigLocalPosition.z) > 13f)
        {
            Debug.LogWarning("Pig out of the pen!");
            PigAgent pigAgentComponent = pigAgent.GetComponent<PigAgent>();
            pigAgentComponent.SetReward(-5f);
            pigAgentComponent.AgentReset();
            ResetArea();
        }
    }

    public List<GameObject> GetSmellyObjects()
    {
        return spawnedTruffles;
    }

    /// <summary>
    /// Swap ground material, wait time seconds, then swap back to the regular material.
    /// </summary>
    public IEnumerator SwapGroundMaterial(bool success)
    {
        if (success)
        {
            groundRenderer.material = successMaterial;
        }
        else
        {
            groundRenderer.material = failureMaterial;
        }

        yield return new WaitForSeconds(0.5f);
        groundRenderer.material = groundMaterial;
    }

    public void UpdateScore(float score)
    {
        scoreText.text = score.ToString("0.00");
    }

    /// <summary>
    /// Reset the agent
    /// </summary>
    private void ResetAgent()
    {
        // Reset location and rotation
        RandomlyPlaceObject(pigAgent, spawnRange, 10);
    }

    /// <summary>
    /// Resets all truffles in the area
    /// </summary>
    private void ResetTruffles()
    {
        if (spawnedTruffles != null)
        {
            // Destroy any truffles remaining from the previous run
            foreach (GameObject spawnedMushroom in spawnedTruffles.ToArray())
            {
                Destroy(spawnedMushroom);
            }
        }

        spawnedTruffles = new List<GameObject>();

        for (int i = 0; i < numTruffles; i++)
        {
            // Create a new truffle instance and place it randomly
            GameObject truffleInstance = Instantiate(trufflePrefab, transform);
            RandomlyPlaceObject(truffleInstance, spawnRange, 50);
            spawnedTruffles.Add(truffleInstance);
        }
    }

    /// <summary>
    /// Resets all stumps in the area
    /// </summary>
    private void ResetStumps()
    {
        if (spawnedStumps != null)
        {
            // Destroy any stumps remaining from the previous run
            foreach (GameObject spawnedTree in spawnedStumps.ToArray())
            {
                Destroy(spawnedTree);
            }
        }

        spawnedStumps = new List<GameObject>();

        for (int i = 0; i < numStumps; i++)
        {
            // Create a new stump instance and place it randomly
            GameObject stumpInstance = Instantiate(stumpPrefab, transform);
            RandomlyPlaceObject(stumpInstance, spawnRange, 50);
            spawnedStumps.Add(stumpInstance);
        }
    }

    /// <summary>
    /// Attempts to randomly place an object by checking a sphere around a potential location for collisions
    /// </summary>
    /// <param name="objectToPlace">The object to be randomly placed</param>
    /// <param name="range">The range in x and z to choose random points within.</param>
    /// <param name="maxAttempts">Number of times to attempt placement</param>
    private void RandomlyPlaceObject(GameObject objectToPlace, float range, float maxAttempts)
    {
        // Temporarily disable collision
        objectToPlace.GetComponent<Collider>().enabled = false;

        // Calculate test radius 10% larger than the collider extents
        float testRadius = GetColliderRadius(objectToPlace) * 1.1f;

        // Set a random rotation
        objectToPlace.transform.rotation = Quaternion.Euler(new Vector3(0f, UnityEngine.Random.Range(0f, 360f), 0f));

        // Make several attempts at randomly placing the object
        int attempt = 1;
        while (attempt <= maxAttempts)
        {
            Vector3 randomLocalPosition = new Vector3(UnityEngine.Random.Range(-range, range), 0, UnityEngine.Random.Range(-range, range));
            randomLocalPosition.Scale(transform.localScale);

            //if (!Physics.CheckSphere(transform.position + randomLocalPosition, testRadius, notGroundLayerMask))
            if (CheckIfPositionIsOpen(transform.position + randomLocalPosition, testRadius))
            {
                objectToPlace.transform.localPosition = randomLocalPosition;
                occupiedPositions.Add(new Tuple<Vector3, float>(objectToPlace.transform.position, testRadius));
                break;
            }
            else if (attempt == maxAttempts)
            {
                Debug.LogError(string.Format("{0} couldn't be placed randomly after {1} attempts.", objectToPlace.name, maxAttempts));
                break;
            }

            attempt++;
        }

        // Enable collision
        objectToPlace.GetComponent<Collider>().enabled = true;
    }

    /// <summary>
    /// Gets a local space radius that draws a circle on the X-Z plane around the boundary of the collider
    /// </summary>
    /// <param name="obj">The game object to test</param>
    /// <returns>The local space radius around the collider</returns>
    private static float GetColliderRadius(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();

        Vector3 boundsSize = Vector3.zero; 
        if (col.GetType() == typeof(MeshCollider))
        {
            boundsSize = ((MeshCollider)col).sharedMesh.bounds.size;
        }
        else if (col.GetType() == typeof(BoxCollider))
        {
            boundsSize = col.bounds.size;
        }

        boundsSize.Scale(obj.transform.localScale);
        return Mathf.Max(boundsSize.x, boundsSize.z) / 2f;
    }

    /// <summary>
    /// Detects if a test position has a radius of clear space around it
    /// </summary>
    /// <param name="testPosition">The world position to test</param>
    /// <param name="testRadius">The radius to test</param>
    /// <returns><c>true</c> if the position is open</returns>
    private bool CheckIfPositionIsOpen(Vector3 testPosition, float testRadius)
    {
        foreach (Tuple<Vector3, float> occupied in occupiedPositions)
        {
            Vector3 occupiedPosition = occupied.Item1;
            float occupiedRadius = occupied.Item2;
            if (Vector3.Distance(testPosition, occupiedPosition) - occupiedRadius <= testRadius)
            {
                return false;
            }
        }

        return true;
    }
}
