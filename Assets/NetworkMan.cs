﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    // Start is called before the first frame update
    void Start()
    {
        ClientID = null;
        len = 0;
        udp = new UdpClient();

        udp.Connect("54.159.195.244", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");

        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
    }

    void OnDestroy()
    {
        udp.Dispose();
    }


    public enum commands
    {
        NEW_CLIENT,
        UPDATE
    };

    [Serializable]
    public class Message
    {
        public commands cmd;
    }

    [Serializable]
    public class Player
    {
        public string id;

        [Serializable]
        public struct receivedColor
        {
            public float r;
            public float g;
            public float b;
        }
        public receivedColor color;
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
    public class PlayerForJson
    {
        public Quaternion rotation;
        public Vector3 position;
    }

        [Serializable]
    public class NewPlayer
    {
        public GameObject playerObj;
        public Player player;
        public NewPlayer()
        {
            playerObj = null;
            player = new Player();
        }
    }
    List<NewPlayer> newPlayers = new List<NewPlayer>();
    public int len = 0;
    public String ClientID = null;
    bool anyNewClient = false;

    [Serializable]
    public class GameState
    {
        public Player[] players;
    }

    public Message latestMessage;
    public GameState lastestGameState;
    void OnReceived(IAsyncResult result)
    {
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;

        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);

        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        //Debug.Log("Got this: " + returnData);

        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try
        {
            switch (latestMessage.cmd)
            {
                case commands.NEW_CLIENT:
                    anyNewClient = true;
                    Debug.Log("New Client Entered");
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    if (anyNewClient)
                    {
                        anyNewClient = false;
                        ShowAllClient();
                    }
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    public void SendPlayerInfo(Transform trans)
    {
        PlayerForJson playerInfo = new PlayerForJson();
        playerInfo.position = trans.position;
        playerInfo.rotation = trans.rotation;

        string jsonString = JsonUtility.ToJson(playerInfo);
        Debug.Log(jsonString);
        Byte[] sendBytes = Encoding.ASCII.GetBytes(jsonString);
        udp.Send(sendBytes, sendBytes.Length);
    }

    void SpawnPlayers()
    {
        if (lastestGameState.players.Length > 0 && ClientID == null)
        {
            ClientID = lastestGameState.players[lastestGameState.players.Length - 1].id;
        }
        while (len <= lastestGameState.players.Length - 1)
        {
            NewPlayer temp = new NewPlayer();
            temp.playerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Renderer rend = temp.playerObj.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Standard"));
            temp.playerObj.GetComponent<Renderer>().material = rend.material;
            temp.playerObj.transform.position = new Vector3(len * 2, 0, 0);
            temp.playerObj.AddComponent<ClientCube>();

            temp.player.color = lastestGameState.players[len].color;
            temp.player.id = lastestGameState.players[len].id;
            temp.playerObj.GetComponent<ClientCube>().id = temp.player.id;

            temp.playerObj.AddComponent<PlayerCS>();
            temp.playerObj.GetComponent<PlayerCS>().id = temp.player.id;
            temp.playerObj.GetComponent<PlayerCS>().netMan = this;

            newPlayers.Add(temp);
            len++;
        }
    }

    void UpdatePlayers()
    {
        if (ClientID == null) return;
        for (int i = 0; i < lastestGameState.players.Length; i++)
        {
            // if (ClientID == lastestGameState.players[i].id)
            // {
            //Debug.Log("Find Update Target");
            //Debug.Log(new Color(lastestGameState.players[i].color.r, lastestGameState.players[i].color.g, lastestGameState.players[i].color.b));

            Color color = new Color(lastestGameState.players[i].color.r, lastestGameState.players[i].color.g, lastestGameState.players[i].color.b);
            newPlayers[i].player.color.r = color.r;
            newPlayers[i].player.color.g = color.g;
            newPlayers[i].player.color.b = color.b;
            newPlayers[i].playerObj.GetComponent<Renderer>().material.SetColor("_Color", color);
            
            if (ClientID != lastestGameState.players[i].id)
            {
                newPlayers[i].playerObj.transform.position = lastestGameState.players[i].position;
                newPlayers[i].playerObj.transform.rotation = lastestGameState.players[i].rotation;
            }
            else
            {
                lastestGameState.players[i].position = newPlayers[i].playerObj.transform.position;
                lastestGameState.players[i].rotation = newPlayers[i].playerObj.transform.rotation;
            }
            //newPlayers[i].playerObj.transform.position = lastestGameState.players[i].position;
            //newPlayers[i].playerObj.transform.rotation = lastestGameState.players[i].rotation;
            // }
        }
    }

    bool DestroyPlayers()
    {
        if (lastestGameState.players.Length < len)
        {
            for (int i = 0; i < lastestGameState.players.Length; i++)
            {
                if (ClientID == lastestGameState.players[i].id)
                {
                    Destroy(newPlayers[i].playerObj);
                    newPlayers[i].playerObj = null;
                    newPlayers[i].player = null;
                    newPlayers.RemoveAt(i);
                    len--;
                    return true;
                }
            }
        }
        return false;
    }

    void RelocateBox()
    {
        for (int i = 0; i < lastestGameState.players.Length; i++)
        {
            newPlayers[i].playerObj.transform.position = new Vector3(i * 2, 0, 0);
        }
    }

    void ShowAllClient()
    {
        for (int i = 0; i < lastestGameState.players.Length; i++)
        {
            Debug.Log("Current Client(" + i + ") ID: " + lastestGameState.players[i].id);
        }
    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        if (DestroyPlayers())
        {
            Debug.Log("Client Dropped");
            ShowAllClient();
            //RelocateBox();
        }
    }
}