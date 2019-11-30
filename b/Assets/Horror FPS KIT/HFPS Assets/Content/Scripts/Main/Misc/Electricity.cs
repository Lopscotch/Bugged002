/*
 * Electricity.cs - wirted by ThunderWire Games
 * ver. 2.0
*/

using UnityEngine;

public class Electricity : MonoBehaviour {

	private HFPS_GameManager gameManager;

    public string offHint;
    public float time;
	public GameObject LampIndicator;

    [SaveableField]
	public bool isPoweredOn = true;

	void Start()
	{
        gameManager = HFPS_GameManager.Instance;
    }

	public void ShowOffHint()
	{
        gameManager.ShowHint (offHint, time);
	}

	public void SwitchElectricity(bool power)
	{
		isPoweredOn = power;

		if (LampIndicator) {
            if (power)
            {
                LampIndicator.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(1f, 1f, 1f));
                LampIndicator.GetComponentInChildren<Light>().enabled = true;
            }
            else
            {
                LampIndicator.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(0f, 0f, 0f));
                LampIndicator.GetComponentInChildren<Light>().enabled = false;
            }
		}
	}
}