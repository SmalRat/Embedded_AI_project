using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireAtTarget : MonoBehaviour
{
    [Header("Turret Settings")]
    [Tooltip("Pivot for horizontal rotation")]
    public Transform HorizontalPivot;

    [Tooltip("Pivot for vertical rotation")]
    public Transform VerticalPivot;

    [Header("Horizontal Rotation Settings")]
    [Tooltip("If you want to limit horizontal turret rotation")]
    public bool HorizontalRotationLimit;

    [Tooltip("Right rotation limit")]
    [Range(0, 180)]
    public float RightRotationLimit;

    [Tooltip("Left rotation limit")]
    [Range(0, 180)]
    public float LeftRotationLimit;

    [Header("Vertical Rotation Settings")]
    [Tooltip("If you want to limit vertical turret rotation")]
    public bool VerticalRotationLimit;

    [Tooltip("Upwards rotation limit")]
    [Range(0, 70)]
    public float UpwardsRotationLimit;

    [Tooltip("Downwards rotation limit")]
    [Range(0, 70)]
    public float DownwardsRotationLimit;

    [Tooltip("Turning speed")]
    [Range(0, 300)]
    public float TurnSpeed;

    [Header("Gun Settings")]

    [Tooltip("Click if you want to use pooling")]
    public bool UsePooling;

    [Tooltip("Gun firing rate")]
    public float FireRate;

    [Tooltip("Projectile traveling speed")]
    public float ProjectileSpeed;

    [Tooltip("How many projectile in this turret")]
    public float ProjectileCount;

    [Tooltip("Projectile prefabs")]
    public GameObject ProjectilePrefab;

    [Tooltip("Adjust the efficiency of this turret")]
    [Range(3f, 4f)]
    public float Efficiency;

    [Tooltip("Barrel for instantiating projectile")]
    public Transform[] Barrel;

    public GameObject targetObject;

    [HideInInspector]
    public Transform target;

    [HideInInspector]
    public Vector3 predictedTargetPosition;
    private Vector3 rayDirection;

    [Header("Effects (Optional)")]
    [Tooltip("Shoot effect when firing the gun (optional)")]
    public GameObject ShootFX;
    public GameObject BulletShellFX;

    private Vector3 targetlastPosition;
    protected ParticleSystem bulletShellFX_PS;
    protected ParticleSystem shootFX_PS;
    protected float nextFireAllowed;
    protected bool IsAiming = false;

    public bool connected = false;

    protected virtual void Start()
    {
        target = targetObject.transform;

        if (HorizontalPivot == null)
        {
            Debug.Log("There is no horizontal pivot found, Please drag your pivot into this script");
            return;
        }

        if (VerticalPivot == null)
        {
            Debug.Log("There is no vertical pivot found, Please drag your pivot into this script");
            return;
        }

        if (Barrel.Length == 0)
        {
            Debug.Log("There is no Barrel found, Please drag your pivots into this script");
            return;
        }

        if (ProjectilePrefab == null)
        {
            Debug.Log("There is no projectile prefab found, Please drag your projectile prefab into this script");
            return;
        }

        if (UsePooling)
        {
            if (MyPoolManager.instance == null)
            {
                Debug.Log("PoolManager is missing, Please create a GameObject and add PoolManager.cs");
                return;
            }
            else
                MyPoolManager.instance.CreatePool(ProjectilePrefab, 100);
        }

        if (BulletShellFX != null)
        {
            BulletShellFX.SetActive(true);
            bulletShellFX_PS = BulletShellFX.GetComponent<ParticleSystem>();
            bulletShellFX_PS.Stop();
        }

        if (ShootFX != null)
        {
            ShootFX.SetActive(true);
            shootFX_PS = ShootFX.GetComponent<ParticleSystem>();
            shootFX_PS.Stop();
        }
    }

    private void FixedUpdate()
    {
        // Debug.Log("Hello from gun!");
        LeadTarget();
        HorizontalRotation();
        VerticalRotation();
        Fire();
    }

    private void LeadTarget()
    {
        if (target == null) return;
        // Debug.Log(target);
        Vector3 targetSpeed = (target.position - targetlastPosition);
        targetSpeed /= Time.deltaTime;

        float distance = Vector3.Distance(transform.position, target.position);
        float projectileTravelTime = distance / Mathf.Max(ProjectileSpeed, 2f);
        Vector3 aimPoint = target.position + targetSpeed * Efficiency / 4 * projectileTravelTime;

        float distance2 = Vector3.Distance(transform.position, aimPoint);
        float projectileTravelTime2 = distance2 / Mathf.Max(ProjectileSpeed, 2f);
        predictedTargetPosition = target.position + targetSpeed * Efficiency / 4 * projectileTravelTime2;

        Debug.DrawLine(transform.position, predictedTargetPosition, Color.blue);

        targetlastPosition = target.position;
    }

    private void HorizontalRotation()
    {
        if (HorizontalPivot == null && VerticalPivot == null || target == null) return;

        // Vector3 targetPositionInLocalSpace = this.transform.InverseTransformPoint(predictedTargetPosition);
        // targetPositionInLocalSpace.y = 0f;

        // Vector3 clamp = targetPositionInLocalSpace;
        Vector3 rayDirectionInLocalSpace = rayDirection;
        rayDirectionInLocalSpace.y = 0f;
        Vector3 clamp = rayDirectionInLocalSpace;

        if (HorizontalRotationLimit)
        {
            if (rayDirectionInLocalSpace.x >= 0f)
                clamp = Vector3.RotateTowards(Vector3.forward, rayDirectionInLocalSpace, Mathf.Deg2Rad * RightRotationLimit, 0f);
            else
                clamp = Vector3.RotateTowards(Vector3.forward, rayDirectionInLocalSpace, Mathf.Deg2Rad * LeftRotationLimit, 0f);
        }

        Quaternion whereToRotate = Quaternion.LookRotation(clamp);
        HorizontalPivot.localRotation = Quaternion.RotateTowards(HorizontalPivot.localRotation, whereToRotate, TurnSpeed * Time.deltaTime);
    }

    private void VerticalRotation()
    {
        if (HorizontalPivot == null && VerticalPivot == null || target == null) return;

        // Vector3 targetPositionInLocalSpace = HorizontalPivot.transform.InverseTransformPoint(predictedTargetPosition);
        // targetPositionInLocalSpace.x = 0f;

        // Vector3 clamp = targetPositionInLocalSpace;

        Vector3 rayDirectionInLocalSpace = rayDirection;
        rayDirectionInLocalSpace.x = 0f;
        Vector3 clamp = rayDirectionInLocalSpace;

        if (VerticalRotationLimit)
        {
            if (rayDirectionInLocalSpace.y >= 0f)
                clamp = Vector3.RotateTowards(Vector3.forward, rayDirectionInLocalSpace, Mathf.Deg2Rad * UpwardsRotationLimit, 0f);
            else
                clamp = Vector3.RotateTowards(Vector3.forward, rayDirectionInLocalSpace, Mathf.Deg2Rad * DownwardsRotationLimit, 0f);
        }

        Quaternion whereToRotate = Quaternion.LookRotation(clamp);
        VerticalPivot.localRotation = Quaternion.RotateTowards(VerticalPivot.localRotation, whereToRotate, 2 * TurnSpeed * Time.deltaTime);

        Vector3 dirTotarget = rayDirection.normalized;
        float angle = Mathf.Abs(Vector3.Angle(VerticalPivot.forward, dirTotarget));
        IsAiming = angle < 5;
    }

    protected virtual void Fire()
    {
        if (target != null && ProjectileCount > 0 && Time.time > nextFireAllowed && IsAiming && connected)
        {
            for (int i = 0; i < Barrel.Length; i++)
            {
                if (UsePooling)
                    MyPoolManager.instance.ReuseObject(ProjectilePrefab, Barrel[i].position, Barrel[i].rotation, predictedTargetPosition, ProjectileSpeed, targetObject);
                else
                {
                    Bullet newProjectile = Instantiate(ProjectilePrefab, Barrel[i].position, Barrel[i].rotation).GetComponent<Bullet>();
                    // newProjectile.transform.LookAt(predictedTargetPosition);
                    newProjectile.Speed = this.ProjectileSpeed;
                    newProjectile.SetTarget(targetObject);
                    newProjectile.gameObject.SetActive(true);
                }

                GameManager.Instance.IncreaseShotCount();
                ProjectileCount--;
            }

            nextFireAllowed = Time.time + FireRate;

            if (BulletShellFX != null)
            {
                bulletShellFX_PS.Play();
                Invoke("StopBulletShellEffect", 1.2f);
            }

            if (ShootFX == null) return;
            shootFX_PS.Play();
        }
    }

    void StopBulletShellEffect()
    {
        bulletShellFX_PS.Stop();
    }

    public void SetRayDirection(Vector3 newDirection)
    {
        rayDirection = newDirection.normalized;
    }
}
