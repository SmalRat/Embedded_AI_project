using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MyPoolObject {

	[Header("Projectile settings")]
	[Tooltip("Projectile traveling speed")]
	[HideInInspector]
	public float Speed; 

	[Tooltip("Projectile life time")]
	public float TimeTodestroy;

	[Tooltip("Projectile Explosion FX (Optional)")]
	public GameObject Explosion;

    [HideInInspector]
    public GameObject targetObject;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("No Rigidbody found on Bullet! Please attach a Rigidbody.");
        }

        gameObject.SetActive(false);
    }

	private void OnEnable()
	{
		StartCoroutine(DestroyDelay()); // Destroy this projectile after TimeToDestroy time, every time this projectile is enable

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
	}

    private void Start()
    {
        if (rb != null)
        {
            rb.velocity = transform.forward * Speed;
        }
    }

	// Destroy gameobject when collisin happen
	protected virtual void OnCollisionEnter(Collision col)
	{	
        Debug.Log($"Collision with {col.gameObject}");
        if (col.gameObject == targetObject)
        {
            GameManager.Instance.IncreaseHitCount();
        }
        
		if(IsPooling)
			Destroy(gameObject); // disable this projectile
		else
			Destroy(gameObject, 0.1f); // if not pooling destroy it immediately
		
		if(Explosion != null)
		Instantiate(Explosion, transform.position, transform.rotation);
	}

	IEnumerator DestroyDelay()
	{
		yield return new WaitForSeconds(TimeTodestroy);
		if(IsPooling)
			Destroy(gameObject); // disable this projectile
		else
			Destroy(gameObject,0.1f); // if not pooling destroy it immediately
		
		if(Explosion != null)
		Instantiate(Explosion, transform.position, transform.rotation);

	}

	public override void OnobjectReuse(Vector3 target, float speed, GameObject targetObj)
	{
        targetObject = targetObj;
        Speed = speed;
		// transform.LookAt(target);
		if (rb != null)
        {
            rb.velocity = transform.forward.normalized * Speed;
        }
	}

    public void SetTarget(GameObject newTarget)
    {
        targetObject = newTarget;
        // Debug.Log(newTarget);
    }	
}
