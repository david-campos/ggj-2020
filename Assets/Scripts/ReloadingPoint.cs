using DefaultNamespace;
using UnityEngine;

public class ReloadingPoint : MonoBehaviour
{
    public LoadType loadType;
    
    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Player")) {
            other.gameObject.GetComponent<PlayerLoading>().Reload(loadType);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
