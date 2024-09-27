using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json;

[Serializable]
public class Event
{
	public string type;
	public string data;

	public Event(string type, string data)
	{
		this.type = type;
		this.data = data;
	}
}

public class EventService : MonoBehaviour
{
	private const string EventQueuePref = "EventQueue";

	[SerializeField] private string _serverUrl;
	[SerializeField] private float _cooldownBeforeSend;

	private Queue<Event> _eventQueue;
	private float _currentCooldown;

	private void Awake()
	{
		var loadedEventQueue = PlayerPrefs.GetString(EventQueuePref);

		if (string.IsNullOrEmpty(loadedEventQueue) == false)
		{
			try
			{
				_eventQueue = JsonConvert.DeserializeObject<Queue<Event>>(loadedEventQueue);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				_eventQueue = new();
			}
		}
		else
		{
			_eventQueue = new();
		}
	}

	private void Update()
	{
		_currentCooldown = Mathf.MoveTowards(_currentCooldown, 0f, Time.unscaledDeltaTime);

		if (Mathf.Approximately(_currentCooldown, 0f) && _eventQueue.Count > 0)
		{
			StartCoroutine((IEnumerator)SendEventCoroutine());
			_currentCooldown = _cooldownBeforeSend;
		}
	}

	private void Enqueue(Event @event)
	{
		_eventQueue.Enqueue(@event);

		var queueJson = JsonConvert.SerializeObject(_eventQueue);
		PlayerPrefs.SetString(EventQueuePref, queueJson);
	}

	private void Dequeue()
	{
		_eventQueue.Dequeue();

		var queueJson = JsonConvert.SerializeObject(_eventQueue);
		PlayerPrefs.SetString(EventQueuePref, queueJson);
	}

	private IEnumerable SendEventCoroutine()
	{
		var @event = _eventQueue.Peek();

		var json = JsonConvert.SerializeObject(@event);
		var request = UnityWebRequest.Post(_serverUrl, json);

		yield return request.SendWebRequest();

		if (request.responseCode == 200)
		{
			Dequeue();
		}
	}

	public void TrackEvent(string type, string data)
	{
		var @event = new Event(type, data);
		Enqueue(@event);
	}
}
