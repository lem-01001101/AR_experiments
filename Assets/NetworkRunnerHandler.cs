using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class NetworkRunnerHandler : MonoBehaviour
{
    private NetworkRunner _runner;

    async void Start()
    {
        _runner = GetComponent<NetworkRunner>();

        if (_runner != null)
        {
            _runner.ProvideInput = true;
            await StartGame(_runner);
        }
        else
        {
            Debug.LogError("NetworkRunner component missing on NetworkRunner GameObject.");
        }
    }

    protected virtual Task StartGame(NetworkRunner runner)
    {
        return runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = "ARSession",
            Scene = null, // No scene loading
            SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }


}
