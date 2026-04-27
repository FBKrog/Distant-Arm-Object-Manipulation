using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace Cutscene
{
    public class CutsceneController : MonoBehaviour
    {
        [SerializeField] private InputAction startInput;
        [SerializeField] private InputAction skipInput;
        [SerializeField] private PlayableDirector cutsceneDirector;
        [SerializeField, Tooltip("How much of the cutscene is still viewed when skipped")] private float skipContent = 2f;

        private void Awake()
        {
            // Make button start cutscene
            startInput.performed += StartCutscene;
        }

        private void OnEnable()
        {
            startInput.Enable();
            skipInput.Enable();
        }

        private void OnDisable()
        {
            startInput.Disable();
            skipInput.Disable();
        }

        private void StartCutscene(InputAction.CallbackContext context)
        {
            // Play cutscene
            cutsceneDirector.Play();
            // Make button no longer start cutscene
            startInput.performed -= StartCutscene;
            // Make button skip cutscene instead
            skipInput.performed += SkipCutscene;
        }

        private void SkipCutscene(InputAction.CallbackContext context)
        {
            cutsceneDirector.time = cutsceneDirector.duration - skipContent;
        }
    }
}
