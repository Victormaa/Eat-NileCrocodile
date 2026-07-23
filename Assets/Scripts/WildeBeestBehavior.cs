using UnityEngine;

public class WildeBeestBehavior : MonoBehaviour
{
    public float sheepMoveSpeed = 3.0f;

    public float UpandDown;
    public float UporDown;

    // 跳跃参数
    public float jumpCrocodileHeight = 2.5f;
    public float jumpHeight = 2.5f;
    public float jumpDistance = 3.0f;
    public float jumpDuration = 1.0f;

    private bool isJumping = false;
    private float jumpTimer = 0.0f;

    private Vector3 jumpStartPosition;
    private Vector3 jumpEndPosition;

    [Header("步伐起伏")]
    public float waveFrequency = 2.0f;   // 上下摆动频率
    public float waveAmplitude = 0.5f;   // 摆动幅度
    private float waveOffset;            // 随机相位，让每只角马不同步

    [Header("快慢变化")]
    public float speedVariation = 0.5f;   // 速度波动范围
    private float baseSpeed;
    private float currentSpeed;
    private float speedChangeTimer;
    private float nextSpeedChangeTime;

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
        // 如果正在跳跃，只执行跳跃代码
        if (isJumping){ Jump();return; }

        // 羊到达右边后，从左边重新出现
        if (transform.position.x > 12.0f)
        {
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
            // 正常移动
            float verticalDelta = Mathf.Sin(Time.time * waveFrequency + waveOffset) * waveAmplitude * Time.deltaTime;

            speedChangeTimer += Time.deltaTime;
            if (speedChangeTimer > nextSpeedChangeTime)
            {
                speedChangeTimer = 0f;
                nextSpeedChangeTime = Random.Range(1f, 3f);
                currentSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
            }

            float horizontalDelta = currentSpeed * Time.deltaTime;


            // 如果原来有倾斜方向，可以叠加，但建议去掉旧的恒定漂移，完全由正弦波控制
            Vector3 moveDelta = new Vector3(horizontalDelta, verticalDelta, 0);
            transform.position += moveDelta;
        }

        if (!isJumping && Random.value < 0.001f)   // 大约每几秒有一次
        {
            // 触发一次小跳跃，方向和距离可以随机
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

        // t从0变化到1
        float t = jumpTimer / jumpDuration;

        // 水平移动
        Vector3 currentPosition = Vector3.Lerp(
            jumpStartPosition,
            jumpEndPosition,
            t
        );

        // 使用Sin制作跳跃弧线
        currentPosition.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;

        transform.position = currentPosition;

        // 跳跃结束
        if (t >= 1.0f)
        {
            transform.position = jumpEndPosition;
            isJumping = false;
            jumpTimer = 0.0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Crocodile") && !isJumping)
        {
            isJumping = true;
            jumpTimer = 0.0f;

            jumpStartPosition = transform.position;

            jumpEndPosition = new Vector3(
                jumpStartPosition.x + jumpDistance,
                jumpStartPosition.y,
                jumpStartPosition.z
            );

            jumpHeight = jumpCrocodileHeight;
        }
    }
}