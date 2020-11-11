using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    [SerializeField]
    private float _speed = 1.0f;

    [SerializeField]
    private int _powerUpID;

    [SerializeField]
    private AudioClip _clip;

    void Update()
    {
        transform.Translate(Vector3.down * _speed * Time.deltaTime );

        if (transform.position.y < -6.5f)
        {
            Destroy(this.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            AudioSource.PlayClipAtPoint(_clip, Camera.main.transform.position, 1f);

            Player player = other.GetComponent<Player>();

            if (player != null)
            {
                if (_powerUpID == 0)
                {
                    player.TripleShotPowerUpOn();
                }

                else if (_powerUpID == 1)
                {
                    player.SpeedPowerUpEnabled();
                }

                else if (_powerUpID == 2)
                {
                    player.ShieldPowerUpEnabled();
                }
            }
            
            Destroy(this.gameObject);
        }
    }
}
