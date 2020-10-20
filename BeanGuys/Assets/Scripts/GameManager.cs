using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;

    public NetworkManager networkManager;

    public Transform spawnpoint;
    public GameObject Main, Slave;
    public Transform mainCam;
    private CSceneManager cSceneManager;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LoadWorldTemplate()
    {
        SceneManager.LoadScene("TestMap");
    }

    private void OnLevelWasLoaded(int level)
    {
        if (SceneManager.GetActiveScene().name == "TestMap")
        {
            Debug.Log("NOVA SCENE CARREGADA");
            cSceneManager = GameObject.Find("SceneManager").GetComponent<CSceneManager>();
            mainCam = cSceneManager.camera;
            spawnpoint = cSceneManager.spawnPoint;
            Debug.Log("dsadasdadadsaDADADSADS IS :" + networkManager.player);
            Debug.Log("dsadasdadadsaDADADSADS IS :" + networkManager.player.GameState);
            networkManager.SyncUp();
        }
    }

    public void SpawnPlayer()
    {
        GameObject player = GameObject.Instantiate(Main, spawnpoint.position, Quaternion.identity);
        player.GetComponent<PlayerController>().camera = mainCam;
        mainCam.GetComponent<PlayerCamera>().enabled = true;
        mainCam.GetComponent<PlayerCamera>().ToFollow = player.transform;
    }

    public void SpawnSlave()
    {
        GameObject.Instantiate(Slave, spawnpoint.position, Quaternion.identity);
    }
}
