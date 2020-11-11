using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _enemyShipPrefab;

    [SerializeField]
    private GameObject[] _powerUps;

    private GameManager _gameManager;

    private void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    private void Update()
    {
         
    }

    public void StartSpawning()
    {
        StartCoroutine(SpawningEnemiesDelay());
        StartCoroutine(SpawningPowerUpsDelay());
    }

    private IEnumerator SpawningEnemiesDelay()
    {
        while (_gameManager.gameOver == false)
        {
            Instantiate(_enemyShipPrefab, new Vector3(Random.Range(-8f, 8f), 6.5f, 0), Quaternion.identity);
            yield return new WaitForSeconds(5.0f);
        }
    }

    private IEnumerator SpawningPowerUpsDelay()
    {
        
        while (_gameManager.gameOver == false)
        {
            int randomPowerUp = Random.Range(0, 3);
            Instantiate(_powerUps[randomPowerUp], new Vector3(Random.Range(-8f, 8f), 6.5f, 0), Quaternion.identity);
            yield return new WaitForSeconds(5.0f);
        }
    }

}
