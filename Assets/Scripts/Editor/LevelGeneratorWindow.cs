﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class LevelGraphNode{
	private List<LevelGraphNode> connections;
	private bool isCriticalPath;
	private int doorCount = 0;

	private static int id_ = 0;
	private int id;

	public LevelGraphNode(bool isCriticalPath){
		connections = new List<LevelGraphNode> ();
		this.isCriticalPath = isCriticalPath;
		id = id_++;
	}

	public bool IsCriticalPath {
		get {
			return this.isCriticalPath;
		}
	}

	public List<LevelGraphNode> Connections {
		get {
			return this.connections;
		}
	}

	public void AddConnection(LevelGraphNode otherNode){
		connections.Add (otherNode);
		otherNode.IncreaseDoorCount();
	}

	public void IncreaseDoorCount(){
		doorCount++;
	}

	public int ID{
		get{ return id; }
	}

	public int DoorCount {
		get {
			return this.doorCount + connections.Count;
		}
	}
}

public class LevelGraph{
	private List<LevelGraphNode> rootnodes;
	private LevelGraphNode rootnode;
	private int roomsCount;
	private int nodesCreated;
	private int critPathLength;
	private int maxDoors;
	private float distribution;

	public LevelGraph(){
		rootnodes = new List<LevelGraphNode> ();
	}

	public void GenerateGraph (int roomsCount, int critPathLength, int maxDoors, float distribution){
		rootnodes.Clear ();	
		nodesCreated = 0;

		this.roomsCount = roomsCount;
		this.critPathLength = critPathLength;
		this.maxDoors = maxDoors;
		this.distribution = distribution;

		CreateCriticalPath ();
		ShuffleRootnodes ();
		CreateSideRooms ();
		PrintGraph (rootnode);
		Debug.Log ("Created: " + nodesCreated.ToString ());
	}

	private void CreateCriticalPath(){
		LevelGraphNode prevNode = new LevelGraphNode (true);
		rootnode = prevNode;
		rootnodes.Add (rootnode);
		nodesCreated++;

		for (int i = 1; i < critPathLength; i++) {
			LevelGraphNode newNode = new LevelGraphNode (true);
			prevNode.AddConnection (newNode);
			rootnodes.Add (newNode);
			prevNode = newNode;
			nodesCreated++;
		}
	}

	private void ShuffleRootnodes(){
		rootnodes = rootnodes.OrderBy (n => Random.value).Select (n => n).ToList ();
	}

	private void CreateSideRooms(){
		int sideRoomCount = roomsCount - critPathLength; //Remaining rooms are side rooms

		//No siderooms to be created, return
		if (sideRoomCount == 0) {
			return;
		}
		//Each rootnode has a certain supply of doors to be created
		//These doors don't have to be used by the rootnode or it's child all at once
		//Since CreateSideRooms will be later called recursively, the supply will be used up until it's zero		
		int supplyPerNode = (int)Mathf.Ceil ((sideRoomCount / (float)critPathLength) / distribution);

		for (int i = 0; i < critPathLength; i++) {
			//How many rooms can be created until roomsCount is reached
			int availableRooms = roomsCount - nodesCreated;
			//Has been previously computed. Since ceil was used for rounding, there's the
			//Potential for supplyPerNode to be larger than the amount of availableRooms.
			int sideRoomsSupply = supplyPerNode > availableRooms ? availableRooms : supplyPerNode;
			//Recursively create subNodes
			CreateSideRooms (rootnodes [i], sideRoomsSupply);
		}
	}

	private void CreateSideRooms(LevelGraphNode node, int roomSupply){
		int availableDoors = Mathf.Max (0, maxDoors - node.DoorCount);
		//There's nothing to do if no doors can be created
		//This will prevent endless loops since the roomSupply eventually drains
		//Prevent, that more doors are created than roomsCounts defines
		if (availableDoors == 0 || roomSupply <= 0 || nodesCreated >= roomsCount)
			return;
		//At least one door should be placed, else return
		int min = 1; 
		//Only create as much doors as there are available
		//Don't create more doors than the supply offers
		int max = Mathf.Min (roomSupply, availableDoors); 
		//The amount of rooms to be created
		//Since Range's max is exclusive I have to add 1 in order to make it inclusive
		int roomsCreated = Random.Range (min, max + 1);
		int remainingSupply = roomSupply - roomsCreated;
		//Prevent possible division by zero.
		int supplyPerNode = (remainingSupply > 0) ? (int)Mathf.Ceil (remainingSupply / (float)roomsCreated) : 0;
		//Create new graph nodes, recursively call this function again with the remainingSupply
		for (int i = 0; i < roomsCreated; i++) {
			LevelGraphNode newNode = new LevelGraphNode (false);
			node.AddConnection (newNode);
			int newNodeSupply = (supplyPerNode > remainingSupply) ? Mathf.Max(0, remainingSupply) : supplyPerNode;
			CreateSideRooms (newNode, newNodeSupply);
			nodesCreated++;
		}
	}

	private void PrintGraph(LevelGraphNode node){
		foreach (LevelGraphNode nextNode in node.Connections) {
			string nodeID = node.IsCriticalPath ? node.ID.ToString() + "*" : node.ID.ToString();
			string nextNodeID = nextNode.IsCriticalPath ? nextNode.ID.ToString() + "*" : nextNode.ID.ToString();
			Debug.Log (nodeID + " -> " + nextNodeID);
		}
		foreach (LevelGraphNode nextNode in node.Connections) {
			PrintGraph (nextNode);
		}
	}

	public List<LevelGraphNode> Nodes {
		get {
			return this.rootnodes;
		}
	}
}

public class LevelGeneratorWindow : EditorWindow {

	private int roomCount;
	private int critPathLength;
	private int maxDoors;
	private float distribution;

	[MenuItem("Window/Level Generator")]
	public static void ShowWindow(){
		EditorWindow.GetWindow (typeof(LevelGeneratorWindow));
	}

	void OnGUI(){

		EditorGUILayout.Space ();

		roomCount = EditorGUILayout.IntField ("Room Count", roomCount);
		roomCount = Mathf.Max (0, roomCount);
		critPathLength = EditorGUILayout.IntField ("Critical Path", critPathLength);
		critPathLength = Mathf.Clamp (critPathLength, 2, roomCount);
		maxDoors = EditorGUILayout.IntField("Max. Doors", maxDoors);
		maxDoors = Mathf.Clamp (maxDoors, 3, 10);
		distribution = EditorGUILayout.Slider ("Distribution", distribution, 0.05f, 1f);

		EditorGUILayout.Space ();

		if (GUILayout.Button ("Generate Level")) {
			LevelGraph levelGraph = new LevelGraph ();
			levelGraph.GenerateGraph (roomCount, critPathLength, maxDoors, distribution);
		}
	}
}
