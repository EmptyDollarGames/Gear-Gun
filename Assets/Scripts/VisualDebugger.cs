using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class VisualDebugger : MonoBehaviour
{
    public GameObject currentVelocityPanel;
    public GameObject targetVelocityPanel;
    public GameObject currentInputPanel;
    public GameObject currentStatePanel;

    public PlayerMovement move;

    private void Update()
    {
        currentVelocityPanel.GetComponent<TextMeshProUGUI>().text = move.GetComponent<Rigidbody>().velocity.magnitude.ToString();
        targetVelocityPanel.GetComponent<TextMeshProUGUI>().text = move.CurrentTargetSpeed.ToString();
        currentInputPanel.GetComponent<TextMeshProUGUI>().text = move.CurrentInput.ToString();
        currentStatePanel.GetComponent<TextMeshProUGUI>().text = move.currentState.ToString();
    }
}
