using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Cutscene
{
    public class HeadTurnManager : MonoBehaviour
    {
        [HideInInspector] public static HeadTurnManager manager;

        private List<HeadTurner> allHeadTurners = new List<HeadTurner>();

        [SerializeField] private Transform playerHeadTrans;

        [Header("Values for animating")]
        [SerializeField] public bool turnHeads = false;
        [SerializeField, Range(0f, 1f)] public float turningWeight = 0f;

        void Awake()
        {
#if UNITY_EDITOR
            if (manager != null)
            {
                Debug.LogWarning("More than one head turn manager exists, destroying self", this);
                Destroy(this);
            }
#endif
            manager = this;
            if (playerHeadTrans == null)
                playerHeadTrans = Camera.main.transform;
        }

        // Update is called once per frame
        void Update()
        {
            // If we are to turn heads, make each robot turn their head
            if (turnHeads)
            {
                foreach (HeadTurner head in allHeadTurners)
                {
                    // Make the robot look towards target with the current weight
                    head.LookAt(playerHeadTrans.position, turningWeight);
                }
            }

        }

        public void Subscribe(HeadTurner toSubscribe)
        {
            allHeadTurners.Add(toSubscribe);
        }

        public void Unsubscribe(HeadTurner toUnsubscribe)
        {
            allHeadTurners.Remove(toUnsubscribe);
        }
    }
}
