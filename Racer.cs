// Steve Yeager
// 11/21/2012
// Robot Pirate Racing

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;



/// <summary>
/// Race position for a vehicle. Contains ships current lap, checkpoint, and distance to next checkpoint. When race is completed an event is triggered.
/// </summary>
public class Racer : MonoBehaviour, IComparable
{
	#region Variables
	public string racerName {get; private set;}
	public int lap;
	int laps;
	public TimeSpan[] lapTimes;
	TimeSpan lapStart;
	TimeSpan lastLapTime;
	int _checkpoint;
	/// <summary>Returns current checkpoint, updates the vehicles position, and finishes the race if applicable.</summary>
	public int checkpoint
	{
		get { return _checkpoint; }
		set 
		{
			lastCheckpoint = checkpoint;
			_checkpoint++;
			if (_checkpoint > checkpoints)
			{
				_checkpoint = 1;
			}
			if (_checkpoint == 2)
			{
				FinishedLap();
				if (LapEvent != null) LapEvent(lastLapTime);
			}
			if (!isFinished)
			{
				nextCheckpointPosition = GameObject.Find("Checkpoint_"+_checkpoint).transform.position;
			}
		}
	}
	public int lastCheckpoint {get; private set;}
	int checkpoints;
	public float distanceToCheckpoint;
	public int position {get; private set;}
	public delegate void FinishedHandler();
	public event FinishedHandler Finished;

	public delegate void LapHandler(TimeSpan completedTime);
	public event LapHandler LapEvent;

	public bool isFinished;
	public float finishTime {get; private set;}
	Vector3 nextCheckpointPosition;
	Transform myTransform;
	#endregion


	public int CompareTo(object obj)
	{
		Racer other = (Racer)obj;

		if (lap.CompareTo(other.lap) != 0)
		{
			return -lap.CompareTo(other.lap);
		}
		else if (checkpoint.CompareTo(other.checkpoint) != 0)
		{
			return -checkpoint.CompareTo(other.checkpoint);
		}
		else
		{
			return distanceToCheckpoint.CompareTo(other.distanceToCheckpoint);
		}
	} // End CompareTo


	public override string  ToString()
	{
		return racerName + "; Lap: " + lap + "; Checkpoint: " + _checkpoint + "; Next Checkpoint: " + distanceToCheckpoint + "; Finished: " + isFinished.ToString() + (isFinished ? "; Finished Time: "+finishTime : "") + ";";
	} // End ToString


	void Awake()
	{
		// get references
		myTransform = transform;

		racerName = GetComponent<ShipBase>().pirateName;

		Race_Manager.main.StartRaceEvent += Go;
	} // End Awake


	void Start()
	{
		// get race info
		lap = 0;
		laps = Race_Manager.main.laps;
		lapTimes = new TimeSpan[laps];
		_checkpoint = 1;
		checkpoints = Race_Manager.main.checkpoints;
		distanceToCheckpoint = 0f;
		isFinished = false;
		finishTime = 0f;
		Finished += FinishedRace;

		// get first checkpoint past finishline
		nextCheckpointPosition = GameObject.Find("Checkpoint_2").transform.position;
	} // End Start


	void Update()
	{
		if (isFinished) return;

		distanceToCheckpoint = Vector3.Distance(myTransform.position, nextCheckpointPosition);
	} // End Update


	void OnDestroy()
	{
		Race_Manager.main.StartRaceEvent -= Go;
	} // End OnDestroyed


	void Go()
	{
		lapStart = DateTime.Now.TimeOfDay;
	} // End Go


	public void SetPosition(int position)
	{
		this.position = position;
	} // End SetPosition


	void FinishedLap()
	{
		if (lap > 0)
		{
			lapTimes[lap-1] = DateTime.Now.TimeOfDay-lapStart;
			lapStart = DateTime.Now.TimeOfDay;
			lastLapTime = lapTimes[lap-1];
		}
		
		if (lap == laps)
		{
			if (Finished != null) Finished();
		}
		else
		{
			lap++;
		}
	} // End FinishedLap


	/// <summary>
	/// The vehicle has finished the race. Saves the finish time.
	/// </summary>
	void FinishedRace()
	{
		isFinished = true;
		finishTime = Time.time;
		Race_Manager.main.RacerFinished();
		print(finishTime);
	} // End FinishRace

} // End RacePosition class