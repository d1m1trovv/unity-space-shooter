using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField]
    private float _speed = 1.0f;

    [SerializeField]
    private GameObject _enemy;

    [SerializeField]
    private GameObject _enemyExplosionPrefab;

    [SerializeField]
    private AudioClip _clip;

    private UIManager _uiManager;

    private GameManager _gameManager;

    void Start()
    {
        _uiManager = GameObject.Find("Canvas").GetComponent<UIManager>();

        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }
    
    void Update()
    {
        Movement();

        if (_gameManager.gameOver == true && transform.position.y <= -6.4f)
        {
            Destroy(_enemy.gameObject);
        }
    }

    private void Movement()
    {
        transform.Translate(Vector3.down * _speed * Time.deltaTime);

        if (transform.position.y <= -6.5f)
        {
            float randomX = Random.Range(-10.5f, 10.5f);
            transform.position = new Vector3(randomX, 6.5f, 0);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            Player player = other.GetComponent<Player>();

            if (player != null)
            {
                player.Damage();
            }

            Instantiate(_enemyExplosionPrefab, transform.position, Quaternion.identity);
            AudioSource.PlayClipAtPoint(_clip, Camera.main.transform.position, 1f);
            Destroy(this.gameObject);
        }

        else if (other.tag == "laser")
        {

            if (other.transform.parent != null)
            {
                Destroy(other.transform.parent.gameObject);
            }

            Instantiate(_enemyExplosionPrefab, transform.position, Quaternion.identity);
            Destroy(other.gameObject);
            AudioSource.PlayClipAtPoint(_clip, Camera.main.transform.position, 1f);
            Destroy(this.gameObject);
            _uiManager.UpdateScore();
        }
    }
    /*private void SpawnEnemy()
    {
       Instantiate(_enemy, new Vector3(Random.Range(-10.5f, 10.5f), 6.5f, 0), Quaternion.identity);  
    }*/
}
