using UnityEngine;

public class EventServiceTester : MonoBehaviour
{
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.G))
		{
			var type = Random.Range(0, 100).ToString();
			var data = Random.Range(0, 100).ToString();

			EventService.Instance.TrackEvent(type, data);
		}
	}
}
