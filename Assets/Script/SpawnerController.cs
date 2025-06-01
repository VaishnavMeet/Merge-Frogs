using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;


public class SpawnerController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public GameObject[] allFrogPrefabs;
    public Transform spawnPoint;
    public Transform boxPoint;
    public float dragLimitX = 2.5f;
    public float dropGravity = 5f;

    private GameObject currentFrog;
    private bool isDragging = false;
    private bool hasDropped = false;

    private List<int> spawnHistory = new List<int>();
    public int maxHistory = 10;

    private int mergeCount = 0;
    private int spawnCount = 0;

    private bool spawnMidLevelsOnce = false;
    private bool hasSpawned5 = false;
    private bool hasSpawned6 = false;

    public int level5Index = 4; // Index of 5th frog
    public int level6Index = 5; // Index of 6th frog
     AudioSource audioSource;
    public AudioClip popupsomething;
    public AudioClip uipopup;

    int lastMergedLevel = -1; // New variable in your script

    

    public void SetLastMergedLevel(int level)
    {
        lastMergedLevel = level;
    }
    public void OnFrogMerged()
    {
        mergeCount++;
    }


    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        SpawnRandomFrogFromFirstFive();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!hasDropped)
        {
            isDragging = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (hasDropped) return;

        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out pos
        );

        float clampedX = Mathf.Clamp(pos.x, -dragLimitX, dragLimitX);
        transform.localPosition = new Vector3(clampedX, transform.localPosition.y, transform.localPosition.z);

        if (currentFrog != null)
        {
            currentFrog.transform.position = spawnPoint.position;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (hasDropped || currentFrog == null) return;

        Rigidbody2D rb = currentFrog.GetComponent<Rigidbody2D>();
        rb.gravityScale = dropGravity;

        currentFrog.transform.SetParent(boxPoint.transform);

        isDragging = false;
        hasDropped = true;
       
        StartCoroutine(SpawnNextFrogAfterDelay(1f)); 
    }

    IEnumerator SpawnNextFrogAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        hasDropped = false;
        SpawnRandomFrogFromFirstFive();
    }

    void SpawnRandomFrogFromFirstFive()
    {
        GameObject frogToSpawn = null;

        // After 6 merges, spawn last prefab
        if (mergeCount >= 10)
        {
            frogToSpawn = allFrogPrefabs[^1]; // C# shortcut for last item
            mergeCount = 0;
        }
        // After 20 spawns, allow 5th & 6th prefab once
        else if (spawnCount >= 20 && !spawnMidLevelsOnce)
        {
            if (!hasSpawned5)
            {
                frogToSpawn = allFrogPrefabs[level5Index];
                hasSpawned5 = true;
            }
            else if (!hasSpawned6)
            {
                frogToSpawn = allFrogPrefabs[level6Index];
                hasSpawned6 = true;
            }

            if (hasSpawned5 && hasSpawned6)
            {
                spawnMidLevelsOnce = true;
            }
        }

        // If no forced logic applied, use weighted logic
        if (frogToSpawn == null)
        {
            int selectedLevel = GetWeightedRandomLevel();
            frogToSpawn = allFrogPrefabs[selectedLevel];

            // Store spawn history
            spawnHistory.Add(selectedLevel);
            if (spawnHistory.Count > maxHistory)
                spawnHistory.RemoveAt(0);
        }

        spawnCount++;

        // Instantiate frog
        currentFrog = Instantiate(frogToSpawn, spawnPoint.position, Quaternion.identity, spawnPoint);
        currentFrog.transform.localPosition = Vector3.zero;
        currentFrog.transform.localScale = Vector3.zero;
        StartCoroutine(AnimateScale(currentFrog.transform, 0.3f));
        playPopUp();
        var rb = currentFrog.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
    }

    IEnumerator AnimateScale(Transform target, float duration = 0.3f)
    {
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;
        float time = 0f;

        while (time < duration)
        {
            target.localScale = Vector3.Lerp(startScale, endScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        target.localScale = endScale;
    }


    public void playPopUp()
    {
        audioSource.PlayOneShot(popupsomething);
    }

    public void playUiPopUp()
    {
        audioSource.PlayOneShot(uipopup);
    }


    int GetWeightedRandomLevel()
    {
        int[] weights = new int[5];

        for (int i = 0; i < 5; i++)
        {
            int baseWeight = Mathf.Clamp(5 - i, 1, 5); // Lower levels favored

            // Reduce weight if recently merged
            if (i == lastMergedLevel)
                baseWeight = Mathf.Max(1, baseWeight - 3);

            // Reduce weight if too frequent in recent history
            int recentCount = spawnHistory.FindAll(x => x == i).Count;
            baseWeight = Mathf.Max(1, baseWeight - recentCount);

            weights[i] = baseWeight;
        }

        // Build pool
        List<int> weightedPool = new List<int>();
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i]; j++)
            {
                weightedPool.Add(i);
            }
        }

        if (weightedPool.Count == 0) return 0; // fallback

        return weightedPool[Random.Range(0, weightedPool.Count)];
    }





}

