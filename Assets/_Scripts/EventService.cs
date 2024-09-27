using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json;

[Serializable]
public class Events
{
	public Event[] events;

	public Events(Event[] events)
	{
		this.events = events;
	}
}

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
	[SerializeField] private bool _fake200Response;

	private Queue<Event> _eventQueue;
	private float _currentCooldown;
	private bool _requestInProcess;

	public static EventService Instance { get; private set; }

	private void Awake()
	{
		Instance = this;

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

		if (Mathf.Approximately(_currentCooldown, 0f) &&
			_eventQueue.Count > 0 &&
			_requestInProcess == false)
		{
			_currentCooldown = _cooldownBeforeSend;
			StartCoroutine(SendEventsCoroutine());
		}
	}

	private void Enqueue(Event @event)
	{
		_eventQueue.Enqueue(@event);

		var queueJson = JsonConvert.SerializeObject(_eventQueue);
		PlayerPrefs.SetString(EventQueuePref, queueJson);

		Debug.Log($"Enqueue: {_eventQueue.Count}");
	}

	private void DequeueAmount(int amount)
	{
		for (var i = 0; i < amount; i++)
		{
			_eventQueue.Dequeue();
		}

		var queueJson = JsonConvert.SerializeObject(_eventQueue);
		PlayerPrefs.SetString(EventQueuePref, queueJson);

		Debug.Log($"Dequeue: {_eventQueue.Count}");
	}

	private IEnumerator SendEventsCoroutine()
	{
		_requestInProcess = true;

		var eventsArray = _eventQueue.ToArray();
		var events = new Events(eventsArray);

		var json = JsonConvert.SerializeObject(events);

		using (var request = UnityWebRequest.Post(_serverUrl, json))
		{
			yield return request.SendWebRequest();

			if (request.responseCode == 200 ||
				_fake200Response)
			{
				DequeueAmount(eventsArray.Length);
			}
			else
			{
				Debug.LogError($"Request error: {request.responseCode}");
			}
		}

		_requestInProcess = false;
	}

	public void TrackEvent(string type, string data)
	{
		var @event = new Event(type, data);
		Enqueue(@event);
	}
}
