using System.Collections;
using UnityEngine;

public class WildeBeestBehavior : MonoBehaviour
{
    public float sheepMoveSpeed = 3.0f;

    public float UpandDown;
    public float UporDown;

    public float jumpCrocodileHeight = 2.5f;
    public float jumpHeight = 2.5f;
    public float jumpDistance = 3.0f;
    public float jumpDuration = 1.0f;

    private bool isJumping = false;
    private float jumpTimer = 0.0f;

    private Vector3 jumpStartPosition;
    private Vector3 jumpEndPosition;

    [Header("Wave Motion")]
    public float waveFrequency = 2.0f;   // How fast the vertical sine wave oscillates
    public float waveAmplitude = 0.5f;   // Vertical wave strength
    private float waveOffset;            // Per-instance phase so herd members don't sync

    [Header("Speed Variation")]
    public float speedVariation = 0.5f;   // Random +/- offset applied to base speed
    private float baseSpeed;
    private float currentSpeed;
    private float speedChangeTimer;
    private float nextSpeedChangeTime;

    [Header("Crocodile Avoidance")]
    public float avoidRadius = 3.0f;      // Start steering away when a croc is within this range
    public float maxAvoidStrength = 2.0f; // Cap on vertical avoidance force

    [Header("Movement State")]
    [SerializeField] private bool canMove = false;
    private bool isCaught;

    [Header("Caught Reaction")]
    public float scaredReactionDuration = 0.5f;
    public float scaredShakeAmount = 0.15f;
    public float scaredShakeFrequency = 40f;

    public bool CanMove => canMove;
    public bool IsCaught => isCaught;
    public float CurrentSpeed => currentSpeed;
    public bool DespawnOnExit => despawnOnExit;

    private bool despawnOnExit;
    private float exitBoundaryX = 14f;

    public void SetCanMove(bool value)
    {
        canMove = value;
    }

    public void SetDespawnOnExit(bool value, float boundaryX = 12f)
    {
        despawnOnExit = value;
        exitBoundaryX = boundaryX;
    }

    public void StartMoving()
    {
        canMove = true;
    }

    public void BecomeCaught()
    {
        isCaught = true;
        canMove = false;
        isJumping = false;
        jumpTimer = 0f;
    }

    /// <summary>
    /// Scared reaction: shake transform. Hook extra animation here later.
    /// </summary>
    public IEnumerator PlayScaredReaction()
    {
        Vector3 origin = transform.position;
        float elapsed = 0f;

        while (elapsed < scaredReactionDuration)
        {
            elapsed += Time.deltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / scaredReactionDuration);
            float ox = Mathf.Sin(elapsed * scaredShakeFrequency) * scaredShakeAmount * damper;
            float oy = Mathf.Cos(elapsed * scaredShakeFrequency * 1.3f) * scaredShakeAmount * damper;
            transform.position = origin + new Vector3(ox, oy, 0f);
            yield return null;
        }

        transform.position = origin;
    }

    public void TryEscapeJumpFromCrocodile()
    {
        if (isCaught || isJumping || !canMove) return;

        isJumping = true;
        jumpTimer = 0f;
        jumpStartPosition = transform.position;
        jumpEndPosition = new Vector3(
            jumpStartPosition.x + jumpDistance,
            jumpStartPosition.y,
            jumpStartPosition.z
        );
        jumpHeight = jumpCrocodileHeight;
    }

    void Start()
    {
        UpandDown = Random.Range(0.0f, 1.0f);
        UporDown = Random.Range(0.0f, 1.0f);

        waveOffset = Random.Range(0f, Mathf.PI * 2f);

        baseSpeed = sheepMoveSpeed;
        currentSpeed = baseSpeed;
        nextSpeedChangeTime = Random.Range(1f, 3f);
    }

    void Update()
    {
        if (isCaught || !canMove) return;

        // Jump takes priority over normal movement
        if (isJumping){ Jump();return; }

        // Past right edge: despawn on exit, or wrap back to the left
        if (transform.position.x > exitBoundaryX)
        {
            if (despawnOnExit)
            {
                Destroy(gameObject);
                return;
            }

            if (transform.position.y > 3.0f)
            {
                transform.position = new Vector3(
                    -13.0f,
                    transform.position.y - Random.Range(2.0f, 5.0f),
                    transform.position.z
                );
            }
            else if (transform.position.y < -5.0f)
            {
                transform.position = new Vector3(
                    -13.0f,
                    transform.position.y + Random.Range(2.0f, 7.0f),
                    transform.position.z
                );
            }
            else
            {
                transform.position = new Vector3(
                    -13.0f,
                    transform.position.y,
                    transform.position.z
                );
            }
        }
        else
        {
            // Periodic speed jitter
            float verticalDelta = Mathf.Sin(Time.time * waveFrequency + waveOffset) * waveAmplitude * Time.deltaTime;

            speedChangeTimer += Time.deltaTime;
            if (speedChangeTimer > nextSpeedChangeTime)
            {
                speedChangeTimer = 0f;
                nextSpeedChangeTime = Random.Range(1f, 3f);
                currentSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
            }

            float horizontalDelta = currentSpeed * Time.deltaTime;


            Vector3 moveDelta = CalculateBaseMovement(); // Forward + vertical wave

            // Steer away from nearby crocodiles
            Vector3 avoidDelta = GetAvoidanceVector();

            transform.position += moveDelta * Time.deltaTime;
        }

        if (!isJumping && Random.value < 0.001f)   // Rare idle hop
        {
            // Random short hop while roaming
            StartJump(Random.Range(1f, 2f), Random.Range(0.3f, 1.6f));
        }
    }

    private void StartJump(float offset, float heightOffset)
    {
        if (!isJumping)
        {
            isJumping = true;
            jumpTimer = 0.0f;

            jumpStartPosition = transform.position;

            jumpEndPosition = new Vector3(
                jumpStartPosition.x + offset,
                jumpStartPosition.y,
                jumpStartPosition.z
            );

            jumpHeight = heightOffset;
        }
    }

    void Jump()
    {
        jumpTimer += Time.deltaTime;

        // t goes from 0 to 1 over the jump
        float t = jumpTimer / jumpDuration;

        // Horizontal lerp toward landing point
        Vector3 currentPosition = Vector3.Lerp(
            jumpStartPosition,
            jumpEndPosition,
            t
        );

        // Arc height via sine
        currentPosition.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;

        transform.position = currentPosition;

        // Land and end jump
        if (t >= 1.0f)
        {
            transform.position = jumpEndPosition;
            isJumping = false;
            jumpTimer = 0.0f;
        }
    }

    Vector3 CalculateBaseMovement()
    {
        float vertical = Mathf.Sin(Time.time * waveFrequency + waveOffset) * waveAmplitude;
        return new Vector3(currentSpeed, vertical, 0);
    }

    Vector3 GetAvoidanceVector()
    {
        Vector3 totalAvoid = Vector3.zero;
        Crocodile[] crocs = FindObjectsOfType<Crocodile>(); // Find all crocs in scene
        foreach (var croc in crocs)
        {
            Vector3 toCroc = croc.transform.position - transform.position;
            float dist = toCroc.magnitude;
            if (dist < avoidRadius && dist > 0.01f)
            {
                // Push away on Y so we don't overlap crocs vertically
                float avoidY = -toCroc.normalized.y; // Opposite of crocs relative Y
                float strength = Mathf.Clamp(1.0f / (dist * dist), 0, maxAvoidStrength);
                totalAvoid.y += avoidY * strength;
            }
        }
        // Clamp avoidance so it doesn't overpower movement
        totalAvoid.y = Mathf.Clamp(totalAvoid.y, -maxAvoidStrength, maxAvoidStrength);
        return totalAvoid; // Y-only avoidance vector
    }
}