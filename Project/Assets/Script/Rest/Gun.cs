using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint; // The point from which bullets will be fired
    public float shootForce = 100f;
    public float fireRate = 10.0f;
    private float nextFireTime = 0f;
    public float azimuth = 90.0f;  // Angle around the vertical axis in degrees
    public float altitude = 30.0f; // Angle above the horizontal plane in degrees
    public float radius = 1.0f;   // Adjust as needed

    void Update()
    {
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        // Define azimuth and altitude
        

        // Convert azimuth and altitude to Cartesian coordinates
        float azimuthRad = azimuth * Mathf.Deg2Rad;
        float altitudeRad = altitude * Mathf.Deg2Rad;

        float x = 1;
        float y = Mathf.Sin(altitudeRad);
        float z = 1;

        // Create bullet
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        Vector3 direction = new Vector3(x, y, z).normalized;

        // Apply force to the bullet
        rb.AddForce(direction * shootForce, ForceMode.Impulse);

        // Update total shots count
        GameManager.Instance.IncreaseShotCount();
    }
}
