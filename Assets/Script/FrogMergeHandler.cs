using UnityEngine;

public class FrogMergeHandler : MonoBehaviour
{
    public int frogLevel;
    public GameObject nextLevelPrefab;

    private bool hasMerged = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasMerged) return;

        FrogMergeHandler otherFrog = collision.gameObject.GetComponent<FrogMergeHandler>();

        if (otherFrog != null && otherFrog.frogLevel == frogLevel && !otherFrog.hasMerged)
        {
            hasMerged = true;
            otherFrog.hasMerged = true;

            // Calculate merge point in world space
            Vector3 spawnPosition = (transform.position + otherFrog.transform.position) / 2f;

            // Use one of the frog's parent (if any)
            Transform parentToUse = transform.parent != null ? transform.parent : otherFrog.transform.parent;

            if (nextLevelPrefab != null)
            {
                // Instantiate with parent
                GameObject nextFrog = Instantiate(nextLevelPrefab, spawnPosition, Quaternion.identity, parentToUse);

                // Fix Z position
                Vector3 fixedPosition = nextFrog.transform.position;
                fixedPosition.z = 0f;
                nextFrog.transform.position = fixedPosition;

                // Add bounce force to the new frog
                Rigidbody2D rb = nextFrog.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 forceDir = new Vector2(Random.Range(-0.2f, 0.2f), 0.4f); // upward + slight random
                    rb.AddForce(forceDir.normalized * 200f, ForceMode2D.Impulse);
                }
            }
            FindObjectOfType<SpawnerController>().SetLastMergedLevel(frogLevel);
            FindObjectOfType<SpawnerController>().OnFrogMerged();
            FindObjectOfType<SpawnerController>().playUiPopUp();


            Destroy(otherFrog.gameObject);
            Destroy(this.gameObject);
        }
    }
}
