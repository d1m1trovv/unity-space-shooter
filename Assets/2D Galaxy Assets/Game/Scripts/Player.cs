using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public bool canTripleShot = false;
    public bool isSpeedPowerUpEnabled = false;
    public bool isShieldEnabled = false;

    public int lives = 3;

    [SerializeField]
    private GameObject _shieldGameObject;

    [SerializeField]
    private GameObject _explosionPrefab;

    [SerializeField]
    private GameObject _laserPrefab;

    [SerializeField]
    private GameObject _tripleShotPrefab;

    [SerializeField]
    private float _fireRate = 0.25f;

    [SerializeField]
    private GameObject[] _engines;

    private float _nextFire = 0.0f;

    [SerializeField]
    private float _speed = 5.0f;

    private UIManager _uiManager;

    private GameManager _gameManager;

    private AudioSource _audioSource;

    private int _hitCounter = 0;

    private void Start()
    {
        transform.position = new Vector3(0, 0, 0);

        _uiManager = GameObject.Find("Canvas").GetComponent<UIManager>();

        if (_uiManager != null)
        {
            _uiManager.UpdateLives(lives);
        }

        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        _audioSource = GetComponent<AudioSource>();

        _hitCounter = 0;
    }

    private void Update()
    {
        Movement();

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButton(0))
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (Time.time > _nextFire)
        {
            _audioSource.Play();

            if (canTripleShot == true)
            {
                Instantiate(_tripleShotPrefab, transform.position , Quaternion.identity);
                _nextFire = Time.time + _fireRate;
            }
            else
            {
                Instantiate(_laserPrefab, transform.position + new Vector3(0, 0.9f, 0), Quaternion.identity);
                _nextFire = Time.time + _fireRate;
            }
        }
    }

    private void Movement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        if (isSpeedPowerUpEnabled == true)
        {
            transform.Translate(Vector3.right * Time.deltaTime * _speed * 2.0f * horizontalInput);
            transform.Translate(Vector3.up * Time.deltaTime * _speed * 2.0f * verticalInput);
        }

        else
        {
            transform.Translate(Vector3.right * Time.deltaTime * _speed * horizontalInput);
            transform.Translate(Vector3.up * Time.deltaTime * _speed * verticalInput);
        }

        if (transform.position.y > 0)
        {
            transform.position = new Vector3(transform.position.x, 0, 0);
        }

        else if (transform.position.y < -4.2f)
        {
            transform.position = new Vector3(transform.position.x, -4.2f, 0);
        }

        else if (transform.position.x > 9.6f)
        {
            transform.position = new Vector3(-9.6f, transform.position.y, 0);
        }

        else if (transform.position.x < -9.6f)
        {
            transform.position = new Vector3(9.6f, transform.position.y, 0);
        }
    }

    public void TripleShotPowerUpOn()
    {
        canTripleShot = true;
        StartCoroutine(TripleShotPowerUpDownRoutine());
    }

    public IEnumerator TripleShotPowerUpDownRoutine()
    {
        yield return new WaitForSeconds(5.0f);
        canTripleShot = false;
    }

    public void SpeedPowerUpEnabled()
    {
        isSpeedPowerUpEnabled = true;
        StartCoroutine(SpeedPowerUpDownRoutine());
    }

    public IEnumerator SpeedPowerUpDownRoutine()
    {
        yield return new WaitForSeconds(5.0f);
        isSpeedPowerUpEnabled = false;
    }

    public void ShieldPowerUpEnabled()
    {
        isShieldEnabled = true;
        _shieldGameObject.SetActive(true);
    }

    public void Damage()
    {
        if (isShieldEnabled == true)
        {
            isShieldEnabled = false;
            _shieldGameObject.SetActive(false);
            return;
        }

        _hitCounter++;

        if (_hitCounter == 1 && isShieldEnabled == false)
        {
            _engines[0].SetActive(true);
        }

        else if (_hitCounter == 2 && isShieldEnabled == false)
        {
            _engines[1].SetActive(true);
        }

        lives -= 1;
        _uiManager.UpdateLives(lives);

        if (lives < 1)
        {
            Destroy(this.gameObject);
            Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
            _gameManager.gameOver = true;
            _uiManager.ShowTitleScreen();
        }
    }
}
