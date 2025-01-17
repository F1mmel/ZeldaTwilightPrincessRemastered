using System;
using System.Threading.Tasks;
using UnityEngine;

public class SceneChange : MonoBehaviour
{
    public SceneChangeType Type;

    public void Init()
    {
        // Do proper scaling of object
        Actor actor = GetComponent<Actor>();
        transform.localScale = new Vector3(transform.localScale.x * actor.Scale.x, transform.localScale.y * actor.Scale.y, transform.localScale.z * actor.Scale.z);
        
        if (Type == SceneChangeType.AREA)
        {
            // Make collider trigger
            
            MeshCollider collider = GetComponent<MeshCollider>();
            collider.isTrigger = true;
        }
    }

    private async void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && Type == SceneChangeType.AREA)
        {
            Actor actor = GetComponent<Actor>();
            int sclcIndex = actor.SCOB_targetIndex;
            
            Actor sclc = StageLoader.Instance.SCLC[sclcIndex];
            Debug.Log(sclc.MapName);
            Debug.Log(sclc.SpawnIndex);

            Stage stage = GetEnumFromMapString(sclc.MapName);
            
            await Task.Run(() =>
            {
                TransitionManager.Fade(() =>
                {
                    // If player already in same stage, then don't load stage
                    if (stage == StageLoader.Instance.Stage)
                    {
                        Link.SetControlsWithoutResetInput(Link.Controls.Frozen);
                
                        Vector3 pos = GetSpawnLocationFromIndex(stage, sclc.SpawnIndex);
                        //Link.Teleport(pos);
                        //Link.Instance.PlayerController.Move = Vector2.zero;
                        //Input.ResetInputAxes();
                        Link.Instance.PlayerController.transform.position = pos;
                
                        Link.SetControlsWithoutResetInput(Link.Controls.Default);
                    }
                    else
                    {
                        StageLoader.Instance.EnterStage(stage, StageEnterType.DESTROY);
                        
                        // Get hier pos von Spawn
                    }
                });
            });
            



            //foreach (Actor sclc in StageLoader.Instance.SCLC)
            {
                
            }
        }
    }

    private Stage GetEnumFromMapString(string map)
    {
        foreach (Stage stage in Enum.GetValues(typeof(Stage)))
        {
            string name = stage.GetStageFile();
            if (name == map)
            {
                return stage;
            }
        }

        return Stage.___MISC___;
    }

    private Vector3 GetSpawnLocationFromIndex(Stage stage, int index)
    {
        return Vector3.zero;
    }
}

public enum SceneChangeType
{
    AREA,
    DOOR
} 